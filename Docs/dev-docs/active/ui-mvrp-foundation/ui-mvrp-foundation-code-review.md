# Code Review: UI_Study 01-MVRP-Foundation

Last Updated: 2026-03-27

Reviewed by: Claude Code (claude-sonnet-4-6)
Scope: `UI_Study/Assets/_Study/01-MVRP-Foundation/Scripts/` — 16 C# files

---

## Executive Summary

The MV(R)P Foundation layer is fundamentally sound. The three-layer separation (Model / View / Presenter) is correctly applied across all files, VContainer registrations are idiomatic, and the VContainer lifecycle feedback (`Construct()`에서 Subscribe 금지) is already honored everywhere. The majority of best-practice rules are followed well.

However, five issues deserve attention before this pattern is replicated into Project_Sun production code. Two of those issues are **critical** from a correctness/safety standpoint, two are **important** improvements, and several are **minor** polish items.

---

## Critical Issues (must fix)

### C-1. `DialogService.ShowConfirmAsync` — CancellationToken race condition on close path

**File:** `Scripts/Services/DialogService.cs`, lines 72-75

```csharp
// 닫기
if (_animatedPanel != null)
    await _animatedPanel.HideAsync(ct);   // <-- uses the SAME ct
else
    _dialogView.Hide();
```

**Problem:** The `ct` that was registered to cancel the `UniTaskCompletionSource` on line 48 is the same token passed into `HideAsync`. If the caller cancels (`ct` fires), `tcs.TrySetCanceled()` is called, the `await tcs.Task` throws `OperationCanceledException`, control jumps to `finally`, and then `HideAsync(ct)` is called with a token **that is already cancelled**. `HideAsync` will immediately throw again (or silently skip the animation), leaving the dialog visible and in an undefined state. The dialog never closes on cancellation.

**Fix:** Use `CancellationToken.None` (or a separate linked source) for the hide path inside `finally`, so the panel is always closed regardless of cancellation state:

```csharp
finally
{
    confirmSub.Dispose();
    cancelSub.Dispose();

    if (_animatedPanel != null)
        await _animatedPanel.HideAsync(CancellationToken.None); // always close
    else
        _dialogView.Hide();
}
```

---

### C-2. `DialogService.ShowConfirmAsync` — Subscription leak when `ct` cancels before buttons are clicked

**File:** `Scripts/Services/DialogService.cs`, lines 50-76

```csharp
ct.Register(() => tcs.TrySetCanceled());

var confirmSub = _dialogView.OnConfirmClick.Subscribe(...);
var cancelSub  = _dialogView.OnCancelClick.Subscribe(...);

try { await tcs.Task; }
finally { confirmSub.Dispose(); cancelSub.Dispose(); ... }
```

**Problem 1 — `ct.Register` returns `CancellationTokenRegistration` which is never disposed.** Every call to `ShowConfirmAsync` registers a callback on the external `ct` without cleanup. If `ct` is long-lived (e.g., the `_cts` in `ResourceWithDialogPresenter`), each dialog invocation adds one more callback that is never removed, growing the list permanently.

**Problem 2 — `OperationCanceledException` propagation.** When `ct` cancels, `tcs.TrySetCanceled()` fires, `await tcs.Task` throws `OperationCanceledException`, and the `finally` runs. But the exception propagates up to `SubscribeAwait`'s lambda in `ResourceWithDialogPresenter`. Since `SubscribeAwait` with `AwaitOperation.Drop` does **not** automatically swallow `OperationCanceledException`, this will log an unhandled exception in the Unity console every time the presenter is disposed mid-dialog. The caller should either catch `OperationCanceledException` or `DialogService` should handle it internally.

**Fix:**

```csharp
using var ctReg = ct.Register(() => tcs.TrySetCanceled());

var confirmSub = _dialogView.OnConfirmClick.Subscribe(...);
var cancelSub  = _dialogView.OnCancelClick.Subscribe(...);

try
{
    await tcs.Task;
}
catch (OperationCanceledException)
{
    // Swallow — caller's ct was cancelled, hide and return false
}
finally
{
    confirmSub.Dispose();
    cancelSub.Dispose();
    if (_animatedPanel != null)
        await _animatedPanel.HideAsync(CancellationToken.None);
    else
        _dialogView.Hide();
}

return confirmed; // false by default on cancel
```

---

## Important Improvements (should fix)

### I-1. `AnimatedPanelView.ShowAsync` / `HideAsync` — Cancellation does not close the panel cleanly

**File:** `Scripts/Views/AnimatedPanelView.cs`, lines 45-46 and 63-64

```csharp
await _currentSequence.AsyncWaitForCompletion().AsUniTask()
    .AttachExternalCancellation(ct);
```

**Problem:** `AttachExternalCancellation` cancels the `UniTask` awaiter when `ct` fires, but it does **not** kill the underlying DOTween `Sequence`. The sequence continues running, completes its `OnComplete` callback (which sets `interactable = true` or calls `SetActive(false)`), and the object is left in an unpredictable state after the caller has already moved on.

The correct pattern is `WithCancellation(ct)` applied to a `UniTask` that is also wired to kill the sequence:

```csharp
_currentSequence = DOTween.Sequence()
    .Join(...)
    .OnComplete(() => _canvasGroup.interactable = true)
    .SetLink(gameObject); // auto-kill when GameObject is destroyed

await _currentSequence
    .ToUniTask(cancellationToken: ct); // kills tween on cancel
```

Alternatively, register a cancellation callback:

```csharp
ct.Register(KillCurrentSequence);
```

`AttachExternalCancellation` is the right tool when the underlying work **can be safely abandoned mid-way** — DOTween animations typically cannot.

---

### I-2. `ResourceWithDialogPresenter` — `_cts` is created but its token is never passed to `ShowConfirmAsync`

**File:** `Scripts/Presenters/ResourceWithDialogPresenter.cs`, lines 22, 78-87

```csharp
private readonly CancellationTokenSource _cts = new();

// ...
_view.OnSpendGoldClick
    .SubscribeAwait(async (_, ct) =>      // <-- ct comes from SubscribeAwait's own token
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            $"Gold {GoldCost}을 소비하시겠습니까?", ct);
    }, AwaitOperation.Drop, configureAwait: false)
    .AddTo(_disposables);
```

The `_cts` field is declared and disposed in `Dispose()`, but its token is **never used anywhere** — it is dead code. The `ct` variable inside `SubscribeAwait`'s lambda is the per-operation token provided by R3, not `_cts.Token`. This means:

1. `_cts` provides no actual cancellation link to the presenter's lifetime.
2. Dead fields are confusing to future readers who may assume `_cts` controls the dialog lifecycle.

**Fix (option A — remove `_cts` entirely):** The R3-provided `ct` in `SubscribeAwait` is already cancelled when the subscription is disposed (when `_disposables.Dispose()` runs in `Dispose()`). That is sufficient.

**Fix (option B — use `_cts` properly):** If you want an explicit presenter-lifetime cancel, link them:

```csharp
// In Initialize():
using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
var confirmed = await _dialogService.ShowConfirmAsync("...", linked.Token);
```

Option A is preferred for clarity at this learning stage.

---

## Minor Suggestions (nice to have)

### M-1. `DialogLifetimeScope` and `ResourceLifetimeScope` are missing XML summary comments

`CounterLifetimeScope` and `StudyLifetimeScope` have `<summary>` comments. The other two LifetimeScope files are missing them. Consistency matters when these files serve as learning references.

---

### M-2. `ConfirmDialogView` comment is forward-referencing a "Step 5" that is already implemented

**File:** `Scripts/Views/ConfirmDialogView.cs`, line 11

```csharp
/// 표시/숨김은 SetActive로 제어 (Step 5에서 애니메이션으로 교체).
```

`AnimatedPanelView` (Step 5) is already written. The comment implies the view still uses `SetActive` as a placeholder, but `DialogService` already conditionally uses `AnimatedPanelView` when present. The comment should be updated to reflect the actual current state (the view uses `SetActive` directly; animation is applied externally via `AnimatedPanelView` component).

---

### M-3. `ResourceHUDPresenter` — magic numbers duplicated verbatim in `ResourceWithDialogPresenter`

Both presenters declare the same four constants:

```csharp
private const int GoldPerClick = 10;
private const int WoodPerClick = 5;
private const int GoldCost = 25;
private const int WoodCost = 15;
```

For a study project this is acceptable, but when graduating to `Project_Sun`, these should live in `GameConfig` (which already exists and is injected by VContainer) to avoid divergence.

---

### M-4. `CounterView` — unused `using R3.Triggers`

**File:** `Scripts/Views/CounterView.cs`, line 2

```csharp
using R3.Triggers;
```

`OnClickAsObservable()` is from `R3` (via the `Button.OnClickAsObservable()` extension), not from `R3.Triggers`. This unused import adds noise and may cause confusion about which extension provides which API. Remove it.

---

### M-5. `SimpleService.Start()` logs to console only — consider removing or making conditional

`SimpleService` exists purely to demonstrate DI injection at Step 1. Its `Debug.Log` in `Start()` will fire on every domain reload in the Editor. For a production pattern reference, wrapping with `#if UNITY_EDITOR` or removing the service when progressing to later steps keeps the console clean.

---

### M-6. `DialogService.Dispose()` is an empty body

```csharp
public void Dispose()
{
}
```

The service holds references to `ConfirmDialogView` and `AnimatedPanelView`. While those are MonoBehaviours (Unity manages their lifetime), the empty `Dispose()` body is misleading — a future developer might add disposable resources inside and forget to clean them. Either populate it with a comment explaining why nothing needs disposing, or seal the class if no extension is planned.

---

## Architecture Considerations

### A-1. `DialogService` is a Pure C# class that calls `GetComponent<AnimatedPanelView>()` in its constructor

```csharp
public DialogService(ConfirmDialogView dialogView)
{
    _dialogView = dialogView;
    _animatedPanel = dialogView.GetComponent<AnimatedPanelView>();
```

This is the only place in the entire codebase where a non-View class directly queries `GetComponent`. It works, but it couples `DialogService`'s behavior to a Unity-specific runtime discovery. A cleaner pattern for `Project_Sun` would be to inject `AnimatedPanelView` separately (as a nullable second parameter) and register it conditionally in the LifetimeScope — making the dependency graph explicit:

```csharp
// DialogLifetimeScope.cs
builder.RegisterComponent(_confirmDialogView);
builder.RegisterComponentInHierarchy<AnimatedPanelView>(); // optional
builder.Register<DialogService>(Lifetime.Singleton);

// DialogService constructor
public DialogService(ConfirmDialogView dialogView, AnimatedPanelView animatedPanel = null)
```

For this learning step the current approach is acceptable; just document the intent to change it before Project_Sun adoption.

---

### A-2. LifetimeScope hierarchy is flat — parent-child scope strategy not yet demonstrated

The README mentions `Root → Scene → Panel/Popup` scope hierarchy and `CreateChildFromPrefab()` as the recommended approach for Project_Sun. None of the four LifetimeScopes in this module use `Parent` scope references. This is fine for a foundation module, but the next learning step should explicitly demonstrate scope chaining so the learner understands how `ResourceModel` (registered in a scene scope) becomes accessible to a dialog popup scope without re-registration.

---

### A-3. `AwaitOperation.Drop` is the correct choice here — worth documenting why

`ResourceWithDialogPresenter` correctly uses `AwaitOperation.Drop` to prevent re-entrant dialog stacking. This is the most important `SubscribeAwait` design decision in the entire codebase. Consider adding an inline comment explaining what `Drop` does (ignores new button clicks while a dialog is open) vs the alternatives (`Sequential` queues them, `Parallel` opens multiple dialogs). This makes the pattern pedagogically complete.

---

## Next Steps

Priority order for fixes:

1. **C-1** — Fix `HideAsync(ct)` → `HideAsync(CancellationToken.None)` in `DialogService.finally` block. One line change, high safety impact.
2. **C-2** — Fix `ct.Register` leak and add `OperationCanceledException` catch in `ShowConfirmAsync`. Prevents accumulating callbacks and console spam on scene unload.
3. **I-1** — Replace `AttachExternalCancellation` with proper DOTween cancellation in `AnimatedPanelView`. Prevents animation orphans on panel transitions.
4. **I-2** — Remove dead `_cts` field from `ResourceWithDialogPresenter`. Reduces confusion before this pattern is copied to `Project_Sun`.
5. **M-4** — Remove unused `using R3.Triggers` from `CounterView`.
6. **M-1** / **M-2** — Update missing/stale comments in `DialogLifetimeScope`, `ResourceLifetimeScope`, `ConfirmDialogView`.

Issues M-3, M-5, M-6, A-1, A-2, A-3 are noted for the Project_Sun integration phase rather than immediate action.
