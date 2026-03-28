# 08-Game-UI-Patterns

MV(R)P + VContainer + R3 + UniTask + DOTween 기반 게임 UI 패턴 학습 모듈.

## Scenes

### 01-DamageNumbers
플로팅 데미지 넘버 — 오브젝트 풀 + DOTween 애니메이션.
- **Normal**: 흰색 텍스트, 위로 떠오르며 페이드아웃
- **Critical**: 큰 노란색 텍스트, 펀치 스케일 + 떠오르기
- **Heal**: 초록색 텍스트, 위로 떠오르며 페이드아웃
- 핵심: `List<T>` 기반 단순 오브젝트 풀, `DamageNumberService`

### 02-DialogTypewriter
대화 타이프라이터 — TMP maxVisibleCharacters + UniTask.
- `DialogView.TypeText()`: 한 글자씩 표시 (maxVisibleCharacters 패턴)
- 스킵 버튼: CancellationToken으로 즉시 전체 표시
- 계속 화살표: DOFade 루프 깜빡임
- `DialogService`: 대화 큐 관리, UniTaskCompletionSource await 패턴

### 03-InventoryGrid
4x4 인벤토리 그리드 — ReactiveProperty + 슬롯 선택.
- `InventoryModel`: ReactiveProperty<InventoryItem[]> (16슬롯)
- 슬롯 클릭 -> 디테일 패널 표시
- 레어리티별 배경 색상 (Common/Rare/Epic)
- R3 ReactiveProperty로 선택 인덱스 관리

## Tech Stack
| Library | Usage |
|---------|-------|
| VContainer | DI + LifetimeScope |
| R3 | Button.OnClickAsObservable, ReactiveProperty |
| UniTask | Dialog typewriter async, CancellationToken skip |
| DOTween | Damage float/fade, critical punch, arrow blink |

## File Structure
```
Scripts/
  Models/       DamageModel, DialogLine, InventoryItem+InventoryModel
  Views/        DamageNumberView, DamageNumberDemoView, DialogView,
                DialogDemoView, InventorySlotView, InventoryGridView
  Presenters/   DamageNumberDemoPresenter, DialogDemoPresenter, InventoryPresenter
  Services/     DamageNumberService (object pool), DialogService (dialog queue)
  LifetimeScopes/ DamageNumberLifetimeScope, DialogTypewriterLifetimeScope,
                  InventoryGridLifetimeScope
```

## Scene Setup Notes

### 01-DamageNumbers
1. Canvas (Screen Space - Overlay) 생성
2. DamageNumberLifetimeScope 컴포넌트 추가
3. DamageNumberDemoView: 버튼 3개 + 타겟 Image + NumberContainer (빈 RectTransform)
4. DamageNumberView 프리팹: TMP + CanvasGroup, 비활성 상태로 시작
5. LifetimeScope에 프리팹과 컨테이너 연결

### 02-DialogTypewriter
1. Canvas 생성
2. DialogTypewriterLifetimeScope 컴포넌트 추가
3. DialogDemoView: Start 버튼 + 상태 텍스트
4. DialogView: Speaker TMP + Body TMP + CanvasGroup(화살표) + Skip 버튼 + DialogPanel CanvasGroup
5. LifetimeScope에 두 View 연결

### 03-InventoryGrid
1. Canvas 생성
2. InventoryGridLifetimeScope 컴포넌트 추가
3. InventoryGridView: GridLayoutGroup(4x4) 안에 InventorySlotView 16개
4. 각 SlotView: Background Image + Icon TMP + Count TMP + SelectionHighlight GO + Button
5. Detail Panel: Name TMP + Rarity TMP + Description TMP + Icon TMP
6. Gold TMP
7. LifetimeScope에 GridView 연결
