# R3 UI 데이터 바인딩 패턴 리서치

- **작성일**: 2026-03-28
- **카테고리**: library / practice
- **상태**: 조사완료
- **대상 버전**: R3 v1.3.x (2025.02 기준), R3.Unity 패키지

---

## 1. 요약

R3는 UniRx의 3세대 후속으로 UI 데이터 바인딩을 위한 강력한 패턴 집합을 제공한다. 핵심 차이점은 세 가지다: (1) 에러 시 구독이 종료되지 않는 `OnErrorResume`, (2) async/await와의 네이티브 통합(`SubscribeAwait`), (3) `Throttle`→`Debounce` 등 LINQ 표준과 일치하는 명명 규칙. 이 문서는 Unity UI(UGUI) 개발에서 실전 적용 가능한 10개 패턴을 코드 예시와 함께 정리한다.

---

## 2. 사전 조건: 네임스페이스 및 설치

```csharp
using R3;                   // 핵심 API
using R3.Triggers;          // ObservableTriggers (UI 이벤트 변환)
using ObservableCollections; // ObservableList, ISynchronizedView
```

패키지 설치:
```
// UPM Git URL (R3.Unity)
https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity

// ObservableCollections (컬렉션 바인딩 시 필요)
https://github.com/Cysharp/ObservableCollections.git?path=src/ObservableCollections/Assets/ObservableCollections
```

UniRx에서 마이그레이션 시 주요 네임스페이스 변경:
- `using UniRx;` → `using R3;`
- `using UniRx.Triggers;` → `using R3.Triggers;`
- `IObservable<T>` → `Observable<T>` (타입 선언)

---

## 3. 패턴 상세

### 3.1 ReactiveProperty → UI 바인딩

`ReactiveProperty<T>`는 값 변경 시 자동으로 구독자에게 알린다. BehaviorSubject와 동일하나 중복 값 제거(DistinctUntilChanged)가 기본 적용된다.

**TMP Text 바인딩:**
```csharp
// Model
public class CounterModel : IDisposable
{
    public ReactiveProperty<int> Count { get; } = new(0);
    public void Increment() => Count.Value++;
    public void Dispose() => Count.Dispose();
}

// Presenter (Initialize()에서 구독)
_model.Count
    .Subscribe(count => _view.CountText.text = $"Count: {count}")
    .AddTo(_disposables);

// R3 내장 확장 — SubscribeToText (Observable<string> 전용)
_model.Count
    .Select(c => c.ToString())
    .SubscribeToText(_view.CountText)   // TMP는 지원 안 함, UGUI Text 전용
    .AddTo(_disposables);

// TMP는 직접 Subscribe 사용:
_model.Count
    .Subscribe(c => _tmpText.text = c.ToString())
    .AddTo(_disposables);
```

**Image.fillAmount 바인딩 (HP바):**
```csharp
// StatModel
public ReactiveProperty<int> CurrentHp { get; } = new(100);
public ReactiveProperty<int> MaxHp { get; } = new(100);

// Presenter — CombineLatest로 정규화
Observable.CombineLatest(
        _model.CurrentHp,
        _model.MaxHp,
        (current, max) => max > 0 ? (float)current / max : 0f)
    .Subscribe(normalized =>
    {
        _view.HpBar.fillAmount = normalized;
        // 잔량별 색상 변경
        _view.HpBar.color = normalized switch
        {
            <= 0.2f => Color.red,
            <= 0.5f => Color.yellow,
            _ => Color.green
        };
    })
    .AddTo(_disposables);
```

주의: `Image.fillAmount`가 동작하려면 반드시 Type=Filled + Source Image가 할당되어 있어야 한다.

**Slider 바인딩:**
```csharp
// Model → Slider (단방향, 값 변화 반영)
_model.Volume
    .Subscribe(v => _view.VolumeSlider.value = v)
    .AddTo(_disposables);

// Slider → Model (UI 입력 → 모델 반영)
_view.VolumeSlider
    .OnValueChangedAsObservable()     // 구독 시 현재값 즉시 emit
    .Subscribe(v => _model.Volume.Value = v)
    .AddTo(_disposables);
```

**Toggle 바인딩:**
```csharp
// Toggle → Model
_view.SoundToggle
    .OnValueChangedAsObservable()
    .Subscribe(isOn => _model.SoundEnabled.Value = isOn)
    .AddTo(_disposables);

// Model → Toggle (단방향)
_model.SoundEnabled
    .Subscribe(enabled => _view.SoundToggle.isOn = enabled)
    .AddTo(_disposables);
```

**Inspector 노출이 필요한 경우:**
```csharp
// SerializableReactiveProperty — Inspector에서 초기값 편집 가능
[SerializeField] SerializableReactiveProperty<int> _maxHp = new(100);
[SerializeField] SerializableReactiveProperty<float> _speed = new(5f);
// 일반 ReactiveProperty와 동일하게 Subscribe 가능
// 단, 약간의 추가 메모리 사용 (직렬화용 필드 1개)
```

---

### 3.2 ReactiveCommand — 버튼 클릭 + CanExecute

`ReactiveCommand`는 `ICommand`를 구현하는 Observable이다. `canExecuteSource`로 실행 가능 상태를 동적으로 제어할 수 있다.

**기본 ReactiveCommand:**
```csharp
// canExecute 없이 단순 커맨드
var command = new ReactiveCommand();
command.Subscribe(_ => Debug.Log("실행됨")).AddTo(_disposables);

// 버튼에 연결
_view.ActionButton
    .OnClickAsObservable()
    .Subscribe(_ => command.Execute(Unit.Default))
    .AddTo(_disposables);
```

**CanExecute 제어:**
```csharp
// Observable<bool>로 canExecute 정의
ReactiveProperty<bool> isReady = new(false);
var command = isReady.ToReactiveCommand();  // isReady가 true일 때만 실행 가능

command.Subscribe(_ => ExecuteAction()).AddTo(_disposables);

// 버튼 interactable을 CanExecute와 동기화
command.SubscribeToInteractable(_view.ActionButton).AddTo(_disposables);
// 또는 명시적으로:
isReady.SubscribeToInteractable(_view.ActionButton).AddTo(_disposables);
```

**더블클릭 방지 (AsyncReactiveCommand 패턴):**
```csharp
// SubscribeAwait + AwaitOperation.Drop — 진행 중 클릭 무시
_view.PurchaseButton
    .OnClickAsObservable()
    .SubscribeAwait(async (_, ct) =>
    {
        // 실행 중에는 새 클릭 Drop
        _view.PurchaseButton.interactable = false;
        await ProcessPurchaseAsync(ct);
        _view.PurchaseButton.interactable = true;
    }, AwaitOperation.Drop, configureAwait: false)
    .AddTo(_disposables);

// 또는 ReactiveCommand의 async 생성자 사용
var buyCommand = new ReactiveCommand<Unit>(
    async (_, ct) => await ProcessPurchaseAsync(ct),
    awaitOperation: AwaitOperation.Drop);
```

**여러 조건 결합:**
```csharp
// 자원이 충분하고 쿨다운이 끝났을 때만 실행 가능
Observable<bool> canExecute = Observable.CombineLatest(
    _model.Gold.Select(g => g >= GoldCost),
    _model.IsOnCooldown.Select(c => !c),
    (hasGold, notOnCooldown) => hasGold && notOnCooldown);

var buildCommand = canExecute.ToReactiveCommand();
canExecute.SubscribeToInteractable(_view.BuildButton).AddTo(_disposables);
buildCommand.Subscribe(_ => _model.Build()).AddTo(_disposables);
```

---

### 3.3 파생 상태 — CombineLatest, Merge, Select

복수의 ReactiveProperty에서 계산된 UI 상태를 만드는 패턴이다.

**CombineLatest — 두 값의 조합:**
```csharp
// 건설 가능 조건 = Gold >= 30 AND Wood >= 20
public Observable<bool> CanBuild =>
    Observable.CombineLatest(
        Gold, Wood,
        (g, w) => g >= GoldCost && w >= WoodCost);

// 정규화된 HP 값
public Observable<float> NormalizedHp =>
    Observable.CombineLatest(
        CurrentHp, MaxHp,
        (c, m) => m > 0 ? (float)c / m : 0f);
```

**Select — 단일 값 변환:**
```csharp
// int → string 포맷 변환
_model.Gold
    .Select(g => $"Gold: {g:N0}")
    .Subscribe(text => _view.GoldLabel.text = text)
    .AddTo(_disposables);

// 값 범위에 따른 색상 변환
_model.Hp
    .Select(hp => hp > 50 ? Color.green : hp > 20 ? Color.yellow : Color.red)
    .Subscribe(color => _view.HpText.color = color)
    .AddTo(_disposables);

// 파생 ReadOnlyReactiveProperty (값 캐싱)
public ReadOnlyReactiveProperty<bool> IsDead =>
    CurrentHp.Select(hp => hp <= 0).ToReadOnlyReactiveProperty();
```

**Merge — 여러 이벤트 소스 통합:**
```csharp
// 어떤 버튼이든 클릭 시 공통 처리
Observable.Merge(
    _view.SaveButton.OnClickAsObservable(),
    _view.SaveAndExitButton.OnClickAsObservable())
    .Subscribe(_ => SaveGame())
    .AddTo(_disposables);
```

**EveryValueChanged — 폴링 기반 변화 감지:**
```csharp
// ReactiveProperty가 없는 외부 값을 Observable로 변환
Observable.EveryValueChanged(this, x => x.transform.position)
    .Subscribe(pos => _minimap.UpdatePosition(pos))
    .AddTo(_disposables);
```

---

### 3.4 컬렉션 바인딩 — ScrollView 동적 갱신

R3의 컬렉션 바인딩은 `ObservableCollections` 패키지(`Cysharp/ObservableCollections`)와 함께 사용한다.

**ObservableList 기본 사용:**
```csharp
using ObservableCollections;

// Model
private readonly ObservableList<ItemData> _items = new();
public IReadOnlyObservableList<ItemData> Items => _items;

// 추가/삭제
_items.Add(new ItemData { Name = "Sword", Value = 100 });
_items.RemoveAt(0);
```

**R3 연동 — 변경 이벤트 구독:**
```csharp
// 아이템 추가 시 UI 셀 생성
_model.Items
    .ObserveAdd()
    .Subscribe(e =>
    {
        var cell = Instantiate(_cellPrefab, _container);
        cell.SetData(e.Value);
    })
    .AddTo(_disposables);

// 아이템 제거 시 UI 셀 제거
_model.Items
    .ObserveRemove()
    .Subscribe(e =>
    {
        var cell = _container.GetChild(e.Index);
        Destroy(cell.gameObject);
    })
    .AddTo(_disposables);

// 전체 변경 감지 (범용)
_model.Items
    .ObserveChanged()
    .Subscribe(_ => RefreshAllCells())
    .AddTo(_disposables);
```

**ISynchronizedView 패턴 (필터/정렬 포함):**
```csharp
// 뷰 생성 — 원본 컬렉션과 동기화된 뷰
using var view = _model.Items.CreateView(item => new ItemCellData(item));

// 필터 적용 (반응형)
view.AttachFilter(item => item.Rarity >= _filterRarity);

// 필터 해제
view.ResetFilter();

// 뷰 순회 (필터된 결과만)
foreach (var cellData in view)
    UpdateCell(cellData);
```

**카운트 변화 감지:**
```csharp
_model.Items
    .ObserveCountChanged()
    .Subscribe(count => _view.CountLabel.text = $"Items: {count}")
    .AddTo(_disposables);
```

---

### 3.5 Throttle / Debounce — UI 입력 최적화

R3는 UniRx의 `Throttle`을 `Debounce`로 이름 변경했다 (LINQ/Rx.NET 표준).

| UniRx 용어 | R3 용어 | 설명 |
|---|---|---|
| `Throttle` | `Debounce` | 마지막 이벤트 후 N ms 침묵 시 emit |
| `ThrottleFirst` | `ThrottleFirst` | 동일 |
| `Sample` | `ThrottleLast` | 주기적으로 최신값 emit |

**검색 입력 디바운싱:**
```csharp
// InputField 텍스트 변경 → 0.3초 침묵 후 검색 실행
_view.SearchField
    .OnValueChangedAsObservable()
    .Debounce(TimeSpan.FromMilliseconds(300))
    .Subscribe(query => SearchItems(query))
    .AddTo(_disposables);

// 빈 문자열 제외
_view.SearchField
    .OnValueChangedAsObservable()
    .Where(s => !string.IsNullOrEmpty(s))
    .Debounce(TimeSpan.FromMilliseconds(300))
    .SubscribeAwait(async (query, ct) => await SearchAsync(query, ct), AwaitOperation.Switch)
    .AddTo(_disposables);
```

**Slider 값 쓰로틀링:**
```csharp
// Slider 드래그 중 과도한 업데이트 방지 — 100ms마다 최신값만 처리
_view.VolumeSlider
    .OnValueChangedAsObservable()
    .ThrottleLast(TimeSpan.FromMilliseconds(100))
    .Subscribe(v => ApplyVolumeChange(v))
    .AddTo(_disposables);
```

**프레임 기반 (TimeSpan 대신 프레임 카운트 사용):**
```csharp
// 3프레임마다 최신값 처리 (TimeProvider 불필요)
someObservable
    .DebounceFrame(3)
    .Subscribe(x => ProcessValue(x))
    .AddTo(_disposables);
```

---

### 3.6 에러 처리 — 리액티브 체인에서 예외 표시

R3는 에러 발생 시 구독을 종료하지 않는다 (`OnErrorResume` 설계).

**기본 에러 핸들링 (구독 유지):**
```csharp
someObservable
    .Subscribe(
        onNext: x => ProcessData(x),
        onErrorResume: ex =>
        {
            Debug.LogException(ex);
            _view.ErrorLabel.text = $"오류: {ex.Message}";
            _view.ErrorLabel.gameObject.SetActive(true);
        },
        onCompleted: _ => { }
    )
    .AddTo(_disposables);
```

**에러 발생 시 스트림 종료 원할 때:**
```csharp
// OnErrorResumeAsFailure() → OnCompleted(Failure)로 전환
someObservable
    .OnErrorResumeAsFailure()   // 이후부터는 에러 시 종료
    .Subscribe(
        onNext: x => ProcessData(x),
        onCompleted: result =>
        {
            if (result.IsFailure)
            {
                Debug.LogError(result.Exception);
                ShowErrorUI(result.Exception.Message);
            }
        }
    )
    .AddTo(_disposables);
```

**전역 에러 핸들러:**
```csharp
// 앱 시작 시 등록 (모든 구독되지 않은 OnErrorResume 처리)
ObservableSystem.RegisterUnhandledExceptionHandler(ex =>
{
    Debug.LogError($"[R3 Unhandled] {ex}");
    // 에러 리포팅 서비스로 전송
});
```

**Catch로 폴백 값 제공:**
```csharp
// API 호출 실패 시 빈 목록 반환
_apiService.FetchItemsAsObservable()
    .Catch<IList<Item>, Exception>(ex =>
    {
        Debug.LogWarning($"아이템 로딩 실패: {ex.Message}");
        return Observable.Return(new List<Item>());
    })
    .Subscribe(items => RefreshItemList(items))
    .AddTo(_disposables);
```

---

### 3.7 구독 수명 관리 — VContainer 연동

VContainer의 수명주기 인터페이스와 R3의 disposal 패턴을 조합한다.

**핵심 규칙:**
- Subscribe는 반드시 `IInitializable.Initialize()` 또는 `IStartable.Start()`에서 수행
- `VContainer Construct()`에서 Subscribe 금지 (Awake 전 실행으로 데드락 발생 가능)
- 모든 구독은 `CompositeDisposable`에 `AddTo`로 등록
- `IDisposable.Dispose()`에서 일괄 해제

**표준 Presenter 패턴:**
```csharp
public class MyPresenter : IInitializable, IDisposable
{
    private readonly MyModel _model;
    private readonly MyView _view;
    private readonly CompositeDisposable _disposables = new();

    public MyPresenter(MyModel model, MyView view) // 생성자: 주입만
    {
        _model = model;
        _view = view;
    }

    public void Initialize()  // ← 여기서 Subscribe
    {
        _model.SomeValue
            .Subscribe(_view.UpdateDisplay)
            .AddTo(_disposables);

        _view.ActionButton
            .OnClickAsObservable()
            .Subscribe(_ => _model.DoAction())
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}

// VContainer 등록
builder.RegisterEntryPoint<MyPresenter>();
```

**DisposableBag (struct, 고성능):**
```csharp
// CompositeDisposable보다 빠르나 스레드 비안전
// 고정 스코프(단일 클래스 내)에서 ref 파라미터로 사용
private DisposableBag _bag;

public void Initialize()
{
    _model.Value.Subscribe(OnValueChanged).AddTo(ref _bag);
    _view.Button.OnClickAsObservable().Subscribe(OnClick).AddTo(ref _bag);
}

public void Dispose() => _bag.Dispose();
```

**성능 우선순위 (빠른 순):**
```csharp
// 1. Disposable.Combine (최대 8개, 최고 성능)
var d = Disposable.Combine(sub1, sub2, sub3);

// 2. DisposableBag.Create() 빌더 패턴
var d = Disposable.CreateBuilder();
sub1.AddTo(ref d);
sub2.AddTo(ref d);
var disposable = d.Build();

// 3. DisposableBag (struct, AddTo(ref bag))
DisposableBag bag = new();
sub1.AddTo(ref bag);

// 4. CompositeDisposable (스레드 안전, 동적 추가 가능)
CompositeDisposable composite = new();
sub1.AddTo(composite);
```

**MonoBehaviour에서 AddTo(this):**
```csharp
public class MyView : MonoBehaviour
{
    private void Start()
    {
        // destroyCancellationToken과 연동 — GameObject 파괴 시 자동 해제
        someObservable
            .Subscribe(x => UpdateUI(x))
            .AddTo(this);  // MonoBehaviour 오버로드
    }
}
```

**ObservableTracker로 누수 디버깅:**
```csharp
// 개발 중 활성화 (Window > Observable Tracker)
ObservableTracker.EnableTracking = true;
ObservableTracker.EnableStackTrace = true;
// 빌드 전 반드시 비활성화
```

---

### 3.8 양방향 바인딩 — UI ↔ Model

R3는 단방향 스트림이 기본이다. 양방향 바인딩은 명시적인 두 구독으로 구현하되, 무한 루프 방지가 핵심이다.

**InputField ↔ ReactiveProperty<string>:**
```csharp
private bool _isUpdating = false;

public void Initialize()
{
    // Model → View (외부 변경 반영)
    _model.PlayerName
        .Subscribe(name =>
        {
            if (_isUpdating) return;  // 피드백 루프 차단
            _view.NameField.text = name;
        })
        .AddTo(_disposables);

    // View → Model (유저 입력 반영)
    _view.NameField
        .OnValueChangedAsObservable()
        .Subscribe(name =>
        {
            _isUpdating = true;
            _model.PlayerName.Value = name;
            _isUpdating = false;
        })
        .AddTo(_disposables);
}
```

**ReactiveProperty의 EqualityComparer 활용 (자동 중복 방지):**
```csharp
// ReactiveProperty는 기본적으로 DistinctUntilChanged 적용
// 동일 값 재설정 시 구독자 미호출 → 루프 자동 방지
_model.Volume.Value = slider.value;   // 값이 동일하면 emit 없음
```

**Slider ↔ ReactiveProperty<float>:**
```csharp
// 주의: Slider.value 설정 시 OnValueChanged 이벤트 발생 → 무한 루프 가능
// Skip(1)로 첫 emit(구독 시 초기값) 이후부터만 반응
_view.VolumeSlider
    .OnValueChangedAsObservable()
    .Skip(1)  // 구독 시 초기값 emit 무시
    .DistinctUntilChanged()
    .Subscribe(v => _model.Volume.Value = v)
    .AddTo(_disposables);

_model.Volume
    .DistinctUntilChanged()
    .Subscribe(v =>
    {
        if (Mathf.Approximately(_view.VolumeSlider.value, v)) return;
        _view.VolumeSlider.value = v;
    })
    .AddTo(_disposables);
```

---

### 3.9 Observable Triggers — UI 이벤트 Observable 변환

`using R3.Triggers;` 추가 후 Component/GameObject 확장 메서드로 사용한다. Trigger 컴포넌트가 자동으로 추가되며, GameObject 파괴 시 `OnCompleted` 호출로 자동 정리된다.

**포인터 이벤트:**
```csharp
// OnPointerEnter / OnPointerExit — 호버 감지
_targetUI
    .OnPointerEnterAsObservable()
    .Subscribe(_ => ShowHighlight())
    .AddTo(_disposables);

_targetUI
    .OnPointerExitAsObservable()
    .Subscribe(_ => HideHighlight())
    .AddTo(_disposables);

// 호버 상태를 boolean Observable로 변환
var isHovered = Observable.Merge(
    _targetUI.OnPointerEnterAsObservable().Select(_ => true),
    _targetUI.OnPointerExitAsObservable().Select(_ => false));

isHovered
    .Subscribe(hovered => _view.SetHighlight(hovered))
    .AddTo(_disposables);
```

**드래그 이벤트 체인:**
```csharp
// 드래그 중 델타 계산
_dragTarget
    .OnBeginDragAsObservable()
    .SelectMany(_ => _dragTarget.OnDragAsObservable()
        .TakeUntil(_dragTarget.OnEndDragAsObservable()))
    .Subscribe(eventData =>
    {
        _panel.anchoredPosition += eventData.delta;
    })
    .AddTo(_disposables);
```

**스크롤 이벤트:**
```csharp
// 스크롤 방향 감지
_scrollArea
    .OnScrollAsObservable()
    .Subscribe(eventData =>
    {
        float scrollDelta = eventData.scrollDelta.y;
        _model.Zoom.Value = Mathf.Clamp(_model.Zoom.Value + scrollDelta * 0.1f, 0.5f, 2f);
    })
    .AddTo(_disposables);
```

**Observable.Timer로 딜레이 트리거 (툴팁 패턴):**
```csharp
// 0.4초 호버 유지 시 툴팁 표시
public void OnPointerEnter(PointerEventData eventData)
{
    _delaySubscription?.Dispose();
    _delaySubscription = Observable.Timer(TimeSpan.FromSeconds(0.4f))
        .Subscribe(_ => ShowTooltip());
}

public void OnPointerExit(PointerEventData eventData)
{
    _delaySubscription?.Dispose();
    HideTooltip();
}
```

**사용 가능한 주요 Trigger 메서드 목록:**
```
// 포인터
OnPointerEnterAsObservable()    — IPointerEnterHandler
OnPointerExitAsObservable()     — IPointerExitHandler
OnPointerClickAsObservable()    — IPointerClickHandler
OnPointerDownAsObservable()     — IPointerDownHandler
OnPointerUpAsObservable()       — IPointerUpHandler

// 드래그
OnBeginDragAsObservable()       — IBeginDragHandler
OnDragAsObservable()            — IDragHandler
OnEndDragAsObservable()         — IEndDragHandler

// 스크롤
OnScrollAsObservable()          — IScrollHandler

// MonoBehaviour 라이프사이클
OnDestroyAsObservable()
OnEnableAsObservable()
OnDisableAsObservable()
OnTriggerEnterAsObservable()    — Physics
OnCollisionEnterAsObservable()  — Physics
```

---

### 3.10 R3 + UniTask 통합 — Observable ↔ Task 변환

R3와 UniTask는 상호 변환이 가능하며, 각각이 잘하는 영역이 다르다.

**Observable → UniTask (단일 값 대기):**
```csharp
// 첫 번째 값을 await (UniTask.FromObservable 또는 FirstAsync)
var result = await _model.IsReady
    .Where(ready => ready)
    .FirstAsync(cancellationToken);

// 다이얼로그 버튼 클릭 대기
var confirmed = await _view.ConfirmButton
    .OnClickAsObservable()
    .FirstAsync(ct);
```

**SubscribeAwait — Observable 안에서 async 실행:**
```csharp
// 버튼 클릭 → 비동기 처리 (R3 스트림 유지)
_view.SaveButton
    .OnClickAsObservable()
    .SubscribeAwait(async (_, ct) =>
    {
        _view.SetLoadingVisible(true);
        await _saveService.SaveAsync(ct);
        _view.SetLoadingVisible(false);
        _view.ShowSavedFeedback();
    }, AwaitOperation.Drop, configureAwait: false)
    .AddTo(_disposables);

// AwaitOperation 선택 가이드:
// - Drop: 저장, 구매 — 이미 처리 중이면 무시
// - Sequential: 큐잉 필요한 경우 — 순서 보장
// - Switch: 검색, 자동완성 — 최신 요청만 유효
// - Parallel: 독립적인 병렬 작업
```

**다이얼로그 await 패턴 (UniTask 기반):**
```csharp
// DialogService: R3 Observable 이벤트를 UniTaskCompletionSource로 브릿징
public async UniTask<bool> ShowConfirmAsync(string message, CancellationToken ct = default)
{
    _view.SetMessage(message);
    _view.Show();

    var tcs = new UniTaskCompletionSource();
    var confirmed = false;

    using var confirmSub = _view.OnConfirmClick
        .Subscribe(_ => { confirmed = true; tcs.TrySetResult(); });
    using var cancelSub = _view.OnCancelClick
        .Subscribe(_ => { confirmed = false; tcs.TrySetResult(); });

    // 취소 토큰 연결
    using var ctr = ct.CanBeCanceled
        ? ct.Register(() => tcs.TrySetCanceled())
        : default;

    try { await tcs.Task; }
    catch (OperationCanceledException) { confirmed = false; }
    finally { _view.Hide(); }

    return confirmed;
}

// 사용처 (Presenter)
_view.SpendButton
    .OnClickAsObservable()
    .SubscribeAwait(async (_, ct) =>
    {
        var ok = await _dialogService.ShowConfirmAsync("소비하시겠습니까?", ct);
        if (ok) _model.Spend();
    }, AwaitOperation.Drop, configureAwait: false)
    .AddTo(_disposables);
```

**SelectAwait — Observable 내 비동기 변환:**
```csharp
// 클릭 → 비동기 데이터 로드 → 결과 emit
_view.ItemButton
    .OnClickAsObservable()
    .SelectAwait(async (_, ct) => await _api.FetchItemDetailAsync(ct),
        AwaitOperation.Switch)  // 새 클릭 시 이전 요청 취소
    .Subscribe(detail => _view.ShowItemDetail(detail))
    .AddTo(_disposables);
```

**UniTask → Observable 변환:**
```csharp
// UniTask를 Observable로 래핑 (단발성 비동기를 스트림에 편입)
Observable.FromAsync(ct => LoadDataAsync(ct))
    .Subscribe(data => InitializeUI(data))
    .AddTo(_disposables);
```

---

## 4. R3 내장 UI 확장 메서드 참조

`R3.Unity` 패키지가 제공하는 UI 컴포넌트 확장 메서드 전체 목록:

| 컴포넌트 | 메서드 | 반환 타입 | 비고 |
|---|---|---|---|
| `Button` | `OnClickAsObservable()` | `Observable<Unit>` | |
| `Toggle` | `OnValueChangedAsObservable()` | `Observable<bool>` | 구독 시 현재값 emit |
| `Slider` | `OnValueChangedAsObservable()` | `Observable<float>` | 구독 시 현재값 emit |
| `InputField` | `OnValueChangedAsObservable()` | `Observable<string>` | 구독 시 현재값 emit |
| `InputField` | `OnEndEditAsObservable()` | `Observable<string>` | 엔터/포커스 아웃 |
| `Scrollbar` | `OnValueChangedAsObservable()` | `Observable<float>` | 구독 시 현재값 emit |
| `ScrollRect` | `OnValueChangedAsObservable()` | `Observable<Vector2>` | normalized position |
| `Dropdown` | `OnValueChangedAsObservable()` | `Observable<int>` | 구독 시 현재값 emit |
| `Observable<string>` | `SubscribeToText(Text)` | `IDisposable` | UGUI Text 전용 |
| `Observable<T>` | `SubscribeToText<T>(Text)` | `IDisposable` | ToString() 사용 |
| `Observable<T>` | `SubscribeToText<T>(Text, Func<T,string>)` | `IDisposable` | 커스텀 포맷터 |
| `Observable<bool>` | `SubscribeToInteractable(Selectable)` | `IDisposable` | interactable 바인딩 |

---

## 5. 패턴 선택 가이드

| 상황 | 권장 패턴 |
|---|---|
| 단순 값 표시 (int/string → TMP) | `ReactiveProperty + Subscribe` |
| 여러 값의 조합 상태 | `CombineLatest + Select` |
| 버튼 활성/비활성 제어 | `Observable<bool>.SubscribeToInteractable()` |
| 버튼 클릭 → 즉시 실행 | `OnClickAsObservable().Subscribe()` |
| 버튼 클릭 → 비동기 (더블클릭 방지) | `OnClickAsObservable().SubscribeAwait(Drop)` |
| 버튼 클릭 → 다이얼로그 확인 | `SubscribeAwait + UniTaskCompletionSource` |
| 검색/자동완성 입력 | `OnValueChangedAsObservable().Debounce(300ms)` |
| Slider 성능 최적화 | `OnValueChangedAsObservable().ThrottleLast(100ms)` |
| HP바 (fillAmount) | `CombineLatest(current, max) + Subscribe(normalized)` |
| 동적 리스트 UI | `ObservableList + ObserveAdd/Remove` |
| 호버 툴팁 | `Observable.Timer + OnPointerEnter/Exit` |
| 드래그 UI | `OnBeginDrag.SelectMany(...OnDrag.TakeUntil(OnEndDrag))` |
| 에러 표시 (구독 유지) | `Subscribe(onErrorResume:)` |
| Inspector 초기값 설정 | `SerializableReactiveProperty<T>` |

---

## 6. 주의사항 및 안티패턴

### 하면 안 되는 것들

**Construct()에서 Subscribe:**
```csharp
// 위험! VContainer Construct()는 Awake 이전 — View가 초기화 안 됐을 수 있음
public MyPresenter(MyModel model, MyView view)
{
    model.Value.Subscribe(view.UpdateUI); // 데드락 가능
}
// 올바른 위치: IInitializable.Initialize()
```

**Subscribe 반환값 무시:**
```csharp
// 누수! IDisposable 반환값을 버리면 GC 전까지 구독 유지
_model.Value.Subscribe(x => { });  // 나쁨

// 올바른 방법
_model.Value.Subscribe(x => { }).AddTo(_disposables);  // 좋음
```

**양방향 바인딩에서 무한 루프:**
```csharp
// 위험! 서로가 서로를 트리거
_model.Value.Subscribe(v => _slider.value = v);   // Slider 값 변경
_slider.OnValueChangedAsObservable().Subscribe(v => _model.Value.Value = v);  // Model 다시 변경 → 루프!

// 해결: DistinctUntilChanged 또는 플래그 사용
```

**Hot Observable에 AsUniTask() 직접 사용:**
```csharp
// 위험! Hot Observable은 OnCompleted가 없어 UniTask가 영원히 완료 안 됨
await someReactiveProperty.AsUniTask(); // 데드락

// 올바른 방법: First/FirstAsync로 단일 값만 추출
await someReactiveProperty.FirstAsync(ct);
```

---

## 7. 참고 자료

1. [Cysharp/R3 GitHub](https://github.com/Cysharp/R3)
2. [R3 README (공식 API 문서)](https://github.com/Cysharp/R3/blob/main/README.md)
3. [R3 공식 문서 사이트](https://filzrev.github.io/R3/articles/)
4. [neuecc - R3 설계 철학 (Medium)](https://neuecc.medium.com/r3-a-new-modern-reimplementation-of-reactive-extensions-for-c-cf29abcc5826)
5. [Cysharp/ObservableCollections GitHub](https://github.com/Cysharp/ObservableCollections)
6. [zerodev1200/R3Utility - 유효성 검사 + 양방향 바인딩 유틸](https://github.com/zerodev1200/R3Utility)
7. [UniRx → R3 마이그레이션 가이드 (일본어)](https://wiki.nonip.net/index.php/Unity/R3/UniRx%E3%81%8B%E3%82%89%E7%A7%BB%E6%A4%8D)
8. [Aiming 블로그 - R3 + UniTask SubscribeAwait 실전](https://developer.aiming-inc.com/csharp/post-10773/)
9. [R3 Observable as UniTask 완료 이슈 #576](https://github.com/Cysharp/UniTask/issues/576)
10. [R3 ReactiveCommand with CommandParameter 이슈 #349](https://github.com/Cysharp/R3/issues/349)

---

## 8. 프로젝트 내 실전 예시 파일

이 프로젝트에서 실제로 구현된 패턴들:

| 파일 | 패턴 |
|---|---|
| `UI_Study/Assets/_Study/01-MVRP-Foundation/Scripts/Presenters/CounterPresenter.cs` | 기본 ReactiveProperty→TMP 바인딩, AddTo(CompositeDisposable) |
| `UI_Study/Assets/_Study/01-MVRP-Foundation/Scripts/Presenters/ResourceHUDPresenter.cs` | 다중 바인딩 + interactable 제어 |
| `UI_Study/Assets/_Study/01-MVRP-Foundation/Scripts/Presenters/ResourceWithDialogPresenter.cs` | SubscribeAwait + AwaitOperation.Drop + 다이얼로그 |
| `UI_Study/Assets/_Study/01-MVRP-Foundation/Scripts/Services/DialogService.cs` | UniTaskCompletionSource 다이얼로그 브릿지 |
| `UI_Study/Assets/_Study/05-Advanced-Patterns/Scripts/Models/BuildActionModel.cs` | CombineLatest 파생 상태 (CanBuild) |
| `UI_Study/Assets/_Study/05-Advanced-Patterns/Scripts/Presenters/AnimatedBarPresenter.cs` | CombineLatest(HP/MaxHP) → fillAmount 정규화 |
| `UI_Study/Assets/_Study/05-Advanced-Patterns/Scripts/Views/AnimatedBarView.cs` | fillAmount + DOTween 애니메이션 |
| `UI_Study/Assets/_Study/05-Advanced-Patterns/Scripts/Views/TooltipTrigger.cs` | Observable.Timer + IPointerEnterHandler 조합 |
