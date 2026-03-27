# MV(R)P Foundation 학습 예제

## 개요

MV(R)P (Model-View-Reactive Presenter) 패턴을 VContainer + R3 + UniTask + DOTween 조합으로 구현한 학습 예제.

## 구조

```
Scripts/
├── Models/
│   ├── GameConfig.cs          — Step 1: Plain C# 설정 데이터
│   ├── CounterModel.cs        — Step 2: ReactiveProperty 기초
│   └── ResourceModel.cs       — Step 3: 다중 자원 관리
├── Views/
│   ├── CounterView.cs         — Step 2: 버튼+텍스트 View
│   ├── ResourceHUDView.cs     — Step 3: 자원 HUD View
│   ├── ConfirmDialogView.cs   — Step 4: 확인 다이얼로그 View
│   └── AnimatedPanelView.cs   — Step 5: DOTween 패널 애니메이션
├── Presenters/
│   ├── CounterPresenter.cs         — Step 2: 기본 MV(R)P Presenter
│   ├── ResourceHUDPresenter.cs     — Step 3: 다중 바인딩 Presenter
│   └── ResourceWithDialogPresenter.cs — Step 4: UniTask+Dialog Presenter
├── Services/
│   └── DialogService.cs       — Step 4+5: 다이얼로그 await 서비스
├── LifetimeScopes/
│   ├── StudyLifetimeScope.cs     — Step 1: VContainer 기초
│   ├── CounterLifetimeScope.cs   — Step 2: MV(R)P 등록
│   ├── ResourceLifetimeScope.cs  — Step 3: 자원 HUD 등록
│   └── DialogLifetimeScope.cs    — Step 4+5: 다이얼로그 통합 등록
└── SimpleService.cs           — Step 1: IStartable 서비스
```

## 실행 방법

각 Step의 씬을 Unity Editor에서 열고 Play:

1. 씬에 빈 GameObject 생성 → 해당 Step의 LifetimeScope 컴포넌트 부착
2. UI Canvas 생성 → View 컴포넌트 부착 + UI 요소 연결
3. LifetimeScope의 SerializeField에 View 드래그 연결
4. Play 버튼 클릭

## 핵심 패턴

### MV(R)P 레이어 규칙
- **View**: MonoBehaviour, UI 참조만, Observable 이벤트 노출
- **Presenter**: Pure C#, IInitializable/IDisposable, Subscribe는 Initialize()에서만
- **Model**: Plain C#, ReactiveProperty로 상태 노출, UI 무관

### VContainer 등록 패턴
- `Register<T>(Lifetime)` — Plain C# 클래스
- `RegisterComponent(view)` — 씬 MonoBehaviour
- `RegisterEntryPoint<T>()` — Presenter (Initialize/Dispose 자동)

### R3 구독 규칙
- Subscribe는 반드시 `IInitializable.Initialize()`에서
- 생성자(Construct)에서 Subscribe 금지 (데드락 위험)
- `CompositeDisposable` + `AddTo`로 수명 관리

### UniTask 다이얼로그 패턴
- `UniTaskCompletionSource`로 버튼 클릭을 await
- `SubscribeAwait(AwaitOperation.Drop)` — 처리 중 추가 클릭 무시
- `configureAwait: false` 필수

### DOTween UI 애니메이션
- `CanvasGroup.DOFade` + `transform.DOScale` 조합
- `AsyncWaitForCompletion()` → UniTask await 가능
- Animator 미사용 (매 프레임 Canvas dirty 방지)

## 학습 포인트

1. VContainer의 LifetimeScope가 DI 컨테이너의 루트 역할
2. ReactiveProperty는 Model의 상태를 View에 자동 전파하는 핵심 도구
3. Presenter는 MonoBehaviour가 아닌 Pure C# — 테스트 용이
4. UniTask의 UniTaskCompletionSource로 "다이얼로그 결과 await" 패턴 구현
5. DOTween은 Animator보다 UI에 적합 (성능 + 코드 중심)

## Project_Sun 적용 시 고려사항

- LifetimeScope 계층: Root → Scene → Panel/Popup 구조로 확장
- 팝업은 `LifetimeScope.CreateChildFromPrefab()`으로 동적 스코프 생성/파괴
- UnityScreenNavigator의 Page/Modal/Sheet와 LifetimeScope 연동 필요
- Addressables로 UI 프리팹 로딩 시 `IObjectResolver.InjectGameObject()` 사용
