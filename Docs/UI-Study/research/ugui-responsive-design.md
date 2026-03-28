# UGUI Responsive & Adaptive Design 리서치 리포트

- **작성일**: 2026-03-28
- **카테고리**: practice
- **상태**: 조사완료

---

## 1. 요약

Unity UGUI에서 반응형 UI는 CanvasScaler + Anchor 시스템이 기반이며, 대부분의 모바일 게임은 `Scale With Screen Size` + `Match = 0` (portrait) 또는 `Match = 1` (landscape)로 설계한다. 노치/펀치홀 등 Safe Area는 `Screen.safeArea`를 RectTransform anchor로 변환하는 단일 컴포넌트로 처리하되, `Update()`에서 변화를 폴링하여 방향 전환에 대응한다. TextMeshPro auto-size는 기준 레이블 하나만 auto-size를 켜고 나머지가 그 크기를 따라가게 하는 패턴이 로컬라이제이션에 가장 안전하다.

---

## 2. 상세 분석

### 2.1 CanvasScaler 전략

CanvasScaler는 Canvas 하위 모든 UI 요소의 크기와 픽셀 밀도를 제어한다. 세 가지 `UI Scale Mode`가 있다.

#### 2.1.1 Scale With Screen Size (권장)

가장 많이 사용하는 모드. `Reference Resolution`을 기준 해상도로 지정하고, 실제 화면이 그것보다 크거나 작으면 전체 UI를 비례 스케일한다.

**Screen Match Mode 옵션:**

| Match Mode | 동작 | 사용 시기 |
|---|---|---|
| Match Width or Height | matchWidthOrHeight 슬라이더(0~1)로 너비/높이 중 어느 것을 기준으로 할지 비율 결정 | 단일 방향 앱 |
| Expand | Canvas가 reference resolution보다 작아지지 않게 확장 | 여백이 생겨도 되는 게임 |
| Shrink | Canvas가 reference resolution보다 커지지 않게 축소 | 요소 잘림이 절대 안 되는 UI |

**matchWidthOrHeight 공식:**

Unity는 너비 로그 비율과 높이 로그 비율을 linearly interpolate하여 최종 스케일을 결정한다.

```
scaleFactor = pow(
    pow(screenWidth / referenceWidth, 1 - matchWidthOrHeight) *
    pow(screenHeight / referenceHeight, matchWidthOrHeight),
    1
)
```

실무 설정:
- Portrait 전용 앱: `matchWidthOrHeight = 0` → 너비 기준 스케일, 세로가 길어져도 요소 유지
- Landscape 전용 앱: `matchWidthOrHeight = 1` → 높이 기준 스케일
- 양쪽 지원: `matchWidthOrHeight = 0.5` → 비율 차이를 상쇄 (Landscape가 1.5배 넓고 1.5배 짧으면 두 스케일이 상쇄되어 균형)

**권장 기준 해상도:**

| 대상 | Reference Resolution | Match |
|---|---|---|
| 모바일 Portrait | 1080 × 1920 | 0 |
| 모바일 Landscape | 1920 × 1080 | 1 |
| 태블릿 겸용 | 1080 × 1920 | 0.5 |
| PC | 1920 × 1080 | 0.5 |

#### 2.1.2 Constant Physical Size

실제 물리적 크기(mm, points, picas)를 기준으로 스케일한다. `Screen.dpi`를 사용하므로 기기가 DPI를 정확히 보고할 때만 신뢰할 수 있다. 문제: 많은 Android 기기가 DPI를 부정확하게 보고함 → 예측 불가한 스케일 문제 발생. **게임에서는 거의 사용하지 않는다.**

#### 2.1.3 Constant Pixel Size

화면 크기에 관계없이 픽셀 크기를 유지한다. 대형 모니터에서 UI가 상대적으로 작아 보인다. 고정 해상도 PC 게임이나 에디터 전용 도구에 적합하다.

---

### 2.2 Safe Area 처리 (노치/펀치홀/둥근 모서리)

`Screen.safeArea`는 OS가 제공하는 Rect로, 화면 내 UI를 배치하기에 안전한 영역을 나타낸다. 원점은 좌하단.

#### 2.2.1 기본 구현 패턴

```csharp
using UnityEngine;

/// <summary>
/// Canvas 하위에 SafeAreaContainer GameObject를 만들고
/// RectTransform을 전체 stretch로 설정한 뒤 이 컴포넌트를 붙인다.
/// 모든 UI 콘텐츠는 이 오브젝트의 하위에 배치한다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    private RectTransform _rt;
    private Rect _lastSafeArea;
    private ScreenOrientation _lastOrientation;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    // Update()로 폴링: 방향 전환 시 Screen.safeArea가 변경됨
    private void Update()
    {
        if (_lastSafeArea != Screen.safeArea ||
            _lastOrientation != Screen.orientation)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        _lastSafeArea = Screen.safeArea;
        _lastOrientation = Screen.orientation;

        var anchorMin = _lastSafeArea.position;
        var anchorMax = anchorMin + _lastSafeArea.size;

        // 픽셀 좌표 → 정규화(0~1) anchor
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
    }
}
```

#### 2.2.2 선택적 엣지 무시 패턴

경우에 따라 상단 노치는 무시하고 하단 홈 바만 피하고 싶을 수 있다.

```csharp
[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    [SerializeField] private bool _ignoreTop    = false;
    [SerializeField] private bool _ignoreBottom = false;
    [SerializeField] private bool _ignoreLeft   = false;
    [SerializeField] private bool _ignoreRight  = false;

    private RectTransform _rt;
    private Rect _lastSafeArea;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        if (_lastSafeArea != Screen.safeArea)
            ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        _lastSafeArea = Screen.safeArea;

        var anchorMin = _lastSafeArea.position;
        var anchorMax = anchorMin + _lastSafeArea.size;

        if (_ignoreLeft)   anchorMin.x = 0f;
        if (_ignoreBottom) anchorMin.y = 0f;
        if (_ignoreRight)  anchorMax.x = Screen.width;
        if (_ignoreTop)    anchorMax.y = Screen.height;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
    }
}
```

#### 2.2.3 Android 빌드 설정 주의사항

Android는 기본적으로 safe area 밖을 검정으로 채운다.
**Player Settings → Resolution and Presentation → "Render outside safe area"** 를 반드시 체크해야 전체 화면을 활용할 수 있다. iOS는 이미 safe area 밖을 렌더링하므로 설정 불필요.

#### 2.2.4 에디터 테스트: Device Simulator

Unity 내장 Device Simulator를 사용하면 빌드 없이 다양한 기기의 노치/펀치홀/Safe Area를 시뮬레이션할 수 있다.
- `Window → General → Device Simulator`
- 기기 드롭다운에서 iPhone X, Pixel 등 선택
- "Safe Area" 버튼으로 safe area 경계 시각화

커스텀 기기 추가: `.device` 확장자의 JSON 파일 + 오버레이 이미지로 정의.

#### 2.2.5 서드파티: Notch Solution

[Notch Solution](https://exceed7.com/notch-solution/) 패키지는 `Screen.safeArea` 외에 `Screen.cutouts`(구멍)도 대응하며, 에디터에서 실시간 미리보기가 가능한 Notch Simulator를 제공한다.

```
// UPM 설치 (manifest.json)
"com.e7.notch-solution": "https://github.com/5argon/NotchSolution.git"
```

---

### 2.3 Aspect Ratio 적응

#### 2.3.1 Anchor 전략

Anchor는 Unity UI 반응형의 핵심이다.

| 상황 | Anchor 설정 |
|---|---|
| 좌상단 버튼 | anchorMin = anchorMax = (0, 1) |
| 우하단 버튼 | anchorMin = anchorMax = (1, 0) |
| 화면 전체 stretch | anchorMin = (0,0), anchorMax = (1,1) |
| 가로 전체 + 상단 고정 | anchorMin = (0,1), anchorMax = (1,1) |
| 세로 가운데 정렬 | anchorMin.y = anchorMax.y = 0.5 |

Anchor가 분리되어 있으면 (anchorMin ≠ anchorMax) 해당 방향으로 부모 크기에 비례해 늘어난다. Anchor가 겹쳐 있으면 고정 크기를 유지한다.

#### 2.3.2 AspectRatioFitter

자신의 RectTransform 크기를 지정된 비율로 유지한다.

| 모드 | 동작 |
|---|---|
| Width Controls Height | 너비에서 높이 자동 계산 |
| Height Controls Width | 높이에서 너비 자동 계산 |
| Fit In Parent | 부모 안에 꼭 맞게 (letterbox 가능) |
| Envelope Parent | 부모를 완전히 덮도록 (clipping 발생 가능) |

16:9 컨테이너가 4:3 화면에 표시될 때 letterbox를 만들려면 `Fit In Parent`를 사용한다.

```csharp
// 16:9 비율 컨테이너 코드로 설정
var arf = GetComponent<AspectRatioFitter>();
arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
arf.aspectRatio = 16f / 9f;
```

#### 2.3.3 Layout Groups + ContentSizeFitter

- `HorizontalLayoutGroup` / `VerticalLayoutGroup`: 자식들을 일렬로 정렬, `childForceExpandWidth/Height`로 stretch 제어
- `GridLayoutGroup`: 격자 배치, 셀 크기 고정
- `ContentSizeFitter`: 자신의 크기를 콘텐츠(자식들의 preferred size)에 맞게 자동 조정

주의: `ContentSizeFitter`를 Layout Group 안에 넣으면 레이아웃 재계산 루프 경고가 발생할 수 있다. 이 경우 `LayoutElement`의 `preferredWidth/Height`를 스크립트로 수동 설정하는 것이 더 안전하다.

---

### 2.4 Multi-Resolution Sprite 전략

#### 2.4.1 SpriteAtlas Variant (@2x, @0.5x)

Unity SpriteAtlas의 Variant는 Master Atlas의 텍스처를 지정된 비율로 다운스케일한 버전이다.

**워크플로:**
1. Master Atlas 생성: Type = "Master", 원본 해상도 스프라이트 추가
2. Variant Atlas 생성: Type = "Variant", Master Atlas 지정, Scale = 0.5 (절반 해상도)
3. 저사양 기기에서는 Variant Atlas를 빌드에 포함, 고사양은 Master 포함

```csharp
// Variant Atlas를 동적으로 로드하는 패턴 (Addressables 사용 시)
public class AtlasResolutionSwitcher : MonoBehaviour
{
    [SerializeField] private AssetReferenceT<SpriteAtlas> _highResAtlas;
    [SerializeField] private AssetReferenceT<SpriteAtlas> _lowResAtlas;

    private async UniTaskVoid Start()
    {
        bool isHighEnd = SystemInfo.graphicsMemorySize >= 2048;
        var atlasRef = isHighEnd ? _highResAtlas : _lowResAtlas;
        var atlas = await atlasRef.LoadAssetAsync<SpriteAtlas>().ToUniTask();

        // SpriteAtlas.GetSprite()로 개별 스프라이트 가져오기
        var sprite = atlas.GetSprite("icon_health");
        GetComponent<Image>().sprite = sprite;
    }
}
```

#### 2.4.2 중요 주의사항

- Variant의 "Include in Build"를 끄고 Master만 켜면 빌드에는 Master만 포함된다
- 둘 다 끄면 스프라이트가 투명하게 렌더됨 (실수하기 쉬운 함정)
- `atlasRequested` late binding 이벤트는 에디터에서만 동작하고 실기기에서 실패하는 버그가 보고된 바 있다 — Resources 폴더나 Addressables를 사용하는 것이 안전

#### 2.4.3 SpriteAtlas V2 (Unity 2022.1+)

SpriteAtlas V2는 에디터 전용 DB를 사용해 패킹 결과를 저장하며, 패킹 속도가 개선되었다. 기존 V1과 API는 동일하다.

---

### 2.5 방향 전환 대응 (Portrait ↔ Landscape)

#### 2.5.1 CanvasScaler Match 설정

방향 전환을 지원한다면 `matchWidthOrHeight = 0.5`로 설정하면 양방향에서 균형 잡힌 스케일을 얻는다.

만약 Portrait/Landscape 전용 레이아웃을 별도로 사용한다면 방향 전환 시 Canvas를 교체하는 방법이 있다.

#### 2.5.2 R3로 방향 전환 감지

```csharp
using R3;
using UnityEngine;
using UnityEngine.UI;

public class OrientationResponsiveLayout : MonoBehaviour
{
    [SerializeField] private GameObject _portraitLayout;
    [SerializeField] private GameObject _landscapeLayout;

    private ScreenOrientation _lastOrientation;

    private void Start()
    {
        // R3 Observable.EveryUpdate로 폴링
        Observable.EveryUpdate()
            .Select(_ => Screen.orientation)
            .DistinctUntilChanged()
            .Subscribe(orientation =>
            {
                bool isPortrait = orientation == ScreenOrientation.Portrait ||
                                  orientation == ScreenOrientation.PortraitUpsideDown;
                _portraitLayout.SetActive(isPortrait);
                _landscapeLayout.SetActive(!isPortrait);

                // 레이아웃 그룹 즉시 재계산
                LayoutRebuilder.MarkLayoutForRebuild(
                    _portraitLayout.GetComponent<RectTransform>() ??
                    _landscapeLayout.GetComponent<RectTransform>()
                );
                Canvas.ForceUpdateCanvases();
            })
            .AddTo(this);
    }
}
```

#### 2.5.3 CanvasScaler를 동적으로 전환

```csharp
public class CanvasScalerOrientationAdapter : MonoBehaviour
{
    [SerializeField] private CanvasScaler _scaler;

    private void Start()
    {
        Observable.EveryUpdate()
            .Select(_ => Screen.orientation)
            .DistinctUntilChanged()
            .Subscribe(UpdateScaler)
            .AddTo(this);
    }

    private void UpdateScaler(ScreenOrientation orientation)
    {
        bool isPortrait = orientation == ScreenOrientation.Portrait ||
                          orientation == ScreenOrientation.PortraitUpsideDown;
        _scaler.referenceResolution = isPortrait
            ? new Vector2(1080, 1920)
            : new Vector2(1920, 1080);
        _scaler.matchWidthOrHeight = isPortrait ? 0f : 1f;
    }
}
```

---

### 2.6 TextMeshPro 텍스트 처리

#### 2.6.1 Auto-Size 기본 사용법

```csharp
using TMPro;

// Inspector에서 설정하거나 코드로 제어
var tmp = GetComponent<TMP_Text>();
tmp.enableAutoSizing = true;
tmp.fontSizeMin = 12f;
tmp.fontSizeMax = 36f;
// WrapMode는 Overflow 설정에 따름
```

#### 2.6.2 Overflow 모드

| 모드 | 동작 | 사용 시기 |
|---|---|---|
| Overflow | 박스 밖으로 넘침 | 스크롤 가능한 영역 |
| Ellipsis | 끝에 "..." 표시 | 리스트 아이템 |
| Truncate | 그냥 잘림 | HUD 숫자 표시 |
| Scroll Rect | 수평 스크롤 | 채팅, 이름 |
| Linked | 다음 TMP로 이어짐 | 긴 본문 텍스트 |
| Page | 페이지 단위 표시 | 책/대화 |

#### 2.6.3 로컬라이제이션 대응 패턴

로컬라이제이션 시 텍스트 길이가 언어마다 다르기 때문에 여러 버튼/레이블이 서로 다른 폰트 크기가 되는 문제가 발생한다. 해결 방법: 가장 긴 텍스트가 들어가는 레이블 하나만 auto-size 활성화, 나머지는 그 크기를 따른다.

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using R3;

/// <summary>
/// _leader 하나에만 enableAutoSizing = true를 켜고,
/// 나머지 _followers는 leader의 fontSize를 Update에서 동기화한다.
/// </summary>
public class TMP_FontSizeSynchronizer : MonoBehaviour
{
    [Tooltip("Auto-size가 켜진 기준 레이블")]
    [SerializeField] private TMP_Text _leader;

    [Tooltip("기준 레이블의 크기를 따를 레이블들 (enableAutoSizing = false)")]
    [SerializeField] private List<TMP_Text> _followers;

    private float _lastFontSize;

    private void Awake()
    {
        // leader만 auto-size 활성
        _leader.enableAutoSizing = true;
        foreach (var f in _followers)
            f.enableAutoSizing = false;
    }

    private void Update()
    {
        float size = _leader.fontSize;
        if (Mathf.Approximately(size, _lastFontSize)) return;
        _lastFontSize = size;
        foreach (var f in _followers)
            f.fontSize = size;
    }
}
```

#### 2.6.4 ContentSizeFitter와 auto-size 충돌 주의

`ContentSizeFitter`가 있는 오브젝트에 auto-size TMP를 쓰면 `GetPreferredWidth()`가 `fontSizeMax` 기준으로 계산되어 박스가 필요 이상으로 넓어진다. 이 경우 auto-size를 끄고 고정 크기를 사용하거나, TMP가 부모보다 먼저 레이아웃되도록 순서를 조정해야 한다.

---

### 2.7 Phone vs Tablet 적응형 레이아웃

#### 2.7.1 기기 분류 유틸리티

```csharp
using UnityEngine;

public static class DeviceProfile
{
    private const float TabletDiagonalInchThreshold = 6.5f;

    /// <summary>
    /// 화면 대각선이 6.5인치 이상이면 태블릿으로 판단.
    /// Screen.dpi가 0을 반환하면 (DPI 미지원 기기) false 반환.
    /// </summary>
    public static bool IsTablet
    {
        get
        {
            float dpi = Screen.dpi;
            if (dpi <= 0f) return false;
            float width  = Screen.width  / dpi;
            float height = Screen.height / dpi;
            float diagonal = Mathf.Sqrt(width * width + height * height);
            return diagonal >= TabletDiagonalInchThreshold;
        }
    }

    public static bool IsPhone => !IsTablet;

    /// <summary>
    /// 현재 화면의 aspect ratio (항상 >= 1)
    /// </summary>
    public static float AspectRatio
    {
        get
        {
            float w = Screen.width;
            float h = Screen.height;
            return w > h ? w / h : h / w;
        }
    }
}
```

#### 2.7.2 레이아웃 스위처 패턴

```csharp
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class AdaptiveLayoutSwitcher : IStartable
{
    [SerializeField] private GameObject _phoneLayout;
    [SerializeField] private GameObject _tabletLayout;

    // VContainer로 주입받는 경우 LifetimeScope에서 바인딩
    public AdaptiveLayoutSwitcher(GameObject phoneLayout, GameObject tabletLayout)
    {
        _phoneLayout  = phoneLayout;
        _tabletLayout = tabletLayout;
    }

    public void Start()
    {
        bool isTablet = DeviceProfile.IsTablet;
        _phoneLayout.SetActive(!isTablet);
        _tabletLayout.SetActive(isTablet);
    }
}
```

MonoBehaviour 버전 (View에서 직접 사용):

```csharp
public class AdaptiveLayoutView : MonoBehaviour
{
    [SerializeField] private GameObject _phoneRoot;
    [SerializeField] private GameObject _tabletRoot;
    [SerializeField] private CanvasScaler _scaler;

    private void Awake()
    {
        bool isTablet = DeviceProfile.IsTablet;
        _phoneRoot.SetActive(!isTablet);
        _tabletRoot.SetActive(isTablet);

        // 태블릿은 기준 해상도를 키워서 UI가 상대적으로 작게 보이게
        if (isTablet)
        {
            _scaler.referenceResolution = new Vector2(1536, 2048);
            _scaler.matchWidthOrHeight  = 0.5f;
        }
    }
}
```

---

### 2.8 UI Scale 접근성 (사용자 설정 가능 UI 크기)

#### 2.8.1 CanvasScaler의 scaleFactor 활용

`Scale With Screen Size` 모드에서 `CanvasScaler.scaleFactor`는 추가 배율 승수로 작동한다. 이를 이용해 사용자 지정 스케일을 구현할 수 있다.

```csharp
using UnityEngine;
using UnityEngine.UI;
using R3;
using VContainer;
using VContainer.Unity;

public class UIScaleService : IStartable
{
    public const float MinScale = 0.75f;
    public const float MaxScale = 1.5f;
    public const string PrefKey = "UIScale";

    private readonly CanvasScaler[] _scalers;
    private readonly ReactiveProperty<float> _scale = new ReactiveProperty<float>(1f);

    public ReadOnlyReactiveProperty<float> Scale => _scale;

    [Inject]
    public UIScaleService(CanvasScaler[] scalers)
    {
        _scalers = scalers;
    }

    public void Start()
    {
        // 저장된 설정 로드
        float saved = PlayerPrefs.GetFloat(PrefKey, 1f);
        SetScale(saved);
    }

    public void SetScale(float value)
    {
        float clamped = Mathf.Clamp(value, MinScale, MaxScale);
        _scale.Value = clamped;
        PlayerPrefs.SetFloat(PrefKey, clamped);

        foreach (var scaler in _scalers)
            scaler.scaleFactor = clamped;
    }
}
```

Settings View에서 슬라이더로 연결:

```csharp
public class UIScaleSettingsView : MonoBehaviour
{
    [SerializeField] private Slider _scaleSlider;
    [SerializeField] private TMP_Text _scaleLabel;

    private UIScaleService _uiScaleService;

    [Inject]
    public void Construct(UIScaleService uiScaleService)
    {
        _uiScaleService = uiScaleService;
    }

    private void Start()
    {
        _scaleSlider.minValue = UIScaleService.MinScale;
        _scaleSlider.maxValue = UIScaleService.MaxScale;
        _scaleSlider.value    = _uiScaleService.Scale.CurrentValue;

        // 슬라이더 → 서비스
        _scaleSlider.OnValueChangedAsObservable()
            .ThrottleLast(TimeSpan.FromSeconds(0.1f))
            .Subscribe(v =>
            {
                _uiScaleService.SetScale(v);
                _scaleLabel.text = $"{v * 100:F0}%";
            })
            .AddTo(this);

        // 서비스 → 레이블 동기화
        _uiScaleService.Scale
            .Subscribe(v => _scaleLabel.text = $"{v * 100:F0}%")
            .AddTo(this);
    }
}
```

#### 2.8.2 주의: Scale With Screen Size + scaleFactor

`Scale With Screen Size` 모드에서는 `scaleFactor`를 직접 설정해도 내부 계산이 매 프레임 덮어쓸 수 있다. 이 경우 `referenceResolution`을 역으로 조정하거나, `Constant Pixel Size` 모드로 전환 후 `scaleFactor`를 설정하는 방법을 써야 한다.

```csharp
// 안전한 런타임 스케일 변경 방법 (Constant Pixel Size 활용)
private void ApplyUserScale(CanvasScaler scaler, float userScale)
{
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
    scaler.scaleFactor = userScale;
}
```

---

### 2.9 DPI-Aware UI

#### 2.9.1 Screen.dpi 신뢰성

`Screen.dpi`는 OS가 보고하는 값이며, 기기마다 정확도가 다르다.
- iOS: 대체로 정확
- Android: 부정확한 기기 다수 (Samsung 구형 기기, 중저가폰)
- `Screen.dpi == 0`: DPI를 알 수 없는 기기 (웹, 오래된 플랫폼)

#### 2.9.2 DPI 기반 최소 터치 크기 보장

모바일에서 터치 대상은 최소 44pt(iOS HIG) 또는 48dp(Android Material)를 권장한다.

```csharp
using UnityEngine;
using UnityEngine.UI;

public static class DPIAwareMinSize
{
    private const float FallbackDPI = 160f; // 미지원 기기 폴백

    /// <summary>
    /// 물리적 mm 크기를 현재 DPI 기준 Unity 픽셀 크기로 변환
    /// </summary>
    public static float MmToPixels(float mm)
    {
        float dpi = Screen.dpi > 0 ? Screen.dpi : FallbackDPI;
        float inches = mm / 25.4f;
        return inches * dpi;
    }

    /// <summary>
    /// 최소 터치 크기 보장 (기본값: 10mm ≈ 44pt iOS, 48dp Android)
    /// </summary>
    public static void EnsureMinTouchSize(RectTransform rt, float minSizeMm = 10f)
    {
        float minPx = MmToPixels(minSizeMm);
        var size = rt.sizeDelta;
        if (size.x < minPx) size.x = minPx;
        if (size.y < minPx) size.y = minPx;
        rt.sizeDelta = size;
    }
}
```

#### 2.9.3 Reference DPI와 폴백 설정

`CanvasScaler.Constant Physical Size` 모드를 사용할 때는 `defaultSpriteDPI`와 `fallbackScreenDPI`를 설정한다.

```csharp
var scaler = GetComponent<CanvasScaler>();
scaler.uiScaleMode       = CanvasScaler.ScaleMode.ConstantPhysicalSize;
scaler.physicalUnit      = CanvasScaler.Unit.Points;
scaler.fallbackScreenDPI = 96f;   // 웹, 에디터 폴백
scaler.defaultSpriteDPI  = 96f;
```

---

### 2.10 Unity 6000+ 반응형 UI 관련 변경사항

Unity 6 (6000.x) 기준 UGUI 자체의 반응형 레이아웃 관련 주요 신기능은 많지 않으나, 다음 사항들이 실질적 개선을 가져온다.

| 기능 | 버전 | 내용 |
|---|---|---|
| Shader Graph Canvas Target | Unity 6.0 | URP/HDRP에서 UI용 커스텀 셰이더를 Shader Graph로 제작 가능 |
| UI Toolkit Runtime Binding | Unity 6.0 | UI Toolkit에서 데이터 바인딩 (UGUI와는 별개) |
| LayoutGroup 자식 빈 RectTransform 버그 수정 | 6000.x 패치 | 빈 RectTransform 자식이 레이아웃 갱신을 막던 버그 수정 |
| uGUI 2.0 패키지 | 2022.1+ | SpriteAtlas V2, Canvas 개선, 하위 호환 유지 |
| Device Simulator 통합 | 2021.2+ | 에디터 Game View에 통합 (별도 패키지 불필요) |
| UI Toolkit World Space Input | Unity 6.x | World Space Canvas와 UI Toolkit 혼용 가능 |

UI Toolkit은 Flexbox 기반 레이아웃(Yoga 엔진)을 지원하여 CSS-style 반응형이 가능하지만, 현재 프로젝트 스택(UGUI)과는 별개이다.

---

## 3. 베스트 프랙티스

### DO (권장)

- [x] **CanvasScaler는 항상 `Scale With Screen Size`** 사용 (Constant Physical Size는 DPI 신뢰 불가 이유로 게임에 부적합)
- [x] **Portrait 앱은 Match = 0 (너비 기준)**, Landscape 앱은 Match = 1 (높이 기준)
- [x] **Safe Area 패널은 Canvas 바로 아래 단독 레이어**로 배치, 모든 게임플레이 UI는 그 안에 넣기
- [x] **SafeAreaPanel의 Update() 폴링**으로 방향 전환 시 자동 갱신
- [x] **Android는 "Render outside safe area" 반드시 활성화** (없으면 검정 바 생김)
- [x] **Device Simulator로 노치/Safe Area 에디터 검증** (빌드 전에 확인)
- [x] **여러 TMP 레이블 폰트 크기 동기화**는 리더-팔로워 패턴 사용
- [x] **Canvas를 용도별로 분리** (Static UI Canvas, Dynamic HUD Canvas) → 배치 최적화
- [x] **Anchor를 반드시 설정** — 새 UI 요소 추가 시 anchor 점검이 첫 번째 체크리스트
- [x] **사용자 UI 스케일은 PlayerPrefs에 저장**, 앱 재시작 시 복원

### DON'T (금지)

- [ ] **Constant Physical Size를 모바일 게임에 사용** — DPI 신뢰도 문제
- [ ] **모든 UI를 단일 Canvas에 넣기** — 한 요소 변경 시 전체 재계산 발생
- [ ] **TMP에서 enableAutoSizing + ContentSizeFitter 조합** — 레이아웃 계산 충돌
- [ ] **SafeAreaPanel을 Awake()에서만 설정** — 방향 전환 시 갱신 안 됨
- [ ] **SpriteAtlas Master + Variant 둘 다 "Include in Build" 해제** — 스프라이트 투명 렌더
- [ ] **Screen.dpi를 무조건 신뢰** — 0이거나 부정확한 기기 대비 폴백 필수
- [ ] **layoutGroup 하위에서 ForceRebuildLayoutImmediate 남용** — 성능 비용 큼, MarkLayoutForRebuild + Canvas.ForceUpdateCanvases 선호

### CONSIDER (상황별)

- [ ] **방향 전환 지원 시 matchWidthOrHeight = 0.5** 고려
- [ ] **태블릿 전용 레이아웃 오브젝트**를 별도로 만들고 DeviceProfile로 전환 고려
- [ ] **Notch Solution 패키지** — `Screen.cutouts`까지 대응해야 하는 경우 (Android 구형 딥 노치)
- [ ] **AspectRatioFitter `Fit In Parent`** — 16:9 고정 콘텐츠를 다양한 비율에서 letterbox 처리할 때
- [ ] **SpriteAtlas Variant** — 저사양 기기 메모리 최적화가 필요할 때
- [ ] **UI 스케일 0.75 ~ 1.5배 범위** 정도가 실용적, 지나치게 작거나 크면 UX 파괴

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 6000.x | 테스트 기준 버전 |
| com.unity.ugui | 2.0.0 | Unity 6과 함께 배포 |
| Screen.safeArea | 2017.2+ | Android/iOS 지원 |
| Screen.cutouts | 2019.1+ | Android notch API |
| Device Simulator (통합) | 2021.2+ | 별도 패키지 설치 불필요 |
| SpriteAtlas V2 | 2022.1+ | V1과 하위 호환 |
| TextMeshPro | 4.0.0-pre (Unity 6) | 번들 포함 |
| SpriteAtlas Variant | 2018.x+ | |
| Notch Solution (써드파티) | Unity 2019+ | E7.NotchSolution |

---

## 5. 예제 코드

### 5.1 완전한 SafeAreaPanel

```csharp
using UnityEngine;

/// <summary>
/// 사용법:
/// 1. Canvas 바로 아래에 빈 GameObject "SafeAreaContainer" 생성
/// 2. RectTransform: Anchor/Pivot 모두 (0,0)~(1,1) stretch 설정
/// 3. 이 스크립트 부착
/// 4. 모든 UI 콘텐츠를 SafeAreaContainer 하위에 배치
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    [Header("엣지별 무시 설정 (배경 이미지 등 Safe Area 무시 필요 시)")]
    [SerializeField] private bool _ignoreTop    = false;
    [SerializeField] private bool _ignoreBottom = false;
    [SerializeField] private bool _ignoreLeft   = false;
    [SerializeField] private bool _ignoreRight  = false;

    private RectTransform     _rt;
    private Rect              _lastSafeArea;
    private ScreenOrientation _lastOrientation;
    private Vector2Int        _lastScreenSize;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        var currentSize = new Vector2Int(Screen.width, Screen.height);
        if (_lastSafeArea    != Screen.safeArea   ||
            _lastOrientation != Screen.orientation ||
            _lastScreenSize  != currentSize)
        {
            Apply();
        }
    }

    private void Apply()
    {
        _lastSafeArea    = Screen.safeArea;
        _lastOrientation = Screen.orientation;
        _lastScreenSize  = new Vector2Int(Screen.width, Screen.height);

        var area = _lastSafeArea;

        // 선택적 엣지 무시
        if (_ignoreLeft)   area.xMin = 0f;
        if (_ignoreBottom) area.yMin = 0f;
        if (_ignoreRight)  area.xMax = Screen.width;
        if (_ignoreTop)    area.yMax = Screen.height;

        var anchorMin = new Vector2(area.xMin / Screen.width,
                                    area.yMin / Screen.height);
        var anchorMax = new Vector2(area.xMax / Screen.width,
                                    area.yMax / Screen.height);

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
```

### 5.2 Multi-Canvas 구조

```csharp
// LifetimeScope에서 Canvas들을 명시적으로 분리
// 씬 구조:
// [Canvas - Static UI]      ← 배경, 프레임 등 변하지 않는 것
//   [Canvas - HUD]          ← HP바, 쿨다운 등 자주 갱신되는 것
//     SafeAreaContainer     ← SafeAreaPanel 부착
//       HUDView
//   [Canvas - Modal]        ← 팝업, 다이얼로그 전용 (renderOrder 높게)
//     SafeAreaContainer
//       DialogView

// CanvasScaler는 각 root Canvas마다 설정
// HUD Canvas는 PixelPerfect = false, updateRate = 30fps로 제한 가능
```

### 5.3 기기별 CanvasScaler 초기화

```csharp
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class CanvasScalerInitializer : IStartable
{
    private readonly CanvasScaler _scaler;

    [Inject]
    public CanvasScalerInitializer(CanvasScaler scaler)
    {
        _scaler = scaler;
    }

    public void Start()
    {
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        if (DeviceProfile.IsTablet)
        {
            _scaler.referenceResolution  = new Vector2(1536, 2048);
            _scaler.matchWidthOrHeight   = 0.5f;
            _scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }
        else
        {
            bool isLandscape = Screen.width > Screen.height;
            _scaler.referenceResolution = isLandscape
                ? new Vector2(1920, 1080)
                : new Vector2(1080, 1920);
            _scaler.matchWidthOrHeight  = isLandscape ? 1f : 0f;
            _scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }
    }
}
```

### 5.4 Aspect Ratio별 레이아웃 조정

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 21:9 이상 울트라와이드에서 사이드 패널을 숨기거나
/// 4:3 태블릿에서 추가 컬럼을 표시하는 패턴
/// </summary>
public class AspectRatioLayoutAdapter : MonoBehaviour
{
    [SerializeField] private GameObject _sidePanel;
    [SerializeField] private GameObject _extraColumn;
    [SerializeField] private GridLayoutGroup _grid;

    private void Start()
    {
        float ar = DeviceProfile.AspectRatio;

        // 21:9 이상 (ultra-wide)
        if (ar >= 2.1f)
        {
            _sidePanel.SetActive(true);   // 여분 공간에 사이드 패널
            _grid.constraintCount = 3;
        }
        // 16:9 ~ 21:9 표준 모바일
        else if (ar >= 1.6f)
        {
            _sidePanel.SetActive(false);
            _grid.constraintCount = 2;
        }
        // 4:3 태블릿
        else
        {
            _extraColumn.SetActive(true); // 태블릿은 컬럼 추가
            _grid.constraintCount = 3;
        }
    }
}
```

---

## 6. UI_Study 적용 계획

이 리서치를 바탕으로 다음 학습 예제를 만들 수 있다:

| 예제 번호 | 주제 | 핵심 컴포넌트 |
|---|---|---|
| 06-01 | SafeAreaPanel 구현 + Device Simulator 검증 | SafeAreaPanel, Screen.safeArea |
| 06-02 | CanvasScaler 3가지 모드 비교 씬 | CanvasScaler, 3개 Canvas |
| 06-03 | Phone vs Tablet 적응형 레이아웃 | DeviceProfile, AdaptiveLayoutSwitcher |
| 06-04 | 방향 전환 반응형 레이아웃 (R3) | OrientationResponsiveLayout, R3 |
| 06-05 | TMP 다국어 폰트 크기 동기화 | TMP_FontSizeSynchronizer |
| 06-06 | 사용자 UI Scale 설정 (접근성) | UIScaleService, PlayerPrefs |
| 06-07 | AspectRatioFitter + SpriteAtlas Variant | AspectRatioFitter, VariantAtlas |

---

## 7. 참고 자료

1. [Unity Docs - Canvas Scaler (uGUI 2.0)](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-CanvasScaler.html)
2. [Unity Docs - Designing UI for Multiple Resolutions](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/HOWTO-UIMultiResolution.html)
3. [Unity Docs - Screen.safeArea](https://docs.unity3d.com/ScriptReference/Screen-safeArea.html)
4. [Unity Docs - Variant Sprite Atlas](https://docs.unity3d.com/6000.3/Documentation/Manual/sprite/atlas/master-variant/master-variant-sprite-atlases.html)
5. [Unity Docs - Aspect Ratio Fitter](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-AspectRatioFitter.html)
6. [Unity Docs - What's New in Unity 6](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity6.html)
7. [Medium - The right way to wrap your UI inside the safe area (Farrukh Sajjad)](https://farrukhsajjad.medium.com/the-right-way-to-wrap-your-ui-inside-the-safe-area-unity-71668119f02d)
8. [Medium - Adaptive Unity UI (Kyrylo Sydorenko)](https://044developer.medium.com/adaptive-unity-ui-system-f87a29b0a66f)
9. [Medium - TextMeshPro Universal Button Font Size (Thomas Steffen)](https://thomassteffen.medium.com/textmeshpro-in-unity-universal-button-font-size-1b5ffc9efac8)
10. [Gist - SafeArea Implementation (SeanMcTex)](https://gist.github.com/SeanMcTex/c28f6e56b803cdda8ed7acb1b0db6f82)
11. [Gist - UISafeArea with edge ignore flags (luke161)](https://gist.github.com/luke161/ea44eab73891d3bcfcb79d6491f81a60)
12. [Gist - Device Orientation Manager (yasirkula)](https://gist.github.com/yasirkula/e056df5e7af1ce2de98c6372d0f26ade)
13. [GitHub - NotchSafeAreaSample (Unity Technologies)](https://github.com/Unity-Technologies/NotchSafeAreaSample)
14. [Notch Solution by Exceed7](https://exceed7.com/notch-solution/)
15. [GitHub - SafeAreaLayout (gilzoide)](https://github.com/gilzoide/unity-safe-area-layout)
16. [5argon - Demystifying Sprite Atlas Variants](https://gametorrahod.com/demystifying-sprite-atlas-variants/)
17. [Unity Docs - LayoutRebuilder.ForceRebuildLayoutImmediate](https://docs.unity3d.com/2017.3/Documentation/ScriptReference/UI.LayoutRebuilder.ForceRebuildLayoutImmediate.html)
18. [Unity Blog - Mobile UI Design Best Practices Part 1](https://unity.com/blog/games/mobile-ui-design-best-practices-part-1)
19. [Unity Discussions - Understanding Canvas Scaler Screen Match Mode](https://discussions.unity.com/t/understanding-canvas-scaler-screen-match-mode-and-reference-resolution/696551)
20. [Unity UI Performance Optimization Tips](https://unity.com/how-to/unity-ui-optimization-tips)

---

## 8. 미해결 질문

- [ ] Unity 6000.2에서 `CanvasScaler.scaleFactor`를 `Scale With Screen Size` 모드에서 런타임 변경 시 실제로 덮어쓰이는지 실기기 검증 필요
- [ ] `Screen.cutouts`(Android 구멍 뚫린 노치)의 UnityEngine.Rect[] 배열을 실제로 사용하는 구현 패턴 추가 조사 필요
- [ ] R3 `Observable.EveryUpdate()` 폴링 vs `OnRectTransformDimensionsChange()` 어느 쪽이 방향 전환 감지에 더 신뢰성 있는지 실험 필요
- [ ] SpriteAtlas Variant + Addressables 조합의 최신 안정성 (atlasRequested 버그가 Unity 6에서 수정되었는지)
- [ ] `ContentSizeFitter` + auto-size TMP 충돌 문제의 uGUI 2.0 공식 해결 여부
