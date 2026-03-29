# UI Toolkit Simple MVP 패턴

- **작성일**: 2026-03-29
- **출처**: 12-UIToolkit-Lightweight (Step 2~8)
- **등급**: A- (리뷰 검증됨)

---

## 1. 개요

VContainer/R3 없이 순수 C# 이벤트와 Bootstrapper로 구현하는 경량 MVP 패턴.
UI Toolkit + UniTask + DOTween만으로 게임 UI를 만들 수 있는 최소 아키텍처.

**적합 조건**: 1~3인 팀, 화면 15개 이하, 복합 스트림 연산 불필요

---

## 2. 구조

```
Bootstrapper (MonoBehaviour)
  ├── Model       (Plain C#, C# event)
  ├── View        (MonoBehaviour + UIDocument)
  └── Presenter   (Pure C#, IDisposable)
```

### 데이터 흐름

```
사용자 입력 → View.event → Presenter → Model.method
Model.event → Presenter → View.display method
```

---

## 3. 코드 템플릿

### Model

```csharp
using System;

public class ExampleModel
{
    private int _value;
    public event Action<int> ValueChanged;

    public int Value
    {
        get => _value;
        private set { _value = value; ValueChanged?.Invoke(value); }
    }

    public void Increment(int amount) => Value += amount;
}
```

### View

```csharp
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ExampleView : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private Label _valueLabel;
    private Button _actionBtn;

    public event Action OnActionClicked;

    // named method — 익명 람다 금지
    private void HandleActionClicked() => OnActionClicked?.Invoke();

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _valueLabel = root.Q<Label>("value-label");
        _actionBtn  = root.Q<Button>("action-btn");
        _actionBtn.clicked += HandleActionClicked;
    }

    private void OnDisable()
    {
        if (_actionBtn != null) _actionBtn.clicked -= HandleActionClicked;
    }

    public void SetValue(int v) => _valueLabel.text = v.ToString();
}
```

### Presenter

```csharp
using System;

public class ExamplePresenter : IDisposable
{
    private readonly ExampleModel _model;
    private readonly ExampleView _view;

    public ExamplePresenter(ExampleModel model, ExampleView view)
    {
        _model = model;
        _view = view;

        _view.OnActionClicked += HandleAction;
        _model.ValueChanged   += _view.SetValue;
    }

    public void Initialize() => _view.SetValue(_model.Value);

    private void HandleAction() => _model.Increment(1);

    public void Dispose()
    {
        _view.OnActionClicked -= HandleAction;
        _model.ValueChanged   -= _view.SetValue;
    }
}
```

### Bootstrapper

```csharp
using UnityEngine;

public class ExampleBootstrapper : MonoBehaviour
{
    [SerializeField] private ExampleView _view;

    private ExampleModel _model;
    private ExamplePresenter _presenter;

    private void Start()
    {
        _model = new ExampleModel();
        _presenter = new ExamplePresenter(_model, _view);
        _presenter.Initialize();
    }

    private void OnDestroy() => _presenter?.Dispose();
}
```

---

## 4. 핵심 규칙

| 규칙 | 설명 |
|------|------|
| View에 로직 금지 | display 메서드 + C# event 노출만 |
| Model에 UI 금지 | C# event로만 변경 알림 |
| Presenter는 Pure C# | MonoBehaviour 상속 금지 |
| OnEnable에서 캐싱 | Awake에서 rootVisualElement 접근 금지 |
| named method 등록 | `clicked += NamedMethod` (익명 람다 해제 불가) |
| IDisposable 필수 | Presenter는 반드시 모든 구독 해제 |
| Bootstrapper가 조립 | DI 없이 수동 연결 |

---

## 5. 복잡도 임계점

| 조건 | 기본 스택 | R3 부분 도입 | VContainer 도입 |
|------|-----------|-------------|----------------|
| 화면 < 10 | ✅ | | |
| Debounce/Throttle 필요 | | ✅ | |
| 화면 10~15 | ✅ | Maybe | |
| 화면 15+ | | ✅ | ✅ |
| 복합 스트림 (Merge/Combine) | | ✅ | |
| 팀 3인+ | | Maybe | Maybe |

---

## 6. 실전 코드 위치

| 패턴 | 파일 |
|------|------|
| 기본 MVP | `ResourceModel.cs` + `ResourcePanelView.cs` + `ResourcePanelPresenter.cs` |
| async 다이얼로그 | `ConfirmDialogView.cs` (UniTaskCompletionSource) |
| DOTween + CSS | `VisualElementTweenExtensions.cs` + `AnimatedPanelView.cs` |
| ListView 가상화 | `InventoryView.cs` (makeItem/bindItem + userData) |
| R3 부분 도입 | `SearchPresenter_R3.cs` vs `SearchPresenter_CSharpEvent.cs` |
