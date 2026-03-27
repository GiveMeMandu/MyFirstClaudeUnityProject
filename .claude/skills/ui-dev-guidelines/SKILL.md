---
name: ui-dev-guidelines
description: Unity UGUI 개발 시 확정된 기술 스택(MV(R)P + VContainer + R3 + UniTask)의 올바른 사용법과 패턴을 제공. UI 스크립트 작성/수정 시 자동 활성화되어 아키텍처 위반을 방지한다.
---

# Unity UI 개발 가이드라인

## 확정 기술 스택

| 역할 | 라이브러리 | 비고 |
|---|---|---|
| 아키텍처 | MV(R)P (Model-View-Reactive Presenter) | |
| DI | VContainer | Git URL 설치 |
| 리액티브 | R3 (UniRx 후속) | NuGet + Git URL |
| 비동기 | UniTask | Git URL 설치 |
| 네비게이션 | UnityScreenNavigator | Page/Modal/Sheet |
| 애니메이션 | DOTween | Asset Store |
| 테마 | uPalette | Git URL 설치 |
| 스크롤뷰 | ScrollRect + FancyScrollView | 보조 활용 |
| 로컬라이제이션 | Unity Localization | Unity Registry |
| 에셋 관리 | Addressables + SpriteAtlas V2 | Unity Registry |
| 폰트 | Interop-Regular SDF (한글) | TMP 기본 폰트 |

---

## MV(R)P 아키텍처 규칙

### 레이어별 규칙

**View (MonoBehaviour)**
```csharp
public class BuildMenuView : MonoBehaviour
{
    [SerializeField] private Button _buildButton;
    [SerializeField] private TextMeshProUGUI _goldText;

    // ✅ Observable 이벤트 노출
    public Observable<Unit> OnBuildClick => _buildButton.OnClickAsObservable();

    // ✅ 단순 표시 메서드
    public void SetGold(int gold) => _goldText.text = $"{gold:N0}";

    // ❌ 금지: 비즈니스 로직, Model 직접 참조, Subscribe 호출
}
```

**Presenter (Pure C# — MonoBehaviour 아님)**
```csharp
public class BuildMenuPresenter : IInitializable, IDisposable
{
    private readonly BuildMenuView _view;
    private readonly ResourceModel _model;
    private readonly CompositeDisposable _disposables = new();

    // ✅ 생성자 주입 (VContainer)
    public BuildMenuPresenter(BuildMenuView view, ResourceModel model)
    {
        _view = view;
        _model = model;
    }

    // ✅ Initialize에서 Subscribe
    public void Initialize()
    {
        _model.Gold.Subscribe(_view.SetGold).AddTo(_disposables);
        _view.OnBuildClick.Subscribe(_ => _model.SpendGold(100)).AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
    // ❌ 금지: MonoBehaviour 상속, Update(), 생성자에서 Subscribe
}
```

**Model (Plain C#)**
```csharp
public class ResourceModel : IDisposable
{
    public ReactiveProperty<int> Gold { get; } = new(0);

    public void SpendGold(int amount)
    {
        if (Gold.Value >= amount) Gold.Value -= amount;
    }

    public void Dispose() => Gold.Dispose();
    // ❌ 금지: UI 참조, using UnityEngine.UI
}
```

---

## VContainer 핵심 규칙

### LifetimeScope 등록

```csharp
public class GameSceneLifetimeScope : LifetimeScope
{
    [SerializeField] private ResourceBarView _view;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_view);             // View (MonoBehaviour)
        builder.RegisterEntryPoint<Presenter>();       // Presenter (Initialize/Dispose 자동)
        builder.Register<ResourceModel>(Lifetime.Singleton); // Model
    }
}
```

### ⚠️ 실전 함정 (검증됨)

```csharp
// ❌ Register<T>()에서 생성자에 string/int 등 기본 타입 파라미터 금지
// VContainer가 DI로 해결하려다 "No such registration of type: System.String" 에러
public class GameConfig
{
    public GameConfig(string title = "Game") { } // ❌ 실패!
}

// ✅ 파라미터 없는 생성자 + 프로퍼티 초기화
public class GameConfig
{
    public string Title { get; set; } = "Game";
}

// ✅ 또는 RegisterInstance로 미리 생성한 인스턴스 등록
builder.RegisterInstance(new GameConfig { Title = "Game" });
```

### 동적 자식 스코프 (화면 수명 연동)

```csharp
// Page Push 시 자식 스코프 생성
_childScope = _parentScope.CreateChild(builder =>
{
    builder.RegisterComponent(pageView);
    builder.RegisterEntryPoint<PagePresenter>();
});

// Pop 시 파괴 → Presenter.Dispose() 자동 호출
_childScope?.Dispose();
```

---

## R3 사용 규칙

### Subscribe 타이밍

```csharp
// ✅ IInitializable.Initialize() 또는 IStartable.Start()에서만
public void Initialize()
{
    _model.Value.Subscribe(x => _view.SetText(x)).AddTo(_disposables);
}

// ❌ 생성자 또는 [Inject]에서 Subscribe → 데드락 위험
```

### AddTo 패턴 (실전 검증)

```csharp
// ✅ Presenter (Pure C#) — CompositeDisposable
_model.Value.Subscribe(x => { }).AddTo(_disposables);

// ✅ MonoBehaviour — AddTo(this)
observable.Subscribe(x => { }).AddTo(this);

// ❌ AddTo(destroyCancellationToken) — ref 키워드 필요해서 컴파일 에러
// observable.Subscribe(x => { }).AddTo(destroyCancellationToken); // CS1620 에러!
// ✅ 대신 AddTo(this) 사용
```

### API 이름 (UniRx → R3 변환)

| UniRx | R3 |
|---|---|
| `.Throttle()` | `.Debounce()` |
| `.Buffer()` | `.Chunk()` |
| `.StartWith()` | `.Prepend()` |
| `.Sample()` | `.ThrottleLast()` |

### ❌ 금지 / 주의

```csharp
// ❌ hot observable → UniTask 변환 금지
observable.AsSystemObservable().ToUniTask(); // 미완료 문제

// ⚠️ SubscribeAwait 필수 옵션
.SubscribeAwait(async (x, ct) => { await Work(ct); },
    AwaitOperation.Drop, configureAwait: false);
// configureAwait: false 빠지면 메인 스레드 복귀 실패 가능
```

---

## UniTask 사용 규칙

### 다이얼로그 await 패턴 (검증됨)

```csharp
public async UniTask<bool> ShowConfirmAsync(string message, CancellationToken ct)
{
    var confirmed = false;
    var tcs = new UniTaskCompletionSource();

    // ✅ CancellationTokenRegistration 반드시 Dispose
    var ctr = ct.CanBeCanceled ? ct.Register(() => tcs.TrySetCanceled()) : default;

    var confirmSub = _view.OnConfirmClick.Subscribe(_ => { confirmed = true; tcs.TrySetResult(); });
    var cancelSub = _view.OnCancelClick.Subscribe(_ => { confirmed = false; tcs.TrySetResult(); });

    try { await tcs.Task; }
    catch (OperationCanceledException) { confirmed = false; }
    finally
    {
        ctr.Dispose();         // ✅ 등록 해제
        confirmSub.Dispose();
        cancelSub.Dispose();
        // ✅ 취소된 ct가 아닌 CancellationToken.None으로 닫기
        await _animatedPanel.HideAsync(CancellationToken.None);
    }

    return confirmed;
}
```

### ❌ 금지

```csharp
// ❌ async void 금지 → async UniTaskVoid 또는 UniTask.UnityAction()
// ❌ UniTask 두 번 await 금지 (풀 반환됨)
// ❌ System.Progress<T> 금지 → Cysharp.Threading.Tasks.Progress.Create<float>()
```

---

## UnityScreenNavigator 사용 규칙

### 화면 분류

| 타입 | Container | 스택 | 메서드 |
|---|---|---|---|
| Page | PageContainer | Push/Pop 히스토리 | Push(resourceKey) / Pop() |
| Modal | ModalContainer | Push/Pop, 배경 차단 | Push(resourceKey) / Pop() |
| Sheet | SheetContainer | 히스토리 없음 | ShowByResourceKey() / Show(sheetId) |

### ⚠️ 실전 함정 (검증됨)

```csharp
// ❌ Push/Pop의 AsyncProcessHandle을 무시하면 CS4014 경고 + 타이밍 문제
_modalContainer.Push("Modal", true); // 경고! 애니메이션 완료 전에 다음 코드 실행

// ✅ AsyncProcessHandle.Task를 await하여 애니메이션 완료 보장
var pushHandle = _modalContainer.Push("Modal", true, onLoad: args => { ... });
await pushHandle.Task;

// ... 버튼 대기 ...

var popHandle = _modalContainer.Pop(true);
await popHandle.Task;
```

### 프리팹 위치

USN은 기본적으로 `Resources.Load(resourceKey)`로 프리팹 로드.
프리팹은 `Resources/` 폴더에 배치해야 함.
Addressables 연동 시 커스텀 `IAssetLoader` 구현 필요.

---

## DOTween UI 패턴

```csharp
// 패널 열기 (Scale + Fade)
var seq = DOTween.Sequence()
    .Join(transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack))
    .Join(canvasGroup.DOFade(1f, 0.2f));

// ✅ UniTask await 가능
await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);

// ✅ 시퀀스 정리 — OnDestroy에서 Kill
private void OnDestroy() { _currentSequence?.Kill(); }

// ❌ Animator를 UI에 사용하지 말 것 — 매 프레임 Canvas dirty
```

---

## Canvas 레이어 규칙

| 레이어 | Sort Order | 용도 |
|---|---|---|
| World | 0 | 월드 스페이스 UI |
| HUD | 10 | 항상 표시 |
| Screens | 20 | Page/Sheet 콘텐츠 |
| Modals | 30 | 모달 다이얼로그 |
| Toast | 35 | 토스트 알림 |
| Overlay | 40 | 로딩, 페이드 |
| Debug | 100 | 디버그 오버레이 |

---

## 에셋 관리 규칙

### Addressables

```csharp
// ✅ ScopedAssetLoader — Dispose 시 자동 Release
public class ScopedAssetLoader : IDisposable
{
    private readonly List<AsyncOperationHandle> _handles = new();

    public async UniTask<T> LoadAsync<T>(string key, CancellationToken ct) where T : class
    {
        var handle = Addressables.LoadAssetAsync<T>(key);
        _handles.Add(handle);
        return await handle.Task.AsUniTask().AttachExternalCancellation(ct);
    }

    public void Dispose()
    {
        foreach (var h in _handles) if (h.IsValid()) Addressables.Release(h);
        _handles.Clear();
    }
}

// VContainer에 Scoped로 등록 → LifetimeScope Dispose 시 자동 해제
builder.Register<ScopedAssetLoader>(Lifetime.Scoped);
```

### SpriteAtlas

```csharp
// ✅ SpriteAtlas를 Addressable로 마킹 (개별 스프라이트 아님)
// ✅ 아틀라스 단위 로드/해제
var handle = Addressables.LoadAssetAsync<SpriteAtlas>("AtlasKey");
var atlas = await handle.Task;
var sprites = new Sprite[atlas.spriteCount];
atlas.GetSprites(sprites);

// ❌ Resources.Load 사용 금지
```

---

## 기타 규칙

### 입력
- `UnityEngine.Input` 사용 금지 — New Input System만
- EventSystem: `InputSystemUIInputModule` 사용 (StandaloneInputModule 제거)
- 게임패드/키보드 네비게이션: `Selectable.navigation` 설정 필수

### 폰트
- TMP 기본 폰트: **Interop-Regular SDF** (한글 지원)
- LiberationSans SDF는 한글 미지원 → 사용 금지

### 패키지 설치
- Git URL 우선 (Asset Store 전용 제외)
- R3는 NuGetForUnity + R3.Unity Git URL 이중 설치 필요
- R3 NuGet 의존성: Microsoft.Bcl.TimeProvider, Microsoft.Bcl.AsyncInterfaces 수동 설치 필요할 수 있음

### 테마/접근성
- ThemeService: ReactiveProperty로 테마 상태 관리, uPalette 팔레트 전환
- AccessibilityService: 폰트 스케일(0.75~2.0), 색약 모드 토글
- 폰트 스케일은 `transform.localScale`로 적용 (fontSize 직접 변경은 레이아웃 깨짐)
