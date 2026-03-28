# UGUI 고급 UI 애니메이션 패턴

- **작성일**: 2026-03-28
- **카테고리**: animation
- **상태**: 조사완료
- **관련 스택**: DOTween + UniTask + R3 + UnityScreenNavigator

---

## 1. 요약

Unity UI 애니메이션의 프로덕션 품질 패턴을 10개 주제로 정리한다.
핵심 결론:
- DOTween은 코드 기반 UI 애니메이션의 사실상 표준이며, UniTask와의 통합(`AsyncWaitForCompletion().AsUniTask()`)으로 await 가능한 애니메이션을 구현한다.
- 할당(allocation) 최소화를 위해 `SetAutoKill(false)` + 캐싱, 또는 PrimeTween/LitMotion 같은 zero-allocation 대안을 고려한다.
- UnityScreenNavigator의 커스텀 트랜지션은 `TransitionAnimationBehaviour`를 상속하고 내부에서 DOTween Sequence를 구동하는 방식으로 구현한다.
- UI 파티클은 `ParticleEffectForUGUI` 패키지(mob-sakai)로 해결한다.
- 복잡한 골격 애니메이션(캐릭터 초상화, 복잡한 감정 표현)에는 Spine SkeletonGraphic, 간단한 벡터/이모지 애니메이션에는 Lottie(SkiaForUnity)를 고려한다.

---

## 2. DOTween UI 패턴 — 시퀀스, 이펙트, 커스텀 이징

### 2.1 UI 전용 확장 메서드 목록

DOTween은 `DOTweenModuleUI`를 통해 UGUI 컴포넌트에 직접 확장 메서드를 제공한다.

| 컴포넌트 | 메서드 | 설명 |
|---|---|---|
| `CanvasGroup` | `DOFade(float, float)` | alpha 트윈 |
| `Graphic` (Image, Text 포함) | `DOColor(Color, float)` | color 트윈 |
| `Graphic` | `DOFade(float, float)` | alpha만 트윈 (color는 유지) |
| `Image` | `DOFillAmount(float, float)` | fillAmount 0~1 트윈 |
| `Image` | `DOGradientColor(Gradient, float)` | 그라디언트 색 변화 (내부적으로 Sequence) |
| `Text` | `DOText(string, float)` | 타이핑 효과 (문자 단위 출력) |
| `Text` | `DOFontSize(int, float)` | 폰트 크기 트윈 |
| `RectTransform` | `DOAnchorPos(Vector2, float)` | anchoredPosition 트윈 |
| `RectTransform` | `DOSizeDelta(Vector2, float)` | sizeDelta 트윈 |
| `RectTransform` | `DOPunchAnchorPos(Vector2, float, int, float)` | 위치 펀치 |
| `RectTransform` | `DOShakeAnchorPos(float, float, int, float)` | 위치 쉐이크 |
| `Slider` | `DOValue(float, float)` | 슬라이더 값 트윈 |
| `ScrollRect` | `DONormalizedPos(Vector2, float)` | 스크롤 위치 트윈 |
| `Transform` | `DOPunchScale(Vector3, float, int, float)` | 스케일 펀치 |
| `Transform` | `DOShakeScale(float, float, int, float)` | 스케일 쉐이크 |

### 2.2 Sequence API

```csharp
// Sequence 기본 구조
Sequence seq = DOTween.Sequence();

// Append: 이전 트윈 완료 후 순차 실행
seq.Append(transform.DOScale(1.2f, 0.2f));
seq.Append(transform.DOScale(1f, 0.1f));

// Join: 마지막 Append와 동시 실행
seq.Join(canvasGroup.DOFade(1f, 0.2f));

// Insert: 특정 시간에 삽입 (Append/Join 무관)
seq.Insert(0.1f, image.DOFade(0f, 0.15f));

// AppendInterval: 무음 딜레이 삽입
seq.AppendInterval(0.5f);

// AppendCallback: 특정 시점에 콜백 실행
seq.AppendCallback(() => SomeMethod());

// 주의: Sequence에 추가된 트윈은 잠금(locked)된다.
// 트윈을 먼저 완전히 생성한 후 Sequence에 추가해야 한다.
```

### 2.3 커스텀 이징 (AnimationCurve)

```csharp
// AnimationCurve를 Ease로 사용
[SerializeField] private AnimationCurve _customEase;

transform.DOScale(1.2f, 0.3f).SetEase(_customEase);

// EaseFactory.StopMotion — 스톱모션 효과 (특정 FPS로 계단화)
transform.DOScale(1.5f, 0.5f).SetEase(EaseFactory.StopMotion(12, Ease.OutBounce));
```

**주의**: `Back`과 `Elastic` Ease는 Path 애니메이션에서 작동하지 않는다.

### 2.4 재사용 가능한 애니메이션 프리셋 — ScriptableObject 패턴

```csharp
// AnimationPreset.cs
[CreateAssetMenu(menuName = "UI/Animation Preset")]
public class AnimationPreset : ScriptableObject
{
    public float duration = 0.25f;
    public float delay = 0f;
    public Ease ease = Ease.OutQuad;
    public Vector3 punchScale = new Vector3(0.1f, 0.1f, 0f);
    public int vibrato = 5;
    public float elasticity = 0.5f;
}

// 사용 예시
public class AnimatedButton : MonoBehaviour
{
    [SerializeField] private AnimationPreset _pressPreset;

    public void OnPointerDown()
    {
        transform.DOKill();
        transform.DOPunchScale(_pressPreset.punchScale, _pressPreset.duration,
            _pressPreset.vibrato, _pressPreset.elasticity)
            .SetDelay(_pressPreset.delay);
    }
}
```

---

## 3. 화면 전환 효과

### 3.1 화면 전환 유형별 DOTween 구현

```csharp
// === 페이드 전환 ===
public static async UniTask FadeTransition(
    CanvasGroup fromGroup, CanvasGroup toGroup,
    float duration = 0.3f, CancellationToken ct = default)
{
    toGroup.alpha = 0f;
    toGroup.gameObject.SetActive(true);

    var seq = DOTween.Sequence()
        .Join(fromGroup.DOFade(0f, duration * 0.5f))
        .AppendCallback(() => fromGroup.gameObject.SetActive(false))
        .Append(toGroup.DOFade(1f, duration * 0.5f));

    await seq.AsyncWaitForCompletion().AsUniTask()
        .AttachExternalCancellation(ct);
}

// === 슬라이드 전환 (좌 → 우) ===
public static async UniTask SlideTransition(
    RectTransform from, RectTransform to,
    float screenWidth, float duration = 0.35f,
    CancellationToken ct = default)
{
    to.anchoredPosition = new Vector2(screenWidth, 0f);

    var seq = DOTween.Sequence()
        .Join(from.DOAnchorPos(new Vector2(-screenWidth * 0.3f, 0f), duration)
            .SetEase(Ease.InOutCubic))
        .Join(to.DOAnchorPos(Vector2.zero, duration)
            .SetEase(Ease.InOutCubic));

    await seq.AsyncWaitForCompletion().AsUniTask()
        .AttachExternalCancellation(ct);
}

// === 스케일 팝인 (모달용) ===
public static async UniTask ScalePopIn(
    Transform target, CanvasGroup group,
    float duration = 0.25f, CancellationToken ct = default)
{
    target.localScale = Vector3.one * 0.7f;
    group.alpha = 0f;

    var seq = DOTween.Sequence()
        .Join(target.DOScale(Vector3.one, duration).SetEase(Ease.OutBack))
        .Join(group.DOFade(1f, duration * 0.7f));

    await seq.AsyncWaitForCompletion().AsUniTask()
        .AttachExternalCancellation(ct);
}
```

### 3.2 UnityScreenNavigator 커스텀 TransitionAnimation 구현

UnityScreenNavigator는 `TransitionAnimationBehaviour`(MonoBehaviour) 또는 `TransitionAnimationObject`(ScriptableObject)를 상속해 커스텀 트랜지션을 정의한다.

구현해야 하는 추상 멤버:
- `float Duration { get; }` — 애니메이션 총 길이(초)
- `void Setup()` — 애니메이션 시작 전 초기화
- `void SetTime(float time)` — 0~Duration 범위의 시간에 따른 상태 정의

```csharp
using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Shared.Views;
using DG.Tweening;

/// <summary>
/// USN 커스텀 트랜지션 — 아래에서 슬라이드 + 페이드인.
/// Page의 AnimationContainer > Asset Type: Mono Behaviour 로 할당한다.
/// </summary>
public class SlideUpTransitionBehaviour : TransitionAnimationBehaviour
{
    [SerializeField] private float _duration = 0.35f;
    [SerializeField] private float _slideDistance = 80f;
    [SerializeField] private Ease _ease = Ease.OutCubic;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Vector2 _originalAnchoredPos;

    public override float Duration => _duration;

    public override void Setup()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _originalAnchoredPos = _rectTransform.anchoredPosition;
    }

    public override void SetTime(float time)
    {
        // DOTween의 Evaluate 대신 직접 AnimationCurve나 Mathf.Lerp 사용
        float t = Mathf.Clamp01(time / _duration);
        float easedT = DOVirtual.EasedValue(0f, 1f, t, _ease);

        float yOffset = Mathf.Lerp(-_slideDistance, 0f, easedT);
        _rectTransform.anchoredPosition = _originalAnchoredPos + new Vector2(0f, yOffset);

        if (_canvasGroup != null)
            _canvasGroup.alpha = easedT;
    }
}
```

**ScriptableObject 방식** (`TransitionAnimationObject` 상속):
- 동일한 추상 멤버 구현
- 여러 화면에서 공유하는 기본 트랜지션에 적합
- `Assets > Create > Screen Navigator > Simple Transition Animation` 으로 기본 구현 생성 가능

**파트너 화면 참조**:
- `PartnerRectTransform` 프로퍼티로 반대편 화면의 `RectTransform` 접근 가능
- 상대 화면이 없을 경우 `null`
- 화면 간 연동 애니메이션(한쪽이 들어오면서 다른 쪽이 나가는)에 활용

**Timeline 트랜지션**:
- `TimelineTransitionAnimationBehaviour` 컴포넌트를 사용해 Playable Director + Timeline Asset 연결 가능
- Play On Awake 비활성화 필수

---

## 4. 마이크로 인터랙션

### 4.1 버튼 상태별 애니메이션

```csharp
/// <summary>
/// DOTween 기반 버튼 애니메이션 — Animator 없이 상태 관리.
/// IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler 구현.
/// </summary>
[RequireComponent(typeof(Button))]
public class AnimatedButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale")]
    [SerializeField] private float _hoverScale = 1.05f;
    [SerializeField] private float _pressScale = 0.93f;
    [SerializeField] private float _hoverDuration = 0.12f;
    [SerializeField] private float _pressDuration = 0.08f;

    private Button _button;
    private Vector3 _originalScale;
    private bool _isPressed;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (!_button.interactable || _isPressed) return;
        transform.DOKill();
        transform.DOScale(_originalScale * _hoverScale, _hoverDuration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_isPressed) return;
        transform.DOKill();
        transform.DOScale(_originalScale, _hoverDuration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerDown(PointerEventData _)
    {
        if (!_button.interactable) return;
        _isPressed = true;
        transform.DOKill();
        transform.DOScale(_originalScale * _pressScale, _pressDuration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData _)
    {
        _isPressed = false;
        transform.DOKill();
        transform.DOScale(_originalScale, _hoverDuration)
            .SetEase(Ease.OutBack);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
```

### 4.2 토글 애니메이션

```csharp
public class AnimatedToggle : MonoBehaviour
{
    [SerializeField] private RectTransform _knob;
    [SerializeField] private Image _trackImage;
    [SerializeField] private Color _onColor = new Color(0.2f, 0.8f, 0.4f);
    [SerializeField] private Color _offColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private float _knobOnX = 30f;
    [SerializeField] private float _knobOffX = -30f;
    [SerializeField] private float _duration = 0.2f;

    private bool _isOn;
    private Sequence _currentSeq;

    public void SetState(bool isOn, bool animate = true)
    {
        _isOn = isOn;
        _currentSeq?.Kill();

        float targetX = isOn ? _knobOnX : _knobOffX;
        Color targetColor = isOn ? _onColor : _offColor;

        if (animate)
        {
            _currentSeq = DOTween.Sequence()
                .Join(_knob.DOAnchorPosX(targetX, _duration).SetEase(Ease.OutBack))
                .Join(_trackImage.DOColor(targetColor, _duration));
        }
        else
        {
            _knob.anchoredPosition = new Vector2(targetX, _knob.anchoredPosition.y);
            _trackImage.color = targetColor;
        }
    }

    private void OnDestroy() => _currentSeq?.Kill();
}
```

### 4.3 로딩 스피너

```csharp
public class LoadingSpinner : MonoBehaviour
{
    [SerializeField] private RectTransform _spinnerTransform;
    [SerializeField] private float _rotationDuration = 1f;

    private Tween _spinTween;

    private void OnEnable()
    {
        _spinTween = _spinnerTransform
            .DORotate(new Vector3(0f, 0f, -360f), _rotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    private void OnDisable()
    {
        _spinTween?.Kill();
        _spinTween = null;
    }
}
```

---

## 5. 스태거 애니메이션 (Cascade/Sequential List)

### 5.1 기본 스태거 패턴

```csharp
/// <summary>
/// 리스트 아이템을 순차적으로 슬라이드인 + 페이드인.
/// </summary>
public class StaggeredListAnimator : MonoBehaviour
{
    [SerializeField] private float _itemDelay = 0.06f;
    [SerializeField] private float _itemDuration = 0.3f;
    [SerializeField] private float _slideOffsetY = -30f;
    [SerializeField] private Ease _ease = Ease.OutCubic;

    private Sequence _sequence;

    /// <summary>
    /// items: 애니메이션할 RectTransform 배열 (정렬 순서대로 전달)
    /// </summary>
    public async UniTask AnimateIn(
        RectTransform[] items,
        CancellationToken ct = default)
    {
        _sequence?.Kill();
        _sequence = DOTween.Sequence();

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var group = item.GetComponent<CanvasGroup>();
            float delay = i * _itemDelay;

            // 시작 상태 초기화
            var startPos = item.anchoredPosition;
            item.anchoredPosition = startPos + new Vector2(0f, _slideOffsetY);
            if (group != null) group.alpha = 0f;

            // Insert로 딜레이 오프셋 삽입
            _sequence.Insert(delay,
                item.DOAnchorPos(startPos, _itemDuration).SetEase(_ease));

            if (group != null)
                _sequence.Insert(delay,
                    group.DOFade(1f, _itemDuration * 0.6f));
        }

        await _sequence.AsyncWaitForCompletion().AsUniTask()
            .AttachExternalCancellation(ct);
    }

    public async UniTask AnimateOut(
        RectTransform[] items,
        CancellationToken ct = default)
    {
        _sequence?.Kill();
        _sequence = DOTween.Sequence();

        // 역순으로 퇴장
        for (int i = items.Length - 1; i >= 0; i--)
        {
            var item = items[i];
            var group = item.GetComponent<CanvasGroup>();
            float delay = (items.Length - 1 - i) * _itemDelay;
            var endPos = item.anchoredPosition + new Vector2(0f, _slideOffsetY);

            _sequence.Insert(delay,
                item.DOAnchorPos(endPos, _itemDuration * 0.7f).SetEase(Ease.InCubic));

            if (group != null)
                _sequence.Insert(delay,
                    group.DOFade(0f, _itemDuration * 0.4f));
        }

        await _sequence.AsyncWaitForCompletion().AsUniTask()
            .AttachExternalCancellation(ct);
    }

    private void OnDestroy() => _sequence?.Kill();
}
```

### 5.2 레이아웃 그룹과 스태거 — 주의사항

LayoutGroup(VerticalLayoutGroup 등) 아래 있는 아이템은 `anchoredPosition`을 DOTween으로 직접 제어하면 LayoutGroup의 재계산과 충돌한다. 해결책:

```csharp
// 방법 A: LayoutGroup을 비활성화 후 애니메이션
layoutGroup.enabled = false;
// ... 애니메이션 수행 ...
// 완료 후 재활성화 (필요한 경우)

// 방법 B: CanvasGroup.alpha만 조작 (위치는 LayoutGroup에 맡김)
// 위치 이동 없이 페이드만 스태거링

// 방법 C: ContentSizeFitter + 수동 LayoutRebuilder
LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
// 빌드 후 값을 읽어 DOTween 시작
```

---

## 6. 애니메이션 상태 머신 패턴

### 6.1 열거형 기반 상태 관리

```csharp
public enum UIButtonState { Idle, Hovered, Pressed, Disabled }

public class StatefulAnimatedButton : MonoBehaviour
{
    private UIButtonState _currentState = UIButtonState.Idle;
    private Tween _activeTween;

    private static readonly Dictionary<UIButtonState, (float scale, Ease ease, float duration)>
        StateParams = new()
        {
            [UIButtonState.Idle]     = (1.00f, Ease.OutQuad,  0.12f),
            [UIButtonState.Hovered]  = (1.05f, Ease.OutQuad,  0.10f),
            [UIButtonState.Pressed]  = (0.93f, Ease.OutQuad,  0.07f),
            [UIButtonState.Disabled] = (0.95f, Ease.OutQuad,  0.15f),
        };

    public void SetState(UIButtonState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        var (scale, ease, duration) = StateParams[newState];
        _activeTween?.Kill();
        _activeTween = transform.DOScale(scale, duration).SetEase(ease);
    }

    private void OnDestroy() => _activeTween?.Kill();
}
```

### 6.2 R3 Observable 기반 상태 연결 (MV(R)P 패턴)

```csharp
// View: 상태를 Observable로 노출
public class AnimatedButtonView : MonoBehaviour
{
    private readonly Subject<UIButtonState> _stateSubject = new();
    public Observable<UIButtonState> OnStateChanged => _stateSubject;

    private StatefulAnimatedButton _animator;

    private void Awake() => _animator = GetComponent<StatefulAnimatedButton>();

    public void ApplyState(UIButtonState state)
    {
        _animator.SetState(state);
    }
}

// Presenter: Model 상태 → View 상태 변환
public class BuildButtonPresenter : IInitializable, IDisposable
{
    readonly BuildingModel _model;
    readonly AnimatedButtonView _view;
    readonly CompositeDisposable _disposables = new();

    public void Initialize()
    {
        _model.CanBuild
            .Select(canBuild => canBuild ? UIButtonState.Idle : UIButtonState.Disabled)
            .Subscribe(_view.ApplyState)
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
```

---

## 7. DOTween 성능 최적화

### 7.1 핵심 설정

```csharp
// 게임 시작 시 DOTween 초기화 설정 (한 번만)
DOTween.Init(recycleAllByDefault: false, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);

// 최대 동시 트윈 수 사전 예약 → 내부 배열 재할당 방지
DOTween.SetTweensCapacity(tweenersCapacity: 200, sequencesCapacity: 50);
```

### 7.2 자주 실행되는 애니메이션 — SetAutoKill(false) 캐싱 패턴

```csharp
public class CachedTweenButton : MonoBehaviour
{
    private Tween _pressTween;

    private void Start()
    {
        // 미리 생성, AutoKill 비활성화, 일시정지 상태로 대기
        _pressTween = transform
            .DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 0.5f)
            .SetAutoKill(false)
            .Pause();
    }

    public void OnClick()
    {
        _pressTween.Restart(); // 새 트윈 생성 없이 재시작
    }

    private void OnDestroy()
    {
        _pressTween?.Kill();
    }
}
```

**주의**: `SetAutoKill(false)` 트윈은 완료 후에도 매 프레임 업데이트를 받는다. 반드시 `OnDestroy()`에서 `Kill()`해야 한다.

### 7.3 전역 재활용 설정 (SetRecyclable)

```csharp
// 전역 설정 — 모든 트윈에 적용
DOTween.Init(recycleAllByDefault: true);

// 또는 개별 트윈에 적용
transform.DOScale(1.2f, 0.3f).SetRecyclable(true);
```

**주의**: `SetRecyclable(true)` 사용 시 Kill 후 트윈 참조가 새 트윈으로 재활용될 수 있다. 저장된 참조가 예상치 않게 활성 상태로 보일 수 있으므로 신중히 사용한다.

### 7.4 SetUpdate(true) — timeScale 독립 애니메이션

```csharp
// UI 애니메이션은 게임 일시정지(timeScale=0)에도 동작해야 할 때
canvasGroup.DOFade(1f, 0.3f).SetUpdate(UpdateType.Normal, isIndependent: true);

// 단축 형태
canvasGroup.DOFade(1f, 0.3f).SetUpdate(true);
```

### 7.5 DOTween vs 대안 라이브러리 성능 비교

| 라이브러리 | GC 할당 (애니메이션 1회) | 시작 시간 | 특징 |
|---|---|---|---|
| DOTween | ~734 B | 33.54 ms | 가장 풍부한 기능, Asset Store |
| PrimeTween | 0 B | 5.76 ms (5.8x 빠름) | Zero-allocation, 무료, Git URL |
| LitMotion | 0 B | ~6.5 ms (~5x 빠름) | DOTS 기반, Job+Burst 지원 |

**권고**: 현재 프로젝트 스택(DOTween)은 기능과 생태계 측면에서 합리적인 선택이다. 퍼포먼스 크리티컬한 씬(수백 개 동시 애니메이션)에서 문제가 생기면 PrimeTween 마이그레이션을 검토한다.

---

## 8. Spine / Lottie — 언제 DOTween 대신 사용하는가

### 8.1 Spine2D (EsotericSoftware)

**사용 케이스**:
- 복잡한 골격(skeletal) 애니메이션 — 캐릭터 초상화, 보스 UI 연출
- 여러 bone이 연동되는 감정 표현, 복잡한 idle 루프
- 아티스트가 Spine 에디터로 제작한 애셋을 그대로 사용하는 경우

**UI 통합**: `SkeletonGraphic` 컴포넌트를 사용하면 Canvas와 RectMask2D가 작동한다.

```csharp
// Spine SkeletonGraphic — 상태별 애니메이션 제어
using Spine.Unity;

public class CharacterPortrait : MonoBehaviour
{
    [SerializeField] private SkeletonGraphic _skeletonGraphic;

    // 절대 Update()마다 SetAnimation을 호출하지 말 것
    public void PlayIdle() =>
        _skeletonGraphic.AnimationState.SetAnimation(0, "idle", true);

    public void PlayDamaged() =>
        _skeletonGraphic.AnimationState.SetAnimation(0, "hit", false)
            .Complete += _ => PlayIdle(); // 완료 후 idle로 복귀
}
```

**주의**: 텍스처를 단일 아틀라스에 패킹해야 SkeletonGraphic의 다중 렌더러 오버헤드를 피할 수 있다.

### 8.2 Lottie (SkiaForUnity / SkiaSharp)

**사용 케이스**:
- 디자이너가 After Effects/Lottie Files에서 제작한 JSON 애니메이션
- 아이콘 애니메이션, 성공/실패 이펙트, 온보딩 튜토리얼
- 벡터 기반이므로 해상도 독립적

**설치**: `https://github.com/ammariqais/SkiaForUnity.git` (Git URL)

```csharp
// SkottiePlayer 컴포넌트 예시
public class LottieIcon : MonoBehaviour
{
    [SerializeField] private SkottiePlayer _player;

    public void PlayOnce() => _player.Play(loop: false);
    public void PlayLoop() => _player.Play(loop: true);
    public void SeekTo(float progress) => _player.Seek(progress); // 0~1
}
```

### 8.3 결정 기준표

| 상황 | 권장 도구 |
|---|---|
| 버튼 press/hover 효과 | DOTween |
| 화면 페이드/슬라이드 | DOTween |
| 체력바, 진행바 | DOTween (DOFillAmount) |
| 스피너, 루프 UI 효과 | DOTween |
| 텍스트 타이핑 효과 | DOTween (DOText) |
| 캐릭터 초상화 감정 표현 | Spine SkeletonGraphic |
| 복잡한 골격 애니메이션 | Spine SkeletonGraphic |
| 디자이너 제작 아이콘 애니메이션 | Lottie (SkottiePlayer) |
| 로딩 아이콘 (간단한 경우) | DOTween 또는 Lottie |

---

## 9. UI 파티클 이펙트

### 9.1 Canvas 렌더 모드별 파티클 처리

기본 Unity 파티클 시스템은 **Screen Space - Overlay Canvas에서 동작하지 않는다**. 해결책은 세 가지다.

| 방식 | 장점 | 단점 |
|---|---|---|
| `ParticleEffectForUGUI` 패키지 | 모든 Canvas 모드 지원, Mask 가능, 추가 Camera 불필요 | 커스텀 UI 셰이더 필요 |
| Screen Space - Camera + Camera Stack (URP) | 네이티브 파티클 셰이더 사용 가능 | 추가 Camera 필요, Canvas 모드 변경 필요 |
| World Space Canvas | 간단한 설정 | 화면 크기 추적 스크립트 필요 |

### 9.2 ParticleEffectForUGUI 설치 및 사용

```
// 설치 (packages/manifest.json)
"com.coffee.ui-particle": "https://github.com/mob-sakai/ParticleEffectForUGUI.git"
```

```csharp
// UIParticle 컴포넌트 사용 예시
// GameObject에 UIParticle 컴포넌트 추가, 자식에 ParticleSystem 배치

public class UIParticleEffect : MonoBehaviour
{
    [SerializeField] private UIParticle _uiParticle;

    public void PlayEffect()
    {
        _uiParticle.Play();
    }

    public void StopEffect()
    {
        _uiParticle.Stop();
    }
}
```

**주의사항**:
- 빌트인 셰이더 미지원 → UI 호환 셰이더(예: `UI/Default`, `Particles/Standard Unlit`)로 교체 필요
- Mask 기능을 위해 Stencil 지원 셰이더 필요
- `Mesh Sharing` 옵션으로 동일 이펙트 다수 표시 시 성능 최적화 가능

### 9.3 DOTween + UI 파티클 연동

```csharp
// 화면 전환 중 파티클 이펙트 타이밍 제어
public async UniTask ShowWithParticles(
    CanvasGroup panel, UIParticle particles,
    CancellationToken ct = default)
{
    particles.Play();

    var seq = DOTween.Sequence()
        .Append(panel.DOFade(1f, 0.3f))
        .AppendCallback(() => particles.Stop());

    await seq.AsyncWaitForCompletion().AsUniTask()
        .AttachExternalCancellation(ct);
}
```

---

## 10. 반응형 애니메이션 — 화면 크기 기반 파라미터 조정

### 10.1 Canvas Scaler 기준 스케일 계산

```csharp
/// <summary>
/// Canvas의 scale factor에 기반해 DOTween 애니메이션 파라미터를 정규화한다.
/// 기준 해상도(1080x1920)와 현재 해상도의 차이를 보정.
/// </summary>
public static class ResponsiveAnimation
{
    private static Canvas _rootCanvas;
    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

    public static void Init(Canvas rootCanvas) => _rootCanvas = rootCanvas;

    /// <summary>
    /// 기준 해상도 기준으로 설계된 픽셀 거리를 현재 화면에 맞게 스케일링한다.
    /// </summary>
    public static float ScaleDistance(float designPixels)
    {
        if (_rootCanvas == null) return designPixels;
        float scale = _rootCanvas.scaleFactor;
        return designPixels * scale;
    }

    /// <summary>
    /// 화면 너비를 기준으로 슬라이드 거리를 계산한다.
    /// </summary>
    public static float ScreenWidthUnits()
    {
        if (_rootCanvas == null) return Screen.width;
        return Screen.width / _rootCanvas.scaleFactor;
    }
}

// 사용 예시
float slideDistance = ResponsiveAnimation.ScreenWidthUnits();
rectTransform.DOAnchorPos(new Vector2(slideDistance, 0f), 0.35f);
```

### 10.2 DOTween Duration 조정 (저사양 기기 대응)

```csharp
// 기기 성능에 따른 애니메이션 품질 단계
public enum AnimationQuality { Full, Reduced, Disabled }

public static class AnimationSettings
{
    public static AnimationQuality Quality { get; private set; } = AnimationQuality.Full;

    // 게임 시작 시 기기 성능 기반 설정
    public static void Initialize()
    {
        Quality = SystemInfo.processorFrequency < 1500 || SystemInfo.systemMemorySize < 2048
            ? AnimationQuality.Reduced
            : AnimationQuality.Full;
    }

    /// <summary>
    /// 품질 단계에 따라 duration을 조정한다.
    /// Disabled 시 0을 반환 — DOTween은 duration=0이면 즉시 완료한다.
    /// </summary>
    public static float AdjustDuration(float baseDuration) => Quality switch
    {
        AnimationQuality.Disabled => 0f,
        AnimationQuality.Reduced  => baseDuration * 0.5f,
        _                         => baseDuration,
    };
}

// 실 사용
float duration = AnimationSettings.AdjustDuration(0.3f);
canvasGroup.DOFade(1f, duration);
```

### 10.3 Safe Area 처리와 애니메이션 시작 위치

노치/펀치홀 기기(iPhone X 이상)에서 화면 밖으로 슬라이드하는 애니메이션은 `Screen.safeArea`를 반영해야 한다.

```csharp
// Safe Area를 고려한 슬라이드 시작 위치 계산
Rect safeArea = Screen.safeArea;
float topEdge = (Screen.height - safeArea.yMax) / _rootCanvas.scaleFactor;
float startY = rectTransform.rect.height + topEdge; // 노치 위까지 완전히 숨김
rectTransform.anchoredPosition = new Vector2(0f, startY);
rectTransform.DOAnchorPos(Vector2.zero, 0.35f).SetEase(Ease.OutCubic);
```

---

## 11. 프로젝트 내 기존 구현 참고

현재 UI_Study 프로젝트에 이미 구현된 DOTween 패턴:

### `AnimatedPanelView.cs` — CanvasGroup + Scale 복합 Show/Hide

경로: `UI_Study/Assets/_Study/01-MVRP-Foundation/Scripts/Views/AnimatedPanelView.cs`

- `DOTween.Sequence().Join(fade).Join(scale)` 복합 애니메이션
- `AsyncWaitForCompletion().AsUniTask()` + `AttachExternalCancellation` 패턴
- 기존 Sequence `Kill()` 후 새 Sequence 시작하는 안전한 교체 패턴
- `OnComplete`에서 `interactable` 복원

### `AnimatedBarView.cs` — 체력바 2-tier 애니메이션

경로: `UI_Study/Assets/_Study/05-Advanced-Patterns/Scripts/Views/AnimatedBarView.cs`

- 데미지/힐 방향에 따른 전경/배경 레이어 전환
- `DOFillAmount` + `SetEase(Ease.InQuad / OutQuad)` 방향별 이징
- `SetUpdate(true)` — timeScale 독립
- `DOPunchScale` — 큰 변화(>= threshold) 시 스케일 피드백
- `_trailingTween?.Kill()` — 안전한 트윈 교체

---

## 12. 요약 치트시트

### DOTween 안전한 사용 패턴

```csharp
// 1. Kill 후 새 트윈 시작 (덮어쓰기 안전)
transform.DOKill();
transform.DOScale(1.1f, 0.2f);

// 2. Sequence 안전한 교체
_seq?.Kill();
_seq = DOTween.Sequence()...;

// 3. UniTask await
await tween.AsyncWaitForCompletion().AsUniTask()
    .AttachExternalCancellation(ct);

// 4. timeScale 독립 UI 애니메이션
tween.SetUpdate(true);

// 5. OnDestroy 정리
private void OnDestroy()
{
    transform.DOKill();
    _seq?.Kill();
}

// 6. 캐싱된 트윈 (자주 재사용)
_tween = transform.DOPunchScale(...)
    .SetAutoKill(false).Pause();
// 재사용 시
_tween.Restart();
```

---

## 13. 참고 자료

- [DOTween 공식 문서](https://dotween.demigiant.com/documentation.php)
- [DOTweenModuleUI 소스 (프로젝트 내)](../../../UI_Study/Assets/Plugins/Demigiant/DOTween/Modules/DOTweenModuleUI.cs)
- [UnityScreenNavigator GitHub](https://github.com/Haruma-K/UnityScreenNavigator)
- [UnityScreenNavigator README — 트랜지션 애니메이션 섹션](https://github.com/Haruma-K/UnityScreenNavigator/blob/master/README.md)
- [ParticleEffectForUGUI GitHub (mob-sakai)](https://github.com/mob-sakai/ParticleEffectForUGUI)
- [PrimeTween GitHub (KyryloKuzyk)](https://github.com/KyryloKuzyk/PrimeTween)
- [PrimeTween vs DOTween 성능 비교](https://github.com/KyryloKuzyk/PrimeTween/discussions/8)
- [LitMotion GitHub (annulusgames)](https://github.com/annulusgames/LitMotion)
- [LitMotion 성능 비교 (TweenPerformance)](https://github.com/annulusgames/TweenPerformance)
- [DOTween Configs (ScriptableObject 패턴)](https://github.com/rfadeev/dotween-configs)
- [Spine-Unity 공식 문서](http://en.esotericsoftware.com/spine-unity)
- [SkiaForUnity (Lottie/Skottie)](https://github.com/ammariqais/SkiaForUnity)
- [DeepWiki DOTween UI 컴포넌트 애니메이션](https://deepwiki.com/Demigiant/dotween/3.2-ui-component-animations)
