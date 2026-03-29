# UGUI Drag & Drop 구현 패턴 리서치 리포트

- **작성일**: 2026-03-28
- **카테고리**: practice
- **상태**: 조사완료

---

## 1. 요약

Unity UGUI의 드래그&드롭 시스템은 `IBeginDragHandler` / `IDragHandler` / `IEndDragHandler` / `IDropHandler` 4개 인터페이스 조합으로 구성된다. Ghost 이미지(반투명 복사본)를 전용 DragLayer에 올리고 `CanvasGroup.blocksRaycasts = false`로 레이캐스트를 비활성화하는 것이 핵심 패턴이며, `RectTransformUtility.ScreenPointToLocalPointInRectangle`로 Canvas 좌표 변환을 처리한다. R3에서는 `OnBeginDragAsObservable().SelectMany(...).TakeUntil(OnEndDragAsObservable())` 체인으로 드래그 이벤트를 Observable 스트림으로 노출할 수 있으며, MV(R)P 아키텍처에서 Presenter가 이 스트림을 구독해 비즈니스 로직을 처리하는 구조가 권장된다.

---

## 2. 상세 분석

### 2.1 IDragHandler / IDropHandler / IPointerDownHandler — 표준 인터페이스 트리오

UGUI 드래그 시스템에 사용되는 핵심 인터페이스는 다음과 같다.

| 인터페이스 | 메서드 | 역할 |
|---|---|---|
| `IPointerDownHandler` | `OnPointerDown` | 클릭 감지 (선택적, 드래그와 충돌 주의) |
| `IBeginDragHandler` | `OnBeginDrag` | 드래그 시작 — Ghost 생성, 알파 변경 |
| `IDragHandler` | `OnDrag` | 드래그 중 — 위치 갱신 |
| `IEndDragHandler` | `OnEndDrag` | 드래그 종료 — Ghost 제거, 상태 복구 |
| `IDropHandler` | `OnDrop` | 드롭 수신 — 슬롯에서 구현 |
| `IPointerEnterHandler` | `OnPointerEnter` | 드롭 존 하이라이트 진입 |
| `IPointerExitHandler` | `OnPointerExit` | 드롭 존 하이라이트 해제 |

**중요 버그**: `IPointerDownHandler`와 `IDropHandler`를 동일 오브젝트에 함께 구현하면 `OnDrop`이 트리거되지 않는 알려진 버그가 있다 (Unity 2020.1.0a19~2020.2.0a15). 2020.2.9f1 / 2020.1.0b17에서 수정됨. 현재 Unity 6 환경에서는 해소된 버그지만, 드래그 소스와 드롭 수신자를 별도 컴포넌트/오브젝트로 분리하는 설계가 더 안전하다.

**OnBeginDrag 미호출 문제**: `OnDrag`가 호출되려면 반드시 같은 오브젝트에 `IBeginDragHandler`도 구현되어 있어야 한다 (EventSystem 요구사항). `OnDrag` 단독 구현은 작동하지 않는다.

```csharp
// 올바른 인터페이스 조합 — DraggableItem 컴포넌트
public class DraggableItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // IPointerDownHandler는 드래그 소스와 분리하거나 조심스럽게 사용
}

// 드롭 수신 — DropSlot 컴포넌트 (별도 오브젝트)
public class DropSlot : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
}
```

---

### 2.2 드래그 시각 피드백 — Ghost 복사본, 알파 감소, 스냅 프리뷰

#### 패턴 A: Alpha + raycastTarget 토글 (가장 단순)

```csharp
public class DragScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Canvas _canvas;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _canvasGroup.alpha = 0.6f;
        _canvasGroup.blocksRaycasts = false; // 드롭 존이 레이캐스트를 받을 수 있도록
    }

    public void OnDrag(PointerEventData eventData)
    {
        // canvas.scaleFactor로 나눠야 해상도 독립적
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
    }
}
```

#### 패턴 B: Ghost 이미지 — 전용 DragLayer에 복사본 생성

```csharp
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Canvas _rootCanvas;
    [SerializeField] private Transform _dragLayer; // Canvas 최상위 자식 오브젝트

    private RectTransform _rectTransform;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();

        // DragLayer로 부모 변경 → 다른 UI 위에 렌더링
        transform.SetParent(_dragLayer, worldPositionStays: true);
        transform.SetAsLastSibling();

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.8f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드롭 성공 여부는 OnDrop에서 처리됨 — 실패 시 복구
        if (transform.parent == _dragLayer)
        {
            transform.SetParent(_originalParent, worldPositionStays: true);
            transform.SetSiblingIndex(_originalSiblingIndex);
        }

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
    }
}
```

#### DragLayer 설정 (씬 구조)
```
Canvas (Screen Space - Overlay)
  ├── DefaultLayer (기본 컨텐츠)
  │     └── DraggableItem
  └── DragLayer (드래그 중 아이템 — 항상 최상위)
```

---

### 2.3 드롭 존 유효성 검사 — canDrop 로직, 시각 인디케이터

```csharp
public class DropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _highlightImage;
    [SerializeField] private Color _validColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private Color _invalidColor = new Color(1, 0, 0, 0.3f);

    // 슬롯 유효성 판단 조건 (게임 로직에 따라 커스터마이즈)
    private bool CanAcceptDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return false;
        var draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggable == null) return false;
        // 예: 슬롯 타입 일치 검사
        return draggable.ItemType == this.AcceptedItemType;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging)
        {
            _highlightImage.enabled = true;
            _highlightImage.color = CanAcceptDrop(eventData) ? _validColor : _invalidColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _highlightImage.enabled = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        _highlightImage.enabled = false;

        if (!CanAcceptDrop(eventData)) return;

        var draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
        // 데이터 먼저 업데이트 → UI 갱신 (Model-first 원칙)
        SwapSlotData(draggable);
    }

    private void SwapSlotData(DraggableItem source)
    {
        // 슬롯 간 데이터 교환 로직
    }
}
```

---

### 2.4 Sortable List — 드래그로 항목 재정렬

```csharp
public class SortableListItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas _canvas;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Transform _listContainer; // VerticalLayoutGroup 부모

    private int _originalIndex;
    private Vector3 _originalPosition;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _listContainer = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalIndex = transform.GetSiblingIndex();
        _originalPosition = transform.position;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        UpdateSiblingIndex();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
        // LayoutGroup이 자동으로 위치 재계산
    }

    private void UpdateSiblingIndex()
    {
        // 현재 드래그 위치 기준으로 삽입 인덱스를 계산
        int newIndex = 0;
        float myY = _rectTransform.position.y;

        for (int i = 0; i < _listContainer.childCount; i++)
        {
            var child = _listContainer.GetChild(i);
            if (child == transform) continue;
            if (myY < child.position.y) // 위 → 인덱스 증가
                newIndex = i;
        }

        if (newIndex != transform.GetSiblingIndex())
            transform.SetSiblingIndex(newIndex);
    }
}
```

**주의사항**: `SetSiblingIndex` 호출 후 `VerticalLayoutGroup`이 다음 프레임에 레이아웃을 재계산하므로, 드래그 중에는 LayoutGroup을 비활성화하고 종료 시 재활성화하는 접근도 유효하다.

---

### 2.5 Grid 재배열 — 인벤토리 슬롯 교환

```csharp
// 데이터 모델
public class Inventory
{
    public Item[,] Items { get; private set; }
    private int _width, _height;

    public Inventory(int width, int height)
    {
        _width = width;
        _height = height;
        Items = new Item[width, height];
    }

    public bool IsValid(int x, int y) =>
        x >= 0 && x < _width && y >= 0 && y < _height;

    // Model-first: 데이터 교환 후 UI에 이벤트 통보
    public void SwapItems(int x1, int y1, int x2, int y2)
    {
        if (!IsValid(x1, y1) || !IsValid(x2, y2)) return;
        (Items[x1, y1], Items[x2, y2]) = (Items[x2, y2], Items[x1, y1]);
    }
}

// 슬롯 View (IDropHandler + IBeginDragHandler 분리)
public class InventorySlotView : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int SlotX { get; set; }
    public int SlotY { get; set; }

    [SerializeField] private Image _itemImage;
    [SerializeField] private Image _highlightImage;

    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private Transform _dragLayer;

    // Presenter가 주입
    public System.Action<int, int, int, int> OnSlotSwapRequested;

    public void OnDrop(PointerEventData eventData)
    {
        _highlightImage.color = Color.clear;

        var sourceSlot = eventData.pointerDrag?.GetComponent<InventorySlotView>();
        if (sourceSlot == null) return;

        // Presenter에 스왑 요청
        OnSlotSwapRequested?.Invoke(sourceSlot.SlotX, sourceSlot.SlotY, SlotX, SlotY);
    }

    public void UpdateDisplay(Item item)
    {
        if (item != null)
        {
            _itemImage.sprite = item.Icon;
            _itemImage.enabled = true;
        }
        else
        {
            _itemImage.sprite = null;
            _itemImage.enabled = false;
        }
    }
    // ... BeginDrag/Drag/EndDrag 구현 생략 (패턴 B 참조)
}
```

**설계 원칙**: 항상 Model(데이터)을 먼저 업데이트한 뒤 View를 갱신한다. UI 먼저 변경하면 데이터-UI 불일치 버그가 발생한다.

---

### 2.6 카드 핸드 관리 — Fan 레이아웃, 플레이 에어리어 드래그

```csharp
public class CardHandManager : MonoBehaviour
{
    [SerializeField] private List<CardView> _cards = new();
    [SerializeField] private float _fanRadius = 600f;
    [SerializeField] private float _fanAngleRange = 30f; // 전체 부채꼴 각도

    // 카드를 호 위에 균등 배치
    public void ArrangeCards()
    {
        int count = _cards.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float angle = Mathf.Lerp(-_fanAngleRange * 0.5f, _fanAngleRange * 0.5f, t);
            float rad = angle * Mathf.Deg2Rad;

            // 원호 위 위치
            var card = _cards[i];
            var rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(
                Mathf.Sin(rad) * _fanRadius,
                -Mathf.Cos(rad) * _fanRadius + _fanRadius  // 아래쪽 원호
            );
            rt.localRotation = Quaternion.Euler(0, 0, -angle);

            // 레이어 순서 — 중앙 카드가 위
            rt.SetSiblingIndex(i);
        }
    }

    // DOTween으로 재배열 애니메이션
    public void ArrangeCardsAnimated()
    {
        int count = _cards.Count;
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float angle = Mathf.Lerp(-_fanAngleRange * 0.5f, _fanAngleRange * 0.5f, t);
            float rad = angle * Mathf.Deg2Rad;

            var rt = _cards[i].GetComponent<RectTransform>();
            var targetPos = new Vector2(
                Mathf.Sin(rad) * _fanRadius,
                -Mathf.Cos(rad) * _fanRadius + _fanRadius
            );
            var targetRot = Quaternion.Euler(0, 0, -angle);

            rt.DOAnchorPos(targetPos, 0.3f).SetEase(Ease.OutBack);
            rt.DOLocalRotateQuaternion(targetRot, 0.3f).SetEase(Ease.OutBack);
        }
    }
}

// 카드 드래그 — 플레이 에어리어로 드래그 시 카드 사용
public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Transform _playArea; // 카드 사용 판정 영역
    private Vector3 _handPosition;
    private Quaternion _handRotation;
    private Canvas _canvas;
    private RectTransform _rt;

    public System.Action<CardView> OnCardPlayed;
    public System.Action<CardView> OnCardReturned;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _handPosition = transform.localPosition;
        _handRotation = transform.localRotation;
        transform.SetAsLastSibling(); // 드래그 중 최상위
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rt.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        // 호버 회전 정규화
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation, Quaternion.identity, Time.deltaTime * 10f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        // 플레이 에어리어 진입 판정
        if (IsInPlayArea())
        {
            OnCardPlayed?.Invoke(this);
        }
        else
        {
            // 핸드로 귀환
            _rt.DOAnchorPos(_handPosition, 0.2f);
            transform.DOLocalRotateQuaternion(_handRotation, 0.2f);
            OnCardReturned?.Invoke(this);
        }
    }

    private bool IsInPlayArea()
    {
        if (_playArea == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            _playArea.GetComponent<RectTransform>(),
            Input.mousePosition, // New Input System: Mouse.current.position.ReadValue()
            null);
    }
}
```

---

### 2.7 Canvas 고려사항 — 드래그 좌표 변환

드래그 구현에서 Canvas 렌더 모드에 따라 좌표 변환 방식이 달라진다.

#### Screen Space - Overlay (가장 단순)

```csharp
public void OnDrag(PointerEventData eventData)
{
    // delta는 픽셀 단위 — scaleFactor로 나눠야 Canvas 단위로 변환
    _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
}
```

#### Screen Space - Camera (카메라 참조 필요)

```csharp
public void OnDrag(PointerEventData eventData)
{
    // RectTransformUtility 사용 필수
    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _canvas.transform as RectTransform,
        eventData.position,
        _canvas.worldCamera,  // Screen Space - Camera의 Camera
        out Vector2 localPoint))
    {
        _rectTransform.anchoredPosition = localPoint;
    }
}
```

#### World Space Canvas

```csharp
public void OnDrag(PointerEventData eventData)
{
    // World Space에서는 반드시 eventCamera 지정
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _parentRect,
        eventData.position,
        eventData.pressEventCamera, // PointerEventData의 카메라 사용
        out Vector2 localPoint);
    _rectTransform.anchoredPosition = localPoint;
}
```

#### 렌더 모드별 비교

| 항목 | Screen Space - Overlay | Screen Space - Camera | World Space |
|---|---|---|---|
| 드래그 delta 사용 | 가능 (`/ scaleFactor`) | 비권장 | 불가 |
| RectTransformUtility | 선택적 | 필수 | 필수 |
| Camera 참조 | 불필요 | `canvas.worldCamera` | `eventData.pressEventCamera` |
| 복잡도 | 낮음 | 중간 | 높음 |

**드래그 중 부모 변경 시 주의**: `SetParent`를 호출할 때 `worldPositionStays: true`를 전달해야 화면 위치가 유지된다.

```csharp
transform.SetParent(dragLayer, worldPositionStays: true);
```

---

### 2.8 R3 통합 — Observable 드래그 이벤트 체인

R3(Cysharp)는 UniRx의 후속으로 동일한 ObservableTriggers API를 제공한다. 드래그 이벤트를 Observable 스트림으로 노출하면 Presenter에서 반응형으로 구독할 수 있다.

#### View: 드래그 이벤트 Observable 노출

```csharp
using R3;
using ObservableExtensions = ObservableExtensions;

public class DraggableItemView : MonoBehaviour
{
    private ObservableEventTrigger _trigger;

    void Awake()
    {
        _trigger = gameObject.GetOrAddComponent<ObservableEventTrigger>();
    }

    // 드래그 스트림 — (시작, 현재) 튜플 페어
    public Observable<(PointerEventData begin, PointerEventData current)> OnDragStream()
    {
        return _trigger.OnBeginDragAsObservable()
            .SelectMany(begin =>
                _trigger.OnDragAsObservable()
                    .Select(current => (begin, current))
                    .TakeUntil(_trigger.OnEndDragAsObservable()))
            .RepeatUntilDestroy(this);
    }

    // 드롭 이벤트 Observable
    public Observable<PointerEventData> OnDropObservable() =>
        _trigger.OnDropAsObservable();

    // 드래그 시작/종료만 필요한 경우
    public Observable<PointerEventData> OnBeginDragObservable() =>
        _trigger.OnBeginDragAsObservable();

    public Observable<PointerEventData> OnEndDragObservable() =>
        _trigger.OnEndDragAsObservable();
}
```

#### Presenter: Observable 구독

```csharp
public class InventoryPresenter : IStartable, IDisposable
{
    private readonly InventoryModel _model;
    private readonly InventoryView _view;
    private readonly CompositeDisposable _disposables = new();

    // VContainer 생성자 주입
    public InventoryPresenter(InventoryModel model, InventoryView view)
    {
        _model = model;
        _view = view;
    }

    public void Start()
    {
        // 슬롯 드롭 이벤트 구독
        foreach (var slot in _view.Slots)
        {
            slot.OnDropObservable()
                .Subscribe(eventData => HandleDrop(slot, eventData))
                .AddTo(_disposables);

            // 드래그 중 하이라이트
            slot.OnBeginDragObservable()
                .Subscribe(_ => _view.ShowDropZoneHints())
                .AddTo(_disposables);

            slot.OnEndDragObservable()
                .Subscribe(_ => _view.HideDropZoneHints())
                .AddTo(_disposables);
        }
    }

    private void HandleDrop(InventorySlotView targetSlot, PointerEventData eventData)
    {
        var sourceSlot = eventData.pointerDrag?.GetComponent<InventorySlotView>();
        if (sourceSlot == null) return;

        // Model 먼저 업데이트
        _model.SwapItems(sourceSlot.SlotX, sourceSlot.SlotY, targetSlot.SlotX, targetSlot.SlotY);

        // View 갱신
        _view.RefreshSlot(sourceSlot.SlotX, sourceSlot.SlotY, _model.GetItem(sourceSlot.SlotX, sourceSlot.SlotY));
        _view.RefreshSlot(targetSlot.SlotX, targetSlot.SlotY, _model.GetItem(targetSlot.SlotX, targetSlot.SlotY));
    }

    public void Dispose() => _disposables.Dispose();
}
```

#### UniRx → R3 API 차이점

| UniRx | R3 | 비고 |
|---|---|---|
| `IObservable<T>` | `Observable<T>` | 타입 변경 |
| `Subject<T>` | `Subject<T>` | 동일 |
| `.AddTo(this)` | `.AddTo(this)` | 동일 (파라미터 타입 다를 수 있음) |
| `RepeatUntilDestroy` | `RepeatUntilDestroy` | 동일 |
| `ObservableEventTrigger` | `ObservableEventTrigger` | 동일 (패키지 포함) |

---

### 2.9 성능 최적화

#### GraphicRaycaster 최적화

```csharp
// 드래그 중 현재 아이템의 RaycastTarget 비활성화 → GraphicRaycaster가 스킵
_image.raycastTarget = false;        // OnBeginDrag
// 또는
_canvasGroup.blocksRaycasts = false; // CanvasGroup 방식 (하위 전체 적용)

// OnEndDrag에서 복구
_canvasGroup.blocksRaycasts = true;
```

#### 드래그 전용 Canvas 분리

```
Canvas (Main UI - Static elements)     ← 드래그 없는 정적 UI
Canvas (Inventory Grid - Interactive)  ← 인벤토리 그리드 전용
Canvas (Drag Layer - Always on top)    ← 드래그 Ghost 전용 (Sort Order: 최상위)
```

드래그 Ghost를 별도 Canvas에 올리면:
- 메인 UI Canvas의 Rebuild를 유발하지 않음
- `GraphicRaycaster`를 Drag Layer에서는 비활성화 가능

#### 할당(Allocation) 회피

```csharp
// OnDrag에서 new Vector2() 회피
private Vector2 _dragDelta;

public void OnDrag(PointerEventData eventData)
{
    _dragDelta = eventData.delta / _canvas.scaleFactor;
    _rectTransform.anchoredPosition += _dragDelta; // struct이므로 할당 없음
}

// Color 캐싱 — OnPointerEnter에서 new Color() 반복 생성 회피
private static readonly Color ValidHighlight = new Color(0, 1, 0, 0.3f);
private static readonly Color InvalidHighlight = new Color(1, 0, 0, 0.3f);
```

#### Raycaster 설정 권장사항

```
GraphicRaycaster 컴포넌트:
- Ignore Reversed Graphics: true  (뒤집힌 UI 제외)
- Blocking Objects: None          (3D 오브젝트 차단 불필요 시)
- Blocking Mask: Nothing          (레이어 마스크 최소화)
```

---

### 2.10 터치 지원 — 모바일 터치 vs 마우스

New Input System 환경에서는 `InputSystemUIInputModule`이 터치와 마우스를 통합 처리한다. `IBeginDragHandler` 등 UGUI 인터페이스는 터치/마우스 모두 동일하게 작동하며, `PointerEventData`에서 구분 가능하다.

```csharp
public void OnDrag(PointerEventData eventData)
{
    // New Input System: ExtendedPointerEventData 캐스팅으로 포인터 타입 확인
    if (eventData is UnityEngine.InputSystem.UI.ExtendedPointerEventData extendedData)
    {
        bool isTouch = extendedData.pointerType ==
            UnityEngine.InputSystem.UI.UIPointerType.Touch;
        // 터치별 처리 가능
    }

    // 공통 처리 — delta는 터치/마우스 모두 동작
    _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
}
```

#### 멀티터치 드래그 고려사항

```csharp
// PointerEventData.pointerId로 각 터치 구분
// 터치: pointerId = deviceId * 1000 + touchId
// 마우스: pointerId = -1 (Left), -2 (Right), -3 (Middle)

public void OnBeginDrag(PointerEventData eventData)
{
    // 단일 드래그만 허용 (첫 번째 터치/마우스만 처리)
    if (_isDragging) return;
    _activeDragPointerId = eventData.pointerId;
    _isDragging = true;
}

public void OnDrag(PointerEventData eventData)
{
    // 다른 포인터 ID는 무시
    if (eventData.pointerId != _activeDragPointerId) return;
    _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
}

public void OnEndDrag(PointerEventData eventData)
{
    if (eventData.pointerId != _activeDragPointerId) return;
    _isDragging = false;
}
```

#### 모바일 드래그 UX 권장사항

- 드래그 임계값(drag threshold): `EventSystem.current.pixelDragThreshold`로 조정 (기본 10px)
- 터치 타깃 최소 크기: 48dp 이상 (약 44px at 1x)
- 장시간 누르기(LongPress) 후 드래그: `IPointerDownHandler`에서 타이머로 드래그 시작 지연

---

## 3. 베스트 프랙티스

### DO (권장)
- [x] `CanvasGroup.blocksRaycasts = false`로 드래그 중 Ghost가 드롭 존 레이캐스트를 막지 않도록 설정
- [x] 드래그 중 Ghost를 최상위 DragLayer Canvas로 이동 — `SetParent(dragLayer, worldPositionStays: true)`
- [x] `eventData.delta / canvas.scaleFactor`로 Canvas 해상도 독립적 이동 처리
- [x] Model-first 원칙 — 데이터 교환 후 View 갱신
- [x] 드래그 소스(DraggableItem)와 드롭 수신(DropSlot)을 별도 컴포넌트로 분리
- [x] R3 Observable 체인으로 드래그 이벤트 노출 — Presenter에서 구독
- [x] 정적 UI 요소의 `RaycastTarget` 비활성화 — GraphicRaycaster 부하 감소
- [x] `static readonly Color`로 하이라이트 색상 캐싱
- [x] Screen Space - Camera에서 `RectTransformUtility.ScreenPointToLocalPointInRectangle` 사용

### DON'T (금지)
- [x] `IPointerDownHandler`와 `IDropHandler`를 같은 컴포넌트에 구현 (Unity 2020 버그 — 분리 권장)
- [x] `OnDrag`에서 `new Vector2()` 등 매 프레임 할당 생성
- [x] 드래그 중 `transform.position = eventData.position` 직접 대입 — Canvas 스케일 무시됨
- [x] World Space Canvas에서 `eventData.delta` 직접 사용 — 좌표계 불일치
- [x] LayoutGroup이 있는 컨테이너에서 드래그 중 `SetSiblingIndex` 빈번 호출 — 매 프레임 LayoutGroup Rebuild 유발
- [x] VContainer Construct()에서 드래그 Observable 구독 — Awake 전 실행으로 데드락 위험

### CONSIDER (상황별)
- [x] 슬롯이 많은 인벤토리 — `IPointerEnterHandler` 대신 커스텀 레이캐스팅으로 하이라이트
- [x] 정렬 가능한 긴 리스트 — LayoutGroup 비활성화 후 드래그, 종료 시 재활성화
- [x] 카드 게임 — DOTween으로 핸드 재배열 애니메이션 적용
- [x] 터치 멀티포인터 — `pointerId` 기반 단일 드래그 잠금

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 6000.x | Screen Space Camera 버그 대부분 수정됨 |
| com.unity.ugui | 2.0.0 | Unity 6 번들 |
| com.unity.inputsystem | 1.7+ | InputSystemUIInputModule 필수 |
| R3 (Cysharp) | 1.x | ObservableTriggers 포함 |
| DOTween | 1.2.x | 카드 핸드 애니메이션 |
| IPointerDownHandler + IDropHandler 버그 | 수정됨 | 2020.2.9f1 이상에서 해소 |

---

## 5. 예제 코드

### 기본 사용법 — 전체 드래그&드롭 시스템

```csharp
// DraggableItem.cs — 드래그 소스
[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Canvas _rootCanvas;

    private RectTransform _rt;
    private CanvasGroup _cg;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private Transform _dragLayer;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        // DragLayer는 Canvas의 최상위 자식으로 씬에 미리 배치
        _dragLayer = _rootCanvas.transform.Find("DragLayer");
    }

    public void OnBeginDrag(PointerEventData e)
    {
        _originalParent = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        transform.SetParent(_dragLayer, worldPositionStays: true);
        transform.SetAsLastSibling();
        _cg.blocksRaycasts = false;
        _cg.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData e)
    {
        _rt.anchoredPosition += e.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData e)
    {
        // 드롭 성공 시 DropSlot.OnDrop이 부모를 슬롯으로 변경함
        // 드롭 실패 시 (부모가 여전히 DragLayer) 원위치 복구
        if (transform.parent == _dragLayer)
        {
            transform.SetParent(_originalParent, worldPositionStays: true);
            transform.SetSiblingIndex(_originalSiblingIndex);
        }
        _cg.blocksRaycasts = true;
        _cg.alpha = 1f;
    }
}

// DropSlot.cs — 드롭 수신자
public class DropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _highlight;
    private static readonly Color HighlightColor = new Color(1f, 1f, 0f, 0.4f);

    public void OnPointerEnter(PointerEventData e)
    {
        if (e.dragging)
        {
            _highlight.enabled = true;
            _highlight.color = HighlightColor;
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        _highlight.enabled = false;
    }

    public void OnDrop(PointerEventData e)
    {
        _highlight.enabled = false;
        if (e.pointerDrag == null) return;

        // 아이템을 이 슬롯의 자식으로 배치
        var draggable = e.pointerDrag.GetComponent<DraggableItem>();
        if (draggable == null) return;

        e.pointerDrag.transform.SetParent(transform, worldPositionStays: false);
        e.pointerDrag.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }
}
```

### 고급 패턴 — MV(R)P + VContainer + R3

```csharp
// LifetimeScope
public class InventoryLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<InventoryView>();
        builder.Register<InventoryModel>(Lifetime.Scoped);
        builder.RegisterEntryPoint<InventoryPresenter>(Lifetime.Scoped);
    }
}

// Model
public class InventoryModel
{
    public ReactiveProperty<Item[]> Items { get; } = new(new Item[9]);

    public void SwapItems(int fromIndex, int toIndex)
    {
        var arr = Items.Value;
        (arr[fromIndex], arr[toIndex]) = (arr[toIndex], arr[fromIndex]);
        Items.Value = arr; // 값 변경 통지
    }
}

// View
public class InventoryView : MonoBehaviour
{
    [SerializeField] private InventorySlotView[] _slots;
    public IReadOnlyList<InventorySlotView> Slots => _slots;
}

// Presenter
public class InventoryPresenter : IStartable, IDisposable
{
    private readonly InventoryModel _model;
    private readonly InventoryView _view;
    private readonly CompositeDisposable _d = new();

    public InventoryPresenter(InventoryModel model, InventoryView view)
    {
        _model = model;
        _view = view;
    }

    public void Start()
    {
        // Model → View 바인딩
        _model.Items
            .Subscribe(items => RefreshAllSlots(items))
            .AddTo(_d);

        // View 이벤트 → Model 처리
        for (int i = 0; i < _view.Slots.Count; i++)
        {
            int slotIndex = i;
            _view.Slots[i].OnDropObservable()
                .Subscribe(e => HandleDrop(slotIndex, e))
                .AddTo(_d);
        }
    }

    private void HandleDrop(int targetIndex, PointerEventData e)
    {
        var sourceSlot = e.pointerDrag?.GetComponentInParent<InventorySlotView>();
        if (sourceSlot == null) return;
        _model.SwapItems(sourceSlot.SlotIndex, targetIndex);
    }

    private void RefreshAllSlots(Item[] items)
    {
        for (int i = 0; i < items.Length && i < _view.Slots.Count; i++)
            _view.Slots[i].SetItem(items[i]);
    }

    public void Dispose() => _d.Dispose();
}
```

---

## 6. UI_Study 적용 계획

이 리서치를 기반으로 UI_Study에서 구현 가능한 예제 단계:

| 단계 | 예제 | 핵심 학습 |
|---|---|---|
| 01 | 기본 드래그&드롭 (단일 아이템) | IBeginDragHandler 트리오, alpha + blocksRaycasts |
| 02 | 드롭 존 유효성 + 하이라이트 | IDropHandler, CanAcceptDrop, Color 인디케이터 |
| 03 | 인벤토리 그리드 슬롯 교환 | 2D 배열 Model-first, SwapItems |
| 04 | 정렬 가능한 리스트 (Sortable List) | SetSiblingIndex, 삽입 위치 계산 |
| 05 | 카드 핸드 관리 (Fan Layout) | 호 위치 수식, DOTween 재배열 |
| 06 | MV(R)P + VContainer + R3 통합 | Observable 드래그 스트림, Presenter 구독 |

---

## 7. 참고 자료

1. [Unity UGUI IDragHandler 공식 문서](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/SupportedEvents.html)
2. [Drag & Drop for UI elements in Unity — Medium (Jonas Hundertmark)](https://medium.com/medialesson/drag-drop-for-ui-elements-in-unity-the-simple-ish-way-9efcb4617648)
3. [Unity Drag and Drop System for Inventory UI — VionixStudio](https://vionixstudio.com/2022/11/24/unity-drag-and-drop-system/)
4. [Unity UI Drag and Drop Inventory System — Sean Duggan (Medium)](https://medium.com/@sean.duggan/unity-ui-drag-and-drop-inventory-system-ae84d1173d3e)
5. [Building a Robust Grid-Based Inventory System — Wayline](https://www.wayline.io/blog/unity-grid-inventory-system)
6. [Unity Drag and Drop Complete Tutorial — Generalist Programmer](https://generalistprogrammer.com/tutorials/unity-drag-drop-system-complete-tutorial-ui-gameplay)
7. [Unity UGUI Drag and Drop Gist (wakeup5)](https://gist.github.com/wakeup5/c929daa1ffd517c3d9dbeb642daf329b)
8. [UniRx ObservableEventTrigger Source (neuecc)](https://github.com/neuecc/UniRx/blob/master/Assets/Plugins/UniRx/Scripts/UnityEngineBridge/Triggers/ObservableEventTrigger.cs)
9. [R3 (Cysharp) GitHub](https://github.com/Cysharp/R3)
10. [IDropHandler not triggered with IPointerDownHandler — Unity Issue Tracker](https://issuetracker.unity3d.com/issues/idrophandler-is-not-triggered-when-the-dragged-object-has-ipointerdownhandler-implemented)
11. [Card Fan UI — Pixit Games](https://www.pixitgames.com/docs/cardfan/)
12. [Creating a card fan like Slay The Spire — Unity Discussions](https://discussions.unity.com/t/creating-a-card-fan-alignment-function-like-slay-the-spire/888524)
13. [Unity Input System UI Support Docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/UISupport.html)
14. [UGUI Drag Drop GitHub (tjcccc)](https://github.com/tjcccc/ugui-dragdrop)
15. [FullStackForger UGUI Sortable List](https://github.com/FullStackForger/ifup-ui-sortable-list)

---

## 8. 미해결 질문

- [ ] R3 v1.x의 `ObservableEventTrigger`가 UniRx와 100% API 호환인지 확인 필요 (네임스페이스 변경 여부)
- [ ] VerticalLayoutGroup + SetSiblingIndex 조합에서 드래그 중 레이아웃 재계산을 완전히 억제하는 최적의 방법
- [ ] 멀티터치 동시 드래그 시 서로 다른 슬롯을 동시에 드래그하는 UX가 필요한지 (게임 설계 의존)
- [ ] Unity 6 UGUI 2.0.0에서 `PointerEventData.delta`의 World Space Canvas 동작 변경 여부
- [ ] 물리 기반 드래그 (spring/inertia) 구현 시 UniTask vs DOTween 트레이드오프
