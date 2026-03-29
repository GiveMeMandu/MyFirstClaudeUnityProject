# UI Toolkit + UniTask + DOTween 실전 패턴

- **작성일**: 2026-03-29
- **카테고리**: practice
- **상태**: 조사완료

---

## 1. 요약

UniTask와 UI Toolkit의 공식 확장 메서드는 PR #338 stale 처리로 존재하지 않으므로, `UniTaskCompletionSource`와 수동 `clicked` 이벤트 연결로 async 다이얼로그를 구현해야 한다. DOTween은 UI Toolkit VisualElement에 대한 직접 확장 메서드를 일부 제공하지만(`DOMove`, `DOScale`), opacity/color/width 같은 스타일 속성은 `DOTween.To()` getter/setter 람다 패턴으로 직접 구현해야 한다. CSS Transition은 hover/상태 전환 같은 단순 선언형 애니메이션에 적합하고, DOTween은 시퀀스/스태거/탄성 같은 절차적 복잡 애니메이션에 적합하다. R3는 프로젝트 전체에 도입하지 않고 디바운스/쓰로틀이 필요한 특정 화면에만 부분 도입이 권장된다.

---

## 2. 상세 분석

### 2.1 UniTask + UI Toolkit Async 패턴

#### UniTask PR #338 현황 및 수동 구현 이유

UniTask GitHub PR #338 (2022년 2월)은 UI Toolkit 버튼 클릭을 `AsyncEnumerable` 스트림으로 노출하는 확장 메서드를 제안했다. 유지보수자(neuecc)가 "UI Toolkit에 충분히 익숙하지 않아 검토할 수 없다"고 밝히며 90일 후 stale 처리로 **미머지**되었다. 따라서 현재(2026-03-29 기준) `button.OnClickAsync()` 같은 공식 API는 UI Toolkit에서 사용할 수 없다. 수동으로 `UniTaskCompletionSource`와 `clicked` 이벤트를 연결해야 한다.

#### 패턴 A: Async 다이얼로그 (사용자 입력 대기)

`UniTaskCompletionSource<T>`는 사용자가 버튼을 클릭할 때까지 await할 수 있게 해주는 핵심 패턴이다.

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 기반 Async 확인 다이얼로그.
/// UGUI의 MonoBehaviour button.onClick과 달리,
/// UI Toolkit은 button.clicked C# event를 사용한다.
/// </summary>
public class ConfirmDialogView : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private VisualElement _root;
    private Label         _messageLabel;
    private Button        _confirmButton;
    private Button        _cancelButton;

    // UniTaskCompletionSource: 사용자 클릭 시까지 await 가능하게 하는 핵심
    private UniTaskCompletionSource<bool> _tcs;

    private void OnEnable()
    {
        _root          = _document.rootVisualElement;
        _messageLabel  = _root.Q<Label>("dialog-message");
        _confirmButton = _root.Q<Button>("confirm-btn");
        _cancelButton  = _root.Q<Button>("cancel-btn");

        // 시작 시 숨김
        _root.style.display = DisplayStyle.None;

        _confirmButton.clicked += OnConfirmClicked;
        _cancelButton.clicked  += OnCancelClicked;
    }

    private void OnDisable()
    {
        if (_confirmButton != null) _confirmButton.clicked -= OnConfirmClicked;
        if (_cancelButton  != null) _cancelButton.clicked  -= OnCancelClicked;

        // 혹시 await 중이면 취소
        _tcs?.TrySetCanceled();
    }

    /// <summary>
    /// 다이얼로그를 표시하고 사용자 응답을 비동기로 기다린다.
    /// true = 확인, false = 취소, OperationCanceledException = ct 취소
    /// </summary>
    public UniTask<bool> ShowAsync(string message, CancellationToken ct = default)
    {
        _messageLabel.text  = message;
        _root.style.display = DisplayStyle.Flex;

        _tcs = new UniTaskCompletionSource<bool>();

        // destroyCancellationToken 또는 외부 ct로 취소 연동
        ct.RegisterWithoutCaptureExecutionContext(() =>
            _tcs.TrySetCanceled());

        return _tcs.Task;
    }

    private void OnConfirmClicked()
    {
        _root.style.display = DisplayStyle.None;
        _tcs?.TrySetResult(true);
    }

    private void OnCancelClicked()
    {
        _root.style.display = DisplayStyle.None;
        _tcs?.TrySetResult(false);
    }
}
```

**호출 측 코드 (Presenter)**

```csharp
public class BuildingPresenter : IDisposable
{
    private readonly BuildingModel       _model;
    private readonly BuildingView        _view;
    private readonly ConfirmDialogView   _dialog;
    private readonly CancellationToken   _ct;

    public BuildingPresenter(BuildingModel model, BuildingView view,
        ConfirmDialogView dialog, MonoBehaviour owner)
    {
        _model  = model;
        _view   = view;
        _dialog = dialog;
        _ct     = owner.destroyCancellationToken;

        _view.OnBuildClicked += () => HandleBuildAsync().Forget();
    }

    private async UniTaskVoid HandleBuildAsync()
    {
        // 확인 다이얼로그 — 사용자 응답을 await
        bool confirmed = await _dialog.ShowAsync(
            "건물을 건설하시겠습니까? (골드 50 소비)", _ct);

        if (!confirmed) return;

        _model.BuildHouse();
        _view.PlayBuildAnimation();
    }

    public void Dispose()
    {
        _view.OnBuildClicked -= () => HandleBuildAsync().Forget();
    }
}
```

#### 패턴 B: WhenAny로 다중 버튼 경쟁

```csharp
/// <summary>
/// 세 버튼 중 하나를 기다리는 패턴.
/// UniTask.WhenAny는 인덱스(어느 것이 먼저 완료됐는지)를 반환한다.
/// </summary>
public async UniTask<int> ShowThreeChoiceAsync(CancellationToken ct)
{
    _root.style.display = DisplayStyle.Flex;

    var tcsList = new UniTaskCompletionSource<int>[3];
    for (int i = 0; i < 3; i++) tcsList[i] = new UniTaskCompletionSource<int>();

    // 각 버튼에 해당 TCS 연결
    Action[] handlers = new Action[3];
    var buttons = new[] { _btn1, _btn2, _btn3 };

    for (int i = 0; i < 3; i++)
    {
        int index = i; // 클로저 캡처 방지
        handlers[i] = () => tcsList[index].TrySetResult(index);
        buttons[i].clicked += handlers[i];
    }

    ct.RegisterWithoutCaptureExecutionContext(() =>
    {
        foreach (var tcs in tcsList) tcs.TrySetCanceled();
    });

    // 세 Task 중 첫 완료를 기다림. 결과: (winnerIndex, result0, result1)
    var (winnerIndex, _, _) = await UniTask.WhenAny(
        tcsList[0].Task,
        tcsList[1].Task,
        tcsList[2].Task);

    // 이벤트 정리
    for (int i = 0; i < 3; i++)
        buttons[i].clicked -= handlers[i];

    _root.style.display = DisplayStyle.None;
    return winnerIndex;
}
```

#### 패턴 C: Async 로딩 화면 + ProgressBar

UI Toolkit의 `ProgressBar` 컨트롤을 UniTask 진행 콜백으로 업데이트한다. `Cysharp.Threading.Tasks.Progress.Create<float>()`를 사용해야 한다 (System.Progress<float>는 클로저 할당 발생).

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoadingScreenView : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private VisualElement _root;
    private ProgressBar   _progressBar;
    private Label         _statusLabel;

    private void OnEnable()
    {
        _root         = _document.rootVisualElement;
        _progressBar  = _root.Q<ProgressBar>("load-progress");
        _statusLabel  = _root.Q<Label>("load-status");

        _progressBar.lowValue  = 0f;
        _progressBar.highValue = 100f;

        _root.style.display = DisplayStyle.None;
    }

    public void Show(string status = "로딩 중...")
    {
        _root.style.display = DisplayStyle.Flex;
        _statusLabel.text   = status;
        _progressBar.value  = 0f;
    }

    public void Hide() => _root.style.display = DisplayStyle.None;

    public void SetProgress(float normalized)
    {
        _progressBar.value = normalized * 100f;
        _statusLabel.text  = $"로딩 중... {normalized * 100f:F0}%";
    }

    /// <summary>씬 로드와 ProgressBar를 연결하는 완전 예제</summary>
    public async UniTask LoadSceneWithProgressAsync(string sceneName, CancellationToken ct)
    {
        Show($"'{sceneName}' 로딩 중...");

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // Cysharp Progress.Create — System.Progress<T>보다 GC 할당 없음
        var progress = Progress.Create<float>(p => SetProgress(p));

        await op.ToUniTask(progress: progress, cancellationToken: ct);

        // 진행률 100% 표시 후 잠깐 대기 (UX 용도)
        SetProgress(1f);
        await UniTask.Delay(300, cancellationToken: ct);

        op.allowSceneActivation = true;
        Hide();
    }
}
```

#### 패턴 D: CancellationToken과 UIDocument 수명 연동

UI Toolkit의 `VisualElement`는 `MonoBehaviour`가 아니므로 `GetCancellationTokenOnDestroy()`를 직접 호출할 수 없다. UIDocument를 보유한 MonoBehaviour의 `destroyCancellationToken`을 활용한다.

```csharp
public class SearchPanelView : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    private TextField _searchField;
    private Label     _resultLabel;

    // OnDisable/OnDestroy 양쪽 토큰 관리 패턴
    private CancellationTokenSource _disableCts;

    private void OnEnable()
    {
        var root     = _document.rootVisualElement;
        _searchField = root.Q<TextField>("search-field");
        _resultLabel = root.Q<Label>("result-label");

        // OnDisable에서 취소되는 토큰 생성
        _disableCts = CancellationTokenSource.CreateLinkedTokenSource(
            destroyCancellationToken);  // OnDestroy에서도 취소

        _searchField.RegisterValueChangedCallback(OnSearchChanged);
    }

    private void OnDisable()
    {
        _disableCts?.Cancel();
        _disableCts?.Dispose();

        if (_searchField != null)
            _searchField.UnregisterValueChangedCallback(OnSearchChanged);
    }

    private void OnSearchChanged(ChangeEvent<string> evt)
    {
        // 검색은 디바운스 필요 — 섹션 2.4 참조
        SearchAsync(evt.newValue, _disableCts.Token).Forget();
    }

    private async UniTaskVoid SearchAsync(string query, CancellationToken ct)
    {
        await UniTask.Delay(300, cancellationToken: ct); // 간단 디바운스
        var results = await FetchResultsAsync(query, ct);
        if (ct.IsCancellationRequested) return; // 취소 확인
        _resultLabel.text = $"결과: {results.Count}건";
    }

    private async UniTask<System.Collections.Generic.List<string>> FetchResultsAsync(
        string query, CancellationToken ct)
    {
        // 실제 데이터 소스 쿼리
        await UniTask.Delay(100, cancellationToken: ct);
        return new System.Collections.Generic.List<string>();
    }
}
```

---

### 2.2 DOTween + UI Toolkit VisualElement 트위닝

#### DOTween과 VisualElement 연동 원리

DOTween은 UI Toolkit `VisualElement`에 대한 직접 확장 메서드(`DOMove`, `DOScale`, `DORotate`)를 일부 제공한다. 그러나 opacity, color, width/height 같은 스타일 속성은 DOTween이 직접 알지 못하므로, `DOTween.To()` getter/setter 람다 패턴으로 수동 구현해야 한다.

**DOTween.To() 핵심 시그니처**

```csharp
// float 트위닝 (opacity, alpha 등)
DOTween.To(
    getter: () => element.resolvedStyle.opacity,  // 현재 값 읽기
    setter: x  => element.style.opacity = x,      // 값 적용
    endValue: 1f,                                  // 목표값
    duration: 0.3f                                 // 초
);
```

#### VisualElement 스타일 트위닝 전체 예제

```csharp
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit VisualElement를 DOTween으로 트위닝하는 유틸리티.
/// DOTween이 VisualElement style 속성을 직접 알지 못하므로
/// DOTween.To()의 getter/setter 람다 패턴으로 구현한다.
/// </summary>
public static class VisualElementTweenExtensions
{
    // ── Opacity ──────────────────────────────────────────────────────────────

    /// <summary>투명도 트위닝 (0~1)</summary>
    public static Tweener DOFade(this VisualElement el, float to, float duration)
    {
        return DOTween.To(
            () => el.resolvedStyle.opacity,
            x  => el.style.opacity = x,
            to,
            duration
        ).SetTarget(el);
    }

    // ── Position (translate style) ────────────────────────────────────────────

    /// <summary>수평 위치 트위닝 (style.translate.x, px 단위)</summary>
    public static Tweener DOTranslateX(this VisualElement el, float to, float duration)
    {
        return DOTween.To(
            () => el.resolvedStyle.translate.x,
            x  => el.style.translate = new Translate(x, el.resolvedStyle.translate.y),
            to,
            duration
        ).SetTarget(el);
    }

    /// <summary>수직 위치 트위닝 (style.translate.y, px 단위)</summary>
    public static Tweener DOTranslateY(this VisualElement el, float to, float duration)
    {
        return DOTween.To(
            () => el.resolvedStyle.translate.y,
            y  => el.style.translate = new Translate(el.resolvedStyle.translate.x, y),
            to,
            duration
        ).SetTarget(el);
    }

    // ── Scale ─────────────────────────────────────────────────────────────────

    /// <summary>균일 스케일 트위닝</summary>
    public static Tweener DOScaleUniform(this VisualElement el, float to, float duration)
    {
        return DOTween.To(
            () => el.resolvedStyle.scale.value.x,
            s  => el.style.scale = new Scale(new Vector2(s, s)),
            to,
            duration
        ).SetTarget(el);
    }

    // ── Background Color ──────────────────────────────────────────────────────

    /// <summary>배경 색상 트위닝</summary>
    public static Tweener DOBackgroundColor(this VisualElement el, Color to, float duration)
    {
        return DOTween.To(
            () => el.resolvedStyle.backgroundColor,
            c  => el.style.backgroundColor = c,
            to,
            duration
        ).SetTarget(el);
    }

    // ── Width / Height ────────────────────────────────────────────────────────

    /// <summary>너비 트위닝 (px 단위 고정값)</summary>
    public static Tweener DOWidth(this VisualElement el, float to, float duration)
    {
        return DOTween.To(
            () => el.resolvedStyle.width,
            w  => el.style.width = w,
            to,
            duration
        ).SetTarget(el);
    }

    /// <summary>너비를 퍼센트로 트위닝 (예: HP 바)</summary>
    public static Tweener DOWidthPercent(this VisualElement el, float toPercent, float duration)
    {
        float current = el.resolvedStyle.width /
                        (el.parent?.resolvedStyle.width ?? 1f) * 100f;
        return DOTween.To(
            () => current,
            w  => { current = w; el.style.width = Length.Percent(w); },
            toPercent,
            duration
        ).SetTarget(el);
    }

    // ── Left / Top (absolute 위치) ────────────────────────────────────────────

    /// <summary>절대 위치 left 트위닝</summary>
    public static Tweener DOLeft(this VisualElement el, float to, float duration)
    {
        return DOTween.To(
            () => el.resolvedStyle.left,
            v  => el.style.left = v,
            to,
            duration
        ).SetTarget(el);
    }

    // ── Kill helper ────────────────────────────────────────────────────────────

    /// <summary>이 요소에 연결된 모든 Tween 종료</summary>
    public static void DOKillAll(this VisualElement el, bool complete = false)
        => DOTween.Kill(el, complete);
}
```

#### 패널 열기/닫기 시퀀스 (scale + fade)

```csharp
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;

public class AnimatedPanelController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private VisualElement _panel;
    private VisualElement _overlay;

    private void OnEnable()
    {
        var root  = _document.rootVisualElement;
        _panel   = root.Q<VisualElement>("settings-panel");
        _overlay = root.Q<VisualElement>("overlay");

        // 초기 상태: 숨김
        _panel.style.opacity   = 0f;
        _panel.style.scale     = new Scale(new Vector2(0.8f, 0.8f));
        _panel.style.display   = DisplayStyle.None;
        _overlay.style.opacity = 0f;
        _overlay.style.display = DisplayStyle.None;
    }

    /// <summary>패널 열기 애니메이션 (await 가능)</summary>
    public async UniTask ShowAsync(CancellationToken ct = default)
    {
        _panel.style.display   = DisplayStyle.Flex;
        _overlay.style.display = DisplayStyle.Flex;

        // 오버레이와 패널을 동시에 fade-in + scale-up
        var overlayFade  = _overlay.DOFade(0.5f, 0.2f);
        var panelFade    = _panel.DOFade(1f, 0.25f);
        var panelScale   = _panel.DOScaleUniform(1f, 0.25f).SetEase(Ease.OutBack);

        // 세 트윈이 동시에 실행되도록 Sequence에 Join
        var seq = DOTween.Sequence()
            .Join(overlayFade)
            .Join(panelFade)
            .Join(panelScale);

        await seq.WithCancellation(ct);
    }

    /// <summary>패널 닫기 애니메이션 (await 가능)</summary>
    public async UniTask HideAsync(CancellationToken ct = default)
    {
        // 닫기: 동시에 fade-out + scale-down
        var seq = DOTween.Sequence()
            .Join(_overlay.DOFade(0f, 0.15f))
            .Join(_panel.DOFade(0f, 0.2f))
            .Join(_panel.DOScaleUniform(0.9f, 0.2f).SetEase(Ease.InQuad));

        await seq.WithCancellation(ct);

        _panel.style.display   = DisplayStyle.None;
        _overlay.style.display = DisplayStyle.None;
    }
}
```

#### 리스트 스태거 애니메이션 (아이템 순차 등장)

```csharp
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;

public static class ListStaggerAnimation
{
    /// <summary>
    /// 리스트 아이템을 staggerDelay 간격으로 순차 fade-in.
    /// ListView의 makeItem/bindItem 이후 호출한다.
    /// </summary>
    public static async UniTask StaggerFadeIn(
        IList<VisualElement> items,
        float itemDuration  = 0.15f,
        float staggerDelay  = 0.05f,
        CancellationToken ct = default)
    {
        // 초기 상태: 모두 투명 + 아래로 오프셋
        foreach (var item in items)
        {
            item.style.opacity = 0f;
            item.style.translate = new Translate(0, 20f);
        }

        var tasks = new List<UniTask>();

        for (int i = 0; i < items.Count; i++)
        {
            var item  = items[i];
            float delay = i * staggerDelay;

            var seq = DOTween.Sequence()
                .AppendInterval(delay)
                .Join(item.DOFade(1f, itemDuration))
                .Join(item.DOTranslateY(0f, itemDuration).SetEase(Ease.OutQuad));

            tasks.Add(seq.WithCancellation(ct));
        }

        await UniTask.WhenAll(tasks);
    }
}
```

#### 버튼 피드백 애니메이션

```csharp
public static class ButtonFeedback
{
    /// <summary>버튼 클릭 시 pulse 효과 (scale 1 → 0.95 → 1)</summary>
    public static void PlayPulse(VisualElement button)
    {
        button.DOKillAll();
        DOTween.Sequence()
            .Append(button.DOScaleUniform(0.92f, 0.08f).SetEase(Ease.InQuad))
            .Append(button.DOScaleUniform(1.0f,  0.12f).SetEase(Ease.OutBack))
            .SetTarget(button);
    }

    /// <summary>버튼 클릭 시 색상 피드백</summary>
    public static void PlayColorFlash(VisualElement button, Color flashColor)
    {
        var original = button.resolvedStyle.backgroundColor;
        DOTween.Sequence()
            .Append(button.DOBackgroundColor(flashColor,  0.1f))
            .Append(button.DOBackgroundColor(original,    0.2f))
            .SetTarget(button);
    }

    /// <summary>데미지 숫자 팝업 (위로 이동 + fade-out)</summary>
    public static async UniTask PlayDamagePopup(
        Label label, Vector2 startPos, float damage, CancellationToken ct)
    {
        label.style.display  = DisplayStyle.Flex;
        label.style.opacity  = 1f;
        label.style.left     = startPos.x;
        label.style.top      = startPos.y;
        label.text           = $"-{damage:F0}";

        await DOTween.Sequence()
            .Join(label.DOTranslateY(-60f, 0.8f).SetEase(Ease.OutQuad))
            .Join(label.DOFade(0f, 0.8f).SetDelay(0.3f))
            .WithCancellation(ct);

        label.style.display = DisplayStyle.None;
    }
}
```

---

### 2.3 CSS Transition vs DOTween 선택 기준표

UI Toolkit의 CSS Transition은 USS 파일에서 선언하며, 런타임 코드 없이 상태 변화에 따라 자동으로 애니메이션된다. DOTween은 코드에서 절차적으로 제어한다.

**AnimatableUSS 속성 (Unity 6)**

| 속성 범주 | 속성 목록 | 비고 |
|-----------|----------|------|
| 색상 | color, background-color, border-color, tint-color | 최적화됨 |
| 투명도 | opacity | 최적화됨 |
| Transform | translate, rotate, scale | 권장 (레이아웃 재계산 없음) |
| 크기/위치 | width, height, top, left, right, bottom | 레이아웃 재계산 발생 |
| 여백 | margin, padding | 레이아웃 재계산 발생 |
| 테두리 | border-width, border-radius | - |
| 폰트 | font-size | - |

**선택 기준표**

| 시나리오 | CSS Transition | DOTween | 이유 |
|----------|---------------|---------|------|
| 버튼 hover 하이라이트 | 권장 | 불필요 | USS :hover pseudo-class로 선언적 |
| Toggle 상태 전환 색상 | 권장 | 불필요 | USS :checked 상태로 충분 |
| 패널 fade-in/out (단순) | 가능 | 가능 | USS transition이 더 간단 |
| 패널 열기 (scale + fade 동시) | 어려움 | 권장 | 여러 속성 동기화가 Sequence로 명확 |
| 리스트 아이템 스태거 | 불가 | 권장 | delay 계산이 절차적 |
| 탄성/바운스 이징 | 제한적 | 권장 | ease-out-back 있지만 커스텀 불가 |
| 루프 애니메이션 | 가능 | 권장 | SetLoops()가 더 직관적 |
| 값 기반 (HP 바, 진행률) | 가능 | 권장 | DOTween.To로 실시간 값과 연동 |
| 화면 전환 await 가능 여부 | TransitionEndEvent 필요 | .WithCancellation() | DOTween이 더 단순 |
| 조건부 스킵 (저사양 모드) | 불가 | DOTween.timeScale | 애니메이션 속도 제어 용이 |

**TransitionEndEvent로 CSS 완료 감지 (await 대안)**

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIToolkitTransitionExtensions
{
    /// <summary>
    /// CSS Transition 완료까지 await.
    /// DOTween .WithCancellation() 의 USS 대응 버전.
    ///
    /// 주의: TransitionEndEvent는 이전 스타일 상태가 없으면 발행되지 않는다.
    /// 요소가 처음 표시될 때 (display: none → flex) 직후 transition이
    /// 발생하지 않는 경우가 있으므로, 한 프레임 대기 후 스타일 변경 권장.
    /// </summary>
    public static UniTask WaitForTransitionEndAsync(
        this VisualElement element,
        string propertyName = null,
        CancellationToken ct = default)
    {
        var tcs = new UniTaskCompletionSource();

        void Handler(TransitionEndEvent evt)
        {
            // 특정 속성만 기다리는 경우 필터링
            if (propertyName != null &&
                !evt.stylePropertyNames.Contains(propertyName)) return;

            element.UnregisterCallback<TransitionEndEvent>(Handler);
            tcs.TrySetResult();
        }

        element.RegisterCallback<TransitionEndEvent>(Handler);
        ct.RegisterWithoutCaptureExecutionContext(() =>
        {
            element.UnregisterCallback<TransitionEndEvent>(Handler);
            tcs.TrySetCanceled();
        });

        return tcs.Task;
    }

    /// <summary>
    /// USS 클래스 추가 + 해당 transition 완료 await.
    /// 한 프레임 대기 후 클래스를 추가해 transition이 올바르게 발생하도록 한다.
    /// </summary>
    public static async UniTask AddClassAndWaitTransitionAsync(
        this VisualElement element,
        string className,
        string propertyName = null,
        CancellationToken ct = default)
    {
        // 레이아웃 확정 후 클래스 추가 (transition 미발생 방지)
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, ct);

        var waitTask = element.WaitForTransitionEndAsync(propertyName, ct);
        element.AddToClassList(className);
        await waitTask;
    }
}

// 사용 예 — USS에 .panel--visible { opacity: 1; transition: opacity 0.3s; } 정의 시
// await _panel.AddClassAndWaitTransitionAsync("panel--visible", "opacity", ct);
```

---

### 2.4 R3 부분 도입: C# event가 부족한 경우

#### C# event로 충분한 경우

```csharp
// 단순 클릭 → C# event 충분
_view.OnSpendClicked += HandleSpend;

// 단일 값 변경 알림 → ObservableValue<T> 충분
_model.Gold.Changed += _view.SetGold;

// 1:N 멀티 리스너 → C# event 다중 구독 지원
_model.GoldChanged += _hudView.SetGold;
_model.GoldChanged += _shopView.UpdatePrices;
```

#### R3가 필요한 경우 (부분 도입 패턴)

```csharp
using R3;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 이벤트를 Observable로 변환하는 미니 헬퍼.
/// 프로젝트 전체가 아닌 R3가 필요한 특정 View에서만 사용한다.
/// R3 의존성이 없는 View는 이 파일을 참조하지 않는다.
/// </summary>
public static class UIToolkitR3Extensions
{
    // ── Button ────────────────────────────────────────────────────────────────

    /// <summary>Button.clicked → Observable{Unit}</summary>
    public static Observable<Unit> OnClickAsObservable(
        this Button button,
        CancellationToken ct = default)
    {
        return Observable.Create<Unit>(observer =>
        {
            void Handler() => observer.OnNext(Unit.Default);
            button.clicked += Handler;
            return Disposable.Create(() => button.clicked -= Handler);
        }).TakeUntil(ct.AsObservable());
    }

    // ── TextField ─────────────────────────────────────────────────────────────

    /// <summary>TextField 값 변경 → Observable{string}</summary>
    public static Observable<string> OnValueChangedAsObservable(
        this TextField field,
        CancellationToken ct = default)
    {
        return Observable.Create<string>(observer =>
        {
            void Handler(ChangeEvent<string> evt) => observer.OnNext(evt.newValue);
            field.RegisterValueChangedCallback(Handler);
            return Disposable.Create(() => field.UnregisterValueChangedCallback(Handler));
        }).TakeUntil(ct.AsObservable());
    }

    // ── 범용 VisualElement 이벤트 ────────────────────────────────────────────

    /// <summary>RegisterCallback 기반 범용 래퍼</summary>
    public static Observable<TEvent> OnEventAsObservable<TEvent>(
        this VisualElement element,
        TrickleDown trickleDown = TrickleDown.NoTrickleDown,
        CancellationToken ct = default)
        where TEvent : EventBase<TEvent>, new()
    {
        return Observable.Create<TEvent>(observer =>
        {
            void Handler(TEvent e) => observer.OnNext(e);
            element.RegisterCallback<TEvent>(Handler, trickleDown);
            return Disposable.Create(() => element.UnregisterCallback<TEvent>(Handler, trickleDown));
        }).TakeUntil(ct.AsObservable());
    }
}
```

**디바운스 검색 예제 (R3 부분 도입)**

```csharp
/// <summary>
/// R3를 사용하는 검색 패널 — 디바운스가 필요한 경우.
/// 검색 패널만 R3에 의존하고, 나머지 화면은 C# event 사용.
/// </summary>
public class SearchPanelPresenter : IDisposable
{
    private readonly SearchPanelView _view;
    private readonly ItemDatabase    _db;
    private CompositeDisposable      _disposables = new();

    public SearchPanelPresenter(SearchPanelView view, ItemDatabase db, CancellationToken ct)
    {
        _view = view;
        _db   = db;

        // R3 디바운스: 300ms 입력 없으면 검색 실행
        _view.SearchField
            .OnValueChangedAsObservable(ct)
            .Debounce(TimeSpan.FromMilliseconds(300))
            .DistinctUntilChanged()
            .SubscribeAwait(async (query, token) =>
            {
                var results = await _db.SearchAsync(query, token);
                _view.ShowResults(results);
            }, AwaitOperation.Switch, configureAwait: false) // configureAwait:false = Unity 메인 스레드 유지
            .AddTo(_disposables);

        // R3 쓰로틀: 빠른 클릭 방지 (0.5초 내 재클릭 무시)
        _view.SearchButton
            .OnClickAsObservable(ct)
            .ThrottleFirst(TimeSpan.FromSeconds(0.5))
            .Subscribe(_ => TriggerSearch(_view.SearchField.value))
            .AddTo(_disposables);
    }

    private void TriggerSearch(string query)
    {
        // 즉시 검색 실행
    }

    public void Dispose() => _disposables.Dispose();
}
```

**R3 최소 도입 결정 흐름**

```
사용자 입력이 있는가?
  └─ 단순 클릭 / 토글 → C# event 충분
  └─ 텍스트 입력 (실시간 검색) → R3 Debounce 고려
       └─ 디바운스를 수동으로 구현할 수 있는가?
            ├─ 예 → UniTask.Delay + CancellationTokenSource (섹션 2.1 패턴 D)
            └─ 스트림 연산이 복잡한가? → R3 부분 도입

여러 이벤트를 결합해야 하는가?
  ├─ 아니오 → C# event
  └─ 예 (CombineLatest, Merge, Zip) → R3 도입

이벤트 발행 빈도가 높은가? (게임패드, 마우스 이동)
  ├─ 아니오 → C# event
  └─ 예 → R3 ThrottleFrame 또는 Sample 고려
```

---

### 2.5 완전 동작 예제: Settings Screen

다음은 설정 화면 전체 구현이다. UIDocument View + Presenter + UniTask 확인 다이얼로그 + DOTween 패널 전환을 모두 포함한다.

**Settings.uxml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<UXML xmlns="UnityEngine.UIElements">
    <!-- 오버레이 (다이얼로그 배경) -->
    <VisualElement name="overlay" class="overlay" picking-mode="Ignore"/>

    <!-- 메인 설정 패널 -->
    <VisualElement name="settings-panel" class="settings-panel">
        <Label text="Settings" class="panel-title"/>

        <!-- 볼륨 설정 -->
        <VisualElement class="settings-row">
            <Label text="Master Volume" class="settings-label"/>
            <Slider name="master-volume" low-value="0" high-value="1" value="0.8"
                    class="settings-slider"/>
        </VisualElement>

        <VisualElement class="settings-row">
            <Label text="SFX Volume" class="settings-label"/>
            <Slider name="sfx-volume" low-value="0" high-value="1" value="0.7"
                    class="settings-slider"/>
        </VisualElement>

        <!-- 그래픽 품질 -->
        <VisualElement class="settings-row">
            <Label text="Quality" class="settings-label"/>
            <DropdownField name="quality-dropdown" class="settings-dropdown"/>
        </VisualElement>

        <!-- 전체화면 -->
        <VisualElement class="settings-row">
            <Label text="Fullscreen" class="settings-label"/>
            <Toggle name="fullscreen-toggle" class="settings-toggle"/>
        </VisualElement>

        <!-- 하단 버튼 -->
        <VisualElement class="button-row">
            <Button name="apply-btn"  text="Apply"  class="btn btn-primary"/>
            <Button name="cancel-btn" text="Cancel" class="btn btn-secondary"/>
        </VisualElement>
    </VisualElement>

    <!-- 확인 다이얼로그 -->
    <VisualElement name="confirm-dialog" class="dialog-overlay">
        <VisualElement class="dialog-box">
            <Label name="dialog-message" class="dialog-message"/>
            <VisualElement class="dialog-buttons">
                <Button name="dialog-yes" text="Yes" class="btn btn-primary"/>
                <Button name="dialog-no"  text="No"  class="btn btn-secondary"/>
            </VisualElement>
        </VisualElement>
    </VisualElement>
</UXML>
```

**Settings.uss**

```css
/* Settings.uss — 다크 테마 */
.overlay {
    position: absolute;
    top: 0; left: 0; right: 0; bottom: 0;
    background-color: rgba(0, 0, 0, 0.5);
    opacity: 0;
}

.settings-panel {
    position: absolute;
    left: 50%;
    top: 50%;
    translate: -50% -50%;
    width: 480px;
    background-color: #1e1e2e;
    border-radius: 12px;
    padding: 24px;
    flex-direction: column;
    opacity: 0;
    scale: 0.85 0.85;
    /* CSS Transition — 단순 hover는 USS에서, 복잡한 열기/닫기는 DOTween에서 */
    transition-property: opacity, scale;
    transition-duration: 0.25s;
    transition-timing-function: ease-out;
}

.panel-title {
    font-size: 22px;
    color: #cdd6f4;
    margin-bottom: 20px;
    -unity-font-style: bold;
}

.settings-row {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16px;
}

.settings-label {
    font-size: 14px;
    color: #bac2de;
    width: 160px;
}

.settings-slider {
    flex-grow: 1;
}

.settings-dropdown {
    width: 160px;
}

.button-row {
    flex-direction: row;
    justify-content: flex-end;
    margin-top: 24px;
}

.btn {
    padding: 10px 24px;
    border-radius: 6px;
    font-size: 14px;
    margin-left: 8px;
    /* 버튼 hover는 CSS Transition으로 충분 */
    transition-property: background-color, scale;
    transition-duration: 0.1s;
}

.btn-primary {
    background-color: #89b4fa;
    color: #1e1e2e;
}

.btn-primary:hover {
    background-color: #b4d0fb;
}

.btn-primary:active {
    scale: 0.96 0.96;
}

.btn-secondary {
    background-color: #313244;
    color: #cdd6f4;
}

.btn-secondary:hover {
    background-color: #45475a;
}

.dialog-overlay {
    position: absolute;
    top: 0; left: 0; right: 0; bottom: 0;
    background-color: rgba(0, 0, 0, 0.6);
    justify-content: center;
    align-items: center;
    display: none;
}

.dialog-box {
    background-color: #1e1e2e;
    border-radius: 8px;
    padding: 24px;
    min-width: 300px;
}

.dialog-message {
    font-size: 16px;
    color: #cdd6f4;
    margin-bottom: 20px;
    white-space: normal;
}

.dialog-buttons {
    flex-direction: row;
    justify-content: flex-end;
}
```

**SettingsModel.cs — C# event 기반 상태**

```csharp
using System;

public class SettingsModel
{
    // 현재 (미적용) 값
    public float MasterVolume { get; set; } = 0.8f;
    public float SfxVolume    { get; set; } = 0.7f;
    public int   QualityIndex { get; set; } = 2;
    public bool  Fullscreen   { get; set; } = false;

    // 저장된 (적용된) 값 — 취소 시 복원용
    private float _savedMaster;
    private float _savedSfx;
    private int   _savedQuality;
    private bool  _savedFullscreen;

    public event Action SettingsApplied;

    public void SaveCurrent()
    {
        _savedMaster     = MasterVolume;
        _savedSfx        = SfxVolume;
        _savedQuality    = QualityIndex;
        _savedFullscreen = Fullscreen;
    }

    public void RestoreSaved()
    {
        MasterVolume = _savedMaster;
        SfxVolume    = _savedSfx;
        QualityIndex = _savedQuality;
        Fullscreen   = _savedFullscreen;
    }

    public bool HasUnsavedChanges =>
        MasterVolume != _savedMaster ||
        SfxVolume    != _savedSfx    ||
        QualityIndex != _savedQuality ||
        Fullscreen   != _savedFullscreen;

    public void Apply()
    {
        SaveCurrent();
        // 실제 적용 (AudioMixer, QualitySettings 등)
        UnityEngine.QualitySettings.SetQualityLevel(QualityIndex);
        UnityEngine.Screen.fullScreen = Fullscreen;
        SettingsApplied?.Invoke();
    }
}
```

**SettingsView.cs — View (MonoBehaviour + UIDocument)**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsView : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    // === 사용자 입력 이벤트 ===
    public event Action                OnApplyClicked;
    public event Action                OnCancelClicked;
    public event Action<float>         OnMasterVolumeChanged;
    public event Action<float>         OnSfxVolumeChanged;
    public event Action<int>           OnQualityChanged;
    public event Action<bool>          OnFullscreenChanged;
    public event Action<bool>          OnDialogAnswered;  // true = Yes

    // === UI 요소 캐시 ===
    private VisualElement  _panel;
    private VisualElement  _overlay;
    private Slider         _masterSlider;
    private Slider         _sfxSlider;
    private DropdownField  _qualityDropdown;
    private Toggle         _fullscreenToggle;
    private Button         _applyButton;
    private Button         _cancelButton;

    // 인라인 다이얼로그
    private VisualElement  _confirmDialog;
    private Label          _dialogMessage;
    private Button         _dialogYes;
    private Button         _dialogNo;

    public VisualElement Panel   => _panel;
    public VisualElement Overlay => _overlay;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _panel           = root.Q<VisualElement>("settings-panel");
        _overlay         = root.Q<VisualElement>("overlay");
        _masterSlider    = root.Q<Slider>("master-volume");
        _sfxSlider       = root.Q<Slider>("sfx-volume");
        _qualityDropdown = root.Q<DropdownField>("quality-dropdown");
        _fullscreenToggle= root.Q<Toggle>("fullscreen-toggle");
        _applyButton     = root.Q<Button>("apply-btn");
        _cancelButton    = root.Q<Button>("cancel-btn");
        _confirmDialog   = root.Q<VisualElement>("confirm-dialog");
        _dialogMessage   = root.Q<Label>("dialog-message");
        _dialogYes       = root.Q<Button>("dialog-yes");
        _dialogNo        = root.Q<Button>("dialog-no");

        // 품질 옵션 목록 설정
        _qualityDropdown.choices = new List<string> { "Low", "Medium", "High", "Ultra" };

        // ✅ named method로 이벤트 등록 (익명 람다 해제 불가 방지)
        _applyButton.clicked  += HandleApplyClicked;
        _cancelButton.clicked += HandleCancelClicked;
        _dialogYes.clicked    += HandleDialogYes;
        _dialogNo.clicked     += HandleDialogNo;

        _masterSlider.RegisterValueChangedCallback(
            e => OnMasterVolumeChanged?.Invoke(e.newValue));
        _sfxSlider.RegisterValueChangedCallback(
            e => OnSfxVolumeChanged?.Invoke(e.newValue));
        _qualityDropdown.RegisterValueChangedCallback(
            e => OnQualityChanged?.Invoke(_qualityDropdown.index));
        _fullscreenToggle.RegisterValueChangedCallback(
            e => OnFullscreenChanged?.Invoke(e.newValue));

        // 초기 숨김
        _panel.style.display  = DisplayStyle.None;
        _overlay.style.display = DisplayStyle.None;
        _confirmDialog.style.display = DisplayStyle.None;
    }

    // ✅ named methods
    private void HandleApplyClicked()      => OnApplyClicked?.Invoke();
    private void HandleCancelClicked()     => OnCancelClicked?.Invoke();
    private void HandleDialogYes()         => OnDialogAnswered?.Invoke(true);
    private void HandleDialogNo()          => OnDialogAnswered?.Invoke(false);

    private void OnDisable()
    {
        if (_applyButton  != null) _applyButton.clicked  -= HandleApplyClicked;
        if (_cancelButton != null) _cancelButton.clicked -= HandleCancelClicked;
        if (_dialogYes    != null) _dialogYes.clicked    -= HandleDialogYes;
        if (_dialogNo     != null) _dialogNo.clicked     -= HandleDialogNo;
    }

    // === display 메서드 ===
    public void SetValues(float master, float sfx, int quality, bool fullscreen)
    {
        _masterSlider.SetValueWithoutNotify(master);   // notify 없이 설정
        _sfxSlider.SetValueWithoutNotify(sfx);
        _qualityDropdown.index = quality;
        _fullscreenToggle.SetValueWithoutNotify(fullscreen);
    }

    public void ShowConfirmDialog(string message)
    {
        _dialogMessage.text          = message;
        _confirmDialog.style.display = DisplayStyle.Flex;
    }

    public void HideConfirmDialog()
        => _confirmDialog.style.display = DisplayStyle.None;
}
```

**SettingsPresenter.cs — Presenter (Pure C# + UniTask + DOTween)**

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class SettingsPresenter : IDisposable
{
    private readonly SettingsView    _view;
    private readonly SettingsModel   _model;
    private readonly CancellationToken _ct;
    private bool _disposed;

    // 다이얼로그 응답을 기다리기 위한 TCS
    private UniTaskCompletionSource<bool> _dialogTcs;

    public SettingsPresenter(SettingsView view, SettingsModel model, CancellationToken ct)
    {
        _view  = view;
        _model = model;
        _ct    = ct;

        // View 이벤트 구독
        _view.OnApplyClicked       += () => HandleApplyAsync().Forget();
        _view.OnCancelClicked      += () => HandleCancelAsync().Forget();
        _view.OnMasterVolumeChanged += v  => _model.MasterVolume = v;
        _view.OnSfxVolumeChanged    += v  => _model.SfxVolume    = v;
        _view.OnQualityChanged      += i  => _model.QualityIndex  = i;
        _view.OnFullscreenChanged   += b  => _model.Fullscreen    = b;
        _view.OnDialogAnswered      += b  => _dialogTcs?.TrySetResult(b);
    }

    public void Initialize()
    {
        _model.SaveCurrent();
        _view.SetValues(_model.MasterVolume, _model.SfxVolume,
                        _model.QualityIndex, _model.Fullscreen);
    }

    /// <summary>DOTween으로 패널 열기 (await 가능)</summary>
    public async UniTask ShowAsync()
    {
        _view.Panel.style.display  = DisplayStyle.Flex;
        _view.Overlay.style.display = DisplayStyle.Flex;

        // 초기 상태 설정 (DOTween 시작값)
        _view.Panel.style.opacity = 0f;
        _view.Panel.style.scale   = new UnityEngine.UIElements.Scale(
            new UnityEngine.Vector2(0.85f, 0.85f));

        var seq = DOTween.Sequence()
            .Join(_view.Overlay.DOFade(0.5f, 0.2f))
            .Join(_view.Panel.DOFade(1f, 0.25f))
            .Join(_view.Panel.DOScaleUniform(1f, 0.25f)
                .SetEase(Ease.OutBack));

        await seq.WithCancellation(_ct);
    }

    /// <summary>DOTween으로 패널 닫기 (await 가능)</summary>
    public async UniTask HideAsync()
    {
        var seq = DOTween.Sequence()
            .Join(_view.Overlay.DOFade(0f, 0.15f))
            .Join(_view.Panel.DOFade(0f, 0.2f))
            .Join(_view.Panel.DOScaleUniform(0.9f, 0.2f).SetEase(Ease.InQuad));

        await seq.WithCancellation(_ct);

        _view.Panel.style.display   = DisplayStyle.None;
        _view.Overlay.style.display = DisplayStyle.None;
    }

    private async UniTaskVoid HandleApplyAsync()
    {
        if (!_model.HasUnsavedChanges)
        {
            await HideAsync();
            return;
        }

        // UniTask async 다이얼로그 — 사용자 응답 대기
        bool confirmed = await ShowInlineDialogAsync(
            "변경사항을 적용하시겠습니까?");

        if (confirmed)
        {
            _model.Apply();
            await HideAsync();
        }
    }

    private async UniTaskVoid HandleCancelAsync()
    {
        if (_model.HasUnsavedChanges)
        {
            bool confirmed = await ShowInlineDialogAsync(
                "변경사항을 취소하시겠습니까?");
            if (!confirmed) return;
        }

        _model.RestoreSaved();
        _view.SetValues(_model.MasterVolume, _model.SfxVolume,
                        _model.QualityIndex, _model.Fullscreen);
        await HideAsync();
    }

    /// <summary>
    /// 인라인 다이얼로그를 표시하고 사용자 응답을 await.
    /// View.OnDialogAnswered 이벤트가 TCS를 완료시킨다.
    /// </summary>
    private UniTask<bool> ShowInlineDialogAsync(string message)
    {
        _dialogTcs = new UniTaskCompletionSource<bool>();
        _view.ShowConfirmDialog(message);

        _ct.RegisterWithoutCaptureExecutionContext(() =>
        {
            _view.HideConfirmDialog();
            _dialogTcs?.TrySetCanceled();
        });

        return _dialogTcs.Task.ContinueWith(result =>
        {
            _view.HideConfirmDialog();
            return result;
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _dialogTcs?.TrySetCanceled();
    }
}
```

**SettingsBootstrapper.cs — 최종 조합**

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SettingsBootstrapper : MonoBehaviour
{
    [SerializeField] private SettingsView _settingsView;

    private SettingsModel     _model;
    private SettingsPresenter _presenter;

    private void Start()
    {
        _model     = new SettingsModel();
        _presenter = new SettingsPresenter(_settingsView, _model,
                         destroyCancellationToken);
        _presenter.Initialize();
    }

    // 외부 (MainMenu Presenter 등)에서 호출
    public async UniTask OpenSettingsAsync()
    {
        await _presenter.ShowAsync();
    }

    private void OnDestroy() => _presenter?.Dispose();
}
```

---

## 3. 베스트 프랙티스

### DO (권장)

- **UniTask.Forget() + UniTaskVoid**: fire-and-forget 패턴은 `async UniTaskVoid` 반환 타입과 `.Forget()` 조합 사용
- **destroyCancellationToken 우선 사용**: Unity 2022.2+의 `MonoBehaviour.destroyCancellationToken`으로 OnDestroy 시 자동 취소
- **DOTween.Sequence().Join()**: 여러 트윈을 동기화할 때 Append 대신 Join으로 병렬 실행
- **DOTween.SetTarget(el)**: VisualElement를 타겟으로 등록해 `DOTween.Kill(el)` 로 일괄 종료 가능
- **CSS Transition은 USS에서, 복잡 애니메이션은 DOTween에서**: 역할 분리로 코드/스타일 혼재 방지
- **UniTaskCompletionSource<bool>**: async 다이얼로그의 표준 패턴, await 후 정리 (TrySetCanceled 포함)
- **R3 부분 도입**: 디바운스/쓰로틀이 필요한 View에서만 R3 확장 사용, 나머지는 C# event 유지
- **SubscribeAwait configureAwait:false**: R3 SubscribeAwait 사용 시 Unity 메인 스레드 유지 필수

### DON'T (금지)

- **AttachToPanelEvent에서 UniTask 시작 금지**: 여러 번 발생 가능, Initialize() 또는 Start()에서 시작
- **async void 금지**: Unity 런타임이 예외를 삼킴. `async UniTaskVoid` 또는 `UniTask.UnityAction` 사용
- **UniTask double-await 금지**: 같은 UniTask 인스턴스를 두 번 await하면 InvalidOperationException 발생
- **DOTween.To에서 resolvedStyle 직접 쓰기 금지**: setter에서는 `el.style.xxx = x`, getter에서만 `resolvedStyle` 사용
- **DisplayStyle.None 전환 직전 transition 기대 금지**: 요소가 None이 되면 transition 미발행, Fade-out 완료 후 None으로 변경
- **System.Progress<float> 사용 금지**: `Cysharp.Threading.Tasks.Progress.Create<float>()` 사용 (GC 절약)

### CONSIDER (상황별)

- **OnDisable CancellationTokenSource**: OnDestroy보다 OnDisable에서 작업을 취소해야 하면 별도 `_disableCts` 생성
- **TransitionEndEvent**: CSS Transition 완료를 코드에서 감지해야 할 때 사용 (DOTween .WithCancellation()의 대안)
- **DOTween timeScale**: 저사양 모드나 테스트에서 `DOTween.timeScale = 0f`로 모든 애니메이션 일시 중단 가능
- **SubscribeAwait AwaitOperation.Switch**: 자동완성/검색처럼 새 입력이 이전 작업을 취소해야 할 때

---

## 4. 코드 요약 (Quick Reference)

```csharp
// ── DOTween.To 패턴 (UI Toolkit 스타일 트위닝) ──────────────────────────────
DOTween.To(() => el.resolvedStyle.opacity, x => el.style.opacity = x, 1f, 0.3f);
DOTween.To(() => el.resolvedStyle.translate.x, x => el.style.translate = new Translate(x, 0), 100f, 0.5f);
DOTween.To(() => el.resolvedStyle.scale.value.x, s => el.style.scale = new Scale(new Vector2(s, s)), 1f, 0.25f);
DOTween.To(() => el.resolvedStyle.backgroundColor, c => el.style.backgroundColor = c, Color.red, 0.2f);

// ── UniTask 다이얼로그 패턴 ──────────────────────────────────────────────────
var tcs = new UniTaskCompletionSource<bool>();
button.clicked += () => tcs.TrySetResult(true);
bool result = await tcs.Task;

// ── UniTask async 버튼 핸들러 ─────────────────────────────────────────────────
button.clicked += () => HandleAsync().Forget();
async UniTaskVoid HandleAsync() { await UniTask.Delay(100); DoWork(); }

// ── R3 UI Toolkit 래퍼 ───────────────────────────────────────────────────────
Observable.Create<Unit>(obs => {
    void h() => obs.OnNext(Unit.Default);
    button.clicked += h;
    return Disposable.Create(() => button.clicked -= h);
}).Debounce(TimeSpan.FromMilliseconds(300)).Subscribe(OnSearch);

// ── CSS Transition + C# 감지 ─────────────────────────────────────────────────
element.RegisterCallback<TransitionEndEvent>(evt => Debug.Log($"Ended: {evt.elapsedTime}"));

// ── CancellationToken 패턴 ───────────────────────────────────────────────────
var ct = destroyCancellationToken;
await UniTask.Delay(1000, cancellationToken: ct);
```

---

## 5. UI_Study 적용 계획

| 단계 | 예제 | 패턴 | 경로 |
|------|------|------|------|
| 10-01 | Async Dialog | UniTaskCompletionSource + UI Toolkit Button | 10-UI-Toolkit/01-AsyncDialog |
| 10-02 | Loading Screen | UniTask + ProgressBar + IProgress | 10-UI-Toolkit/02-LoadingScreen |
| 10-03 | Panel Animation | DOTween.To + Sequence + await | 10-UI-Toolkit/03-PanelAnimation |
| 10-04 | List Stagger | DOTween stagger delay + UniTask.WhenAll | 10-UI-Toolkit/04-ListStagger |
| 10-05 | Search Debounce | R3 부분 도입 + SubscribeAwait | 10-UI-Toolkit/05-SearchDebounce |
| 10-06 | Settings Screen | 전체 통합 예제 (View+Presenter+DOTween+Dialog) | 10-UI-Toolkit/06-SettingsScreen |

---

## 6. 참고 자료

- [Cysharp/UniTask GitHub](https://github.com/Cysharp/UniTask)
- [UniTask PR #338 — UI Toolkit 확장 (stale)](https://github.com/Cysharp/UniTask/pull/338)
- [UniTask Issue #261 — UI Toolkit 지원 요청](https://github.com/Cysharp/UniTask/issues/261)
- [DOTween 공식 문서](https://dotween.demigiant.com/documentation.php)
- [Unity 6 Transition Events 문서](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Transition-Events.html)
- [Unity 6 USS Transition 문서](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Transitions.html)
- [R3 GitHub — SubscribeAwait configureAwait 이슈 #99](https://github.com/Cysharp/R3/issues/99)
- [Cysharp Progress.Create vs System.Progress — UniTask 문서](https://cysharp.github.io/UniTask/#progress)
- [TransitionEndEvent API Reference](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UIElements.TransitionEndEvent.html)

---

## 7. 미해결 질문

1. **DOTween.To로 resolvedStyle 읽기 타이밍**: 레이아웃이 확정되지 않은 첫 프레임에서 `resolvedStyle.opacity`가 기본값인 1f를 반환해 getter가 잘못된 초기값을 읽을 수 있음 — `await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate)`로 한 프레임 대기 후 트윈 시작 여부 확인 필요
2. **DOTween VisualElement 직접 확장 메서드 범위**: DOTween이 `DOMove`, `DOScale`, `DORotate`를 VisualElement에 직접 제공한다는 문서가 불명확 — `DOTweenModuleUIToolkit.cs` 파일 존재 여부와 정확한 API 목록 확인 필요
3. **CSS Transition과 DOTween 동시 사용 충돌**: 같은 속성(예: opacity)에 CSS Transition과 DOTween.To가 동시에 적용되면 어느 쪽이 우선하는지 검증 필요 — DOTween 사용 시 해당 속성의 USS transition을 비활성화해야 할 가능성
4. **UniTask.WhenAll과 DOTween Sequence 혼용**: `Sequence.WithCancellation(ct)`를 `UniTask.WhenAll`에 전달할 때 한 트윈이 취소되면 다른 트윈이 남아있는 상태 처리 방법 확인
5. **TransitionEndEvent 미발행 케이스 목록**: `display: none → flex` 전환 직후, 이미 목표값인 경우, 프로퍼티가 animatable하지 않은 경우 — 정확한 미발행 시나리오 목록 문서화 필요
