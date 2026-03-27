# MV(R)P 기초 학습 계획

- **작성일**: 2026-03-27
- **기반 리서치**: [tech-stack-decisions.md](../research/tech-stack-decisions.md)
- **목표**: MV(R)P + VContainer + R3 패턴의 기초를 익히고, 간단한 자원 관리 HUD를 만든다
- **예상 단계**: 5개

---

## 사전 준비

### 필요 패키지 (manifest.json에 이미 추가됨)

| 패키지 | 설치 방식 | 상태 |
|---|---|---|
| VContainer | Git URL | manifest.json 추가 완료 |
| R3 + R3.Unity | NuGet + Git URL | manifest.json 추가, NuGet 수동 설치 필요 |
| UniTask | Git URL | manifest.json 추가 완료 |
| DOTween | Asset Store | 수동 설치 필요 |
| TextMeshPro | Unity 내장 | 설치됨 |

### 추가 필수 작업
- [ ] Unity Editor에서 UI_Study 프로젝트 열기
- [ ] NuGet > Manage NuGet Packages에서 `R3` 코어 설치
- [ ] Asset Store에서 DOTween 설치
- [ ] 컴파일 에러 없이 프로젝트 정상 로드 확인

### 프로젝트 구조

```
UI_Study/Assets/
├── _Study/
│   ├── 01-MVRP-Foundation/
│   │   ├── Scripts/
│   │   │   ├── Models/
│   │   │   ├── Views/
│   │   │   ├── Presenters/
│   │   │   └── LifetimeScopes/
│   │   ├── Scenes/
│   │   ├── Prefabs/
│   │   └── README.md
```

---

## 학습 단계

### Step 1: VContainer 기초 — LifetimeScope와 의존성 등록

- **목표**: VContainer의 기본 DI 패턴 이해
- **핵심 개념**: LifetimeScope, Register, RegisterComponent, Lifetime(Singleton/Scoped/Transient)
- **예제**: 간단한 서비스를 등록하고 주입받아 사용
- **파일 목록**:
  - `Scripts/Models/GameConfig.cs` — 설정 데이터 (Plain C#)
  - `Scripts/LifetimeScopes/StudyLifetimeScope.cs` — 루트 LifetimeScope
  - `Scripts/SimpleService.cs` — DI로 주입되는 간단한 서비스
  - `Scenes/01-VContainer-Basic.unity` — 테스트 씬
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] LifetimeScope에서 서비스 등록 → 다른 클래스에서 생성자 주입으로 수신
  - [ ] Console에 주입 성공 로그 출력

### Step 2: R3 기초 — ReactiveProperty와 Subscribe

- **목표**: R3의 ReactiveProperty와 구독 패턴 이해
- **핵심 개념**: ReactiveProperty, Subscribe, AddTo, CompositeDisposable, Observable 오퍼레이터
- **예제**: 카운터 값을 ReactiveProperty로 관리하고 변화를 구독
- **파일 목록**:
  - `Scripts/Models/CounterModel.cs` — ReactiveProperty<int> Count
  - `Scripts/Views/CounterView.cs` — TMP 텍스트 + 버튼
  - `Scripts/Presenters/CounterPresenter.cs` — Model↔View 연결
  - `Scripts/LifetimeScopes/CounterLifetimeScope.cs`
  - `Scenes/02-R3-Basic.unity`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] 버튼 클릭 → 카운터 증가 → 텍스트 자동 업데이트
  - [ ] Presenter는 Pure C# (MonoBehaviour 아님)
  - [ ] Subscribe는 Initialize()에서만 호출

### Step 3: MV(R)P 완성 — 자원 관리 HUD

- **목표**: 완전한 MV(R)P 패턴으로 자원 관리 HUD 제작
- **핵심 개념**: 여러 ReactiveProperty 바인딩, View 이벤트 → Model 로직, 레이어 분리 검증
- **예제**: Gold, Wood, Population 3개 자원 표시 + 자원 획득/소비 버튼
- **파일 목록**:
  - `Scripts/Models/ResourceModel.cs` — Gold, Wood, Population ReactiveProperty
  - `Scripts/Views/ResourceHUDView.cs` — 3개 자원 TMP + 획득/소비 버튼
  - `Scripts/Presenters/ResourceHUDPresenter.cs` — 양방향 바인딩
  - `Scripts/LifetimeScopes/ResourceLifetimeScope.cs`
  - `Scenes/03-Resource-HUD.unity`
  - `Prefabs/ResourceHUD.prefab`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] 3개 자원 실시간 바인딩 동작
  - [ ] 자원 부족 시 소비 불가 (Model에서 검증)
  - [ ] View에 비즈니스 로직 없음 확인
  - [ ] Model에 UI 참조 없음 확인

### Step 4: UniTask 통합 — 비동기 다이얼로그

- **목표**: UniTask로 다이얼로그 await 패턴 구현
- **핵심 개념**: UniTaskCompletionSource, async/await in Presenter, CancellationToken
- **예제**: 자원 소비 전 확인 다이얼로그 → await → 결과에 따라 처리
- **파일 목록**:
  - `Scripts/Views/ConfirmDialogView.cs` — 확인/취소 버튼, 메시지 텍스트
  - `Scripts/Presenters/ConfirmDialogPresenter.cs` — UniTaskCompletionSource 관리
  - `Scripts/Services/DialogService.cs` — ShowConfirmAsync() 제공
  - `Scripts/LifetimeScopes/DialogLifetimeScope.cs`
  - `Scenes/04-Async-Dialog.unity`
  - `Prefabs/ConfirmDialog.prefab`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] `bool result = await dialogService.ShowConfirmAsync("정말 소비?")` 동작
  - [ ] 확인 → 자원 소비, 취소 → 아무 일 없음
  - [ ] CancellationToken으로 씬 전환 시 안전 정리

### Step 5: DOTween + 패널 애니메이션

- **목표**: DOTween으로 UI 패널 열기/닫기 애니메이션 구현
- **핵심 개념**: DOTween Sequence, CanvasGroup 페이드, Scale 애니메이션, async 연동
- **예제**: Step 4의 다이얼로그에 열기/닫기 애니메이션 추가
- **파일 목록**:
  - `Scripts/Views/AnimatedPanelView.cs` — DOTween 기반 Show/Hide 애니메이션
  - `Scripts/Views/ConfirmDialogView.cs` (수정) — AnimatedPanelView 통합
  - `Scenes/05-Animated-Panel.unity`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] 다이얼로그가 Scale+Fade 애니메이션으로 열리고 닫힘
  - [ ] 애니메이션 완료를 await 가능
  - [ ] Animator 미사용 (DOTween만)

---

## 검증 체크리스트

### 아키텍처
- [ ] 모든 Presenter가 Pure C# (MonoBehaviour 아님)
- [ ] View에 비즈니스 로직 없음
- [ ] Model에 UI 참조 없음
- [ ] VContainer LifetimeScope에서 모든 의존성 등록
- [ ] Subscribe는 Initialize()/Start()에서만

### 성능
- [ ] 프레임당 GC Alloc 0 (R3 구독 루프)
- [ ] Animator 미사용 (DOTween으로 대체)

### 코드 품질
- [ ] PascalCase (public), _camelCase (private)
- [ ] async void 미사용 (UniTask/UniTaskVoid)
- [ ] UnityEngine.Input 미사용

---

## 완료 후 다음 단계

- [ ] `/ui-review 01-MVRP-Foundation`으로 코드 리뷰
- [ ] 패턴 문서화 (`Docs/UI-Study/patterns/mvrp-basic.md`)
- [ ] 2단계 학습 계획: UnityScreenNavigator + 화면 전환 패턴
- [ ] 3단계 학습 계획: Addressables + SpriteAtlas 에셋 관리
