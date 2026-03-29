# UI Toolkit Fundamentals

- **작성일**: 2026-03-29
- **카테고리**: technology
- **상태**: 조사완료

---

## 1. 요약

UI Toolkit은 UGUI의 GameObject 기반 구조를 완전히 대체하는 DOM 유사 시각적 트리(visual tree) 모델로, VisualElement가 모든 UI 요소의 기반이 된다. UGUI의 RectTransform + Canvas 조합 대신 UIDocument + PanelSettings 구성을 사용하며, Yoga 엔진 기반 Flexbox 레이아웃이 기본 적용되어 별도 LayoutGroup 없이 자동 배치된다. Unity 6에서 ToggleButtonGroup, Tab/TabView 등 신규 컨트롤이 추가되고 UxmlElement/UxmlAttribute 속성 기반 커스텀 컨트롤 등록 방식이 도입되었다. 런타임 UI 접근은 반드시 OnEnable에서 수행해야 하며, Awake에서는 rootVisualElement가 아직 준비되지 않을 수 있다.

---

## 2. 상세 분석

### 2.1 VisualElement 계층 구조와 시각적 트리

UI Toolkit의 모든 UI 요소는 `VisualElement`에서 파생된다. VisualElement는 MonoBehaviour를 상속하지 않으며, 독립적인 경량 노드로 구성된 **시각적 트리(visual tree)**를 형성한다.

**UGUI vs UI Toolkit 구조 비교**

| 측면 | UGUI | UI Toolkit |
|------|------|-----------|
| 기반 단위 | GameObject + Component | VisualElement (경량 노드) |
| 씬 표현 | Hierarchy 창에 GameObject로 표시 | 시각적 트리 (씬 계층에 없음) |
| 상속 | MonoBehaviour | VisualElement (자체 클래스 트리) |
| 메모리 | GameObject 오버헤드 | 순수 C# 객체, 저오버헤드 |
| 레이아웃 | Anchor/Pivot 수동 배치 | Yoga(Flexbox) 자동 레이아웃 |

**Visual Tree 구조**

```
Panel (PanelSettings로 설정됨)
└── rootVisualElement (UIDocument.rootVisualElement)
    ├── VisualElement (container)
    │   ├── Label
    │   ├── Button
    │   └── VisualElement
    │       └── Toggle
    └── ScrollView
        └── ListView
```

런타임에서 루트는 `UIDocument.rootVisualElement`이고, 에디터 창에서는 `EditorWindow.rootVisualElement`이다.

**DOM과의 유사성**

UI Toolkit의 시각적 트리는 HTML DOM과 매우 유사하다:
- `VisualElement` → `<div>`
- `Label` → `<p>` 또는 `<span>`
- `Button` → `<button>`
- UXML → HTML
- USS → CSS
- UQuery → jQuery/querySelectorAll

### 2.2 VisualElement 라이프사이클 이벤트

VisualElement는 MonoBehaviour의 Awake/Start/OnEnable 같은 콜백을 가지지 않는다. 대신 패널과의 연결 상태 변화를 이벤트로 추적한다.

**AttachToPanelEvent**

VisualElement가 패널에 연결될 때(= 화면에 표시될 때) 발생한다. 계층 구조 전체가 패널에 추가되면 부모, 자식, 손자 모두 이 이벤트를 받는다.

```csharp
public class MyElement : VisualElement
{
    public MyElement()
    {
        RegisterCallback<AttachToPanelEvent>(OnAttach);
        RegisterCallback<DetachFromPanelEvent>(OnDetach);
    }

    private void OnAttach(AttachToPanelEvent evt)
    {
        // evt.destinationPanel: 연결된 패널
        // 여기서 초기화 수행 — 이 시점에 layout 계산 완료 보장 안됨
        Debug.Log($"Attached to panel: {evt.destinationPanel}");
    }

    private void OnDetach(DetachFromPanelEvent evt)
    {
        // evt.originPanel: 분리 전 패널
        // 여기서 리소스 해제 수행
        Debug.Log($"Detached from panel: {evt.originPanel}");
    }
}
```

**DetachFromPanelEvent**

VisualElement가 패널에서 분리되기 직전에 발생한다. 이벤트 구독 해제, 리소스 정리에 사용한다.

**GeometryChangedEvent**

레이아웃 계산 후 요소의 위치나 크기가 변경될 때 발생한다. 씬 첫 프레임 이후 레이아웃이 확정된 시점이다.

```csharp
public class MyElement : VisualElement
{
    public MyElement()
    {
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        Rect oldRect = evt.oldRect;  // 이전 크기/위치
        Rect newRect = evt.newRect;  // 새 크기/위치

        // 레이아웃 의존 계산 수행 (예: 동적 콘텐츠 배치)
        float width = newRect.width;
        float height = newRect.height;
    }
}
```

**이벤트 전파 특성**

| 이벤트 | Trickle Down | Bubble Up | 취소 가능 |
|--------|-------------|-----------|----------|
| AttachToPanelEvent | 아니오 | 아니오 | 아니오 |
| DetachFromPanelEvent | 아니오 | 아니오 | 아니오 |
| GeometryChangedEvent | 아니오 | 아니오 | 아니오 |

주의: 부모 요소는 자식의 Attach/Detach 이벤트를 받지 않는다. 계층 전체가 동시에 받는다.

### 2.3 Yoga/Flexbox 레이아웃 엔진

UI Toolkit은 Facebook의 **Yoga** 엔진을 사용하며, 이는 CSS Flexbox의 서브셋을 구현한다. UGUI의 VerticalLayoutGroup/HorizontalLayoutGroup/GridLayoutGroup을 모두 USS 속성으로 대체한다.

**기본 Flex 동작**

```
기본값: flex-direction: column (세로 배치)
→ UGUI의 VerticalLayoutGroup과 동일
```

**핵심 Flexbox 속성**

```css
/* 방향 설정 */
flex-direction: row;         /* 가로 배치 */
flex-direction: column;      /* 세로 배치 (기본값) */
flex-direction: row-reverse;

/* 주축 정렬 (justify-content) */
justify-content: flex-start;    /* 시작 정렬 (기본) */
justify-content: center;        /* 중앙 정렬 */
justify-content: flex-end;      /* 끝 정렬 */
justify-content: space-between; /* 균등 분배 */
justify-content: space-around;  /* 여백 포함 균등 */

/* 교차축 정렬 (align-items) */
align-items: stretch;  /* 늘이기 (기본) */
align-items: center;
align-items: flex-start;
align-items: flex-end;

/* 크기 유연성 */
flex-grow: 1;        /* 남은 공간 차지 비율 */
flex-shrink: 0;      /* 축소 방지 */
flex-basis: auto;    /* 기본 크기 */
flex: 1;             /* flex-grow:1, flex-shrink:1, flex-basis:0 단축 */

/* 절대 위치 (레이아웃에서 제외) */
position: absolute;
left: 10px;
top: 20px;
```

**Yoga vs 웹 CSS Flexbox 차이점**

| 항목 | 웹 CSS | Unity Yoga |
|------|--------|-----------|
| 기본 flex-direction | row | column |
| flex-wrap | 지원 | 제한적 지원 |
| align-content | 지원 | 지원 |
| grid | 지원 | 미지원 (flex만) |
| z-index | 지원 | 미지원 (트리 순서로 결정) |

### 2.4 UIDocument와 PanelSettings

**UIDocument 컴포넌트**

UIDocument는 MonoBehaviour 컴포넌트로, UXML 파일을 씬에 표시하는 역할을 한다.

```csharp
// UIDocument에서 rootVisualElement 접근
public class GameHUD : MonoBehaviour
{
    private UIDocument _document;
    private Label _scoreLabel;
    private Button _pauseButton;

    // 반드시 OnEnable에서 접근 (Awake는 너무 이름)
    private void OnEnable()
    {
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;

        _scoreLabel = root.Q<Label>("score-label");
        _pauseButton = root.Q<Button>("pause-button");

        _pauseButton.clicked += OnPauseClicked;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (_pauseButton != null)
            _pauseButton.clicked -= OnPauseClicked;
    }

    private void OnPauseClicked() { /* ... */ }
}
```

**PanelSettings 주요 속성**

| 속성 | 설명 | 권장값 |
|------|------|--------|
| Theme Style Sheet | UI 기본 테마 TSS | 런타임 기본 테마 or 커스텀 |
| Scale Mode | UI 스케일링 방식 | Scale With Screen Size |
| Reference Resolution | 기준 해상도 | 1920×1080 |
| Screen Match Mode | 해상도 불일치 처리 | Match Width Or Height |
| Sort Order | 패널 렌더 순서 | 낮을수록 아래 |
| Render Mode | 화면 vs 월드 공간 | Screen Space Overlay (기본) |

**Scale Mode 옵션**

```
Constant Pixel Size    → 픽셀 크기 고정 (해상도 무관)
Constant Physical Size → 물리적 크기 고정 (DPI 기반)
Scale With Screen Size → 화면 크기에 비례 (게임 UI에 권장)
```

**Sort Order와 Multiple UIDocuments**

```
PanelSettings A (Sort Order: 0)  → 배경 UI
PanelSettings B (Sort Order: 10) → 게임 HUD
PanelSettings C (Sort Order: 20) → 팝업/오버레이
PanelSettings D (Sort Order: 30) → 디버그 UI
```

여러 UIDocument가 같은 PanelSettings를 참조할 수 있다. 이때 UIDocument의 Sort Order는 같은 패널 내 순서를 결정한다.

```csharp
// 런타임에서 PanelSettings 할당
var doc = GetComponent<UIDocument>();
doc.panelSettings = myPanelSettings; // ScriptableObject 참조
```

### 2.5 런타임 vs 에디터 차이

**런타임에서 사용 가능한 컨트롤 (UnityEngine.UIElements)**

| 카테고리 | 컨트롤 |
|---------|--------|
| 기본 | VisualElement, Label, Button, Toggle, TextField |
| 입력 | Slider, SliderInt, MinMaxSlider, IntegerField, FloatField |
| 선택 | DropdownField, RadioButton, RadioButtonGroup |
| 레이아웃 | ScrollView, Foldout, GroupBox |
| 목록 | ListView, TreeView, MultiColumnListView, MultiColumnTreeView |
| 진행/표시 | ProgressBar |
| Unity 6 신규 | ToggleButtonGroup, Tab, TabView |

**에디터 전용 컨트롤 (UnityEditor.UIElements)**

```
PropertyField, InspectorElement, ObjectField, EnumField,
EnumFlagsField, MaskField, LayerField, LayerMaskField,
TagField, ColorField, CurveField, GradientField,
BoundsField, RectField, Vector2Field, Vector3Field,
Vector4Field, BoundsIntField, RectIntField, Vector2IntField,
Vector3IntField, Toolbar, ToolbarMenu, ToolbarButton,
ToolbarSearchField, Pane, SplitView, TwoPaneSplitView ...
```

**런타임 설정 체크리스트**

```
[ ] Scene에 UIDocument GameObject 추가
[ ] UIDocument에 PanelSettings 할당
[ ] UIDocument에 UXML Asset 할당
[ ] Input System: UI Toolkit용 EventSystem 사용
    (GameObject > UI > Event System → UI Toolkit 전용 선택)
[ ] UXML에서 editor 네임스페이스 요소 제거
[ ] USS에서 editor 전용 속성 제거
```

**Awake vs OnEnable 타이밍**

```csharp
// 잘못된 방법 - Awake에서는 UXML이 아직 로드되지 않을 수 있음
private void Awake()
{
    var root = GetComponent<UIDocument>().rootVisualElement; // null 가능
    var btn = root.Q<Button>("my-btn"); // NullReferenceException 위험
}

// 올바른 방법 - OnEnable에서 접근
private void OnEnable()
{
    var root = GetComponent<UIDocument>().rootVisualElement; // 항상 유효
    var btn = root.Q<Button>("my-btn"); // 안전
}
```

UIDocument의 실행 순서(Execution Order)는 -100으로 설정되어 있어 일반 MonoBehaviour의 OnEnable보다 먼저 실행된다.

### 2.6 Unity 6 신규 기능

**신규 컨트롤**

```csharp
// ToggleButtonGroup — 단일/복수 선택 버튼 그룹
var tbg = root.Q<ToggleButtonGroup>("options");
tbg.value = new ToggleButtonGroupState(0b0101, 4); // 0, 2번 선택

// TabView / Tab — 탭 내비게이션
var tabView = root.Q<TabView>("main-tabs");
tabView.selectedTabIndex = 0;
```

UXML:
```xml
<ui:ToggleButtonGroup name="options">
    <ui:Button text="A"/>
    <ui:Button text="B"/>
    <ui:Button text="C"/>
</ui:ToggleButtonGroup>

<ui:TabView name="main-tabs">
    <ui:Tab label="Stats">
        <ui:Label text="Statistics content"/>
    </ui:Tab>
    <ui:Tab label="Inventory">
        <ui:Label text="Inventory content"/>
    </ui:Tab>
</ui:TabView>
```

**UxmlElement / UxmlAttribute (Unity 6)**

기존 UxmlFactory + UxmlTraits 패턴을 대체하는 간소화된 커스텀 컨트롤 등록:

```csharp
// Unity 6 방식 (신규)
[UxmlElement]
public partial class HealthBar : VisualElement
{
    [UxmlAttribute]
    public float maxHealth { get; set; } = 100f;

    [UxmlAttribute]
    public float currentHealth { get; set; } = 100f;

    public HealthBar()
    {
        // 생성자에서 초기화
        RegisterCallback<AttachToPanelEvent>(OnAttach);
    }

    private void OnAttach(AttachToPanelEvent evt)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // USS 클래스로 상태 표현
        RemoveFromClassList("health-low");
        RemoveFromClassList("health-critical");

        float ratio = currentHealth / maxHealth;
        if (ratio < 0.3f) AddToClassList("health-critical");
        else if (ratio < 0.5f) AddToClassList("health-low");
    }
}
```

UXML에서 사용:
```xml
<HealthBar max-health="100" current-health="75" />
```

**런타임 데이터 바인딩 (Unity 6)**

```csharp
// C#에서 데이터 바인딩 설정
var label = root.Q<Label>("hp-label");
label.SetBinding("text", new DataBinding
{
    dataSourcePath = new PropertyPath(nameof(PlayerData.HealthText)),
    bindingMode = BindingMode.ToTarget
});
```

**아이콘 지원 (Unity 6)**

Button, ListView, TreeView 등에 아이콘 직접 지원:
```xml
<ui:Button text="Settings" icon="/Icons/settings_icon.png" />
```

---

## 3. 베스트 프랙티스

### DO (권장)

- **OnEnable에서 rootVisualElement 접근**: UXML은 OnEnable 시점에 로드됨
- **쿼리 결과 캐싱**: `Q<T>()` 결과를 필드에 저장, Update에서 반복 쿼리 금지
- **USS 클래스로 상태 전환**: `AddToClassList`/`RemoveFromClassList`로 시각적 상태 변경
- **OnDisable에서 이벤트 구독 해제**: `clicked -=`, `UnregisterCallback` 수행
- **Scale With Screen Size**: PanelSettings에서 화면 크기 대응 스케일링 사용
- **여러 PanelSettings로 레이어 분리**: HUD/팝업/디버그 등 Sort Order로 Z순서 관리

### DON'T (금지)

- **Awake에서 rootVisualElement 접근 금지**: null 또는 미완성 상태일 수 있음
- **Update에서 Q<T>() 반복 호출 금지**: 매 프레임 쿼리는 성능 낭비
- **동일 요소에 직접 style 프로퍼티와 USS 혼용 금지**: C# 스타일이 USS를 덮어씀
- **편집기 전용 네임스페이스(UnityEditor.UIElements) 런타임 사용 금지**
- **VisualElement에서 MonoBehaviour 상속 시도 금지**: VisualElement는 독립 클래스 트리
- **generateVisualContent 콜백 내부에서 VisualElement 속성 변경 금지**: 무한 루프 위험

### CONSIDER (상황별)

- **여러 UIDocument를 같은 PanelSettings에 연결**: 복잡한 UI를 파일별로 분리할 때 같은 패널 공유 가능
- **GeometryChangedEvent 활용**: 요소 크기 확정 후 실행해야 하는 로직에 사용
- **UsageHints 설정**: 자주 이동/변형되는 요소에 `UsageHints.DynamicTransform` 설정으로 성능 향상
- **MarkDirtyRepaint()**: generateVisualContent로 커스텀 그리기를 한 요소를 강제 재그리기할 때

---

## 4. UGUI 대비 매핑표

| UGUI 개념 | UI Toolkit 대응 | 비고 |
|-----------|----------------|------|
| Canvas | UIDocument + PanelSettings | PanelSettings이 Canvas Scaler 역할 포함 |
| Canvas Scaler | PanelSettings > Scale Mode | Scale With Screen Size 대응 |
| Canvas Sort Order | PanelSettings > Sort Order | 값이 클수록 위에 렌더 |
| RectTransform | VisualElement (style 속성) | position: absolute 로 수동 배치 가능 |
| GameObject (UI용) | VisualElement | MonoBehaviour 없음 |
| Image | VisualElement + background-image | Image 컨트롤도 별도 존재 |
| RawImage | VisualElement + background-image | 동일 |
| Button | Button | `clicked` 이벤트 또는 ClickEvent |
| Text (Legacy) | Label | `text` 속성 동일 |
| TextMeshProUGUI | Label | `style.fontSize` 등으로 스타일 |
| Toggle | Toggle | `value` 프로퍼티로 상태 확인 |
| Slider | Slider / SliderInt | `value`, `lowValue`, `highValue` |
| InputField | TextField | `value`, `RegisterValueChangedCallback` |
| Dropdown | DropdownField | `choices` List<string> |
| ScrollRect | ScrollView | `contentContainer` 에 자식 추가 |
| Vertical LayoutGroup | flex-direction: column (기본) | USS로 자동 |
| Horizontal LayoutGroup | flex-direction: row | USS로 자동 |
| Grid LayoutGroup | 없음 (Flexbox wrap으로 근사) | Grid는 미지원 |
| ContentSizeFitter | 자동 (기본 동작) | 내용에 맞게 자동 크기 조정 |
| LayoutElement | flex-grow, flex-shrink, flex-basis | USS 속성으로 대응 |
| EventSystem | UI Toolkit 전용 EventSystem | 다른 컴포넌트, 같이 쓰면 충돌 |
| GraphicRaycaster | Panel (내장) | 별도 컴포넌트 불필요 |
| CanvasGroup | opacity + :disabled 상태 | `style.opacity`, `SetEnabled()` |
| FindChild | root.Q("name") | UQuery로 대체 |
| GetComponentInChildren | root.Q<T>() | 타입 기반 쿼리 |
| SetSiblingIndex | PlaceBehind / PlaceInFront | 순서 제어 메서드 |
| SetAsLastSibling | BringToFront() | - |
| SetAsFirstSibling | SendToBack() | - |
| gameObject.SetActive(true) | style.display = DisplayStyle.Flex | 레이아웃 공간도 함께 제어 |
| gameObject.SetActive(false) | style.display = DisplayStyle.None | 레이아웃에서 완전 제거 |
| - | style.visibility = Visibility.Hidden | 공간은 유지하고 숨김 |

---

## 5. 예제 코드

### 기본 사용법

**씬 설정 및 초기화 패턴**

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    // UI 요소 캐시
    private Button _playButton;
    private Button _settingsButton;
    private Button _quitButton;
    private Label _versionLabel;

    private void OnEnable()
    {
        // UXML은 OnEnable 시점에 로드됨 — 항상 여기서 쿼리
        var root = _document.rootVisualElement;

        _playButton     = root.Q<Button>("play-btn");
        _settingsButton = root.Q<Button>("settings-btn");
        _quitButton     = root.Q<Button>("quit-btn");
        _versionLabel   = root.Q<Label>("version-label");

        // 이벤트 등록
        _playButton.clicked     += OnPlayClicked;
        _settingsButton.clicked += OnSettingsClicked;
        _quitButton.clicked     += OnQuitClicked;

        // 초기 데이터 설정
        _versionLabel.text = Application.version;
    }

    private void OnDisable()
    {
        // 반드시 구독 해제
        if (_playButton != null)     _playButton.clicked     -= OnPlayClicked;
        if (_settingsButton != null) _settingsButton.clicked -= OnSettingsClicked;
        if (_quitButton != null)     _quitButton.clicked     -= OnQuitClicked;
    }

    private void OnPlayClicked()     { /* 씬 전환 */ }
    private void OnSettingsClicked() { /* 설정 패널 열기 */ }
    private void OnQuitClicked()     { Application.Quit(); }
}
```

**동적 요소 생성 패턴**

```csharp
private void PopulateItemList(List<ItemData> items)
{
    var container = _document.rootVisualElement.Q<VisualElement>("item-container");
    container.Clear(); // 기존 자식 제거

    foreach (var item in items)
    {
        // C#으로 직접 생성
        var itemRow = new VisualElement();
        itemRow.AddToClassList("item-row");

        var nameLabel = new Label(item.name);
        nameLabel.AddToClassList("item-name");

        var countLabel = new Label($"x{item.count}");
        countLabel.AddToClassList("item-count");

        itemRow.Add(nameLabel);
        itemRow.Add(countLabel);
        container.Add(itemRow);
    }
}
```

**UXML 동적 인스턴스화**

```csharp
[SerializeField] private VisualTreeAsset _itemRowTemplate;

private VisualElement CreateItemRow(ItemData data)
{
    // UXML 템플릿에서 인스턴스 생성
    var row = _itemRowTemplate.Instantiate();

    row.Q<Label>("item-name").text  = data.name;
    row.Q<Label>("item-count").text = $"x{data.count}";

    return row;
}
```

### 고급 패턴

**USS 클래스로 상태 전환 (전환 애니메이션 포함)**

```csharp
// USS 파일:
// .health-bar { transition: width 0.3s ease; }
// .health-bar--low { background-color: #ff8800; }
// .health-bar--critical { background-color: #ff0000; }

public class HealthBarController : MonoBehaviour
{
    private VisualElement _fillBar;

    private void OnEnable()
    {
        _fillBar = GetComponent<UIDocument>()
            .rootVisualElement
            .Q<VisualElement>("health-fill");
    }

    public void SetHealth(float current, float max)
    {
        float ratio = current / max;

        // 너비를 퍼센트로 설정 — USS transition이 자동 애니메이션
        _fillBar.style.width = Length.Percent(ratio * 100f);

        // USS 클래스로 색상 상태 전환
        _fillBar.EnableInClassList("health-bar--low",      ratio < 0.5f);
        _fillBar.EnableInClassList("health-bar--critical", ratio < 0.3f);
    }
}
```

**커스텀 VisualElement (Unity 6 방식)**

```csharp
[UxmlElement]
public partial class CircularProgress : VisualElement
{
    [UxmlAttribute]
    public float progress { get; set; } = 0f; // 0~1

    [UxmlAttribute]
    public Color fillColor { get; set; } = Color.green;

    public CircularProgress()
    {
        generateVisualContent += DrawCircle;
    }

    private void DrawCircle(MeshGenerationContext mgc)
    {
        var p = mgc.painter2D;
        var center = new Vector2(contentRect.width * 0.5f,
                                 contentRect.height * 0.5f);
        float radius = Mathf.Min(center.x, center.y) - 4f;

        // 배경 원
        p.strokeColor = Color.gray;
        p.lineWidth = 6f;
        p.BeginPath();
        p.Arc(center, radius, 0f, 360f);
        p.Stroke();

        // 진행 호
        p.strokeColor = fillColor;
        p.BeginPath();
        p.Arc(center, radius, -90f, -90f + (progress * 360f));
        p.Stroke();
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        MarkDirtyRepaint(); // 강제 재그리기
    }
}
```

**VisualElement 가시성 제어**

```csharp
// 레이아웃 공간도 제거 (UGUI SetActive(false) 대응)
element.style.display = DisplayStyle.None;
element.style.display = DisplayStyle.Flex; // 다시 표시

// 레이아웃 공간 유지하며 숨김 (UGUI CanvasGroup alpha=0 대응)
element.style.visibility = Visibility.Hidden;
element.style.visibility = Visibility.Visible;

// opacity (투명도, 0~1)
element.style.opacity = 0f;
element.style.opacity = 1f;
```

**ListView 기본 패턴 (가상화 포함)**

```csharp
private void SetupListView(List<string> items)
{
    var listView = _root.Q<ListView>("item-list");

    listView.itemsSource   = items;
    listView.fixedItemHeight = 50f;    // 가상화에 필요
    listView.makeItem = () =>
    {
        var label = new Label();
        label.AddToClassList("list-item");
        return label;
    };
    listView.bindItem = (element, index) =>
    {
        ((Label)element).text = items[index];
    };

    listView.Rebuild(); // 데이터 변경 후 갱신
}
```

---

## 6. UI_Study 적용 계획

### 학습 우선순위

1. **기초 UXML + USS + UIDocument 설정** (예제 01)
   - PanelSettings 설정, UXML 로드, Q<T>() 쿼리 패턴 확립

2. **Flexbox 레이아웃 마스터** (예제 02)
   - flex-direction, justify-content, align-items 조합
   - UGUI LayoutGroup 대응 패턴 실습

3. **USS 클래스 기반 상태 전환** (예제 03)
   - AddToClassList/RemoveFromClassList + USS transition
   - UGUI Animator 대체 패턴

4. **커스텀 VisualElement (Unity 6)** (예제 04)
   - UxmlElement/UxmlAttribute 기반 컴포넌트 제작
   - generateVisualContent로 커스텀 그래픽

5. **ListView + 가상화** (예제 05)
   - 대량 아이템 목록 (인벤토리, 로그)
   - makeItem/bindItem 패턴

### 기지 경영 게임 UI 활용 계획

| 게임 UI | UI Toolkit 구현 전략 |
|---------|---------------------|
| 자원 HUD | Label + 업데이트 패턴, USS transition |
| 건물 선택 패널 | TabView 또는 ToggleButtonGroup |
| 인벤토리 그리드 | ListView (가상화) 또는 ScrollView |
| 데미지 숫자 | 절대 위치 Label + USS 애니메이션 |
| 다이얼로그 박스 | UXML 템플릿 인스턴스화 |
| 팝업 오버레이 | 별도 UIDocument (높은 Sort Order) |

---

## 7. 참고 자료

- [Unity 6 Runtime UI 시작 가이드](https://docs.unity3d.com/6000.1/Documentation/Manual/UIE-get-started-with-runtime-ui.html)
- [PanelSettings 속성 레퍼런스](https://docs.unity3d.com/Manual/UIE-Runtime-Panel-Settings.html)
- [Visual Tree 소개](https://docs.unity3d.com/Manual/UIE-VisualTree.html)
- [UGUI → UI Toolkit 마이그레이션 가이드](https://docs.unity3d.com/Manual/UIE-Transitioning-From-UGUI.html)
- [패널 이벤트 레퍼런스](https://docs.unity3d.com/Manual/UIE-Panel-Events.html)
- [GeometryChangedEvent API](https://docs.unity3d.com/ScriptReference/UIElements.GeometryChangedEvent.html)
- [Yoga 레이아웃 엔진](https://docs.unity3d.com/6000.1/Documentation/Manual/UIE-LayoutEngine.html)
- [UI Toolkit 런타임 퍼포먼스 고려사항](https://docs.unity3d.com/Manual/UIE-performance-consideration-runtime.html)
- [커스텀 컨트롤 생성](https://docs.unity3d.com/6000.1/Documentation/Manual/UIE-create-custom-controls.html)
- [Unity 6 UI Toolkit 업데이트 블로그 (unity.com)](https://unity.com/blog/unity-6-ui-toolkit-updates)
- [QuizU UI Toolkit 샘플 프로젝트](https://assetstore.unity.com/packages/essentials/tutorial-projects/quizu-a-ui-toolkit-sample-268492)
- [UIToolkitUnityRoyaleRuntimeDemo (GitHub)](https://github.com/Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo)
- [UI Toolkit First Steps (loglog.games)](https://loglog.games/blog/unity-ui-toolkit-first-steps/)
- [UI Toolkit vs UGUI 2025 가이드](https://www.angry-shark-studio.com/blog/unity-ui-toolkit-vs-ugui-2025-guide/)

---

## 8. 미해결 질문

1. **Unity 6에서 런타임 데이터 바인딩의 성능 오버헤드는?** — 단순 Update 루프 vs 바인딩 시스템 비교 필요
2. **ToggleButtonGroup의 value 타입 ToggleButtonGroupState 직렬화 방법?** — 저장/불러오기 패턴 미확인
3. **여러 UIDocument가 같은 PanelSettings를 공유할 때 이벤트 처리 격리는?** — 터치/마우스 이벤트 소비 동작 검증 필요
4. **generateVisualContent와 DOTween 조합 시 MarkDirtyRepaint 타이밍?** — 매 프레임 vs DOTween onUpdate 패턴 검토 필요
5. **World Space UI Toolkit으로 3D 게임 오브젝트 위에 HP바 구현 가능성?** — RenderMode: WorldSpace + Collider 설정 조사 필요
6. **UI Toolkit + UGUI 혼용 시 입력 처리 우선순위?** — 기존 UGUI EventSystem과의 충돌 패턴 문서 필요
