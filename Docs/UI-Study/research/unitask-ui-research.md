# UniTask UI Research Report
*Date: 2026-03-27 | Model: Claude Sonnet 4.6*

---

## Executive Summary

UniTask (Cysharp/UniTask) is the de-facto standard async/await library for Unity, offering zero-allocation task primitives, full PlayerLoop integration, and rich UI-specific APIs that coroutines and native `Task<T>` cannot match. As of October 2024 (v2.5.10), it is actively maintained and recommended for all Unity versions from 2018.4 onward. For UI work specifically — dialogs, screen transitions, progress bars, button sequencing — UniTask's `UniTaskCompletionSource`, `GetCancellationTokenOnDestroy`, and async enumerable patterns provide clean, coroutine-free solutions that integrate naturally with both R3 and VContainer.

---

## 1. Current Version and Maintenance Status

| Item | Detail |
|------|--------|
| Latest stable | **v2.5.10** (October 3, 2024) |
| Previous notable | v2.5.9 — added `AsyncInstantiateOperation` support (Unity 2022.3.20+ / 2023.3+) |
| Minimum Unity | Unity 2018.4.13f1 (requires C# 7.0 task-like custom builder) |
| License | MIT |
| Maintenance | Actively maintained by Cysharp (Yoshifumi Kawai / neuecc) |
| Install | UPM git URL or OpenUPM (`com.cysharp.unitask`) |

Recent additions worth noting:
- `UniTask.WhenEach` added in v2.5.8
- `WaitUntil` / `WaitWhile` / `Defer` overloads in v2.5.7
- `AsyncInstantiateOperation` cancellation support in v2.5.9

---

## 2. UniTask vs Native C# async/await vs Unity Awaitable

### The Three Options

| | `Task<T>` | `UniTask<T>` | `Awaitable` (Unity 6+) |
|--|--|--|--|
| Allocation | High (heap class) | Near-zero (struct) | Low (pooled object) |
| GC Pressure | High | Minimal | Low |
| PlayerLoop integration | None | Full | Full |
| `WhenAll` / `WhenAny` | Yes | Yes | No |
| `DelayFrame` | No | Yes | No |
| uGUI event awaiting | No | Yes | No |
| R3 / VContainer integration | No | Yes | No |
| Debug tracker window | No | Yes | No |
| External dependency | No | Yes | No |

### When to Use Which

**Use UniTask** for all application/game code. It is a superset of both. Awaitable's own designer, Unity, acknowledges UniTask as the inspiration and recommends it for production apps.

**Use `Awaitable`** only when writing a reusable package/library where you want zero external dependencies. You can still adapt it to UniTask in calling code via `awaitable.AsUniTask()`.

**Avoid `Task<T>`** for anything that runs on the Unity main thread. It creates heap garbage, has no PlayerLoop awareness, and carries `SynchronizationContext` overhead that UniTask deliberately removes.

**Avoid native `async void`** entirely. Use `async UniTaskVoid` instead.

### Performance Summary

- UniTask achieves ~10x lower total memory load versus `Task<T>` in profiler comparisons
- Zero GC allocation in release builds (async state machines compile to structs, not classes)
- Debug builds still allocate (state machines are classes there — expected, same as standard C#)
- UniTask.Yield() timing matches coroutines, with significantly less GC

---

## 3. UniTask for UI: Core Patterns

### 3.1 Async Button Click (direct await)

```csharp
// Await a single click - resolves after first click
await button.OnClickAsync(this.GetCancellationTokenOnDestroy());

// Async handler registered to onClick via UniTask.UnityAction
button.onClick.AddListener(UniTask.UnityAction(async () =>
{
    button.interactable = false;
    await DoSomethingAsync(this.GetCancellationTokenOnDestroy());
    button.interactable = true;
}));
```

**Never use `async void` for button listeners.** `UniTask.UnityAction` wraps an `async UniTaskVoid` lambda that routes exceptions to `UniTaskScheduler.UnobservedTaskException`.

### 3.2 Async uGUI Events (AsyncEnumerable stream)

```csharp
// Pull-based: process every other click using LINQ
await foreach (var _ in button.OnClickAsAsyncEnumerable()
    .Where((_, i) => i % 2 == 0)
    .WithCancellation(this.GetCancellationTokenOnDestroy()))
{
    Debug.Log("Even click");
}
```

### 3.3 Screen Transitions (sequential + parallel)

```csharp
// Sequential fade-out → load → fade-in
async UniTask TransitionToScene(string sceneName, CancellationToken ct)
{
    await fadePanel.FadeInAsync(ct);                          // wait for cover
    await SceneManager.LoadSceneAsync(sceneName).WithCancellation(ct);
    await fadePanel.FadeOutAsync(ct);                         // reveal new scene
}

// Parallel asset loads before activating scene
var (sprite, config, audio) = await UniTask.WhenAll(
    LoadAsSprite("ui/icon", ct),
    LoadConfig("settings", ct),
    LoadAudio("bgm/main", ct));
```

### 3.4 Loading/Progress Indicators

```csharp
async UniTask LoadSceneWithProgress(string sceneName, Slider progressBar, CancellationToken ct)
{
    var op = SceneManager.LoadSceneAsync(sceneName);
    op.allowSceneActivation = false;

    // Use Cysharp's Progress.Create (not System.Progress<T>) — avoids allocation
    await op.ToUniTask(
        Progress.Create<float>(p => progressBar.value = p),
        cancellationToken: ct);

    op.allowSceneActivation = true;
}

// Web request with progress
async UniTask DownloadFile(string url, Slider bar, CancellationToken ct)
{
    var req = UnityWebRequest.Get(url);
    await req.SendWebRequest().ToUniTask(
        Progress.Create<float>(p => bar.value = p),
        cancellationToken: ct);
}
```

**Important:** Use `Cysharp.Threading.Tasks.Progress.Create<float>()`, not `System.Progress<T>`. The system version allocates a closure on every report.

---

## 4. UniTask + R3 Integration

Both are Cysharp libraries and are designed to interoperate. The division of responsibility is:

| Use R3 | Use UniTask |
|--------|------------|
| Multi-value streams (ongoing events) | One-shot async operations |
| Property binding / state propagation | Sequenced operations with await |
| Filtering / debouncing / throttling event streams | Loading, waiting for a result |
| `ObserveEveryValueChanged`, `OnClickAsObservable` | `UniTaskCompletionSource` dialog wait |

### 4.1 Converting R3 Observable to UniTask

```csharp
// Wait for the first value emitted
var result = await observable.FirstAsync(cancellationToken: ct);

// Consume all values sequentially
await observable
    .TakeUntil(ct)
    .ForEachAsync(x => Process(x), ct);
```

### 4.2 SubscribeAwait — async work per event

```csharp
// Sequential: each async handler completes before the next starts
button.OnClickAsObservable()
    .SubscribeAwait(async (_, ct) =>
    {
        await DoHeavyWorkAsync(ct);
    }, AwaitOperation.Sequential, configureAwait: false)
    .AddTo(destroyCancellationToken);
```

**Warning:** When `SubscribeAwait` is used with `AwaitOperation.Sequential`, subsequent iterations may resume off the main thread. Pass `configureAwait: false` to stay on the Unity main thread (R3 issue #99).

### 4.3 R3 Operator + UniTask Completion

```csharp
// Debounce search input, then fire async query
searchField.OnValueChangedAsObservable()
    .DebounceFrame(10)
    .SubscribeAwait(async (query, ct) =>
    {
        var results = await SearchDatabase(query, ct);
        UpdateResultsUI(results);
    }, AwaitOperation.Drop)  // Drop: ignore new events while one is in flight
    .AddTo(destroyCancellationToken);
```

`AwaitOperation` modes:
- `Sequential` — queue events, process one at a time
- `Drop` — ignore new events while handler is running (good for search)
- `Switch` — cancel current handler on new event (good for autocomplete)
- `Parallel` — run all concurrently (use with care for UI)

---

## 5. UniTask + VContainer Integration

When `com.cysharp.unitask` is present, VContainer auto-enables `VCONTAINER_UNITASK_INTEGRATION`, which makes `IAsyncStartable.StartAsync` return `UniTask` instead of `Task`.

### 5.1 IAsyncStartable — Async Initialization

```csharp
public class UIBootstrapper : IAsyncStartable
{
    readonly ISceneLoader _sceneLoader;
    readonly LoadingScreen _loadingScreen;

    public UIBootstrapper(ISceneLoader sceneLoader, LoadingScreen loadingScreen)
    {
        _sceneLoader = sceneLoader;
        _loadingScreen = loadingScreen;
    }

    public async UniTask StartAsync(CancellationToken cancellation)
    {
        _loadingScreen.Show();
        await _sceneLoader.LoadAsync("GameScene", cancellation);
        _loadingScreen.Hide();
    }
}

// Registration
builder.RegisterEntryPoint<UIBootstrapper>();
```

**Critical:** The `CancellationToken` passed to `StartAsync` cancels when the `LifetimeScope` is destroyed. This is the correct place to tie loading to scene lifetime.

### 5.2 Lifecycle Timing

All `StartAsync` calls are fired simultaneously on the main thread at VContainer's startup phase. They do not block subsequent PlayerLoop phases. To run code at a specific PlayerLoop phase:

```csharp
public async UniTask StartAsync(CancellationToken ct)
{
    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, ct);
    // Now executing in PostLateUpdate
}
```

### 5.3 VContainer + Async Exception Handling

```csharp
// Per-scope handler
builder.RegisterEntryPointExceptionHandler(ex =>
{
    Debug.LogException(ex);
    SceneManager.LoadScene("ErrorScene");
});

// Global fallback
UniTaskScheduler.UnobservedTaskException += ex =>
{
    Debug.LogError($"Unobserved UniTask exception: {ex}");
};
```

### 5.4 The VContainer Construct() / Subscribe() Warning

**Do not call R3 `Subscribe()` inside VContainer's `Construct()` (constructor injection).** `Construct()` runs before `Awake()`, so `MonoBehaviour` references may not be initialized. Defer subscriptions to `IStartable.Start()` or `IAsyncStartable.StartAsync()`.

---

## 6. Async Dialog Pattern (Await User Response)

This is the most important UI pattern UniTask enables that coroutines make awkward.

### 6.1 UniTaskCompletionSource Dialog

```csharp
public class ConfirmDialog : MonoBehaviour
{
    [SerializeField] Button _confirmBtn;
    [SerializeField] Button _cancelBtn;

    UniTaskCompletionSource<bool> _tcs;

    public UniTask<bool> ShowAsync(CancellationToken ct)
    {
        gameObject.SetActive(true);
        _tcs = new UniTaskCompletionSource<bool>();

        // Cancel if the dialog is destroyed mid-wait
        ct.RegisterWithoutCaptureExecutionContext(() =>
            _tcs.TrySetCanceled());

        return _tcs.Task;
    }

    void OnConfirmClicked() => _tcs.TrySetResult(true);
    void OnCancelClicked()  => _tcs.TrySetResult(false);

    void Awake()
    {
        _confirmBtn.onClick.AddListener(OnConfirmClicked);
        _cancelBtn.onClick.AddListener(OnCancelClicked);
    }
}

// Caller:
var confirmed = await dialog.ShowAsync(this.GetCancellationTokenOnDestroy());
if (confirmed) ProceedWithAction();
```

### 6.2 Enum Result Dialog

```csharp
public enum DialogResult { Confirm, Cancel, Timeout }

public async UniTask<DialogResult> ShowWithTimeoutAsync(float seconds, CancellationToken ct)
{
    var tcs = new UniTaskCompletionSource<DialogResult>();

    var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct)
        .ContinueWith(() => tcs.TrySetResult(DialogResult.Timeout));

    gameObject.SetActive(true);
    return await tcs.Task;
}
```

### 6.3 WhenAny for Multiple Concurrent Buttons

```csharp
// For simple cases: race two one-shot click awaits
var (index, _, _) = await UniTask.WhenAny(
    confirmButton.OnClickAsync(cts.Token),
    cancelButton.OnClickAsync(cts.Token),
    UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: cts.Token));

// index == 0: confirmed, 1: cancelled, 2: timed out
```

**Note:** `WhenAny` does not auto-cancel the losing tasks. The shared `cts` above handles cleanup.

---

## 7. Sequential UI Animations

```csharp
// Sequential: each step waits for the previous
async UniTask PlayOpenAnimation(CancellationToken ct)
{
    await panel.DOFade(1f, 0.3f).WithCancellation(ct);
    await title.DOLocalMoveY(0f, 0.2f).WithCancellation(ct);
    await buttons.DOFade(1f, 0.15f).WithCancellation(ct);
}

// Parallel: all start at once, wait for all to finish
async UniTask PlayCloseAnimation(CancellationToken ct)
{
    await UniTask.WhenAll(
        panel.DOFade(0f, 0.25f).WithCancellation(ct),
        title.DOLocalMoveY(-50f, 0.2f).WithCancellation(ct));
}

// Combined: sequential sections with internal parallelism
async UniTask TransitionOut(CancellationToken ct)
{
    await PlayCloseAnimation(ct);       // parallel internal
    await fadePanel.DOFade(1f, 0.1f).WithCancellation(ct);  // then sequential
}
```

---

## 8. Debouncing User Input

### 8.1 Using R3 (preferred for streams)

```csharp
// Debounce on Observable stream (cleanest approach)
inputField.OnValueChangedAsObservable()
    .Debounce(TimeSpan.FromMilliseconds(300))
    .SubscribeAwait(async (value, ct) =>
    {
        results = await QueryAsync(value, ct);
        UpdateUI(results);
    }, AwaitOperation.Switch)
    .AddTo(destroyCancellationToken);
```

### 8.2 Using UniTask only

```csharp
public class DebounceHelper
{
    CancellationTokenSource _cts;

    public async UniTask Debounce(Func<CancellationToken, UniTask> action,
        int delayMs, CancellationToken lifetimeToken)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);

        try
        {
            await UniTask.Delay(delayMs, cancellationToken: _cts.Token);
            await action(_cts.Token);
        }
        catch (OperationCanceledException) { /* Debounced — expected */ }
    }
}

// Usage in MonoBehaviour:
void OnSearchChanged(string value)
{
    _debounce.Debounce(
        ct => QueryAndRefreshAsync(value, ct),
        300,
        this.GetCancellationTokenOnDestroy()).Forget();
}
```

---

## 9. CancellationToken Patterns for UI Lifecycle

### 9.1 Token Priority by Scope

```csharp
// Unity 2022.2+ — best approach, zero boilerplate
await SomeOperationAsync(this.destroyCancellationToken);

// Older Unity — the standard extension method
await SomeOperationAsync(this.GetCancellationTokenOnDestroy());

// Application shutdown
await SomeOperationAsync(Application.exitCancellationToken);
```

### 9.2 Linked Sources (timeout + destroy + user cancel)

```csharp
async UniTask LoadWithSafeguards(CancellationToken outerToken)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
        outerToken,
        timeout.Token,
        this.destroyCancellationToken);

    try
    {
        await LoadAssetsAsync(linked.Token);
    }
    catch (OperationCanceledException)
    {
        if (timeout.IsCancellationRequested)
            ShowTimeoutError();
        // else: user or scene destroyed — silent
    }
}
```

### 9.3 Cancellation Without Exception (SuppressCancellationThrow)

```csharp
// When you want a bool result instead of exception overhead
var (wasCanceled, _) = await UniTask.Delay(5000, cancellationToken: token)
    .SuppressCancellationThrow();

if (wasCanceled)
    HandleTimeout();
```

### 9.4 Deferred vs Immediate Cancellation

```csharp
// Deferred (default) — checked at next PlayerLoop tick
await UniTask.DelayFrame(100, cancellationToken: token);

// Immediate — cancels synchronously via callback registration
// Use for sub-frame-critical operations only (slight overhead)
await UniTask.DelayFrame(100, cancellationToken: token, cancelImmediately: true);
```

---

## 10. Performance Benefits Over Coroutines

| Metric | Coroutine | UniTask |
|--------|-----------|---------|
| Allocation per operation | ~100 bytes (IEnumerator box + StateMachine) | ~0 bytes (release build struct) |
| Exception handling | Not supported | Full try/catch/finally |
| Return values | Not supported (requires callbacks) | Native `async UniTask<T>` |
| Cancellation | Implicit (StopCoroutine) | Explicit CancellationToken |
| Composition (WhenAll) | Awkward, manual | `UniTask.WhenAll()` |
| Debugging | Stack trace poor | UniTask Tracker Window |
| Thread support | Main thread only | ThreadPool via `UniTask.RunOnThreadPool()` |

Coroutines do have one edge: they are marginally cheaper for the simplest single-frame-yield loops because they have no async state machine at all. But for any UI-complexity operation (loading, dialogs, transitions), UniTask wins significantly.

---

## 11. Gotchas and Anti-Patterns

### Double-Await (Critical)

```csharp
// WRONG — throws InvalidOperationException
var task = UniTask.DelayFrame(10);
await task;
await task; // CRASH — task already returned to pool

// CORRECT — use UniTask.Lazy for multi-await
var lazy = UniTask.Lazy(() => SomeExpensiveAsync());
await lazy; // safe
await lazy; // also safe
```

### async void in Button Handlers

```csharp
// WRONG — exceptions silently swallowed by Unity runtime
button.onClick.AddListener(async void () => { await DoStuff(); });

// CORRECT — exceptions go to UniTaskScheduler.UnobservedTaskException
button.onClick.AddListener(UniTask.UnityAction(async () => { await DoStuff(); }));

// ALSO CORRECT — explicit UniTaskVoid method
button.onClick.AddListener(() => HandleClickAsync().Forget());
async UniTaskVoid HandleClickAsync() { await DoStuff(); }
```

### Forget() Without Handler

```csharp
// RISKY — exceptions discarded silently
SomeAsync().Forget();

// BETTER — set global handler first, then Forget() is safer
UniTaskScheduler.UnobservedTaskException += ex => Debug.LogException(ex);
SomeAsync().Forget();

// BEST for fire-and-forget — use UniTaskVoid return type
async UniTaskVoid FireAndForget() { await SomeAsync(); }
FireAndForget().Forget();
```

### Not Canceling on OnDisable vs OnDestroy

```csharp
// If a screen is disabled (not destroyed), GetCancellationTokenOnDestroy won't trigger.
// Create separate tokens for disable and destroy if needed:
CancellationTokenSource _disableCts;
CancellationTokenSource _destroyCts = new();

void OnEnable()
{
    _disableCts = CancellationTokenSource.CreateLinkedTokenSource(
        _destroyCts.Token);
}

void OnDisable()
{
    _disableCts?.Cancel();
    _disableCts?.Dispose();
}

void OnDestroy()
{
    _destroyCts?.Cancel();
    _destroyCts?.Dispose();
}
```

### R3 SubscribeAwait Main Thread Loss

```csharp
// WRONG — may resume on background thread after await
observable.SubscribeAwait(async (val, ct) =>
{
    await SomeAsync(ct);
    label.text = val.ToString(); // NullRef or cross-thread error
});

// CORRECT — configureAwait: false keeps Unity main thread
observable.SubscribeAwait(async (val, ct) =>
{
    await SomeAsync(ct);
    label.text = val.ToString(); // safe
}, AwaitOperation.Sequential, configureAwait: false);
```

### VContainer Construct() Subscribe (Project-Specific Warning)

```csharp
// WRONG — Construct() runs before Awake(), MonoBehaviour not ready
public class UIController
{
    [Inject]
    public void Construct(ReactiveProperty<int> score)
    {
        score.Subscribe(UpdateScoreLabel); // deadlock / null MonoBehaviour
    }
}

// CORRECT — defer to IStartable or IAsyncStartable
public class UIController : IStartable
{
    ReactiveProperty<int> _score;

    [Inject]
    public void Construct(ReactiveProperty<int> score) => _score = score;

    public void Start()
    {
        _score.Subscribe(UpdateScoreLabel).AddTo(destroyCancellationToken);
    }
}
```

### OperationCanceledException Leaking Through Exception Filters

```csharp
// WRONG — catches OperationCanceledException and hides it
try { await SomeAsync(ct); }
catch (Exception ex)
{
    HandleError(ex); // This will swallow cancellation
}

// CORRECT — let cancellation propagate
try { await SomeAsync(ct); }
catch (OperationCanceledException) { throw; }  // Re-throw cancel
catch (Exception ex)
{
    HandleError(ex);
}

// OR — using exception filter
catch (Exception ex) when (ex is not OperationCanceledException)
{
    HandleError(ex);
}
```

---

## 12. Installation and Setup

```
// Package Manager → Add package from git URL:
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask

// Or OpenUPM:
openupm add com.cysharp.unitask
```

Enable the UniTask Tracker Window: `Window > UniTask Tracker` — invaluable for debugging leaked tasks.

---

## Sources and References

- [Cysharp/UniTask GitHub](https://github.com/Cysharp/UniTask)
- [UniTask Releases](https://github.com/Cysharp/UniTask/releases)
- [UniTask Official Documentation](https://cysharp.github.io/UniTask/)
- [OpenUPM Package Page](https://openupm.com/packages/com.cysharp.unitask/)
- [UniTask v2 by neuecc (Medium)](https://neuecc.medium.com/unitask-v2-zero-allocation-async-await-for-unity-with-asynchronous-linq-1aa9c96aa7dd)
- [Task vs UniTask vs Awaitable Performance Comparison](https://medium.com/@DanielMcRon/task-vs-unitask-vs-awaitable-performance-and-api-comparison-is-awaitable-a-new-leader-0e4904dfb0d4)
- [Benchmarking Async/Await, Coroutine, and UniTask](https://prasetion.medium.com/benchmarking-async-await-coroutine-and-unitask-in-unity-which-one-is-best-1-59beec0fb53a)
- [VContainer + UniTask Integration Docs](https://vcontainer.hadashikick.jp/integrations/unitask)
- [UniTask vs Awaitable — GitHub Discussion #627](https://github.com/Cysharp/UniTask/discussions/627)
- [WhenAny Best Practices — GitHub Discussion #389](https://github.com/Cysharp/UniTask/discussions/389)
- [CancellationToken System — DeepWiki](https://deepwiki.com/Cysharp/UniTask/7.1-cancellationtoken-system)
- [Cysharp/R3 GitHub](https://github.com/Cysharp/R3)
- [R3 SubscribeAwait configureAwait Issue #99](https://github.com/Cysharp/R3/issues/99)
- [UniTask Best Practice Cancellation — Unity Discussions](https://discussions.unity.com/t/unitask-best-practice-of-cancellation-on-demand-and-on-destroy/946985)
- [UniTask Debounce — Unity Discussions](https://discussions.unity.com/t/unitask-based-debounce-method-help/946936)
- [Deep Dive async/await in Unity with UniTask (SlideShare)](https://www.slideshare.net/slideshow/deep-dive-asyncawait-in-unity-with-unitasken/148969393)
- [Performance in Unity: async vs coroutines — LogRocket](https://blog.logrocket.com/performance-unity-async-await-tasks-coroutines-c-job-system-burst-compiler/)
