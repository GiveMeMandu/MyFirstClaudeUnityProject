# 게임 UI 패턴 심층 리서치

- **작성일**: 2026-03-28
- **카테고리**: pattern
- **상태**: 조사완료

---

## 요약

게임 UI 12개 핵심 패턴을 MV(R)P + VContainer + R3 + UniTask 스택 기준으로 조사했다.
각 패턴의 아키텍처 분리 방법, DI 통합 포인트, 리액티브 바인딩 기회, 재사용 컴포넌트 설계를 정리한다.
오픈소스 레퍼런스 프로젝트 및 주요 라이브러리 API도 함께 기록한다.

---

## 1. HUD 패턴 — 체력바, 리소스 카운터, 미니맵, 나침반, 데미지 숫자

### 개요

HUD는 게임의 핵심 상태를 항상 표시하는 Overlay Canvas이다.
성능 관점에서 **자주 갱신되는 요소(체력, 리소스)와 정적 요소(테두리, 레이블)를 별도 Canvas로 분리**하는 것이 필수다.
Unity는 Canvas 내 어느 요소라도 변경되면 전체 Canvas를 dirty로 표시하고 재드로우하기 때문이다.

### 아키텍처 분리 (MV(R)P)

```
Model  : PlayerStatsModel — ReactiveProperty<float> HP, ReactiveProperty<int> Gold, etc.
View   : HudView (MonoBehaviour) — Slider, TMP_Text, RawImage (미니맵) 레퍼런스 보유
Presenter : HudPresenter (IStartable) — Model Observable → View 업데이트
```

### VContainer 통합

```csharp
public class HudLifetimeScope : LifetimeScope
{
    [SerializeField] private HudView _hudView;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_hudView);
        builder.Register<PlayerStatsModel>(Lifetime.Scoped);
        builder.RegisterEntryPoint<HudPresenter>();
    }
}
```

### R3 바인딩

```csharp
// HudPresenter.cs (IStartable)
public class HudPresenter : IStartable, IDisposable
{
    private readonly HudView _view;
    private readonly PlayerStatsModel _model;
    private readonly CompositeDisposable _disposables = new();

    public void Start()
    {
        _model.HP
            .Subscribe(hp => _view.SetHP(hp, _model.MaxHP.Value))
            .AddTo(_disposables);

        _model.Gold
            .Subscribe(g => _view.SetGoldText(g))
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
```

### 체력바 구현 전략

| 방식 | 장점 | 단점 | 추천 용도 |
|---|---|---|---|
| Slider component | 빠른 설정 | 커스텀 제한 | 프로토타입 |
| Image.fillAmount (Filled) | 제어 자유 | Sprite 필수 할당 | 메인 HUD |
| annulusgames/UnityProgressBar | FillMode.Stretch 지원, ProgressBarBase 상속 가능 | 외부 패키지 | 세련된 바 |

annulusgames/UnityProgressBar API:
- `ProgressBar.Min`, `ProgressBar.Max`, `ProgressBar.Value` 직접 설정
- `OnValueChanged` 이벤트 제공
- `FillMode.FillAmount` / `FillMode.Stretch` 두 가지 방식

### 미니맵 (RenderTexture 방식)

```
1. 오버헤드 카메라 생성 → Target Texture = MinimapRenderTexture
2. Canvas > Raw Image → Texture = MinimapRenderTexture
3. 미니맵 전용 Layer 생성 → 미니맵 카메라의 Culling Mask에만 포함
4. 플레이어/적 아이콘은 해당 Layer로 설정
```

MinimapModel은 플레이어 월드 위치를 ReactiveProperty로 노출,
MinimapPresenter가 카메라 위치를 동기화.

### 나침반

나침반 UI는 RectTransform.anchoredPosition 또는 UV Offset을 이용해 방향 텍스처를 수평 스크롤.
카메라 Y축 회전값을 Model의 ReactiveProperty로 노출 → Presenter가 UV offset 계산 후 View 갱신.

### 재사용 컴포넌트 설계

```
IResourceBar          — SetValue(float current, float max)
ICounterDisplay       — SetValue(int value)
IMinimapView          — SetWorldBounds(Bounds), SetTrackedTargets(IList<MinimapTarget>)
```

---

## 2. 인벤토리/그리드 UI — 드래그&드롭, 그리드 레이아웃, 아이템 툴팁, 정렬/필터

### 아키텍처 분리

```
Model  : InventoryModel — ReactiveCollection<ItemData> items
View   : InventoryView — GridLayoutGroup, SlotView[] 관리
Presenter : InventoryPresenter — 드래그 이벤트 수신 → Model 업데이트 → View 갱신
SlotView : IBeginDragHandler, IDragHandler, IEndDragHandler 구현
```

### VContainer 통합

팩토리 패턴으로 SlotView를 동적 생성:

```csharp
builder.RegisterFactory<ItemData, SlotView>(container =>
    itemData =>
    {
        var slot = Object.Instantiate(_slotPrefab, _gridParent);
        slot.Bind(itemData);
        return slot;
    }, Lifetime.Scoped);
```

### R3 바인딩

```csharp
// InventoryPresenter
_model.Items
    .ObserveAdd()
    .Subscribe(e => _view.AddSlot(e.Value))
    .AddTo(_disposables);

_model.Items
    .ObserveRemove()
    .Subscribe(e => _view.RemoveSlot(e.Index))
    .AddTo(_disposables);
```

### 드래그&드롭 패턴

Unity UGUI 드래그 앤 드롭은 `IDragHandler` 인터페이스 트리오를 구현:

```
IBeginDragHandler.OnBeginDrag  → DragImage 생성, 원본 슬롯 alpha 줄이기
IDragHandler.OnDrag            → DragImage.position = Input position
IEndDragHandler.OnEndDrag      → 대상 슬롯 탐지(RaycastAll) → Presenter에 스왑 요청
```

Canvas에 `GraphicRaycaster` 필수, `EventSystem` 필수.
드래그 중 이미지를 Canvas 최상단(Raycast Block 포함)으로 임시 이동.

오픈소스 레퍼런스:
- `github.com/tjcccc/ugui-dragdrop` — UGUI 드래그&드롭 포지션 스왑
- `github.com/cemuka/InventorySystem` — 시그널 기반 이벤트, 기본 드래그앤드롭

### 정렬/필터

```csharp
// R3 + LINQ 조합
_filterQuery                            // ReactiveProperty<string>
    .CombineLatest(_sortOrder, (q, s) => (q, s))
    .Subscribe(t => _view.Refresh(_model.GetFiltered(t.q, t.s)))
    .AddTo(_disposables);
```

### 툴팁 연동

SlotView에서 IPointerEnterHandler/IPointerExitHandler 구현,
TooltipPresenter에게 ItemData 전달 → 별도 Canvas Overlay에 Tooltip 표시.
(ugui-tooltip-positioning.md 008 참고)

---

## 3. 대화/컨버세이션 시스템 UI — 타이프라이터, 선택지, 초상화, 분기

### 아키텍처 분리

```
Model  : DialogModel — 현재 노드(DialogNode), 선택지 목록, 화자 정보
View   : DialogView — 텍스트 박스, 초상화 Image, 선택지 버튼 목록
Presenter : DialogPresenter (IStartable, IDisposable) — 진행 제어, 타이프라이터 실행
```

### 타이프라이터 효과 (UniTask + TextMeshPro)

TextMeshPro의 `maxVisibleCharacters`를 활용한 UniTask 구현:

```csharp
public async UniTask TypeText(string text, float charInterval,
    CancellationToken ct = default)
{
    _tmp.text = text;
    _tmp.maxVisibleCharacters = 0;

    for (int i = 0; i <= text.Length; i++)
    {
        _tmp.maxVisibleCharacters = i;
        await UniTask.Delay(
            TimeSpan.FromSeconds(charInterval),
            cancellationToken: ct);
    }
}
```

CancellationToken을 넘겨 `OnClickAnywhere`로 스킵 가능:

```csharp
// Presenter
_view.OnAnyClick
    .FirstAsync(ct: _cts.Token)
    .Subscribe(_ => _cts.Cancel());
```

### VContainer 통합

```csharp
builder.Register<DialogModel>(Lifetime.Scoped);
builder.RegisterComponent(_dialogView);
builder.RegisterEntryPoint<DialogPresenter>();
// 씬/팝업 단위로 ChildScope 생성 권장
```

### 분기 선택지 버튼 동적 생성

```csharp
// View
public void ShowChoices(IReadOnlyList<ChoiceData> choices)
{
    ClearChoices();
    foreach (var c in choices)
    {
        var btn = Instantiate(_choicePrefab, _choiceContainer);
        btn.SetLabel(c.Text);
        btn.OnClick.Subscribe(_ => _onChoiceSelected.OnNext(c.Id))
                   .AddTo(_choiceDisposables);
    }
}
```

### 초상화 크로스페이드

DOTween으로 현재 초상화 alpha 0 → 새 초상화 alpha 1:

```csharp
public UniTask SwapPortrait(Sprite next, float duration = 0.3f)
{
    _portrait.sprite = next;
    _portrait.color = Color.clear;
    return _portrait.DOFade(1f, duration).ToUniTask();
}
```

### 레퍼런스 라이브러리

- **Yarn Spinner**: `yarn.unity.effects.Typewriter(TMP, charDelay, onFinish, interruptToken)` 제공하는 완성도 높은 대화 시스템
- `github.com/redbluegames/unity-text-typer` — Rich text 태그 지원 타이프라이터

---

## 4. 팝업/알림 시스템 — 토스트, 확인 다이얼로그, 팝업 스태킹/우선순위 큐

### 아키텍처 분리

```
Model  : PopupQueueModel — Queue<PopupData>, 우선순위 정렬 로직
Service : PopupService — PushPopup(data), GetOpenedPopup()
View   : 각 팝업 프리팹 (ToastView, ConfirmView, AlertView)
Factory : PopupFactory — VContainer RegisterFactory, 프리팹별 생성
```

### 우선순위 큐 설계

```csharp
public enum PopupPriority { Low = 1, Medium = 2, High = 3, Urgent = 4 }

public class PopupQueueModel
{
    private readonly List<PopupData> _queue = new();
    private PopupData _hiddenPopup;   // Urgent 인터럽트 시 일시 보관

    public void Enqueue(PopupData data)
    {
        if (data.Priority == PopupPriority.Urgent && _activePopup != null)
        {
            _hiddenPopup = _activePopup;
            CloseActive();
        }
        _queue.Add(data);
        TryShowNext();
    }

    private PopupData DequeueHighest()
        => _queue.OrderByDescending(p => p.Priority).First();
}
```

### VContainer ChildScope 방식

각 팝업을 별도 LifetimeScope child로 생성 → 팝업 닫힐 때 Scope dispose:

```csharp
// PopupService
public async UniTask<TResult> ShowAsync<TPopup, TResult>(PopupData data)
    where TPopup : IPopupView<TResult>
{
    using var scope = _rootScope.CreateChildFromPrefab(_popupPrefab);
    var popup = scope.Container.Resolve<TPopup>();
    popup.Show(data);
    return await popup.OnClosedAsync(_cts.Token);
}
```

### R3 스태킹 상태 관리

```csharp
public class PopupStackModel
{
    public readonly ReactiveProperty<int> StackDepth = new(0);
    public readonly Subject<PopupData> OnPopupOpened = new();
    public readonly Subject<PopupData> OnPopupClosed = new();
}
```

### 토스트 알림 패턴

토스트는 우선순위 큐 없이 독립 표시. 큐에 쌓이며 순차 표시:

```
ToastQueue — UniTask while loop, 각 토스트 표시 후 await DOTween 완료
```

오픈소스: `github.com/herbou/Unity_ToastUI` — 크로스 플랫폼 토스트 UI

---

## 5. 탭/패널 내비게이션 — 탭 바, 사이드 패널, 하단 내비, 브레드크럼

### UnityScreenNavigator (Haruma-K)

프로젝트 확정 기술 스택에 포함된 USN의 3가지 화면 타입:

| 타입 | 역할 | 히스토리 | 동시 표시 |
|---|---|---|---|
| Page | 순차 전환, 뒤로가기 지원 | O (스택) | X (한 번에 하나) |
| Modal | 블로킹 오버레이 | O (스택) | O (레이어 쌓기) |
| Sheet | 탭 UI, 상태 보존 | X | X (하나만 활성) |

```csharp
// 탭 전환 — SheetContainer 사용
await SheetContainer.Of(transform).Show("tab_inventory", animate: true);

// 페이지 푸시
await PageContainer.Of(transform).Push("shop_page", animate: true);

// 모달 닫기
await ModalContainer.Of(transform).Pop(animate: true);
```

AsyncProcessHandle로 완료 대기:
```csharp
var handle = PageContainer.Of(transform).Push("shop_page");
await handle.Task;  // UniTask 방식
```

### VContainer 통합

USN에서 각 페이지/모달/시트 프리팹에 LifetimeScope를 붙여 자동 DI:

```csharp
// ShopPage.cs
public class ShopPage : Page, ISerializationCallbackReceiver
{
    [SerializeField] private ShopLifetimeScope _scope;
    // USN의 DidEnterForeground, WillExitForeground 라이프사이클과 연동
}
```

### 브레드크럼 패턴

```csharp
public class BreadcrumbModel
{
    public readonly ReactiveCollection<PageInfo> Trail = new();
}
// 페이지 Push → Trail.Add, Pop → Trail.RemoveLast
// BreadcrumbView가 Trail.ObserveCountChanged로 자동 업데이트
```

---

## 6. 진행/로딩 UI — 프로그레스 바, 로딩 화면, 비동기 작업 트래킹

### 아키텍처 분리

```
Model  : LoadingModel — ReactiveProperty<float> Progress (0~1), ReactiveProperty<string> StatusMessage
View   : LoadingView — Slider or ProgressBar, TMP_Text
Presenter : LoadingPresenter — AsyncOperation 진행률 폴링 or UniTask Progress 콜백
```

### UniTask + IProgress 패턴

```csharp
public async UniTask LoadSceneWithProgress(string sceneName, CancellationToken ct)
{
    var progress = Progress.Create<float>(p => _model.Progress.Value = p);

    var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    op.allowSceneActivation = false;

    await op.ToUniTask(progress: progress, cancellationToken: ct);

    // 로딩 완료 후 전환 연출
    _model.Progress.Value = 1f;
    await UniTask.Delay(500, cancellationToken: ct); // 100% 잠깐 표시
    op.allowSceneActivation = true;
}
```

### R3 진행률 바인딩

```csharp
_model.Progress
    .Select(p => Mathf.RoundToInt(p * 100))
    .Subscribe(pct => _view.SetProgressText($"{pct}%"))
    .AddTo(_disposables);

_model.StatusMessage
    .Subscribe(_view.SetStatusText)
    .AddTo(_disposables);
```

### annulusgames/UnityProgressBar

```csharp
_progressBar.Min = 0f;
_progressBar.Max = 1f;
// R3 바인딩
_model.Progress.Subscribe(p => _progressBar.Value = p).AddTo(_disposables);
```

FillMode.FillAmount → Image.fillAmount 직접 제어
FillMode.Stretch → RectTransform width 비율 조정

### 다중 작업 트래킹

```csharp
public class MultiTaskLoadingModel
{
    private readonly float[] _taskProgress;
    public readonly ReactiveProperty<float> TotalProgress = new(0f);

    public void UpdateTask(int index, float progress)
    {
        _taskProgress[index] = progress;
        TotalProgress.Value = _taskProgress.Average();
    }
}
```

---

## 7. 설정 메뉴 패턴 — 슬라이더, 토글, 드롭다운, 키바인딩, 오디오 설정

### 아키텍처 분리

```
Model  : SettingsModel — 각 설정값을 SerializableReactiveProperty로 노출 (Inspector 편집 가능)
View   : SettingsView — Slider, Toggle, TMP_Dropdown, 키바인딩 버튼 레퍼런스
Presenter : SettingsPresenter — 양방향 바인딩 (View 변경 → Model 저장, Model 로드 → View 초기화)
```

### R3 양방향 바인딩

```csharp
// 슬라이더 → 모델
_view.VolumeSlider.OnValueChangedAsObservable()
    .Subscribe(v => _model.MasterVolume.Value = v)
    .AddTo(_disposables);

// 모델 → 슬라이더 (초기화 및 외부 변경 반영)
_model.MasterVolume
    .Subscribe(v => _view.VolumeSlider.SetValueWithoutNotify(v))
    .AddTo(_disposables);
```

`SetValueWithoutNotify` 사용으로 피드백 루프 방지.

### 드롭다운 바인딩

```csharp
_view.QualityDropdown.OnValueChangedAsObservable()
    .Subscribe(i => _model.QualityLevel.Value = i)
    .AddTo(_disposables);

_model.QualityLevel
    .Subscribe(i => _view.QualityDropdown.SetValueWithoutNotify(i))
    .AddTo(_disposables);
```

### 키바인딩 리바인딩 (New Input System)

```csharp
public async UniTask StartRebind(string actionName, int bindingIndex,
    CancellationToken ct = default)
{
    var action = _inputActions.FindAction(actionName);
    action.Disable();

    var op = action.PerformInteractiveRebinding(bindingIndex)
        .OnComplete(op =>
        {
            op.Dispose();
            SaveBindings();
        })
        .Start();

    await UniTask.WaitUntil(() => !op.started, cancellationToken: ct);
    action.Enable();
}

private void SaveBindings()
{
    var json = _inputActions.SaveBindingOverridesAsJson();
    PlayerPrefs.SetString("InputBindings", json);
}

private void LoadBindings()
{
    var json = PlayerPrefs.GetString("InputBindings", "");
    if (!string.IsNullOrEmpty(json))
        _inputActions.LoadBindingOverridesFromJson(json);
}
```

Unity Input System Package에 "Rebinding UI" 샘플 포함 (Package Manager에서 임포트 가능).

### 오디오 설정 패턴

```csharp
// AudioMixer와 연동
_model.MasterVolume
    .Subscribe(v => _audioMixer.SetFloat("MasterVolume",
        Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f))
    .AddTo(_disposables);
```

---

## 8. 월드 스페이스 UI — 적 체력바, 플로팅 데미지 숫자, 인터랙션 프롬프트

### 구현 방식 비교

| 방식 | 장점 | 단점 | 추천 용도 |
|---|---|---|---|
| World Space Canvas (적마다 부착) | 원근 자동 처리 | Canvas 수 증가 → 드로우콜 비용 | 적 체력바 |
| Screen Space Overlay + WorldToScreen | 단일 Canvas, 드로우콜 최소화 | 좌표 변환 코드 필요 | 데미지 숫자 |
| Billboard (카메라 추적) | 3D 공간에서 자연스러움 | 별도 스크립트 필요 | 인터랙션 프롬프트 |

### 적 체력바 (World Space Canvas)

```csharp
// EnemyHUDView.cs — 각 Enemy에 부착된 World Space Canvas 자식
public class EnemyHUDView : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private Canvas _canvas;

    private void Awake()
    {
        // Canvas scale = 0.01f로 설정 (월드 단위 기준)
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;
    }

    public void SetHP(float normalized) => _fillImage.fillAmount = normalized;
}
```

Billboard 회전:
```csharp
void LateUpdate()
{
    transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                     Camera.main.transform.rotation * Vector3.up);
}
```

### 플로팅 데미지 숫자 (오브젝트 풀 + DOTween)

풀 기반 설계:

```csharp
public class DamageNumberPool : MonoBehaviour
{
    [SerializeField] private DamageNumberView _prefab;
    private readonly Stack<DamageNumberView> _pool = new();

    public async UniTaskVoid Spawn(Vector3 worldPos, int damage, Color color,
        CancellationToken ct = default)
    {
        var item = _pool.Count > 0 ? _pool.Pop() : Instantiate(_prefab);
        item.gameObject.SetActive(true);

        // 월드 → 스크린 → 캔버스 좌표 변환
        var screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, null, out var localPos);
        item.RectTransform.anchoredPosition = localPos;

        item.SetText(damage.ToString(), color);

        await item.PlayAnimation(ct);    // DOTween: 위로 이동 + fade out

        item.gameObject.SetActive(false);
        _pool.Push(item);
    }
}
```

DOTween 내부에도 tween 풀이 있어 이중 풀링 효과.

오픈소스: `github.com/bryjch/unity-easy-damage-numbers` — SimplePool.cs 기반, Animator 방식, constantScale/scaleWithDistance 지원

### 인터랙션 프롬프트

월드 오브젝트 위 "E 키 상호작용" 표시:
```
IInteractable 인터페이스 → OnPlayerNearby 이벤트
→ InteractionPromptPresenter: WorldToScreenPoint 계산
→ 단일 CanvasOverlay에서 prompt RectTransform 위치 설정
```

---

## 9. 래디얼/파이 메뉴 — 원형 선택 메뉴

### 아키텍처 분리

```
Model  : RadialMenuModel — ReactiveProperty<int> HoveredIndex, List<RadialItemData> items
View   : RadialMenuView — 원형 배치된 RadialItemView[], 중앙 레이블
Presenter : RadialMenuPresenter — 마우스/스틱 방향 → 각도 → 인덱스 계산 → 선택 처리
```

### 원형 배치 계산

```csharp
public void ArrangeItems(int count)
{
    float angleStep = 360f / count;
    for (int i = 0; i < count; i++)
    {
        float angle = i * angleStep * Mathf.Deg2Rad;
        float x = Mathf.Sin(angle) * _radius;
        float y = Mathf.Cos(angle) * _radius;
        _items[i].RectTransform.anchoredPosition = new Vector2(x, y);
    }
}
```

### 마우스/스틱 입력 → 인덱스 변환

```csharp
// R3 + New Input System
_inputActions.RadialMenu.Point.AsObservable()
    .Select(ctx => ctx.ReadValue<Vector2>())
    .Where(dir => dir.magnitude > _deadzone)
    .Select(dir =>
    {
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        return Mathf.FloorToInt(angle / (360f / _model.ItemCount));
    })
    .DistinctUntilChanged()
    .Subscribe(idx => _model.HoveredIndex.Value = idx)
    .AddTo(_disposables);
```

### 열기/닫기 애니메이션 (DOTween)

```csharp
public UniTask OpenAsync()
{
    _group.alpha = 0;
    _group.gameObject.SetActive(true);
    return DOTween.Sequence()
        .Append(_group.DOFade(1f, 0.15f))
        .Join(transform.DOScale(1f, 0.15f).From(0.8f))
        .ToUniTask();
}
```

### 레퍼런스

- `github.com/psvantares/unity_circle_menu` — RadialLayout.cs 기반, iOS/Android/PC/WebGL 지원
- ameye.dev — UI Toolkit Vector API로 에디터 원형 메뉴 구현 예제

---

## 10. 튜토리얼/온보딩 UI — 하이라이트 마스크, 단계별 가이드, 스포트라이트

### 아키텍처 분리

```
Model  : TutorialModel — ReactiveProperty<TutorialStep> CurrentStep, 단계 목록
View   : TutorialOverlayView — TutorialMask, 가이드 텍스트 패널, 화살표 인디케이터
Presenter : TutorialPresenter (IStartable) — 단계 진행, 완료 조건 감시
```

### TutorialMaskForUGUI (codewriter-packages)

```
설치: git package — https://github.com/codewriter-packages/TutorialMaskForUGUI.git

구성:
1. 전체 화면 Image 오브젝트 → TutorialMask 컴포넌트 추가 (어두운 오버레이)
2. 하이라이트할 각 UI 오브젝트 → TutorialObject 컴포넌트 추가
3. Mask와 함께 사용 시 MaskFixForTutorial 추가 (스텐실 버퍼 충돌 해결)
```

Unity 최소 버전 2021.3+.

### 스포트라이트 효과 (Shader/Custom Material 방식)

Stencil mask 없이 RectMask2D + Overlay Image로 구현:

```csharp
// TutorialSpotlightView.cs
public void FocusOn(RectTransform target)
{
    // target의 월드 → 로컬 좌표 변환
    var corners = new Vector3[4];
    target.GetWorldCorners(corners);
    // _maskRect 크기/위치를 corners 기반으로 조정
    // DOTween으로 부드럽게 이동
}
```

SoftMaskForUGUI (`github.com/mob-sakai/SoftMaskForUGUI`) — 소프트 마스킹, 부드러운 엣지 효과.

### 단계 진행 관리

```csharp
public async UniTask RunTutorial(CancellationToken ct)
{
    foreach (var step in _model.Steps)
    {
        _model.CurrentStep.Value = step;

        // 목표 UI 하이라이트
        _view.HighlightTarget(step.TargetUI);

        // 완료 조건 대기 (R3 Observable로 감시)
        await step.CompletionTrigger
            .FirstAsync(cancellationToken: ct);

        _view.ClearHighlight();

        await UniTask.Delay(300, cancellationToken: ct); // 다음 단계 전 잠깐 대기
    }
}
```

### Unity 공식 Tutorial Framework

`com.unity.learn.iet-framework` 패키지: Highlighting Guide 제공.
에디터용 튜토리얼에 최적화되어 있으나 런타임 튜토리얼에는 직접 구현 추천.

---

## 11. 리더보드/랭킹 UI — 스크롤 리스트, 플레이어 데이터, 페이지네이션

### 아키텍처 분리

```
Model  : LeaderboardModel — ReactiveProperty<IReadOnlyList<RankEntry>> Entries,
                            ReactiveProperty<int> CurrentPage
Service : LeaderboardService — API 호출, 데이터 파싱
View   : LeaderboardView — FancyScrollView 또는 RecyclingListView
Presenter : LeaderboardPresenter — 데이터 로드 → View 업데이트, 페이지 전환
```

### FancyScrollView 구현 패턴

`github.com/setchi/FancyScrollView` — 셀 가상화, 고유연 애니메이션.

```csharp
// 1. 데이터 클래스 정의
public class RankEntryData
{
    public int Rank;
    public string PlayerName;
    public int Score;
}

// 2. 셀 구현
public class RankCell : FancyCell<RankEntryData>
{
    [SerializeField] private TMP_Text _rankText, _nameText, _scoreText;

    public override void UpdateContent(RankEntryData data)
    {
        _rankText.text = $"#{data.Rank}";
        _nameText.text = data.PlayerName;
        _scoreText.text = data.Score.ToString("N0");
    }

    // 스크롤 위치 기반 시각 효과 (선택)
    public override void UpdatePosition(float position)
    {
        float scale = Mathf.Lerp(0.8f, 1f, 1f - Mathf.Abs(position - 0.5f) * 2f);
        transform.localScale = Vector3.one * scale;
    }
}

// 3. 스크롤 뷰 구현
public class LeaderboardScrollView : FancyScrollView<RankEntryData>
{
    [SerializeField] private Scroller _scroller;

    protected override void Initialize()
    {
        base.Initialize();
        _scroller.OnValueChanged(UpdatePosition);
    }
}
```

### R3 바인딩

```csharp
_model.Entries
    .Subscribe(entries => _view.UpdateData(entries))
    .AddTo(_disposables);

_model.CurrentPage
    .Subscribe(page => _view.SetPageIndicator(page, _model.TotalPages))
    .AddTo(_disposables);
```

### 페이지네이션 패턴

```csharp
// 페이지 버튼 클릭 → Subject 발행
_view.OnNextPageClicked
    .Where(_ => _model.CurrentPage.Value < _model.TotalPages)
    .Subscribe(_ => _model.CurrentPage.Value++)
    .AddTo(_disposables);

// 페이지 변경 → 데이터 로드
_model.CurrentPage
    .SelectMany(page => _leaderboardService.FetchPageAsync(page).ToObservable())
    .Subscribe(entries => _model.Entries.Value = entries)
    .AddTo(_disposables);
```

### UnityRecyclingListView

`github.com/sinbad/UnityRecyclingListView` — 셀 재사용 RecyclingListView.
델리게이트 콜백으로 데이터 바인딩, 대량 데이터셋에 적합.

---

## 12. 스토어/샵 UI — 아이템 카드, 화폐 표시, 구매 확인 플로우

### 아키텍처 분리

```
Model  : ShopModel — ReactiveProperty<Currency> PlayerCurrency,
                     ReactiveProperty<IReadOnlyList<ShopItemData>> Items,
                     ReactiveProperty<ShopItemData> SelectedItem
Service : ShopService — 구매 처리, 서버/로컬 재고 관리
View   : ShopView — 아이템 그리드, 통화 표시, 선택 패널, 구매 버튼
Presenter : ShopPresenter — 선택/구매 로직, 잔액 부족 피드백
```

### 아이템 카드 동적 생성

```csharp
// ShopPresenter
_model.Items
    .Subscribe(items =>
    {
        _view.ClearCards();
        foreach (var item in items)
        {
            var card = _cardFactory(item);  // VContainer RegisterFactory
            card.OnSelected
                .Subscribe(_ => _model.SelectedItem.Value = item)
                .AddTo(_cardDisposables);
        }
    })
    .AddTo(_disposables);
```

### 구매 가능 여부 리액티브 계산

```csharp
// CombineLatest로 파생 상태 계산
Observable.CombineLatest(
    _model.SelectedItem,
    _model.PlayerCurrency,
    (item, currency) => item != null && currency >= item.Price)
    .Subscribe(canBuy =>
    {
        _view.SetBuyButtonInteractable(canBuy);
        _view.SetBuyButtonColor(canBuy ? _enabledColor : _disabledColor);
    })
    .AddTo(_disposables);
```

### 구매 확인 팝업 플로우

```csharp
// ShopPresenter
_view.OnBuyClicked
    .Where(_ => _model.SelectedItem.Value != null)
    .SelectMany(_ => _popupService.ShowConfirmAsync(
        title: "구매 확인",
        message: $"{_model.SelectedItem.Value.Name}을 구매하시겠습니까?")
        .ToObservable())
    .Where(confirmed => confirmed)
    .Subscribe(_ => PurchaseSelected())
    .AddTo(_disposables);
```

### 화폐 표시 애니메이션

구매 후 통화 변화를 숫자 롤업 효과로 표시 (DOTween):

```csharp
public UniTask AnimateCurrencyChange(int from, int to, float duration = 0.5f)
{
    return DOTween.To(
        () => from,
        v => _currencyText.text = v.ToString("N0"),
        to,
        duration)
        .SetEase(Ease.OutCubic)
        .ToUniTask();
}
```

### Unity 공식 가상 상점 샘플

`github.com/Unity-Technologies/com.unity.services.samples.use-cases`의 Virtual Shop 샘플:
Unity Gaming Services + Economy 통합 완성 예제.

---

## 공통 재사용 컴포넌트 설계 원칙

### 인터페이스 계층

```csharp
// 모든 View의 공통 계약
public interface IView
{
    void Show();
    void Hide();
    UniTask ShowAsync(CancellationToken ct = default);
    UniTask HideAsync(CancellationToken ct = default);
}

// 데이터 바인딩 가능 View
public interface IView<TData> : IView
{
    void Bind(TData data);
}

// 결과를 반환하는 View (확인 다이얼로그, 팝업 등)
public interface IView<TData, TResult> : IView<TData>
{
    Observable<TResult> OnResult { get; }
}
```

### Presenter 기본 클래스

```csharp
public abstract class PresenterBase : IStartable, IDisposable
{
    protected readonly CompositeDisposable Disposables = new();

    public abstract void Start();

    public virtual void Dispose() => Disposables.Dispose();
}
```

### 공통 LifetimeScope 패턴

```csharp
public abstract class UILifetimeScope<TView, TModel, TPresenter> : LifetimeScope
    where TView : MonoBehaviour
    where TPresenter : PresenterBase
{
    [SerializeField] protected TView View;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(View);
        RegisterModel(builder);
        builder.RegisterEntryPoint<TPresenter>();
    }

    protected abstract void RegisterModel(IContainerBuilder builder);
}
```

---

## 오픈소스 레퍼런스 프로젝트

| 프로젝트 | 용도 | URL |
|---|---|---|
| UnityScreenNavigator | 화면 전환/탭/모달 | github.com/Haruma-K/UnityScreenNavigator |
| FancyScrollView | 가상화 스크롤 리스트 | github.com/setchi/FancyScrollView |
| TutorialMaskForUGUI | 튜토리얼 하이라이트 마스크 | github.com/codewriter-packages/TutorialMaskForUGUI |
| SoftMaskForUGUI | 소프트 마스킹 효과 | github.com/mob-sakai/SoftMaskForUGUI |
| UnityProgressBar | 프로그레스 바 컴포넌트 | github.com/annulusgames/UnityProgressBar |
| Unity_ToastUI | 토스트 알림 UI | github.com/herbou/Unity_ToastUI |
| unity-easy-damage-numbers | 플로팅 데미지 숫자 | github.com/bryjch/unity-easy-damage-numbers |
| UnityRecyclingListView | 셀 재사용 리스트 | github.com/sinbad/UnityRecyclingListView |
| unity-text-typer | Rich text 타이프라이터 | github.com/redbluegames/unity-text-typer |
| Unity MVP with VContainer | VContainer MVP 예제 | github.com/NorthTH/Unity-MVP-with-Vcontainer |
| UI-Framework (laphedhendad) | 리액티브 MVP 프레임워크 | github.com/laphedhendad/UI-Framework |
| ugui-dragdrop | 드래그앤드롭 스왑 | github.com/tjcccc/ugui-dragdrop |
| unity_circle_menu | 래디얼 메뉴 | github.com/psvantares/unity_circle_menu |
| use-cases (Virtual Shop) | Unity GS 가상 상점 | github.com/Unity-Technologies/com.unity.services.samples.use-cases |

---

## 패턴별 핵심 정리표

| 패턴 | 핵심 R3 연산자 | 핵심 VContainer 기능 | 주요 성능 주의점 |
|---|---|---|---|
| HUD | Subscribe, DistinctUntilChanged | RegisterEntryPoint | Canvas 분리 (동적/정적) |
| 인벤토리 | ObserveAdd/Remove, CombineLatest | RegisterFactory | GridLayoutGroup rebuild 최소화 |
| 대화 시스템 | FirstAsync, Subject | ChildScope per dialog | maxVisibleCharacters 갱신 빈도 |
| 팝업 큐 | OrderByDescending, Subject | CreateChildFromPrefab | 동시 Canvas 수 제한 |
| 탭 내비 | ObserveCountChanged | 씬별 LifetimeScope | Sheet 상태 보존 메모리 |
| 로딩 화면 | Select, Subscribe | IAsyncStartable | allowSceneActivation 타이밍 |
| 설정 메뉴 |양방향 + SetValueWithoutNotify | Singleton Settings | PlayerPrefs 직렬화 |
| 월드 UI | EveryValueChanged (폴링) | Factory per enemy | World Space Canvas 드로우콜 |
| 래디얼 메뉴 | DistinctUntilChanged, Atan2 | Transient per open | 각도 계산 주파수 |
| 튜토리얼 | FirstAsync per step | Scoped TutorialScope | Stencil 버퍼 충돌 |
| 리더보드 | SelectMany (페이지 로드) | RegisterInstance data | 셀 가상화 필수 |
| 샵 UI | CombineLatest (구매 가능) | RegisterFactory cards | 카드 생성 풀링 |

---

## 추가 조사 필요 항목

- FancyScrollView v4 → R3 직접 통합 방법 (현재 이벤트 기반)
- UnityScreenNavigator VContainer 자동 주입 공식 Extension 유무
- 래디얼 메뉴 게임패드 네비게이션 (InputSystem Navigation Event) 상세
- 튜토리얼 조건 DSL 설계 (ScriptableObject vs JSON 노드 그래프)
