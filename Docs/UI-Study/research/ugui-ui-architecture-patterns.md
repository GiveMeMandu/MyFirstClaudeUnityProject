# Unity UGUI UI 아키텍처 패턴 리서치 리포트

- **작성일**: 2026-03-27
- **카테고리**: pattern
- **상태**: 조사완료

---

## 1. 요약

Unity UGUI 환경에서는 **MV(R)P (Model-View-Reactive Presenter)** 패턴이 VContainer + R3 조합과 가장 자연스럽게 통합되며 현재 커뮤니티에서 가장 폭넓게 채택된 접근법이다. MVVM은 UI Toolkit의 런타임 바인딩 시스템과 맞춰 설계되었기 때문에 UGUI에서는 바인딩 레이어를 직접 구현해야 하는 오버헤드가 따른다. Flux/Redux 계열은 복잡한 전역 상태 관리가 필요한 멀티플레이어/협동 게임에는 유효하지만 일반 UI에는 과도한 보일러플레이트를 유발한다.

---

## 2. 상세 분석

### 2.1 MVP (Model-View-Presenter) in Unity UGUI

#### MonoBehaviour 매핑

```
Model         → Pure C# class (게임 데이터 + 비즈니스 로직)
View          → MonoBehaviour (Canvas 컴포넌트, 버튼, 텍스트 등 순수 렌더링)
Presenter     → Pure C# class (VContainer EntryPoint, Model ↔ View 중개)
```

Unity의 Canvas/UI 컴포넌트가 View 역할을 자연스럽게 담당한다. Presenter는 VContainer의 `IStartable` / `ITickable` 같은 EntryPoint 인터페이스로 등록해 MonoBehaviour 없이 동작하는 Pure C# 클래스로 유지할 수 있다.

#### 기본 통신 흐름

```
[사용자 입력] → View (버튼 클릭 이벤트) → Presenter → Model (상태 변경)
                                                     ↓
              View (UI 업데이트) ← Presenter ← Model (OnChanged 이벤트 / Observable)
```

#### Presenter의 역할

1. **포맷팅**: Model 데이터를 UI 표시용 형식으로 변환
2. **입력 처리**: View의 버튼/슬라이더 이벤트 수신 → Model 메서드 호출
3. **View 업데이트**: Model 변경 이벤트 수신 → View 메서드 호출

#### 장점

- Unity Learn에서 공식 지원하는 패턴
- View가 완전히 덤(dumb)하므로 View 테스트 불필요
- Presenter를 Interface로 추상화하면 단위 테스트에서 Mock View 사용 가능
- VContainer EntryPoint로 Presenter를 순수 C# 유지 가능
- 팀원 간 역할 분리 명확 (디자이너: View, 프로그래머: Presenter/Model)

#### 단점

- 화면마다 View 인터페이스 정의 필요 (보일러플레이트 증가)
- 복잡한 UI에서 Presenter가 비대해지는 경향 (God Presenter)
- Model 변경 이벤트 관리가 많아지면 구독 해제 관리 복잡

---

### 2.2 MV(R)P — Reactive Presenter Pattern (권장 변형)

일본 Unity 커뮤니티(Qiita)에서 정착된 패턴으로, MVP에 Reactive를 결합한 변형이다. UniRx의 후계자인 **R3**와 함께 사용하면 이벤트 기반 업데이트를 Observable 스트림으로 대체하여 코드량을 줄이고 구독 관리를 자동화할 수 있다.

#### VContainer + R3 통합 구조

```csharp
// LifetimeScope 등록 예시
public class HudLifetimeScope : LifetimeScope
{
    [SerializeField] private HudView _view;

    protected override void Configure(IContainerBuilder builder)
    {
        // Model: Scoped 수명 (이 스코프 내에서 단일 인스턴스)
        builder.Register<HudModel>(Lifetime.Scoped);

        // View: MonoBehaviour 인스턴스를 직접 등록
        builder.RegisterComponent(_view);

        // Presenter: EntryPoint로 등록 (IStartable 자동 호출)
        builder.RegisterEntryPoint<HudPresenter>();
    }
}
```

```csharp
// Model: R3 ReactiveProperty로 상태 노출
public class HudModel
{
    public ReactiveProperty<int> CurrentGold { get; } = new(0);
    public ReactiveProperty<int> CurrentTurn  { get; } = new(1);

    public void AddGold(int amount) => CurrentGold.Value += amount;
    public void NextTurn()          => CurrentTurn.Value++;
}
```

```csharp
// Presenter: Pure C# EntryPoint (IStartable 구현)
public class HudPresenter : IStartable, IDisposable
{
    private readonly HudModel _model;
    private readonly HudView  _view;
    private readonly CompositeDisposable _disposables = new();

    public HudPresenter(HudModel model, HudView view)
    {
        _model = model;
        _view  = view;
    }

    // ★ VContainer 피드백: Construct()가 아닌 Start()에서 Subscribe
    public void Start()
    {
        _model.CurrentGold
            .Subscribe(gold => _view.SetGoldText(gold))
            .AddTo(_disposables);

        _model.CurrentTurn
            .Subscribe(turn => _view.SetTurnText(turn))
            .AddTo(_disposables);

        _view.EndTurnButtonClicked
            .Subscribe(_ => _model.NextTurn())
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
```

```csharp
// View: MonoBehaviour — 순수 UI 처리만 담당
public class HudView : MonoBehaviour
{
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private Button   _endTurnButton;

    // Presenter가 Subscribe할 수 있는 Observable 이벤트 노출
    public Observable<Unit> EndTurnButtonClicked =>
        _endTurnButton.OnClickAsObservable();

    public void SetGoldText(int gold) => _goldText.text = $"Gold: {gold}";
    public void SetTurnText(int turn) => _turnText.text = $"Turn: {turn}";
}
```

> **VContainer 주의사항**: `Construct()` (= VContainer의 생성자 주입 직후)에서 R3 `Subscribe()`를 호출하면 Awake 전에 실행되어 데드락이 발생할 수 있다. 반드시 `IStartable.Start()`에서 구독을 시작해야 한다.

---

### 2.3 MVVM (Model-View-ViewModel) in Unity UGUI

#### WPF 스타일 바인딩 없이 구현하는 방법

Unity는 WPF와 달리 네이티브 데이터 바인딩 시스템이 없다. UGUI에서 MVVM을 구현하려면 세 가지 접근법이 있다:

**방법 A: R3 ReactiveProperty + 수동 Subscribe (경량)**

```csharp
// ViewModel
public class InventoryViewModel : IDisposable
{
    public ReactiveProperty<int> ItemCount { get; } = new(0);
    public ReactiveProperty<bool> IsEmpty  { get; }

    public InventoryViewModel()
    {
        // 파생 Observable: ItemCount에서 자동 계산
        IsEmpty = ItemCount.Select(c => c == 0).ToReactiveProperty();
    }

    public void Dispose()
    {
        ItemCount.Dispose();
        IsEmpty.Dispose();
    }
}
```

이 방식은 실질적으로 MV(R)P와 동일하며, ViewModel이 View를 모르는 것이 MVP Presenter와의 차이다. Unity UGUI에서는 View가 직접 ViewModel을 구독하는 구조가 된다.

**방법 B: UnityMvvmToolkit 라이브러리 사용**

- `CanvasView<TBindingContext>` 상속으로 자동 바인딩
- `IProperty<T>` 인터페이스로 ViewModel 프로퍼티 정의
- UGUI와 UI Toolkit 모두 지원
- IL2CPP에서 Unity 2022+ 요구 (소스 코드 생성 필요)

**방법 C: Unity UI Toolkit 런타임 바인딩 (UGUI 아님)**

Unity 6에서 UI Toolkit에 공식 런타임 바인딩이 추가됨. `DataBinding` 클래스로 XAML-스타일 바인딩 가능. **단, UGUI에는 적용 불가**.

#### UGUI에서 MVVM vs MVP 비교

| 기준 | MVP (MV(R)P) | MVVM |
|------|-------------|------|
| View의 ViewModel 의존성 | View는 Presenter 몰라도 됨 | View가 ViewModel을 직접 구독 |
| 테스트 | Presenter 단독 테스트 쉬움 | ViewModel 단독 테스트 더 쉬움 (View 불필요) |
| 보일러플레이트 | View 인터페이스 필요 | 바인딩 설정 코드 필요 |
| UGUI 적합성 | 높음 (자연스러운 매핑) | 중간 (추가 바인딩 레이어 필요) |
| VContainer 통합 | 매우 자연스러움 | 가능하지만 추가 설계 필요 |

---

### 2.4 MVC in Unity

#### 실용성 평가

Unity에서 순수 MVC는 **View가 Model을 직접 관찰**해야 하는데, 이는 View에 관찰 로직이 들어가 관심사 분리가 약화된다. 대부분의 Unity MVC 구현은 실질적으로 MVP에 가깝거나, Controller가 View와 Model 사이를 중개하는 변형 MVC다.

**Unity의 MVC 구현 패턴:**

```
Model       → ScriptableObject 또는 Pure C# 데이터 컨테이너
View        → MonoBehaviour + Canvas 컴포넌트
Controller  → MonoBehaviour (View와 같은 GameObject에 붙는 경우가 많음)
```

#### MVC의 한계

- View가 Controller를 통해 간접적으로 Model에 접근해야 하지만, 현실에서는 View가 Model을 직접 읽는 단축 경로가 생김
- Controller가 씬마다 존재하면 SceneManager와 Controller 수명 관리가 복잡
- Unity Learn에서도 MVC보다 MVP를 더 권장

---

### 2.5 Flux/Redux 단방향 데이터 흐름

#### Unity 구현체: Unidux

GitHub `mattak/Unidux`는 Redux 아키텍처를 Unity에 이식한 라이브러리다.

```
Action 디스패치 → Reducer (순수 함수, 새 State 반환) → Store → UI 구독자 업데이트
```

#### 성능 문제

- `StateBase.Clone()`이 `BinaryFormatter + MemoryStream` 사용 → 느림
- `Equals()` 비교가 Reflection 기반 → 잦은 상태 변경 시 성능 저하
- 프로덕션 사용 시 커스텀 Clone/Equals 구현 필수

#### 언제 유용한가

- 전역 게임 상태(세이브 데이터, 멀티플레이어 동기화)에는 적합
- 복잡한 규칙 시스템이 있는 보드게임/턴제 전략 게임의 **게임 로직 레이어**에 적합
- 단, UI 레이어 자체에는 과도한 보일러플레이트를 유발

#### 평가: UI 레이어에서는 오버킬

턴제 전략 게임의 경우 게임 상태 관리에 Redux 스타일을 쓰고, UI 레이어는 MV(R)P로 구성하는 **하이브리드 접근**이 현실적이다.

---

### 2.6 패턴 간 종합 비교

| 기준 | MVC | MVP / MV(R)P | MVVM | Flux/Redux |
|------|-----|-------------|------|------------|
| UGUI 적합성 | 낮음 | 높음 | 중간 | 낮음 (UI 레이어) |
| VContainer 통합 | 가능 | 매우 자연스러움 | 가능 | 별도 Store 필요 |
| R3 통합 | 수동 | 자연스러움 | 자연스러움 | UniRx 의존 |
| 테스트 용이성 | 낮음 | 높음 | 매우 높음 | 높음 |
| 팀 확장성 | 낮음 | 높음 | 높음 | 매우 높음 |
| 학습 곡선 | 낮음 | 중간 | 중간~높음 | 높음 |
| 보일러플레이트 | 낮음 | 중간 | 중간 | 높음 |
| 프로덕션 사례 | 드뭄 | 많음 | 증가 중 | 드뭄 |

---

## 3. 베스트 프랙티스

### DO (권장)

- [ ] Presenter/ViewModel을 Pure C#으로 유지 (MonoBehaviour 아님)
- [ ] VContainer `RegisterEntryPoint<TPresenter>()`로 Presenter 등록
- [ ] `IStartable.Start()`에서 R3 Subscribe() 시작 (Construct/생성자에서 금지)
- [ ] `CompositeDisposable`로 구독 일괄 관리, `IDisposable` 구현으로 정리
- [ ] View에서는 Observable 이벤트만 노출, 상태 변경 로직 금지
- [ ] LifetimeScope 계층을 Panel/Popup 단위로 분리 (화면별 독립 스코프)
- [ ] Screen별 Presenter 하나, Screen 내 복잡한 Widget은 Sub-Presenter 분리

### DON'T (금지)

- [ ] Presenter에서 `FindObjectOfType<>()` 또는 `GameObject.Find()` 사용
- [ ] View에서 Model을 직접 참조 (의존 방향 역전)
- [ ] 생성자(Construct)에서 R3 Subscribe 호출 (VContainer 데드락)
- [ ] Presenter가 다른 Presenter를 직접 호출 (메시지 버스나 이벤트로 분리)
- [ ] 하나의 Presenter에 여러 화면 로직 집중 (God Presenter)

### CONSIDER (상황별)

- [ ] 팀이 소규모(1-2명)인 경우 패턴 엄격 적용보다 일관성이 우선
- [ ] 복잡한 화면 간 상태 공유가 필요하면 SharedModel 또는 MessageBroker 도입
- [ ] 팝업/다이얼로그는 별도 ChildLifetimeScope로 수명 관리
- [ ] 성능이 중요한 HUD(매 프레임 업데이트)는 ReactiveProperty 대신 직접 Tick 고려

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 6000.x (Unity 6) | UGUI 기준, UI Toolkit 아님 |
| VContainer | 1.x | EntryPoint, LifetimeScope 계층 지원 |
| R3 | 1.x | ReactiveProperty, AddTo(disposable) |
| UnityMvvmToolkit | 최신 | UGUI + UI Toolkit 지원, IL2CPP Unity 2022+ 필요 |
| Unidux | 레거시 | UniRx 의존, 프로덕션 사용 전 Clone/Equals 커스텀 필요 |

---

## 5. 예제 코드

### 5.1 HUD Panel — MV(R)P 기본 구조

```csharp
// ===== Model =====
public class HudModel
{
    public ReactiveProperty<int>  Gold    { get; } = new(0);
    public ReactiveProperty<int>  Turn    { get; } = new(1);
    public ReactiveProperty<bool> IsMyTurn { get; } = new(true);

    public void SpendGold(int amount) => Gold.Value -= amount;
    public void EarnGold(int amount)  => Gold.Value += amount;
    public void EndTurn()
    {
        Turn.Value++;
        IsMyTurn.Value = !IsMyTurn.Value;
    }
}
```

```csharp
// ===== View =====
public class HudView : MonoBehaviour
{
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private Button   _endTurnButton;
    [SerializeField] private GameObject _myTurnIndicator;

    // Presenter가 구독할 Observable 이벤트
    public Observable<Unit> OnEndTurnClicked =>
        _endTurnButton.OnClickAsObservable();

    // Presenter가 호출하는 업데이트 메서드
    public void SetGold(int value)       => _goldText.text = $"Gold: {value:N0}";
    public void SetTurn(int value)       => _turnText.text = $"Turn {value}";
    public void SetMyTurn(bool isMyTurn) => _myTurnIndicator.SetActive(isMyTurn);
}
```

```csharp
// ===== Presenter =====
public class HudPresenter : IStartable, IDisposable
{
    private readonly HudModel _model;
    private readonly HudView  _view;
    private readonly CompositeDisposable _disposables = new();

    // VContainer가 생성자 주입
    public HudPresenter(HudModel model, HudView view)
    {
        _model = model;
        _view  = view;
    }

    public void Start()
    {
        // Model → View 단방향 바인딩
        _model.Gold    .Subscribe(_view.SetGold)    .AddTo(_disposables);
        _model.Turn    .Subscribe(_view.SetTurn)    .AddTo(_disposables);
        _model.IsMyTurn.Subscribe(_view.SetMyTurn)  .AddTo(_disposables);

        // View → Model (사용자 입력)
        _view.OnEndTurnClicked
            .Subscribe(_ => _model.EndTurn())
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
```

```csharp
// ===== LifetimeScope =====
public class HudLifetimeScope : LifetimeScope
{
    [SerializeField] private HudView _hudView;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<HudModel>(Lifetime.Scoped);
        builder.RegisterComponent(_hudView);
        builder.RegisterEntryPoint<HudPresenter>();
    }
}
```

---

### 5.2 팝업 패널 — ChildLifetimeScope 패턴

```csharp
// 팝업은 ChildScope로 별도 수명 관리
public class ConfirmPopupLifetimeScope : LifetimeScope
{
    [SerializeField] private ConfirmPopupView _view;

    protected override void Configure(IContainerBuilder builder)
    {
        // 팝업 전용 Model (팝업 닫히면 자동 소멸)
        builder.Register<ConfirmPopupModel>(Lifetime.Scoped);
        builder.RegisterComponent(_view);
        builder.RegisterEntryPoint<ConfirmPopupPresenter>();
    }
}
```

---

### 5.3 공유 상태 — MessageBroker 패턴 (Presenter 간 통신)

```csharp
// Presenter 간 직접 참조 대신 메시지 버스 사용
public class TurnEndedMessage { public int NewTurn { get; init; } }

// HudPresenter에서 발행
MessageBroker.Default.Publish(new TurnEndedMessage { NewTurn = _model.Turn.Value });

// 다른 Presenter에서 수신
MessageBroker.Default
    .Receive<TurnEndedMessage>()
    .Subscribe(msg => HandleTurnEnd(msg.NewTurn))
    .AddTo(_disposables);
```

---

## 6. UI_Study 적용 계획

이 리서치를 기반으로 다음 예제를 UI_Study에서 구현할 수 있다:

1. **예제 01**: MV(R)P 기본 — HUD (Gold, Turn, HP 표시)
2. **예제 02**: ChildLifetimeScope 팝업 — 확인/취소 다이얼로그
3. **예제 03**: 공유 Model — 여러 UI 패널이 동일 Model 구독
4. **예제 04**: 패널 스택 관리자 — Dialog vs Panel 레이어 구조
5. **예제 05**: 인벤토리 UI — ReactiveCollection + 동적 리스트 렌더링
6. **예제 06**: Screen 전환 — LifetimeScope 계층 기반 화면 전환

학습 순서: 01 → 02 → 03 → 04 → 05 → 06 (복잡도 순)

---

## 7. 참고 자료

1. [Unity Learn - Build a modular codebase with MVC and MVP](https://learn.unity.com/course/design-patterns-unity-6/tutorial/build-a-modular-codebase-with-mvc-and-mvp-programming-patterns)
2. [Unity Learn - Model-View-ViewModel pattern](https://learn.unity.com/tutorial/model-view-viewmodel-pattern)
3. [GitHub - VContainer by hadashiA](https://github.com/hadashiA/VContainer)
4. [GitHub - R3 by Cysharp (neuecc)](https://github.com/Cysharp/R3)
5. [GitHub - Unity-VContainer-UniRx-MVP-Example (jinhosung96)](https://github.com/jinhosung96/Unity-VContainer-UniRx-MVP-Example)
6. [GitHub - UnityMvvmToolkit (LibraStack)](https://github.com/LibraStack/UnityMvvmToolkit)
7. [GitHub - Unity-MVP-with-Vcontainer (NorthTH)](https://github.com/NorthTH/Unity-MVP-with-Vcontainer)
8. [Qiita - MV(R)P パターンとは何なのか (toRisouP)](https://qiita.com/toRisouP/items/5365936fc14c7e7eabf9)
9. [Game Developer - A UI System Architecture and Workflow for Unity](https://www.gamedeveloper.com/programming/a-ui-system-architecture-and-workflow-for-unity)
10. [Game Developer - A critique of MVC/MVVM as a pattern for game development](https://www.gamedeveloper.com/design/a-critique-of-mvc-mvvm-as-a-pattern-for-game-development)
11. [GitHub - Unidux: Redux Architecture for Unity (mattak)](https://github.com/mattak/Unidux)
12. [Medium - MVP Design Patterns for Unity (OnurKiris)](https://medium.com/@onurkiris05/model-view-presenter-pattern-mvp-design-patterns-for-unity-4-60e17dd9e7c0)
13. [Medium - Flux Architecture in Games (Lev Perlman)](https://medium.com/front-end-weekly/flux-architecture-in-games-porting-the-web-design-pattern-to-game-development-f9d0959346ec)
14. [Unity Discussions - MVC/MVP/MVA/MVVM Patterns](https://discussions.unity.com/t/mvc-mvp-mva-mvvm-and-so-on-patterns/939221)
15. [Unity Blog - Ultimate Guide to Creating UI Interfaces](https://unity.com/blog/games/ultimate-guide-to-creating-ui-interfaces)

---

## 8. 미해결 질문

- [ ] R3 `BindTo()` 확장 메서드가 UGUI에서 직접 동작하는가, 아니면 수동 Subscribe가 필수인가?
- [ ] VContainer의 `MessageBroker` vs R3의 `Subject` — Presenter 간 통신에 어느 것이 더 적합한가?
- [ ] ReactiveCollection (ObservableList)을 사용한 인벤토리 동적 렌더링 최적화 방법
- [ ] 패널 스택 관리자(UIManager)를 VContainer로 구현할 때 ChildScope 생성/소멸 타이밍 제어 방법
- [ ] 턴제 전략 게임에서 전투 시스템 상태를 Flux-스타일로 관리하고 UI는 MV(R)P로 연결하는 구체적 패턴
