# UI Toolkit + UGUI 하이브리드 전략

- **작성일**: 2026-03-29
- **대상**: UI Toolkit(스크린 UI) + UGUI(월드 공간 UI)를 한 프로젝트에서 공존시키는 기술 가이드
- **전제**: Unity 6.x, New Input System, UniTask, DOTween

---

## 1. 공존 아키텍처

UI Toolkit과 UGUI는 완전히 독립된 렌더링 파이프라인을 사용하므로 한 씬에서 공존할 수 있다. UI Toolkit은 UIDocument 컴포넌트를 통해 PanelSettings에 바인딩되고, UGUI는 Canvas 컴포넌트로 렌더링된다.

### 씬 구조

```
GameScene
├── [UIDocument] HUD_UIDocument          -- UI Toolkit, PanelSettings (SO:0)
├── [UIDocument] Screen_UIDocument       -- UI Toolkit, PanelSettings (SO:10)
├── [UIDocument] Modal_UIDocument        -- UI Toolkit, PanelSettings (SO:20)
├── [UIDocument] Toast_UIDocument        -- UI Toolkit, PanelSettings (SO:30)
│
├── [Canvas] WorldSpaceCanvas            -- UGUI, World Space, Sort Order 무관
│   ├── HPBarPool
│   ├── NameTagPool
│   └── DamageNumberPool
│
├── [EventSystem]                        -- 단일 EventSystem
│   └── InputSystemUIInputModule         -- New Input System 기반
│
└── GameUIBootstrapper                   -- MVP 수동 조립
```

### 핵심 원칙

1. **스크린 UI는 모두 UI Toolkit**: HUD, 메뉴, 다이얼로그, 설정, 리스트 등.
2. **월드 공간 UI는 모두 UGUI**: HP바, 이름표, 데미지 넘버 등 3D 월드에 부착되는 UI.
3. **하나의 EventSystem**: 씬에 EventSystem은 반드시 하나만 존재. InputSystemUIInputModule 사용.
4. **PanelSettings 공유**: 동일 Sort Order 레이어 내 UIDocument들은 하나의 PanelSettings를 공유 가능. 다른 레이어는 별도 PanelSettings.

---

## 2. 렌더링 순서 관리

UI Toolkit과 UGUI의 렌더링 순서는 각각 독립적으로 관리된다. 혼용 시 두 시스템 간 겹침 순서를 명확히 정의해야 한다.

### 레이어 체계

| 레이어 | 시스템 | Sort Order | 용도 |
|--------|--------|------------|------|
| World UI | UGUI Canvas (World Space) | N/A (카메라 depth 기준) | HP바, 건물 이름표, 데미지 넘버 |
| HUD | UI Toolkit UIDocument | 0 | 자원바, 미니맵 프레임, 턴 표시 |
| Screens | UI Toolkit UIDocument | 10 | 건설 메뉴, 유닛 목록, 기술 트리 |
| Modals | UI Toolkit UIDocument | 20 | 확인/취소 다이얼로그, 상세 팝업 |
| Toast | UI Toolkit UIDocument | 30 | 알림 토스트 |
| Overlay | UI Toolkit UIDocument | 40 | 로딩, 씬 전환 페이드 |

### PanelSettings 설정

```
PanelSettings_HUD.asset
  Scale Mode: Scale With Screen Size
  Reference Resolution: 1920 x 1080
  Sort Order: 0

PanelSettings_Screen.asset
  Scale Mode: Scale With Screen Size
  Reference Resolution: 1920 x 1080
  Sort Order: 10

PanelSettings_Modal.asset
  Scale Mode: Scale With Screen Size
  Reference Resolution: 1920 x 1080
  Sort Order: 20

PanelSettings_Toast.asset
  Scale Mode: Scale With Screen Size
  Reference Resolution: 1920 x 1080
  Sort Order: 30
```

### UGUI Canvas 설정 (월드 공간)

```
WorldSpaceCanvas
  Render Mode: World Space
  Event Camera: Main Camera
  Sorting Layer: Default
  Order in Layer: 0
```

월드 공간 Canvas는 카메라와의 거리로 렌더링 순서가 결정되므로 Sort Order는 스크린 UI에 영향을 주지 않는다. 스크린 오버레이 UGUI Canvas를 사용하지 않는 한 충돌하지 않는다.

---

## 3. 이벤트 시스템 공존

### 입력 라우팅

UI Toolkit과 UGUI는 입력을 서로 다른 경로로 수신한다.

| 시스템 | 입력 경로 | 이벤트 타입 |
|--------|----------|------------|
| UI Toolkit | PanelEventHandler (자동 추가) | PointerDownEvent, ClickEvent, NavigationMoveEvent 등 |
| UGUI | EventSystem + InputSystemUIInputModule | PointerEventData, IPointerClickHandler 등 |

두 시스템 모두 InputSystemUIInputModule을 통해 New Input System의 입력을 수신한다.

### 입력 충돌 방지

UI Toolkit의 VisualElement가 입력을 소비하면 UGUI로 전파되지 않는다. 반대로 UGUI Canvas가 Screen Space Overlay로 설정되면 UI Toolkit의 입력을 가릴 수 있다.

**규칙**:

1. UGUI Canvas는 **World Space만** 사용한다. Screen Space Overlay/Camera UGUI Canvas를 UI Toolkit과 혼용하지 않는다.
2. UI Toolkit에서 입력을 차단할 영역은 `pickingMode = PickingMode.Position`으로 설정한다.
3. 모달 다이얼로그의 백드롭은 UI Toolkit에서 화면 전체를 덮는 VisualElement로 구현하여 하위 UI와 월드 공간 클릭을 차단한다.

```csharp
// 모달 백드롭 -- 클릭 차단
var backdrop = new VisualElement();
backdrop.style.position = Position.Absolute;
backdrop.style.left = backdrop.style.right = backdrop.style.top = backdrop.style.bottom = 0;
backdrop.style.backgroundColor = new Color(0, 0, 0, 0.5f);
backdrop.pickingMode = PickingMode.Position;
backdrop.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
```

### 게임패드/키보드 내비게이션

UI Toolkit은 `NavigationMoveEvent`, `NavigationSubmitEvent`로 게임패드 입력을 수신한다. UGUI는 `Selectable.navigation`으로 처리한다. 두 시스템의 포커스가 동시에 활성화되면 입력이 양쪽에 전파될 수 있다.

**해결**: 스크린 UI가 활성화될 때 월드 공간 Canvas의 `GraphicRaycaster`를 비활성화한다.

```csharp
// 스크린 UI 활성화 시
worldCanvas.GetComponent<GraphicRaycaster>().enabled = false;

// 스크린 UI 비활성화 시
worldCanvas.GetComponent<GraphicRaycaster>().enabled = true;
```

---

## 4. 공유 데이터 모델

UI Toolkit View와 UGUI View가 동일한 Model 클래스를 참조한다. Model은 순수 C# 클래스로 UI 프레임워크에 의존하지 않는다.

### 데이터 흐름

```
                 ┌─ UI Toolkit View (Label, ListView)
Model (C# event) ─┤
                 └─ UGUI View (TextMeshProUGUI, Image)
                        │
                 Presenter (Pure C#, IDisposable)
```

### 구현 예시: 자원 시스템

```csharp
// === Model (순수 C#, UI 프레임워크 무관) ===
public class ResourceModel
{
    public int Gold { get; private set; }
    public event Action<int> GoldChanged;

    public void AddGold(int amount)
    {
        Gold += amount;
        GoldChanged?.Invoke(Gold);
    }
}

// === UI Toolkit View (스크린 HUD) ===
public class ResourceHudView : MonoBehaviour
{
    UIDocument _doc;
    Label _goldLabel;

    void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        _goldLabel = _doc.rootVisualElement.Q<Label>("gold-value");
    }

    public void SetGold(int value) => _goldLabel.text = value.ToString("N0");
}

// === UGUI View (월드 공간 -- 건물 위 자원 표시) ===
public class BuildingResourceView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _goldText;

    public void SetGold(int value) => _goldText.text = value.ToString("N0");
}

// === Presenter (양쪽 View를 동일 Model에 연결) ===
public class ResourcePresenter : IDisposable
{
    readonly ResourceModel _model;
    readonly ResourceHudView _hudView;       // UI Toolkit
    readonly BuildingResourceView _worldView; // UGUI

    public ResourcePresenter(ResourceModel model,
                             ResourceHudView hudView,
                             BuildingResourceView worldView)
    {
        _model = model;
        _hudView = hudView;
        _worldView = worldView;
    }

    public void Initialize()
    {
        _model.GoldChanged += OnGoldChanged;
        OnGoldChanged(_model.Gold); // 초기값 표시
    }

    void OnGoldChanged(int gold)
    {
        _hudView.SetGold(gold);
        _worldView.SetGold(gold);
    }

    public void Dispose()
    {
        _model.GoldChanged -= OnGoldChanged;
    }
}
```

### 핵심 규칙

1. **Model에 UI 프레임워크 참조 금지**: `using UnityEngine.UIElements` 또는 `using UnityEngine.UI`가 Model에 존재하면 안 된다.
2. **View는 표시 메서드만 노출**: `SetGold(int)`, `SetHP(float)` 등. 비즈니스 로직 없음.
3. **Presenter가 양쪽 View를 연결**: 하나의 Presenter가 UI Toolkit View와 UGUI View를 모두 참조할 수 있다. 또는 별도 Presenter로 분리해도 무방하다.

---

## 5. 마이그레이션 패턴

기존 UGUI 화면을 UI Toolkit으로 한 번에 하나씩 전환하는 절차.

### 단계별 전환 프로세스

**1단계: UXML/USS 작성**

기존 UGUI 프리팹의 레이아웃을 UXML로 재현한다. RectTransform 앵커/피벗을 Flexbox로 변환한다.

| UGUI 개념 | UI Toolkit 대응 |
|-----------|-----------------|
| RectTransform anchors | flex-grow, align-self, position |
| VerticalLayoutGroup | flex-direction: column |
| HorizontalLayoutGroup | flex-direction: row |
| GridLayoutGroup | flex-wrap: wrap + 고정 width |
| ContentSizeFitter | flex-shrink: 0, align-self: auto |
| Image (Filled) | 미지원 -- Painter2D 또는 radial-gradient 대체 |
| TextMeshProUGUI | Label (USS -unity-font) |
| Button | Button (USS :hover/:active/:disabled) |
| ScrollRect | ScrollView |
| Toggle | Toggle |
| Slider | Slider, SliderInt |

**2단계: View 클래스 전환**

```csharp
// Before (UGUI)
public class SettingsView : MonoBehaviour
{
    [SerializeField] Slider _volumeSlider;
    [SerializeField] Toggle _musicToggle;

    void Awake()
    {
        _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }
}

// After (UI Toolkit)
public class SettingsView : MonoBehaviour
{
    Slider _volumeSlider;
    Toggle _musicToggle;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _volumeSlider = root.Q<Slider>("volume-slider");
        _musicToggle = root.Q<Toggle>("music-toggle");

        _volumeSlider.RegisterValueChangedCallback(HandleVolumeChanged);
    }

    void OnDisable()
    {
        _volumeSlider.UnregisterValueChangedCallback(HandleVolumeChanged);
    }
}
```

**3단계: Presenter 수정 (최소)**

Model은 변경 없음. Presenter에서 View의 타입과 메서드 시그니처만 갱신한다. 비즈니스 로직은 동일하다.

**4단계: UGUI 프리팹 비활성화**

전환 완료 후 기존 UGUI 프리팹과 Canvas를 비활성화(삭제가 아님)한다. 1~2 스프린트간 병행 테스트 후 삭제한다.

### 전환 우선순위

| 순위 | 화면 | 이유 |
|------|------|------|
| 1 | 설정 화면 | 가장 독립적. 다른 시스템 의존 없음 |
| 2 | 저장/불러오기 | ListView 이점 명확. 독립적 |
| 3 | 인벤토리/유닛 목록 | ListView 가상화 성능 이점 최대 |
| 4 | 건설 메뉴 | flex-wrap + :hover 생산성 이점 |
| 5 | HUD | 항상 표시되므로 성능 회귀 시 즉시 체감. 마지막에 전환 |
| N/A | 월드 공간 UI | 전환 불가. UGUI 유지 |

---

## 6. 주의사항

### 입력 관련

1. **Screen Space Overlay Canvas 금지**: UI Toolkit과 혼용 시 렌더링 순서와 입력 라우팅 모두 문제 발생. UGUI는 World Space만 사용.
2. **EventSystem 중복 금지**: 씬에 EventSystem이 2개 이상이면 UGUI 입력이 무시될 수 있다. DontDestroyOnLoad 오브젝트에 EventSystem이 있는지 확인.
3. **포커스 상태 관리**: UI Toolkit 모달이 열리면 UGUI GraphicRaycaster를 비활성화하여 월드 클릭을 차단.

### 렌더링 관련

4. **Dynamic Atlas vs SpriteAtlas V2**: UI Toolkit은 PanelSettings에서 동적 아틀라스를 자동 생성한다. UGUI의 SpriteAtlas V2와 별개의 아틀라스 시스템이므로 텍스처를 공유하지 않는다. 동일 스프라이트를 양쪽에서 사용하면 메모리가 이중 소비된다.
5. **카메라 스택**: URP Camera Stack 사용 시 UI Toolkit은 Overlay Camera가 아닌 Base Camera 위에 렌더링된다. UGUI World Space Canvas의 Event Camera 설정 확인.

### 아키텍처 관련

6. **Bootstrapper에서 양쪽 View 참조**: Bootstrapper는 `[SerializeField]`로 UI Toolkit View와 UGUI View를 모두 참조한다. 둘 다 MonoBehaviour이므로 동일한 방식으로 조립 가능.
7. **씬 로드 시 UIDocument 초기화 타이밍**: UIDocument의 `rootVisualElement`는 `OnEnable` 이후에만 유효하다. `Awake`에서 접근하면 null. UGUI와 달리 이 타이밍을 반드시 준수해야 한다.
8. **USS 변수와 uPalette 공존 불가**: 같은 프로젝트에서 UI Toolkit은 USS 변수로, UGUI는 uPalette로 테마를 관리하면 색상/폰트 동기화가 필요하다. ScriptableObject에서 공통 색상 팔레트를 정의하고 양쪽에서 참조하는 방안을 권장한다.

### 성능 관련

9. **프로파일러 분리 확인**: UI Toolkit은 `UIR.DrawChain`으로, UGUI는 `Canvas.SendWillRenderCanvases`로 프로파일러에 표시된다. 양쪽의 CPU 비용을 독립적으로 모니터링.
10. **UI Toolkit 1프레임 지연**: UI Toolkit의 레이아웃 계산은 렌더링 직전에 수행된다. `style.display = Flex`로 표시한 직후 `resolvedStyle`에서 크기를 읽으면 이전 프레임 값이 반환될 수 있다. `schedule.Execute`로 1프레임 대기 후 읽는다.

---

## 7. 참고 문서

- [UI Toolkit vs UGUI 최종 판정 매트릭스](../research/uitoolkit-vs-ugui-decision-matrix.md)
- [UGUI vs UI Toolkit 비교 회고](../reviews/ugui-vs-uitoolkit-retrospective.md)
- [Project_Sun UI Toolkit 도입 결정](../reviews/project-sun-uitoolkit-decision.md)
- [UI Toolkit Simple MVP 패턴](./uitoolkit-simple-mvp.md)
- [UI Toolkit 성능 분석 및 한계](../research/uitoolkit-performance-and-limits.md)
