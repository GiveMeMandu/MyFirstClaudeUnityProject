# UGUI ScrollView 풀링 & 버추얼 스크롤 리서치 리포트

- **작성일**: 2026-03-28
- **카테고리**: practice
- **상태**: 조사완료

---

## 1. 요약

Unity UGUI ScrollRect는 아이템이 100개를 넘어가면 모바일에서 60FPS → 15FPS로 급락할 수 있으며, **버추얼 스크롤(뷰포트에 보이는 셀만 유지·재활용)** 이 유일한 근본 해법이다. 오픈소스 솔루션은 **LoopScrollRect**(가장 성숙·무료), **FancyScrollView**(애니메이션 특화), **EnhancedScroller**(유료·데이터 드리븐)로 3분되며, 프로젝트 기술 스택(MV(R)P + VContainer + R3)과의 통합은 **ObservableCollections.R3**의 `ISynchronizedView` + 커스텀 셀 Presenter 패턴으로 구성한다. Canvas 성능은 ScrollRect를 **독립 서브 Canvas**에 분리하고 **RectMask2D**, **RaycastTarget 비활성화**, **Pixel Perfect OFF**, **LayoutGroup 제거**를 조합하면 드로우 콜을 최대 80% 절감할 수 있다.

---

## 2. 상세 분석

### 2.1 버추얼 스크롤의 필요성

#### 표준 ScrollRect의 문제점

표준 `ScrollRect + VerticalLayoutGroup + ContentSizeFitter` 구성은 데이터가 많아질수록 다음 세 가지 비용이 모두 선형으로 증가한다.

| 비용 항목 | 원인 | 영향 |
|---|---|---|
| GameObject 생성 비용 | 아이템 수 = 인스턴스 수 | 초기 로드 시간 |
| Canvas 배치(Batching) 비용 | 전체 Content 변경 시 전체 Canvas dirty | 매 스크롤 프레임 |
| LayoutGroup 재계산 | `MarkLayoutForRebuild` 계층 순회 + `GetComponents` 루프 | 매 프레임 |

실측 데이터 기준: **100~200개 이하**는 모바일에서 허용 가능, 그 이상은 풀링이 사실상 필수이다. 풀링 적용 시 오버헤드를 약 98% 감소시켜 수천 개 아이템도 부드럽게 처리 가능하다.

#### 버추얼 스크롤 원리

```
전체 데이터: 10,000개
뷰포트에 보이는 아이템: 8개
실제 인스턴스 수: 8 + 버퍼(2~4)개 = 약 10~12개

스크롤 발생 시:
  뷰포트 아래로 사라진 셀 → RectTransform 위치만 이동 → 새 인덱스 데이터로 재설정
  새로 진입하는 셀 → 풀에서 꺼내어 데이터 바인딩
```

핵심은 **셀 GameObject를 Destroy/Instantiate하지 않고 RectTransform 위치만 변경**하여 재활용하는 것이다.

---

### 2.2 수동 풀링 구현 (라이브러리 없이)

기본 원리를 이해하기 위한 최소 구현. 고정 높이 아이템 기준.

#### 셀 인터페이스 정의

```csharp
// 셀이 구현해야 할 인터페이스
public interface IScrollCell<TData>
{
    void Bind(TData data, int index);
}
```

#### 풀 관리자

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PooledScrollView<TData> : MonoBehaviour
{
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] RectTransform content;
    [SerializeField] GameObject cellPrefab;
    [SerializeField] float cellHeight = 100f;
    [SerializeField] float spacing = 4f;
    [SerializeField] int bufferCount = 2;

    IList<TData> dataList;
    readonly Queue<RectTransform> pool = new();
    readonly Dictionary<int, RectTransform> visibleCells = new();
    int firstVisibleIndex = -1;
    int lastVisibleIndex = -1;

    float CellStep => cellHeight + spacing;
    int TotalCount => dataList?.Count ?? 0;

    void Awake()
    {
        scrollRect.onValueChanged.AddListener(_ => RefreshVisible());
    }

    public void SetData(IList<TData> data)
    {
        dataList = data;
        // Content 높이 설정
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            TotalCount * CellStep - spacing);
        ReturnAllToPool();
        RefreshVisible();
    }

    void RefreshVisible()
    {
        if (dataList == null) return;

        float viewportHeight = scrollRect.viewport.rect.height;
        float scrollY = content.anchoredPosition.y;

        // 보여야 할 인덱스 범위 계산
        int newFirst = Mathf.Max(0, Mathf.FloorToInt(scrollY / CellStep) - bufferCount);
        int newLast  = Mathf.Min(TotalCount - 1,
            Mathf.CeilToInt((scrollY + viewportHeight) / CellStep) + bufferCount);

        // 범위 밖 셀 반환
        var toRemove = new List<int>();
        foreach (var kv in visibleCells)
        {
            if (kv.Key < newFirst || kv.Key > newLast)
            {
                ReturnCell(kv.Value);
                toRemove.Add(kv.Key);
            }
        }
        foreach (var idx in toRemove) visibleCells.Remove(idx);

        // 새 범위 셀 활성화
        for (int i = newFirst; i <= newLast; i++)
        {
            if (visibleCells.ContainsKey(i)) continue;
            var cell = GetCell();
            PositionCell(cell, i);
            cell.GetComponent<IScrollCell<TData>>()?.Bind(dataList[i], i);
            visibleCells[i] = cell;
        }
    }

    RectTransform GetCell()
    {
        if (pool.Count > 0)
        {
            var c = pool.Dequeue();
            c.gameObject.SetActive(true);
            return c;
        }
        var go = Instantiate(cellPrefab, content);
        return go.GetComponent<RectTransform>();
    }

    void ReturnCell(RectTransform cell)
    {
        cell.gameObject.SetActive(false);
        pool.Enqueue(cell);
    }

    void ReturnAllToPool()
    {
        foreach (var kv in visibleCells) ReturnCell(kv.Value);
        visibleCells.Clear();
    }

    void PositionCell(RectTransform cell, int index)
    {
        // 상단 기준 anchoredPosition
        cell.anchorMin = new Vector2(0, 1);
        cell.anchorMax = new Vector2(1, 1);
        cell.pivot     = new Vector2(0.5f, 1);
        cell.sizeDelta = new Vector2(0, cellHeight);
        cell.anchoredPosition = new Vector2(0, -(index * CellStep));
    }
}
```

**한계**: 가변 높이 아이템은 각 인덱스의 누적 높이 테이블을 별도 관리해야 하며 복잡도가 급격히 증가한다. 이 경우 오픈소스 라이브러리 사용을 권장한다.

---

### 2.3 오픈소스 솔루션 비교

#### LoopScrollRect

- **GitHub**: `https://github.com/qiankanglai/LoopScrollRect`
- **설치**: `openupm add me.qiankanglai.loopscrollrect` 또는 Git URL UPM
- **라이선스**: MIT
- **별점**: ~3.5k
- **최종 활동**: 2024년까지 활성 유지

**동작 방식**: 표준 `ScrollRect`를 상속하여 셀 재사용 레이어를 추가한다. 스크롤 이벤트마다 뷰포트 경계를 벗어난 셀을 풀로 반환하고, 새로 진입하는 인덱스에 풀 셀을 꺼내 데이터를 채운다.

**필수 구현 인터페이스**:

```csharp
// 1) 셀 프리팹 제공 (캐시 풀 포함)
public class MyPrefabSource : MonoBehaviour, LoopScrollPrefabSource
{
    public GameObject prefab;
    readonly Stack<Transform> pool = new();

    public Transform GetObject(int index)
    {
        if (pool.Count > 0)
        {
            var candidate = pool.Pop();
            candidate.gameObject.SetActive(true);
            return candidate;
        }
        return Instantiate(prefab).transform;
    }

    public void ReturnObject(Transform trans)
    {
        trans.gameObject.SetActive(false);
        trans.SetParent(transform);
        pool.Push(trans);
    }
}

// 2) 셀에 데이터 주입 (MonoBehaviour에 추가)
public class MyDataSource : MonoBehaviour, LoopScrollDataSource
{
    public string[] dataArray;

    public void ProvideData(Transform transform, int idx)
    {
        var cell = transform.GetComponent<MyCell>();
        cell.SetData(dataArray[idx], idx);
    }
}
```

**셀 설정**:

```csharp
// 셀 프리팹에 붙는 스크립트
public class MyCell : MonoBehaviour
{
    [SerializeField] Text label;
    [SerializeField] Image icon;

    public void SetData(string data, int index)
    {
        label.text = data;
    }
}
```

**Inspector 설정**:
- `LoopVerticalScrollRect` (또는 Horizontal) 컴포넌트 추가
- `PrefabSource` 및 `DataSource` 필드 연결
- `TotalCount` 설정 (음수 = 무한 스크롤)
- 셀 프리팹에 `LayoutElement` 컴포넌트로 `preferredHeight` 명시 필수

**핵심 메서드**:

```csharp
loopScroll.totalCount = dataArray.Length;   // 데이터 수 변경
loopScroll.RefillCells();                   // 처음부터 재구성
loopScroll.RefreshCells();                  // 레이아웃 유지, 데이터만 갱신
loopScroll.ScrollToCell(index, speed);      // 특정 인덱스로 스크롤
```

**장점**:
- 무료·MIT, 가장 널리 사용됨
- 무한 스크롤 기본 지원 (`totalCount = -1`)
- 가변 크기 셀 지원 (`LoopScrollSizeHelper`)
- 수평/수직 모두 지원

**단점**:
- 셀마다 `LayoutElement` 컴포넌트 필수 → LayoutGroup 의존
- "rebuild loop" 관련 내부 경고 이슈 (GitHub Issue #11, #19)
- 공식 문서 빈약, 샘플 코드 의존

---

#### FancyScrollView

- **GitHub**: `https://github.com/setchi/FancyScrollView`
- **설치**: Git URL UPM (`...FancyScrollView.git#upm`) 또는 Asset Store
- **라이선스**: MIT
- **최신 버전**: v1.9.0 (2025-09)
- **Unity 요구사항**: 2019.4+ (.NET 4.x)

**동작 방식**: 셀이 뷰포트 내 정규화 위치(0.0~1.0)를 받아 **자체적으로 위치와 외관을 제어**한다. 스크롤 라이브러리보다 **애니메이션 프레임워크**에 가깝다.

```csharp
// 1) 데이터 모델
public class ItemData
{
    public string Name;
    public Sprite Icon;
}

// 2) 셀 View
public class MyCell : FancyCell<ItemData>
{
    [SerializeField] Text nameLabel;
    [SerializeField] Image iconImage;

    // 데이터 바인딩
    public override void UpdateContent(ItemData itemData)
    {
        nameLabel.text = itemData.Name;
        iconImage.sprite = itemData.Icon;
    }

    // 스크롤 위치(0~1)로 셀 연출 제어
    public override void UpdatePosition(float position)
    {
        // position = 0.5가 화면 중앙
        var scale = Mathf.Lerp(0.7f, 1.0f, 1f - Mathf.Abs(position - 0.5f) * 2f);
        transform.localScale = Vector3.one * scale;
    }
}

// 3) ScrollView
public class MyScrollView : FancyScrollView<ItemData>
{
    [SerializeField] Scroller scroller;
    [SerializeField] GameObject cellPrefab;

    void Awake()
    {
        base.cellPrefab = cellPrefab.GetComponent<FancyCell<ItemData>>();
        scroller.OnValueChanged(base.UpdatePosition);
    }

    public void UpdateItems(IList<ItemData> items)
    {
        UpdateContents(items);
    }
}
```

**장점**:
- 셀별 애니메이션 연출이 자유로움 (캐러셀, 원형 스크롤 등)
- 셀 재활용 내장
- 스냅핑/무한 스크롤 지원

**단점**:
- 데이터 바인딩 방식이 인덱스 기반이 아닌 정규화 위치 기반 → MVP 통합 시 별도 인덱스 관리 필요
- 순수 목록 UI(퀘스트 목록, 아이템 인벤토리)에는 오버스펙
- LoopScrollRect보다 커뮤니티 사례 적음

---

#### EnhancedScroller

- **Asset Store**: `https://assetstore.unity.com/packages/tools/gui/enhancedscroller-36378`
- **가격**: 유료 (~$30)
- **라이선스**: Unity Asset Store EULA

**동작 방식**: `ScrollRect`를 래핑하며 `EnhancedScrollerCellView`를 상속한 셀 프리팹을 데이터 드리븐 방식으로 관리한다. 셀뷰 재활용은 자동으로 처리된다.

```csharp
// 데이터 컨트롤러 (IEnhancedScrollerDelegate 구현)
public class MyController : MonoBehaviour, IEnhancedScrollerDelegate
{
    [SerializeField] EnhancedScroller scroller;
    [SerializeField] EnhancedScrollerCellView cellPrefab;
    List<MyData> data = new();

    void Start()
    {
        scroller.Delegate = this;
        scroller.ReloadData();
    }

    // 총 아이템 수
    public int GetNumberOfCells(EnhancedScroller s) => data.Count;

    // 셀 높이 (가변 가능)
    public float GetCellViewSize(EnhancedScroller s, int dataIndex) => 100f;

    // 셀 뷰 제공
    public EnhancedScrollerCellView GetCellView(EnhancedScroller s,
        int dataIndex, int cellIndex)
    {
        var cell = s.GetCellView(cellPrefab) as MyCellView;
        cell.SetData(data[dataIndex]);
        return cell;
    }
}

// 셀 뷰
public class MyCellView : EnhancedScrollerCellView
{
    [SerializeField] Text label;

    public void SetData(MyData data)
    {
        label.text = data.Name;
    }
}
```

**장점**:
- 풍부한 공식 예제 및 문서
- 가변 크기 셀 기본 지원
- 데이터 드리븐 델리게이트 패턴으로 통합 용이
- 수평/수직/그리드 지원

**단점**:
- 유료
- Asset Store EULA로 오픈소스 프로젝트 배포 제한

---

#### 비교 표

| 기준 | LoopScrollRect | FancyScrollView | EnhancedScroller |
|---|---|---|---|
| 가격 | 무료 (MIT) | 무료 (MIT) | 유료 (~$30) |
| 설치 | UPM/OpenUPM | UPM/OpenUPM | Asset Store |
| 주 목적 | 대용량 목록 최적화 | 애니메이션 스크롤 | 데이터 드리븐 목록 |
| 가변 셀 크기 | 지원 (SizeHelper) | 지원 | 기본 지원 |
| 무한 스크롤 | 기본 지원 | 기본 지원 | 지원 |
| MVP 통합 난이도 | 보통 | 높음 | 쉬움 |
| Unity 6 호환 | 확인됨 | v1.9.0 (2025-09) | 확인됨 |
| 커뮤니티 크기 | 크다 (3.5k stars) | 중간 (2k+ stars) | 유료 포럼 |
| 문서 품질 | 보통 | 좋음 | 우수 |

**추천**:
- 일반 목록 UI (인벤토리, 퀘스트 목록) → **LoopScrollRect**
- 캐러셀/카드 슬라이드 등 연출 중심 → **FancyScrollView**
- 팀 프로젝트 + 충분한 예산 → **EnhancedScroller**
- 이미 확정 스택(FancyScrollView) → **FancyScrollView**

---

### 2.4 MV(R)P + VContainer + R3 통합 패턴

#### ObservableCollections.R3 활용

`UniRx.ReactiveCollection`은 R3에서 제거되었다. 대신 **ObservableCollections** + **ObservableCollections.R3** 패키지를 사용한다.

```
// 설치 (Git URL UPM)
https://github.com/Cysharp/ObservableCollections.git?path=src/ObservableCollections/Assets/ObservableCollections
https://github.com/Cysharp/ObservableCollections.git?path=src/ObservableCollections.R3/Assets/ObservableCollections.R3
```

#### 아키텍처 구조

```
Model (ObservableList<ItemData>)
  └─ ISynchronizedView<ItemData, ItemViewModel>
       └─ Presenter (구독 + VContainer 주입)
            └─ LoopScrollRect 또는 커스텀 풀 뷰
                 └─ 셀 View (MonoBehaviour)
```

#### 완전한 통합 예시

```csharp
// === Model ===
// 순수 C# 데이터 (MonoBehaviour 아님)
public class ItemData
{
    public int Id;
    public string Name;
    public int Count;
}

public class InventoryModel
{
    // ObservableList가 변경 이벤트를 방출함
    public readonly ObservableList<ItemData> Items = new();

    public void AddItem(ItemData item) => Items.Add(item);
    public void RemoveItem(int id) => Items.RemoveAll(x => x.Id == id);
    public void UpdateCount(int id, int delta)
    {
        var idx = Items.IndexOf(Items.FirstOrDefault(x => x.Id == id));
        if (idx < 0) return;
        var old = Items[idx];
        Items[idx] = new ItemData { Id = old.Id, Name = old.Name, Count = old.Count + delta };
    }
}
```

```csharp
// === View (MonoBehaviour — 셀) ===
public class InventoryCellView : MonoBehaviour
{
    [SerializeField] Text itemName;
    [SerializeField] Text itemCount;
    [SerializeField] Button button;

    // 셀이 눌렸을 때 인덱스를 외부에 알림
    public IObservable<int> OnCellClicked => button.OnClickAsObservable()
        .Select(_ => currentIndex);

    int currentIndex;

    // Presenter가 호출
    public void Bind(ItemData data, int index)
    {
        currentIndex = index;
        itemName.text = data.Name;
        itemCount.text = data.Count.ToString();
    }
}
```

```csharp
// === View (MonoBehaviour — ScrollView 컨테이너) ===
public class InventoryScrollView : MonoBehaviour
{
    [SerializeField] LoopVerticalScrollRect loopScroll;
    [SerializeField] MyPrefabSource prefabSource;   // LoopScrollPrefabSource 구현체

    // 내부 데이터 스냅샷 (Presenter가 갱신)
    ItemData[] snapshot = Array.Empty<ItemData>();

    // LoopScrollDataSource 인터페이스를 View 내부에서 구현
    internal void ProvideData(Transform t, int idx)
    {
        t.GetComponent<InventoryCellView>()?.Bind(snapshot[idx], idx);
    }

    // Presenter가 호출
    public void UpdateList(ItemData[] newSnapshot)
    {
        snapshot = newSnapshot;
        loopScroll.totalCount = snapshot.Length;
        loopScroll.RefillCells();
    }

    public void RefreshList(ItemData[] newSnapshot)
    {
        snapshot = newSnapshot;
        loopScroll.RefreshCells();   // 레이아웃 유지, 데이터만 갱신
    }
}
```

```csharp
// === Presenter (VContainer EntryPoint) ===
public class InventoryPresenter : IStartable, IDisposable
{
    readonly InventoryModel model;
    readonly InventoryScrollView view;
    readonly CompositeDisposable disposables = new();

    // VContainer가 생성자 주입
    public InventoryPresenter(InventoryModel model, InventoryScrollView view)
    {
        this.model = model;
        this.view  = view;
    }

    public void Start()
    {
        // 전체 컬렉션 변경 구독 (Add/Remove/Replace/Reset)
        // ObservableCollections.R3 사용
        model.Items
            .ObserveChanged()                        // R3 Observable<CollectionChangedEvent>
            .ObserveOnMainThread()
            .Subscribe(_ => SyncView())
            .AddTo(disposables);

        // 초기 동기화
        SyncView();
    }

    void SyncView()
    {
        // ISynchronizedView를 통해 스냅샷 생성 (필터/정렬 적용 가능)
        view.UpdateList(model.Items.ToArray());
    }

    public void Dispose() => disposables.Dispose();
}
```

```csharp
// === LifetimeScope ===
public class InventoryLifetimeScope : LifetimeScope
{
    [SerializeField] InventoryScrollView scrollView;

    protected override void Configure(IContainerBuilder builder)
    {
        // Model
        builder.Register<InventoryModel>(Lifetime.Scoped);

        // View (씬에 배치된 MonoBehaviour)
        builder.RegisterComponent(scrollView);

        // Presenter (EntryPoint로 등록 → IStartable 자동 호출)
        builder.RegisterEntryPoint<InventoryPresenter>();
    }
}
```

#### ISynchronizedView를 활용한 필터/정렬

```csharp
// Presenter 내부에서 뷰 생성 후 필터 적용
var syncView = model.Items.CreateView(item => item);  // identity transform
syncView.AttachFilter(item => item.Count > 0);        // 0개 아이템 숨기기

// 정렬이 필요한 경우: 별도 Sorted 래퍼 사용
var sorted = model.Items.CreateSortedView(
    keySelector: item => item.Name,
    comparer: StringComparer.Ordinal);
```

#### 고급: R3 스트림으로 세분화된 구독

```csharp
// Add만 구독 (성능 최적화 — 전체 리프레시 피하기)
model.Items
    .ObserveAdd()
    .ObserveOnMainThread()
    .Subscribe(e =>
    {
        // 추가된 아이템만 처리하여 전체 RefillCells 회피
        view.RefreshList(model.Items.ToArray());
    })
    .AddTo(disposables);

// Remove 구독
model.Items
    .ObserveRemove()
    .ObserveOnMainThread()
    .Subscribe(e => view.UpdateList(model.Items.ToArray()))
    .AddTo(disposables);
```

---

### 2.5 Canvas 성능 최적화

#### 독립 서브 Canvas 전략

ScrollRect의 Content를 별도 Canvas로 분리하면 스크롤 중 발생하는 Canvas dirty가 외부 UI에 전파되지 않는다.

```
[루트 Canvas]
  ├── [정적 UI] (배경, 타이틀 — 별도 재구성 없음)
  ├── [동적 UI Canvas] (스탯, 타이머 등 자주 변하는 것)
  └── [InventoryScrollView]
        └── [서브 Canvas]  ← Pixel Perfect OFF, Override Sorting ON
              └── [Viewport (RectMask2D)]
                    └── [Content]
                          └── 셀들 (재사용)
```

**핵심 설정**:
- 서브 Canvas의 `Pixel Perfect` = **OFF**
- 서브 Canvas의 `Override Sorting` = **ON** (Raycaster 계층 순회 차단)
- Viewport에 **RectMask2D** (Mask 컴포넌트 대신)

#### RectMask2D vs Mask

| 항목 | RectMask2D | Mask |
|---|---|---|
| 방식 | Shader 기반 클리핑 | Stencil Buffer |
| 추가 드로우 콜 | 없음 | +2 (스텐실 쓰기/지우기) |
| 모양 제한 | 직사각형만 | 임의 모양 가능 |
| 배치 영향 | 외부 요소와 배치 분리 | 내부 요소끼리 배치 유지 |
| ScrollRect 권장 | **권장** | 비권장 |

실측: RectMask2D 적용만으로 드로우 콜 26~30 → 6~7로 감소 사례 있음.

#### RaycastTarget 최소화

```csharp
// 에디터 유틸리티: 하위 모든 비인터랙티브 요소 RaycastTarget OFF
[MenuItem("Tools/UI/Disable Raycast Target on Non-Interactive")]
static void DisableRaycast()
{
    var selected = Selection.activeGameObject;
    if (!selected) return;

    foreach (var graphic in selected.GetComponentsInChildren<Graphic>())
    {
        // Button, Toggle, Slider 등 인터랙티브 요소는 제외
        bool isInteractive = graphic.GetComponent<Selectable>() != null;
        if (!isInteractive)
            graphic.raycastTarget = false;
    }
}
```

**규칙**:
- `Text`, `Image` (장식용) → `raycastTarget = false`
- `Button` 배경 Image에도 개별 raycastTarget 불필요 → Button에 단일 Collider

#### LayoutGroup / ContentSizeFitter 대안

`VerticalLayoutGroup`은 자식 수에 비례해 `GetComponents` 루프를 실행한다. 고정 높이 아이템 목록에서는 수동 `RectTransform` 위치 설정이 훨씬 빠르다.

```csharp
// VerticalLayoutGroup 제거 후 수동 배치
void LayoutCells(RectTransform[] cells, float cellHeight, float spacing)
{
    for (int i = 0; i < cells.Length; i++)
    {
        cells[i].anchorMin = new Vector2(0, 1);
        cells[i].anchorMax = new Vector2(1, 1);
        cells[i].pivot     = new Vector2(0.5f, 1);
        cells[i].sizeDelta = new Vector2(0, cellHeight);
        cells[i].anchoredPosition = new Vector2(0, -(i * (cellHeight + spacing)));
    }
}

// Content 높이도 수동 설정
content.SetSizeWithCurrentAnchors(
    RectTransform.Axis.Vertical,
    cells.Length * (cellHeight + spacing) - spacing);
```

#### UI Animator 금지

`Animator` 컴포넌트를 UI에 사용하면 시각적 변화 없이도 **매 프레임 레이아웃을 dirty** 처리한다.

```csharp
// BAD: UI에 Animator 사용
animator.SetTrigger("Show");

// GOOD: DOTween 사용 (DOTween은 R3/UGUI와 잘 통합됨)
canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
rectTransform.DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutBack);
```

#### Canvas 컴포넌트 비활성화 vs GameObject 비활성화

```csharp
// BAD: GameObject 비활성화 → OnEnable 시 Text 재빌드 비용 큼
panel.SetActive(false);
panel.SetActive(true);  // 재활성화 시 Heavy rebuild

// GOOD: Canvas 컴포넌트 비활성화 → 렌더링만 중단, 레이아웃 유지
panel.GetComponent<Canvas>().enabled = false;
panel.GetComponent<Canvas>().enabled = true;  // 재활성화 저비용

// 또는 CanvasGroup.alpha = 0으로 숨기기 (레이아웃 완전 유지)
canvasGroup.alpha = 0f;
canvasGroup.blocksRaycasts = false;
```

---

### 2.6 ContentSizeFitter + LayoutGroup 성능 이슈

#### 비용 발생 메커니즘

```
ContentSizeFitter.OnRectTransformDimensionsChange()
  └── LayoutRebuilder.MarkLayoutForRebuild(rect)
        └── 계층 위로 순회 → LayoutGroup 루트 탐색
              └── Canvas.willRenderCanvases 이벤트에 등록
                    └── 다음 프레임에 LayoutGroup.CalculateLayout() 실행
                          └── foreach(child) child.GetComponents(ILayoutIgnorer)  ← 루프
```

`ContentSizeFitter`를 스크롤 목록 아이템마다 붙이면 이 루프가 **아이템 수 × 프레임당 수회** 실행될 수 있다.

#### 언제 써도 되는가

| 사용 | 권장 여부 | 이유 |
|---|---|---|
| 고정 높이 풀링 셀 | **사용 금지** | 매 프레임 재계산 |
| 채팅 말풍선 (동적 텍스트) | **1회 사용 후 제거** | 크기 계산 후 `Destroy(csf)` |
| 팝업 다이얼로그 (열 때만) | **조건부 허용** | 빈도 낮으면 허용 가능 |
| 스크롤 목록 Content | **사용 금지** | RectTransform 수동 설정 |

#### 동적 텍스트 말풍선 패턴

```csharp
// 텍스트 크기를 계산하고 ContentSizeFitter를 제거하는 패턴
IEnumerator FitAndRemove(Text textComponent, ContentSizeFitter fitter)
{
    textComponent.text = "some dynamic text...";
    // 한 프레임 대기해서 레이아웃 계산 완료
    yield return null;
    // 이후 크기 고정, CSF 제거
    Destroy(fitter);
}

// UniTask 버전
async UniTaskVoid FitAndRemoveAsync(Text textComponent, ContentSizeFitter fitter)
{
    textComponent.text = "some dynamic text...";
    await UniTask.NextFrame();
    Destroy(fitter);
}
```

---

### 2.7 무한 스크롤 / 페이지네이션 패턴

#### 무한 스크롤 (LoopScrollRect)

```csharp
// totalCount = -1 설정만으로 무한 스크롤 활성화
// DataSource에서 인덱스 모듈러 연산으로 순환
public class InfiniteDataSource : MonoBehaviour, LoopScrollDataSource
{
    string[] data = { "Item A", "Item B", "Item C" };

    public void ProvideData(Transform t, int idx)
    {
        // 모듈러로 순환 (음수 인덱스 처리 포함)
        int realIdx = ((idx % data.Length) + data.Length) % data.Length;
        t.GetComponent<MyCell>().SetLabel(data[realIdx]);
    }
}

// 설정
loopScroll.totalCount = -1;  // 무한
```

#### 페이지네이션 (lazy load)

```csharp
// Presenter에서 스크롤 위치 모니터링으로 다음 페이지 로드
public class PagedInventoryPresenter : IStartable, IDisposable
{
    readonly InventoryModel model;
    readonly InventoryScrollView view;
    readonly ScrollRect scrollRect;
    readonly CompositeDisposable disposables = new();

    const float LoadThreshold = 0.85f;  // 85% 스크롤 시 다음 페이지
    bool isLoading = false;

    public void Start()
    {
        // R3로 스크롤 값 구독
        scrollRect.onValueChanged
            .AsObservable()
            .Where(v => v.y <= (1f - LoadThreshold) && !isLoading)
            .ThrottleFirst(TimeSpan.FromSeconds(0.5f))  // 연속 호출 억제
            .ObserveOnMainThread()
            .SubscribeAwait(async (_, ct) =>
            {
                isLoading = true;
                await model.LoadNextPageAsync(ct);
                view.UpdateList(model.Items.ToArray());
                isLoading = false;
            })
            .AddTo(disposables);
    }

    public void Dispose() => disposables.Dispose();
}
```

#### 스냅 페이징 (FancyScrollView)

```csharp
// FancyScrollView의 스냅 기능으로 카드 스타일 페이징
public class CardScrollView : FancyScrollView<CardData>
{
    [SerializeField] Scroller scroller;

    void Awake()
    {
        scroller.OnValueChanged(UpdatePosition);
        scroller.OnSelectionChanged(SelectCell);
    }

    // 선택된 카드로 스냅
    public void ScrollTo(int index) => scroller.ScrollTo(index, 0.4f, Ease.OutQuart);
}
```

---

## 3. 베스트 프랙티스

### DO (권장)

- [x] 아이템 50개 이상 목록은 반드시 풀링 솔루션 사용
- [x] ScrollRect는 독립 서브 Canvas로 분리하고 `Pixel Perfect = OFF` 설정
- [x] Viewport에 `RectMask2D` 사용 (Mask 컴포넌트 대신)
- [x] 장식용 Text/Image 모두 `raycastTarget = false`
- [x] 셀 데이터 바인딩은 `IScrollCell<TData>` 인터페이스로 추상화
- [x] R3 컬렉션 구독은 `ObserveOnMainThread()` 후 처리 (스레드 안전)
- [x] 컬렉션 변경 구독 dispose는 `CompositeDisposable` + `AddTo(disposables)` 패턴
- [x] 셀 애니메이션은 `DOTween` 사용 (Animator는 UI에 금지)
- [x] 숨김 처리는 `Canvas.enabled = false` 또는 `CanvasGroup.alpha = 0`
- [x] SpriteAtlas로 같은 목록 내 셀 스프라이트를 하나의 아틀라스로 통합
- [x] VContainer에서 셀 프리팹 팩토리는 `IObjectResolver.Instantiate`로 처리

### DON'T (금지)

- [x] 스크롤 목록 셀에 `ContentSizeFitter` 상시 부착 금지
- [x] 대용량 목록에 `VerticalLayoutGroup` 단독 사용 금지 (수동 RectTransform 권장)
- [x] UI에 `Animator` 컴포넌트 사용 금지 (매 프레임 dirty 유발)
- [x] ScrollRect와 외부 UI를 같은 Canvas에 배치 금지
- [x] `GameObject.SetActive(false/true)` 빈번한 토글 금지 (OnEnable 비용)
- [x] 셀 데이터 바인딩에서 `Instantiate` / `Destroy` 호출 금지
- [x] R3 Subscribe를 VContainer의 `Construct()` 내에서 호출 금지 (Awake 전 실행으로 데드락)

### CONSIDER (상황별)

- [x] 가변 높이 셀: LoopScrollRect `SizeHelper` or 누적 오프셋 캐싱 테이블
- [x] 다중 셀 타입: LoopScrollRect `PrefabSource`에서 타입별 분기, 별도 풀 유지
- [x] 1000개 이상 + 자주 변경: ObservableCollections `ISynchronizedView` + 필터/정렬 레이어
- [x] 모바일 성능 우선: LoopScrollRect (LayoutGroup 의존도 최소화 설정)
- [x] 캐러셀/카드 UI: FancyScrollView (이미 확정 스택)
- [x] 무한 스크롤: LoopScrollRect `totalCount = -1`

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 2019.4+ | LoopScrollRect/FancyScrollView 공통 |
| Unity 6 (6000.x) | 지원 | FancyScrollView v1.9.0 (2025-09) 확인 |
| LoopScrollRect | master | OpenUPM: `me.qiankanglai.loopscrollrect` |
| FancyScrollView | v1.9.0 | OpenUPM or Git UPM |
| ObservableCollections | 3.x | `ObservableCollections.R3` 별도 패키지 |
| R3 | 1.x | ObservableCollections.R3과 버전 일치 필요 |
| VContainer | 1.x | LifetimeScope + RegisterEntryPoint |
| DOTween | 1.x | UI 애니메이션 대안 |

---

## 5. 예제 코드 — UI_Study 적용 가이드

### 기본: LoopScrollRect + MVP + VContainer 전체 구조

```csharp
// === LifetimeScope (씬에 배치) ===
public class ItemListLifetimeScope : LifetimeScope
{
    [SerializeField] ItemListScrollView scrollView;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ItemListModel>(Lifetime.Scoped);
        builder.RegisterComponent(scrollView);
        builder.RegisterEntryPoint<ItemListPresenter>();
    }
}

// === Model ===
public class ItemListModel
{
    public readonly ObservableList<ItemData> Items = new();
    // 아이템 추가/제거/갱신 메서드...
}

// === View ===
public class ItemListScrollView : MonoBehaviour, LoopScrollDataSource
{
    [SerializeField] LoopVerticalScrollRect loopScroll;
    ItemData[] snapshot = Array.Empty<ItemData>();

    void Start()
    {
        loopScroll.dataSource = this;  // DataSource 연결
    }

    // LoopScrollDataSource 구현
    public void ProvideData(Transform t, int idx)
    {
        t.GetComponent<ItemCellView>()?.Bind(snapshot[idx], idx);
    }

    public void Reload(ItemData[] newData)
    {
        snapshot = newData;
        loopScroll.totalCount = snapshot.Length;
        loopScroll.RefillCells();
    }
}

// === Cell View ===
public class ItemCellView : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text countText;
    [SerializeField] Button selectButton;

    public Subject<int> OnSelected { get; } = new();
    int index;

    void Awake()
    {
        selectButton.OnClickAsObservable()
            .Subscribe(_ => OnSelected.OnNext(index))
            .AddTo(this);
    }

    public void Bind(ItemData data, int idx)
    {
        index = idx;
        nameText.text = data.Name;
        countText.text = $"x{data.Count}";
    }
}

// === Presenter ===
public class ItemListPresenter : IStartable, IDisposable
{
    readonly ItemListModel model;
    readonly ItemListScrollView view;
    readonly CompositeDisposable disposables = new();

    public ItemListPresenter(ItemListModel model, ItemListScrollView view)
    {
        this.model = model;
        this.view  = view;
    }

    public void Start()
    {
        // 컬렉션 변경 시 뷰 갱신
        model.Items
            .ObserveChanged()
            .ObserveOnMainThread()
            .Subscribe(_ => view.Reload(model.Items.ToArray()))
            .AddTo(disposables);

        // 초기 로드
        view.Reload(model.Items.ToArray());
    }

    public void Dispose() => disposables.Dispose();
}
```

### 고급: 필터/정렬 적용 ISynchronizedView 패턴

```csharp
public class FilteredItemListPresenter : IStartable, IDisposable
{
    readonly ItemListModel model;
    readonly ItemListScrollView view;
    readonly CompositeDisposable disposables = new();

    // 필터 상태
    readonly ReactiveProperty<string> searchQuery = new("");
    ISynchronizedView<ItemData, ItemData> syncView;

    public FilteredItemListPresenter(ItemListModel model, ItemListScrollView view)
    {
        this.model = model;
        this.view  = view;
    }

    public void Start()
    {
        // SynchronizedView 생성
        syncView = model.Items.CreateView(item => item);

        // 검색어 변경 시 필터 갱신
        searchQuery
            .Subscribe(query =>
            {
                if (string.IsNullOrEmpty(query))
                    syncView.ResetFilter();
                else
                    syncView.AttachFilter(item =>
                        item.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

                view.Reload(syncView.ToArray());
            })
            .AddTo(disposables);

        // 컬렉션 변경 구독
        model.Items
            .ObserveChanged()
            .ObserveOnMainThread()
            .Subscribe(_ => view.Reload(syncView.ToArray()))
            .AddTo(disposables);

        view.Reload(syncView.ToArray());
    }

    // 외부에서 검색어 설정
    public void SetSearchQuery(string query) => searchQuery.Value = query;

    public void Dispose()
    {
        syncView?.Dispose();
        disposables.Dispose();
    }
}
```

---

## 6. UI_Study 적용 계획

### 제안 예제 씬 구성

| 씬 번호 | 주제 | 핵심 기법 |
|---|---|---|
| 01 | 기본 풀링 ScrollView | 수동 풀링 구현 (라이브러리 없이) |
| 02 | LoopScrollRect 기본 | PrefabSource + DataSource 구현 |
| 03 | LoopScrollRect + MVP + R3 | ObservableList → 뷰 갱신 전체 파이프라인 |
| 04 | FancyScrollView 캐러셀 | UpdatePosition 애니메이션 커스텀 |
| 05 | 가변 높이 셀 | SizeHelper 또는 수동 누적 오프셋 |
| 06 | 필터/정렬 ISynchronizedView | 검색 + ObservableCollections.R3 |
| 07 | 무한 스크롤 | LoopScrollRect totalCount=-1 |
| 08 | 페이지네이션 | 스크롤 위치 감지 + UniTask async load |

이미 확정된 기술 스택에 **FancyScrollView**가 포함되어 있으므로 씬 04는 우선순위 높음. LoopScrollRect는 FancyScrollView의 "목록형 대안"으로 사이드바이사이드 비교 예제를 만들면 학습 효과가 크다.

---

## 7. 참고 자료

1. [LoopScrollRect — GitHub (qiankanglai)](https://github.com/qiankanglai/LoopScrollRect)
2. [FancyScrollView — GitHub (setchi)](https://github.com/setchi/FancyScrollView)
3. [FancyScrollView Releases](https://github.com/setchi/FancyScrollView/releases)
4. [EnhancedScroller — Unity Asset Store](https://assetstore.unity.com/packages/tools/gui/enhancedscroller-36378)
5. [ObservableCollections — GitHub (Cysharp)](https://github.com/Cysharp/ObservableCollections)
6. [R3 ReactiveCollection Issue #63 — ObservableCollections.R3 로 대체](https://github.com/Cysharp/R3/issues/63)
7. [UnityRecyclingListView — GitHub (sinbad)](https://github.com/sinbad/UnityRecyclingListView)
8. [VirtualList — GitHub (disruptorbeaminc)](https://github.com/disruptorbeaminc/VirtualList)
9. [UnityDynamicScrollRect — GitHub (Mukarillo)](https://github.com/Mukarillo/UnityDynamicScrollRect)
10. [Optimizing Unity UI — Unity Learn](https://learn.unity.com/tutorial/optimizing-unity-ui)
11. [Unity UI Optimization Tips — Unity Official](https://unity.com/how-to/unity-ui-optimization-tips)
12. [Best Optimization Tips by Unity Engineers (Unite)](https://gamedev.center/best-optimization-tips-by-unity-engineers-at-unite/)
13. [Optimizing Unity UI — Tantzy Games](https://www.tantzygames.com/blog/optimizing-unity-ui/)
14. [Optimizing UI Performance: LayoutElement and LayoutGroup — Medium](https://llmagicll.medium.com/optimizing-ui-performance-in-unity-deep-dive-into-layoutelement-and-layoutgroup-components-b6a575187ee4)
15. [UnityUIOptimizationTool — GitHub (JoanStinson)](https://github.com/JoanStinson/UnityUIOptimizationTool)
16. [RectMask2D Manual — Unity Docs](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-RectMask2D.html)
17. [recyclable-scroll-view — GitHub (alfredo1995)](https://github.com/alfredo1995/recyclable-scroll-view)

---

## 8. 미해결 질문

- [ ] LoopScrollRect와 VContainer `IObjectResolver.Instantiate` 통합 시 PrefabSource 내부 DI 주입 방법 (셀이 Presenter를 주입받아야 할 때)
- [ ] FancyScrollView에서 가변 높이 셀 처리 공식 방법 — v1.9.0에서 지원 여부 확인 필요
- [ ] ObservableCollections `ISynchronizedView`의 `SyncRoot` lock이 Unity 메인 스레드에서 성능 병목이 되는지 실측 필요
- [ ] Unity 6 UI Toolkit `ListView` 내장 가상화 vs LoopScrollRect 성능 비교 벤치마크
- [ ] LoopScrollRect에서 `RefreshCells()` vs `RefillCells()` 선택 기준 — 애니메이션 없이 데이터만 변경 시 어느 것이 더 저비용인지
