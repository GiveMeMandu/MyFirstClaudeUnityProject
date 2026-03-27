# UI 기술 스택 결정 종합 문서

- **작성일**: 2026-03-27
- **카테고리**: integration
- **상태**: 확정

---

## 1. 요약

UI_Study 프로젝트의 기술 스택이 12개 영역에서 확정되었다. UGUI + MV(R)P + VContainer + R3 + UniTask를 핵심 축으로 하며, UnityScreenNavigator로 화면 전환, DOTween으로 애니메이션, Addressables로 에셋 관리를 담당한다.

## 2. 확정된 기술 스택

| # | 영역 | 결정 | 설치 방식 | 비고 |
|---|---|---|---|---|
| 1 | 아키텍처 패턴 | MV(R)P | - | Reactive Presenter, Pure C# |
| 2 | 리액티브 | R3 v1.3.0+ | Git URL | UniRx 공식 후속 (Cysharp) |
| 3 | 비동기 | UniTask v2.5.10+ | Git URL | Cysharp, VContainer 자동 통합 |
| 4 | DI | VContainer v1.17.0+ | Git URL | LifetimeScope 계층 구조 |
| 5 | 화면 네비게이션 | UnityScreenNavigator | Git URL | Page/Modal/Sheet 분류 |
| 6 | UI 애니메이션 | DOTween | Asset Store | Animator보다 UI에 적합 |
| 7 | 스크롤뷰 | 기본 ScrollRect + FancyScrollView | Git URL (보조) | OSA 미도입 |
| 8 | 로컬라이제이션 | Unity Localization | Unity Registry | 공식 패키지 |
| 9 | 테마/스타일링 | uPalette | Git URL | + ScriptableObject 보조 |
| 10 | 팝업/다이얼로그 | USN ModalContainer + 커스텀 토스트 | - | |
| 11 | 에셋 관리 | Addressables + SpriteAtlas V2 | Unity Registry | 처음부터 적용 |
| 12 | 입력 | New Input System + 게임패드/키보드 | Unity Registry | 이미 설치됨 |
| 13 | 접근성 | 폰트 스케일링 + 색약 모드 | - | uPalette 팔레트 활용 |

## 3. 아키텍처 패턴: MV(R)P

### 레이어 규칙

| 레이어 | 클래스 타입 | 역할 | 허용 | 금지 |
|---|---|---|---|---|
| View | MonoBehaviour | UI 요소 참조, 이벤트 노출 | SerializeField, Observable 노출, 단순 표시 메서드 | 비즈니스 로직, Model 직접 참조 |
| Presenter | Pure C# (IInitializable, IDisposable) | View↔Model 연결 | R3 Subscribe, 상태 변환, UniTask async | MonoBehaviour 상속, Update() |
| Model | Plain C# | 게임 상태 보유 | ReactiveProperty, 순수 로직 | UI 참조, UnityEngine 의존 |

### 핵심 코드 패턴

```csharp
// === View (MonoBehaviour) ===
public class ResourceBarView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _goldText;
    [SerializeField] Button _buildButton;

    public Observable<Unit> OnBuildClick => _buildButton.OnClickAsObservable();
    public void SetGoldText(int gold) => _goldText.text = $"{gold:N0}";
}

// === Presenter (Pure C#) ===
public class ResourceBarPresenter : IInitializable, IDisposable
{
    readonly ResourceSystem _model;
    readonly ResourceBarView _view;
    readonly CompositeDisposable _disposables = new();

    public ResourceBarPresenter(ResourceSystem model, ResourceBarView view)
    {
        _model = model;
        _view = view;
    }

    public void Initialize()
    {
        _model.Gold.Subscribe(_view.SetGoldText).AddTo(_disposables);
        _view.OnBuildClick.Subscribe(_ => _model.Build()).AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}

// === Model (Plain C#) ===
public class ResourceSystem : IDisposable
{
    public ReactiveProperty<int> Gold { get; } = new(0);
    public void Build() { Gold.Value -= 100; }
    public void Dispose() => Gold.Dispose();
}

// === LifetimeScope ===
public class GameSceneLifetimeScope : LifetimeScope
{
    [SerializeField] ResourceBarView _resourceBarView;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_resourceBarView);
        builder.RegisterEntryPoint<ResourceBarPresenter>();
        builder.Register<ResourceSystem>(Lifetime.Singleton);
    }
}
```

## 4. VContainer LifetimeScope 계층

```
RootLifetimeScope (DontDestroyOnLoad)
  └─ 전역: AudioService, LocalizationService, UIStateModel

GameSceneLifetimeScope
  └─ 씬: ResourceSystem, BuildingRegistry, TurnSystem

  HUDLifetimeScope (자식)
    └─ HUD Presenter들

  PopupLifetimeScope (동적 생성/소멸)
    └─ 팝업별 Presenter + View
```

## 5. 핵심 규칙

1. **Construct()에서 R3 Subscribe() 금지** — Awake 전 실행, 데드락 위험
2. **Subscribe는 IInitializable.Initialize() 또는 IStartable.Start()에서만**
3. **View에 로직 없음** — Observable 노출 + 표시 메서드만
4. **Presenter는 Pure C#** — MonoBehaviour 상속 금지
5. **async void 금지** — async UniTaskVoid 또는 UniTask.UnityAction() 사용
6. **SubscribeAwait 사용 시 configureAwait: false 필수**
7. **패키지 설치는 Git URL 우선** (Asset Store 전용 제외)

## 6. Canvas 레이어 분류

| 레이어 | Sort Order | 용도 |
|---|---|---|
| World | 0 | 월드 스페이스 UI (체력바, 건물 위 표시) |
| HUD | 10 | 항상 표시 (자원바, 미니맵) |
| Screens | 20 | Page/Sheet 콘텐츠 |
| Modals | 30 | 모달 다이얼로그 |
| Toast | 35 | 토스트 알림 |
| Overlay | 40 | 로딩, 페이드 |
| Debug | 100 | 디버그 오버레이 |

## 7. 참고 자료

- [VContainer](https://github.com/hadashiA/VContainer)
- [R3](https://github.com/Cysharp/R3)
- [UniTask](https://github.com/Cysharp/UniTask)
- [UnityScreenNavigator](https://github.com/Haruma-K/UnityScreenNavigator)
- [uPalette](https://github.com/Haruma-K/uPalette)
- [FancyScrollView](https://github.com/setchi/FancyScrollView)
- [DOTween](https://dotween.demigiant.com/)
