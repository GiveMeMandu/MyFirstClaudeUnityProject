# 고급 UI 패턴 학습 계획

- **작성일**: 2026-03-28
- **전제**: 01~04 완료
- **목표**: 라이브러리 공식 예제와 실전 게임 UI 패턴을 구현하여 프로덕션 수준의 UI 아키텍처 역량을 확보
- **예상 단계**: 8개

---

## 학습 단계 개요

| Step | 주제 | 핵심 라이브러리 | 우선순위 |
|---|---|---|---|
| 1 | VContainer 고급 스코핑 | VContainer | 필수 |
| 2 | R3 고급 오퍼레이터 | R3 | 필수 |
| 3 | USN 라이프사이클 + Presenter 통합 | UnityScreenNavigator | 필수 |
| 4 | UniTask 고급 비동기 패턴 | UniTask | 필수 |
| 5 | 로딩 화면 + 프로그레스 바 | Addressables + UniTask | 필수 |
| 6 | 체력바 / 프로그레스바 리액티브 애니메이션 | R3 + DOTween | 권장 |
| 7 | 토스트 알림 큐 시스템 | UniTask + DOTween | 권장 |
| 8 | 툴팁 시스템 | R3 | 권장 |

---

## Step 1: VContainer 고급 스코핑

- **목표**: Project Root LifetimeScope, 멀티씬 계층, RegisterFactory, IAsyncStartable
- **핵심 개념**: VContainerSettings, EnqueueParent, Func<TParam,T> 팩토리, ITickable
- **파일 목록**:
  - `Scripts/LifetimeScopes/RootLifetimeScope.cs` — VContainerSettings 연동 글로벌 스코프
  - `Scripts/Services/UIFactory.cs` — RegisterFactory 패턴
  - `Scripts/Services/AsyncBootstrapper.cs` — IAsyncStartable 비동기 초기화
  - `Scripts/Services/GameLoopService.cs` — ITickable 게임 루프
- **수락 기준**:
  - [ ] RootLifetimeScope가 씬 전환에도 유지
  - [ ] EnqueueParent로 자식 씬에 부모 스코프 주입
  - [ ] RegisterFactory로 런타임 프리팹 생성
  - [ ] IAsyncStartable로 비동기 초기화 완료 후 게임 시작

---

## Step 2: R3 고급 오퍼레이터

- **목표**: ReactiveCommand, CombineLatest, SerializableReactiveProperty, ObservableTracker
- **핵심 개념**: CanExecute 게이트, 파생 상태, Inspector 노출, 구독 누수 디버깅
- **파일 목록**:
  - `Scripts/Models/BuildActionModel.cs` — ReactiveCommand + CanExecute
  - `Scripts/Presenters/CombinedStatePresenter.cs` — CombineLatest로 다중 조건 파생
  - `Scripts/Models/InspectorModel.cs` — SerializableReactiveProperty Inspector 노출
  - `Scripts/Services/LeakDetector.cs` — ObservableTracker 활성화 + 로그
- **수락 기준**:
  - [ ] ReactiveCommand로 버튼 interactable 자동 연동
  - [ ] CombineLatest(Gold, Wood)로 "건설 가능" 상태 파생
  - [ ] Inspector에서 ReactiveProperty 값 실시간 편집 → UI 반영
  - [ ] ObservableTracker 윈도우에서 활성 구독 확인

---

## Step 3: USN 라이프사이클 + Presenter 통합

- **목표**: AddLifecycleEvent, Preloading, 중첩 SheetContainer, Addressable IAssetLoader
- **핵심 개념**: Page<TView> 제네릭 베이스, WillPushEnter 프리로드, 탭 중첩 컨테이너
- **파일 목록**:
  - `Scripts/Pages/ManagedPageBase.cs` — Page + AddLifecycleEvent Presenter 통합 베이스
  - `Scripts/Services/PreloadingNavigationService.cs` — WillPushEnter에서 다음 화면 프리로드
  - `Scripts/Pages/TabbedPageView.cs` — Page 안에 중첩 SheetContainer
  - `Scripts/Services/AddressableAssetLoaderSetup.cs` — IAssetLoader Addressable 교체
- **수락 기준**:
  - [ ] Presenter가 AddLifecycleEvent로 Page 수명주기에 연결
  - [ ] 로딩 화면 동안 다음 화면 프리로드 완료
  - [ ] Page 안에 중첩된 SheetContainer 탭 동작
  - [ ] AddressableAssetLoader로 Resources 대체

---

## Step 4: UniTask 고급 비동기 패턴

- **목표**: WhenAny 타임아웃, Channel<T> 이벤트 버스, AsyncReactiveProperty, SuppressCancellationThrow
- **핵심 개념**: CancelAfterSlim, 턴 이벤트 브로커, BindTo, Preserve
- **파일 목록**:
  - `Scripts/Services/TimedDialogService.cs` — WhenAny + CancelAfterSlim 타임아웃 다이얼로그
  - `Scripts/Services/TurnEventBroker.cs` — Channel<T> pub/sub 이벤트 버스
  - `Scripts/Presenters/AsyncBindingPresenter.cs` — AsyncReactiveProperty.BindTo TMP 직접 바인딩
- **수락 기준**:
  - [ ] 5초 타임아웃 후 다이얼로그 자동 취소
  - [ ] 이벤트 Publish → 여러 구독자에게 팬아웃
  - [ ] AsyncReactiveProperty로 TMP 텍스트 자동 바인딩

---

## Step 5: 로딩 화면 + 프로그레스 바

- **목표**: Addressables 로딩 + IProgress<float> + USN non-stacking Page
- **핵심 개념**: Progress.CreateOnlyValueChanged, 병렬 에셋 로딩 집계, 씬 전환
- **파일 목록**:
  - `Scripts/Views/LoadingScreenView.cs` — 프로그레스 바 + 상태 텍스트
  - `Scripts/Presenters/LoadingScreenPresenter.cs` — IProgress<float> 구현
  - `Scripts/Services/SceneLoadService.cs` — Addressables 씬 로딩 + 진행률
- **수락 기준**:
  - [ ] Addressables 씬 로딩 중 프로그레스 바 실시간 업데이트
  - [ ] USN Page Push(stack:false)로 로딩 화면이 히스토리에 남지 않음
  - [ ] Progress.CreateOnlyValueChanged로 불필요 UI 갱신 방지

---

## Step 6: 체력바 / 프로그레스바 리액티브 애니메이션

- **목표**: R3 ReactiveProperty → DOTween 스무스 애니메이션 + 프리뷰 필
- **핵심 개념**: 2-tier 필 (배경 + 전경), DOTween Sequence, Subscribe에서 애니메이션 트리거
- **파일 목록**:
  - `Scripts/Views/AnimatedBarView.cs` — 2-tier 필 + DOTween 펀치
  - `Scripts/Presenters/AnimatedBarPresenter.cs` — Model 변화 → 애니메이션 트리거
  - `Scripts/Models/StatModel.cs` — HP/경험치 등 범용 스탯
- **수락 기준**:
  - [ ] 값 변화 시 배경 필은 즉시 목표값, 전경 필은 DOTween 보간
  - [ ] 큰 변화 시 스케일 펀치 애니메이션
  - [ ] DelayType.UnscaledDeltaTime으로 일시정지 중에도 애니메이션 동작

---

## Step 7: 토스트 알림 큐 시스템

- **목표**: 큐 기반 순차 알림 표시 + 오브젝트 풀링
- **핵심 개념**: Queue<ToastData>, 순차 UniTask 루프, DOTween 슬라이드 인/아웃
- **파일 목록**:
  - `Scripts/Services/ToastService.cs` — 큐 관리 + 순차 표시 루프
  - `Scripts/Views/ToastView.cs` — 슬라이드 + 페이드 애니메이션
  - `Scripts/Models/ToastData.cs` — 메시지, 타입(Success/Warning/Error), 지속 시간
- **수락 기준**:
  - [ ] 연속 Enqueue 시 하나씩 순차 표시
  - [ ] 큐 최대 5개 제한
  - [ ] Success/Warning/Error 타입별 색상 구분

---

## Step 8: 툴팁 시스템

- **목표**: 호버 딜레이 → 위치 클램핑 → 자동 크기 조절 툴팁
- **핵심 개념**: IPointerEnterHandler, R3 Timer 딜레이, ContentSizeFitter, 화면 가장자리 클램핑
- **파일 목록**:
  - `Scripts/Services/TooltipService.cs` — 싱글톤 툴팁 컨트롤러
  - `Scripts/Views/TooltipView.cs` — ContentSizeFitter + 위치 클램핑
  - `Scripts/Views/TooltipTrigger.cs` — IPointerEnter/Exit + 딜레이 Observable
- **수락 기준**:
  - [ ] 0.4초 호버 후 툴팁 표시
  - [ ] 마우스 이탈 시 즉시 숨김
  - [ ] 화면 가장자리에서 위치 자동 조정

---

## 검증 체크리스트

### 아키텍처
- [ ] RootLifetimeScope가 전역 서비스 보유
- [ ] 모든 Presenter가 Pure C# + IInitializable + IDisposable
- [ ] R3 Subscribe는 Initialize()에서만
- [ ] AddTo(this) for MonoBehaviour, AddTo(_disposables) for Pure C#

### 성능
- [ ] ObservableTracker로 구독 누수 0 확인
- [ ] DOTween 시퀀스 OnDestroy에서 Kill
- [ ] DelayType.UnscaledDeltaTime for UI 애니메이션
- [ ] UNITASK_DOTWEEN_SUPPORT 스크립팅 디파인 추가

### 프로덕션 준비
- [ ] Addressables IAssetLoader로 Resources 대체
- [ ] VContainer Source Generator 평가
- [ ] Canvas 5-레이어 구조 확립
