# UGUI World Space UI 패턴 리서치

- **작성일**: 2026-03-28
- **카테고리**: pattern / practice
- **상태**: 조사완료

---

## 1. 요약

World Space UI는 HP 바, 상호작용 프롬프트, 이름 플레이트 등 3D 공간에 붙는 UI 요소의 표준 구현 방식이다. Screen Space Overlay 대비 씬과의 통합감이 높으나 Canvas 하나당 드로우콜 오버헤드, 카메라 참조 캐싱, 빌보딩 타이밍(LateUpdate) 등 주의점이 있다. R3 ReactiveProperty + VContainer DI 조합으로 모델 데이터 변경을 UI에 반응적으로 바인딩하면 Update 폴링 없이 깔끔한 아키텍처를 유지할 수 있다.

---

## 2. 상세 분석

### 2.1 World Space Canvas — 렌더 모드 비교

| 항목 | Screen Space Overlay | Screen Space Camera | World Space |
|---|---|---|---|
| 위치 | 화면 최상단 고정 | 카메라 앞 평면 | 씬 3D 좌표 |
| 스케일 단위 | 픽셀 | 픽셀 | 월드 유닛 |
| 다른 3D 오브젝트와 교차 | 불가 | 불가 | 가능 |
| 깊이 소팅 | 항상 최상위 | Canvas Order | Sorting Layer + 카메라 거리 |
| 이벤트 카메라 | 불필요 | 자동 | **필수 할당** |
| 주 사용처 | HUD, 팝업 | 미니맵 오버레이 | HP바, 이름 플레이트, 상호작용 프롬프트 |

**World Space Canvas 핵심 설정 절차:**

1. Canvas 컴포넌트 → Render Mode: **World Space**
2. Event Camera 필드에 Main Camera 할당 (비워두면 `Camera.main`을 매 프레임 7~10회 호출하는 버그)
3. Rect Transform Width/Height를 실제 월드 크기로 설정 (예: 2.0 x 0.25 → 2m x 0.25m 바)
4. Transform Scale 조정: Canvas는 기본 100px/unit이라 `0.01` 스케일이 1m 기준에서 자연스럽다
5. Sorting Layer / Order in Layer 설정 (여러 World Canvas 간 소팅 제어)

```csharp
// Event Camera 런타임 할당 (Prefab이 씬 캐싱 불가능한 경우)
[SerializeField] Canvas _canvas;

void Awake()
{
    // Camera.main 캐싱 — World Space Canvas에서 가장 중요한 최적화
    _canvas.worldCamera = Camera.main;
}
```

**Sorting Order 전략:**

- 같은 Sorting Layer 내에서는 카메라와의 Z-거리로 자동 소팅
- 여러 Canvas를 동일 Layer에 두고 Order in Layer로 우선순위 제어
- Screen Space Overlay Canvas는 World Space Canvas 항상 위에 렌더링

---

### 2.2 빌보딩 (Billboarding)

World Space UI가 항상 카메라를 향하도록 회전시키는 기법. 세 가지 방식이 있다.

#### 방법 A: Rotation 복사 (가장 간단)

```csharp
// WorldSpaceBillboard.cs
public class WorldSpaceBillboard : MonoBehaviour
{
    Camera _camera;

    void Awake() => _camera = Camera.main;

    void LateUpdate()
    {
        // 카메라 회전을 그대로 복사 — 모든 빌보드가 동일 방향
        transform.rotation = _camera.transform.rotation;
    }
}
```

- 장점: 구현 단순, 모든 빌보드가 일관된 방향
- 단점: 카메라가 위/아래를 볼 때 텍스트가 기울어짐

#### 방법 B: LookAt (원근 효과)

```csharp
void LateUpdate()
{
    // 카메라 위치를 바라보되, 카메라 up 벡터 유지
    transform.LookAt(
        transform.position + _camera.transform.rotation * Vector3.forward,
        _camera.transform.rotation * Vector3.up
    );
}
```

- 장점: 원근 depth 효과 유지, 멀리 있는 오브젝트가 자연스럽게 보임
- 단점: 인접 오브젝트에서 약간의 왜곡 발생

#### 방법 C: 원통형 빌보딩 (Y축 고정)

```csharp
void LateUpdate()
{
    // X/Z만 회전, Y는 고정 — 키 큰 나무, NPC 이름 플레이트에 적합
    Vector3 euler = _camera.transform.eulerAngles;
    euler.x = 0f;
    euler.z = 0f;
    transform.eulerAngles = euler;
}
```

- 장점: 수직 오브젝트(나무, NPC)에서 자연스러움
- 단점: 카메라가 극단적인 앙각/부감일 때 어색함

#### LateUpdate 사용 이유

카메라 이동은 보통 Update나 FixedUpdate에서 처리된다. `LateUpdate`에서 빌보딩을 처리하면 카메라가 이미 이동을 완료한 뒤 UI가 회전하므로 지터(jitter)가 발생하지 않는다.

#### 빌보딩 최적화

- `Update` 대신 `LateUpdate` 사용 (카메라 이동 완료 후 실행)
- 카메라 참조를 `Awake`에서 캐싱 (`Camera.main`은 `FindObjectOfType`와 동일 비용)
- 거리 기반 LOD: 일정 거리 이상에서 빌보딩 비활성화

---

### 2.3 적 HP 바 — World Space로 3D 오브젝트 따라가기

#### 패턴 A: Canvas를 적 프리팹의 자식으로

```
Enemy (GameObject)
  └── HPBarCanvas (World Space Canvas)
        └── HPBarPanel
              ├── Background (Image)
              └── Fill (Image, Filled, Horizontal)
```

장점: 위치 동기화 코드 불필요, 프리팹 자기완결적
단점: 적 수만큼 Canvas가 생성 → 드로우콜 증가

```csharp
// HPBarView.cs — Canvas가 자식인 경우
public class HPBarView : MonoBehaviour
{
    [SerializeField] Image _fillImage;
    [SerializeField] Canvas _canvas;

    Camera _camera;
    CompositeDisposable _disposables = new();

    // VContainer로 주입
    IEnemyHealthModel _model;

    [Inject]
    public void Construct(IEnemyHealthModel model, Camera mainCamera)
    {
        _model = model;
        _camera = mainCamera;
        _canvas.worldCamera = _camera; // Event Camera 캐싱
    }

    void Start()
    {
        // R3 ReactiveProperty 바인딩 — Update 폴링 없음
        _model.CurrentHp
            .Subscribe(hp => _fillImage.fillAmount = hp / _model.MaxHp)
            .AddTo(_disposables);

        // HP가 0이면 숨기기
        _model.CurrentHp
            .Select(hp => hp > 0f)
            .DistinctUntilChanged()
            .Subscribe(alive => gameObject.SetActive(alive))
            .AddTo(_disposables);
    }

    void OnDestroy() => _disposables.Dispose();
}
```

#### 패턴 B: GPU Instancing으로 단일 드로우콜

Canvas 없이 MeshRenderer + 커스텀 셰이더 + MaterialPropertyBlock 사용:

```csharp
// 단일 드로우콜 HP 바 — stevestreeting.com 기법
public class InstancedHPBar : MonoBehaviour
{
    static readonly int FillId = Shader.PropertyToID("_Fill");

    [SerializeField] MeshRenderer _renderer;
    MaterialPropertyBlock _block;

    void Awake() => _block = new MaterialPropertyBlock();

    public void SetFill(float normalized) // 0~1
    {
        _renderer.GetPropertyBlock(_block);
        _block.SetFloat(FillId, normalized);
        _renderer.SetPropertyBlock(_block);
    }
}
```

셰이더 핵심 (ShaderLab):

```hlsl
#pragma multi_compile_instancing
UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(float, _Fill)
UNITY_INSTANCING_BUFFER_END(Props)

// Fragment: UV.x < _Fill 이면 초록, 아니면 빨강
```

머티리얼에서 **Enable GPU Instancing** 체크 필수.

#### 거리 기반 스케일링

카메라와 가까울수록 커지는 것을 방지:

```csharp
void LateUpdate()
{
    float dist = Vector3.Distance(_camera.transform.position, transform.position);
    float scale = dist * _scalePerUnit; // 예: 0.1
    transform.localScale = Vector3.one * scale;
}
```

또는 CanvasScaler의 `Constant World Size` 모드를 활용하면 스크립트 없이 일정 월드 크기 유지.

---

### 2.4 Screen Space Overlay 방식 (WorldToScreenPoint)

World Space Canvas를 쓰지 않고, Screen Space Overlay Canvas에 UI를 놓고 3D 오브젝트의 화면 좌표로 이동시키는 방식.

```csharp
// ScreenSpaceFollower.cs
public class ScreenSpaceFollower : MonoBehaviour
{
    [SerializeField] RectTransform _uiElement;
    [SerializeField] Transform _worldTarget;  // 따라갈 3D 오브젝트
    [SerializeField] Vector3 _worldOffset = new(0, 2f, 0); // 머리 위 오프셋

    Camera _camera;
    Canvas _rootCanvas;

    void Awake()
    {
        _camera = Camera.main;
        _rootCanvas = _uiElement.GetComponentInParent<Canvas>();
    }

    void LateUpdate()
    {
        Vector3 worldPos = _worldTarget.position + _worldOffset;
        Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);

        // 카메라 뒤에 있으면 숨기기
        if (screenPos.z < 0f)
        {
            _uiElement.gameObject.SetActive(false);
            return;
        }
        _uiElement.gameObject.SetActive(true);

        // Screen Space → Canvas Anchored Position 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.GetComponent<RectTransform>(),
                screenPos,
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _camera,
                out Vector2 localPoint))
        {
            _uiElement.anchoredPosition = localPoint;
        }
    }
}
```

또는 확장 메서드 활용 (FlaShG 헬퍼):

```csharp
// Canvas 확장 — Overlay, Camera 모드 모두 지원
_uiElement.anchoredPosition = _rootCanvas.WorldToCanvasPosition(worldTarget.position + offset, _camera);
```

**Screen Space vs World Space 선택 기준:**

| 상황 | 권장 방식 |
|---|---|
| 적 수가 많고 각각 HP 바 필요 | Screen Space (드로우콜 절약) |
| HP 바가 벽에 가려져야 함 | World Space |
| VR/XR 환경 | World Space 필수 |
| UI 요소가 씬과 depth 교차 필요 | World Space |
| 구현 단순성 우선 | Screen Space |

---

### 2.5 상호작용 프롬프트 ("E 키를 눌러 상호작용")

#### 구조 설계

```
InteractableObject (GameObject)
  ├── SphereCollider (Trigger, radius: 3m)
  └── PromptCanvas (World Space Canvas, scale: 0.01)
        └── PromptPanel
              ├── KeyIcon (Image)
              └── ActionText (TMP)
```

#### VContainer + R3 패턴

```csharp
// InteractableModel.cs
public class InteractableModel : IDisposable
{
    public ReactiveProperty<bool> IsPlayerNear { get; } = new(false);
    public ReactiveProperty<string> ActionText { get; } = new("상호작용");

    public void Dispose() => IsPlayerNear.Dispose();
}

// InteractableView.cs
public class InteractableView : MonoBehaviour
{
    [SerializeField] GameObject _promptRoot;
    [SerializeField] TMP_Text _actionText;
    [SerializeField] WorldSpaceBillboard _billboard; // 2.2 참조

    CompositeDisposable _disposables = new();

    [Inject]
    public void Construct(InteractableModel model)
    {
        model.IsPlayerNear
            .DistinctUntilChanged()
            .Subscribe(near => _promptRoot.SetActive(near))
            .AddTo(_disposables);

        model.ActionText
            .Subscribe(txt => _actionText.text = txt)
            .AddTo(_disposables);
    }

    void OnDestroy() => _disposables.Dispose();
}

// InteractablePresenter.cs
public class InteractablePresenter : IDisposable
{
    readonly InteractableModel _model;
    readonly float _interactRange;

    CompositeDisposable _disposables = new();

    public InteractablePresenter(InteractableModel model, float interactRange = 3f)
    {
        _model = model;
        _interactRange = interactRange;
    }

    // PlayerController가 Observable<Vector3>으로 위치를 노출한다고 가정
    public void BindPlayerPosition(Observable<Vector3> playerPosition, Vector3 objectPosition)
    {
        playerPosition
            .Select(pos => Vector3.Distance(pos, objectPosition) <= _interactRange)
            .DistinctUntilChanged()
            .Subscribe(near => _model.IsPlayerNear.Value = near)
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
```

**Trigger 기반 대안 (간단한 경우):**

```csharp
// 간단한 경우: Trigger 방식
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
        _promptRoot.SetActive(true);
}

void OnTriggerExit(Collider other)
{
    if (other.CompareTag("Player"))
        _promptRoot.SetActive(false);
}
```

**카메라 레이어링으로 오브젝트에 가려지지 않도록:**

별도 "Overlay" 카메라를 Main Camera Stack에 추가하고 해당 카메라의 Culling Mask를 UI 레이어만으로 제한하면 프롬프트가 3D 오브젝트 앞에 항상 렌더링된다.

---

### 2.6 플로팅 전투 텍스트 (Floating Combat Text)

#### 오브젝트 풀 기반 구현

```csharp
// FloatingTextPool.cs
public class FloatingTextPool : MonoBehaviour
{
    [SerializeField] FloatingTextView _prefab;
    [SerializeField] int _initialSize = 20;

    readonly Queue<FloatingTextView> _pool = new();

    void Awake()
    {
        for (int i = 0; i < _initialSize; i++)
            _pool.Enqueue(CreateNew());
    }

    FloatingTextView CreateNew()
    {
        var t = Instantiate(_prefab, transform);
        t.gameObject.SetActive(false);
        t.OnComplete += Return;
        return t;
    }

    public void Spawn(string text, Vector3 worldPos, Color color)
    {
        var t = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        t.transform.position = worldPos + Random.insideUnitSphere * 0.3f; // 랜덤 오프셋
        t.transform.rotation = Camera.main.transform.rotation;             // 빌보딩
        t.Play(text, color);
    }

    void Return(FloatingTextView t)
    {
        t.gameObject.SetActive(false);
        _pool.Enqueue(t);
    }
}

// FloatingTextView.cs
public class FloatingTextView : MonoBehaviour
{
    [SerializeField] TMP_Text _text;

    public event Action<FloatingTextView> OnComplete;

    public void Play(string text, Color color)
    {
        gameObject.SetActive(true);
        _text.text = text;
        _text.color = color;

        // DOTween 애니메이션: 위로 이동 + 페이드 아웃
        DOTween.Kill(this);
        transform.localScale = Vector3.one;

        var seq = DOTween.Sequence().SetTarget(this);
        seq.Append(transform.DOMoveY(transform.position.y + 1.5f, 1f).SetEase(Ease.OutCubic));
        seq.Join(_text.DOFade(0f, 0.8f).SetDelay(0.2f));
        seq.OnComplete(() => OnComplete?.Invoke(this));
    }
}
```

**텍스트 색상 관례:**

| 유형 | 색상 |
|---|---|
| 일반 데미지 | 흰색 / 노란색 |
| 치명타 | 주황색 / 빨간색 |
| 힐 | 초록색 |
| 방어 흡수 | 회색 |
| 버프 | 파란색 |

**TextMeshPro vs TextMeshProUGUI:**

월드 스페이스에서 Canvas 없이 직접 배치할 때는 `TextMeshPro`(MeshRenderer 기반)를 사용하면 Canvas 오버헤드가 없다. Canvas 기반 UI와 함께 사용할 때만 `TextMeshProUGUI`.

---

### 2.7 네임 플레이트 (Name Plate)

```
NPC (GameObject)
  └── NamePlateCanvas (World Space Canvas)
        └── NamePlatePanel
              ├── Background (Image, 9-Sliced)
              ├── NameText (TMP, Rich Text 지원)
              └── TitleText (TMP, 선택)
```

```csharp
// NamePlateView.cs
public class NamePlateView : MonoBehaviour
{
    [SerializeField] TMP_Text _nameText;
    [SerializeField] Image _backgroundImage;

    static readonly Dictionary<TeamType, Color> TeamColors = new()
    {
        { TeamType.Player,  new Color(0.2f, 0.6f, 1.0f) }, // 파랑
        { TeamType.Ally,    new Color(0.2f, 0.8f, 0.2f) }, // 초록
        { TeamType.Enemy,   new Color(1.0f, 0.3f, 0.3f) }, // 빨강
        { TeamType.Neutral, new Color(0.8f, 0.8f, 0.8f) }, // 회색
    };

    CompositeDisposable _disposables = new();

    [Inject]
    public void Construct(ICharacterModel model)
    {
        model.DisplayName
            .Subscribe(name => _nameText.text = name)
            .AddTo(_disposables);

        model.Team
            .Subscribe(team =>
            {
                if (TeamColors.TryGetValue(team, out Color c))
                    _backgroundImage.color = c;
            })
            .AddTo(_disposables);
    }

    void OnDestroy() => _disposables.Dispose();
}
```

**멀티플레이어에서 로컬/원격 플레이어 구분:**

```csharp
// 로컬 플레이어의 네임 플레이트는 숨기는 패턴
model.IsLocalPlayer
    .Subscribe(isLocal => gameObject.SetActive(!isLocal))
    .AddTo(_disposables);
```

---

### 2.8 성능 최적화

#### Canvas 분리 전략

```
모든 적에 대해 개별 Canvas 생성 (BAD)
→ 적 100명 = Canvas 100개 = 드로우콜 100+

대안 A: 모든 월드 UI를 하나의 공유 Canvas에 배치 (GOOD for small counts)
대안 B: GPU Instancing + 셰이더 (GOOD for 100+ enemies)
대안 C: Screen Space Overlay Canvas 1개 + WorldToScreenPoint (BEST for performance)
```

#### Canvas 비활성화 (vs GameObject 비활성화)

```csharp
// 잘못된 방법: 비싼 OnDisable/OnEnable 콜백 발생
gameObject.SetActive(false); // BAD for frequent toggling

// 올바른 방법: 버텍스 버퍼 유지, 드로우콜만 중단
_canvas.enabled = false; // GOOD
```

#### 화면 밖 컬링

```csharp
// GeometryUtility를 사용한 Frustum Culling
public class WorldUIFrustumCuller : MonoBehaviour
{
    [SerializeField] Canvas _canvas;
    [SerializeField] Renderer _trackedRenderer; // 또는 Collider bounds 사용

    Camera _camera;
    Plane[] _frustumPlanes = new Plane[6];

    void Awake() => _camera = Camera.main;

    void LateUpdate()
    {
        GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);
        bool visible = GeometryUtility.TestPlanesAABB(_frustumPlanes, _trackedRenderer.bounds);
        _canvas.enabled = visible;
    }
}
```

#### 오브젝트 풀링 순서

```csharp
// 잘못된 순서: 계층 구조를 두 번 dirty하게 만듦
gameObject.SetActive(false);        // dirty 1회
transform.SetParent(_poolRoot);     // dirty 2회

// 올바른 순서: dirty 최소화
transform.SetParent(_poolRoot);     // reparent 먼저
// 데이터 업데이트
gameObject.SetActive(false);        // 마지막에 비활성화
```

#### Camera.main 캐싱 — World Space Canvas의 핵심

World Space Canvas는 Event Camera가 null이면 매 프레임 `Camera.main`을 7~10회 호출한다. **반드시 Event Camera를 명시적으로 할당**하거나 캐싱 후 코드로 설정.

```csharp
// 씬 LifetimeScope에서 일괄 설정하는 패턴
public class WorldUIInitializer : IInitializable
{
    readonly Camera _camera;
    readonly Canvas[] _worldCanvases;

    public WorldUIInitializer(Camera camera, Canvas[] worldCanvases)
    {
        _camera = camera;
        _worldCanvases = worldCanvases;
    }

    public void Initialize()
    {
        foreach (var canvas in _worldCanvases)
            if (canvas.renderMode == RenderMode.WorldSpace)
                canvas.worldCamera = _camera;
    }
}
```

---

### 2.9 오클루전 — 벽 뒤 UI 숨기기

```csharp
// OcclusionChecker.cs — 적 HP 바가 벽 뒤에 있을 때 숨김
public class OcclusionChecker : MonoBehaviour
{
    [SerializeField] Canvas _canvas;
    [SerializeField] LayerMask _occluderMask;   // 벽, 지형 레이어
    [SerializeField] float _checkInterval = 0.1f; // 매 프레임 체크는 과도함

    Transform _target;  // 추적 대상 (적 Transform)
    Camera _camera;
    float _nextCheckTime;

    [Inject]
    public void Construct(Transform target, Camera cam)
    {
        _target = target;
        _camera = cam;
    }

    void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + _checkInterval;
        CheckOcclusion();
    }

    void CheckOcclusion()
    {
        Vector3 camPos = _camera.transform.position;
        Vector3 targetPos = _target.position + Vector3.up * 1.8f; // 머리 위 포인트
        Vector3 dir = targetPos - camPos;
        float dist = dir.magnitude;

        bool occluded = Physics.Raycast(camPos, dir.normalized, dist, _occluderMask);
        _canvas.enabled = !occluded;
    }
}
```

**성능 고려:**
- `_checkInterval`을 0.05~0.2초로 설정해 매 프레임 레이캐스트 회피
- 다수의 적에 대해 체크를 시간 분산 (FrameDistributer 패턴)
- 단순 거리 컬링 후 근거리에서만 오클루전 체크 수행

---

### 2.10 스케일링 전략 — 거리별 크기 유지

#### 옵션 A: 상수 월드 크기 (기본 World Space)

오브젝트와 같이 원근법 적용 → 멀어질수록 작게 보임. 자연스러운 느낌.

#### 옵션 B: 상수 화면 크기 (Constant Pixel Size 근사)

```csharp
// ConstantScreenSizeScaler.cs
public class ConstantScreenSizeScaler : MonoBehaviour
{
    [SerializeField] float _targetScreenHeight = 40f; // 픽셀 기준 목표 높이

    Camera _camera;
    float _baseHeight; // 오브젝트의 월드 공간 높이

    void Awake()
    {
        _camera = Camera.main;
        _baseHeight = GetComponent<RectTransform>()?.rect.height ?? 1f;
    }

    void LateUpdate()
    {
        float dist = Vector3.Distance(_camera.transform.position, transform.position);
        // 거리에 비례해 스케일 조정, 화면상 크기 일정하게 유지
        float worldSize = (_targetScreenHeight / Screen.height)
                          * (2f * dist * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
        float scale = worldSize / _baseHeight;
        transform.localScale = Vector3.one * scale;
    }
}
```

#### 옵션 C: 거리 기반 LOD

```csharp
// 거리별 UI 복잡도 축소
void LateUpdate()
{
    float dist = Vector3.Distance(_camera.transform.position, transform.position);

    if (dist > _hideDistance)
        _canvas.enabled = false;
    else if (dist > _simplifyDistance)
    {
        _canvas.enabled = true;
        _detailGroup.SetActive(false); // 상세 정보 숨김
    }
    else
    {
        _canvas.enabled = true;
        _detailGroup.SetActive(true);  // 전체 표시
    }
}
```

---

## 3. 베스트 프랙티스

### DO (권장)

- [x] World Space Canvas의 Event Camera는 항상 명시적으로 할당 (`Camera.main` 자동참조 금지)
- [x] 빌보딩은 반드시 `LateUpdate()`에서 처리 (카메라 이동 완료 후 회전)
- [x] 카메라 참조는 `Awake()`에서 캐싱 후 재사용
- [x] `Canvas` 컴포넌트를 비활성화할 때는 `canvas.enabled = false` 사용 (gameObject.SetActive 대신)
- [x] 오브젝트 풀에서 꺼낼 때: reparent → 데이터 업데이트 → SetActive(true) 순서
- [x] HP 바 fill은 `Image.fillAmount` + Sprite 할당 필수 (Sprite 없으면 fillAmount 무효)
- [x] FloatingText는 오브젝트 풀 사용 (전투 중 다수 생성/파괴 방지)
- [x] R3 구독은 View의 `Start()`에서 (VContainer Construct/Awake는 R3 Subscribe 금지)
- [x] CompositeDisposable 사용 후 `OnDestroy()`에서 Dispose

### DON'T (금지)

- [ ] Event Camera 비워두기 (매 프레임 Camera.main 7~10회 호출 발생)
- [ ] `Update()`에서 빌보딩 처리 (LateUpdate 사용)
- [ ] World Space Canvas의 Animator 사용 (매 프레임 dirty 발생)
- [ ] 100+ 적에 개별 World Canvas 붙이기 (GPU Instancing 또는 Screen Space 고려)
- [ ] `Raycast Target` 끄지 않기 (클릭 필요 없는 HP 바의 모든 이미지에서 비활성화)
- [ ] VContainer `Construct()`에서 R3 `Subscribe()` 호출 (Awake 전 실행 → 데드락)

### CONSIDER (상황별)

- [ ] 적이 50명 이상: GPU Instancing 방식으로 HP 바 전환
- [ ] VR/XR 환경: WorldToScreenPoint 방식 대신 World Space Canvas 필수
- [ ] 오클루전이 중요한 경우: 레이캐스트 체크 + 체크 주기 분산 (0.1초)
- [ ] 이름 플레이트가 많은 경우: 일정 거리 이상에서 Canvas 비활성화
- [ ] 별도 Overlay 카메라 스택으로 UI가 항상 앞에 보이도록 설정 (상호작용 프롬프트)

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 6000.x (LTS) | World Space UI Toolkit은 6.x에서 개선됨 |
| UGUI (com.unity.ugui) | 2.0+ | World Space Canvas 완전 지원 |
| TextMeshPro | 3.2+ | Unity 패키지 동봉 |
| R3 | 1.x | ReactiveProperty 기반 바인딩 |
| VContainer | 1.15+ | LifetimeScope 자식 스코프 |
| DOTween | 1.2.745+ | DOTween.Sequence, SetTarget |
| New Input System | 1.7+ | UnityEngine.Input 사용 금지 |

---

## 5. 예제 코드

### 기본 사용법 — World Space HP 바 (자식 Canvas 방식)

```csharp
// EnemyHPBarPresenter.cs
public class EnemyHPBarPresenter : IInitializable, IDisposable
{
    readonly IEnemyHealthModel _health;
    readonly HPBarView _view;

    CompositeDisposable _disposables = new();

    public EnemyHPBarPresenter(IEnemyHealthModel health, HPBarView view)
    {
        _health = health;
        _view = view;
    }

    public void Initialize()
    {
        _health.CurrentHp
            .CombineLatest(_health.MaxHp, (cur, max) => max > 0 ? cur / max : 0f)
            .DistinctUntilChanged()
            .Subscribe(_view.SetFill)
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
```

### 고급 패턴 — LifetimeScope 자식 스코프로 적 단위 DI

```csharp
// EnemyLifetimeScope.cs (각 적 프리팹에 첨부)
public class EnemyLifetimeScope : LifetimeScope
{
    [SerializeField] HPBarView _hpBarView;
    [SerializeField] NamePlateView _namePlateView;

    protected override void Configure(IContainerBuilder builder)
    {
        // 이 적 인스턴스의 모델 등록
        builder.Register<EnemyHealthModel>(Lifetime.Scoped)
               .AsImplementedInterfaces();
        builder.Register<EnemyCharacterModel>(Lifetime.Scoped)
               .AsImplementedInterfaces();

        // View 등록 (씬에 있는 인스턴스)
        builder.RegisterComponent(_hpBarView);
        builder.RegisterComponent(_namePlateView);

        // Presenter 등록
        builder.RegisterEntryPoint<EnemyHPBarPresenter>();
        builder.RegisterEntryPoint<NamePlatePresenter>();
    }
}
```

### Screen Space 방식 — 대량 적 처리

```csharp
// MassEnemyHPBarManager.cs — 단일 Canvas에서 100+ 적 처리
public class MassEnemyHPBarManager : MonoBehaviour
{
    [SerializeField] Canvas _overlayCanvas;          // Screen Space Overlay Canvas 1개
    [SerializeField] HPBarWidgetView _hpBarPrefab;

    readonly Dictionary<int, HPBarWidgetView> _active = new();
    readonly Queue<HPBarWidgetView> _pool = new();

    Camera _camera;

    void Awake() => _camera = Camera.main;

    public HPBarWidgetView Register(int enemyId, Transform enemyTransform)
    {
        var bar = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_hpBarPrefab, _overlayCanvas.transform);
        bar.gameObject.SetActive(true);
        bar.BindTarget(enemyTransform, _camera, _overlayCanvas);
        _active[enemyId] = bar;
        return bar;
    }

    public void Unregister(int enemyId)
    {
        if (_active.Remove(enemyId, out var bar))
        {
            bar.gameObject.SetActive(false);
            _pool.Enqueue(bar);
        }
    }
}
```

---

## 6. UI_Study 적용 계획

### 예제 01: World Space Canvas 기초 설정

- World Space Canvas 설정 (Event Camera 할당, 스케일)
- 세 가지 Render Mode 시각적 비교 씬

### 예제 02: 빌보딩 3종 비교

- Rotation 복사 / LookAt / 원통형 빌보딩 나란히 배치
- LateUpdate 타이밍 효과 시각화 (Update와 비교)

### 예제 03: 적 HP 바 시스템

- Enemy 프리팹에 자식 Canvas HP 바
- R3 ReactiveProperty 바인딩 (EnemyHealthModel)
- VContainer EnemyLifetimeScope 자식 스코프

### 예제 04: 상호작용 프롬프트

- 거리 기반 프롬프트 등장/소멸
- R3 + VContainer 반응형 패턴
- Overlay 카메라 스택으로 항상 앞에 표시

### 예제 05: 플로팅 전투 텍스트

- 오브젝트 풀 기반 FloatingText
- DOTween 이동 + 페이드 애니메이션
- 데미지/힐/치명타 색상 구분

### 예제 06: 네임 플레이트

- 팀 색상 반응형 바인딩
- 거리 기반 LOD (가까이에서만 표시)
- TextMeshPro Rich Text 팀 색상

---

## 7. 참고 자료

1. [Unity 6 Manual — Create a World Space UI](https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/create-world-space-ui.html)
2. [Unity UGUI Package — Creating a World Space UI](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/HOWTO-UIWorldSpace.html)
3. [Billboards in Unity (Game Dev Beginner)](https://gamedevbeginner.com/billboards-in-unity-and-how-to-make-your-own/)
4. [Enemy Health Bars in 1 Draw Call — Steve Streeting](https://www.stevestreeting.com/2019/02/22/enemy-health-bars-in-1-draw-call-in-unity/)
5. [Unity Show Player How to Interact with In-World UI](https://medium.com/@gaetano.tonzuso/unity-show-the-player-how-to-interact-with-in-world-ui-1bae3cc6d9dc)
6. [Floating Combat Text — Wayline](https://www.wayline.io/blog/unity-floating-combat-text)
7. [Canvas Render Mode Explained — Chris Hilton](https://christopherhilton88.medium.com/canvas-render-mode-in-unity-screen-space-overlay-camera-and-world-space-1534e010c8c6)
8. [Unity FPSSample NamePlate.cs](https://github.com/Unity-Technologies/FPSSample/blob/master/Assets/Scripts/Game/Systems/NamePlate/NamePlate.cs)
9. [CanvasPositioningExtensions Helper — FlaShG](https://gist.github.com/FlaShG/ac3afac0ef65d98411401f2b4d8a43a5)
10. [Unity UI Optimization Tips (Official)](https://create.unity.com/Unity-UI-optimization-tips)
11. [GeometryUtility.CalculateFrustumPlanes (Unity Docs)](https://docs.unity3d.com/ScriptReference/GeometryUtility.CalculateFrustumPlanes.html)
12. [World Space UI — Sorting Order Discussions](https://discussions.unity.com/t/how-to-handle-sort-order-for-multiple-world-space-canvases/847211)
13. [Player Name on Top of Character — Medium](https://medium.com/geekculture/unity-ui-use-cases-1-player-name-on-top-of-the-character-281ae02fd253)
14. [World Space vs Screen Space UI — Unity Discussions](https://discussions.unity.com/t/floating-ui-health-bar-text-screen-space-ui-or-world-space-ui/943651)

---

## 8. 미해결 질문

- [ ] Unity 6 UI Toolkit의 World Space 패널이 UGUI World Canvas 대비 성능이 얼마나 차이나는가?
- [ ] GPU Instancing HP 바 셰이더의 URP/HDRP 포팅 방법 (SRP Batcher와의 호환성)
- [ ] 100+ 적에서 오클루전 레이캐스트 FrameDistributer 구체적 구현 패턴
- [ ] DOTween SetTarget이 VContainer 생명주기와 충돌하는 경우가 있는가?
- [ ] Screen Space Overlay 방식에서 멀티 카메라(씬 카메라 + UI 카메라) 구성 시 WorldToScreenPoint 보정 방법
