# 고급 UI 패턴 학습 예제

## 개요

라이브러리 공식 예제와 실전 게임 UI 패턴을 구현. VContainer/R3/UniTask/USN/DOTween의 고급 기능을 프로덕션 수준으로 활용.

## 구조

```
Scripts/
├── LifetimeScopes/
│   └── RootLifetimeScope.cs          — Step 1: VContainerSettings 연동 글로벌 스코프
├── Services/
│   ├── UIFactory.cs                   — Step 1: IObjectResolver.Instantiate 래핑
│   ├── AsyncBootstrapper.cs           — Step 1: IAsyncStartable 비동기 초기화
│   ├── GameLoopService.cs             — Step 1: ITickable Pure C# Update 루프
│   ├── LeakDetector.cs                — Step 2: ObservableTracker 구독 누수 디버깅
│   ├── PreloadingNavigationService.cs — Step 3: WillPushEnter 프리로드
│   ├── AddressableAssetLoaderSetup.cs — Step 3: IAssetLoader Addressable 가이드
│   ├── TimedDialogService.cs          — Step 4: WhenAny + CancelAfterSlim 타임아웃
│   ├── TurnEventBroker.cs             — Step 4: Channel<T> pub/sub 이벤트 버스
│   ├── ToastService.cs                — Step 7: 큐 기반 순차 알림
│   └── TooltipService.cs              — Step 8: 싱글톤 툴팁 컨트롤러
├── Models/
│   ├── BuildActionModel.cs            — Step 2: ReactiveCommand + CombineLatest CanBuild
│   ├── InspectorModel.cs              — Step 2: SerializableReactiveProperty Inspector
│   ├── StatModel.cs                   — Step 6: 범용 HP/경험치 모델
│   └── ToastData.cs                   — Step 7: 토스트 데이터 구조체
├── Presenters/
│   ├── CombinedStatePresenter.cs      — Step 2: 다중 조건 파생 상태 + View
│   ├── AsyncBindingPresenter.cs       — Step 4: AsyncReactiveProperty.BindTo
│   ├── LoadingScreenPresenter.cs      — Step 5: IProgress<float> 구현
│   └── AnimatedBarPresenter.cs        — Step 6: Model→AnimatedBar 바인딩
├── Pages/
│   ├── ManagedPageBase.cs             — Step 3: AddLifecycleEvent Presenter 어댑터
│   └── TabbedPageView.cs              — Step 3: 중첩 SheetContainer 탭
└── Views/
    ├── LoadingScreenView.cs           — Step 5: 프로그레스 바 + 상태 텍스트
    ├── AnimatedBarView.cs             — Step 6: 2-tier fill + DOTween 보간
    ├── ToastView.cs                   — Step 7: 슬라이드 + 페이드 애니메이션
    └── TooltipTrigger.cs              — Step 8: IPointerEnter/Exit + 딜레이
```

## 핵심 패턴

### VContainer 고급
- **RootLifetimeScope**: VContainerSettings → 전역 DI 컨테이너
- **IAsyncStartable**: 비동기 초기화 (설정 로드, 네트워크 연결 등)
- **ITickable**: MonoBehaviour 없는 Update 루프
- **UIFactory**: IObjectResolver.Instantiate로 DI 주입된 프리팹 생성

### R3 고급
- **CombineLatest**: 다중 자원 조건 → 건설 가능 상태 파생
- **SerializableReactiveProperty**: Inspector에서 실시간 편집 → 구독자 자동 전파
- **ObservableTracker**: Window > Observable Tracker로 구독 누수 실시간 감시

### UniTask 고급
- **WhenAny + CancelAfterSlim**: 타임아웃 다이얼로그
- **Channel\<T\>**: 다중 생산자 → 다중 소비자 이벤트 버스
- **AsyncReactiveProperty.BindTo**: TMP 텍스트 직접 바인딩
- **SuppressCancellationThrow**: 핫 루프에서 예외 없는 취소 처리

### 실전 UI
- **2-tier 체력바**: 배경 필(즉시) + 전경 필(DOTween 보간) + 스케일 펀치
- **토스트 큐**: Queue + 순차 UniTask 루프 + 최대 5개 제한
- **툴팁**: 0.4초 딜레이 + 화면 가장자리 클램핑 + ContentSizeFitter

## Project_Sun 적용 시 고려사항

- RootLifetimeScope를 VContainerSettings에 등록하여 전역 서비스 관리
- TurnEventBroker로 턴 해결 이벤트 → UI 알림 연동
- TimedDialogService로 턴 종료 전 확인 다이얼로그
- AnimatedBarView로 자원/HP 바 시각적 피드백
- ToastService로 건설 완료/자원 부족 등 알림 표시
