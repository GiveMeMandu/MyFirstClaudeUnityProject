# UI Toolkit 경량 아키텍처 — DI 없는 Simple MVP

- **작성일**: 2026-03-29
- **카테고리**: pattern
- **상태**: 조사완료

---

## 1. 요약

UI Toolkit의 경량 아키텍처는 VContainer/R3 없이 순수 C# 클래스 Presenter + MonoBehaviour View + C# 이벤트 Model 3계층으로 구성한다. View는 UIDocument를 보유한 MonoBehaviour로 UI 요소를 캐싱하고 C# event를 통해 입력을 외부에 노출하며, Presenter는 View와 Model을 생성자로 받는 순수 C# 클래스로 양방향 연결을 담당한다. 화면 내비게이션은 UnityScreenNavigator 없이 UIDocument의 Sort Order 레이어링과 `DisplayStyle.None/Flex` 토글로 구현하며, 10개 이하 화면 기준에서 Dictionary 기반 단순 ScreenManager로 충분하다. C# events는 팀 규모 1-2인, 화면 10개 미만 프로젝트에서 R3/VContainer 없이도 완전히 충분하고, 복잡도 임계점을 넘을 때만 R3/VContainer를 부분 도입한다.

---

## 2. 상세 분석

### 2.1 DI 없는 심플 MVP with UI Toolkit

MVP 3계층의 핵심 책임 분리:

| 계층 | 타입 | 책임 |
|------|------|------|
| View | MonoBehaviour | UIDocument 보유, UI 요소 캐싱, C# event 노출 |
| Presenter | Pure C# class | View event 구독 + Model 업데이트 + View 갱신 |
| Model | Pure C# class | 데이터 보유, C# event로 변경 알림 |

**Presenter 생성 위치 비교**

| 방식 | 장점 | 단점 |
|------|------|------|
| View.Start()에서 생성 | 간단, self-contained | View가 Model 참조를 알아야 함 |
| 루트 Bootstrapper | 관심사 분리 명확 | 별도 Bootstrapper 클래스 필요 |
| ScriptableObject 서비스 로케이터 | Inspector 연결 용이 | 전역 상태, 테스트 어려움 |

#### 완전 동작 예제: Resource Panel (Gold/Wood/Food + 지출 버튼)

**ResourceModel.cs — 순수 C# 모델**

```csharp
using System;

/// <summary>
/// 순수 C# 모델. MonoBehaviour를 상속하지 않는다.
/// C# event로 변경을 알리므로 R3 ReactiveProperty가 필요 없다.
/// </summary>
public class ResourceModel
{
    private int _gold;
    private int _wood;
    private int _food;

    public event Action<int> GoldChanged;
    public event Action<int> WoodChanged;
    public event Action<int> FoodChanged;
    public event Action<bool> SpendButtonInteractableChanged;

    public int Gold
    {
        get => _gold;
        private set
        {
            _gold = value;
            GoldChanged?.Invoke(value);
            SpendButtonInteractableChanged?.Invoke(CanSpend(10));
        }
    }

    public int Wood
    {
        get => _wood;
        private set { _wood = value; WoodChanged?.Invoke(value); }
    }

    public int Food
    {
        get => _food;
        private set { _food = value; FoodChanged?.Invoke(value); }
    }

    public ResourceModel(int gold = 100, int wood = 50, int food = 30)
    {
        // 이벤트 발행 없이 초기값 설정 — 필드 직접 세팅
        _gold = gold;
        _wood = wood;
        _food = food;
    }

    public bool CanSpend(int amount) => _gold >= amount;

    /// <summary>골드 10 지출. 부족하면 false 반환.</summary>
    public bool SpendGold(int amount)
    {
        if (!CanSpend(amount)) return false;
        Gold -= amount;
        return true;
    }

    public void AddResources(int gold, int wood, int food)
    {
        Gold += gold;
        Wood += wood;
        Food += food;
    }
}
```

**ResourcePanelView.cs — UIDocument를 보유한 MonoBehaviour View**

```csharp
using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// View 책임:
/// 1. UIDocument 보유 + OnEnable에서 요소 캐싱
/// 2. 사용자 입력을 C# event로 외부에 노출
/// 3. Presenter가 호출하는 display 메서드 제공
/// 4. OnDisable에서 모든 구독 해제
/// </summary>
public class ResourcePanelView : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    // === 사용자 입력 이벤트 (Presenter가 구독) ===
    public event Action OnSpendClicked;

    // === UI 요소 캐시 ===
    private Label _goldLabel;
    private Label _woodLabel;
    private Label _foodLabel;
    private Button _spendButton;

    private void OnEnable()
    {
        // UXML은 UIDocument의 OnEnable보다 늦게 로드될 수 있으므로
        // UIDocument ExecutionOrder(-100)가 먼저 실행된 후 이 MonoBehaviour가 실행됨
        var root = _document.rootVisualElement;

        _goldLabel   = root.Q<Label>("gold-label");
        _woodLabel   = root.Q<Label>("wood-label");
        _foodLabel   = root.Q<Label>("food-label");
        _spendButton = root.Q<Button>("spend-button");

        _spendButton.clicked += HandleSpendClicked;
    }

    private void OnDisable()
    {
        if (_spendButton != null)
            _spendButton.clicked -= HandleSpendClicked;
    }

    private void HandleSpendClicked() => OnSpendClicked?.Invoke();

    // === Presenter가 호출하는 display 메서드 ===
    public void SetGold(int amount)  => _goldLabel.text = $"Gold: {amount:N0}";
    public void SetWood(int amount)  => _woodLabel.text = $"Wood: {amount:N0}";
    public void SetFood(int amount)  => _foodLabel.text = $"Food: {amount:N0}";

    public void SetSpendButtonInteractable(bool enabled)
        => _spendButton.SetEnabled(enabled);

    /// <summary>초기 상태를 표시. Presenter가 Start()에서 호출한다.</summary>
    public void Initialize(int gold, int wood, int food)
    {
        SetGold(gold);
        SetWood(wood);
        SetFood(food);
        SetSpendButtonInteractable(gold >= 10);
    }
}
```

**ResourcePanelPresenter.cs — 순수 C# Presenter**

```csharp
using System;

/// <summary>
/// Presenter 책임:
/// 1. View event를 구독하여 Model 업데이트
/// 2. Model event를 구독하여 View 갱신
/// 3. 생성자로 View + Model을 받아 연결
/// 4. Dispose()로 모든 구독 해제
/// </summary>
public class ResourcePanelPresenter : IDisposable
{
    private readonly ResourcePanelView _view;
    private readonly ResourceModel _model;
    private bool _disposed;

    public ResourcePanelPresenter(ResourcePanelView view, ResourceModel model)
    {
        _view  = view;
        _model = model;

        // View → Model: 사용자 입력 처리
        _view.OnSpendClicked += HandleSpend;

        // Model → View: 데이터 변경 반영
        _model.GoldChanged                  += _view.SetGold;
        _model.WoodChanged                  += _view.SetWood;
        _model.FoodChanged                  += _view.SetFood;
        _model.SpendButtonInteractableChanged += _view.SetSpendButtonInteractable;
    }

    /// <summary>Presenter 시작 시 View 초기화. Bootstrapper가 호출한다.</summary>
    public void Initialize()
    {
        _view.Initialize(_model.Gold, _model.Wood, _model.Food);
    }

    private void HandleSpend()
    {
        bool success = _model.SpendGold(10);
        // 실패 처리는 Presenter에서 — View에 피드백 가능
        if (!success)
        {
            // 예: View에 "골드 부족" 애니메이션 트리거
            // _view.PlayInsufficientGoldFeedback();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _view.OnSpendClicked -= HandleSpend;

        _model.GoldChanged                    -= _view.SetGold;
        _model.WoodChanged                    -= _view.SetWood;
        _model.FoodChanged                    -= _view.SetFood;
        _model.SpendButtonInteractableChanged -= _view.SetSpendButtonInteractable;
    }
}
```

**ResourceBootstrapper.cs — 씬 루트 MonoBehaviour가 조합**

```csharp
using UnityEngine;

/// <summary>
/// Bootstrapper 패턴:
/// - 씬의 루트 GameObject에 부착
/// - View + Model + Presenter를 생성하고 연결
/// - VContainer 없이 수동 의존성 주입 역할
/// </summary>
public class ResourceBootstrapper : MonoBehaviour
{
    [SerializeField] private ResourcePanelView _resourceView;

    private ResourceModel _model;
    private ResourcePanelPresenter _presenter;

    private void Start()
    {
        _model     = new ResourceModel(gold: 100, wood: 50, food: 30);
        _presenter = new ResourcePanelPresenter(_resourceView, _model);
        _presenter.Initialize();
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
    }
}
```

**Resource.uxml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="resource-panel" class="resource-panel">
        <VisualElement name="resources" class="resource-row">
            <Label name="gold-label" text="Gold: 0" class="resource-label"/>
            <Label name="wood-label" text="Wood: 0" class="resource-label"/>
            <Label name="food-label" text="Food: 0" class="resource-label"/>
        </VisualElement>
        <Button name="spend-button" text="Spend 10 Gold" class="spend-button"/>
    </VisualElement>
</UXML>
```

---

### 2.2 View Registration Without DI

**DI 없이 View를 연결하는 세 가지 방법**

**방법 A: [SerializeField] 직접 참조 (소규모 권장)**

Inspector에서 View를 직접 Bootstrapper에 연결한다. 가장 단순하고 Unity 네이티브한 방법이다.

```csharp
// Bootstrapper에서
[SerializeField] private ResourcePanelView _resourceView; // Inspector 연결
[SerializeField] private HudView _hudView;
[SerializeField] private SettingsView _settingsView;

private void Start()
{
    var model = new GameModel();
    new ResourcePanelPresenter(_resourceView, model).Initialize();
    new HudPresenter(_hudView, model).Initialize();
    new SettingsPresenter(_settingsView, model).Initialize();
}
```

장점: 타입 안전, 리플렉션 없음, 참조가 Inspector에서 명확히 보임.
단점: View 수가 15개를 넘으면 Inspector가 지저분해짐.

**방법 B: 자기 참조 (View가 Presenter를 직접 생성)**

View 자신이 Start()에서 Presenter를 생성한다. Model은 ScriptableObject나 싱글턴으로 공유한다.

```csharp
public class SettingsView : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private GameSettingsSO _settings; // ScriptableObject 모델

    private SettingsPresenter _presenter;

    private void OnEnable() { /* 요소 캐싱 */ }

    private void Start()
    {
        // View가 Presenter를 직접 생성 — 소규모 화면에 적합
        _presenter = new SettingsPresenter(this, _settings);
        _presenter.Initialize();
    }

    private void OnDestroy() => _presenter?.Dispose();
}
```

장점: 화면 단위 self-contained, Bootstrapper 불필요.
단점: View가 Model 타입을 알아야 함(약한 결합 위반).

**방법 C: ScriptableObject 서비스 로케이터**

GameServices ScriptableObject가 공유 Model을 보유한다.

```csharp
[CreateAssetMenu(menuName = "Game/Services")]
public class GameServicesSO : ScriptableObject
{
    public ResourceModel Resources { get; private set; }
    public PlayerModel   Player   { get; private set; }

    private void OnEnable()
    {
        Resources = new ResourceModel();
        Player    = new PlayerModel();
    }
}

// View에서
public class ResourcePanelView : MonoBehaviour
{
    [SerializeField] private GameServicesSO _services;

    private void Start()
    {
        new ResourcePanelPresenter(this, _services.Resources).Initialize();
    }
}
```

장점: Inspector 연결로 서비스 공유, 씬 간 데이터 유지 가능.
단점: 전역 상태, 단위 테스트 어려움.

**View 수에 따른 방법 선택 기준**

| View 수 | 권장 방법 |
|---------|----------|
| 1-5개   | 방법 A (Bootstrapper SerializeField) |
| 6-15개  | 방법 B (View 자기 생성) 또는 방법 A |
| 15개+   | 방법 C (ScriptableObject 서비스 로케이터) 또는 VContainer 도입 |

---

### 2.3 Screen Navigation Without UnityScreenNavigator

**UIDocument 레이어링 전략**

여러 UIDocument를 Sort Order로 쌓아 레이어를 만든다. Unity 공식 문서는 "Multiple UIDocuments with sort order" 패턴을 권장한다.

```
PanelSettings: HUD     (Sort Order: 0)  → 항상 표시 HUD
PanelSettings: Screen  (Sort Order: 10) → 메인 화면들
PanelSettings: Popup   (Sort Order: 20) → 다이얼로그/팝업
PanelSettings: Toast   (Sort Order: 30) → 토스트 알림
```

각 레이어는 독립적인 PanelSettings를 가지므로 이벤트 소비 격리가 된다.

**단순 ScreenManager 구현**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Push/Pop 스택 기반 화면 관리자.
/// UnityScreenNavigator 없이 10개 이하 화면에 충분하다.
/// </summary>
public class ScreenManager : MonoBehaviour
{
    [Serializable]
    private struct ScreenEntry
    {
        public string     name;
        public UIDocument document;
    }

    [SerializeField] private ScreenEntry[] _screens;

    private readonly Dictionary<string, UIDocument> _screenMap = new();
    private readonly Stack<string>                  _history   = new();
    private string                                  _current;

    private void Awake()
    {
        foreach (var entry in _screens)
        {
            _screenMap[entry.name] = entry.document;
            // 시작 시 모두 숨김
            SetVisible(entry.document, false);
        }
    }

    /// <summary>화면 전환 (히스토리에 현재 화면 push)</summary>
    public void GoTo(string screenName)
    {
        if (!_screenMap.TryGetValue(screenName, out var next))
        {
            Debug.LogError($"ScreenManager: '{screenName}' 화면 없음");
            return;
        }

        // 현재 화면 숨기고 스택에 push
        if (_current != null)
        {
            SetVisible(_screenMap[_current], false);
            _history.Push(_current);
        }

        _current = screenName;
        SetVisible(next, true);
    }

    /// <summary>이전 화면으로 돌아가기</summary>
    public void GoBack()
    {
        if (_history.Count == 0) return;

        // 현재 화면 숨김
        if (_current != null)
            SetVisible(_screenMap[_current], false);

        _current = _history.Pop();
        SetVisible(_screenMap[_current], true);
    }

    /// <summary>히스토리 없이 직접 교체</summary>
    public void Replace(string screenName)
    {
        if (_current != null)
            SetVisible(_screenMap[_current], false);

        _history.Clear();
        _current = screenName;
        SetVisible(_screenMap[_current], true);
    }

    private static void SetVisible(UIDocument doc, bool visible)
    {
        // rootVisualElement.style.display로 표시/숨김
        // DisplayStyle.None은 레이아웃 공간도 제거 (SetActive(false)와 동일)
        doc.rootVisualElement.style.display =
            visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
```

**화면 전환 예제 (MainMenu → Settings → Back)**

```csharp
public class MainMenuView : MonoBehaviour
{
    [SerializeField] private UIDocument    _document;
    [SerializeField] private ScreenManager _screens;

    private Button _settingsButton;
    private Button _playButton;

    // ✅ named method로 저장해야 해제 가능 (익명 람다 금지)
    private void OnSettingsClicked() => _screens.GoTo("Settings");
    private void OnPlayClicked()     => _screens.Replace("GameHUD");

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _settingsButton = root.Q<Button>("settings-btn");
        _playButton     = root.Q<Button>("play-btn");

        _settingsButton.clicked += OnSettingsClicked;
        _playButton.clicked     += OnPlayClicked;
    }

    private void OnDisable()
    {
        if (_settingsButton != null) _settingsButton.clicked -= OnSettingsClicked;
        if (_playButton != null)     _playButton.clicked     -= OnPlayClicked;
    }
}

public class SettingsView : MonoBehaviour
{
    [SerializeField] private UIDocument    _document;
    [SerializeField] private ScreenManager _screens;

    private Button _backButton;

    // ✅ named method
    private void OnBackClicked() => _screens.GoBack();

    private void OnEnable()
    {
        var root  = _document.rootVisualElement;
        _backButton = root.Q<Button>("back-btn");
        _backButton.clicked += OnBackClicked;
    }

    private void OnDisable()
    {
        if (_backButton != null) _backButton.clicked -= OnBackClicked;
    }
}
```

---

### 2.4 C# Event 기반 상태 관리

**기본 C# Event Model**

R3 ReactiveProperty를 대체하는 10줄 ObservableValue<T> 래퍼:

```csharp
using System;

/// <summary>
/// 10줄짜리 단순 Observable 값 래퍼.
/// R3 ReactiveProperty의 경량 대안이다.
/// </summary>
public class ObservableValue<T>
{
    private T _value;

    public event Action<T> Changed;

    public T Value
    {
        get => _value;
        set
        {
            // 값이 같으면 이벤트 발행 안 함 (DistinctUntilChanged 효과)
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = value;
            Changed?.Invoke(value);
        }
    }

    public ObservableValue(T initial = default) => _value = initial;

    /// <summary>같은 값이어도 강제로 이벤트 발행</summary>
    public void ForceNotify() => Changed?.Invoke(_value);
}

// 사용 예
public class GameModel
{
    public ObservableValue<int>    Score    = new(0);
    public ObservableValue<float>  Health   = new(1f);
    public ObservableValue<string> Phase    = new("Lobby");
    public ObservableValue<bool>   IsPaused = new(false);
}

// Presenter에서 구독
_model.Score.Changed   += _view.SetScore;
_model.Health.Changed  += _view.SetHealth;
_model.Phase.Changed   += OnPhaseChanged;

// 정리
_model.Score.Changed   -= _view.SetScore;
_model.Health.Changed  -= _view.SetHealth;
_model.Phase.Changed   -= OnPhaseChanged;
```

**다중 리스너 패턴**

```csharp
// 같은 Model 변경에 여러 View가 반응 — C# event가 자연스럽게 지원
_model.GoldChanged += _hudView.SetGold;
_model.GoldChanged += _resourceBarView.SetGold;
_model.GoldChanged += _buildingPanelView.UpdateAffordability;

// 이벤트 발행 하나로 세 View가 동시에 업데이트됨
_model.Gold = 150;
```

**C# event로 충분한 경우 vs. 더 필요한 경우**

| 상황 | C# event | R3 | 이유 |
|------|----------|----|------|
| 단순 클릭 처리 | 충분 | 불필요 | 1:1 연결, 변환 없음 |
| 단일 값 변경 알림 | 충분 | 불필요 | ObservableValue<T>로 대체 |
| 다중 리스너 | 충분 | 불필요 | C# event 다중 구독 지원 |
| 버튼 빠른 클릭 방지 | 불가 | Throttle | C# event에 쓰로틀 없음 |
| 검색 입력 디바운스 | 불가 | Debounce | 타이머 로직 수동 구현 복잡 |
| 여러 소스 결합 | 복잡 | CombineLatest/Merge | 수동 구현 가능하나 번거로움 |
| 중복 업데이트 방지 | ObservableValue | DistinctUntilChanged | ObservableValue로 대체 가능 |
| 스트림 체이닝 | 불가 | 연산자 체인 | LINQ 수준 처리가 필요하면 R3 |

---

### 2.5 DI/Reactive 도입 판단 기준표

| 조건 | C# events 충분 | R3 고려 | VContainer 고려 |
|------|---------------|---------|-----------------|
| 화면 수 < 10 | 충분 | | |
| 화면 수 10-30 | 충분 | 선택적 | |
| 화면 수 30+ | | 권장 | 권장 |
| 공유 상태 소스 < 5 | 충분 | | |
| 공유 상태 소스 5-15 | 가능 | 고려 | |
| 공유 상태 소스 15+ | | 권장 | 권장 |
| 복잡한 스트림 연산 필요 | | 권장 | |
| (디바운스/쓰로틀/머지) | | | |
| 팀 규모 1-2인 | 충분 | | |
| 팀 규모 3-5인 | 가능 | 선택적 | 선택적 |
| 팀 규모 5인+ | | 권장 | 권장 |
| 단위 테스트 필수 | 가능 | | VContainer |
| 크로스 씬 상태 공유 | ScriptableObject | | VContainer |

**현 프로젝트 (소규모 기지 경영 게임) 판단**

화면 수 예상: 약 8-15개, 팀: 1-2인, 공유 상태: 5개 미만(자원/건물/연구/전투/설정)

결론: **C# events + ObservableValue<T>로 시작, 특정 화면(검색/빠른 클릭)에서만 R3 부분 도입.**

---

### 2.6 UGUI MV(R)P vs UI Toolkit 심플 MVP 비교

같은 기능(골드 표시 + 지출 버튼) 구현 코드 비교:

**UGUI + VContainer + R3 방식**

```csharp
// === Model (R3 기반) ===
public class ResourceModel
{
    public ReactiveProperty<int> Gold { get; } = new(100);
    public bool SpendGold(int amt) { if (Gold.Value < amt) return false; Gold.Value -= amt; return true; }
}

// === View (MonoBehaviour + [SerializeField]) ===
public class ResourceView : MonoBehaviour
{
    [SerializeField] Text       _goldText;    // UGUI Text
    [SerializeField] Button     _spendButton; // UGUI Button

    public IObservable<Unit> OnSpendClicked => _spendButton.OnClickAsObservable();
    public void SetGold(int v) => _goldText.text = $"Gold: {v}";
}

// === Presenter (VContainer EntryPoint) ===
public class ResourcePresenter : IInitializable, IDisposable
{
    readonly ResourceModel _model;
    readonly ResourceView  _view;
    DisposableBag _bag;

    [Inject]
    public ResourcePresenter(ResourceModel model, ResourceView view)
    { _model = model; _view = view; }

    public void Initialize()
    {
        _model.Gold.Subscribe(_view.SetGold).AddTo(ref _bag);
        _view.OnSpendClicked.Subscribe(_ => _model.SpendGold(10)).AddTo(ref _bag);
    }
    public void Dispose() => _bag.Dispose();
}

// === LifetimeScope (VContainer 설정) ===
public class GameScope : LifetimeScope
{
    [SerializeField] ResourceView _view;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ResourceModel>(Lifetime.Singleton);
        builder.RegisterComponent(_view);
        builder.RegisterEntryPoint<ResourcePresenter>();
    }
}
```

의존 파일 수: 4개 (Model + View + Presenter + Scope)
패키지 의존성: VContainer, R3

**UI Toolkit + C# Events 방식**

```csharp
// === Model (Pure C#) ===
public class ResourceModel
{
    private int _gold = 100;
    public event Action<int> GoldChanged;
    public int Gold { get => _gold; private set { _gold = value; GoldChanged?.Invoke(value); } }
    public bool SpendGold(int amt) { if (_gold < amt) return false; Gold -= amt; return true; }
}

// === View (MonoBehaviour + UIDocument) ===
public class ResourceView : MonoBehaviour
{
    [SerializeField] UIDocument _doc;
    private Label  _goldLabel;
    private Button _spendBtn;
    public event Action OnSpendClicked;

    // ✅ named method — 익명 람다 해제 불가 방지
    private void HandleSpendClicked() => OnSpendClicked?.Invoke();

    private void OnEnable()
    {
        var root  = _doc.rootVisualElement;
        _goldLabel = root.Q<Label>("gold-label");
        _spendBtn  = root.Q<Button>("spend-btn");
        _spendBtn.clicked += HandleSpendClicked;
    }
    private void OnDisable() { if (_spendBtn != null) _spendBtn.clicked -= HandleSpendClicked; }
    public void SetGold(int v) => _goldLabel.text = $"Gold: {v}";
}

// === Presenter (Pure C#) ===
public class ResourcePresenter : IDisposable
{
    readonly ResourceModel _model;
    readonly ResourceView  _view;

    // ✅ named method로 저장 — Dispose에서 정확히 해제 가능
    private void HandleSpend() => _model.SpendGold(10);

    public ResourcePresenter(ResourceModel m, ResourceView v) { _model = m; _view = v;
        _view.OnSpendClicked += HandleSpend;
        _model.GoldChanged   += _view.SetGold; }
    public void Initialize() => _view.SetGold(_model.Gold);
    public void Dispose()    { _view.OnSpendClicked -= HandleSpend; _model.GoldChanged -= _view.SetGold; }
}

// === Bootstrapper ===
public class Bootstrapper : MonoBehaviour
{
    [SerializeField] ResourceView _view;
    ResourcePresenter _p;
    void Start()  { _p = new ResourcePresenter(new ResourceModel(), _view); _p.Initialize(); }
    void OnDestroy() => _p?.Dispose();
}
```

의존 파일 수: 4개 (Model + View + Presenter + Bootstrapper)
패키지 의존성: 없음 (Unity 기본 패키지만 사용)

**비교 요약**

| 기준 | UGUI + VContainer + R3 | UI Toolkit + C# events |
|------|------------------------|------------------------|
| 코드 줄 수 (기능 동일) | ~80줄 | ~60줄 |
| 외부 패키지 수 | 3개 (VContainer, R3, UniTask) | 0개 |
| 학습 곡선 | 높음 (DI + Rx 개념 필요) | 낮음 (C# 기초만) |
| 반응형 스트림 연산 | 풍부 (Throttle/Debounce/Merge) | 없음 (직접 구현) |
| 단위 테스트 용이성 | 높음 (인터페이스 주입) | 중간 (직접 생성 가능) |
| 디버깅 난이도 | 높음 (Rx 스트림 추적) | 낮음 (콜스택 명확) |
| 팀 온보딩 비용 | 높음 | 낮음 |
| 확장성 | 높음 | 중간 (임계점 이후 부채 발생) |

---

## 3. 베스트 프랙티스

### DO (권장)

- **OnEnable에서 UI 요소 캐싱**: `_document.rootVisualElement.Q<T>()` 결과를 필드에 저장
- **OnDisable에서 이벤트 해제**: `clicked -=`, `UnregisterCallback` 수행하여 메모리 누수 방지
- **Presenter를 IDisposable로 구현**: 씬 종료 시 Bootstrapper가 Dispose() 호출
- **View는 표시 로직만**: 검증/계산 없이 model → label.text, SetEnabled()만 담당
- **ObservableValue<T>로 단순 반응형 상태**: 10줄로 R3 없이 값 변경 알림
- **화면 전환에 DisplayStyle.None**: `style.display = DisplayStyle.None`은 레이아웃 공간도 제거
- **Bootstrapper 패턴**: 한 곳에서 View+Model+Presenter 조합, Inspector 의존성 명확

### DON'T (금지)

- **Awake에서 rootVisualElement 접근 금지**: UIDocument 로드 타이밍보다 빠를 수 있음
- **Update에서 Q<T>() 반복 호출 금지**: 매 프레임 쿼리는 성능 낭비
- **View에서 비즈니스 로직 실행 금지**: Model 없이 View 내부에서 계산하면 MVP 붕괴
- **Model이 View를 직접 참조 금지**: Model은 View를 몰라야 함 (단방향 의존)
- **익명 람다로 이벤트 등록 금지**: `clicked += () => Foo()` 는 `-=`로 해제 불가능
- **화면 15개 이상에서 수동 SerializeField 의존 금지**: 임계점 이후 ScriptableObject 또는 VContainer 도입

### CONSIDER (상황별)

- **ScriptableObject 모델**: 씬 간 데이터 유지가 필요한 설정/진행 상태에 유용
- **View.Start()에서 Presenter 생성**: 화면이 완전히 독립적이고 5개 이하일 때 Bootstrapper 불필요
- **R3 부분 도입**: 검색 입력 디바운스, 빠른 클릭 쓰로틀이 필요한 특정 화면에만 적용
- **ScreenManager Stack**: 10개 미만 화면에서 UnityScreenNavigator 없이 push/pop 내비게이션 충분

---

## 4. 예제 코드

### 완전 동작 예제: 자원 패널 UXML + USS

```xml
<!-- ResourcePanel.uxml -->
<?xml version="1.0" encoding="utf-8"?>
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="resource-panel" class="resource-panel">
        <VisualElement class="resource-row">
            <VisualElement class="resource-item">
                <Label class="resource-icon" text="G"/>
                <Label name="gold-label"  class="resource-value" text="0"/>
            </VisualElement>
            <VisualElement class="resource-item">
                <Label class="resource-icon" text="W"/>
                <Label name="wood-label"  class="resource-value" text="0"/>
            </VisualElement>
            <VisualElement class="resource-item">
                <Label class="resource-icon" text="F"/>
                <Label name="food-label"  class="resource-value" text="0"/>
            </VisualElement>
        </VisualElement>
        <Button name="spend-button" text="Spend 10 Gold" class="spend-button"/>
    </VisualElement>
</UXML>
```

```css
/* ResourcePanel.uss */
.resource-panel {
    padding: 12px;
    background-color: rgba(0, 0, 0, 0.7);
    border-radius: 8px;
    flex-direction: column;
}

.resource-row {
    flex-direction: row;
    justify-content: space-around;
    margin-bottom: 8px;
}

.resource-item {
    flex-direction: row;
    align-items: center;
}

.resource-icon {
    width: 20px;
    height: 20px;
    font-size: 12px;
    color: #ffd700;
    margin-right: 4px;
}

.resource-value {
    font-size: 18px;
    color: #ffffff;
    /* 값 변경 시 색상 전환 애니메이션 */
    transition-property: color;
    transition-duration: 0.3s;
}

.spend-button {
    margin-top: 8px;
    padding: 8px 16px;
    background-color: #4caf50;
    border-radius: 4px;
    color: white;
    font-size: 14px;
}

.spend-button:disabled {
    background-color: #666666;
    color: #999999;
}

.spend-button:hover:enabled {
    background-color: #66bb6a;
}
```

---

## 5. UI_Study 적용 계획

| 단계 | 주제 | 패턴 | 파일 위치 |
|------|------|------|----------|
| 예제 01 | 기본 MVP 연결 | Bootstrapper + View + Presenter | 10-UI-Toolkit/01-SimpleMVP |
| 예제 02 | ObservableValue<T> | 경량 반응형 상태 | 10-UI-Toolkit/02-ObservableValue |
| 예제 03 | ScreenManager | Push/Pop 내비게이션 | 10-UI-Toolkit/03-ScreenNav |
| 예제 04 | ScriptableObject 모델 | 씬 간 상태 공유 | 10-UI-Toolkit/04-SOModel |
| 예제 05 | 기준표 구현 | C# event vs R3 분기 | 10-UI-Toolkit/05-DecisionDemo |

**기지 경영 게임 적용 계획**

| 게임 화면 | MVP 구성 | 특이사항 |
|----------|----------|----------|
| 자원 HUD | ResourceModel + HudView + HudPresenter | 항상 표시, DisplayStyle 미사용 |
| 건물 선택 | BuildingModel + BuildMenuView + BuildPresenter | Tab 컨트롤 활용 |
| 설정 화면 | SettingsSO + SettingsView + SettingsPresenter | ScriptableObject 모델 |
| 확인 다이얼로그 | 상위 Presenter에서 UniTask로 await | UniTaskCompletionSource |
| 자원 지출 확인 | SpendConfirmView + UniTask dialog | doc 027 패턴 연동 |

---

## 6. 참고 자료

- [Unity 6 Runtime UI 시작 가이드](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-get-started-with-runtime-ui.html)
- [UIDocument 컴포넌트 레퍼런스](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-render-runtime-ui.html)
- [UI Toolkit 이벤트 처리](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Events-Handling.html)
- [LogLog Games — UI Toolkit First Steps](https://loglog.games/blog/unity-ui-toolkit-first-steps/)
- [UI Toolkit vs UGUI 2025 가이드](https://www.angry-shark-studio.com/blog/unity-ui-toolkit-vs-ugui-2025-guide/)
- [UIToolkitUnityRoyaleRuntimeDemo (Unity 공식 샘플)](https://github.com/Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo)
- [QuizU UI Toolkit 샘플 프로젝트](https://assetstore.unity.com/packages/essentials/tutorial-projects/quizu-a-ui-toolkit-sample-268492)

---

## 7. 미해결 질문

1. **익명 람다 이벤트 해제 패턴**: `clicked += () => OnSpendClicked?.Invoke()`을 OnDisable에서 안전하게 해제하려면 람다를 필드에 저장해야 하는데, 권장 패턴 확인 필요
2. **ScreenManager + CSS Transition 연동**: `DisplayStyle.None` 전환 전에 fade-out 트랜지션을 기다리는 패턴 (`TransitionEndEvent` 활용) 구현 방법
3. **여러 PanelSettings 이벤트 소비 격리**: 팝업이 열린 상태에서 하위 레이어 HUD 버튼 클릭 차단 방법 확인 필요
4. **ScriptableObject 모델의 씬 언로드 정리**: PlayMode 종료 시 이벤트 구독이 남아 있는 문제 대응 패턴
5. **ObservableValue<T>의 EqualityComparer 성능**: struct 값 타입에서 boxing 발생 여부 확인 필요 (IEquatable<T> 구현 강제 고려)
