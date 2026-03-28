# UGUI 성능 최적화 기법 종합 리서치

- 카테고리: performance
- 작성일: 2026-03-28
- 상태: 조사완료

---

## Executive Summary

Unity UGUI 성능 최적화의 핵심은 세 가지 병목을 개별적으로 다루는 것이다.

1. **CPU — Canvas Rebuild**: 단일 Canvas에서 하나의 요소가 변경되면 Canvas 전체가 재빌드된다. Sub-Canvas 분리로 오염(dirty) 전파를 차단하는 것이 가장 직접적인 해법이다.
2. **CPU — Batching**: 동일 머티리얼과 텍스처를 공유하지 않으면 드로우콜이 분리된다. SpriteAtlas로 텍스처를 묶고 계층 순서를 관리해야 한다.
3. **GPU — Overdraw**: UGUI의 모든 지오메트리는 Transparent 큐에서 렌더링되므로 반투명 레이어 누적이 즉시 GPU 병목으로 이어진다. 특히 모바일에서 치명적이다.

War Robots 사례에서 최적화 전 UI가 CPU 부하의 최대 30%를 차지했다는 기록이 있다. 개별 최적화 기법의 효과보다 세 병목을 동시에 관리하는 체계적 접근이 중요하다.

---

## 1. Canvas Rebuild 비용

### 1.1 Rebuild가 발생하는 조건

Canvas는 구성 요소의 메시가 변경될 때 dirty 상태로 표시되고, 다음 렌더 전에 배치를 재계산한다. 아래 조작이 Canvas를 dirty로 만든다.

| 트리거 | 비용 수준 | 비고 |
|--------|-----------|------|
| `SetActive(true/false)` | 높음 | 레이아웃 + 배칭 전체 재계산 |
| `RectTransform` 이동/스케일/회전 | 높음 | `UpdateRectTransform` spike |
| `sprite` 교체 | 중간 | 버텍스 dirty |
| 텍스트 내용 변경 | 중간 | 폰트 아틀라스 의존 |
| `color` 변경 | 낮음 | 버텍스 컬러만 dirty |
| `fillAmount` 변경 | 낮음 | 레이아웃 rebuild 없음 |
| `SiblingIndex` 변경 | 높음 | 배칭 순서 재계산 |
| `Animator` 부착 | 매우 높음 | 값 변화 없어도 매 프레임 dirty |

**실측 데이터 (Unity 2022.3.24f1, Google Pixel 1)**

- 단일 요소 RectTransform 변경: 레이아웃 0.2 ms + 배칭 0.65 ms
- 8요소 레이아웃에서 1개 변경: 레이아웃 0.7 ms + 배칭 1.10 ms
- `fillAmount` 변경: 레이아웃 rebuild 없음 (배칭 소폭 증가)

### 1.2 Dirty 상태의 종류

UI 요소는 네 가지 부분 dirty 상태를 가진다. 전체 rebuild보다 부분 상태가 훨씬 저렴하다.

- **Layout dirty**: 크기/위치 재계산 필요
- **Vertices dirty**: 메시 데이터 재생성 필요
- **Material dirty**: 머티리얼 프로퍼티 변경
- **Totally dirty**: 위 모두 포함

### 1.3 Rebuild 최소화 전략

**Animator 제거**

UI Animator는 값이 변하지 않아도 매 프레임 dirty를 발생시킨다. DOTween 또는 R3 기반 트윈으로 교체하거나, 상태가 자주 바뀌는 요소에만 한정적으로 사용한다.

**fillAmount / Shader 기반 애니메이션**

Transform 이동 대신 `Image.fillAmount`, `material.SetFloat()` 등 레이아웃 rebuild를 유발하지 않는 프로퍼티를 활용한다.

**컴포넌트 disable vs GameObject SetActive**

```csharp
// 나쁨: Canvas 전체 rebuild 유발
gameObject.SetActive(false);

// 좋음: 해당 컴포넌트만 렌더링 제외
textComponent.enabled = false;

// 좋음: CanvasGroup으로 배치 보존하며 숨기기
canvasGroup.alpha = 0f;
canvasGroup.blocksRaycasts = false;
```

---

## 2. Canvas 분리 전략

### 2.1 Sub-Canvas의 격리 원리

Sub-Canvas(Canvas 컴포넌트가 붙은 중첩 GameObject)는 dirty 전파를 차단하는 섬(island)이다.

- dirty 자식은 부모 Canvas를 rebuild하지 않는다
- dirty 부모는 자식 Sub-Canvas를 rebuild하지 않는다
- Sub-Canvas는 자체 지오메트리를 유지하고 자체 배칭을 수행한다
- 예외: 부모 Canvas 리사이즈로 자식 Sub-Canvas가 리사이즈될 때는 전파될 수 있다

### 2.2 분리 기준

**갱신 빈도 기반 분리 (권장)**

```
RootCanvas
├── StaticCanvas          ← 첫 표시 시 1회만 배칭, 이후 재사용
│   ├── BackgroundImage
│   ├── FrameDecorations
│   └── StaticLabels
├── DynamicCanvas_HP      ← HP/MP 등 매 프레임 변경
│   └── HealthBar
├── DynamicCanvas_Score   ← 점수, 타이머 등
│   └── ScoreText
└── OverlayCanvas         ← 팝업, 토스트 (간헐적 변경)
    └── ToastMessage
```

**원칙**

- Static 요소는 한 Canvas에 묶는다 — 최초 배칭 후 재사용
- 같은 주기로 변하는 Dynamic 요소는 같은 Sub-Canvas에 묶는다
- 개별 요소를 각각 Sub-Canvas로 만드는 것은 오히려 드로우콜을 증가시킨다
- Unity 5.2 이후 모바일에서 2~3개 Canvas면 대부분의 경우 충분하다

### 2.3 Canvas 컴포넌트 비활성화

```csharp
// Canvas 컴포넌트 비활성화: GPU에 드로우콜 미전송
// 재활성화 시 rebuild 없이 바로 그리기 시작
canvas.enabled = false;
// vs
gameObject.SetActive(false); // rebuild 비용 발생
```

Canvas 컴포넌트를 disable하면 렌더링을 멈추지만 메시 캐시는 유지된다. 재활성화 시 rebuild가 없다는 점이 SetActive와의 핵심 차이다.

---

## 3. 드로우콜 배칭

### 3.1 배칭 조건

같은 Canvas 내에서 다음 조건을 모두 만족해야 배칭된다.

| 조건 | 설명 |
|------|------|
| 동일 텍스처 | SpriteAtlas로 하나의 텍스처에 묶어야 함 |
| 동일 머티리얼 | 기본 UI 머티리얼 공유 |
| 동일 Z값 | UI 요소의 Z position = 0 유지 |
| 계층 간 텍스처 침입 없음 | 중간에 다른 텍스처가 끼어들면 배칭 분리 |

### 3.2 배칭이 깨지는 원인

**텍스처 침입(Interleaving)**

```
Layer 0: Sprite A (AtlasX)   ← 배칭 1 시작
Layer 1: Sprite B (AtlasX)   ← 배칭 1 계속
Layer 2: Icon C  (AtlasY)    ← 배칭 2! (다른 텍스처가 끼어듦)
Layer 3: Sprite D (AtlasX)   ← 배칭 3! (AtlasX지만 2 이후라 새 배치)
```

해결: AtlasX 요소들을 AtlasY 요소 위나 아래에 모두 배치하고 계층이 섞이지 않도록 정리한다.

**TMP 글리프의 투명 공간**

TextMeshPro의 글자 쿼드는 글리프가 차지하는 면적보다 크다. 투명 공간이 다른 요소의 bounding box와 겹치면 배칭이 분리된다. 텍스트 요소의 위치를 조정하거나 계층 순서를 바꿔 해결한다.

### 3.3 SpriteAtlas 설정

```
SpriteAtlas 최적 설정:
- Max Texture Size: 2048 (모바일) / 4096 (고사양)
- Format: ASTC 6x6 (iOS) / ETC2 (Android)
- Power-of-Two: 강제 (GPU 처리 효율)
- Include in Build: true (V1) / Addressables 연동 (V2)
- Tight Packing: 반투명 스프라이트엔 주의
```

**드로우콜 절감 실측**

SpriteAtlas 도입 전후 비교 사례에서 58개 드로우콜 → 5개로 감소가 보고된다. 비율은 아틀라스 설계에 따라 다르지만 10x 수준의 감소가 가능하다.

### 3.4 렌더 순서 최적화

Frame Debugger에서 배칭이 깨지는 지점을 확인한다.

```
Window > Analysis > Frame Debugger
→ UI Camera Draw Calls 섹션 확장
→ 각 드로우콜의 "Why this draw call can't be batched with previous" 확인
```

Unity 5.6+에서 Frame Debugger가 배칭 실패 이유를 직접 표시한다.

---

## 4. Overdraw 감소

### 4.1 UI Overdraw의 특성

UGUI의 모든 지오메트리는 **Transparent 렌더 큐**에서 렌더링된다. 불투명(Opaque) 렌더링과 달리 depth test로 픽셀을 조기 거부할 수 없어, 화면에 쌓인 모든 UI 레이어가 픽셀마다 알파 블렌딩된다.

- 투명 픽셀이라도 렌더링 비용은 동일하다
- Image 컴포넌트의 투명 영역도 전체 사각형을 렌더링한다
- 모바일 GPU는 fill-rate가 낮아 overdraw에 특히 취약하다
- Shader Graph UI 셰이더는 무작위 15x overdraw가 보고되었다

### 4.2 Overdraw 확인 방법

```
Unity Editor Scene View
→ Draw Mode 드롭다운 → "Overdraw"
→ 밝을수록 overdraw 심함 (흰색 = 극심)
```

또는 모바일 기기에서:
- Android: GPU 제조사 툴 (Mali GPU Analyzer, Adreno Profiler)
- iOS: Xcode Instruments — GPU Frame Capture에서 "Renderer" 탭 고비용 확인

### 4.3 Overdraw 감소 전략

**알파 0으로 숨기는 대신 완전 비활성화**

```csharp
// 나쁨: alpha=0이어도 여전히 렌더링됨
image.color = new Color(1, 1, 1, 0);

// 좋음: 렌더링 큐에서 완전 제거
gameObject.SetActive(false);
// 또는
image.enabled = false;
```

**전체화면 UI일 때 배경 카메라 끄기**

인벤토리 등 전체 화면을 덮는 UI를 띄울 때, 뒤편의 게임 월드 카메라를 비활성화하면 GPU 부하를 크게 줄인다.

**사각형 스프라이트 vs 복잡한 외곽선 스프라이트**

복잡한 외곽선의 스프라이트는 투명 픽셀이 많아 overdraw가 늘어난다. 단순한 직사각형 마스킹을 선호한다.

**RectMask2D 사용 (ScrollView)**

RectMask2D는 영역 밖 요소가 배칭에서 완전히 제외되도록 한다. ScrollView에 반드시 추가해야 하는 이유다.

```csharp
// ScrollView Content 부모에 RectMask2D 추가
// → 화면 밖 아이템이 배칭 계산에서 제외됨
scrollView.GetComponent<ScrollRect>()
    .viewport.gameObject.AddComponent<RectMask2D>();
```

---

## 5. LayoutGroup 대안

### 5.1 LayoutGroup의 비용

LayoutGroup은 dirty가 될 때마다 모든 자식 RectTransform의 크기와 위치를 재계산한다. 비용 수준:

```
GridLayoutGroup > VerticalLayoutGroup > HorizontalLayoutGroup
(자식 수에 비례하여 비용 증가)
```

Unity 공식 문서: "Layout components are relatively expensive, avoid where possible."

IndexedSet_Sort, CanvasUpdateRegistry_SortLayoutList 마커가 프로파일러에서 크게 나타나면 LayoutGroup이 병목임을 의심한다.

### 5.2 RectTransform 앵커 기반 수동 배치

고정된 구조에서는 LayoutGroup 없이 앵커로 동일한 결과를 낼 수 있다.

```csharp
// 2열 레이아웃 — LayoutGroup 없이 앵커로 구현
// 왼쪽 열
leftPanel.anchorMin = new Vector2(0, 0);
leftPanel.anchorMax = new Vector2(0.5f, 1);
leftPanel.offsetMin = Vector2.zero;
leftPanel.offsetMax = Vector2.zero;

// 오른쪽 열
rightPanel.anchorMin = new Vector2(0.5f, 0);
rightPanel.anchorMax = new Vector2(1, 1);
rightPanel.offsetMin = Vector2.zero;
rightPanel.offsetMax = Vector2.zero;
```

앵커 기반 배치는 Native Transform 코드로 처리되어 C# Layout 시스템보다 빠르다.

### 5.3 Custom LayoutGroup

요소 수가 동적이지만 레이아웃 로직이 단순한 경우, `LayoutGroup`을 상속받아 `GetComponent` 호출을 최소화한 커스텀 구현이 표준 LayoutGroup보다 효율적이다.

### 5.4 런타임 LayoutGroup 비활성화

디자인 시에는 LayoutGroup을 사용하고, 런타임에서 `layoutGroup.enabled = false`로 비활성화한 뒤 고정된 위치값을 캐싱하면 정적 UI에서 비용을 제로로 만들 수 있다.

---

## 6. TMP 텍스트 렌더링 최적화

### 6.1 SDF vs Bitmap

| 방식 | 장점 | 단점 | 사용 상황 |
|------|------|------|-----------|
| SDF | 크기 조절 시 선명, Outline/Shadow 가능 | 아틀라스 크기 큼 | 동적 UI, 다양한 크기 텍스트 |
| Bitmap | 고정 크기에서 빠름, 아틀라스 작음 | 크기 조절 시 흐림 | 고정 크기 정적 텍스트 |
| MSDF | SDF보다 날카로운 모서리 | Unity 기본 미지원 (서드파티) | 기하학적 폰트 |

권장: SDF 기본 사용. 소형 고정 크기 텍스트에만 Bitmap 검토.

### 6.2 Font Atlas 최적화

**Character Set 제한**

```
TMP Font Asset Creator
→ Character Set: "Custom Characters"
→ 텍스트 파일로 사용할 문자만 지정
→ 한국어: 자주 쓰는 약 2,350자 (완성형) 우선
→ 전체 유니코드 사용 금지 (아틀라스 폭발)
```

게임에서 실제 사용하는 문자 집합은 대부분 1,000자 이하다. 특히 중국어는 50,000자 이상이므로 반드시 서브셋을 만들어야 한다.

**Atlas 해상도**

```
모바일: Max 2048x2048
PC/콘솔: Max 4096x4096
Padding: 5px (512x512 아틀라스 기준)
```

**Fallback Font**

기본 폰트에 없는 문자는 Fallback Font Chain에서 자동 검색된다. Fallback 체인이 길면 런타임 성능이 저하된다. 자주 쓰는 폰트는 체인 앞쪽에 배치한다.

### 6.3 피해야 할 패턴

**Best Fit 금지**

```csharp
// 절대 사용 금지
tmpText.enableAutoSizing = true; // Best Fit과 동일

// 이유:
// - 각 테스트 크기마다 아틀라스에 새 글리프 추가
// - 다른 Text 컴포넌트의 글리프를 아틀라스에서 밀어냄
// - 연쇄 atlas rebuild 유발
```

**Dynamic Font + Best Fit 조합**

Legacy Text의 Best Fit은 테스트한 모든 크기의 글리프를 아틀라스에 추가하므로 빠르게 아틀라스를 가득 채운다. Unity 5.4 이전에는 버그도 있었다.

### 6.4 머티리얼 프리셋으로 드로우콜 감소

같은 폰트 에셋을 사용하는 TMP 오브젝트들은 기본적으로 배칭된다. 색상이나 Outline이 다른 텍스트를 위해서는 Material Preset을 만든다.

```
TMP Font Asset → Create Material Preset
→ 색상/Outline/Shadow 설정
→ 여러 TMP 오브젝트가 같은 프리셋 공유 → 단일 드로우콜
```

동일 폰트 에셋에서 `color` 프로퍼티를 오브젝트마다 개별 설정하면 머티리얼 인스턴스가 생성되어 배칭이 깨진다.

### 6.5 런타임 텍스트 갱신 최적화

```csharp
// 나쁨: 매 프레임 string 할당
scoreText.text = "Score: " + score.ToString();

// 좋음: 변경 시에만 갱신 + StringBuilder 또는 int overload
if (score != _lastScore)
{
    _sb.Clear();
    _sb.Append("Score: ");
    _sb.Append(score);
    scoreText.SetText(_sb);
    _lastScore = score;
}
```

---

## 7. RaycastTarget 최적화

### 7.1 비용 구조

`GraphicRaycaster`는 매 포인터 이벤트마다 Canvas 내 모든 `raycastTarget = true` 요소와의 교차 검사를 수행한다. 요소가 많을수록 검사 비용이 선형 증가한다.

### 7.2 적용 규칙

```csharp
// raycastTarget = false로 설정해야 하는 요소:
// - 텍스트 레이블 (버튼 위의 텍스트 포함)
// - 배경 이미지 (단순 장식)
// - 아이콘 (버튼/슬롯의 자식)
// - 구분선, 프레임, 장식 이미지

// raycastTarget = true를 유지해야 하는 요소:
// - Button의 배경 Image 1개
// - Slider, Toggle, InputField
// - Scrollbar
```

**에디터 유틸리티 예시**

```csharp
// 선택된 하위 계층의 비상호작용 요소 raycastTarget 일괄 해제
[MenuItem("Tools/UI/Disable RaycastTarget on Non-Interactive")]
static void DisableRaycastTargets()
{
    var graphics = Selection.activeGameObject
        .GetComponentsInChildren<Graphic>();
    foreach (var g in graphics)
    {
        if (g.GetComponent<Selectable>() == null)
            g.raycastTarget = false;
    }
}
```

또한 상호작용이 필요 없는 Canvas에는 `GraphicRaycaster` 컴포넌트 자체를 제거한다. 정보 표시 전용 Canvas는 레이캐스트가 불필요하다.

---

## 8. SetActive vs CanvasGroup.alpha 성능 비교

### 8.1 각 방식의 비용

| 방법 | 레이아웃 비용 | 배칭 비용 | 렌더링 비용 | 재활성화 비용 |
|------|--------------|-----------|-------------|--------------|
| `SetActive(false/true)` | 높음 | 높음 | 없음 | 높음 (레이아웃 재계산) |
| `Canvas.enabled = false` | 없음 | 없음 | 없음 | 없음 (캐시 재사용) |
| `CanvasGroup.alpha = 0` | 없음 | 낮음 | 없음\* | 없음 |
| `transform.localScale = 0` | 없음 | 있음 | 있음 | 없음 |
| `Image.enabled = false` | 없음 | 낮음 | 없음 | 낮음 |

\* `CanvasGroup.alpha = 0`이면 렌더링 큐에서 제외되어 GPU 비용 없음.

**실측 (War Robots 팀)**

Canvas Group alpha 전환은 SetActive 대비 "배칭과 렌더링 부하를 크게 감소"시킨다고 보고됨.

**Unity 2018+ SetActive 추가 비용**

Unity 2018 이후 Canvas 하위에서 `SetActive(true)`를 호출하면 같은 Canvas의 다른 요소들도 `SyncTransform`을 트리거하여 UI 업데이트 전체 오버헤드가 증가한다.

### 8.2 권장 패턴

```csharp
public class UIPanel : MonoBehaviour
{
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] Canvas _canvas;

    // 빠른 표시/숨김 (Sub-Canvas 있는 경우)
    public void Show() => _canvas.enabled = true;
    public void Hide() => _canvas.enabled = false;

    // CanvasGroup 방식 (인터랙션 제어 포함)
    public void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
```

`SetActive`는 처음 생성/제거 시에만 사용하고, 반복적인 표시/숨김은 Canvas.enabled 또는 CanvasGroup을 사용한다.

---

## 9. 동적 UI 오브젝트 풀링

### 9.1 언제 풀링이 필요한가

| 상황 | 풀링 필요 | 이유 |
|------|-----------|------|
| 인벤토리 (100+ 아이템) | 필수 | 전체 인스턴스화 시 GC + rebuild 폭발 |
| 채팅창 (스크롤) | 필수 | 메시지 추가/제거 반복 |
| 데미지 텍스트 | 권장 | 빈번한 생성/제거 |
| 알림 배너 | 권장 | 순간적 생성 제거 패턴 |
| 고정 메뉴 (10개 이하) | 불필요 | 생성 빈도 낮음 |

### 9.2 올바른 풀링 순서

```csharp
// 잘못된 방법 — 부모 변경 후 비활성화: 두 번 rebuild
pooledItem.transform.SetParent(poolContainer);
pooledItem.SetActive(false); // 두 번째 dirty

// 올바른 방법 — 비활성화 후 부모 변경: 한 번만 rebuild
pooledItem.SetActive(false);                              // 구 계층 dirty 1회
pooledItem.transform.SetParent(poolContainer, false);    // 풀 계층 dirty 없음

// 재사용 시
item.SetActive(false);                                    // 풀 계층 dirty 없음
item.transform.SetParent(contentTransform, false);        // 새 계층 배치
item.SetActive(true);                                     // 새 계층 dirty 1회
```

Unity 공식 권장: "disable first, then reparent."

### 9.3 Unity 기본 ObjectPool 활용

```csharp
using UnityEngine.Pool;

public class UIItemPool : MonoBehaviour
{
    [SerializeField] GameObject _itemPrefab;
    [SerializeField] Transform _poolParent;
    [SerializeField] Transform _contentParent;

    IObjectPool<UIItem> _pool;

    void Awake()
    {
        _pool = new ObjectPool<UIItem>(
            createFunc: () => {
                var go = Instantiate(_itemPrefab, _poolParent);
                return go.GetComponent<UIItem>();
            },
            actionOnGet: item => {
                item.transform.SetParent(_contentParent, false);
                item.gameObject.SetActive(true);
            },
            actionOnRelease: item => {
                item.gameObject.SetActive(false);
                item.transform.SetParent(_poolParent, false);
            },
            actionOnDestroy: item => Destroy(item.gameObject),
            defaultCapacity: 20,
            maxSize: 100
        );
    }
}
```

### 9.4 가상화(Virtualization) 스크롤

스크롤 뷰에서 실제 보이는 아이템만 렌더링하고 나머지는 풀에 보관하는 패턴이다.

```
ScrollRect 없이 직접 구현하거나 외부 라이브러리 사용:
- FancyScrollView (MIT, GitHub: setchi/FancyScrollView)
  → 뷰포트에 맞는 셀 수만 생성, 스크롤 시 재사용
  → 무한 스크롤 지원
- EnhancedScroller (Asset Store 유료)
  → 자체 풀링으로 GC 최소화
- Unity-PooledScrollList (MIT, GitHub: disas69)
  → 수평/수직/그리드 지원
```

모바일에서 ScrollRect + LayoutGroup은 100~200개 이상부터 심각한 성능 저하가 발생한다. FancyScrollView 등의 도입을 검토한다.

**RectMask2D 필수**

```csharp
// 화면 밖 요소를 배칭에서 제외
// RectMask2D 없으면 화면 밖 수백 개 아이템이 모두 배칭에 참여
scrollView.viewport.gameObject.AddComponent<RectMask2D>();
```

---

## 10. 프로파일링 도구 활용

### 10.1 Unity Profiler — UI 관련 마커

| 마커 | 카테고리 | 의미 |
|------|----------|------|
| `Canvas.SendWillRenderCanvases` | UI | C# rebuild 과정 전체 포함 |
| `CanvasUpdateRegistry.PerformUpdate` | UI | 레이아웃 + 버텍스 재계산 |
| `IndexedSet_Sort` | UI | 레이아웃 컴포넌트 정렬 (LayoutGroup 의존) |
| `Canvas.BuildBatch` | Rendering/Other | Native 배치 계산 |
| `Canvas::UpdateBatches` | Rendering | BuildBatch와 동일, 보일러플레이트 포함 |
| `Text_OnPopulateMesh` | UI | 텍스트 메시 생성 (TMP 포함) |
| `UpdateRectTransform` | UI | RectTransform dirty 처리 |

**병목 진단 기준**

- `Canvas.SendWillRenderCanvases` 높음 → Canvas Rebuild 빈번 → Sub-Canvas 분리
- `Canvas.BuildBatch` 높음 → Canvas당 너무 많은 요소 → Canvas 분리
- `IndexedSet_Sort` 높음 → LayoutGroup 과다 → 수동 배치로 교체
- `Text_OnPopulateMesh` 높음 → 텍스트 변경 빈번 → 갱신 제한

### 10.2 UI Profiler (편집기 전용)

```
Window > Analysis > UI Profiler
→ "Batch Viewer" 탭: 왜 배칭이 분리됐는지 확인
→ "Render Hierarchy" 탭: 드로우콜 계층 확인
※ 에디터 전용 — 빌드에서는 사용 불가
```

Batch Viewer의 "Batch Breaking Reason" 항목이 가장 중요하다. 텍스처 차이, Z값 차이, 머티리얼 차이 등 구체적인 원인을 표시한다.

### 10.3 Frame Debugger

```
Window > Analysis > Frame Debugger
→ Enable 클릭
→ "UI Camera" 또는 카메라별 드로우콜 확장
→ 각 드로우콜 클릭 → 우측에 "Why this draw call can't be batched" 표시
```

Unity 5.6+ 기능. 이 메시지로 어떤 요소가 배칭을 깨는지 정확히 파악한다.

### 10.4 커스텀 프로파일러 마커

```csharp
using Unity.Profiling;

public class UIInventoryPresenter : MonoBehaviour
{
    static readonly ProfilerMarker s_RefreshMarker =
        new ProfilerMarker("UIInventory.Refresh");
    static readonly ProfilerMarker s_PoolGetMarker =
        new ProfilerMarker("UIInventory.PoolGet");

    public void RefreshAll()
    {
        using (s_RefreshMarker.Auto())
        {
            foreach (var item in _items)
            {
                using (s_PoolGetMarker.Auto())
                    var view = _pool.Get();
                // ... 데이터 바인딩
            }
        }
    }
}
```

`ProfilerMarker`는 `Profiler.BeginSample`보다 효율적이다 — string을 전송하지 않고 정수 ID만 전달한다. 비개발 빌드에서 완전히 컴파일 아웃된다.

### 10.5 Unity-CLI를 통한 프로파일링

```bash
# 프로파일러 활성화 후 계층 확인
unity-cli profiler enable
unity-cli editor play

# UI 병목 확인 (상위 항목, 0.5ms 이상)
unity-cli profiler hierarchy --min 0.5 --sort self --max 15

# Canvas 관련 항목 집중 확인
unity-cli profiler hierarchy --root Canvas --depth 3

# 30프레임 평균
unity-cli profiler hierarchy --frames 30 --min 0.3
```

---

## 11. 대규모 UI 최적화 (100+ 요소)

### 11.1 인벤토리 / 스킬 트리 전략

**아이템 수 기준 권장 방식**

| 아이템 수 | 전략 |
|-----------|------|
| ~50개 | 일반 ScrollRect + RectMask2D |
| 50~200개 | ObjectPool + ScrollRect |
| 200+개 | 가상화 스크롤뷰 (FancyScrollView 등) |
| 1,000+개 | 가상화 필수 + 청크 단위 로딩 |

**그리드 아이템 풀 구조**

```
ScrollView
├── Viewport [RectMask2D]
│   └── Content
│       ├── ItemSlot_0  ← 재사용 풀 (화면에 보이는 수 + 버퍼 2)
│       ├── ItemSlot_1
│       └── ...
└── [ObjectPool - 화면 밖 아이템 보관]
```

**ScrollRect 최적화 설정**

```csharp
var scrollRect = GetComponent<ScrollRect>();
// LayoutGroup 제거 후 수동 배치
scrollRect.horizontal = false;       // 단방향 스크롤만
scrollRect.movementType = ScrollRect.MovementType.Clamped; // 탄성 반동 제거 (비용)
// 관성 필요 없으면
scrollRect.inertia = false;
```

### 11.2 Canvas 분리 + 청크 갱신

100개 이상 요소를 한 Canvas에 두면 하나가 변경될 때 전체 재배칭이다. 행 단위로 Sub-Canvas를 나누고 변경된 Sub-Canvas만 rebuild되도록 한다.

```
ContentCanvas
├── RowCanvas_0 [Sub-Canvas]  ← 0~9번 슬롯
├── RowCanvas_1 [Sub-Canvas]  ← 10~19번 슬롯
└── ...
```

단, Sub-Canvas가 너무 많으면 드로우콜이 증가하므로 8~10개 요소당 1 Sub-Canvas 정도로 조절한다.

### 11.3 스킬 트리 특수 전략

```csharp
// 잠금 상태 스킬: Image만 표시, RaycastTarget=false
// 해금 가능: Image + Button (RaycastTarget=true)
// 해금 완료: Image only + 시각적 상태 변경

// 연결선(Line): CanvasRenderer로 직접 메시 생성
// → GL.Lines 또는 LineRenderer보다 Canvas 배칭 활용 가능
```

---

## 12. 메모리 최적화

### 12.1 텍스처 압축 형식

| 플랫폼 | 불투명 스프라이트 | 반투명 스프라이트 |
|--------|-----------------|-----------------|
| Android | ETC2 4bit | ETC2 8bit |
| iOS | ASTC 6x6 | ASTC 6x6 |
| PC/콘솔 | DXT1/BC1 | DXT5/BC3 |
| 범용 | ASTC 6x6 | ASTC 6x6 |

ASTC는 블록 크기를 통해 품질-용량 트레이드오프를 세밀하게 조절할 수 있다 (0.89~8 bits/pixel).

Power-of-Two 크기를 강제한다: GPU 처리 효율, 텍스처 압축, 밉맵 생성에서 모두 유리하다.

### 12.2 SpriteAtlas 메모리 관리

**핵심 원칙**: Atlas 내 스프라이트를 하나만 참조해도 Atlas 전체가 메모리에 로드된다.

**Addressables 기반 지연 로딩**

```csharp
// Atlas만 Addressable로 표시 (개별 스프라이트는 표시 금지)
// 스프라이트 참조 방식
[SerializeField] AssetReferenceAtlasedSprite _iconRef;

async UniTask LoadIconAsync()
{
    // 필요할 때만 Atlas 로드
    var sprite = await _iconRef.LoadAssetAsync<Sprite>().ToUniTask();
    _image.sprite = sprite;
}

void OnDestroy()
{
    // 사용 완료 후 명시적 해제
    _iconRef.ReleaseAsset();
}
```

**Late Binding (V1 방식)**

```csharp
// SpriteAtlasManager를 통한 동적 바인딩
SpriteAtlasManager.atlasRequested += RequestAtlas;

void RequestAtlas(string tag, Action<SpriteAtlas> callback)
{
    // Addressables 또는 AssetBundle로 로드 후 콜백
    StartCoroutine(LoadAtlas(tag, callback));
}
```

**SpriteAtlas V2 (Unity 2022.2+)**

```
Project Settings > Editor > Sprite Atlas Mode: Sprite Atlas V2 - Enabled
```

V2는 Asset Database와 통합되어 빌드 파이프라인이 개선됐다. 메모리 누수 버그가 V1에서 보고됐으나 V2에서 해결됐다.

### 12.3 Atlas 설계 원칙

```
아틀라스 분리 기준:
1. 화면 단위 — 메인 메뉴, 인게임 HUD, 인벤토리 등 별도 Atlas
2. 갱신 주기 단위 — 자주 바뀌는 아이콘과 정적 UI 분리
3. 크기 단위 — 대형 이미지(배경, 프레임)는 별도 Atlas
4. 공통 요소 — 버튼, 슬라이더 등 공용 위젯 = 공용 Atlas 1개

피해야 할 패턴:
- 모든 스프라이트를 하나의 거대 Atlas에 넣기
  → 잘 안 쓰는 화면 스프라이트도 항상 메모리에 상주
- 스프라이트마다 개별 텍스처
  → 드로우콜 폭발, 배칭 불가
```

### 12.4 Sprite Atlas 메모리 계산

```
예시: 2048x2048 ASTC 6x6 Atlas
압축 비율: ~2.7 bits/pixel
메모리: 2048 * 2048 * 2.7 / 8 ≈ 1.4 MB

vs 비압축 RGBA32:
2048 * 2048 * 4 bytes = 16 MB

압축으로 약 11배 절감
```

---

## 정리: 우선순위별 체크리스트

### 즉시 적용 (비용 없음)

- [ ] 비상호작용 요소 전체 `raycastTarget = false`
- [ ] 상호작용 없는 Canvas에서 `GraphicRaycaster` 제거
- [ ] Legacy Text를 TextMeshPro로 교체
- [ ] TMP Best Fit(`enableAutoSizing`) 비활성화
- [ ] ScrollView에 `RectMask2D` 추가

### 단기 적용 (설계 변경 최소)

- [ ] Static / Dynamic Canvas 분리
- [ ] Animator 부착 UI 요소를 DOTween으로 교체
- [ ] `SetActive` 반복 호출을 `Canvas.enabled` / `CanvasGroup`으로 교체
- [ ] 동일 화면 스프라이트를 SpriteAtlas로 묶기
- [ ] TMP 폰트 Character Set 제한

### 중기 적용 (구조 변경 필요)

- [ ] LayoutGroup을 RectTransform 앵커로 대체 (정적 UI)
- [ ] ScrollView에 Object Pooling 적용
- [ ] 100+ 아이템 화면에 FancyScrollView 등 가상화 도입
- [ ] Addressables + SpriteAtlas Late Binding으로 메모리 관리

### 측정 선행 필수

- [ ] Profiler에서 `Canvas.SendWillRenderCanvases` / `Canvas.BuildBatch` 기준값 측정
- [ ] Frame Debugger로 배칭 현황 베이스라인 확인
- [ ] Scene View Overdraw 모드로 핫스팟 파악

---

## 소스 및 참고 자료

- [Unity UI Optimization Tips (Unity 공식)](https://unity.com/how-to/unity-ui-optimization-tips)
- [Optimizing Unity UI — Unity Learn](https://learn.unity.com/tutorial/optimizing-unity-ui)
- [Unity UI Profiling: How Dare You Break My Batches? — TheGamedev.Guru](https://thegamedev.guru/unity-ui/profiling-canvas-rebuilds/)
- [How to Optimize UIs in Unity — MY.GAMES (War Robots 팀)](https://medium.com/my-games-company/how-to-optimize-uis-in-unity-slow-performance-causes-and-solutions-c47af453b1db)
- [Unity UI Profiling: How dare you break my Batches? — Game Developer](https://www.gamedeveloper.com/programming/unity-ui-profiling-how-dare-you-break-my-batches-)
- [Unity Draw Call Batching — TheGamedev.Guru](https://thegamedev.guru/unity-performance/draw-call-optimization/)
- [Unity Overdraw Optimization — TheGamedev.Guru](https://thegamedev.guru/unity-gpu-performance/overdraw-optimization/)
- [Unity Addressables & SpriteAtlas Memory — TheGamedev.Guru](https://thegamedev.guru/unity-addressables/spriteatlas-save-memory/)
- [Unity UI Best Practices for Performance — Wayline](https://www.wayline.io/blog/unity-ui-best-practices-for-performance)
- [Unity UI Performance Optimization — UWA Blog](https://blog.en.uwa4d.com/2022/03/08/unity-performance-optimization-%E2%85%A7-ui-module/)
- [Optimizing LayoutElement and LayoutGroup — Medium](https://llmagicll.medium.com/optimizing-ui-performance-in-unity-deep-dive-into-layoutelement-and-layoutgroup-components-b6a575187ee4)
- [SetActive vs CanvasGroup Performance — Unity Discussions](https://discussions.unity.com/t/performance-difference-between-setactive-and-canvasgroup/809668)
- [TextMeshPro Font Atlas Optimization](https://uhiyama-lab.com/en/notes/unity/unity-textmeshpro-high-quality-text-display/)
- [About SDF Fonts — TMP 4.0 공식 문서](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/FontAssetsSDF.html)
- [Sprite Atlas V2 — Unity 공식 문서](https://docs.unity3d.com/2023.2/Documentation/Manual/SpriteAtlasV2.html)
- [FancyScrollView — GitHub (setchi)](https://github.com/setchi/FancyScrollView)
- [Unity ProfilerMarker API](https://docs.unity3d.com/ScriptReference/Unity.Profiling.ProfilerMarker.html)
- [How to Reduce Draw Calls in Unity UI — ZeePalm](https://www.zeepalm.com/blog/how-to-reduce-draw-calls-in-unity-ui)
- [Unity Mobile Game Optimization Guide 2025](https://generalistprogrammer.com/tutorials/unity-mobile-game-optimization-complete-guide)
