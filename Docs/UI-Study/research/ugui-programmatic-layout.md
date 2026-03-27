# UGUI 코드 기반 레이아웃 — 리서치 리포트

- **작성일**: 2026-03-28
- **카테고리**: practice + technology
- **상태**: 조사완료

---

## 1. 요약

Unity UGUI에서 에디터 없이 순수 코드로 UI를 구성하는 것은 가능하지만,
RectTransform의 앵커/피벗/오프셋 개념을 정확히 이해해야 한다.
`LayoutGroup` + `ContentSizeFitter` 조합을 활용하면 하드코딩 픽셀 위치를 피할 수 있으며,
`DefaultControls` 클래스와 커스텀 팩토리 패턴이 코드 기반 UI 생성의 핵심 도구다.
장기적으로 코드 우선(code-first) 워크플로에는 UI Toolkit이 UGUI보다 적합하나,
현재 프로젝트 스택(MV(R)P + VContainer + R3)은 UGUI 기반이므로
UGUI에서 최선의 코드 패턴을 익히는 것이 현실적인 접근이다.

---

## 2. 상세 분석

### 2.1 RectTransform 핵심 개념

UGUI의 모든 위치/크기는 `RectTransform`으로 제어된다. 에디터 없이 코드로 설정할 때
반드시 이해해야 하는 개념:

#### 앵커(Anchor)

앵커는 부모 Rect의 비율(0.0~1.0)로 정의된다.

```
anchorMin = anchorMax → 고정 크기 (non-stretching)
anchorMin != anchorMax → 부모에 맞게 신축 (stretching)
```

| 앵커 설정 | 의미 |
|---|---|
| `(0,0)` / `(0,0)` | 좌하단 고정 |
| `(0.5f, 0.5f)` / `(0.5f, 0.5f)` | 정중앙 고정 |
| `(0,0)` / `(1,1)` | 부모 전체 신축 |
| `(0,1)` / `(0,1)` | 좌상단 고정 |

#### 비신축(Non-Stretching) 위치 설정

```csharp
// anchoredPosition = 앵커로부터 피벗까지의 오프셋
// sizeDelta = 요소의 실제 크기
rt.anchoredPosition = new Vector2(100f, -50f);
rt.sizeDelta = new Vector2(200f, 80f);
```

#### 신축(Stretching) 위치 설정

```csharp
// offsetMin = 좌하단 앵커로부터 좌하단 코너까지
// offsetMax = 우상단 앵커로부터 우상단 코너까지 (Unity는 Top/Right를 음수로 표시)
rt.offsetMin = new Vector2(10f, 10f);   // left=10, bottom=10
rt.offsetMax = new Vector2(-10f, -10f); // right=10, top=10 (내부 여백)
```

#### 부모 전체 채우기 패턴

```csharp
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.offsetMin = Vector2.zero;
rt.offsetMax = Vector2.zero;
```

**주의**: Unity 인스펙터에서 Top=10으로 보이면 offsetMax.y = -10이다. 부호가 반전됨.

---

### 2.2 앵커 프리셋 코드 패턴 (치트시트)

에디터의 Anchor Presets 버튼과 동일한 효과를 코드로 구현:

```csharp
public static class RectTransformExtensions
{
    public enum AnchorPreset
    {
        // 9개 고정 위치
        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight,
        // 신축 변형
        HorStretchTop, HorStretchMiddle, HorStretchBottom,
        VertStretchLeft, VertStretchCenter, VertStretchRight,
        StretchAll
    }

    public static void SetAnchorPreset(this RectTransform rt, AnchorPreset preset)
    {
        switch (preset)
        {
            case AnchorPreset.TopLeft:
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); break;
            case AnchorPreset.TopCenter:
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f); break;
            case AnchorPreset.TopRight:
                rt.anchorMin = new Vector2(1f, 1f); rt.anchorMax = new Vector2(1f, 1f); break;
            case AnchorPreset.MiddleLeft:
                rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(0f, 0.5f); break;
            case AnchorPreset.MiddleCenter:
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); break;
            case AnchorPreset.MiddleRight:
                rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f); break;
            case AnchorPreset.BottomLeft:
                rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 0f); break;
            case AnchorPreset.BottomCenter:
                rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); break;
            case AnchorPreset.BottomRight:
                rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 0f); break;
            case AnchorPreset.HorStretchTop:
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); break;
            case AnchorPreset.HorStretchMiddle:
                rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f); break;
            case AnchorPreset.HorStretchBottom:
                rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f); break;
            case AnchorPreset.VertStretchLeft:
                rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f); break;
            case AnchorPreset.VertStretchCenter:
                rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 1f); break;
            case AnchorPreset.VertStretchRight:
                rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 1f); break;
            case AnchorPreset.StretchAll:
                rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f); break;
        }
    }
}
```

출처: [unity-forge-extension-methods](https://github.com/rfadeev/unity-forge-extension-methods/blob/master/Source/ExtensionMethods/RectTransform/RectTransformExtensions.AnchorPivotPresets.cs)

---

### 2.3 Auto Layout System (LayoutGroup + ContentSizeFitter)

하드코딩 위치를 피하는 가장 실용적인 방법이다.
LayoutGroup 하위에 자식을 추가하면 위치는 자동 계산된다.

#### 컴포넌트 조합 패턴

```csharp
// VerticalLayoutGroup 설정 (수직 목록)
var vlg = go.AddComponent<VerticalLayoutGroup>();
vlg.spacing = 8f;
vlg.padding = new RectOffset(12, 12, 12, 12);
vlg.childAlignment = TextAnchor.UpperLeft;
vlg.childControlWidth = true;    // 자식 너비를 부모에 맞춤
vlg.childControlHeight = false;  // 자식 높이는 자식이 결정
vlg.childForceExpandWidth = true;
vlg.childForceExpandHeight = false;

// ContentSizeFitter (컨테이너가 콘텐츠에 맞게 크기 조절)
var csf = go.AddComponent<ContentSizeFitter>();
csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
```

#### 중첩 레이아웃 즉시 갱신

코드로 자식을 추가한 뒤 같은 프레임에 크기를 읽으려면:

```csharp
// 중첩된 경우 안쪽에서 바깥쪽 순서로 ForceRebuild
LayoutRebuilder.ForceRebuildLayoutImmediate(innerRect);
LayoutRebuilder.ForceRebuildLayoutImmediate(outerRect);
```

**주의**: `ForceRebuildLayoutImmediate`는 성능 비용이 있다. 일반적으로는
`MarkLayoutForRebuild`(지연 재계산)를 사용하고, 즉시 크기를 읽어야 하는 경우에만 Force 사용.

---

### 2.4 DefaultControls — 내장 UI 팩토리

Unity가 제공하는 `DefaultControls` 클래스로 에디터 GameObject 메뉴와 동일한 UI 요소를
코드로 생성할 수 있다.

```csharp
using UnityEngine.UI;

// Resources 설정 (스프라이트/폰트 없이 기본값 사용 가능)
var res = new DefaultControls.Resources();
// res.standard = mySprite; // 필요 시 지정

// 각 위젯 생성
GameObject panel     = DefaultControls.CreatePanel(res);
GameObject button    = DefaultControls.CreateButton(res);
GameObject image     = DefaultControls.CreateImage(res);
GameObject text      = DefaultControls.CreateText(res);
GameObject inputField = DefaultControls.CreateInputField(res);
GameObject scrollView = DefaultControls.CreateScrollView(res);
GameObject scrollbar  = DefaultControls.CreateScrollbar(res);
GameObject slider     = DefaultControls.CreateSlider(res);
GameObject toggle     = DefaultControls.CreateToggle(res);
GameObject dropdown   = DefaultControls.CreateDropdown(res);
GameObject rawImage  = DefaultControls.CreateRawImage(res);

// 부모 지정 (worldPositionStays=false 필수)
button.transform.SetParent(canvasTransform, false);
```

`DefaultControls`가 생성한 오브젝트는 에디터 메뉴로 만든 것과 완전히 동일하다.

---

### 2.5 Canvas 및 UI 계층 코드로 완전 생성

씬에 캔버스가 없는 상태에서 전체를 코드로 만드는 패턴:

```csharp
// 1. Canvas 생성
var canvasGo = new GameObject("Canvas");
var canvas = canvasGo.AddComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;

// 2. 필수 컴포넌트 추가
var scaler = canvasGo.AddComponent<CanvasScaler>();
scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1920, 1080);
canvasGo.AddComponent<GraphicRaycaster>();

// 3. EventSystem (없으면 추가)
if (FindObjectOfType<EventSystem>() == null)
{
    var esGo = new GameObject("EventSystem");
    esGo.AddComponent<EventSystem>();
    esGo.AddComponent<StandaloneInputModule>();
}

// 4. 자식 UI 요소 추가
var res = new DefaultControls.Resources();
var btn = DefaultControls.CreateButton(res);
btn.transform.SetParent(canvasGo.transform, false);

var rt = btn.GetComponent<RectTransform>();
rt.SetAnchorPreset(AnchorPreset.MiddleCenter); // 위의 확장 메서드
rt.sizeDelta = new Vector2(160f, 40f);
rt.anchoredPosition = Vector2.zero;
```

---

### 2.6 프리팹 기반 하이브리드 워크플로

코드 우선 환경(AI 코딩 어시스턴트)에서 가장 현실적인 전략:

```
[전략 A — 구조는 코드, 시각은 프리팹]
1. 코드로 GameObject 계층/컴포넌트/레이아웃 설정
2. PrefabUtility.SaveAsPrefabAssetAndConnect()로 프리팹 저장
3. 에디터에서 프리팹만 열어 색상/크기/간격 미세조정
4. 조정된 프리팹을 런타임에 Instantiate

[전략 B — 레이아웃 그룹으로 위치 위임]
1. 위치 하드코딩 없이 VerticalLayoutGroup/HorizontalLayoutGroup 사용
2. 자식은 크기만 지정, 위치는 LayoutGroup이 자동 계산
3. 비율 기반 앵커(StretchAll 등)로 해상도 대응

[전략 C — Editor 스크립트 + MenuItem]
1. [MenuItem("Tools/UI/Create HUD")] 어트리뷰트로 에디터 메뉴 생성
2. 스크립트 실행 → 계층 자동 생성 → 씬 저장
3. AI가 스크립트를 수정하면 메뉴 실행으로 씬 재생성
```

#### Editor 스크립트 예시

```csharp
#if UNITY_EDITOR
using UnityEditor;

public static class UISceneBuilder
{
    [MenuItem("Tools/UI/Rebuild HUD Scene")]
    public static void RebuildHUD()
    {
        // 기존 Canvas 제거
        var existing = GameObject.Find("Canvas");
        if (existing != null) Object.DestroyImmediate(existing);

        // 새로 생성
        CreateHUDCanvas();

        // 씬 저장 표시
        EditorUtility.SetDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }
}
#endif
```

---

### 2.7 unity-cli를 활용한 AI 코딩 워크플로

이 프로젝트는 `unity-cli`를 사용하므로, 코드 변경 후 즉시 검증할 수 있다:

```bash
# 1. 컴파일 확인
unity-cli editor refresh --compile

# 2. 콘솔 에러 확인
unity-cli console --filter error

# 3. 에디터 스크립트로 UI 재생성 후 확인
unity-cli menu "Tools/UI/Rebuild HUD Scene"

# 4. 씬 저장
unity-cli menu "File/Save Project"

# 5. 플레이모드로 실제 레이아웃 검증
unity-cli editor play --wait
unity-cli exec "var rt = GameObject.Find(\"Canvas/Panel\").GetComponent<RectTransform>(); return $\"{rt.rect.width}x{rt.rect.height} at {rt.anchoredPosition}\";"
```

**핵심 원칙**: AI가 코드를 수정 → `refresh --compile` → 에러 없으면 `exec`으로
런타임 상태 검증 → 위치/크기 숫자로 확인.

---

### 2.8 UGUI vs UI Toolkit (코드 우선 관점)

| 기준 | UGUI | UI Toolkit |
|---|---|---|
| 코드 우선 친화성 | 중간 (GameObject 계층 필요) | 높음 (VisualElement C# API) |
| AI 생성 용이성 | 중간 (앵커/피벗 수동 설정) | 높음 (flexbox 개념으로 직관적) |
| 애니메이션 | Animator/DOTween 통합 우수 | 제한적 (Transitions만) |
| 성능 | 요소 수 증가 시 DrawCall 증가 | 배치 렌더링, 고성능 |
| 기존 스택 호환 | MV(R)P + VContainer + R3 검증됨 | 생태계 미성숙 |
| 학습 곡선 | Unity 네이티브, 자료 풍부 | UXML/USS 학습 필요 |

**현재 프로젝트 결론**: UI_Study는 UGUI 유지. 단, UI Toolkit은 별도 섹션에서
코드 우선 대안으로 비교 학습 가치 있음.

---

## 3. 베스트 프랙티스

### DO (권장)

- [ ] 위치 하드코딩 대신 `LayoutGroup` + 비율 앵커 사용
- [ ] `SetParent(parent, worldPositionStays: false)` 항상 false로 설정
- [ ] Canvas 생성 시 `CanvasScaler` + `GraphicRaycaster` 항상 함께 추가
- [ ] `DefaultControls.CreateXxx()` 활용해 표준 위젯 생성
- [ ] Editor 스크립트로 UI 씬을 메뉴에서 재생성 가능하게 만들기
- [ ] 중첩 LayoutGroup 즉시 갱신 시 안쪽→바깥쪽 순서로 `ForceRebuildLayoutImmediate`
- [ ] 확장 메서드(`SetAnchorPreset`)로 앵커 설정 코드 가독성 향상
- [ ] `unity-cli exec`으로 런타임 RectTransform 값 실시간 검증

### DON'T (금지)

- [ ] `transform.position` 대신 항상 `anchoredPosition` 사용 (UI 요소에 localPosition 사용 금지)
- [ ] 중첩 ContentSizeFitter + LayoutGroup을 임의 순서로 ForceRebuild하지 않기
- [ ] Canvas 없이 RectTransform을 가진 UI 요소 생성 금지 (계층 깨짐)
- [ ] 픽셀 하드코딩으로 여러 해상도 대응 포기 금지
- [ ] `offsetMax.y`에 양수를 넣으면 위가 아니라 아래로 이동 (부호 주의)

### CONSIDER (상황별)

- [ ] 복잡한 정적 레이아웃은 프리팹으로 저장 후 Instantiate
- [ ] 위치가 중요한 HUD는 코드보다 씬 파일 직접 편집 + reserialize 고려
- [ ] uLayout 라이브러리 — flexbox 방식 LayoutGroup 대안 ([GitHub](https://github.com/pokeblokdude/uLayout))
- [ ] 리스트 UI는 FancyScrollView (이미 스택에 포함)로 가상화 처리

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 6000.x | uGUI 2.0.0 패키지 |
| DefaultControls | 2017.1+ | `UnityEngine.UI` 네임스페이스 |
| LayoutRebuilder | 5.x+ | `UnityEngine.UI` 네임스페이스 |
| unity-cli | 최신 | `exec` 명령으로 런타임 검증 |
| uLayout | 2022.3+ | flexbox 기반 LayoutGroup 대체 |

---

## 5. 예제 코드

### 기본 사용법 — 수직 목록 완전 코드 생성

```csharp
public static GameObject CreateVerticalList(Transform parent, int itemCount)
{
    // 컨테이너
    var container = new GameObject("VerticalList");
    container.transform.SetParent(parent, false);

    var rt = container.AddComponent<RectTransform>();
    rt.anchorMin = new Vector2(0f, 0f);
    rt.anchorMax = new Vector2(1f, 1f); // 부모 전체 신축
    rt.offsetMin = rt.offsetMax = Vector2.zero;

    var vlg = container.AddComponent<VerticalLayoutGroup>();
    vlg.spacing = 8f;
    vlg.padding = new RectOffset(10, 10, 10, 10);
    vlg.childControlWidth = true;
    vlg.childControlHeight = false;
    vlg.childForceExpandWidth = true;

    var csf = container.AddComponent<ContentSizeFitter>();
    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    // 아이템
    var res = new DefaultControls.Resources();
    for (int i = 0; i < itemCount; i++)
    {
        var item = DefaultControls.CreateButton(res);
        item.name = $"Item_{i}";
        item.transform.SetParent(container.transform, false);
        item.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 40f);
        item.GetComponentInChildren<Text>().text = $"Item {i + 1}";
    }

    // 중첩 레이아웃 즉시 갱신
    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

    return container;
}
```

### 고급 패턴 — Editor 스크립트로 씬 재생성

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UISceneBuilder
{
    [MenuItem("Tools/UI/Rebuild Study Scene 06")]
    public static void RebuildScene06()
    {
        // 기존 Canvas 제거
        var old = GameObject.Find("Canvas");
        if (old != null) Object.DestroyImmediate(old);

        // Canvas 생성
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // UI 계층 생성
        CreateVerticalList(canvasGo.transform, 5);

        // 씬 저장 마킹
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[UISceneBuilder] Scene 06 rebuilt. Press Ctrl+S to save.");
    }

    private static void CreateVerticalList(Transform parent, int count)
    {
        var panel = DefaultControls.CreatePanel(new DefaultControls.Resources());
        panel.name = "Panel_List";
        panel.transform.SetParent(parent, false);

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.1f);
        rt.anchorMax = new Vector2(0.9f, 0.9f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        for (int i = 0; i < count; i++)
        {
            var btn = DefaultControls.CreateButton(new DefaultControls.Resources());
            btn.name = $"Btn_{i}";
            btn.transform.SetParent(panel.transform, false);
            btn.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 50f);
        }
    }
}
#endif
```

---

## 6. UI_Study 적용 계획

### 예제 씬으로 만들 수 있는 것

1. **코드 기반 앵커 프리셋 데모** — 9개 고정 위치 + 신축 변형을 버튼으로 전환
2. **LayoutGroup 조합 실습** — VerticalLayoutGroup + HorizontalLayoutGroup 중첩
3. **ContentSizeFitter 동적 리스트** — 런타임에 아이템 추가/제거 시 크기 자동 조절
4. **Editor 스크립트 씬 빌더** — MenuItem으로 씬 자동 구성 → unity-cli로 검증

### AI 코딩 보조 검증 루틴

```bash
# 코드 수정 후 표준 검증 루틴
unity-cli editor refresh --compile
unity-cli console --filter error --lines 10
unity-cli menu "Tools/UI/Rebuild Study Scene 06"
unity-cli editor play --wait
unity-cli exec "var canvas = GameObject.Find(\"Canvas\"); return canvas != null ? \"Canvas OK\" : \"Canvas MISSING\";"
unity-cli editor stop
```

---

## 7. 참고 자료

1. [Creating UI elements from scripting — Unity uGUI 2.0.0 공식 문서](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UICreateFromScripting.html)
2. [DefaultControls API — Unity uGUI 2.0.0](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.DefaultControls.html)
3. [RectTransform.anchoredPosition — Unity Scripting API](https://docs.unity3d.com/ScriptReference/RectTransform-anchoredPosition.html)
4. [LayoutRebuilder.ForceRebuildLayoutImmediate — Unity Scripting API](https://docs.unity3d.com/2017.3/Documentation/ScriptReference/UI.LayoutRebuilder.ForceRebuildLayoutImmediate.html)
5. [unity-forge-extension-methods — RectTransform AnchorPivotPresets](https://github.com/rfadeev/unity-forge-extension-methods/blob/master/Source/ExtensionMethods/RectTransform/RectTransformExtensions.AnchorPivotPresets.cs)
6. [uLayout — flexbox 기반 UGUI LayoutGroup 대체](https://github.com/pokeblokdude/uLayout)
7. [Unity UI Toolkit vs UGUI: 2025 Developer Guide](https://medium.com/@studio.angry.shark/unity-ui-toolkit-vs-ugui-2025-developer-guide-8407312c91ed)
8. [Unity Discussions — ForceRebuildLayoutImmediate 중첩 레이아웃 문제](https://discussions.unity.com/t/layoutrebuilder-forcerebuildlayoutimmediate-doesnt-work-correctly-in-nested-layout-layoutrebuilder-does-not-work-in-nested-complex-layout/245556)
9. [Content Size Fitters and Layout Groups — Medium (Sean Duggan)](https://medium.com/@sean.duggan/unity-ui-content-size-fitters-and-layout-groups-e668f7df5bd7)
10. [How to set Anchor Presets via C# — Unity Discussions](https://discussions.unity.com/t/how-to-set-the-new-unity-ui-rect-transform-anchor-presets-via-c-script/143885)
11. [unity-cli GitHub — youngwoocho02](https://github.com/youngwoocho02/unity-cli)

---

## 8. 미해결 질문

- [ ] `unity-cli exec`에서 에디터 모드 중 `LayoutRebuilder.ForceRebuildLayoutImmediate` 호출 가능한지 확인 필요
- [ ] UI Toolkit의 `VisualElement` 기반 코드 우선 패턴 — 별도 리서치로 분리할지?
- [ ] uLayout 패키지 UI_Study 프로젝트에 실제 설치 후 호환성 검증
- [ ] Editor 스크립트 씬 빌더 패턴이 VContainer LifetimeScope와 충돌하는지 여부
