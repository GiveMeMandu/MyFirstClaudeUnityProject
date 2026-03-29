# UGUI Drag-to-Reorder Sortable List 리서치 리포트

- **작성일**: 2026-03-28
- **카테고리**: practice
- **상태**: 조사완료
- **관련 문서**: [ugui-drag-drop-patterns.md](ugui-drag-drop-patterns.md) (기본 Drag & Drop 패턴)

---

## 1. 요약

Unity UGUI에서 iOS/Android 수준의 Live Reorder(드래그 중 다른 항목이 실시간으로 슬라이드) 구현은 **Placeholder(더미 오브젝트) 방식**이 표준이다. 드래그된 아이템을 Canvas 루트로 reparent해 렌더 최상위에 올리고, 원래 위치에 투명 Placeholder를 남긴 뒤, 드래그 Y 좌표 기반 삽입 인덱스 계산으로 Placeholder의 SiblingIndex를 실시간 갱신한다. VerticalLayoutGroup과의 충돌은 Placeholder를 통해 레이아웃이 갭을 자연스럽게 유지하도록 우회하며, DOTween을 통한 스케일 업/그림자 효과로 "떠 있는" 시각적 피드백을 제공한다. 모바일에서는 Long-Press로 드래그를 개시하고, `Handheld.Vibrate()`로 햅틱 피드백을 준다.

---

## 2. 상세 분석

### 2.1 핵심 문제: VerticalLayoutGroup vs 드래그의 충돌

VerticalLayoutGroup은 자식 오브젝트의 anchoredPosition을 매 프레임 강제로 재계산한다. 드래그 중 항목을 레이아웃 컨테이너 안에 놔두면:

- `OnDrag`에서 `anchoredPosition`을 갱신해도 LayoutGroup이 즉시 덮어씀
- 항목이 제자리에서 꼼짝 못하거나, 레이아웃이 깨지는 현상 발생

**해결 방법 3가지**:

| 방법 | 동작 원리 | 장단점 |
|---|---|---|
| **A. Reparent to Canvas** | 드래그 시작 시 Canvas 루트(또는 전용 DragLayer)로 이동 | 가장 범용적. 렌더 최상위 보장. Canvas 좌표 변환 필요 |
| **B. LayoutGroup 비활성화** | `_layoutGroup.enabled = false` 후 수동 위치 제어 | 단순하지만 비활성화 중 레이아웃 전체가 무너짐 |
| **C. Placeholder 방식** | 원위치에 더미 오브젝트 유지, 실제 항목만 이동 | LayoutGroup이 갭 공간을 자동 유지. 가장 생산적 |

**권장**: A + C 조합 — 항목을 Canvas로 reparent + Placeholder로 갭 유지.

---

### 2.2 표준 알고리즘: Placeholder 방식

```
OnBeginDrag:
  1. 현재 SiblingIndex 저장
  2. Placeholder GameObject 생성 (동일 크기, 투명/반투명)
  3. Placeholder를 원래 위치의 SiblingIndex에 삽입
  4. 드래그 항목을 Canvas 루트(DragLayer)로 reparent
  5. CanvasGroup.blocksRaycasts = false (레이캐스트가 Placeholder에 닿게)
  6. DOTween으로 scale 1.0 → 1.05, 그림자 활성화

OnDrag:
  1. 드래그 항목 위치를 포인터를 따라 갱신
  2. 포인터 Y 좌표 → 삽입 인덱스 계산
  3. 계산된 인덱스가 현재 Placeholder.SiblingIndex와 다르면:
     → Placeholder.SetSiblingIndex(newIndex)
     → LayoutGroup이 자동으로 다른 항목을 슬라이드
  4. (선택) 스크롤뷰 경계 자동 스크롤

OnEndDrag:
  1. 드래그 항목을 원래 컨테이너로 reparent
  2. Placeholder.SiblingIndex 위치로 SetSiblingIndex
  3. Placeholder 제거
  4. CanvasGroup.blocksRaycasts = true
  5. DOTween으로 scale 1.05 → 1.0, 그림자 비활성화
  6. DOTween으로 anchoredPosition → LayoutGroup이 계산한 최종 위치로 스냅
```

---

### 2.3 삽입 인덱스 계산 알고리즘

포인터 Y 위치에서 삽입 인덱스를 계산하는 핵심 로직:

```csharp
private int GetInsertIndex(float pointerWorldY)
{
    int insertIndex = _container.childCount; // 기본: 맨 끝

    for (int i = 0; i < _container.childCount; i++)
    {
        RectTransform child = _container.GetChild(i) as RectTransform;
        if (child == _placeholder) continue; // placeholder는 건너뜀

        // 각 항목의 월드 Y 중간점 계산
        float childCenterY = child.position.y;

        // UGUI Y축: 위가 크고 아래가 작음 (화면 좌표와 반대)
        // 포인터가 이 항목의 중간점보다 위에 있으면 이 항목 앞에 삽입
        if (pointerWorldY > childCenterY)
        {
            insertIndex = i;
            break;
        }
    }

    return insertIndex;
}
```

**대안: GetWorldCorners 방식** (항목 높이가 불균일할 때 더 정확):

```csharp
private int GetInsertIndex(PointerEventData eventData)
{
    Vector3[] corners = new Vector3[4];

    for (int i = 0; i < _container.childCount; i++)
    {
        RectTransform child = _container.GetChild(i) as RectTransform;
        if (child == _placeholder.transform) continue;

        child.GetWorldCorners(corners);
        float topY = corners[1].y;    // 상단 모서리 Y
        float bottomY = corners[0].y; // 하단 모서리 Y
        float midY = (topY + bottomY) * 0.5f;

        if (eventData.position.y > midY)
        {
            return i;
        }
    }

    return _container.childCount;
}
```

**tetr4lab/ReOrderableList 방식** — Marker 오브젝트:
Viewport의 4 모서리에 보이지 않는 Marker GameObject를 배치하고, 포인터 Y를 이 마커들과 비교해 삽입 인덱스를 결정. CanvasScaler의 동적 레이아웃 변화를 자동 수용하는 장점이 있으나, 정밀도는 중간점 방식과 동일.

---

### 2.4 Canvas 좌표 변환 및 렌더 최상위 처리

드래그 항목을 Canvas 루트로 이동할 때 좌표 변환이 필요하다:

```csharp
public void OnBeginDrag(PointerEventData eventData)
{
    // 월드 좌표 기준 위치 보존
    Vector3 worldPos = _rectTransform.position;

    // Canvas 루트(DragLayer)로 reparent
    _originalParent = transform.parent;
    _originalSiblingIndex = transform.GetSiblingIndex();
    transform.SetParent(_dragLayer, worldPositionStays: true);
    transform.SetAsLastSibling(); // 렌더 최상위

    // 포인터 오프셋 계산 (항목 중심과 포인터 간 거리)
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _dragLayer,
        eventData.position,
        eventData.pressEventCamera,
        out Vector2 localPoint
    );
    _pointerOffset = _rectTransform.anchoredPosition - localPoint;
}

public void OnDrag(PointerEventData eventData)
{
    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _dragLayer,
        eventData.position,
        eventData.pressEventCamera,
        out Vector2 localPoint))
    {
        _rectTransform.anchoredPosition = localPoint + _pointerOffset;
    }

    // 삽입 인덱스 갱신 → Placeholder 이동
    UpdatePlaceholderIndex(eventData);
}
```

**핵심**: `worldPositionStays: true`로 reparent하면 화면상 위치가 유지된다.
`SetAsLastSibling()`으로 Canvas 내 다른 모든 UI 위에 렌더링된다.

---

### 2.5 시각적 피드백 — 떠 있는 느낌 (Elevated State)

iOS/Android 수준의 드래그 피드백:

```csharp
// OnBeginDrag: 아이템 "리프트" 효과
private void LiftItem()
{
    // 1. 스케일 업 (DOTween)
    transform.DOScale(Vector3.one * 1.05f, 0.15f)
        .SetEase(Ease.OutBack);

    // 2. 반투명 (약간만 - 너무 흐리면 내용이 안 보임)
    _canvasGroup.alpha = 0.95f;

    // 3. 그림자 효과 — Shadow 컴포넌트 또는 별도 Image
    _shadowImage.DOFade(0.4f, 0.15f);
    _shadowImage.rectTransform.DOAnchorPos(
        new Vector2(4f, -4f), 0.15f); // 그림자 오프셋
}

// OnEndDrag: 아이템 "랜딩" 효과
private void LandItem()
{
    transform.DOScale(Vector3.one, 0.1f)
        .SetEase(Ease.OutQuad);

    _canvasGroup.alpha = 1f;

    _shadowImage.DOFade(0f, 0.1f);
    _shadowImage.rectTransform.DOAnchorPos(Vector2.zero, 0.1f);
}
```

**Placeholder 스타일 옵션**:
- 완전 투명 (visibility: 0) — 빈 공간만 표시
- 반투명 점선 테두리 Image — "여기에 들어갈 것"을 명시
- 원래 아이템의 alpha 0.3 복사본 — 원래 위치 힌트

---

### 2.6 LayoutGroup 충돌 해소 — 상세 전략

#### 전략 A: Placeholder + Reparent (권장)

VerticalLayoutGroup은 `Placeholder`의 크기(LayoutElement)를 레이아웃 계산에 포함한다. Placeholder의 SiblingIndex를 바꾸면 LayoutGroup이 다른 항목들을 자동으로 재배치한다. 이것이 실시간 슬라이드 효과의 원리다.

```
[아이템A]         [아이템A]
[드래그중]   →   [Placeholder] ← LayoutGroup이 갭 유지
[아이템B]         [아이템B]
[아이템C]         [아이템C]
```

LayoutGroup에 `Transition Duration`이 없으므로 항목들은 즉시 점프한다. 부드러운 슬라이드를 원한다면 아래 전략 B를 추가 적용한다.

#### 전략 B: LayoutGroup 비활성화 + DOTween 수동 애니메이션

```csharp
// 삽입 인덱스가 바뀔 때마다 항목들을 DOTween으로 이동
private void AnimateItemsToNewPositions(int newPlaceholderIndex)
{
    // LayoutGroup의 예상 위치를 미리 계산
    LayoutRebuilder.ForceRebuildLayoutImmediate(_container);

    Dictionary<RectTransform, Vector2> targetPositions = new();

    // 임시로 Placeholder 이동
    _placeholder.SetSiblingIndex(newPlaceholderIndex);
    LayoutRebuilder.ForceRebuildLayoutImmediate(_container);

    // 각 항목의 새 위치 기록
    for (int i = 0; i < _container.childCount; i++)
    {
        var rt = _container.GetChild(i) as RectTransform;
        if (rt != _placeholder)
            targetPositions[rt] = rt.anchoredPosition;
    }

    // LayoutGroup 비활성화 후 DOTween으로 이동
    _layoutGroup.enabled = false;

    foreach (var kvp in targetPositions)
    {
        kvp.Key.DOAnchorPos(kvp.Value, 0.15f)
            .SetEase(Ease.OutCubic);
    }

    // 애니메이션 완료 후 LayoutGroup 재활성화
    DOVirtual.DelayedCall(0.15f, () => _layoutGroup.enabled = true);
}
```

**주의**: LayoutGroup 비활성화 중에는 ContentSizeFitter도 동작하지 않으므로 컨테이너 크기가 고정된다. 항목 추가/삭제가 동시에 발생하면 복잡해진다.

#### 전략 C: Stray Pixels 컨테이너 래퍼 기법

각 항목을 `Container → Content` 2계층으로 구성하고, Container에 LayoutElement를 부착해 높이를 애니메이션:

```
VerticalLayoutGroup (부모)
  ├── ItemContainer_A  ← LayoutElement (preferredHeight 애니메이션)
  │     └── ItemContent_A  (실제 내용, ContentSizeFitter)
  ├── ItemContainer_B
  │     └── ItemContent_B
  └── Placeholder     ← LayoutElement.preferredHeight = 0 → itemHeight (DOTween)
```

새 항목 삽입 시 Placeholder의 `preferredHeight`를 0에서 목표 높이로 DOTween하면 부드러운 "확장" 애니메이션이 된다. 그러나 드래그 중 매 프레임 갱신이 필요하므로 오버헤드가 있다.

---

### 2.7 완성형 구현 코드 (MV(R)P 패턴)

#### SortableListItem.cs (View + Drag Handler)

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using R3;

[RequireComponent(typeof(CanvasGroup))]
public class SortableListItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // --- 직렬화 ---
    [SerializeField] private GameObject _shadowPrefab; // 선택적 그림자

    // --- 내부 상태 ---
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private Vector2 _pointerOffset;

    // --- 리스트 연결 ---
    private SortableListController _listController;

    // --- R3 이벤트 ---
    private Subject<int> _onOrderChanged = new();
    public Observable<int> OnOrderChanged => _onOrderChanged;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void Initialize(SortableListController controller)
    {
        _listController = controller;
    }

    // ── OnBeginDrag ──────────────────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();

        // 1. Placeholder 생성 (원위치에 갭 유지)
        _listController.CreatePlaceholder(_originalSiblingIndex, _rectTransform.rect.size);

        // 2. Canvas 루트로 reparent (렌더 최상위)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 canvasLocalPoint);

        Vector2 myPos = _rectTransform.anchoredPosition;
        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
        transform.SetAsLastSibling();

        // 포인터 오프셋 (항목 중심과 포인터 간 거리)
        _pointerOffset = _rectTransform.anchoredPosition - canvasLocalPoint;

        // 3. 레이캐스트 차단 해제 (Placeholder가 이벤트 받도록)
        _canvasGroup.blocksRaycasts = false;

        // 4. "리프트" 애니메이션
        transform.DOScale(Vector3.one * 1.05f, 0.12f).SetEase(Ease.OutBack);
        _canvasGroup.DOFade(0.95f, 0.1f);
    }

    // ── OnDrag ────────────────────────────────────────────────
    public void OnDrag(PointerEventData eventData)
    {
        // 1. 포인터 따라 이동
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            _rectTransform.anchoredPosition = localPoint + _pointerOffset;
        }

        // 2. 삽입 인덱스 계산 → Placeholder 이동
        int newIndex = _listController.CalculateInsertIndex(eventData.position);
        _listController.MovePlaceholder(newIndex);
    }

    // ── OnEndDrag ─────────────────────────────────────────────
    public void OnEndDrag(PointerEventData eventData)
    {
        int finalIndex = _listController.GetPlaceholderIndex();

        // 1. 원래 컨테이너로 복귀
        transform.SetParent(_originalParent, worldPositionStays: true);
        transform.SetSiblingIndex(finalIndex);

        // 2. Placeholder 제거
        _listController.RemovePlaceholder();

        // 3. 레이캐스트 복구
        _canvasGroup.blocksRaycasts = true;

        // 4. "랜딩" 애니메이션 + 최종 위치 스냅
        transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutQuad);
        _canvasGroup.DOFade(1f, 0.1f);

        // LayoutGroup이 재계산한 위치로 DOTween 스냅
        // (LayoutGroup이 다음 프레임에 위치 확정하므로 약간 delay)
        DOVirtual.DelayedCall(0.02f, () =>
        {
            // 이 시점에서 anchoredPosition은 LayoutGroup이 결정한 최종값
            // 이미 그 위치에 있으므로 추가 tween 불필요한 경우가 많음
        });

        // 5. 순서 변경 이벤트 발행
        if (finalIndex != _originalSiblingIndex)
            _onOrderChanged.OnNext(finalIndex);
    }
}
```

#### SortableListController.cs (리스트 관리)

```csharp
using UnityEngine;
using UnityEngine.UI;

public class SortableListController : MonoBehaviour
{
    [SerializeField] private RectTransform _container; // VerticalLayoutGroup 부모
    [SerializeField] private GameObject _placeholderPrefab; // 투명 더미

    private GameObject _placeholder;
    private VerticalLayoutGroup _layoutGroup;
    private ContentSizeFitter _contentSizeFitter;

    void Awake()
    {
        _layoutGroup = _container.GetComponent<VerticalLayoutGroup>();
        _contentSizeFitter = _container.GetComponent<ContentSizeFitter>();
    }

    // Placeholder 생성
    public void CreatePlaceholder(int atIndex, Vector2 size)
    {
        _placeholder = Instantiate(_placeholderPrefab, _container);
        var rt = _placeholder.GetComponent<RectTransform>();
        var le = _placeholder.GetComponent<LayoutElement>();

        // 드래그된 항목과 같은 크기로
        le.preferredHeight = size.y;
        le.preferredWidth = size.x;

        _placeholder.transform.SetSiblingIndex(atIndex);
    }

    // 삽입 인덱스 계산 (포인터 스크린 좌표 → 컨테이너 내 인덱스)
    public int CalculateInsertIndex(Vector2 screenPointerPos)
    {
        int insertIndex = _container.childCount;
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < _container.childCount; i++)
        {
            var child = _container.GetChild(i) as RectTransform;
            if (child == _placeholder.transform) continue;

            child.GetWorldCorners(corners);
            // corners[0]=BottomLeft, corners[1]=TopLeft, corners[2]=TopRight, corners[3]=BottomRight
            float topWorldY = corners[1].y;
            float botWorldY = corners[0].y;
            float midWorldY = (topWorldY + botWorldY) * 0.5f;

            // 스크린 Y를 월드 Y로 변환 (Overlay Canvas는 동일)
            // 포인터가 이 항목 중간점보다 위에 있으면 이 인덱스 앞에 삽입
            if (screenPointerPos.y > midWorldY)
            {
                insertIndex = i;
                break;
            }
        }

        return insertIndex;
    }

    // Placeholder 이동 (SiblingIndex 변경 → LayoutGroup 자동 재배치)
    public void MovePlaceholder(int newIndex)
    {
        if (_placeholder == null) return;
        int current = _placeholder.transform.GetSiblingIndex();
        if (current == newIndex) return;

        _placeholder.transform.SetSiblingIndex(newIndex);
        // VerticalLayoutGroup이 다음 프레임에 다른 항목 위치 자동 갱신
    }

    public int GetPlaceholderIndex()
    {
        return _placeholder != null
            ? _placeholder.transform.GetSiblingIndex()
            : 0;
    }

    public void RemovePlaceholder()
    {
        if (_placeholder != null)
        {
            Destroy(_placeholder);
            _placeholder = null;
        }
    }
}
```

---

### 2.8 모바일 Long-Press + 드래그 연동

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// Long-Press로 드래그를 개시하는 모바일 전용 핸들러
public class LongPressDragInitiator : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float _longPressDuration = 0.4f;
    [SerializeField] private SortableListItem _dragTarget;

    private Coroutine _longPressRoutine;
    private bool _isDragging;

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = false;
        _longPressRoutine = StartCoroutine(LongPressRoutine(eventData));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_longPressRoutine != null)
            StopCoroutine(_longPressRoutine);
        _isDragging = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isDragging && _longPressRoutine != null)
            StopCoroutine(_longPressRoutine);
    }

    private IEnumerator LongPressRoutine(PointerEventData eventData)
    {
        yield return new WaitForSeconds(_longPressDuration);

        // 햅틱 피드백 (iOS/Android)
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif

        _isDragging = true;
        // ExecuteEvents로 BeginDrag 강제 발생
        ExecuteEvents.Execute(_dragTarget.gameObject, eventData,
            ExecuteEvents.beginDragHandler);
    }
}
```

**주의**: Long-Press와 IBeginDragHandler를 연동할 때, 포인터 이동 없이는 이벤트 시스템이 BeginDrag를 호출하지 않는다. `ExecuteEvents.Execute`로 수동 트리거하는 것이 표준 패턴이다.

---

### 2.9 ScrollView 안에서 자동 스크롤

드래그 중 항목이 ScrollView 경계에 닿으면 자동 스크롤:

```csharp
private void UpdateAutoScroll(PointerEventData eventData)
{
    if (_scrollRect == null) return;

    RectTransform viewport = _scrollRect.viewport;
    viewport.GetWorldCorners(_viewportCorners);

    float topEdge    = _viewportCorners[1].y;
    float bottomEdge = _viewportCorners[0].y;
    float pointerY   = eventData.position.y;

    float scrollZone = 80f; // 경계에서 80px 이내면 자동 스크롤
    float scrollSpeed = 600f;

    if (pointerY > topEdge - scrollZone)
    {
        // 위쪽 경계 → 위로 스크롤
        float t = (pointerY - (topEdge - scrollZone)) / scrollZone;
        _scrollRect.verticalNormalizedPosition +=
            t * scrollSpeed * Time.unscaledDeltaTime /
            _scrollRect.content.rect.height;
    }
    else if (pointerY < bottomEdge + scrollZone)
    {
        // 아래쪽 경계 → 아래로 스크롤
        float t = ((bottomEdge + scrollZone) - pointerY) / scrollZone;
        _scrollRect.verticalNormalizedPosition -=
            t * scrollSpeed * Time.unscaledDeltaTime /
            _scrollRect.content.rect.height;
    }

    _scrollRect.verticalNormalizedPosition =
        Mathf.Clamp01(_scrollRect.verticalNormalizedPosition);
}
```

**ScrollRect와 IBeginDragHandler 충돌 회피**: ScrollRect도 IDragHandler를 구현하므로 이벤트가 양쪽에 전달되는 문제가 있다. 해결책은 `IInitializePotentialDragHandler.OnInitializePotentialDrag`에서 `eventData.useDragThreshold = false`를 설정하고, 드래그 시작 시 ScrollRect.OnBeginDrag를 명시적으로 차단하거나 `_scrollRect.enabled = false`로 일시 비활성화한다.

---

### 2.10 R3 통합 — Observable 드래그 스트림

MV(R)P 패턴에서 Presenter가 드래그 이벤트를 구독:

```csharp
// SortableListItem에서 R3 Observable 노출
public class SortableListItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Subject<int> _onDragBegin = new();
    private Subject<(int fromIndex, int toIndex)> _onDragEnd = new();

    public Observable<int> OnDragBegin => _onDragBegin;
    public Observable<(int, int)> OnDragEnd => _onDragEnd;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _onDragBegin.OnNext(transform.GetSiblingIndex());
        // ... 드래그 로직
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        int finalIndex = _listController.GetPlaceholderIndex();
        _onDragEnd.OnNext((_originalSiblingIndex, finalIndex));
        // ... 복귀 로직
    }
}

// Presenter에서 구독 (비즈니스 로직 분리)
public class TaskListPresenter : IDisposable
{
    private CompositeDisposable _disposables = new();

    public void Initialize(SortableListItem item)
    {
        item.OnDragEnd
            .Subscribe(data =>
            {
                var (fromIndex, toIndex) = data;
                if (fromIndex != toIndex)
                    _model.ReorderTask(fromIndex, toIndex);
            })
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
```

**VContainer 통합**: Presenter를 VContainer로 주입받고, `IInitializable`에서 구독 설정. SortableListItem은 `IObjectResolver.Inject()`로 Presenter 주입 또는 이벤트 버스 패턴 사용.

---

### 2.11 오픈소스 레퍼런스 정리

| 저장소 | 특징 | 접근 방식 |
|---|---|---|
| [FullStackForger/ifup-ui-sortable-list](https://github.com/FullStackForger/ifup-ui-sortable-list) | Unity UGUI 전용 Sortable List, 리스트 간 이동 지원 | SortableListItem + SortableListManager |
| [tetr4lab/ReOrderableList](https://github.com/tetr4lab/ReOrderableList) | VerticalLayoutGroup + ScrollRect 전용, Long-Press, Marker 기반 인덱스 계산 | Dummy 오브젝트 + SiblingIndex swap |
| [Unity UI Extensions ReorderableList](https://unity-ui-extensions.github.io/Controls/ReorderableList.html) | Vertical/Horizontal/Grid 지원, 이벤트 콜백 풍부, ScrollRect 자동 스크롤 | placeholder + reparent |
| [tjcccc/ugui-dragdrop](https://github.com/tjcccc/ugui-dragdrop) | DOTween으로 위치 교환 애니메이션, Grid 지원 | 매트릭스 순서 교환 |
| [dipen-apptrait/Vertical-drag-drop-listview-unity](https://github.com/dipen-apptrait/Vertical-drag-drop-listview-unity) | 수직 드래그 앤 드롭 커스텀 구현 | DragController.cs |

**Unity UI Extensions 설치**:
```
// Packages/manifest.json
{
  "dependencies": {
    "com.unity.uiextensions": "https://github.com/Unity-Technologies/com.unity.uiextensions.git#release/2.3.2"
  }
}
```

---

## 3. 베스트 프랙티스

### DO (권장)
- [x] Placeholder 오브젝트로 원위치 갭 유지 — LayoutGroup 자동 처리 활용
- [x] 드래그 항목을 Canvas 루트 / 전용 DragLayer로 reparent + SetAsLastSibling
- [x] CanvasGroup.blocksRaycasts = false로 Placeholder가 이벤트를 받게 설정
- [x] worldPositionStays: true로 reparent하여 화면 위치 유지
- [x] DOTween으로 스케일 1.05 + alpha 0.95 "리프트" 효과 (0.12s OutBack)
- [x] GetWorldCorners로 각 항목의 중간점 Y 계산 → 삽입 인덱스 결정
- [x] R3 Subject로 드래그 이벤트 Observable 노출 → Presenter에서 비즈니스 로직
- [x] 모바일: Long-Press (0.3~0.5s) + `Handheld.Vibrate()` 햅틱
- [x] ScrollView 안: 경계 감지 자동 스크롤 구현
- [x] IBeginDragHandler 없이 IDragHandler만 구현하지 않기 (EventSystem 요구사항)

### DON'T (금지)
- [ ] 드래그 중 LayoutGroup 컨테이너 안에서 anchoredPosition 직접 수정 (LayoutGroup이 덮어씀)
- [ ] `eventData.delta / canvas.scaleFactor` 누적 방식 사용 (Canvas 좌표계 오차 누적)
- [ ] Placeholder 없이 드래그 항목만 이동 (항목이 사라지는 것처럼 보이는 나쁜 UX)
- [ ] VerticalLayoutGroup padding을 크게 설정 (마커/위치 계산 오차 발생)
- [ ] IPointerDownHandler와 IDropHandler를 동일 오브젝트에 함께 구현 (알려진 버그, Unity 2020.1 이하)
- [ ] EventTrigger 컴포넌트 사용 (이벤트가 예측 불가하게 사라지는 문제)

### CONSIDER (상황별)
- [ ] 항목 높이가 불균일하면 GetWorldCorners 방식으로 각 항목 중간점 개별 계산
- [ ] 부드러운 슬라이드가 필요하면 LayoutGroup 비활성화 + DOTween 수동 위치 갱신 (복잡도 증가)
- [ ] 리스트 간 이동이 필요하면 FullStackForger/ifup-ui-sortable-list 참조
- [ ] Unity UI Extensions ReorderableList는 수직/수평/그리드 모두 필요할 때 적합

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 2021.3+ / 6000.x | GetWorldCorners, LayoutGroup API 안정 |
| UGUI | 1.0+ / 2.0+ | IBeginDragHandler 등 EventSystem 인터페이스 동일 |
| DOTween | 1.2.705+ | DOAnchorPos, DOScale, DOFade, DOVirtual.DelayedCall |
| R3 | 1.0+ | Subject, Observable, AddTo |
| VContainer | 1.x | IInitializable, Inject |
| Unity UI Extensions | 2.3.2 | ReorderableList, ScrollRect 자동 스크롤 포함 |

---

## 5. 구현 구조 설계 (UI_Study 예제용)

### 씬 계층 구조

```
Canvas (Screen Space - Overlay)
├── DragLayer          ← 드래그 중인 항목이 여기로 이동 (빈 RectTransform, Stretch)
└── ListPanel
    ├── Header
    └── ScrollView
        └── Viewport
            └── Content   ← VerticalLayoutGroup + ContentSizeFitter
                ├── Item_0 (SortableListItem)
                │     ├── Background (Image)
                │     ├── Label (TMP)
                │     └── DragHandle (Image + LongPressDragInitiator, 모바일)
                ├── Item_1
                └── Item_2
```

### 컴포넌트 구성

```
Content:
  - VerticalLayoutGroup (spacing: 8, padding: 8, childForceExpandWidth: true)
  - ContentSizeFitter (verticalFit: PreferredSize)
  - SortableListController

Item_N:
  - RectTransform
  - CanvasGroup
  - LayoutElement (preferredHeight: 64)
  - SortableListItem
  - Image (background)

Placeholder Prefab:
  - RectTransform
  - LayoutElement (preferredHeight: 동적 할당)
  - Image (alpha: 0.15, 점선 테두리 sprite 선택적)
  - CanvasGroup (blocksRaycasts: true)
```

---

## 6. UI_Study 적용 계획

이 리서치 기반으로 UI_Study에서 구현할 예제:

1. **기본 Sortable List** — Placeholder + Reparent 방식, 즉각 SiblingIndex 교환
2. **부드러운 슬라이드** — DOTween으로 다른 항목들 animate (LayoutGroup 비활성화 방식)
3. **모바일 Long-Press 드래그** — LongPressDragInitiator + 햅틱
4. **ScrollView 안 Sortable List** — 자동 스크롤 포함
5. **R3 + MVP 통합** — Observable 이벤트, Presenter 분리

---

## 7. 참고 자료

1. [Unity Forum: List Order Items by Dragging](https://discussions.unity.com/t/list-order-items-by-dragging/569383)
2. [Unity Forum: Free Reorderable List (UI Extensions 원본)](https://discussions.unity.com/threads/free-reorderable-list.364600/)
3. [GitHub: FullStackForger/ifup-ui-sortable-list](https://github.com/FullStackForger/ifup-ui-sortable-list)
4. [GitHub: tetr4lab/ReOrderableList](https://github.com/tetr4lab/ReOrderableList)
5. [Unity UI Extensions: ReorderableList](https://unity-ui-extensions.github.io/Controls/ReorderableList.html)
6. [GitHub: tjcccc/ugui-dragdrop (DOTween 위치 교환)](https://github.com/tjcccc/ugui-dragdrop)
7. [GitHub: dipen-apptrait/Vertical-drag-drop-listview-unity](https://github.com/dipen-apptrait/Vertical-drag-drop-listview-unity)
8. [Medium: Drag & Drop for UI elements — Jonas Hundertmark](https://medium.com/medialesson/drag-drop-for-ui-elements-in-unity-the-simple-ish-way-9efcb4617648)
9. [GeneralistProgrammer: Unity Drag Drop Complete Tutorial 2025](https://generalistprogrammer.com/tutorials/unity-drag-drop-system-complete-tutorial-ui-gameplay)
10. [Unity Forum: Smooth movement when sibling index changes](https://discussions.unity.com/t/smooth-movement-when-sibling-index-changes-in-a-layout-group/706767)
11. [Stray Pixels: A Technique for Animating a Unity UI Layout Group](https://straypixels.net/layoutgroup-animation/)
12. [TantzyGames: Unity UGUI Button Long Press](https://www.tantzygames.com/blog/unity-ugui-button-long-press/)
13. [Unity Forum: Tweening elements in a layout group](https://discussions.unity.com/t/tweening-elements-in-a-layout-group/550463)
14. [Unity Scripting API: Transform.SetSiblingIndex](https://docs.unity3d.com/ScriptReference/Transform.SetSiblingIndex.html)

---

## 8. 미해결 질문

- [ ] DOTween "부드러운 슬라이드" 방식(전략 B)에서 LayoutGroup 비활성화 중 ContentSizeFitter 동작 검증 필요
- [ ] Unity 6의 Canvas 새 렌더링 파이프라인에서 DragLayer reparent 시 렌더 순서 보장 확인
- [ ] ScrollRect와 SortableList 동시 드래그 시 이벤트 전파 충돌 재현 테스트
- [ ] R3 Subject vs ReactiveProperty: 드래그 상태(bool isDragging)를 ReactiveProperty로 관리하는 게 더 나은지 검토
