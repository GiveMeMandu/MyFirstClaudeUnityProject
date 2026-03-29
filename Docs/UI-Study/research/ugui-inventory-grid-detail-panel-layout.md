# UGUI 인벤토리 그리드 + 디테일 패널 레이아웃 리서치

- **작성일**: 2026-03-28
- **카테고리**: practice
- **상태**: 조사완료

---

## 1. 요약

게임 인벤토리 UI에서 그리드 패널은 **고정 너비**로 설정하고 디테일 패널은 **유연(flexible) 너비**로 확장하는 것이 표준 패턴이다. 디테일 패널은 항상 표시 상태를 유지하고 "아이템을 선택하세요" 빈 상태를 보여주는 방식이 레이아웃 점프 방지와 UX 모두에서 우월하다. 4열 그리드(cellSize=80, spacing=6, padding=10)의 올바른 고정 너비는 **386px**이며, LayoutElement에서는 `minWidth` + `preferredWidth`를 동일 값으로 설정하고 `flexibleWidth = 0`으로 고정하는 것이 핵심이다.

---

## 2. 상세 분석

### 2.1 그리드 너비: 고정 vs 유연

**결론: 그리드는 고정 너비, 디테일 패널은 유연 너비**

| 방식 | 장점 | 단점 |
|---|---|---|
| 그리드 고정 + 디테일 유연 | 레이아웃 안정, 그리드 열수 보장, 디테일 공간 최대화 | 디테일이 좁은 화면에서 찌그러질 수 있음 |
| 둘 다 고정 | 완전 예측 가능한 레이아웃 | 화면 크기 변화 대응 불가 |
| 둘 다 유연 | 반응형 | 열수 보장 어려움, 레이아웃 점프 발생 |

**이유:** GridLayoutGroup은 `cellSize`가 고정값이기 때문에 열 수를 보장하려면 부모 너비가 일정해야 한다. 그리드 너비가 변하면 열 수가 바뀌어 전체 레이아웃이 재계산된다.

---

### 2.2 레이아웃 점프 방지

디테일 패널을 `SetActive(false)`로 숨기면 HorizontalLayoutGroup이 즉시 재계산되어 그리드가 오른쪽으로 확장되는 **레이아웃 점프**가 발생한다.

**해결책 3가지:**

#### A. 항상 표시 (권장)
디테일 패널을 절대 숨기지 않고 빈 상태(Empty State)를 표시한다.
```
[선택된 아이템 없음]
아이템을 선택하면
상세 정보가 표시됩니다.
```
- 레이아웃 점프 완전 제거
- UX 측면에서도 패널 위치가 항상 예측 가능
- "Designer Agnostic" 패턴: 내용만 교체, 구조는 불변

#### B. CanvasGroup.alpha + CanvasGroup.blocksRaycasts
`SetActive` 대신 alpha=0 + blocksRaycasts=false 조합 사용.
- 레이아웃 공간은 유지하면서 시각적으로 숨김
- 점프 없음, 단 공간을 항상 차지함

#### C. LayoutElement.ignoreLayout 토글
패널을 숨길 때 `ignoreLayout = true`로 설정 → 레이아웃에서 제외.
단, 이 경우 그리드가 확장되므로 그리드의 `LayoutElement.preferredWidth`를 별도로 고정해야 한다.

**권장: A안 (항상 표시)**

---

### 2.3 4열 그리드 고정 너비 계산

GridLayoutGroup의 내부 계산식 (Unity 소스코드 기반):

```
cellCountX = Floor((width - padding.horizontal + spacing.x + 0.001) / (cellSize.x + spacing.x))
```

역산으로 N열을 딱 담는 최소 너비:

```
fixedWidth = padding.left + padding.right + (cellSize.x * N) + (spacing.x * (N - 1))
```

**cellSize=80, spacing=6, padding=10 (left+right=20), N=4 계산:**

```
fixedWidth = 20 + (80 * 4) + (6 * 3)
           = 20 + 320 + 18
           = 358px
```

단, ContentSizeFitter와 함께 스크롤뷰를 사용하거나 미래에 스크롤바(기본 15-17px)가 추가될 경우를 대비해 약간의 여유를 주는 것이 실무적:

```
fixedWidth = 358 + 여유(8~12) = 366~370px
```

**ScrollRect 내부에서 사용할 경우:** Viewport 너비를 fixedWidth와 일치시킬 것.

**중요:** GridLayoutGroup은 `Constraint = Fixed Column Count`, `Constraint Count = 4`로 설정해야 열 수가 보장된다. 이 설정 없이는 너비 계산만으로 열 수가 보장되지 않는다.

---

### 2.4 LayoutElement 프로퍼티 선택 기준

Unity 레이아웃 시스템의 3단계 할당 순서:
1. **Min** → 최소 보장 (줄어들지 않음)
2. **Preferred** → 공간이 있으면 이 크기까지 확장
3. **Flexible** → 남은 공간을 가중치 비율로 나눔

| 프로퍼티 | 사용 시나리오 |
|---|---|
| `minWidth` | 패널이 절대 이 크기 이하로 줄지 않아야 할 때 |
| `preferredWidth` | 이상적인 크기 지정. min보다 크게 설정 가능 |
| `flexibleWidth` | 남은 공간을 채울 때. 0이면 확장 안 함 |

#### 그리드 패널 (고정 너비) 설정:
```
LayoutElement:
  minWidth = 358          // 이 이하로 줄지 않음
  preferredWidth = 358    // 이 크기를 목표로 함
  flexibleWidth = 0       // 남은 공간 차지하지 않음
```

#### 디테일 패널 (유연 너비) 설정:
```
LayoutElement:
  minWidth = 200          // 최소 너비 보장
  preferredWidth = 300    // 기본 목표 너비
  flexibleWidth = 1       // 남은 공간 모두 차지
```

#### HorizontalLayoutGroup 설정:
```
Child Control Size Width: true
Child Force Expand Width: false   // 중요! true이면 flexibleWidth=0이어도 강제 확장됨
```

**주의:** `Child Force Expand Width = true`이면 모든 자식의 `flexibleWidth`가 0이어도 강제로 공간을 채운다. 고정/유연 혼합 레이아웃에서는 반드시 `false`로 설정 후 개별 `LayoutElement.flexibleWidth`로 제어해야 한다.

---

### 2.5 디테일 패널: 항상 표시 vs 조건부 표시

**결론: 항상 표시 권장**

| 방식 | 레이아웃 안정성 | UX | 구현 복잡도 |
|---|---|---|---|
| 항상 표시 (Empty State) | 완전 안정 | 사용자가 패널 위치 예측 가능 | 낮음 |
| SetActive Toggle | 점프 발생 | 공간 낭비 없음 (빈 상태) | 낮음 |
| CanvasGroup Toggle | 안정 | 공간 항상 차지함 | 낮음 |
| 애니메이션 슬라이드 인/아웃 | 안정 (애니메이션 중) | 동적이고 세련됨 | 높음 |

**대다수 상용 RPG (Diablo, Path of Exile 등)가 "항상 표시" 방식을 채택한다.** 이유:
1. 레이아웃 점프 없음
2. 빈 상태 텍스트가 유저에게 "여기에 정보가 표시된다"는 힌트 제공
3. R3 ReactiveProperty 바인딩 시 null 처리가 단순해짐

---

## 3. 베스트 프랙티스

### DO (권장)
- [x] GridLayoutGroup에 `Constraint = Fixed Column Count` 설정
- [x] 그리드 부모에 `LayoutElement` 추가, `minWidth = preferredWidth = 고정값`, `flexibleWidth = 0`
- [x] 디테일 패널에 `LayoutElement.flexibleWidth = 1` 설정
- [x] `HorizontalLayoutGroup.childForceExpandWidth = false`
- [x] 디테일 패널을 항상 표시 상태 유지, 빈 상태(Empty State) UI 구성
- [x] 스크롤뷰 사용 시 Viewport 너비 = 그리드 fixedWidth와 일치

### DON'T (금지)
- [x] `SetActive`로 디테일 패널 토글 (레이아웃 점프 원인)
- [x] `HorizontalLayoutGroup.childForceExpandWidth = true` 상태에서 개별 너비 제어 시도
- [x] GridLayoutGroup + ContentSizeFitter 동시 사용 (충돌 발생, Unity 공식 문서에서 경고)
- [x] GridLayoutGroup의 `Constraint = Flexible` 상태로 열 수 보장 시도

### CONSIDER (상황별)
- [x] 디테일 패널 숨김이 꼭 필요하다면 DOTween 슬라이드 아웃 + 그리드 너비 고정 조합
- [x] 매우 작은 화면에서는 그리드/디테일을 탭으로 전환하는 방식 고려
- [x] 디테일 패널 최소 너비(`minWidth`)를 콘텐츠가 깨지지 않는 값으로 설정

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 6000.x | GridLayoutGroup.Constraint enum 확인 |
| com.unity.ugui | 2.0.0 | LayoutElement API 동일 |
| LayoutGroup 버그 | 모든 버전 | ContentSizeFitter+GridLayoutGroup 충돌은 알려진 이슈 |

---

## 5. 예제 코드

### 계층 구조

```
InventoryPanel (HorizontalLayoutGroup)
├── GridSection (LayoutElement: minWidth=358, preferredWidth=358, flexibleWidth=0)
│   └── ScrollRect
│       └── Viewport
│           └── GridContent (GridLayoutGroup: cellSize=80, spacing=6, padding=10, Constraint=Fixed Column Count 4)
│               ├── ItemSlot_00
│               ├── ItemSlot_01
│               └── ...
└── DetailSection (LayoutElement: minWidth=200, preferredWidth=300, flexibleWidth=1)
    ├── EmptyStateView (항상 존재, 아이템 미선택 시 표시)
    └── ItemDetailView (아이템 선택 시 내용 채움)
```

### 고정 너비 계산 유틸리티

```csharp
public static float CalculateGridFixedWidth(
    float cellSizeX, float spacingX,
    int columns, float paddingLeft, float paddingRight)
{
    return paddingLeft + paddingRight
         + (cellSizeX * columns)
         + (spacingX * (columns - 1));
}

// 사용 예: cellSize=80, spacing=6, padding=10, 4열
// CalculateGridFixedWidth(80, 6, 4, 10, 10) = 358
```

### LayoutElement 코드 설정 (VContainer Presenter에서)

```csharp
[SerializeField] LayoutElement _gridLayoutElement;
[SerializeField] LayoutElement _detailLayoutElement;
[SerializeField] HorizontalLayoutGroup _rootHLG;

void SetupLayout()
{
    // HorizontalLayoutGroup 설정
    _rootHLG.childForceExpandWidth = false;
    _rootHLG.childControlWidth = true;

    // 그리드: 고정 너비
    float gridWidth = CalculateGridFixedWidth(80, 6, 4, 10, 10); // 358
    _gridLayoutElement.minWidth = gridWidth;
    _gridLayoutElement.preferredWidth = gridWidth;
    _gridLayoutElement.flexibleWidth = 0;

    // 디테일: 유연 너비
    _detailLayoutElement.minWidth = 200;
    _detailLayoutElement.preferredWidth = 300;
    _detailLayoutElement.flexibleWidth = 1;
}
```

### 디테일 패널 Empty State 패턴 (R3 바인딩)

```csharp
// Presenter
_model.SelectedItem  // ReactiveProperty<ItemData?>
    .Subscribe(item =>
    {
        bool hasItem = item != null;
        _emptyStateView.SetActive(!hasItem);
        _itemDetailView.SetActive(hasItem);
        if (hasItem) _itemDetailView.Render(item);
    })
    .AddTo(_disposables);
```

**주의:** 위 패턴에서 `_itemDetailView`는 내용만 바꾸는 것이고, 패널 컨테이너 자체(`DetailSection`)는 `SetActive` 하지 않는다.

---

## 6. UI_Study 적용 계획

이 리서치를 바탕으로 다음 예제를 구현할 수 있다:

- **예제 06-01**: 인벤토리 그리드 UI — 고정 너비 그리드 + 유연 디테일 패널
  - GridLayoutGroup Fixed Column Count 4열
  - HorizontalLayoutGroup 고정/유연 혼합 레이아웃
  - Empty State / Item Detail 상태 전환 (SetActive가 아닌 내용 교체 방식)
  - R3 ReactiveProperty<ItemData?> 바인딩
  - VContainer LifetimeScope 구성

---

## 7. 참고 자료

1. [Unity UGUI Grid Layout Group Docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-GridLayoutGroup.html)
2. [Unity UGUI Layout Element Docs](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-LayoutElement.html)
3. [Unity UGUI Horizontal Layout Group Docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-HorizontalLayoutGroup.html)
4. [Unity UI Layout Groups Explained — Hallgrim Games](https://www.hallgrimgames.com/blog/2018/10/16/unity-layout-groups-explained)
5. [LayoutElement flexibleWidth=0 still stretches — Unity Discussions](https://discussions.unity.com/t/layoutelement-with-flexiblewidth-0-still-stretches-in-a-horizontallayoutgroup/940992)
6. [Unity4.6 GridLayoutGroup Source Code — GitHub](https://github.com/spawn66336/Unity4.6.0f3UI/blob/master/UnityEngine.UI/UI/Core/Layout/GridLayoutGroup.cs)
7. [10 Ways to Improve Your Inventory Screen — The Wingless](https://thewingless.com/index.php/2021/07/26/10-simple-ways-you-can-improve-your-videogame-inventory-screen-game-ui-ux-design-course/)
8. [Best Practices for Inventory UI — Unity Discussions](https://discussions.unity.com/t/best-practices-for-holding-inventory-ui-gameobjects-etc/915335)

---

## 8. 미해결 질문

- [ ] ScrollRect 내부에서 그리드 fixedWidth와 Viewport 너비를 코드로 동기화하는 올바른 타이밍 (Awake vs Start vs OnEnable)
- [ ] 4열 → 6열 동적 전환이 필요한 경우 너비 재계산 트리거 방법
- [ ] LayoutElement.minWidth와 RectTransform.sizeDelta.x를 직접 설정하는 방식 중 어느 쪽이 우선순위가 높은가
