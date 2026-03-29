# 09 - Drag And Drop

Unity UGUI Drag & Drop 학습 모듈.
MV(R)P + VContainer + R3 + DOTween 아키텍처 적용.

## Scenes

### 01-BasicDragDrop
기본 드래그앤드롭 — 3개 아이템(Sword, Shield, Potion)을 2개 드롭 존(Equip, Discard)에 배치.

- `DraggableItemView`: IBeginDragHandler/IDragHandler/IEndDragHandler 구현. 드래그 시 Canvas 루트로 리페어런팅.
- `DropZoneView`: IDropHandler + 호버 하이라이트. 유효한 드래그 아이템 수신 시 이벤트 발행.
- `BasicDragDropDemoView`: 전체 데모 뷰. 상태 텍스트로 마지막 액션 표시.
- `BasicDragDropPresenter`: 드롭 이벤트 처리 + 상태 갱신.

### 02-SortableList
정렬 가능 리스트 — 6개 아이템을 드래그로 순서 변경.

- `SortableItemView`: 리스트 아이템. 드래그 핸들 + 라벨 + 교대 배경색.
- `SortableListView`: VerticalLayoutGroup 컨테이너. 삽입 인디케이터 라인 표시. 포인터 Y에서 삽입 인덱스 계산.
- `SortableListModel`: ReactiveProperty<string[]> 기반 순서 관리.
- `SortableListPresenter`: 드래그 리오더 로직.

### 03-GridSlotSwap
그리드 슬롯 스왑 — 3x3 그리드에서 아이템 드래그로 위치 교환.

- `SwapSlotView`: 드래그 소스 + 드롭 타겟 겸용. 고스트 오브젝트로 드래그 비주얼 표현.
- `GridSwapView`: GridLayoutGroup(3열) 기반 9슬롯 그리드.
- `GridSwapModel`: ReactiveProperty<string[]> 9개 슬롯 관리. A~E 5개 아이템 + 4개 빈 슬롯.
- `GridSwapPresenter`: 슬롯 간 스왑 로직.

## Key Patterns

1. **Canvas 리페어런팅**: 드래그 시 루트 Canvas로 이동하여 최상위 렌더링 보장.
2. **CanvasGroup.blocksRaycasts**: 드래그 중 false로 설정하여 드롭 존이 이벤트를 수신할 수 있도록 함.
3. **EventData.position**: UnityEngine.Input 대신 이벤트 핸들러의 eventData 사용 (New Input System 호환).
4. **DOTween 스냅백**: 유효하지 않은 드롭 시 원래 위치로 부드럽게 복귀.
5. **R3 Subject**: 드래그/드롭 이벤트를 Presenter에 전달하는 리액티브 스트림.

## Scene Setup Guide

각 씬에서 다음 구조로 설정:

```
Canvas (Screen Space - Overlay)
  +-- [LifetimeScope]
  +-- EventSystem (with InputSystemUIInputModule)
  +-- ... UI elements with View components ...
```

- EventSystem에는 `InputSystemUIInputModule` 사용 (New Input System).
- 각 드래그 아이템에는 반드시 `CanvasGroup` 컴포넌트 부착.
- LifetimeScope에서 View 참조를 SerializeField로 연결.
