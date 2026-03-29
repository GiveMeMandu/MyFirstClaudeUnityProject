# UI Toolkit 경량 스택 코드 리뷰

- **리뷰일**: 2026-03-29
- **대상**: UI_Study/Assets/_Study/12-UIToolkit-Lightweight/
- **등급**: A-
- **파일 수**: C# 31개, UXML 12개, USS 11개

---

## 요약

전반적으로 매우 높은 품질의 학습 예제이다. MVP 레이어 분리가 일관되고, C# event 기반의 구독/해제가 거의 모든 파일에서 올바르게 처리되었으며, UI Toolkit 베스트 프랙티스(OnEnable 캐싱, named method, DisplayStyle 토글)를 충실히 따른다. UniTask/DOTween/R3 사용도 적절하다. Critical 이슈는 없고, 주로 방어적 코딩 누락과 미사용 코드 수준의 Warning이 존재한다.

---

## Critical Issues (반드시 수정)

없음.

---

## Warnings (수정 권장)

### WARN-01: ResourcePanelPresenter -- 생성자에서 이벤트 구독

- **파일**: `Scripts/Presenters/ResourcePanelPresenter.cs:24-30`
- **문제**: 생성자에서 View/Model 이벤트를 구독한다. `Initialize()`가 호출되기 전에 이벤트가 발생하면 View에 초기값이 표시되지 않은 상태에서 콜백이 실행될 수 있다. 현재 코드에서는 Bootstrapper가 `Start()`에서 순차 생성하므로 실제 문제가 발생하지 않지만, 모든 Presenter에서 구독 시점을 `Initialize()`로 통일하면 일관성과 안전성이 향상된다.
- **영향 범위**: ResourcePanelPresenter, ResourceHudPresenter, BuildingDetailPresenter, BuildMenuPresenter, InventoryPresenter, SettingsPresenter (총 6개 Presenter)
- **수정안**:
```csharp
public ResourcePanelPresenter(ResourceModel model, ResourcePanelView view)
{
    _model = model;
    _view = view;
    // 구독은 Initialize()에서 수행
}

public void Initialize()
{
    // 구독 등록
    _view.OnGainClicked  += HandleGain;
    _view.OnSpendClicked += HandleSpend;
    _model.GoldChanged += _view.SetGold;
    _model.WoodChanged += _view.SetWood;
    _model.FoodChanged += _view.SetFood;

    // 초기 표시
    _view.SetGold(_model.Gold);
    _view.SetWood(_model.Wood);
    _view.SetFood(_model.Food);
    _view.SetStatus("Ready");
}
```

### WARN-02: ProfileCardView -- Step 1에서 View가 비즈니스 로직을 포함

- **파일**: `Scripts/Views/ProfileCardView.cs:62-86`
- **문제**: `OnLevelUpClicked()`에서 `_level++`을 하고 스탯을 직접 계산한다. 이는 View가 Model과 Presenter의 역할을 겸하는 것으로, 학습 목적(Step 1 = 기초)을 고려해도 나중 Step과의 일관성이 깨진다.
- **수정안**: Step 1은 MVP 이전 단계이므로, 코드 자체는 문제가 없으나 주석으로 "Step 1은 의도적으로 MVP 없이 작성됨. Step 2에서 분리됨"을 명시하면 학습 흐름이 명확해진다. 현재 주석(`Step 1: UI Toolkit 기초`)만으로는 의도가 충분히 전달되지 않는다.

### WARN-03: BuildMenuView.OnCardHoverExit -- null 체크 누락

- **파일**: `Scripts/Views/BuildMenuView.cs:158`
- **문제**: `OnCardHoverExit?.Invoke()`가 아닌 `OnCardHoverExit.Invoke()`로 호출한다. 구독자가 없으면 NullReferenceException이 발생한다.
- **수정안**:
```csharp
private void HandleCardPointerLeave(PointerLeaveEvent evt)
{
    OnCardHoverExit?.Invoke();
}
```

### WARN-04: BuildingData의 public setter -- Model의 불변성 위반 가능

- **파일**: `Scripts/Models/BuildingData.cs:13-18`
- **문제**: `Name`, `Description`, `GoldCost` 등 모든 프로퍼티에 `set`이 public이다. Presenter가 `building.Level++`로 직접 수정하는 패턴(BuildingDetailPresenter:50, BuildMenuPresenter:69)은 Model의 변경 알림 없이 상태를 변경한다. 학습 예제 범위에서는 허용 가능하지만, 실제 프로젝트에서는 Model 내부에서만 상태를 변경하고 이벤트를 발생시켜야 한다.
- **영향 파일**: `BuildingDetailPresenter.cs:50`, `BuildMenuPresenter.cs:69`
- **수정안**: 최소한 setter를 `internal` 또는 `private set`으로 제한하고, 레벨 업 로직을 Model 쪽 메서드로 이동:
```csharp
[field: SerializeField] public int Level { get; private set; } = 1;

public bool TryLevelUp()
{
    if (IsMaxLevel) return false;
    Level++;
    return true;
}
```

### WARN-05: SettingsView._statusLabel 필드 선언 후 미사용

- **파일**: `Scripts/Views/SettingsView.cs:23`
- **문제**: `_statusLabel` 필드가 선언되었으나 OnEnable에서 초기화되지 않고, 어디에서도 사용되지 않는다. 죽은 코드이다.
- **수정안**: 불필요하면 제거. 향후 Apply/Cancel 결과 표시에 사용할 예정이면 OnEnable에서 캐싱하고 `SetStatus()` 메서드를 추가한다.

### WARN-06: DialogDemoPresenter -- SimulateLoadingAsync 취소 시 Loading 화면이 닫히지 않음

- **파일**: `Scripts/Presenters/DialogDemoPresenter.cs:49-59`
- **문제**: `SimulateLoadingAsync`에서 `await SimulateWorkAsync()`가 `OperationCanceledException`을 throw하면 `_loading.Hide()`에 도달하지 않는다. 로딩 화면이 열린 채로 남는다.
- **수정안**:
```csharp
private async UniTaskVoid SimulateLoadingAsync(CancellationToken ct)
{
    _loading.Show();
    try
    {
        var progress = new Progress<float>(v => _loading.SetProgress(v));
        await SimulateWorkAsync(progress, ct);
        _demoView.SetResult("Loading complete!");
    }
    catch (OperationCanceledException)
    {
        _demoView.SetResult("Loading cancelled.");
    }
    finally
    {
        _loading.Hide();
    }
}
```

### WARN-07: InventoryModel.GenerateDummyItems -- 생성자에서 ItemsChanged 발화

- **파일**: `Scripts/Models/InventoryModel.cs:74`
- **문제**: 생성자 내부의 `GenerateDummyItems()`에서 `ItemsChanged?.Invoke()`가 호출된다. 생성자 시점에서는 아직 아무도 이벤트를 구독하지 않았으므로 이 호출은 항상 no-op이다. 혼란을 줄 수 있으므로 제거하거나, Initialize 패턴으로 분리하는 것이 좋다.
- **수정안**: 생성자 내 `ItemsChanged?.Invoke()` 호출 제거.

### WARN-08: DOTween 확장의 DOTranslateX/Y -- 캡처된 local 변수의 클로저 문제

- **파일**: `Scripts/Utils/VisualElementTweenExtensions.cs:24-31`
- **문제**: `DOTranslateX`에서 `var current = el.resolvedStyle.translate;`로 struct를 복사한 뒤 getter에서 `current.x`를 반환한다. 그러나 `current`는 메서드 호출 시점의 스냅샷이며, Tween 진행 중 다른 곳에서 translate가 변경되면 getter가 실제 현재값을 반환하지 않는다. DOTween.To의 getter는 현재 값을 반환해야 올바르게 보간된다.
- **수정안**:
```csharp
public static Tween DOTranslateX(this VisualElement el, float endValue, float duration)
{
    return DOTween.To(
        () => el.resolvedStyle.translate.x,
        x => el.style.translate = new Translate(x, el.resolvedStyle.translate.y),
        endValue, duration);
}
```

---

## Suggestions (선택적 개선)

### SUGG-01: SearchPresenter_R3 -- Debounce가 메인 스레드에서 실행되는지 명시

- **파일**: `Scripts/Presenters/SearchPresenter_R3.cs:47-48`
- **문제**: `Debounce`는 기본적으로 `TimeProvider.System`을 사용하며, Subscribe 콜백이 메인 스레드에서 실행되는 것이 보장되지 않을 수 있다. 현재 Unity의 R3 설치에서는 `ObservableSystem.DefaultTimeProvider`가 Unity 프레임 기반이므로 문제없지만, 교육 자료로서 `ObserveOnMainThread()` 또는 주석으로 스레드 안전성을 명시하면 좋다.
- **수정안**: 주석 추가로 충분:
```csharp
// R3 Unity에서 Debounce는 UnityTimeProvider를 사용하므로 메인 스레드에서 콜백 실행
.Debounce(TimeSpan.FromMilliseconds(300))
```

### SUGG-02: ConfirmDialogView -- ct.RegisterWithoutCaptureExecutionContext 대신 ct.Register 고려

- **파일**: `Scripts/Views/ConfirmDialogView.cs:58-59`
- **문제**: `RegisterWithoutCaptureExecutionContext`는 UniTask 확장 메서드로, 성능 최적화를 위한 선택이다. 학습 코드에서는 표준 `ct.Register()`가 더 이해하기 쉽다.
- **수정안**: 학습 목적이라면 주석으로 왜 이 메서드를 사용하는지 설명 추가. 또는 간단히 `ct.Register()`로 대체.

### SUGG-03: ProfileCard UXML -- UnityEditor 네임스페이스 참조 불필요

- **파일**: `UI/UXML/ProfileCard.uxml:1`
- **문제**: `xmlns:uie="UnityEditor.UIElements"`가 선언되었지만 UXML 내에서 사용되지 않는다. 런타임 빌드에서는 에디터 전용 요소가 문제가 될 수 있다. 다른 모든 UXML 파일에는 이 선언이 없다.
- **수정안**: `xmlns:uie="UnityEditor.UIElements"` 제거.

### SUGG-04: 일부 UXML에서 기본값 하드코딩

- **파일**: `UI/UXML/ResourcePanel.uxml:12`, `UI/UXML/BuildingDetail.uxml:23-24` 등
- **문제**: `text="0"`, `text="Building Name"` 등 기본 표시값이 UXML에 하드코딩되어 있다. C# View에서 초기화하므로 실질적 문제는 없으나, UI Builder에서 편집 시 혼동될 수 있다.
- **수정안**: 의도된 플레이스홀더 텍스트라면 현행 유지. 교육 자료로서 UXML 편집 단계를 보여주는 목적이면 적절하다.

### SUGG-05: GameResourceModel과 ResourceModel의 중복

- **파일**: `Scripts/Models/ResourceModel.cs`, `Scripts/Models/GameResourceModel.cs`
- **문제**: 두 모델이 매우 유사한 구조(Gold/Wood/Food + event)를 가진다. Step 2 vs Step 6 분리를 위한 의도적 중복이지만, 공통 베이스 클래스나 인터페이스로 추출하면 코드 재사용이 가능하다.
- **수정안**: 학습 목적으로 단계별 독립성이 중요하므로 현행 유지 가능. README에 "의도적 중복" 언급 추가 권장.

### SUGG-06: BuildingCatalog.CreateDefault -- 런타임에 ScriptableObject.CreateInstance 호출

- **파일**: `Scripts/Models/BuildingCatalog.cs:24`
- **문제**: `ScriptableObject.CreateInstance`는 런타임에 사용 가능하지만, 에셋으로 저장되지 않으므로 GC 대상이다. 카탈로그가 없을 때의 폴백으로서 적절하나, 에디터 환경에서 에셋을 반드시 생성하도록 유도하는 주석이 있으면 좋다.
- **수정안**: 이미 GameUIBootstrapper에서 null 체크 후 호출하므로 현행 적절. 주석 보강만으로 충분.

### SUGG-07: ComplexityEvaluator -- 실행 가능 코드 없이 문서화 전용 클래스

- **파일**: `Scripts/Utils/ComplexityEvaluator.cs`
- **문제**: 이 클래스의 메서드들은 문자열을 반환할 뿐 실제로 어디서도 호출되지 않는다. 코드 내 문서화 목적이라면 주석이나 README로 이동하는 것이 더 적절하다.
- **수정안**: 학습 자료로서 코드 내 배치가 의도적이라면 현행 유지. 대안으로 README의 "학습 포인트" 섹션에 통합.

---

## 잘한 점

1. **일관된 MVP 분리**: Step 2부터 Step 8까지 Model(C# event), View(MonoBehaviour + UIDocument), Presenter(Pure C# + IDisposable) 패턴이 일관되게 유지된다. 특히 Presenter가 MonoBehaviour가 아닌 순수 C# 클래스라는 원칙이 모든 파일에서 지켜졌다.

2. **named method 이벤트 등록/해제**: 모든 View에서 `clicked += HandleXxx` / `clicked -= HandleXxx` 패턴을 사용하며, 익명 람다가 단 한 곳도 없다. OnDisable에서의 해제도 빠짐없이 수행된다.

3. **Presenter.Dispose()에서 완전한 구독 해제**: 모든 Presenter가 IDisposable을 구현하고, Dispose에서 View/Model 이벤트를 하나도 빠뜨리지 않고 해제한다. Bootstrapper의 OnDestroy에서 `_presenter?.Dispose()` 호출도 일관적이다.

4. **OnEnable에서 UI 요소 캐싱**: 모든 View가 `OnEnable()`에서 `rootVisualElement.Q<T>()`를 수행한다. Awake에서 접근하는 실수가 전혀 없다.

5. **UniTask 패턴의 모범적 사용**: `UniTaskCompletionSource<bool>` 다이얼로그, `CancellationTokenRegistration.Dispose()` (ConfirmDialogView:76), `CancellationTokenSource.CreateLinkedTokenSource` (DialogDemoPresenter:23), `async UniTaskVoid` (async void 없음) 등 모든 UniTask 패턴이 올바르다.

6. **DOTween 시퀀스 Kill 처리**: AnimatedPanelView, StaggerListView 모두 `OnDestroy()`에서 `_sequence?.Kill()` 호출, 새 시퀀스 시작 전 기존 시퀀스 Kill 처리가 완벽하다.

7. **CSS Transition과 DOTween의 역할 분리**: USS에서 hover/active 상태 전환, C# DOTween에서 Sequence/Stagger 처리라는 이원 체계가 명확히 지켜진다.

8. **ListView userData 캐싱 패턴**: InventoryView에서 `makeItem`에서 `ItemViewCache` 객체를 `userData`에 저장하고 `bindItem`에서 캐스팅하여 `Q<T>()` 반복 호출을 방지한다. BuildMenuView에서도 카드 인덱스를 `userData`에 저장하는 동일 패턴을 사용한다.

9. **SettingsModel의 Clone/ApplyFrom 패턴**: 편집 취소 지원을 위한 Clone-Edit-Apply 패턴과 `SetValueWithoutNotify`를 활용한 Cancel 복구가 교과서적이다.

10. **R3 부분 도입의 절제된 설계**: Step 1-7에서 R3를 일절 사용하지 않고, Step 8에서만 `Observable.FromEvent` + `Debounce`/`ThrottleFirst`를 도입하여 "C# event의 한계" vs "R3의 이점"을 직접 비교할 수 있게 한 교육적 설계가 훌륭하다.

11. **DisplayStyle.None/Flex 토글**: 모달, 로딩, 백드롭, 툴팁 모두 `style.display = DisplayStyle.None/Flex`로 show/hide를 처리한다.

12. **USS 품질**: 모든 USS 파일에서 CSS transition이 적절히 적용되고, 클래스 네이밍이 BEM-like(`build-card--affordable`, `hud-icon--gold`)으로 일관된다. `:hover`, `:active`, `:disabled` 의사 클래스도 올바르게 사용된다.

---

## Step별 평가

| Step | 주제 | 등급 | 비고 |
|------|------|------|------|
| 1 | ProfileCard (UXML/USS/VisualElement) | B+ | MVP 없이 View에서 로직 처리 -- 의도적이나 주석 보강 필요 (WARN-02) |
| 2 | ResourcePanel (Simple MVP) | A | MVP 분리 교과서적. 생성자 구독 시점만 개선 여지 (WARN-01) |
| 3 | Dialog + Loading (UniTask) | A- | UniTaskCompletionSource 패턴 완벽. 취소 시 Loading 미닫힘 (WARN-06) |
| 4 | Animation (DOTween + CSS) | A | CSS/DOTween 이원 체계 명확. DOTween.To 래퍼도 잘 구현됨. 클로저 이슈 소폭 (WARN-08) |
| 5 | BuildingDetail (AI Workflow) | A- | 팝업 패턴 깔끔. BuildingData setter public 이슈 (WARN-04) |
| 6 | Game UI (HUD + BuildMenu + Tooltip) | A | 3개 View + 2개 Presenter 조합이 자연스러움. 툴팁 클램핑 구현 우수 |
| 7 | Inventory + Settings | A | ListView 가상화 + userData 캐싱 + Clone/Apply 모두 모범적 |
| 8 | R3 Partial (Debounce + ThrottleFirst) | A | C# Event vs R3 비교 설계 탁월. CompositeDisposable 해제 완벽 |

---

## 학습 포인트

1. **UI Toolkit의 VisualElement 쿼리는 반드시 OnEnable에서**: UIDocument의 `rootVisualElement`는 Awake 시점에 null일 수 있다. 이 프로젝트는 모든 View에서 이 규칙을 완벽히 지킨다.

2. **named method + OnDisable 해제 = 메모리 누수 제로**: 익명 람다 대신 named method를 사용하면 `clicked -= HandleXxx`로 정확히 해제할 수 있다. 이 프로젝트의 가장 큰 강점이다.

3. **Presenter를 MonoBehaviour로 만들지 않는 이유**: Pure C# 클래스로 만들면 Unity 생명주기에 의존하지 않아 테스트 가능성이 높아지고, IDisposable로 명시적 정리가 가능하다.

4. **DOTween.To() getter/setter 패턴**: UI Toolkit의 VisualElement에는 DOTween 확장이 내장되어 있지 않다. `DOTween.To(() => resolvedStyle.X, x => style.X = x, ...)` 패턴이 유일한 방법이며, 이 프로젝트의 `VisualElementTweenExtensions`가 좋은 레퍼런스이다.

5. **R3는 "필요할 때만" 도입하라**: Step 1-7의 모든 기능이 C# event만으로 구현 가능하다. R3가 필요해지는 정확한 시점(Debounce, ThrottleFirst)을 Step 8에서 체험함으로써, 불필요한 의존성 추가를 방지하는 판단력을 기를 수 있다.

6. **UniTaskCompletionSource로 다이얼로그를 await 가능한 API로 만드는 기법**: `await dialog.ShowAsync()` 패턴은 콜백 지옥 없이 동기적 흐름으로 다이얼로그 결과를 처리할 수 있다. CancellationToken 연동이 핵심이다.

7. **ListView 가상화 + userData 캐싱**: 1000개 아이템도 `makeItem`/`bindItem`으로 가시 영역만 렌더링한다. `userData`에 캐시 객체를 저장하면 `bindItem`마다 `Q<T>()`를 호출하지 않아 성능이 향상된다.

8. **CSS Transition vs DOTween 역할 분리 기준**: 상태 전환(hover, active, disabled)은 USS transition으로, 명령형 시퀀스(show/hide, stagger, elastic)는 DOTween으로. 이 경계를 지키면 코드량이 크게 줄고 유지보수가 쉬워진다.
