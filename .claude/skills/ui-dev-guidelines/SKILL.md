---
name: ui-dev-guidelines
description: Unity UGUI 개발 시 확정된 기술 스택(MV(R)P + VContainer + R3 + UniTask)의 올바른 사용법과 패턴을 제공. UI 스크립트 작성/수정 시 자동 활성화되어 아키텍처 위반을 방지한다.
---

# Unity UI 개발 가이드라인

## 확정 기술 스택

| 역할 | 라이브러리 |
|---|---|
| 아키텍처 | MV(R)P (Model-View-Reactive Presenter) |
| DI | VContainer |
| 리액티브 | R3 |
| 비동기 | UniTask |
| 네비게이션 | UnityScreenNavigator |
| 애니메이션 | DOTween |
| 테마 | uPalette |

---

## MV(R)P 아키텍처 규칙

### 레이어별 규칙

**View (MonoBehaviour)**
```csharp
public class BuildMenuView : MonoBehaviour
{
    [SerializeField] Button _buildButton;
    [SerializeField] TextMeshProUGUI _goldText;
    [SerializeField] Button _closeButton;

    // ✅ Observable 이벤트 노출
    public Observable<Unit> OnBuildClick => _buildButton.OnClickAsObservable();
    public Observable<Unit> OnCloseClick => _closeButton.OnClickAsObservable();

    // ✅ 단순 표시 메서드
    public void SetGold(int gold) => _goldText.text = $"{gold:N0}";
    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    // ❌ 금지: 비즈니스 로직
    // ❌ 금지: Model 직접 참조
    // ❌ 금지: Subscribe 호출
}
```

**Presenter (Pure C# — MonoBehaviour 아님)**
```csharp
public class BuildMenuPresenter : IInitializable, IDisposable
{
    readonly BuildMenuView _view;
    readonly ResourceSystem _model;
    readonly CompositeDisposable _disposables = new();

    // ✅ 생성자 주입 (VContainer)
    public BuildMenuPresenter(BuildMenuView view, ResourceSystem model)
    {
        _view = view;
        _model = model;
    }

    // ✅ Initialize에서 Subscribe
    public void Initialize()
    {
        _model.Gold
            .Subscribe(_view.SetGold)
            .AddTo(_disposables);

        _view.OnBuildClick
            .Subscribe(_ => _model.SpendGold(100))
            .AddTo(_disposables);
    }

    // ✅ Dispose에서 해제
    public void Dispose() => _disposables.Dispose();

    // ❌ 금지: MonoBehaviour 상속
    // ❌ 금지: Update() 사용
    // ❌ 금지: 생성자에서 Subscribe
}
```

**Model (Plain C#)**
```csharp
public class ResourceSystem : IDisposable
{
    // ✅ ReactiveProperty로 상태 노출
    public ReactiveProperty<int> Gold { get; } = new(0);

    // ✅ 순수 로직
    public void SpendGold(int amount)
    {
        if (Gold.Value >= amount)
            Gold.Value -= amount;
    }

    public void Dispose() => Gold.Dispose();

    // ❌ 금지: UI 참조 (View, Canvas 등)
    // ❌ 금지: using UnityEngine.UI
}
```

### LifetimeScope 등록 패턴

```csharp
public class GameSceneLifetimeScope : LifetimeScope
{
    [SerializeField] ResourceBarView _resourceBarView;

    protected override void Configure(IContainerBuilder builder)
    {
        // View — RegisterComponent
        builder.RegisterComponent(_resourceBarView);

        // Presenter — RegisterEntryPoint (Initialize/Dispose 자동 호출)
        builder.RegisterEntryPoint<ResourceBarPresenter>();

        // Model — Register
        builder.Register<ResourceSystem>(Lifetime.Singleton);
    }
}
```

---

## R3 사용 규칙

### Subscribe 타이밍

```csharp
// ✅ 올바름: IInitializable.Initialize()
public void Initialize()
{
    _model.Value.Subscribe(x => _view.SetText(x)).AddTo(_disposables);
}

// ✅ 올바름: IStartable.Start()
public void Start()
{
    _model.Value.Subscribe(x => _view.SetText(x)).AddTo(_disposables);
}

// ❌ 위험: 생성자 또는 [Inject] 메서드
public BuildPresenter(Model m, View v)
{
    m.Value.Subscribe(x => v.SetText(x)); // 데드락 위험!
}
```

### Disposal 패턴

```csharp
// Presenter (Pure C#) — CompositeDisposable 또는 DisposableBag
public class MyPresenter : IInitializable, IDisposable
{
    readonly CompositeDisposable _disposables = new();
    // 또는: DisposableBag _disposables = new();

    public void Dispose() => _disposables.Dispose();
}

// View (MonoBehaviour) — AddTo(this)
public class MyView : MonoBehaviour
{
    void Start()
    {
        someObservable.Subscribe(x => { }).AddTo(this);
        // destroyCancellationToken 자동 연동
    }
}
```

### 자주 쓰는 오퍼레이터

```csharp
// 디바운스 (입력 필드 검색)
inputField.OnValueChangedAsObservable()
    .Debounce(TimeSpan.FromMilliseconds(300))
    .Subscribe(text => Search(text));

// 쓰로틀 (버튼 연타 방지)
button.OnClickAsObservable()
    .ThrottleFirst(TimeSpan.FromSeconds(1))
    .Subscribe(_ => Submit());

// 값 변환
_model.Health
    .Select(hp => $"{hp} / {_model.MaxHealth}")
    .Subscribe(_view.SetHealthText);

// 조건부 필터링
_model.Gold
    .Where(gold => gold < 100)
    .Subscribe(_ => _view.ShowLowGoldWarning());
```

### ❌ 금지 / 주의

```csharp
// ❌ UniRx API 사용 금지 (R3 이름 사용)
.Throttle()    // → .Debounce()
.Buffer()      // → .Chunk()
.StartWith()   // → .Prepend()

// ❌ hot observable을 UniTask로 변환 금지
observable.AsSystemObservable().ToUniTask(); // 미완료 문제

// ⚠️ SubscribeAwait 사용 시 configureAwait 필수
.SubscribeAwait(async (x, ct) => { await Work(ct); },
    AwaitOperation.Drop, configureAwait: false);
```

---

## UniTask 사용 규칙

### 기본 패턴

```csharp
// ✅ 다이얼로그 await
bool confirmed = await _dialog.ShowAsync(destroyCancellationToken);

// ✅ 병렬 로딩
var (a, b) = await UniTask.WhenAll(LoadA(ct), LoadB(ct));

// ✅ 순차 애니메이션
await panel.DOFade(1f, 0.2f).AsyncWaitForCompletion();
await UniTask.Delay(500, cancellationToken: ct);
await panel.DOScale(Vector3.one, 0.3f).AsyncWaitForCompletion();
```

### VContainer 통합

```csharp
// IAsyncStartable — 비동기 초기화
public class SceneBootstrapper : IAsyncStartable
{
    public async UniTask StartAsync(CancellationToken ct)
    {
        await Addressables.LoadSceneAsync("HUD").WithCancellation(ct);
    }
}
```

### ❌ 금지

```csharp
// ❌ async void 금지
async void OnButtonClick() { }  // 예외 삼킴

// ✅ 대안
UniTask.UnityAction(async () => { await DoWork(ct); });
// 또는
async UniTaskVoid OnButtonClickAsync() { }

// ❌ UniTask 두 번 await 금지 (풀로 반환됨)
var task = LoadAsync();
await task;
await task;  // InvalidOperationException!

// ❌ System.Progress<T> 사용 금지 (할당 발생)
// ✅ Cysharp.Threading.Tasks.Progress.Create<float>() 사용
```

---

## UnityScreenNavigator 사용 규칙

### 화면 분류

| 타입 | 용도 | 메서드 |
|---|---|---|
| Page | 히스토리 스택 (뒤로 가기) | Push / Pop |
| Modal | 팝업 (아래 입력 차단) | Push / Pop |
| Sheet | 탭 (히스토리 없음) | Show |

### 기본 사용

```csharp
// Page 전환
await _pageContainer.PushAsync("BuildingDetail", playAnimation: true);
await _pageContainer.PopAsync(playAnimation: true);

// Modal 다이얼로그
await _modalContainer.PushAsync("ConfirmDialog", playAnimation: true);
await _modalContainer.PopAsync(playAnimation: true);
```

### VContainer 통합 (커스텀 팩토리 필요)

```csharp
// LifetimeScope.EnqueueParent + Enqueue로 자식 스코프 주입
using (LifetimeScope.EnqueueParent(_currentScope))
{
    await _pageContainer.PushAsync(screenKey);
}
```

---

## DOTween UI 패턴

```csharp
// 패널 열기
transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
canvasGroup.DOFade(1f, 0.2f);

// 패널 닫기
canvasGroup.DOFade(0f, 0.15f)
    .OnComplete(() => gameObject.SetActive(false));

// await 가능
await transform.DOScale(Vector3.one, 0.25f)
    .SetEase(Ease.OutBack)
    .AsyncWaitForCompletion();

// ❌ Animator를 UI에 사용하지 말 것 — 매 프레임 Canvas dirty
```

---

## Canvas 레이어 규칙

| 레이어 | Sort Order |
|---|---|
| World | 0 |
| HUD | 10 |
| Screens | 20 |
| Modals | 30 |
| Toast | 35 |
| Overlay | 40 |
| Debug | 100 |

---

## 입력 규칙

- `UnityEngine.Input` 사용 금지 — New Input System만 사용
- EventSystem에 `InputSystemUIInputModule` 사용 (StandaloneInputModule 제거)
- 게임패드/키보드 네비게이션: `Selectable.navigation` 설정 필수

---

## 에셋 관리 규칙

```csharp
// ✅ Addressables로 로드
var handle = Addressables.LoadAssetAsync<Sprite>("Atlas[sprite_name]");
var sprite = await handle.ToUniTask(cancellationToken: ct);

// ✅ 해제 필수
Addressables.Release(handle);

// ❌ Resources.Load 사용 금지
// ❌ SpriteAtlas가 아닌 개별 스프라이트를 Addressable 마킹 금지
```
