# VContainer + UGUI 통합 리서치 리포트

- **작성일**: 2026-03-27
- **카테고리**: integration
- **상태**: 조사완료

---

## 1. 요약

VContainer v1.17.0 (2024.07)은 제로 할당 해석과 Zenject 대비 5-10배 속도 우위를 가진 Unity DI 프레임워크. UGUI 통합의 핵심은 Pure C# Presenter를 IInitializable/IDisposable 엔트리 포인트로 사용하고, View MonoBehaviour는 패시브하게 유지하는 것.

## 2. MonoBehaviour 주입 방법

### Method A: RegisterComponent (씬에 이미 존재)
```csharp
[SerializeField] ResourceBarView _view; // Inspector 할당

protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterComponent(_view);
    builder.RegisterEntryPoint<ResourceBarPresenter>();
}
```

### Method B: RegisterComponentInHierarchy (씬 검색)
```csharp
builder.RegisterComponentInHierarchy<HUDView>();
```

### Method C: IObjectResolver.Instantiate (런타임 프리팹)
```csharp
public class UIFactory
{
    readonly IObjectResolver _resolver;
    public UIFactory(IObjectResolver resolver) => _resolver = resolver;
    public BuildMenuView Create(BuildMenuView prefab, Transform parent)
        => _resolver.Instantiate(prefab, parent);
}
```

### Method D: InjectGameObject (외부 로드, Addressables)
```csharp
scope.Container.InjectGameObject(loadedGameObject);
```

## 3. 동적 자식 스코프 (팝업 패턴)

```csharp
public class PopupManager
{
    readonly LifetimeScope _parentScope;
    readonly BuildMenuLifetimeScope _popupPrefab;
    LifetimeScope _activePopup;

    public void Open()
    {
        _activePopup = _parentScope.CreateChildFromPrefab(_popupPrefab);
    }

    public void Close()
    {
        _activePopup.Dispose(); // GameObject 파괴 + Scoped 등록 해제
        _activePopup = null;
    }
}
```

## 4. 주입 타이밍 주의사항

VContainer 주입 순서: `Awake() → OnEnable() → [Inject]`

- OnEnable()에서 주입된 필드 접근 시 NullReferenceException
- resolver.Instantiate() 사용 시 PR #575에서 수정됨 (비활성화 후 주입 후 활성화)
- **안전한 곳**: Start(), IInitializable.Initialize(), IStartable.Start()

## 5. 참고 자료

1. [hadashiA/VContainer](https://github.com/hadashiA/VContainer)
2. [VContainer 공식 문서](https://vcontainer.hadashikick.jp/)
3. [Register MonoBehaviour](https://vcontainer.hadashikick.jp/registering/register-monobehaviour)
4. [Entry Point](https://vcontainer.hadashikick.jp/integrations/entrypoint)
