# UI Toolkit 성능 분석 및 한계

- **작성일**: 2026-03-29
- **카테고리**: performance
- **상태**: 조사완료

---

## 1. 요약

UI Toolkit은 데이터 집약형 화면(대규모 목록, 복잡한 그리드, 빈번한 데이터 갱신)에서 UGUI 대비 명확한 성능 우위를 보인다. 커뮤니티 벤치마크에서 1000개 요소 기준 드로우콜 9배 감소, 프레임 CPU 시간 3배 개선이 보고되었다. 그러나 월드 공간 UI 미지원, CSS 키프레임 애니메이션 부재, 커스텀 셰이더 미지원이라는 구조적 한계가 있으며 2026년 현재 이 세 영역의 로드맵은 진행 중이나 릴리스 시기는 불확실하다. 모바일·콘솔에서는 PanelSettings 스케일 모드와 포커서블 요소 탐색 설정이 필수다.

---

## 2. 상세 분석

### 2.1 성능: UI Toolkit vs UGUI

#### 핵심 차이: 렌더링 아키텍처

**UGUI 렌더링 흐름:**
```
변경 발생 → Canvas dirty 마킹 → 다음 렌더 프레임에 전체 Canvas 메시 재구성
→ 배치(batch) 계산 → 드로우콜 제출
```

**UI Toolkit 렌더링 흐름:**
```
변경 발생 → 해당 VisualElement만 dirty 마킹 → Yoga 레이아웃 재계산 (필요한 서브트리만)
→ UIR(UI Renderer)가 증분 업데이트 → 드로우콜 제출
```

UGUI는 Canvas 단위로 재구성하기 때문에 Canvas 내 단 하나의 요소가 변경되어도 Canvas 전체가 dirty가 된다. 이것이 "Canvas를 정적/동적으로 분리하라"는 UGUI 최적화 원칙의 근거다. UI Toolkit의 UIR은 변경된 요소만 증분 업데이트하므로 이 문제가 구조적으로 없다.

#### 커뮤니티 벤치마크 결과

출처: Angry Shark Studio 블로그 (2025), 1000개 상호작용 가능 요소 기준:

| 지표 | UGUI | UI Toolkit | 비율 |
|------|------|-----------|------|
| 드로우콜 | ~45 | ~5 | 9x 감소 |
| 프레임 CPU 시간 | 12.5ms | 4.2ms | 3x 개선 |
| 메모리 사용 | ~125MB | ~48MB | 2.6x 감소 |
| 100개 항목 생성 속도 | 85ms | 15ms | 5.7x 개선 |
| 스크롤 가능 항목 수 (버벅임 없음) | ~500개 | 10,000개+ | 20x+ |

**주의**: 이 수치는 특정 벤치마크 조건에서의 결과이며, 실제 프로젝트에서는 UI 복잡도, 텍스처 수, 애니메이션 여부에 따라 크게 달라진다.

#### UI Toolkit이 빠른 경우

- 많은 요소가 한 화면에 표시될 때 (인벤토리, 목록, 그리드)
- 데이터가 자주 갱신되는 HUD (리소스, HP 등)
- 대규모 ListView (가상화로 DOM 요소 수 최소화)
- 복잡한 폼 화면 (설정, 캐릭터 생성 등)

#### UGUI가 유리한 경우

- 요소가 적고 각 요소의 애니메이션이 복잡한 화면
- Animator/Timeline 기반 복잡한 UI 시퀀스
- 월드 공간 UI (명함 표지판, 적 HP바 등)
- 파티클 + UI 혼합 (UGUI Canvas + Particle System)
- 커스텀 셰이더가 필요한 특수 효과

#### 드로우콜 배칭: UI Toolkit의 접근

UI Toolkit은 **동적 아틀라스(Dynamic Atlas)** 시스템을 통해 텍스처를 자동으로 하나의 아틀라스로 합쳐 드로우콜을 최소화한다. PanelSettings에서 아틀라스 설정을 조정할 수 있다.

배칭이 깨지는 주요 원인:
1. 서로 다른 텍스처를 사용하는 요소가 인접할 때 (동적 아틀라스로 일부 해결)
2. 중첩 마스크 (`overflow: hidden` + 둥근 모서리) — 스텐실 배치 변경으로 배치 깨짐
3. `opacity` 값이 1이 아닌 요소 — 별도 렌더 대상 필요
4. `generateVisualContent`로 커스텀 메시를 그리는 요소

배칭 유지를 위한 **UsageHints** 활용:

```csharp
// 자주 이동하는 요소 (예: 드래그 아이콘)
element.usageHints = UsageHints.DynamicTransform;

// 자식 요소 다수가 DynamicTransform인 컨테이너
container.usageHints = UsageHints.GroupTransform;

// 중첩 마스크 컨테이너 (배치 최적화)
maskContainer.usageHints = UsageHints.MaskContainer;

// 색상이 매 프레임 변하는 요소 (예: 데미지 숫자)
flashElement.usageHints = UsageHints.DynamicColor;

// 화면을 크게 차지하는 배경 요소
background.usageHints = UsageHints.LargePixelCoverage;
```

**중요**: `usageHints`는 패널에 추가하기 **전**에 설정해야 한다. 패널에 추가된 후 변경하면 예외가 발생하거나 1프레임 패널티가 발생한다.

#### 메모리: VisualElement vs GameObject

UGUI는 모든 UI 요소가 GameObject + 여러 컴포넌트(RectTransform, CanvasRenderer, Graphic 등)로 구성된다. UI Toolkit의 VisualElement는 순수 C# 객체로, MonoBehaviour와 GameObject 오버헤드가 없다.

| 항목 | UGUI Button 1개 | UI Toolkit Button 1개 |
|------|----------------|----------------------|
| GameObject | 1개 (Transform 포함) | 없음 |
| MonoBehaviour | Button, Image, RectTransform 등 | 없음 |
| 네이티브 오브젝트 | Unity 내부 Transform, Renderer 등 | 없음 |
| C# 오브젝트 | 여러 컴포넌트 | Button (VisualElement 서브클래스) 1개 |
| Hierarchy 표시 | O | X |

---

### 2.2 ListView 가상화 성능

#### 가상화 동작 원리

ListView는 실제로 화면에 보이는 영역만큼의 VisualElement만 생성하고, 스크롤 시 기존 요소를 재활용한다.

```
전체 데이터: 10,000개 항목
fixedItemHeight: 64px
ListView 높이: 640px → 10개 항목 표시 가능

실제 생성되는 VisualElement: ~12개 (버퍼 포함)
스크롤 시: makeItem 호출 없음, bindItem만 호출
```

이 방식은 전통적인 UGUI ScrollRect + 수동 Object Pool과 동일한 원리이지만, Unity가 내장 구현을 제공한다는 점이 다르다.

#### 가상화 모드 비교

| 모드 | 설정 | 동작 | 성능 | 사용 조건 |
|------|------|------|------|----------|
| `FixedHeight` | `fixedItemHeight` 필수 | 모든 행이 동일 높이 | 최적 | 항목 높이 일정할 때 |
| `DynamicHeight` | 높이 자동 계산 | 각 행이 다른 높이 가능 | 비용 높음 | 높이 가변 콘텐츠 |

```csharp
// FixedHeight (권장)
_listView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
_listView.fixedItemHeight = 64f;

// DynamicHeight (높이 가변 시만)
_listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
```

#### 아이템 수별 성능 가이드

| 항목 수 | 권장 방식 | 주의사항 |
|---------|---------|---------|
| 1 ~ 50개 | ListView 또는 직접 생성 | 가상화 불필요할 수도 있음 |
| 50 ~ 500개 | ListView FixedHeight | 가상화 명확한 이점 |
| 500 ~ 5,000개 | ListView FixedHeight 필수 | bindItem 최적화 중요 |
| 5,000개+ | ListView FixedHeight + 데이터 페이지네이션 고려 | 메모리 주의 |

#### bindItem/unbindItem 최적화

```csharp
// 좋은 bindItem: 빠른 직접 접근
_listView.bindItem = (element, index) =>
{
    var data = _items[index];
    // 캐시된 쿼리 결과 사용 (초기화 시 저장해둔 것)
    var nameLabel  = element.Q<Label>("item-name"); // 매번 쿼리 — 성능 주의
    nameLabel.text = data.Name;
};

// 더 나은 bindItem: makeItem에서 캐시
_listView.makeItem = () =>
{
    var row = _template.Instantiate();
    // 서브트리 쿼리는 makeItem에서만 (1번만 실행)
    row.userData = new RowCache
    {
        NameLabel   = row.Q<Label>("item-name"),
        LevelLabel  = row.Q<Label>("item-level"),
        PortraitVE  = row.Q<VisualElement>("item-portrait"),
    };
    return row;
};

_listView.bindItem = (element, index) =>
{
    var data  = _items[index];
    var cache = (RowCache)element.userData; // 쿼리 없이 바로 접근
    cache.NameLabel.text  = data.Name;
    cache.LevelLabel.text = $"Lv.{data.Level}";
    cache.PortraitVE.style.backgroundImage = new StyleBackground(data.Portrait);
};

private struct RowCache
{
    public Label NameLabel;
    public Label LevelLabel;
    public VisualElement PortraitVE;
}
```

#### unbindItem에서 이벤트 구독 해제

```csharp
// bindItem에서 이벤트 등록 시
_listView.bindItem = (element, index) =>
{
    var data = _items[index];
    var btn = element.Q<Button>("item-action-btn");
    // 이전 구독이 있을 수 있으므로 unbindItem에서 해제 필수
    btn.userData = data; // 클로저 대신 userData 사용
    btn.clicked += HandleItemButtonClicked;
};

_listView.unbindItem = (element, index) =>
{
    var btn = element.Q<Button>("item-action-btn");
    btn.clicked -= HandleItemButtonClicked;
    btn.userData = null;
};
```

---

### 2.3 프로파일링 도구

#### UI Toolkit Debugger

Unity 에디터 메뉴: `Window > UI Toolkit > Debugger`

주요 기능:
- **Element Picker**: 화면의 VisualElement를 클릭해 즉시 선택
- **Visual Tree Inspector**: 전체 VisualElement 계층 탐색
- **Styles Inspector**: 선택된 요소의 USS 속성 확인 (어디서 왔는지 파일/라인까지 표시)
- **Layout Inspector**: resolved style (최종 계산값) 확인
- **Live Update**: 런타임 중 실시간 트리 변화 확인

디버거는 런타임 중에도 동작하므로, Play Mode에서 `Window > UI Toolkit > Debugger`를 열면 실시간 상태를 확인할 수 있다.

#### Unity Profiler에서 UI Toolkit 마커

프로파일러 창에서 `CPU Usage` 뷰의 `Timeline` 또는 `Hierarchy`에서 확인할 수 있는 핵심 마커:

| 마커 | 의미 | 병목 시 원인 |
|------|------|-------------|
| `UIR.Update` | UIR이 변경된 요소를 처리하는 비용 | 요소 변경이 너무 많음 |
| `UIR.Render` | 드로우콜 제출 비용 | 드로우콜 수가 많음 |
| `UIElements.Layout` | Yoga 레이아웃 계산 비용 | 복잡한 레이아웃 트리, 빈번한 변경 |
| `UIElements.Repaint` | 요소 다시 그리기 | generateVisualContent 과다 사용 |
| `ListView.Refresh` | ListView rebind 비용 | bindItem 콜백 비용 |

```
프로파일러 사용 방법:
1. Window > Analysis > Profiler 열기
2. CPU 탭 선택
3. Play Mode 진입
4. 병목 프레임 클릭
5. "UIR" 또는 "UIElements" 마커 검색
6. 각 마커의 Self/Total 시간 확인
```

#### Frame Debugger에서 드로우콜 확인

`Window > Analysis > Frame Debugger`를 열면 UI Toolkit의 드로우콜을 직접 확인할 수 있다.

- "UI Toolkit" 섹션에서 각 배치(batch) 확인
- 배치가 깨지는 위치와 이유 파악 (텍스처 변경, 스텐실 변경 등)
- 배치 수를 최소화하는 방향으로 레이아웃 최적화

#### 성능 병목 진단 체크리스트

```
[ ] Profiler: UIR.Update 시간이 2ms 초과 → 불필요한 style 변경 제거
[ ] Profiler: UIElements.Layout 시간이 1ms 초과 → 레이아웃 트리 간소화
[ ] Frame Debugger: 배치 수 > 20 → 동적 아틀라스 설정 점검
[ ] Frame Debugger: 마스크 관련 배치 다수 → UsageHints.MaskContainer 적용
[ ] ListView: bindItem 시간 > 0.5ms → makeItem에서 쿼리 캐시
[ ] 화면 전환: Rebuild() 호출 확인 → RefreshItems()로 대체 가능한지 검토
```

---

### 2.4 현재 한계 (2026 기준)

#### 2.4.1 월드 공간 UI 미지원

**현황**: UI Toolkit은 Screen Space Overlay와 Screen Space Camera 모드만 지원한다. UGUI의 `Canvas.renderMode = WorldSpace`에 해당하는 기능이 없다.

**공식 문서**: `PanelSettings.renderMode`의 기본값은 `PanelRenderMode.ScreenSpaceOverlay`이며, 월드 공간 모드는 문서에 언급되지 않는다.

**RenderTexture 우회 방법**:

```csharp
// 1. PanelSettings.targetTexture에 RenderTexture 할당
// Inspector에서 설정하거나 코드에서:
var rt = new RenderTexture(512, 256, 0,
    SystemInfo.GetCompatibleFormat(GraphicsFormat.R8G8B8A8_SRGB,
                                   FormatUsage.Render));
_panelSettings.targetTexture = rt;

// 2. 씬의 Quad/Plane에 RenderTexture를 Material로 적용
_worldQuad.GetComponent<Renderer>().material.mainTexture = rt;

// 3. 입력 전달: UI Toolkit은 RenderTexture 위의 클릭을 자동으로 처리하지 않음
//    SetScreenToPanelSpaceFunction3D로 좌표 변환 구현 필요
_panelSettings.SetScreenToPanelSpaceFunction3D((panel, screenPosition) =>
{
    // 레이캐스트로 Quad 위의 UV 좌표를 패널 좌표로 변환
    // 구현이 복잡하며 터치 멀티포인트 등에서 한계 존재
    return ConvertScreenToQuadUV(screenPosition, panel);
});
```

**한계**: 이 방법은 다음 문제가 있다:
- 입력 처리가 복잡하고 멀티터치 미지원
- 마스킹(`overflow: hidden`에서 임의 형상 마스킹)이 월드 공간에서 제한됨
- 안티에일리어싱(MSAA) 설정이 까다로움 (GraphicsFormat 맞춰야 함)
- 3D 깊이 순서 처리 (다른 오브젝트에 가려지는 것) 수동 구현 필요

**로드맵**: Unity는 월드 공간 UI Toolkit을 공식 로드맵에 올렸으나 구체적 릴리스 시기는 2026년 기준 미발표다.

#### 2.4.2 애니메이션 한계

**CSS Transitions만 가능**: UI Toolkit은 CSS transitions를 지원하지만 CSS @keyframes 애니메이션은 미지원이다.

**USS transitions로 애니메이션 가능한 속성** (일부):
```
opacity, width, height, left, right, top, bottom
background-color, border-color, border-*-width
color, font-size, margin-*, padding-*
rotate, scale, translate, transform-origin
flex-basis, flex-grow, flex-shrink
```

**USS transitions로 애니메이션 불가능한 속성**:
```
display, flex-direction, flex-wrap, justify-content
overflow, position, visibility, white-space
-unity-font, -unity-font-style, -unity-text-align
```

**Animator/Timeline 미통합**: UGUI는 Animator 컴포넌트를 Canvas 계층에 연결할 수 있지만, UI Toolkit의 VisualElement에는 Animator를 직접 연결할 수 없다.

**DOTween 우회 방법**:

```csharp
// DOTween으로 USS transition 한계를 우회
// CSS keyframe 불가 → DOTween Sequence로 대체

public static async UniTask PlayEntryAnimation(VisualElement panel, float duration)
{
    // DOTween은 transform을 직접 조작하지 않고
    // style 속성을 Update에서 직접 변경
    panel.style.opacity = 0f;
    panel.style.translate = new Translate(0, 30, 0);

    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = Mathf.SmoothStep(0f, 1f, t);

        panel.style.opacity = eased;
        panel.style.translate = new Translate(0, 30f * (1f - eased), 0);

        await UniTask.Yield();
    }

    panel.style.opacity = 1f;
    panel.style.translate = new Translate(0, 0, 0);
}
```

또는 DOTween의 `DOVirtual.Float`를 활용:

```csharp
// DOTween 활용 (DOTween은 MonoBehaviour 없이도 동작)
public UniTask FadeInAsync(VisualElement element, float duration)
{
    var tcs = new UniTaskCompletionSource();
    DOTween.To(
        () => element.resolvedStyle.opacity,
        v => element.style.opacity = v,
        1f,
        duration
    ).OnComplete(() => tcs.TrySetResult());
    return tcs.Task;
}
```

#### 2.4.3 런타임 데이터 바인딩 (Unity 6.3)

Unity 6.3 LTS에서 런타임 데이터 바인딩이 정식 지원되었다. 이전 버전(Unity 6.0, 6.1)에서는 에디터 UI 전용이었다.

```csharp
// Unity 6.3+ 런타임 바인딩 예시
public class PlayerData : INotifyBindablePropertyChanged
{
    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

    private int _gold;
    [CreateProperty]
    public int Gold
    {
        get => _gold;
        set
        {
            if (_gold == value) return;
            _gold = value;
            Notify(nameof(Gold));
        }
    }

    private void Notify(string property)
        => propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
}

// 바인딩 설정
var label = root.Q<Label>("gold-label");
label.dataSource = playerData;
label.SetBinding("text", new DataBinding
{
    dataSourcePath = new PropertyPath(nameof(PlayerData.Gold)),
    bindingMode    = BindingMode.ToTarget,
});
```

**주의**: 바인딩 시스템의 성능 오버헤드는 단순 `label.text = value` 직접 갱신 대비 약간 높다. 매우 빈번하게 변하는 값(예: 초당 수십 번 갱신)에는 직접 갱신이 더 적합할 수 있다.

#### 2.4.4 UGUI에는 있고 UI Toolkit에는 없는 기능

| 기능 | UGUI | UI Toolkit | 우회 방법 |
|------|------|-----------|---------|
| RawImage | `RawImage` 컴포넌트 | `style.backgroundImage` (일부 제한) | VisualElement background-image |
| Mask | `Mask` 컴포넌트 | `overflow: hidden` | overflow: hidden + border-radius |
| Sprite Renderer 혼합 | Canvas + SortingOrder | 불가 | RenderTexture 우회 |
| Particle System 통합 | Canvas 위 Particle 가능 | 불가 | 별도 Camera + RenderTexture |
| AnimationClip 연동 | Animator 컴포넌트 연결 가능 | 불가 | DOTween 또는 UniTask |
| 3D 오브젝트 월드 공간 | Canvas.WorldSpace | RenderTexture만 | 복잡한 우회 |
| Image.Type.Tiled | 타일 반복 이미지 | background-repeat (USS) | USS background-repeat |
| Image.Type.Sliced | 9-slice | -unity-slice-* USS 속성 | 지원됨 |
| Image.Type.Filled | 원형/방사형 fill | generateVisualContent | Painter2D Arc 사용 |

#### 2.4.5 커스텀 셰이더 미지원

UI Toolkit의 VisualElement에 커스텀 셰이더(Material)를 직접 적용하는 기능은 2026년 현재 미지원이다. UGUI는 `Graphic.material`을 통해 커스텀 셰이더를 UI에 적용할 수 있다.

**우회 방법**:
- UGUI Canvas를 UI Toolkit과 겹쳐서 커스텀 셰이더가 필요한 요소만 UGUI로 구현
- `generateVisualContent`로 커스텀 메시를 그리되 표준 UI 셰이더 내에서 표현
- RenderTexture + 후처리 효과 적용 (간접적 방법)

**로드맵**: Unity는 커스텀 셰이더 지원을 계획 중이나 구체적 시기 미발표.

#### 2.4.6 벡터 그래픽 지원 현황

`generateVisualContent` + `Painter2D` API로 런타임에 2D 벡터 그래픽을 그릴 수 있다.

지원되는 기능:
- 직선, 곡선(Bezier, Arc)
- 패스(Fill, Stroke)
- 기본적인 도형 그리기

SVG 파일 지원:
- Unity에서 SVG 파일을 `VectorImage` 에셋으로 임포트 가능 (com.unity.vectorgraphics 패키지 필요)
- `style.backgroundImage = new StyleBackground(vectorImage)` 로 배경으로 사용 가능

한계:
- 복잡한 SVG 필터, 그라데이션 완전 지원 미흡
- 실시간 SVG 애니메이션은 `generateVisualContent`로 직접 구현해야 함

---

### 2.5 모바일·콘솔 고려사항

#### 터치 입력 처리

UI Toolkit은 `PointerDownEvent`, `PointerUpEvent`, `PointerMoveEvent`로 터치를 처리한다. PC 마우스와 동일한 이벤트 타입을 공유하므로 별도 코드 분기 없이 동작한다.

```csharp
element.RegisterCallback<PointerDownEvent>(evt =>
{
    // evt.pointerId: 터치 ID (멀티터치 구분)
    // evt.position: 화면 좌표
    Debug.Log($"Pointer {evt.pointerId} down at {evt.position}");
});

// 멀티터치: pointerId 0, 1, 2... 순서
element.RegisterCallback<PointerDownEvent>(OnTouch, TrickleDown.TrickleDown);
```

#### PanelSettings 해상도/DPI 스케일링

모바일 다양한 화면 크기 대응:

```
PanelSettings 설정:
- Scale Mode: Scale With Screen Size (권장)
- Reference Resolution: 1920 x 1080
- Screen Match Mode: Match Width Or Height
- Match: 0.5 (너비와 높이 균형)
```

```csharp
// 코드에서 PanelSettings 스케일 확인
var panel = GetComponent<UIDocument>().runtimePanel;
float scale = panel.scale;
```

#### 콘솔/게임패드 탐색

UI Toolkit은 포커서블(focusable) 요소 간의 탐색을 `NavigationMoveEvent`로 처리한다.

```csharp
// 기본 탐색 활성화: 요소를 focusable로 설정
button.focusable = true;
button.tabIndex  = 0;

// 탐색 이벤트 커스텀
button.RegisterCallback<NavigationMoveEvent>(evt =>
{
    if (evt.direction == NavigationMoveEvent.Direction.Right)
    {
        evt.PreventDefault(); // 기본 탐색 취소
        _customNextElement.Focus(); // 커스텀 다음 요소
    }
});

// 포커스 설정
button.Focus();
```

**콘솔 탐색 주의사항**:
- `tabIndex`로 탐색 순서 명시 설정 권장
- `NavigationSubmitEvent`로 A/X 버튼 클릭 처리
- `NavigationCancelEvent`로 B/O 버튼 뒤로 가기 처리
- Panel 단위로 포커스가 격리되므로 여러 UIDocument 사용 시 포커스 전환에 주의

#### Input System 연동

New Input System 패키지와 UI Toolkit은 공식적으로 연동된다. `EventSystem`(UI Toolkit용) 컴포넌트가 자동으로 Input System의 이벤트를 UI Toolkit으로 라우팅한다.

```
설정 순서:
1. Scene에 EventSystem GameObject 추가
   (GameObject > UI > Event System 시 자동 생성)
2. 기존 UGUI EventSystem이 있으면 충돌 가능 — 하나만 유지
3. Input System Package의 "UI / Point", "UI / Click" 등의 액션이
   자동으로 UI Toolkit의 PointerEvent로 변환됨
```

---

### 2.6 알려진 버그 및 우회 방법

#### 버그 1: display: none → Flex 전환 시 CSS Transition 미발동

**현상**: `style.display = DisplayStyle.Flex` 직후 `AddToClassList`를 호출하면 CSS transition이 발동하지 않는다.

**원인**: 요소가 레이아웃에 참여하기까지 1프레임이 필요하다.

**우회**:
```csharp
element.style.display = DisplayStyle.Flex;
element.schedule.Execute(() => element.AddToClassList("--visible"));
// 또는
await UniTask.Yield(); // 1프레임 대기
element.AddToClassList("--visible");
```

#### 버그 2: ListView bindItem에서 중복 이벤트 구독

**현상**: 스크롤 시 버튼 클릭이 여러 번 발생한다.

**원인**: bindItem이 요소 재활용 시 매번 호출되어 이전 구독이 해제되지 않은 채로 새 구독이 쌓인다.

**우회**:
```csharp
_listView.unbindItem = (element, index) =>
{
    var btn = element.Q<Button>("action-btn");
    btn.clicked -= OnActionButtonClicked; // 반드시 해제
};
```

#### 버그 3: OnEnable/Awake 타이밍

**현상**: `Awake`에서 `rootVisualElement.Q<T>()` 호출 시 null이 반환된다.

**원인**: UIDocument의 UXML 로딩은 OnEnable에서 완료된다.

**우회**: 반드시 `OnEnable`에서 쿼리 수행.

#### 버그 4: UGUI EventSystem과 UI Toolkit EventSystem 충돌

**현상**: UGUI와 UI Toolkit을 혼용할 때 입력이 한쪽에만 전달된다.

**원인**: Scene에 두 개의 EventSystem이 존재할 때 발생.

**우회**: 하나의 EventSystem만 유지. UGUI 전용 컴포넌트(StandaloneInputModule 등)가 UI Toolkit에도 영향을 주므로, Input System 패키지 사용 시 `InputSystemUIInputModule`으로 통합.

#### 버그 5: usageHints 패널 추가 후 변경 시 예외

**현상**: `element.usageHints = UsageHints.DynamicTransform`을 패널에 추가한 후 변경하면 예외 발생.

**우회**: 패널에 추가하기 전에 설정.

```csharp
var element = new VisualElement();
element.usageHints = UsageHints.DynamicTransform; // 패널 추가 전에 설정
root.Add(element); // 이후 변경 불가
```

---

### 2.7 Unity 로드맵 (2026 기준)

공식 Unity 로드맵 및 포럼 발표 기반 정리.

#### 발표된 예정 기능

| 기능 | 상태 (2026-03 기준) | 비고 |
|------|-------------------|----|
| 월드 공간 UI | 개발 중 (In Progress) | 정확한 릴리스 버전 미발표 |
| 커스텀 셰이더 | 계획 중 (Planned) | 시기 미발표 |
| 런타임 데이터 바인딩 | Unity 6.3에서 출시 | INotifyBindablePropertyChanged 기반 |
| TabView / ToggleButtonGroup | Unity 6에서 출시 완료 | 이미 사용 가능 |
| MultiColumnListView | 출시 완료 | 컬럼 정렬 내장 |
| 벡터 그래픽 개선 | 지속 개선 중 | com.unity.vectorgraphics |

#### 월드 공간 UI에 대한 공식 입장

Unity 포럼 및 블로그에서 여러 차례 월드 공간 UI가 중요한 요청임을 인정했다. 2024-2025 Unity Unite 발표에서 로드맵 항목으로 확인되었으나 Unity 6.x 시리즈(6000.x)에서의 출시 여부는 미확정이다.

**실무적 결론**: 월드 공간 UI(적 HP바, 아이템 이름표 등)는 당분간 UGUI + WorldSpace Canvas 조합이 안전하다.

---

### 2.8 서드파티 생태계

#### 공식 Unity 샘플

- **QuizU** (Unity Asset Store): UI Toolkit으로 만든 퀴즈 게임 UI 예제. 화면 전환, 애니메이션, 데이터 바인딩 패턴 포함.
  - Asset Store: `com.unity.template.mobile2d` 또는 Asset Store에서 "QuizU" 검색
- **UIToolkitUnityRoyaleRuntimeDemo** (GitHub): 카드형 타워 디펜스 게임의 런타임 UI Toolkit 구현 예제.
  - URL: `https://github.com/Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo`
  - 주의: Unity 2020.1 기준 작성 — 최신 Unity 6와 API 일부 차이 있음

#### 커뮤니티 오픈소스 라이브러리

| 라이브러리 | 용도 | 상태 |
|-----------|------|------|
| `uitoolkit-graph-view` 계열 | 노드 그래프 에디터 | 에디터 전용이 많음 |
| `Yarn Spinner for Unity` | 대화 시스템 (UXML 뷰 옵션 포함) | 런타임 지원 |

**주의**: UI Toolkit 런타임용 서드파티 생태계는 UGUI에 비해 아직 빈약하다. 대부분의 오픈소스 라이브러리는 에디터 UI 확장에 집중되어 있다.

#### 실무 커뮤니티 리소스

- **Unity Discussions**: `discussions.unity.com` — UI Toolkit 서브섹션에서 활발한 Q&A
- **Unity Blog**: 주요 버전 릴리스마다 UI Toolkit 업데이트 포스트 게시
- **게임 개발 블로그**: loglog.games, angry-shark-studio.com 등에서 실전 경험 공유

---

## 3. 베스트 프랙티스

### DO (권장)

- **usageHints를 패널 추가 전에 설정** — 자주 이동하는 요소에 `DynamicTransform`, 색상 변하는 요소에 `DynamicColor`
- **ListView.fixedItemHeight 항상 명시** — FixedHeight 가상화의 필수 조건
- **bindItem에서 Q<T>() 대신 userData 캐시 활용** — 스크롤마다 쿼리하면 성능 낭비
- **RefreshItems() 우선 사용, Rebuild()는 최소화** — 전체 재구성은 비용이 크다
- **PanelSettings Scale With Screen Size** — 다양한 해상도 대응 필수
- **Profiler에서 UIR.Update와 UIElements.Layout 마커 확인** — 병목 지점 조기 발견
- **Frame Debugger로 드로우콜 배치 수 확인** — 배치 수 최소화가 성능 핵심

### DON'T (금지)

- **usageHints를 패널 추가 후 변경 금지** — 예외 또는 1프레임 패널티
- **bindItem에서 이벤트 구독 후 unbindItem 해제 생략 금지** — 이벤트 누수
- **Update에서 layout 강제 계산 금지** — `element.layout`을 Update에서 매 프레임 읽으면 레이아웃 강제 동기 계산 트리거
- **display: none 직후 즉시 CSS transition 트리거 금지** — schedule.Execute 또는 1프레임 대기
- **월드 공간 UI에 UI Toolkit 사용 금지** — RenderTexture 우회는 복잡하고 제한적, UGUI 사용 권장
- **복잡한 타임라인 시퀀스 애니메이션에 CSS transition만 의존 금지** — DOTween 또는 UniTask 커스텀 루프 사용

### CONSIDER (상황별)

- **UGUI + UI Toolkit 혼용** — 월드 공간 HP바는 UGUI, 메뉴/HUD는 UI Toolkit으로 분리
- **동적 아틀라스 설정 조정** — PanelSettings에서 아틀라스 크기와 필터 설정으로 배칭 최적화
- **DynamicHeight ListView** — 행 높이가 가변적일 때만 (성능 비용 감수)
- **런타임 데이터 바인딩 (Unity 6.3+)** — 자주 갱신되지 않는 데이터에 적합, 초당 수십 회 갱신 데이터는 직접 갱신이 더 적합

---

## 4. UGUI 대비 비교

### 성능 요약표

| 시나리오 | UGUI | UI Toolkit | 결론 |
|---------|------|-----------|------|
| 1000개 정적 요소 | 드로우콜 ~45 | 드로우콜 ~5 | UI Toolkit 압도적 유리 |
| 단순 HUD (요소 10개) | 동등 | 동등 | 차이 없음 |
| 대규모 목록 (1000개) | ScrollRect + 수동 풀 | ListView 가상화 | UI Toolkit 유리 |
| 빈번한 텍스트 갱신 | Canvas dirty 위험 | 개별 요소만 dirty | UI Toolkit 유리 |
| 복잡한 애니메이션 | Animator/DOTween/Timeline | DOTween (제한적) | UGUI 유리 |
| 월드 공간 UI | 완전 지원 | RenderTexture 우회만 | UGUI 유리 |
| 메모리 (요소 1000개) | ~125MB | ~48MB | UI Toolkit 유리 |
| 생성 속도 (100개) | 85ms | 15ms | UI Toolkit 유리 |

### 아키텍처 비교

| 측면 | UGUI | UI Toolkit |
|------|------|-----------|
| 렌더링 단위 | Canvas (전체 재구성) | VisualElement (증분 업데이트) |
| 레이아웃 | RectTransform + LayoutGroup | Yoga/Flexbox (USS) |
| 스타일 | Inspector 설정 + 코드 | USS (선언형) |
| 상태 관리 | 코드로 직접 속성 변경 | USS 클래스 + 코드 |
| 계층 표현 | Hierarchy 창에 GameObject | Debugger 창에 VisualElement 트리 |
| 씬 의존성 | 높음 (프리팹 기반) | 낮음 (UXML 독립) |

---

## 5. 예제 코드

### 5.1 UsageHints 실전 설정

```csharp
// ResourceHUD 설정 예시
private void OnEnable()
{
    var root = _document.rootVisualElement;

    // HUD 전체 컨테이너 — 항상 표시되는 정적 요소
    var hudBar = root.Q<VisualElement>("hud-bar");
    // 특별한 힌트 불필요 (정적)

    // 자원 수치 레이블 — 자주 색상/값 변경
    var goldLabel = root.Q<Label>("gold-label");
    goldLabel.usageHints = UsageHints.DynamicColor; // 반짝임 효과 최적화

    // 드래그 아이콘 — 위치 자주 변경
    var dragIcon = root.Q<VisualElement>("drag-icon");
    dragIcon.usageHints = UsageHints.DynamicTransform;
}
```

### 5.2 ListView userData 캐시 패턴

```csharp
private sealed class UnitRowCache
{
    public readonly Label NameLabel;
    public readonly Label LevelLabel;
    public readonly Label HpLabel;
    public readonly VisualElement Portrait;

    public UnitRowCache(VisualElement row)
    {
        NameLabel = row.Q<Label>("unit-name");
        LevelLabel = row.Q<Label>("unit-level");
        HpLabel   = row.Q<Label>("unit-hp");
        Portrait  = row.Q<VisualElement>("unit-portrait");
    }
}

private void SetupListView()
{
    _listView.makeItem = () =>
    {
        var row = _template.Instantiate();
        row.userData = new UnitRowCache(row); // 한 번만 쿼리
        return row;
    };

    _listView.bindItem = (element, index) =>
    {
        var cache = (UnitRowCache)element.userData; // 쿼리 없이 접근
        var unit  = _units[index];
        cache.NameLabel.text  = unit.Name;
        cache.LevelLabel.text = $"Lv.{unit.Level}";
        cache.HpLabel.text    = $"{unit.Hp}/{unit.MaxHp}";
        cache.Portrait.style.backgroundImage = new StyleBackground(unit.Portrait);
    };
}
```

### 5.3 DOTween + VisualElement 페이드 유틸리티

```csharp
public static class UIAnimationExtensions
{
    /// <summary>
    /// DOTween으로 VisualElement opacity 페이드인
    /// </summary>
    public static UniTask FadeInAsync(this VisualElement element, float duration)
    {
        var tcs = new UniTaskCompletionSource();
        element.style.opacity = 0f;
        element.style.display = DisplayStyle.Flex;

        DOTween.To(
            () => element.resolvedStyle.opacity,
            v  => element.style.opacity = v,
            1f,
            duration
        )
        .SetEase(Ease.OutCubic)
        .OnComplete(() => tcs.TrySetResult());

        return tcs.Task;
    }

    /// <summary>
    /// DOTween으로 VisualElement opacity 페이드아웃 후 display none
    /// </summary>
    public static UniTask FadeOutAsync(this VisualElement element, float duration)
    {
        var tcs = new UniTaskCompletionSource();

        DOTween.To(
            () => element.resolvedStyle.opacity,
            v  => element.style.opacity = v,
            0f,
            duration
        )
        .SetEase(Ease.InCubic)
        .OnComplete(() =>
        {
            element.style.display = DisplayStyle.None;
            tcs.TrySetResult();
        });

        return tcs.Task;
    }
}

// 사용 예시
await _dialogPanel.FadeInAsync(0.25f);
await _dialogPanel.FadeOutAsync(0.25f);
```

---

## 6. UI_Study 적용 계획

| 주제 | 예제 목표 | 검증 지표 |
|------|---------|---------|
| ListView 성능 | 1000개 유닛 목록, 정렬+필터 | 스크롤 60fps 유지, 프로파일러 bindItem < 0.5ms |
| UsageHints | HUD 자원 수치 반짝임 | DynamicColor vs 기본값 드로우콜 비교 |
| 모달 애니메이션 | display+transition 패턴 | schedule.Execute 없을 때 vs 있을 때 차이 확인 |
| DOTween 연동 | 화면 전환 슬라이드 | CSS transition으로 불가한 시퀀스를 DOTween으로 |
| 프로파일링 실습 | Frame Debugger로 배치 수 확인 | 최적화 전후 드로우콜 비교 |

---

## 7. 참고 자료

1. [Unity Manual: Performance Considerations (Runtime)](https://docs.unity3d.com/Manual/UIE-performance-consideration-runtime.html)
2. [Unity ScriptRef: UsageHints](https://docs.unity3d.com/ScriptReference/UIElements.UsageHints.html)
3. [Unity ScriptRef: VisualElement.usageHints](https://docs.unity3d.com/ScriptReference/UIElements.VisualElement-usageHints.html)
4. [Unity Manual: Masking (overflow: hidden + MaskContainer)](https://docs.unity3d.com/Manual/UIE-masking.html)
5. [Unity ScriptRef: PanelSettings.targetTexture](https://docs.unity3d.com/ScriptReference/UIElements.PanelSettings-targetTexture.html)
6. [Unity Manual: Event Dispatching](https://docs.unity3d.com/Manual/UIE-Events-Dispatching.html)
7. [Unity Manual: Runtime Event System](https://docs.unity3d.com/Manual/UIE-Runtime-Event-System.html)
8. [Unity Manual: Best Practices for Managing Elements](https://docs.unity3d.com/Manual/UIE-best-practices-for-managing-elements.html)
9. [Unity Manual: Runtime Data Binding (Unity 6.3)](https://docs.unity3d.com/Manual/UIE-runtime-binding.html)
10. [Unity Manual: USS Animatable Properties](https://docs.unity3d.com/Manual/UIE-USS-Properties-Reference.html)
11. [Unity Manual: Painter2D / generateVisualContent](https://docs.unity3d.com/Manual/UIE-generate-2d-visual-content.html)
12. [Angry Shark Studio: UGUI vs UI Toolkit 2025](https://www.angry-shark-studio.com/blog/unity-ui-toolkit-vs-ugui-2025-guide/)
13. [loglog.games: UI Toolkit First Steps](https://loglog.games/blog/unity-ui-toolkit-first-steps/)

---

## 8. 미해결 질문

- [ ] **Unity 6.3 런타임 바인딩 성능 오버헤드**: `INotifyBindablePropertyChanged` 기반 바인딩이 직접 `label.text = value` 대비 실측 CPU 비용 비교 필요
- [ ] **동적 아틀라스 설정 최적화**: PanelSettings의 Atlas 크기(Default: 4096x4096)가 메모리 vs 드로우콜에 미치는 영향 측정 필요
- [ ] **월드 공간 RenderTexture 방식 입력 처리**: SetScreenToPanelSpaceFunction3D의 멀티터치 지원 여부 및 실제 구현 난이도
- [ ] **ListView DynamicHeight vs FixedHeight 실측 성능 차이**: 500개 항목에서 프로파일러로 실측 비교
- [ ] **UI Toolkit + UGUI 동시 사용 시 EventSystem 설정 최적 방법**: Input System 패키지 기반 단일 EventSystem으로 두 시스템 모두 처리 가능한지
- [ ] **Unity 6 로드맵 업데이트**: 월드 공간 UI와 커스텀 셰이더 지원의 구체적 버전 발표 모니터링 필요
- [ ] **콘솔 플랫폼 (PS5/Xbox) NavigationMoveEvent 기본 설정**: 게임패드 UI 탐색의 기본 동작이 플랫폼별로 차이가 있는지 확인 필요
