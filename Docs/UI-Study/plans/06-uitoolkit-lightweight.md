# UI Toolkit 경량 스택 학습 계획

- **작성일**: 2026-03-29
- **기반 리서치**: [uitoolkit-lightweight-architecture.md](../research/uitoolkit-lightweight-architecture.md), [uitoolkit-unitask-dotween-patterns.md](../research/uitoolkit-unitask-dotween-patterns.md), [uitoolkit-runtime-game-ui-patterns.md](../research/uitoolkit-runtime-game-ui-patterns.md), [uitoolkit-vs-ugui-decision-matrix.md](../research/uitoolkit-vs-ugui-decision-matrix.md)
- **전제**: 01~05 완료 (UGUI MV(R)P 스택 이해)
- **목표**: UI Toolkit + UniTask + DOTween + Simple MVP 경량 스택으로 게임 UI를 구현하고, UGUI 풀 스택 대비 장단점을 체득한다
- **핵심 방향**: DI(VContainer) 없음, R3 없음을 기본으로 하되, 복잡도 임계점에서 부분 도입하는 판단력을 기른다
- **예상 단계**: 8개

---

## 사전 준비

### 필요 패키지

| 패키지 | 설치 방식 | 상태 |
|---|---|---|
| UI Toolkit | Unity 내장 (6000.0+) | 설치됨 |
| UniTask | Git URL | manifest.json 추가 완료 |
| DOTween | Asset Store | 설치 완료 |
| R3 + R3.Unity | NuGet + Git URL | Step 8에서만 사용 (이미 설치됨) |

### 추가 필수 작업
- [ ] Unity Editor에서 UI_Study 프로젝트 열기
- [ ] PanelSettings 에셋 생성 확인 (Scale With Screen Size, 1920x1080 기준)
- [ ] `UNITASK_DOTWEEN_SUPPORT` 스크립팅 디파인 확인
- [ ] 컴파일 에러 없이 프로젝트 정상 로드 확인

### 프로젝트 구조

```
UI_Study/Assets/_Study/12-UIToolkit-Lightweight/
├── Scripts/
│   ├── Models/
│   ├── Views/
│   ├── Presenters/
│   └── Utils/
├── UI/
│   ├── UXML/
│   └── USS/
├── Scenes/
└── README.md
```

---

## 학습 단계 개요

| Step | 주제 | 핵심 기술 | 우선순위 |
|---|---|---|---|
| 1 | UI Toolkit 기초 — VisualElement, UXML, USS | UI Toolkit | 필수 |
| 2 | 심플 MVP 패턴 — DI 없는 View-Presenter-Model | Simple MVP + C# events | 필수 |
| 3 | UniTask 통합 — async 다이얼로그, 로딩 | UniTask + UI Toolkit | 필수 |
| 4 | DOTween + CSS Transition — 애니메이션 이원 체계 | DOTween + USS Transition | 필수 |
| 5 | AI 생성 워크플로우 — UXML/USS 프롬프트 패턴 | AI + UXML/USS | 권장 |
| 6 | 게임 UI 화면 1 — 자원 HUD + 건설 메뉴 | 종합 (HUD + 그리드) | 필수 |
| 7 | 게임 UI 화면 2 — 인벤토리 + 설정 | ListView + TabView | 필수 |
| 8 | 복잡성 판단 실습 — "여기서 R3가 필요한가?" | R3 부분 도입 판단 | 필수 |

---

## Step 1: UI Toolkit 기초 — VisualElement, UXML, USS

- **목표**: UI Toolkit의 기본 구성 요소(VisualElement, UXML, USS)를 이해하고, 코드 없이 선언형 UI를 만들 수 있다
- **핵심 개념**: UIDocument, VisualElement 트리, UXML 마크업, USS 스타일시트, Q/Q<T> 쿼리, pseudo-class(:hover, :active, :disabled), Flexbox 레이아웃
- **예제**: 프로필 카드 화면 — 이름/레벨/아이콘을 UXML로 선언하고, USS로 스타일링하고, C# 코드로 텍스트만 변경한다
- **파일 목록**:
  - `UI/UXML/ProfileCard.uxml` — 프로필 카드 UXML 레이아웃
  - `UI/USS/ProfileCard.uss` — Flexbox 레이아웃 + 색상 + pseudo-class 스타일
  - `Scripts/Views/ProfileCardView.cs` — UIDocument에서 요소 쿼리 + 텍스트 설정
  - `Scenes/01-UIToolkit-Basic.unity` — 테스트 씬 (UIDocument + PanelSettings)
- **수락 기준**:
  - [x] 컴파일 통과
  - [x] UXML에서 선언한 VisualElement 구조가 UI Builder에서 확인됨
  - [x] USS로 Flexbox 레이아웃(row/column), 색상, 패딩, 마진 적용
  - [x] :hover pseudo-class로 카드 호버 시 배경색 변경 (코드 없이)
  - [x] C# 코드에서 `Q<Label>("player-name")` 쿼리로 텍스트 변경 동작
  - [ ] PanelSettings의 Scale With Screen Size 설정 확인

---

## Step 2: 심플 MVP 패턴 — DI 없는 View-Presenter-Model

- **목표**: VContainer 없이 순수 C# 이벤트와 Bootstrapper 패턴으로 완전한 MVP 3계층을 구현한다
- **핵심 개념**: Model(C# event로 변경 알림), View(UIDocument + 요소 캐싱 + event 노출), Presenter(Pure C# class + IDisposable), Bootstrapper(수동 조립), OnEnable 요소 캐싱 패턴
- **예제**: 자원 패널(Gold/Wood/Food) — 자원 획득/소비 버튼, Model의 C# event로 View 자동 갱신, Bootstrapper가 View+Model+Presenter를 조립
- **파일 목록**:
  - `UI/UXML/ResourcePanel.uxml` — 자원 표시 라벨 3개 + 획득/소비 버튼
  - `UI/USS/ResourcePanel.uss` — 자원 패널 스타일
  - `Scripts/Models/ResourceModel.cs` — Gold/Wood/Food 프로퍼티 + C# event(GoldChanged, WoodChanged, FoodChanged)
  - `Scripts/Views/ResourcePanelView.cs` — UIDocument 보유 MonoBehaviour, OnEnable에서 요소 캐싱, C# event로 입력 노출
  - `Scripts/Presenters/ResourcePanelPresenter.cs` — Pure C# class, IDisposable, View event 구독 → Model 업데이트, Model event 구독 → View 갱신
  - `Scripts/ResourceBootstrapper.cs` — SerializeField로 View 참조, Start()에서 Model+Presenter 생성+연결
  - `Scenes/02-SimpleMVP.unity` — 테스트 씬
- **수락 기준**:
  - [x] 컴파일 통과
  - [x] Bootstrapper가 Model, View, Presenter를 조립 (VContainer 미사용)
  - [x] 획득 버튼 클릭 → Model 업데이트 → View 자원 텍스트 자동 갱신
  - [x] 소비 버튼 클릭 → Model.CanSpend() 검증 → 부족 시 소비 불가
  - [x] Presenter가 Pure C# (MonoBehaviour 아님)
  - [x] View에 비즈니스 로직 없음 (display 메서드만)
  - [x] Model에 UI 참조 없음 (C# event만)
  - [x] Bootstrapper.OnDestroy()에서 Presenter.Dispose() 호출 확인

---

## Step 3: UniTask 통합 — async 다이얼로그, 로딩

- **목표**: UniTask의 UniTaskCompletionSource를 활용하여 UI Toolkit에서 async/await 다이얼로그와 로딩 화면을 구현한다
- **핵심 개념**: UniTaskCompletionSource<bool>, button.clicked 수동 연결 (PR #338 stale로 공식 확장 없음), CancellationToken + destroyCancellationToken, IProgress<float> + ProgressBar, DisplayStyle.None/Flex 토글
- **예제 A**: 확인 다이얼로그 — `bool result = await dialog.ShowAsync("정말 소비?", ct)` 패턴으로 사용자 응답 대기
- **예제 B**: 로딩 화면 — ProgressBar + IProgress<float>로 진행률 실시간 표시
- **파일 목록**:
  - `UI/UXML/ConfirmDialog.uxml` — 메시지 라벨 + 확인/취소 버튼
  - `UI/USS/ConfirmDialog.uss` — 모달 배경(position: absolute) + 다이얼로그 중앙 배치
  - `UI/UXML/LoadingScreen.uxml` — ProgressBar + 상태 라벨
  - `UI/USS/LoadingScreen.uss` — 전체 화면 로딩 오버레이 스타일
  - `Scripts/Views/ConfirmDialogView.cs` — UniTaskCompletionSource<bool> 기반 ShowAsync()
  - `Scripts/Views/LoadingScreenView.cs` — ProgressBar.value 업데이트 + Show/Hide
  - `Scripts/Presenters/DialogDemoPresenter.cs` — 다이얼로그 호출 + 결과 처리 데모
  - `Scripts/DialogDemoBootstrapper.cs` — 씬 조립
  - `Scenes/03-AsyncDialog.unity` — 테스트 씬
- **수락 기준**:
  - [x] 컴파일 통과
  - [x] `await dialog.ShowAsync("메시지", ct)` → 확인 시 true, 취소 시 false 반환
  - [x] CancellationToken으로 OnDisable/OnDestroy 시 안전 정리 (TrySetCanceled)
  - [x] ProgressBar가 0~100% 실시간 업데이트
  - [x] 로딩 완료 후 Hide() 정상 동작
  - [x] async void 미사용 (UniTask/UniTaskVoid 사용)
  - [x] ct.RegisterWithoutCaptureExecutionContext 패턴 적용

---

## Step 4: DOTween + CSS Transition — 애니메이션 이원 체계

- **목표**: USS Transition(선언형)과 DOTween(절차적)의 역할 분담을 이해하고, 양쪽을 적절히 조합하여 UI 애니메이션을 구현한다
- **핵심 개념**: USS transition-property/duration/timing-function, DOTween.To() getter/setter 람다, VisualElement 확장 메서드(DOFade, DOTranslateX), Sequence, SetEase, 스태거(지연 반복), CSS Transition은 상태 전환(hover/active)에, DOTween은 시퀀스/스태거/탄성에 적합
- **예제 A**: USS Transition — 버튼 hover/active 시 배경색+스케일 CSS 전환
- **예제 B**: DOTween 패널 — 팝업이 scale(0→1) + fade(0→1) Sequence로 열리고, 역순으로 닫힘
- **예제 C**: DOTween 스태거 — 리스트 아이템이 0.05초 간격으로 순차 슬라이드 인
- **파일 목록**:
  - `UI/UXML/AnimationDemo.uxml` — CSS 버튼 + DOTween 패널 + 스태거 리스트
  - `UI/USS/AnimationDemo.uss` — transition 선언 (hover/active) + 기본 레이아웃
  - `Scripts/Utils/VisualElementTweenExtensions.cs` — DOFade, DOTranslateX, DOTranslateY, DOScale 확장 메서드
  - `Scripts/Views/AnimatedPanelView.cs` — DOTween Sequence 기반 ShowAsync/HideAsync
  - `Scripts/Views/StaggerListView.cs` — DOTween 스태거 리스트 애니메이션
  - `Scripts/AnimationDemoBootstrapper.cs` — 데모 씬 조립
  - `Scenes/04-AnimationDual.unity` — 비교 테스트 씬
- **수락 기준**:
  - [x] 컴파일 통과
  - [x] USS transition으로 hover 시 배경색+border 변경 (C# 코드 없이)
  - [x] DOTween Sequence로 패널 open: scale(0→1) + fade(0→1) 동시 재생
  - [x] DOTween Sequence로 패널 close: 역순 재생 후 DisplayStyle.None
  - [x] 스태거: 5개 아이템이 0.05초 간격으로 순차 슬라이드 인
  - [x] VisualElementTweenExtensions로 DOFade, DOTranslateX 동작
  - [x] DOTween.To() getter/setter 패턴으로 opacity 트위닝
  - [x] OnDestroy에서 Sequence.Kill() 정리

---

## Step 5: AI 생성 워크플로우 — UXML/USS 프롬프트 패턴

- **목표**: AI(Claude, ChatGPT 등)를 활용하여 UXML/USS를 생성하고, 출력을 평가/수정하는 워크플로우를 확립한다
- **핵심 개념**: UXML/USS = HTML/CSS 유사 구조로 AI 생성 정확도 높음, 웹 CSS → USS 변환 주의사항(지원하지 않는 속성: box-shadow, gradient, @keyframes), AI 프롬프트 구조화(레이아웃 설명 → 색상/타이포 → 상호작용 → 데이터 바인딩), 반복 개선 루프
- **예제**: AI에게 "기지 경영 게임의 건물 상세 팝업" 설명 → UXML+USS+C# 코드 생성 → 수정 → 통합
- **파일 목록**:
  - `UI/UXML/BuildingDetail.uxml` — AI 생성 후 수정된 건물 상세 팝업
  - `UI/USS/BuildingDetail.uss` — AI 생성 후 USS 유효성 수정
  - `Scripts/Views/BuildingDetailView.cs` — AI 생성 코드 기반 View
  - `Scripts/Presenters/BuildingDetailPresenter.cs` — AI 생성 후 MVP 패턴에 맞게 리팩터링
  - `Scripts/Models/BuildingData.cs` — 건물 데이터 모델
  - `Scripts/BuildingDetailBootstrapper.cs` — 씬 조립
  - `Scenes/05-AIWorkflow.unity` — 테스트 씬
  - `README-AI-Workflow.md` — AI 프롬프트 템플릿 + 수정 체크리스트 기록
- **수락 기준**:
  - [x] 컴파일 통과
  - [x] AI 생성 UXML이 Unity UI Builder에서 정상 렌더링
  - [x] 웹 CSS에서 USS 미지원 속성 식별 및 대체 (box-shadow → border, gradient → 단색)
  - [x] AI 생성 C# 코드가 MVP 패턴(View/Presenter/Model 분리)을 준수
  - [ ] README-AI-Workflow.md에 프롬프트 템플릿과 수정 과정 기록

---

## Step 6: 게임 UI 화면 1 — 자원 HUD + 건설 메뉴

- **목표**: Step 1~4에서 익힌 기술을 종합하여 실제 게임 화면 2종(상시 표시 HUD, 건설 메뉴)을 구현한다
- **핵심 개념**: 다중 UIDocument 레이어링(Sort Order: HUD=0, Screen=10), 영속 HUD vs 토글 메뉴, Flexbox wrap 그리드, EnableInClassList로 상태 전환(잠금/구매가능/비용초과), USS transition으로 카드 hover, 호버 툴팁(position: absolute + PointerEnterEvent)
- **예제**: 상단 자원 바(Gold/Wood/Food/Pop) + 건설 메뉴(3x3 건물 카드 그리드) — 자원 부족 시 카드 비활성, 호버 시 비용 툴팁
- **파일 목록**:
  - `UI/UXML/ResourceHUD.uxml` — 상단 자원 바 (아이콘 + 라벨 x4)
  - `UI/USS/ResourceHUD.uss` — HUD 스타일 + flash 전환 효과
  - `UI/UXML/BuildMenu.uxml` — 건물 카드 그리드 + 툴팁 영역
  - `UI/USS/BuildMenu.uss` — 카드 스타일 + hover/locked/affordable/too-expensive 상태
  - `UI/UXML/Tooltip.uxml` — 범용 툴팁 (건물 이름 + 비용 + 설명)
  - `UI/USS/Tooltip.uss` — 툴팁 스타일 (position: absolute, 화면 클램핑)
  - `Scripts/Models/GameResourceModel.cs` — 4종 자원 + C# event
  - `Scripts/Models/BuildingCatalog.cs` — 건물 데이터 목록 (ScriptableObject)
  - `Scripts/Views/ResourceHudView.cs` — HUD View + flash 애니메이션(USS transition)
  - `Scripts/Views/BuildMenuView.cs` — 카드 그리드 생성 + 상태 클래스 토글
  - `Scripts/Views/TooltipView.cs` — 포인터 추적 + 화면 가장자리 클램핑
  - `Scripts/Presenters/ResourceHudPresenter.cs` — Model → HUD View 바인딩
  - `Scripts/Presenters/BuildMenuPresenter.cs` — 카드 클릭 → 건설 로직 + 자원 검증
  - `Scripts/GameUIBootstrapper.cs` — HUD + BuildMenu + Tooltip 조립
  - `Scenes/06-GameUI-HUD-Build.unity` — 통합 테스트 씬
- **수락 기준**:
  - [x] 컴파일 통과
  - [x] HUD가 Sort Order 0으로 항상 표시, 건설 메뉴가 Sort Order 10으로 토글
  - [x] 자원 변경 시 HUD 라벨 텍스트 갱신 + USS transition flash 효과
  - [x] 건물 카드 9개가 flex-wrap으로 3x3 그리드 배치
  - [x] 카드 hover 시 USS transition으로 배경색+border 변경
  - [x] 자원 부족 시 EnableInClassList("building-card--too-expensive", true)
  - [x] 카드 hover 시 툴팁 표시 + 마우스 이탈 시 즉시 숨김
  - [x] 툴팁이 화면 가장자리에서 위치 자동 조정 (클램핑)
  - [x] 건물 클릭 → Step 3의 확인 다이얼로그 → 자원 소비

---

## Step 7: 게임 UI 화면 2 — 인벤토리 + 설정

- **목표**: UI Toolkit의 ListView 가상화와 TabView를 활용하여 데이터 집약형 화면을 구현한다
- **핵심 개념**: ListView(makeItem/bindItem 콜백, 가상화, selectionChanged), 정렬/필터 후 RefreshItems(), TabView 컨트롤, SetValueWithoutNotify로 초기화/취소 패턴, Slider/Toggle/DropdownField 내장 컨트롤, Apply/Cancel 패턴(원본 복사 → 편집 → Apply 시 원본에 반영)
- **예제 A**: 인벤토리 — 1000개 아이템 ListView 가상화 + 이름/등급 정렬 + 텍스트 필터
- **예제 B**: 설정 — TabView(그래픽/사운드/게임플레이 탭) + Apply/Cancel
- **파일 목록**:
  - `UI/UXML/Inventory.uxml` — 검색 TextField + 정렬 DropdownField + ListView + 상세 패널
  - `UI/USS/Inventory.uss` — 인벤토리 스타일 + 아이템 행 스타일
  - `UI/UXML/InventoryItem.uxml` — ListView 아이템 템플릿 (아이콘 + 이름 + 등급)
  - `UI/UXML/Settings.uxml` — TabView + 그래픽/사운드/게임플레이 탭 내용
  - `UI/USS/Settings.uss` — 설정 화면 스타일
  - `Scripts/Models/InventoryModel.cs` — 아이템 리스트 + 정렬/필터 메서드 + C# event
  - `Scripts/Models/ItemData.cs` — 아이템 데이터 (이름, 등급, 아이콘, 설명)
  - `Scripts/Models/SettingsModel.cs` — 설정 데이터 + Clone() + Apply 패턴
  - `Scripts/Views/InventoryView.cs` — ListView 설정 (makeItem/bindItem) + 검색/정렬 UI
  - `Scripts/Views/SettingsView.cs` — TabView + Slider/Toggle/DropdownField 바인딩
  - `Scripts/Presenters/InventoryPresenter.cs` — 필터/정렬 → Model 업데이트 → RefreshItems()
  - `Scripts/Presenters/SettingsPresenter.cs` — Apply/Cancel 로직 + SetValueWithoutNotify 초기화
  - `Scripts/InventorySettingsBootstrapper.cs` — 씬 조립
  - `Scenes/07-Inventory-Settings.unity` — 통합 테스트 씬
- **수락 기준**:
  - [x] 컴파일 통과
  - [x] ListView가 1000개 아이템을 버벅임 없이 스크롤 (가상화 동작 확인)
  - [x] makeItem에서 InventoryItem.uxml 인스턴스 생성, bindItem에서 데이터 바인딩
  - [x] 검색 TextField 입력 → 필터 적용 → RefreshItems() 갱신
  - [x] 정렬 DropdownField 변경 → 이름/등급 정렬 → RefreshItems() 갱신
  - [x] 아이템 선택 시 상세 패널에 정보 표시 (selectionChanged)
  - [x] TabView에서 그래픽/사운드/게임플레이 탭 전환 동작
  - [x] Apply 버튼 → 설정 원본에 반영, Cancel 버튼 → 편집 취소 + SetValueWithoutNotify로 UI 복원
  - [x] Slider/Toggle/DropdownField 값 변경이 설정 모델에 반영

---

## Step 8: 복잡성 판단 실습 — "여기서 R3가 필요한가?"

- **목표**: C# event의 한계를 직접 체험하고, R3를 부분 도입하는 판단 기준과 방법을 익힌다
- **핵심 개념**: C# event의 한계(디바운스/쓰로틀/CombineLatest 수동 구현 어려움), R3 부분 도입(프로젝트 전체가 아닌 특정 화면만), Observable.FromEvent로 C# event → R3 스트림 변환, Debounce/Throttle 오퍼레이터, 복잡도 임계점 매트릭스(화면 수/공유 상태/스트림 연산 필요도)
- **예제 A**: Step 7의 인벤토리 검색에 디바운스 추가 — 먼저 UniTask.Delay 수동 디바운스 시도 → 경쟁 조건 체험 → R3 Debounce로 교체
- **예제 B**: 건설 버튼 빠른 클릭 방지 — R3 ThrottleFirst로 0.5초 간격 제한
- **예제 C**: 복잡도 판단 연습 — 현 프로젝트의 화면/상태/스트림 요구를 매트릭스에 대입하여 판정
- **파일 목록**:
  - `Scripts/Views/SearchPanelView.cs` — TextField + 검색 결과 ListView
  - `Scripts/Presenters/SearchPresenter_CSharpEvent.cs` — C# event + UniTask.Delay 수동 디바운스 (한계 체험용)
  - `Scripts/Presenters/SearchPresenter_R3.cs` — R3 Observable.FromEvent + Debounce 도입 버전
  - `Scripts/Presenters/ThrottledBuildPresenter.cs` — R3 ThrottleFirst 빠른 클릭 방지
  - `Scripts/Utils/ComplexityEvaluator.cs` — 복잡도 매트릭스 체크리스트 (코드 주석으로 판단 기준 기록)
  - `Scripts/ComplexityDemoBootstrapper.cs` — 씬 조립 (양쪽 Presenter 전환 가능)
  - `UI/UXML/SearchPanel.uxml` — 검색 패널 레이아웃
  - `UI/USS/SearchPanel.uss` — 검색 패널 스타일
  - `Scenes/08-ComplexityDecision.unity` — 비교 테스트 씬
- **수락 기준**:
  - [x] 컴파일 통과
  - [x] 수동 디바운스(UniTask.Delay)가 빠른 연속 입력에서 경쟁 조건 발생 확인
  - [x] R3 Debounce(300ms)로 교체 후 경쟁 조건 해소 + 마지막 입력만 처리
  - [x] R3 ThrottleFirst(500ms)로 건설 버튼 연타 방지 동작
  - [x] R3는 이 화면에만 도입, 다른 Step의 코드는 C# event 유지 (부분 도입 확인)
  - [x] ComplexityEvaluator.cs에 판단 기준 코드 주석 작성 완료
  - [x] README에 "언제 R3를 도입할 것인가" 판단 결과 기록

---

## 검증 체크리스트

### 아키텍처
- [x] 모든 Presenter가 Pure C# class + IDisposable (MonoBehaviour 아님)
- [x] View에 비즈니스 로직 없음 (display 메서드 + C# event 노출만)
- [x] Model에 UI 참조 없음 (C# event로만 변경 알림)
- [x] VContainer 미사용 (Step 1~7 전체)
- [x] R3 미사용 (Step 1~7), Step 8에서만 부분 도입
- [x] Bootstrapper 패턴으로 수동 의존성 조립

### 성능
- [x] ListView 가상화 1000개 이상 항목 60fps 유지
- [x] USS transition으로 처리 가능한 애니메이션은 DOTween 미사용
- [x] DOTween Sequence는 OnDestroy에서 Kill
- [x] UIDocument Sort Order 레이어링 정상 동작

### UI Toolkit 규칙
- [x] OnEnable에서 요소 캐싱, OnDisable에서 이벤트 해제
- [x] button.clicked += named method (익명 람다 금지, 해제 불가 방지)
- [x] DisplayStyle.None/Flex로 표시/숨김 (SetActive 대신)
- [x] Q<T>("name") 쿼리로 요소 접근 (Find/GetChild 미사용)

### 코드 품질
- [x] PascalCase (public), _camelCase (private)
- [x] async void 미사용 (UniTask/UniTaskVoid 사용)
- [x] UnityEngine.Input 미사용 (New Input System)
- [x] CancellationToken으로 비동기 작업 안전 정리

---

## 완료 후 다음 단계

- [x] `/ui-review 12-UIToolkit-Lightweight`로 코드 리뷰 — A- 등급, Warning 6건 수정
- [ ] 패턴 문서화 (`Docs/UI-Study/patterns/uitoolkit-simple-mvp.md`)
- [ ] UGUI 풀 스택(01~05) vs UI Toolkit 경량 스택(06) 비교 회고 작성
- [ ] 프로덕션 적용 계획: Project_Sun에 UI Toolkit 경량 스택 도입 여부 결정
- [ ] 하이브리드 전략 실습: UI Toolkit(스크린 UI) + UGUI(월드 공간 UI) 공존 테스트
