# 06 - R3 Advanced Binding

R3의 고급 UI 데이터 바인딩 패턴을 학습하는 모듈.

## Scenes

### 01-ReactiveCommand
ReactiveCommand 패턴 데모. CombineLatest로 `canBuy` 조건을 파생하고 버튼 interactable에 바인딩한다.
SubscribeAwait + AwaitOperation.Drop으로 더블클릭을 방지한다.

- **Model**: PurchaseModel (Gold, ItemPrice)
- **View**: PurchaseView (Buy 버튼, +10 Gold 버튼, 상태 텍스트)
- **Presenter**: PurchasePresenter

### 02-DebounceSearch
Debounce 패턴 데모. InputField 입력을 300ms 디바운싱하여 검색을 실행한다.
입력 중 "Searching..." 상태를 표시하고, 디바운스 후 필터 결과를 갱신한다.

- **Model**: SearchModel (AllItems 20개, SearchQuery)
- **View**: SearchView (검색 InputField, 결과 카운트, 8개 결과 슬롯)
- **Presenter**: SearchPresenter

### 03-TwoWayBinding
Two-Way Binding 패턴 데모. InputField/Slider와 Model 사이의 양방향 바인딩을 구현한다.
SetWithoutNotify + Skip(1) + DistinctUntilChanged로 피드백 루프를 방지한다.
CombineLatest로 세 프로퍼티를 결합하여 미리보기 텍스트를 파생한다.

- **Model**: CharacterModel (Name, Health, Attack)
- **View**: CharacterView (이름 InputField, HP/ATK Slider + 라벨, 미리보기)
- **Presenter**: CharacterPresenter

## Tech Stack
- MV(R)P + VContainer + R3 + UniTask + DOTween
- New Input System (UnityEngine.Input 사용 금지)
