# UGUI 고급 HUD 패턴 — Radial Menu / Minimap / Loading Screen

- **작성일**: 2026-03-28
- **카테고리**: practice / pattern
- **상태**: 조사완료
- **관련 스택**: MV(R)P + VContainer + R3 + UniTask + DOTween

---

## 1. 요약

3가지 게임 HUD 고급 패턴의 구현 원리, 수학적 기반, 그리고 MV(R)P+VContainer+R3+UniTask 스택에 적합한 코드 패턴을 정리한다.

- **Radial/Pie Menu**: `Atan2`로 마우스/스틱 각도를 섹터 인덱스로 변환. `Image.Filled + Radial360`으로 wedge 렌더링. 각 wedge는 `fillAmount = 1/N`, `fillOrigin` + `rotation`으로 위치 결정.
- **Minimap / Compass**: RenderTexture 기반 orthographic 카메라가 UI RawImage에 직접 렌더링. 아이콘 추적은 world→minimap 좌표 변환 공식으로 처리. Compass는 RawImage.uvRect.x에 `player.eulerAngles.y / 360f`를 대입하는 UV scrolling 방식이 가장 단순하고 효과적.
- **Loading Screen**: `AsyncOperation.progress`는 0~0.9 범위. `allowSceneActivation = false`로 진행을 지연. Addressables 복수 핸들의 평균 퍼센트를 집계. UniTask `IProgress<float>` 패턴으로 진행 보고. Lerp로 진행 바 부드럽게 처리, 최소 표시 시간과 tips 사이클은 R3으로 관리.

---

## 2. Radial / Pie Menu

### 2.1 핵심 수학 — Atan2 각도 → 섹터 인덱스

래디얼 메뉴의 중심에서 마우스(또는 스틱)까지의 벡터를 각도로 변환한 뒤 섹터 인덱스로 매핑한다.

```
각도(도) = Atan2(delta.y, delta.x) * Rad2Deg
표준화된 각도 = (각도 + 360) % 360    // 음수 방지
섹터 인덱스 = floor(표준화된 각도 / (360 / N))
```

Unity에서는 UI 좌표계(y축 위쪽 양수)를 그대로 사용한다. 메뉴를 위쪽(12시)에서 시작하도록 하려면 각도에 `90f`를 더해 오프셋을 적용한다.

```csharp
// 중심에서 커서까지의 벡터를 섹터 인덱스로 변환
public static int AngleToSectorIndex(Vector2 delta, int sectorCount)
{
    if (delta.sqrMagnitude < 0.01f) return -1; // 데드존

    float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
    // 12시(위쪽)를 0으로 맞추기 위해 +90, 그리고 양수로 정규화
    float normalized = ((angle + 90f) % 360f + 360f) % 360f;
    float sectorAngle = 360f / sectorCount;
    return Mathf.FloorToInt(normalized / sectorAngle);
}
```

### 2.2 wedge 시각화 — Image.Filled + Radial360

각 wedge는 별도의 Image 컴포넌트이며, 다음 세 가지를 설정해 위치와 크기를 결정한다.

| 속성 | 설명 |
|---|---|
| `image.type = Image.Type.Filled` | Filled 모드 사용 |
| `image.fillMethod = Image.FillMethod.Radial360` | 360도 원형 채우기 |
| `image.fillAmount = 1f / sectorCount` | N분의 1만큼 채움 |
| `image.fillOrigin` | 채우기 시작 방향(0=Bottom, 1=Right, 2=Top, 3=Left) |
| `rectTransform.rotation` | wedge를 섹터 위치로 회전 |

**중요**: `Image.Filled`에서 `fillAmount`가 동작하려면 Sprite가 반드시 할당되어야 한다. Sprite 없이는 fillAmount가 무효화된다.

```csharp
void SetupWedge(Image wedgeImage, int index, int total)
{
    float sectorAngle = 360f / total;

    wedgeImage.type = Image.Type.Filled;
    wedgeImage.fillMethod = Image.FillMethod.Radial360;
    wedgeImage.fillAmount = 1f / total;
    // fillOrigin = Top(2)에서 시작, 각 섹터를 index * sectorAngle만큼 시계방향 회전
    wedgeImage.fillOrigin = 2; // Top
    wedgeImage.rectTransform.localRotation =
        Quaternion.Euler(0, 0, -sectorAngle * index);
}
```

**대안: 커스텀 Mesh 방식** (더 정밀한 히트 테스트가 필요할 때)

`Graphic`을 상속하고 `OnPopulateMesh(VertexHelper)`를 오버라이드하면 삼각형 부채꼴을 직접 생성할 수 있다. 이 방식은 `alphaHitTestMinimumThreshold`보다 더 정확한 raycast 경계를 제공하지만 구현 복잡도가 높다.

```csharp
protected override void OnPopulateMesh(VertexHelper vh)
{
    vh.Clear();
    float angleStart = startAngleDeg * Mathf.Deg2Rad;
    float angleEnd   = endAngleDeg   * Mathf.Deg2Rad;
    int steps = 20;

    // 중심 정점
    vh.AddVert(Vector3.zero, color, Vector2.zero);

    for (int i = 0; i <= steps; i++)
    {
        float t = (float)i / steps;
        float a = Mathf.Lerp(angleStart, angleEnd, t);
        Vector3 v = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        vh.AddVert(v, color, new Vector2(t, 1));
    }

    for (int i = 0; i < steps; i++)
        vh.AddTriangle(0, i + 1, i + 2);
}
```

### 2.3 선택 하이라이트 및 hover 피드백

두 가지 방식이 있다.

**방식 A — 색상 직접 변경 (간단)**

```csharp
void UpdateHighlight(int hoveredIndex)
{
    for (int i = 0; i < _wedges.Length; i++)
    {
        bool isHovered = i == hoveredIndex;
        _wedges[i].color = isHovered ? highlightColor : normalColor;
        _wedges[i].transform.localScale =
            Vector3.one * (isHovered ? 1.05f : 1f);
    }
}
```

**방식 B — DOTween 스케일/색상 애니메이션**

```csharp
void AnimateHover(int index, bool enter)
{
    var wedge = _wedges[index];
    float targetScale = enter ? 1.08f : 1f;
    Color targetColor = enter ? highlightColor : normalColor;

    wedge.transform.DOScale(targetScale, 0.12f).SetEase(Ease.OutBack);
    wedge.DOColor(targetColor, 0.1f);
}
```

### 2.4 입력 — New Input System + 게임패드 스틱

**마우스 입력 (New Input System)**

```csharp
using UnityEngine.InputSystem;

Vector2 GetMenuDelta()
{
    // 메뉴 중심의 스크린 좌표
    Vector2 center = RectTransformUtility.WorldToScreenPoint(
        null, _menuRoot.position);
    return Mouse.current.position.ReadValue() - center;
}
```

**게임패드 오른쪽 스틱**

```csharp
Vector2 GetStickDelta()
{
    var gamepad = Gamepad.current;
    if (gamepad == null) return Vector2.zero;
    return gamepad.rightStick.ReadValue();
}
```

**두 입력을 통합해 섹터 결정**

```csharp
void Update()
{
    Vector2 delta = _isGamepad ? GetStickDelta() : GetMenuDelta();
    int newIndex = AngleToSectorIndex(delta, _sectorCount);

    if (newIndex != _currentHoverIndex)
    {
        if (_currentHoverIndex >= 0) AnimateHover(_currentHoverIndex, false);
        _currentHoverIndex = newIndex;
        if (_currentHoverIndex >= 0) AnimateHover(_currentHoverIndex, true);
        // R3 스트림으로 Presenter에 전달
        _hoveredSectorSubject.OnNext(_currentHoverIndex);
    }
}
```

### 2.5 MV(R)P + R3 + VContainer 통합 패턴

```csharp
// --- Model ---
public class RadialMenuModel
{
    public ReactiveProperty<int> SelectedIndex { get; } = new(-1);
    public IReadOnlyList<RadialItemData> Items { get; }
    // ...
}

// --- View ---
public class RadialMenuView : MonoBehaviour
{
    [SerializeField] Image[] wedgeImages;
    [SerializeField] Image[] iconImages;

    // Presenter가 Subscribe할 스트림
    public Observable<int> OnHoveredIndex =>
        _hoveredSubject.AsObservable();
    public Observable<Unit> OnConfirmed =>
        _confirmedSubject.AsObservable();

    private Subject<int>  _hoveredSubject   = new();
    private Subject<Unit> _confirmedSubject = new();

    // Presenter가 호출하는 표시 메서드
    public void SetHighlight(int index, Color color)
    {
        for (int i = 0; i < wedgeImages.Length; i++)
            wedgeImages[i].color = i == index ? color : normalColor;
    }

    void Update()
    {
        Vector2 delta = GetInputDelta();
        int idx = AngleToSectorIndex(delta, wedgeImages.Length);
        _hoveredSubject.OnNext(idx);

        if (IsConfirmPressed())
            _confirmedSubject.OnNext(Unit.Default);
    }
}

// --- Presenter ---
public class RadialMenuPresenter : IStartable, IDisposable
{
    readonly RadialMenuView  _view;
    readonly RadialMenuModel _model;
    readonly CompositeDisposable _disposables = new();

    [Inject]
    public RadialMenuPresenter(RadialMenuView view, RadialMenuModel model)
    {
        _view  = view;
        _model = model;
    }

    public void Start()
    {
        _view.OnHoveredIndex
            .Subscribe(idx => {
                _model.SelectedIndex.Value = idx;
                _view.SetHighlight(idx, Color.yellow);
            })
            .AddTo(_disposables);

        _view.OnConfirmed
            .Where(_ => _model.SelectedIndex.Value >= 0)
            .Subscribe(_ => ExecuteItem(_model.SelectedIndex.Value))
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();

    void ExecuteItem(int index) { /* 아이템 실행 로직 */ }
}

// --- VContainer LifetimeScope ---
public class RadialMenuScope : LifetimeScope
{
    [SerializeField] RadialMenuView view;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(view);
        builder.Register<RadialMenuModel>(Lifetime.Scoped);
        builder.RegisterEntryPoint<RadialMenuPresenter>();
    }
}
```

### 2.6 오픈소스 레퍼런스

| 리포지토리 | 특징 | 링크 |
|---|---|---|
| EXP-Productions/RadialMenu-Unity | UGUI 전용, Unity package 포함 | https://github.com/EXP-Productions/RadialMenu-Unity |
| rito15/Unity-Radial-Menu | v1/v2/v3 점진적 개선, MIT | https://github.com/rito15/Unity-Radial-Menu |
| aillieo/UnityRadialLayoutGroup | LayoutGroup 기반, 각도/반지름 제어 | https://github.com/aillieo/UnityRadialLayoutGroup |
| psvantares/unity_circle_menu | iOS/Android/PC 다중 플랫폼 | https://github.com/psvantares/unity_circle_menu |
| AnnulusGames/ReactiveInputSystem | R3 기반 Input System 래핑 | https://github.com/AnnulusGames/ReactiveInputSystem |

---

## 3. Minimap / Compass HUD

### 3.1 아키텍처 개요

```
[Minimap Camera]  orthographic, 위에서 아래 바라봄
       |
       | Target Texture
       v
[RenderTexture]  (예: 256×256)
       |
       | texture
       v
[RawImage UI]    Canvas 위의 UI 요소 (마스크로 원형 클리핑 가능)
```

**핵심**: `Image`가 아닌 `RawImage`를 사용해야 한다. `Image`는 Sprite만 수용하며 RenderTexture를 직접 표시할 수 없다.

### 3.2 Minimap 카메라 설정

| 설정 | 값 | 이유 |
|---|---|---|
| Projection | Orthographic | 원근 왜곡 없는 정확한 탑뷰 |
| Rotation | (90, 0, 0) | 정면에서 아래를 봄 |
| Clear Flags | Solid Color / Depth only | 씬 배경 처리 방식 선택 |
| Culling Mask | Minimap (전용 레이어) | 불필요한 오브젝트 제외 |
| Target Texture | MinimapRenderTexture | 렌더 결과를 텍스처로 출력 |

**전용 레이어 방식**: 미니맵에 표시할 아이콘(플레이어, 적, 오브젝트)을 자식 quad/sprite로 만들고 "Minimap" 레이어로 지정한다. 카메라의 Culling Mask를 해당 레이어만으로 설정하면 실제 씬 오브젝트를 렌더링하지 않아 성능에 유리하다.

### 3.3 카메라 추적 — 플레이어 따라다니기

```csharp
public class MinimapCamera : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Camera minimapCam;
    [SerializeField] float heightOffset = 50f;

    void LateUpdate()
    {
        // XZ만 추적, Y는 고정 높이 유지
        Vector3 pos = player.position;
        pos.y = player.position.y + heightOffset;
        transform.position = pos;
    }
}
```

### 3.4 world 좌표 → minimap UI 좌표 변환

RenderTexture 기반에서는 카메라가 직접 렌더하므로 별도 변환 없이 3D 오브젝트가 미니맵에 자동 표시된다. 아이콘을 UI 레이어에서 직접 제어해야 할 때(예: 원형 클리핑 + 아이콘 표시)는 다음 공식을 사용한다.

```csharp
// 월드 위치 → 미니맵 RawImage 내 앵커 좌표
public Vector2 WorldToMinimapPosition(Vector3 worldPos,
    Transform player, float minimapWorldSize, RectTransform mapRect)
{
    // 플레이어 기준 상대 오프셋
    Vector3 offset = worldPos - player.position;

    // 미니맵이 커버하는 월드 크기(직경)로 정규화 → -0.5 ~ 0.5
    float normalizedX = offset.x / minimapWorldSize;
    float normalizedZ = offset.z / minimapWorldSize;

    // RawImage 내부 픽셀 좌표 (center = 0,0)
    float halfW = mapRect.rect.width  * 0.5f;
    float halfH = mapRect.rect.height * 0.5f;

    return new Vector2(normalizedX * mapRect.rect.width,
                       normalizedZ * mapRect.rect.height);
}
```

원형 미니맵에서 경계 밖 아이콘을 테두리에 클램프:

```csharp
Vector2 ClampToMinimapRadius(Vector2 pos, float radius)
{
    if (pos.magnitude > radius)
        return pos.normalized * radius;
    return pos;
}
```

### 3.5 아이콘 추적 — R3 기반 런타임 등록

```csharp
// MinimapIcon — 각 추적 대상에 붙이는 컴포넌트
public class MinimapIcon : MonoBehaviour
{
    [SerializeField] RectTransform iconRT;
    [SerializeField] Transform     trackedTarget;

    Subject<Unit> _onDestroy = new();
    public Observable<Unit> OnDestroyed => _onDestroy.AsObservable();

    void Update()
    {
        // MinimapPresenter가 계산한 anchoredPosition을 받아 적용
    }

    void OnDestroy() => _onDestroy.OnNext(Unit.Default);
}

// MinimapPresenter
public class MinimapPresenter : IStartable, IDisposable
{
    readonly MinimapView        _view;
    readonly List<MinimapIcon>  _icons = new();
    readonly CompositeDisposable _disposables = new();

    public void Start()
    {
        // 매 프레임 아이콘 위치 갱신 (R3 Observable.EveryUpdate 대신 UniTask loop)
        UpdateIconsLoop(_disposables.GetCancellationToken()).Forget();
    }

    async UniTaskVoid UpdateIconsLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var icon in _icons)
            {
                Vector2 uiPos = WorldToMinimapPosition(
                    icon.TrackedTarget.position,
                    _view.PlayerTransform,
                    _view.MinimapWorldSize,
                    _view.MapRect);
                uiPos = ClampToMinimapRadius(uiPos, _view.MapRadius);
                icon.SetPosition(uiPos);
            }
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, ct);
        }
    }

    public void Dispose() => _disposables.Dispose();
}
```

### 3.6 줌 레벨 (Orthographic Size 제어)

```csharp
public class MinimapZoom : MonoBehaviour
{
    [SerializeField] Camera minimapCam;
    [SerializeField] float minZoom  = 5f;
    [SerializeField] float maxZoom  = 50f;
    [SerializeField] float zoomStep = 5f;
    [SerializeField] float zoomSpeed = 5f;

    float _targetSize;

    void Start() => _targetSize = minimapCam.orthographicSize;

    public void ZoomIn()  => SetTargetZoom(_targetSize - zoomStep);
    public void ZoomOut() => SetTargetZoom(_targetSize + zoomStep);

    void SetTargetZoom(float size)
        => _targetSize = Mathf.Clamp(size, minZoom, maxZoom);

    void Update()
    {
        minimapCam.orthographicSize = Mathf.Lerp(
            minimapCam.orthographicSize,
            _targetSize,
            Time.deltaTime * zoomSpeed);
    }
}
```

### 3.7 Compass Bar — UV Rect Scrolling

Compass bar의 핵심 공식:

```csharp
// player.eulerAngles.y → 0~360
// RawImage.uvRect.x = yaw / 360
compassRawImage.uvRect = new Rect(player.eulerAngles.y / 360f, 0f, 1f, 1f);
```

이 한 줄로 플레이어의 수평 회전에 따라 텍스처가 스크롤된다. Compass 텍스처는 N/NE/E/SE/S/SW/W/NW 방향 마커가 좌우로 이어진 타일링 이미지여야 한다.

**waypoint 마커의 나침반 내 위치 계산**

```csharp
float GetCompassMarkerOffset(Transform player, Vector3 targetWorldPos)
{
    // 플레이어에서 타겟으로의 방향 벡터 (XZ 평면)
    Vector3 dirToTarget = targetWorldPos - player.position;
    dirToTarget.y = 0;
    dirToTarget.Normalize();

    // 월드 북쪽(Vector3.forward)과의 각도
    float angle = Vector3.SignedAngle(Vector3.forward, dirToTarget, Vector3.up);
    // 0~360으로 정규화
    float normalized = ((angle % 360f) + 360f) % 360f;

    // compass bar 상의 UV 위치 (0~1)
    return normalized / 360f;
}

// RectTransform 위치로 변환 (compass bar 너비 기준)
float compassBarWidth = compassBarRT.rect.width;
float playerYawNorm = player.eulerAngles.y / 360f;
float targetNorm = GetCompassMarkerOffset(player, target.position);

// 플레이어 yaw 기준 상대 오프셋 (화면 중앙이 정면)
float relativeOffset = Mathf.DeltaAngle(
    player.eulerAngles.y,
    targetNorm * 360f) / compassFOV;  // compassFOV: 화면에 보이는 각도 범위

markerRT.anchoredPosition = new Vector2(relativeOffset * compassBarWidth * 0.5f, 0);
```

**compassFOV**: 화면에 보이는 나침반의 각도 범위(예: 90도). 타겟이 `compassFOV/2` 이상 벗어나면 마커를 숨긴다.

### 3.8 플레이어 방향에 따른 minimap 회전

```csharp
// 미니맵이 항상 위가 북쪽 (고정 방향)
// 플레이어 아이콘만 회전
playerIconRT.localRotation = Quaternion.Euler(0, 0, -player.eulerAngles.y);

// 미니맵 전체가 플레이어 방향으로 회전하는 경우
// MinimapCamera를 Player의 자식으로 배치하거나:
minimapContentRT.localRotation = Quaternion.Euler(0, 0, player.eulerAngles.y);
```

### 3.9 오픈소스 레퍼런스

| 리소스 | 설명 |
|---|---|
| CommanderFoo/Unity-Horizontal-Compass | UI Toolkit 기반 수평 나침반, 웨이포인트 지원 |
| Edgar-Unity Minimap | 절차적 던전용 미니맵 시스템 |

---

## 4. Loading Screen with Async Progress

### 4.1 AsyncOperation.progress 의 주의사항

- 값 범위: `0.0 ~ 0.9` (allowSceneActivation = false일 때 0.9에서 정지)
- 0.9를 1.0으로 정규화: `Mathf.Clamp01(op.progress / 0.9f)`
- `isDone`은 씬이 완전히 활성화된 후 `true`가 됨
- 씬이 메모리에 다 올라와도 `allowSceneActivation = false`면 활성화되지 않음

### 4.2 기본 씬 로딩 + allowSceneActivation 제어

```csharp
IEnumerator LoadSceneCoroutine(int sceneIndex)
{
    AsyncOperation op = SceneManager.LoadSceneAsync(sceneIndex);
    op.allowSceneActivation = false;

    while (op.progress < 0.9f)
    {
        float displayProgress = Mathf.Clamp01(op.progress / 0.9f);
        progressBar.fillAmount = displayProgress;
        yield return null;
    }

    // 로딩 완료 → 사용자가 준비됐을 때 활성화
    op.allowSceneActivation = true;
}
```

### 4.3 UniTask + IProgress 패턴

UniTask에서 `AsyncOperation`에 진행 보고를 붙이는 권장 방식:

```csharp
// System.Progress<T> 대신 Cysharp.Threading.Tasks.Progress 사용 (할당 최소화)
var progress = Cysharp.Threading.Tasks.Progress.Create<float>(
    value => progressModel.RawProgress.Value = value);

// LoadSceneAsync는 ToUniTask()보다 직접 await를 권장
// (ToUniTask()는 Start/continuation 순서 차이 발생 가능)
AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
op.allowSceneActivation = false;

while (op.progress < 0.9f)
{
    progress.Report(op.progress / 0.9f);
    await UniTask.Yield(ct);
}
```

**Addressables 진행 보고**

```csharp
// AsyncOperationHandle은 ToUniTask(progress)를 지원
var handle = Addressables.LoadSceneAsync(key);
var addrProgress = Cysharp.Threading.Tasks.Progress.Create<float>(
    v => progressModel.AddressablesProgress.Value = v);
await handle.ToUniTask(progress: addrProgress, cancellationToken: ct);
```

**주의**: `autoReleaseHandle: true`와 `IProgress` 콜백을 동시에 사용하면 예외가 발생할 수 있다 (UniTask GitHub Issue #119). `autoReleaseHandle`은 false로 두고 수동으로 해제할 것.

### 4.4 복수 로딩 작업의 진행 집계

```csharp
// Addressables 핸들 목록의 평균 progress
private float ComputeAggregatedProgress(
    List<AsyncOperationHandle> handles)
{
    if (handles.Count == 0) return 0f;

    float sum = 0f;
    foreach (var h in handles)
        sum += h.GetDownloadStatus().Percent; // 0~1
    return sum / handles.Count;
}

// 가중치를 적용한 집계 (씬 60% + 에셋 40%)
private float ComputeWeightedProgress(
    float sceneProgress,
    float assetsProgress,
    float sceneWeight = 0.6f)
{
    return sceneProgress * sceneWeight
         + assetsProgress * (1f - sceneWeight);
}
```

### 4.5 전체 로딩 시스템 — UniTask + R3 패턴

```csharp
// --- Model ---
public class LoadingModel : IDisposable
{
    // 실제 로딩 진행 (0~1)
    public ReactiveProperty<float> RawProgress      { get; } = new(0f);
    // 화면에 표시할 부드러운 진행 (Lerp 적용)
    public ReactiveProperty<float> DisplayProgress  { get; } = new(0f);
    // 현재 팁 텍스트
    public ReactiveProperty<string> CurrentTip      { get; } = new(string.Empty);

    public void Dispose()
    {
        RawProgress.Dispose();
        DisplayProgress.Dispose();
        CurrentTip.Dispose();
    }
}

// --- Presenter ---
public class LoadingPresenter : IStartable, IDisposable
{
    readonly LoadingView  _view;
    readonly LoadingModel _model;
    readonly CompositeDisposable _disposables = new();

    readonly string[] _tips = {
        "팁: 이동 중 체력 회복 아이템을 사용할 수 있습니다.",
        "팁: 야간에는 적이 더 강해집니다.",
        "팁: 기지 업그레이드로 방어력을 높이세요."
    };

    [Inject]
    public LoadingPresenter(LoadingView view, LoadingModel model)
    {
        _view  = view;
        _model = model;
    }

    public void Start()
    {
        // RawProgress → DisplayProgress Lerp (Update 루프)
        SmoothProgressLoop(_disposables.GetCancellationToken()).Forget();

        // DisplayProgress → View 바인딩
        _model.DisplayProgress
            .Subscribe(v => _view.SetProgressBar(v))
            .AddTo(_disposables);

        _model.CurrentTip
            .Subscribe(tip => _view.SetTipText(tip))
            .AddTo(_disposables);
    }

    async UniTaskVoid SmoothProgressLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _model.DisplayProgress.Value = Mathf.Lerp(
                _model.DisplayProgress.Value,
                _model.RawProgress.Value,
                Time.deltaTime * 5f); // 5f = lerp 속도
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    public void Dispose() => _disposables.Dispose();
}

// --- Service ---
public class SceneLoadingService
{
    readonly LoadingModel _model;

    [Inject]
    public SceneLoadingService(LoadingModel model) => _model = model;

    public async UniTask LoadAsync(
        string sceneName,
        string[] addressableKeys,
        float minimumDisplaySeconds,
        CancellationToken ct)
    {
        float startTime = Time.realtimeSinceStartup;

        // Tips 사이클 시작
        CycleTipsLoop(ct).Forget();

        // 씬 로드 (progress 0.6 weight)
        var sceneOp = SceneManager.LoadSceneAsync(sceneName);
        sceneOp.allowSceneActivation = false;

        // Addressables 동시 로드 (progress 0.4 weight)
        var addrHandles = addressableKeys
            .Select(k => Addressables.LoadAssetAsync<object>(k))
            .ToList();

        // 모든 작업이 완료될 때까지 진행 보고
        while (!IsAllDone(sceneOp, addrHandles))
        {
            float sceneP  = Mathf.Clamp01(sceneOp.progress / 0.9f);
            float addrP   = ComputeAggregatedProgress(
                                addrHandles.Cast<AsyncOperationHandle>().ToList());
            _model.RawProgress.Value =
                ComputeWeightedProgress(sceneP, addrP, 0.6f);
            await UniTask.Yield(ct);
        }

        _model.RawProgress.Value = 1f;

        // 최소 표시 시간 보장
        float elapsed = Time.realtimeSinceStartup - startTime;
        if (elapsed < minimumDisplaySeconds)
            await UniTask.Delay(
                TimeSpan.FromSeconds(minimumDisplaySeconds - elapsed), ct);

        // 씬 활성화
        sceneOp.allowSceneActivation = true;

        // Addressable 핸들 해제
        foreach (var h in addrHandles)
            Addressables.Release(h);
    }

    async UniTaskVoid CycleTipsLoop(CancellationToken ct)
    {
        int idx = 0;
        while (!ct.IsCancellationRequested)
        {
            _model.CurrentTip.Value = _tips[idx % _tips.Length];
            idx++;
            await UniTask.Delay(TimeSpan.FromSeconds(4f), ct);
        }
    }

    bool IsAllDone(AsyncOperation sceneOp,
                   List<AsyncOperationHandle> handles)
        => sceneOp.progress >= 0.9f
           && handles.All(h => h.IsDone);

    float ComputeAggregatedProgress(List<AsyncOperationHandle> handles)
    {
        if (handles.Count == 0) return 0f;
        return handles.Average(h => h.GetDownloadStatus().Percent);
    }

    float ComputeWeightedProgress(float a, float b, float weightA)
        => a * weightA + b * (1f - weightA);
}
```

### 4.6 View — 로딩 화면 컴포넌트

```csharp
public class LoadingView : MonoBehaviour
{
    [SerializeField] Image    progressFill;
    [SerializeField] TMP_Text tipText;
    [SerializeField] TMP_Text percentText;

    public void SetProgressBar(float value)
    {
        progressFill.fillAmount = value;
        percentText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    public void SetTipText(string tip) => tipText.text = tip;
}
```

### 4.7 진행 바 부드럽게 처리하는 전략 비교

| 전략 | 방식 | 특징 |
|---|---|---|
| `Mathf.Lerp` (per frame) | `Lerp(current, target, dt * speed)` | 빠른 구간은 빠르게, 느린 구간은 느리게. 가장 자연스럽지만 절대 100%에 도달하지 않으므로 마지막에 강제로 1f 설정 필요 |
| `Mathf.MoveTowards` | `MoveTowards(current, target, dt * speed)` | 선형 속도, 100%에 정확히 도달. 느낌이 단조로움 |
| DOTween | `DOValue(target, duration)` | Easing 커브 적용 가능. 실시간 target 변경이 필요하면 Kill + 재시작 |

**권장**: Lerp 방식에 `_model.RawProgress.Value = 1f` 강제 처리 조합.

### 4.8 최소 표시 시간 패턴 정리

```csharp
// 패턴 A: UniTask.WhenAll로 로딩과 타이머를 병렬 대기
await UniTask.WhenAll(
    LoadAllAssetsAsync(ct),
    UniTask.Delay(TimeSpan.FromSeconds(minimumSeconds), ct));

// 패턴 B: 직렬 처리 (로딩 후 남은 시간만큼 대기)
await LoadAllAssetsAsync(ct);
float remaining = minimumSeconds - (Time.realtimeSinceStartup - startTime);
if (remaining > 0)
    await UniTask.Delay(TimeSpan.FromSeconds(remaining), ct);
```

패턴 A는 로딩과 타이머가 동시에 진행되어 더 효율적이다.

---

## 5. 패턴별 VContainer 통합 요약

| 패턴 | LifetimeScope 전략 |
|---|---|
| Radial Menu | 메뉴가 열릴 때 생성되는 자식 Scope에 등록. 닫을 때 Dispose. |
| Minimap | GameScope(게임 세션 전체)에 등록. 플레이어 Transform을 ID로 주입. |
| Loading Screen | LoadingScope로 분리. 씬 전환 완료 후 Scope Dispose. |

VContainer의 `IStartable`과 `IDisposable`을 활용해 Presenter의 생명주기를 Scope에 위임한다. `RegisterEntryPoint<T>()`가 `IStartable.Start()`와 `IDisposable.Dispose()`를 자동으로 호출한다.

---

## 6. 소스 및 참고

- [Unity Scripting API: Image.FillMethod.Radial360](https://docs.unity3d.com/2017.2/Documentation/ScriptReference/UI.Image.FillMethod.Radial360.html)
- [EXP-Productions/RadialMenu-Unity (GitHub)](https://github.com/EXP-Productions/RadialMenu-Unity)
- [rito15/Unity-Radial-Menu (GitHub)](https://github.com/rito15/Unity-Radial-Menu)
- [aillieo/UnityRadialLayoutGroup (GitHub)](https://github.com/aillieo/UnityRadialLayoutGroup)
- [AnnulusGames/ReactiveInputSystem (GitHub)](https://github.com/AnnulusGames/ReactiveInputSystem)
- [VionixStudio: Radial Menu in Unity](https://vionixstudio.com/2022/09/05/radial-menu-in-unity/)
- [CommanderFoo/Unity-Horizontal-Compass (GitHub)](https://github.com/CommanderFoo/Unity-Horizontal-Compass)
- [MakeYourGame: Horizontal Compass Battle Royale style](https://makeyourgame.fun/blog/unity/creer-une-boussole-horizontale-a-la-battle-royale)
- [Unity Discussions: Horizontal Compass HUD](https://discussions.unity.com/t/horizontal-compass-hud/613537)
- [Vasundhara: Minimap RenderTexture in Unity](https://vasundhara.io/blogs/minimap-render-texture-in-unity)
- [Medium: Build a UI Minimap in Unity](https://medium.com/@tmaurodot/unity-user-interface-build-a-ui-minimap-in-unity-35456c022c15)
- [Unity Scripting API: AsyncOperation.progress](https://docs.unity3d.com/ScriptReference/AsyncOperation-progress.html)
- [Unity Scripting API: AsyncOperation.allowSceneActivation](https://docs.unity3d.com/ScriptReference/AsyncOperation-allowSceneActivation.html)
- [Addressables: Async operation handles (2.0.8)](https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/AddressableAssetsAsyncOperationHandle.html)
- [Cysharp/UniTask (GitHub)](https://github.com/Cysharp/UniTask)
- [UniTask Issue #119: ToUniTask + autoReleaseHandle + IProgress exception](https://github.com/Cysharp/UniTask/issues/119)
- [PrimeGames: Loading screen with loading bar](https://www.primegames.bg/en/blog/howto-create-a-loading-screen-with-a-loading-bar-in-unity)
- [Medium: Addressables with Loading Bar in Unity](https://medium.com/@onurkiris05/addressables-with-loading-bar-in-unity-aws-part-1-b382a952ac76)
