# VContainer + R3 + UniTask — UI Toolkit 런타임 호환성 리서치

- **작성일**: 2026-03-29
- **카테고리**: integration
- **상태**: 조사완료

---

## 1. 요약

VContainer, R3, UniTask는 모두 UI Toolkit 런타임과 동작은 가능하지만, 전용 공식 통합은 존재하지 않는다.
VContainer는 Pure C# Presenter 패턴을 통해 UIDocument를 다루는 Presenter를 깔끔하게 주입할 수 있다.
R3는 UI Toolkit 전용 Observable 확장이 없어 RegisterCallback 래퍼를 수동으로 작성해야 한다.
UniTask의 UI Toolkit 확장 PR(#338)은 2022년 머지 거부(stale) 처리되어 현재 공식 지원이 없다.
결론적으로 현재(2026) UI_Study에서 UI Toolkit 전환은 시기상조이며 UGUI + 현재 스택 유지가 권장된다.

---

## 2. VContainer + UI Toolkit

### 2.1 핵심 문제

VContainer의 `RegisterComponent()`, `RegisterComponentInHierarchy<T>()` 는 **MonoBehaviour** 전용이다.
`VisualElement`는 MonoBehaviour가 아니므로 이 메서드를 사용할 수 없다.

### 2.2 동작하는 패턴: Pure C# Presenter + UIDocument

UI Toolkit View를 직접 주입하는 대신, `UIDocument`를 보유한 MonoBehaviour를 View로 사용하거나,
순수 C# Presenter가 `UIDocument.rootVisualElement`에 접근하는 패턴이 작동한다.

```csharp
// === UIDocumentView.cs (MonoBehaviour — View 역할) ===
public class UIDocumentView : MonoBehaviour
{
    UIDocument _doc;
    Label _goldLabel;
    Button _buildButton;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;
        _goldLabel = root.Q<Label>("gold-label");
        _buildButton = root.Q<Button>("build-button");
    }

    // Presenter에게 VisualElement 참조 노출 (패시브)
    public Label GoldLabel => _goldLabel;
    public Button BuildButton => _buildButton;
}

// === LifetimeScope ===
public class GameSceneLifetimeScope : LifetimeScope
{
    [SerializeField] UIDocumentView _hudView;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_hudView);           // MonoBehaviour 주입 가능
        builder.RegisterEntryPoint<HudPresenter>();
        builder.Register<ResourceSystem>(Lifetime.Singleton);
    }
}

// === HudPresenter.cs (Pure C# — Presenter 역할) ===
public class HudPresenter : IInitializable, IDisposable
{
    readonly ResourceSystem _model;
    readonly UIDocumentView _view;
    DisposableBag _bag;

    public HudPresenter(ResourceSystem model, UIDocumentView view)
    {
        _model = model;
        _view = view;
    }

    public void Initialize()
    {
        // R3 수동 래퍼로 Button.clicked 바인딩 (섹션 3.3 참조)
        _model.Gold
            .Subscribe(g => _view.GoldLabel.text = g.ToString())
            .AddTo(ref _bag);

        _view.BuildButton.clicked += OnBuildClicked;
    }

    void OnBuildClicked() => _model.Build();

    public void Dispose()
    {
        _view.BuildButton.clicked -= OnBuildClicked;
        _bag.Dispose();
    }
}
```

### 2.3 완전 Pure C# View 패턴 (UIDocument 없이)

VisualElement를 Pure C# 클래스 생성자에 직접 넘기는 방법도 가능하다.
단, VContainer가 VisualElement를 생성하거나 조회하는 것은 지원하지 않으므로 수동 팩토리가 필요하다.

```csharp
// UIDocument를 보유한 MonoBehaviour에서 VisualElement를 꺼내 팩토리에 전달
public class UIPresenterFactory
{
    readonly IObjectResolver _resolver;
    public UIPresenterFactory(IObjectResolver resolver) => _resolver = resolver;

    public HudPresenter Create(VisualElement root)
    {
        // VContainer로는 VisualElement 자체를 Register할 수 없으므로
        // 수동으로 생성하거나 별도 팩토리 메서드 사용
        var model = _resolver.Resolve<ResourceSystem>();
        return new HudPresenter(model, root);
    }
}
```

### 2.4 알려진 제한사항

- VContainer에 UI Toolkit 전용 통합 문서나 샘플이 존재하지 않음 (2026년 3월 기준)
- VisualElement 자체를 Container에 Register하는 공식 방법 없음
- `RegisterComponentInHierarchy<T>()` 는 MonoBehaviour만 탐색
- 커뮤니티에서 VContainer + UI Toolkit 조합 사례가 매우 드묾
- 참조: [VContainer GitHub](https://github.com/hadashiA/VContainer), [DeepWiki](https://deepwiki.com/hadashiA/VContainer)

---

## 3. R3 + UI Toolkit

### 3.1 공식 지원 현황

**R3에는 UI Toolkit 전용 확장 메서드가 없다** (2026년 3월 기준).
R3 README에서 "UIToolkit", "VisualElement", "Button.clicked" 언급 없음.
UniRx의 `Button.OnClickAsObservable()` 같은 편의 확장은 UGUI Button 대상이며,
UI Toolkit의 `UnityEngine.UIElements.Button`에는 적용되지 않는다.

### 3.2 UniTask PR #338 — 머지 거부됨

2022년 커뮤니티 기여자 Iblis가 UI Toolkit 확장 PR을 제출했으나,
유지보수자가 "UI Toolkit을 충분히 파악할 시간이 필요하다"며 6개월 후 stale 처리로 **미머지**됨.

제안된 API (실제 구현과 다를 수 있음):
```csharp
// PR #338에서 제안된 패턴 (공식 미채택)
button.OnClickAsAsyncEnumerable(CancellationToken.None)
slider.OnValueChangedAsAsyncEnumerable(CancellationToken.None)
    .BindTo(inputField, CancellationToken.None);
```

- 참조: [UniTask Issue #261](https://github.com/Cysharp/UniTask/issues/261), [UniTask PR #338](https://github.com/Cysharp/UniTask/pull/338)

### 3.3 수동 Observable 래퍼 작성 (현재 권장 방법)

```csharp
// UI Toolkit Button → R3 Observable 래퍼
public static class UIToolkitR3Extensions
{
    // Button.clicked → Observable<Unit>
    public static Observable<Unit> OnClickAsObservable(
        this UnityEngine.UIElements.Button button,
        CancellationToken ct = default)
    {
        return Observable.Create<Unit>(observer =>
        {
            void Handler() => observer.OnNext(Unit.Default);
            button.clicked += Handler;
            return Disposable.Create(() => button.clicked -= Handler);
        });
    }

    // RegisterCallback 기반 범용 래퍼
    public static Observable<TEvent> OnEventAsObservable<TEvent>(
        this VisualElement element,
        CancellationToken ct = default)
        where TEvent : EventBase<TEvent>, new()
    {
        return Observable.Create<TEvent>(observer =>
        {
            void Handler(TEvent e) => observer.OnNext(e);
            element.RegisterCallback<TEvent>(Handler);
            return Disposable.Create(() => element.UnregisterCallback<TEvent>(Handler));
        });
    }
}

// 사용 예시
button.OnClickAsObservable()
    .Throttle(TimeSpan.FromSeconds(0.5))
    .Subscribe(_ => OnBuildClicked())
    .AddTo(ref _bag);

textField.OnEventAsObservable<ChangeEvent<string>>()
    .Debounce(TimeSpan.FromMilliseconds(300))
    .Subscribe(e => OnSearchChanged(e.newValue))
    .AddTo(ref _bag);
```

### 3.4 ReactiveProperty → UI Toolkit 단방향 바인딩

```csharp
// ReactiveProperty를 UI Toolkit Label에 바인딩
_model.Gold
    .Subscribe(g => _goldLabel.text = $"Gold: {g:N0}")
    .AddTo(ref _bag);

// SerializableReactiveProperty → UI Toolkit (Inspector 노출 불필요 시 불필요)
// UI Toolkit 자체 data binding 시스템과 R3는 독립적으로 병행 사용 가능
```

---

## 4. UniTask + UI Toolkit

### 4.1 공식 지원 현황

UniTask에는 UI Toolkit 전용 확장이 **없다** (PR #338 미머지).
그러나 UniTask의 CancellationToken 시스템은 UI Toolkit과 조합 가능하다.

### 4.2 VisualElement 수명과 CancellationToken

UI Toolkit VisualElement는 MonoBehaviour가 아니므로 `GetCancellationTokenOnDestroy()`를 직접 호출할 수 없다.
대신 **UIDocument를 보유한 MonoBehaviour** 또는 **부모 GameObject**의 토큰을 활용한다.

```csharp
public class HudPresenter : IInitializable, IDisposable
{
    readonly UIDocumentView _view;  // MonoBehaviour
    readonly CancellationTokenSource _cts = new();

    public void Initialize()
    {
        // UIDocument MonoBehaviour의 destroyCancellationToken 사용
        var ct = _view.destroyCancellationToken;

        LoadDataAsync(ct).Forget();
    }

    async UniTaskVoid LoadDataAsync(CancellationToken ct)
    {
        var data = await FetchGameDataAsync(ct);
        if (ct.IsCancellationRequested) return;
        _view.GoldLabel.text = data.Gold.ToString();
    }

    public void Dispose() => _cts.Cancel();
}
```

### 4.3 AttachToPanelEvent / DetachFromPanelEvent 주의사항

UI Toolkit의 패널 이벤트를 수명 관리에 활용하려 할 때 알려진 문제점:

| 이벤트 | 문제 |
|--------|------|
| `AttachToPanelEvent` | 초기화 중 **여러 번 발생** — UniTask 시작 지점으로 부적합 |
| `DetachFromPanelEvent` | 패널 제거 전 발생 — CancellationTokenSource.Cancel() 호출 위치로 사용 가능 |

```csharp
// AttachToPanelEvent는 신뢰할 수 없으므로 피할 것
// 대신 IInitializable.Initialize() 또는 MonoBehaviour.Start()에서 시작

visualElement.RegisterCallback<DetachFromPanelEvent>(_ =>
{
    _cts.Cancel();  // 패널 제거 시 진행 중인 UniTask 취소
});
```

### 4.4 async 패턴 권장

```csharp
// 권장: Presenter에서 async 작업 관리, VisualElement 직접 조작
public async UniTask ShowDialogAsync(string message, CancellationToken ct)
{
    var dialogRoot = _uiDoc.rootVisualElement.Q<VisualElement>("dialog");
    dialogRoot.style.display = DisplayStyle.Flex;

    await UniTask.WaitUntil(() => _dialogClosed, cancellationToken: ct);

    dialogRoot.style.display = DisplayStyle.None;
}
```

---

## 5. Unity 6 UI Toolkit 내장 데이터 바인딩

### 5.1 현황

Unity 2023.2 (alpha)에서 런타임 바인딩 API 도입, Unity 6에서 안정화.
MVVM 패턴을 공식 지원하며, `[CreateProperty]` 어트리뷰트로 컴파일 타임 property bag 생성.

```csharp
// 데이터 소스 클래스
public class PlayerData : ScriptableObject
{
    [SerializeField, DontCreateProperty]
    int m_Health;

    [CreateProperty]
    public int Health
    {
        get => m_Health;
        set => m_Health = value;
    }
}

// C# 코드에서 바인딩 설정
label.SetBinding("text", new DataBinding
{
    dataSource = playerData,
    dataSourcePath = new PropertyPath(nameof(PlayerData.Health)),
    bindingMode = BindingMode.ToTarget,
});
```

### 5.2 내장 바인딩의 한계 (커뮤니티 피드백)

| 문제 | 내용 |
|------|------|
| **학습 곡선** | "docs를 읽었지만 전혀 이해 못함" — 커뮤니티 피드백 (2025.10) |
| **설정 복잡도** | INotifyBindablePropertyChanged + CreateProperty + DataBinding 조합 필요 |
| **인터페이스 제한** | 인터페이스에 `[CreateProperty]`가 있어도 구현체에 재선언 필요 |
| **성능 우려** | ScrollView 내 ~100개 요소에서 성능 저하 보고 (2025.02) |
| **내장 컴포넌트** | Unity 빌트인 컴포넌트에 대한 bindable property 지원 제한적 |
| **디버깅** | 바인딩 실패 시 오류 메시지가 불명확 |

- 참조: [Unity 6 Data Binding 문서](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/data-binding.html)
- 참조: [Runtime Bindings Performance 토론](https://discussions.unity.com/t/ui-toolkit-runtime-bindings-performance/1593988)

### 5.3 R3 ReactiveProperty vs 내장 바인딩 비교

| 기준 | R3 ReactiveProperty | UI Toolkit 내장 바인딩 |
|------|--------------------|-----------------------|
| 설정 복잡도 | 낮음 (Subscribe 1줄) | 높음 (DataBinding 객체 구성 필요) |
| 타입 안전성 | 강함 (제네릭) | 중간 (PropertyPath 문자열) |
| 양방향 지원 | 수동 구현 필요 | 기본 지원 (TwoWay 모드) |
| UGUI 호환 | 완벽 | 미지원 |
| UI Toolkit 호환 | 수동 래퍼 필요 | 기본 지원 |
| 디버깅 도구 | ObservableTracker | 없음 |
| 커뮤니티 성숙도 | 높음 | 낮음 (2023~ 도입) |
| R3 이벤트 체이닝 | 가능 | 불가 |

**결론**: R3가 현재 UGUI + 현재 스택에서 훨씬 성숙하고 유연. UI Toolkit 내장 바인딩은 UI Toolkit으로 완전 전환 시에만 고려.

---

## 6. UI Toolkit 런타임 프로덕션 준비 현황 (2026년 3월)

### 6.1 현재 상태 평가

| 기능 | 상태 |
|------|------|
| 기본 런타임 UI | 안정 |
| 데이터 바인딩 | 베타 수준 (복잡한 케이스에서 성능 문제) |
| 애니메이션 (CSS transitions) | 제한적 — DOTween, Timeline 미지원 |
| World Space UI | 공식 지원 부족 |
| 커스텀 셰이더/이펙트 | UGUI 대비 제한적 |
| Asset Store 에코시스템 | 미성숙 |
| VContainer/R3/UniTask 공식 통합 | 없음 |

### 6.2 커뮤니티 권고 (2025 기준)

- **새 프로젝트 & 데이터 집약적 UI** (관리 화면, 인벤토리 대량 아이템): UI Toolkit 고려
- **기존 UGUI 프로젝트**: 마이그레이션 비용 > 이득
- **복잡한 애니메이션 필요**: UGUI 유지
- **하이브리드**: UGUI HUD + UI Toolkit 정적 메뉴 — 공존 가능하나 복잡도 증가
- 참조: [UI Toolkit vs UGUI 2025 가이드](https://medium.com/@studio.angry.shark/unity-ui-toolkit-vs-ugui-2025-developer-guide-8407312c91ed)

---

## 7. 베스트 프랙티스

### DO (권장)

- UI Toolkit 사용 시 MonoBehaviour(UIDocument 보유)를 View로 두고 VContainer로 주입
- R3 Observable을 UI Toolkit 이벤트에 연결할 때 수동 래퍼(RegisterCallback 기반) 작성
- UniTask CancellationToken은 UIDocument MonoBehaviour의 `destroyCancellationToken` 활용
- DetachFromPanelEvent에서 CancellationTokenSource.Cancel() 호출로 정리

### DON'T (금지)

- VContainer `RegisterComponentInHierarchy<VisualElement>()` 시도 — 동작 안 함
- AttachToPanelEvent에서 UniTask 시작 — 중복 발생 문제
- UniTask PR #338 코드를 그대로 가져와 사용 — 공식 미채택, 유지보수 없음
- UI Toolkit 내장 바인딩을 복잡한 게임 루프 데이터에 적용 — 성능 미검증

### CONSIDER (상황별)

- UI Toolkit + VContainer 조합은 "UIDocument를 감싸는 MonoBehaviour View" 패턴으로 가능
- 현 UI_Study 스택(UGUI+R3+VContainer+UniTask)이 성숙하고 안정적이므로 UI Toolkit 전환은 신중히

---

## 8. 호환성 매트릭스

| 조합 | 호환성 | 비고 |
|------|--------|------|
| VContainer + UI Toolkit (MonoBehaviour View) | 가능 | UIDocument MonoBehaviour를 RegisterComponent |
| VContainer + UI Toolkit (Pure VisualElement) | 부분 가능 | 수동 팩토리 필요, 공식 지원 없음 |
| R3 + UI Toolkit Button | 가능 (수동) | 래퍼 작성 필요 |
| R3 + UI Toolkit 내장 바인딩 | 병행 가능 | 독립적 시스템, 중복 주의 |
| UniTask + UI Toolkit | 가능 (제한) | destroyCancellationToken 활용, PR #338 미머지 |
| DOTween + UI Toolkit | 불가 (직접) | VisualElement에 DOTween 적용 안 됨 |
| USN + UI Toolkit | 미검증 | USN은 UGUI 기반 설계 |

---

## 9. UI_Study 적용 계획

UI Toolkit 전환을 고려한다면:
- **적합 범위**: 09-Drag-And-Drop 이후 별도 챕터 (예: `10-UI-Toolkit-Intro`)
- **실험 예제**: UIDocument + Pure C# Presenter + 수동 R3 래퍼
- **비추천**: 기존 UGUI 예제를 UI Toolkit으로 마이그레이션 (학습 가치 낮음)
- **현실적 판단**: 현재 UGUI 학습을 완료 후 별도 브랜치에서 실험적으로 탐색

---

## 10. 참고 자료

1. [VContainer GitHub](https://github.com/hadashiA/VContainer)
2. [VContainer Entry Point 문서](https://vcontainer.hadashikick.jp/integrations/entrypoint)
3. [R3 GitHub](https://github.com/Cysharp/R3)
4. [UniTask Issue #261 — UI Toolkit 지원 요청](https://github.com/Cysharp/UniTask/issues/261)
5. [UniTask PR #338 — UI Toolkit 확장 (미머지)](https://github.com/Cysharp/UniTask/pull/338)
6. [Unity 6 Runtime Data Binding 문서](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/data-binding.html)
7. [UI Toolkit Runtime Bindings Performance 토론](https://discussions.unity.com/t/ui-toolkit-runtime-bindings-performance/1593988)
8. [UI Toolkit vs UGUI 2025 가이드](https://medium.com/@studio.angry.shark/unity-ui-toolkit-vs-ugui-2025-developer-guide-8407312c91ed)
9. [UI Toolkit Development Status November 2025](https://discussions.unity.com/t/ui-toolkit-development-status-and-next-milestones-november-2025/1698009)
10. [Introducing Runtime Bindings API — Unity Discussions](https://discussions.unity.com/t/introducing-the-runtime-bindings-api-in-unity-2023-2/921903)
11. [bustedbunny MVVM Toolkit for UI Toolkit](https://github.com/bustedbunny/com.bustedbunny.mvvmtoolkit)
12. [LibraStack UnityMvvmToolkit](https://github.com/LibraStack/UnityMvvmToolkit)
13. [Dependency Injection with UI Toolkit — Unity Discussions](https://discussions.unity.com/t/dependency-injection-with-ui-toolkit/812073)

## 11. 미해결 질문

- [ ] UI Toolkit DOTween 연동 — 커뮤니티 래퍼 존재 여부 조사 필요
- [ ] USN (UnityScreenNavigator) + UI Toolkit 공식 지원 계획 여부
- [ ] VContainer 작성자(hadashiA)의 UI Toolkit 지원 로드맵 여부
- [ ] Unity 6 내장 바인딩 + R3 병행 사용 시 성능 충돌 여부 실증 테스트
