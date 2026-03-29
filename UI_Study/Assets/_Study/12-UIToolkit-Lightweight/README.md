# UI Toolkit 경량 스택 학습 예제

## 개요

UI Toolkit + UniTask + DOTween + Simple MVP(DI 없음) 경량 스택으로 게임 UI를 구현하는 8단계 학습 예제.
VContainer/R3 없이 C# event 기반으로 시작하고, Step 8에서 R3 부분 도입의 판단 기준을 체득한다.

## 구조

```
12-UIToolkit-Lightweight/
├── Scripts/
│   ├── Models/           -- Plain C# 모델 (UI 참조 없음, C# event)
│   │   ├── ResourceModel.cs
│   │   ├── BuildingData.cs / BuildingCatalog.cs
│   │   ├── GameResourceModel.cs
│   │   ├── ItemData.cs / InventoryModel.cs
│   │   └── SettingsModel.cs
│   ├── Views/            -- MonoBehaviour + UIDocument (표시+이벤트 노출만)
│   │   ├── ProfileCardView.cs
│   │   ├── ResourcePanelView.cs / ResourceHudView.cs
│   │   ├── ConfirmDialogView.cs / LoadingScreenView.cs / DialogDemoView.cs
│   │   ├── AnimatedPanelView.cs / StaggerListView.cs
│   │   ├── BuildingDetailView.cs / BuildMenuView.cs / TooltipView.cs
│   │   ├── InventoryView.cs / SettingsView.cs
│   │   └── SearchPanelView.cs
│   ├── Presenters/       -- Pure C# + IDisposable (비즈니스 로직)
│   │   ├── ResourcePanelPresenter.cs / ResourceHudPresenter.cs
│   │   ├── DialogDemoPresenter.cs
│   │   ├── BuildingDetailPresenter.cs / BuildMenuPresenter.cs
│   │   ├── InventoryPresenter.cs / SettingsPresenter.cs
│   │   ├── SearchPresenter_CSharpEvent.cs  -- C# event 한계 체험
│   │   ├── SearchPresenter_R3.cs           -- R3 부분 도입
│   │   └── ThrottledBuildPresenter.cs
│   ├── Utils/
│   │   ├── VisualElementTweenExtensions.cs -- DOFade/DOTranslateX/DOScale
│   │   └── ComplexityEvaluator.cs          -- 복잡도 판단 매트릭스
│   └── *Bootstrapper.cs  -- 수동 MVP 조립 (VContainer 대체)
├── UI/
│   ├── UXML/             -- 선언형 UI 마크업 (12개)
│   └── USS/              -- CSS 서브셋 스타일시트 (11개)
└── Scenes/               -- Step별 테스트 씬 (수동 생성 필요)
```

## 실행 방법

각 Step의 씬을 Unity Editor에서 생성:
1. 새 씬 생성
2. UIDocument + PanelSettings 컴포넌트 추가
3. 해당 Step의 UXML 할당
4. Bootstrapper 컴포넌트 추가 후 View 참조 연결
5. Play

## 핵심 패턴

### 1. Simple MVP (DI 없음)
- **Model**: Plain C# + `event Action<T>` (UI 참조 금지)
- **View**: MonoBehaviour + UIDocument + OnEnable 캐싱 + C# event 노출
- **Presenter**: Pure C# + IDisposable (MonoBehaviour 아님)
- **Bootstrapper**: `[SerializeField]` + Start()에서 수동 조립

### 2. 이벤트 등록 규칙
- `clicked += NamedMethod` (익명 람다 금지 — 해제 불가)
- OnDisable에서 반드시 `-=` 해제

### 3. 애니메이션 이원 체계
- **CSS Transition**: hover/active 상태 전환 (코드 없음)
- **DOTween**: Sequence, 스태거, 탄성 이징 (코드 필요)

### 4. UniTask async 패턴
- `UniTaskCompletionSource<bool>` 다이얼로그
- `destroyCancellationToken` 생명주기 연동
- `IProgress<float>` 로딩 진행률

### 5. R3 부분 도입 (Step 8만)
- `Observable.FromEvent` → Debounce/ThrottleFirst
- 프로젝트 전체가 아닌 특정 화면에서만 사용

## 학습 포인트

1. UI Toolkit의 UXML/USS는 HTML/CSS와 유사하여 AI 생성 정확도가 높다
2. VContainer/R3 없이도 C# event + Bootstrapper로 깔끔한 MVP 구현 가능
3. CSS Transition과 DOTween의 역할을 명확히 분리하면 유지보수가 쉽다
4. ListView 가상화는 1000개 이상 아이템에서 UGUI ScrollRect보다 간편하다
5. R3는 Debounce/Throttle 같은 복합 스트림이 필요할 때만 부분 도입한다

## Project_Sun 적용 시 고려사항

- 월드 스페이스 UI는 UI Toolkit 미지원 → UGUI 유지
- 화면 15개 이상으로 늘어나면 ScriptableObject 서비스 로케이터 또는 VContainer 부분 도입 검토
- UXML/USS 파일은 AI 생성 후 수동 검수 필요 (웹 CSS 미지원 속성 주의)
- DOTween VisualElement 트위닝은 `DOTween.To()` getter/setter 패턴 사용
