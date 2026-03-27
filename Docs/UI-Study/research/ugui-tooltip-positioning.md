# Unity UGUI Tooltip Positioning Systems 리서치 리포트

- **작성일**: 2026-03-28
- **카테고리**: practice
- **상태**: 조사완료

---

## 1. 요약

Unity UGUI에서 툴팁 위치 계산의 핵심은 `RectTransformUtility.ScreenPointToLocalPointInRectangle`이며, Canvas 렌더 모드에 따라 카메라 파라미터를 달리 전달해야 한다 (Overlay → null, Camera/WorldSpace → worldCamera). 결과값은 반드시 `anchoredPosition`에 대입해야 하며 `localPosition`에 직접 대입하면 앵커 구성에 따라 오차가 발생한다. 화면 경계 클램핑은 `rectTransform.sizeDelta`와 Canvas의 `scaleFactor`를 조합하여 처리하며, ContentSizeFitter와 함께 사용할 때는 레이아웃 재빌드 타이밍 문제를 별도로 해결해야 한다.

---

## 2. 상세 분석

### 2.1 Canvas 렌더 모드별 좌표 변환

세 가지 Canvas 모드는 서로 다른 좌표 공간을 사용하므로 변환 방법이 다르다.

**Screen Space - Overlay**
- Canvas가 카메라와 독립적으로 화면 최상단에 렌더링된다.
- 스크린 좌표 = Canvas 픽셀 좌표이므로 카메라 변환이 필요 없다.
- `RectTransformUtility.ScreenPointToLocalPointInRectangle`에 `cam = null`을 전달한다.

**Screen Space - Camera**
- Canvas가 특정 카메라로부터 일정 거리에 위치한다.
- 스크린 → Canvas 변환 시 해당 카메라(`canvas.worldCamera`)를 전달해야 한다.
- null 전달 시 위치가 완전히 잘못 계산된다 (Unity 공식 포럼에서 반복 보고된 버그).
- 알려진 Unity 버그: 카메라 위치가 non-zero일 때 Play Mode 진입 시 오브젝트 위치가 변경되는 현상.

**World Space**
- Canvas가 3D 월드 공간에 존재한다.
- 이 모드에서는 `canvas.worldCamera`가 반드시 필요하다.
- 툴팁용으로 거의 사용하지 않는다.

```csharp
// 올바른 Canvas 모드별 카메라 선택
Camera GetCanvasCamera(Canvas canvas)
{
    return canvas.renderMode == RenderMode.ScreenSpaceOverlay
        ? null
        : canvas.worldCamera;
}
```

### 2.2 RectTransformUtility 사용법

**메서드 시그니처**
```csharp
public static bool ScreenPointToLocalPointInRectangle(
    RectTransform rect,    // 로컬 공간의 기준 RectTransform (보통 Canvas 루트)
    Vector2 screenPoint,   // 스크린 좌표 (픽셀 단위)
    Camera cam,            // Overlay → null, 나머지 → canvas.worldCamera
    out Vector2 localPoint // 결과: rect의 로컬 좌표
);
```

**내부 구현 (UnityCsReference 기반)**
```csharp
// cam == null일 때 ScreenPointToRay의 폴백 동작
// 스크린 좌표를 z-100 오프셋의 Ray로 변환 (직교 투영 가정)
public static Ray ScreenPointToRay(Camera cam, Vector2 screenPos)
{
    if (cam != null)
        return cam.ScreenPointToRay(screenPos);

    Vector3 pos = screenPos;
    pos.z -= 100f;
    return new Ray(pos, Vector3.forward);
}
```

이 폴백이 Overlay 모드에서 null이 올바로 동작하는 이유다. Overlay 캔버스는 사실상 직교 투영이므로 z 오프셋 후 정방향 Ray로 평면과의 교점을 구하면 스크린 좌표가 정확히 매핑된다.

**결과값 할당 주의사항**

`ScreenPointToLocalPointInRectangle`의 결과는 반드시 `anchoredPosition`에 대입해야 한다.

```csharp
// 올바른 방법
Vector2 localPoint;
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    canvasRect, mouseScreenPos, canvasCamera, out localPoint);
tooltipRect.anchoredPosition = localPoint;  // 올바름

// 잘못된 방법 — 앵커에 따라 오프셋 오차 발생
tooltipRect.localPosition = localPoint;  // 잘못됨
```

`anchoredPosition`은 앵커 참조점으로부터의 오프셋이고, `localPosition`은 부모 로컬 공간의 원점으로부터의 오프셋이다. Canvas 루트의 앵커가 중앙(0.5, 0.5)에 있을 때 두 값이 일치하지만, 다른 앵커 설정에서는 다르다.

**PointerEventData에서 카메라 얻기**
```csharp
// IPointerEnterHandler 구현 내에서
public void OnPointerEnter(PointerEventData eventData)
{
    // enterEventCamera가 Canvas 모드를 자동으로 처리
    Camera cam = eventData.enterEventCamera; // Overlay 시 null 반환
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        canvasRect,
        eventData.position,
        cam,
        out Vector2 localPoint);
    tooltipRect.anchoredPosition = localPoint;
}
```

### 2.3 화면 경계 클램핑 (Edge Clamping)

툴팁이 화면 밖으로 나가지 않도록 클램핑하는 방법은 두 가지다.

**방법 A: anchoredPosition 기반 클램핑**

Canvas가 항상 화면 전체를 커버할 때 유효하다. 툴팁의 RectTransform이 Canvas 루트의 직계 자식일 때 anchoredPosition이 곧 Canvas 로컬 좌표이므로, Canvas 크기의 절반을 기준으로 클램핑한다.

```csharp
void ClampToScreen(RectTransform tooltipRect, RectTransform canvasRect, Vector2 proposedPos)
{
    Vector2 tooltipSize = tooltipRect.sizeDelta;
    Vector2 canvasSize = canvasRect.sizeDelta;

    // Canvas 중앙이 (0,0)이고 툴팁 pivot이 (0,0)일 때
    float minX = -canvasSize.x * 0.5f;
    float maxX =  canvasSize.x * 0.5f - tooltipSize.x;
    float minY = -canvasSize.y * 0.5f;
    float maxY =  canvasSize.y * 0.5f - tooltipSize.y;

    tooltipRect.anchoredPosition = new Vector2(
        Mathf.Clamp(proposedPos.x, minX, maxX),
        Mathf.Clamp(proposedPos.y, minY, maxY)
    );
}
```

**방법 B: 스크린 좌표 기반 클램핑 후 변환**

```csharp
void PositionTooltip(Vector2 mouseScreenPos)
{
    float scaleFactor = canvas.scaleFactor;
    Vector2 tooltipSize = tooltipRect.sizeDelta * scaleFactor; // 실제 픽셀 크기

    // 스크린 좌표 기준 클램핑
    float clampedX = Mathf.Clamp(mouseScreenPos.x, 0, Screen.width  - tooltipSize.x);
    float clampedY = Mathf.Clamp(mouseScreenPos.y, 0, Screen.height - tooltipSize.y);

    // 클램핑된 스크린 좌표를 캔버스 로컬로 변환
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        canvasRect,
        new Vector2(clampedX, clampedY),
        GetCanvasCamera(canvas),
        out Vector2 localPoint);
    tooltipRect.anchoredPosition = localPoint;
}
```

**방법 C: Pivot 동적 전환 (Code Monkey 패턴)**

클램핑 대신 pivot을 바꿔서 툴팁이 마우스 커서의 반대 방향에 나타나도록 한다. 가장 자연스러운 UX를 제공한다.

```csharp
void PositionWithPivotFlip(Vector2 mouseScreenPos)
{
    // 화면의 어느 사분면에 커서가 있는지 판단
    bool isRight = mouseScreenPos.x > Screen.width  * 0.5f;
    bool isTop   = mouseScreenPos.y > Screen.height * 0.5f;

    // pivot 설정: 툴팁이 커서의 반대편에 오도록
    tooltipRect.pivot = new Vector2(
        isRight ? 1f : 0f,
        isTop   ? 1f : 0f
    );

    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        canvasRect, mouseScreenPos,
        GetCanvasCamera(canvas),
        out Vector2 localPoint);
    tooltipRect.anchoredPosition = localPoint;
}
```

pivot (1,1)이면 툴팁의 우상단이 커서 위치에 정렬되므로 툴팁 본체는 좌하단으로 나타나고, pivot (0,0)이면 툴팁의 좌하단이 정렬되므로 본체가 우상단으로 나타난다.

### 2.4 Anchor/Pivot 전략

**툴팁 패널 권장 설정**

| 항목 | 값 | 이유 |
|------|-----|------|
| Anchor Min/Max | (0.5, 0.5) 또는 (0,0) | Canvas 중앙 고정 앵커 사용 시 anchoredPosition이 직관적 |
| Pivot | (0, 0) 기본 | 커서 위치가 툴팁의 좌하단 모서리 = 오른쪽 위로 열림 |
| Pivot (동적) | 사분면에 따라 전환 | 화면 경계 자동 처리 |

**Canvas 루트 앵커 설정 중요성**

툴팁이 Canvas 루트의 직계 자식일 때, Canvas 루트의 앵커가 (0.5, 0.5)이어야 anchoredPosition 클램핑 수식이 단순해진다. 앵커가 (0,0)이면 수식에서 `canvasSize * 0.5f` 오프셋 보정이 불필요하지만, 일관성을 위해 Canvas 루트의 앵커를 Stretch(0,0 ~ 1,1)로 두고 중앙 pivot을 사용하는 것이 일반적이다.

**부모가 Canvas 루트가 아닌 경우**

툴팁이 Canvas 루트의 직계 자식이 아니라 다른 패널 아래에 있으면 `ScreenPointToLocalPointInRectangle`에 Canvas 루트 대신 **툴팁의 부모 RectTransform**을 첫 번째 인자로 전달해야 한다.

```csharp
// 툴팁이 'parentPanel' 아래에 있을 때
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    tooltipRect.parent as RectTransform,  // Canvas 루트 아님
    mouseScreenPos, cam, out Vector2 localPoint);
tooltipRect.anchoredPosition = localPoint;
```

### 2.5 Canvas ScaleMode 고려사항

**Constant Pixel Size**
- Canvas 픽셀 = 스크린 픽셀 (scaleFactor = 1)
- `ScreenPointToLocalPointInRectangle` 결과를 그대로 사용해도 맞음

**Scale with Screen Size**
- `canvas.scaleFactor`가 1이 아닌 값이 됨
- `RectTransformUtility`를 사용하면 이 스케일을 자동으로 처리한다 (내부적으로 레이캐스팅 기반이므로)
- **문제가 생기는 경우**: 수동으로 스크린 좌표를 anchoredPosition으로 변환하려 할 때
  ```csharp
  // 잘못된 방법 — scaleFactor 미반영
  tooltipRect.anchoredPosition = Mouse.current.position.ReadValue();

  // 올바른 방법 — scaleFactor 반영
  Vector2 mousePos = Mouse.current.position.ReadValue();
  tooltipRect.anchoredPosition = mousePos / canvas.scaleFactor;

  // 가장 올바른 방법 — RectTransformUtility 사용
  RectTransformUtility.ScreenPointToLocalPointInRectangle(
      canvasRect, mousePos, GetCanvasCamera(canvas), out Vector2 local);
  tooltipRect.anchoredPosition = local;
  ```

- 클램핑 시 `tooltipRect.sizeDelta`는 Canvas 로컬 단위이므로, 스크린 픽셀로 비교하려면 `* scaleFactor`를 곱해야 한다.

**CanvasPositioningExtensions (FlaShG Gist) 방식**
```csharp
// viewport 경유 수동 변환 — Scale with Screen Size에서도 동작
public static Vector3 ScreenToCanvasPosition(this Canvas canvas, Vector3 screenPos)
{
    var viewportPos = new Vector3(
        screenPos.x / Screen.width,
        screenPos.y / Screen.height, 0);

    var canvasRect = canvas.GetComponent<RectTransform>();
    var centerBased = viewportPos - new Vector3(0.5f, 0.5f, 0);
    return Vector3.Scale(centerBased, canvasRect.sizeDelta);
}
// 결과를 anchoredPosition에 대입
tooltipRect.anchoredPosition = canvas.ScreenToCanvasPosition(mousePos);
```

이 방법은 Canvas의 sizeDelta (= 레퍼런스 해상도 기준 크기)를 직접 사용하므로 Scale with Screen Size와 무관하게 동작한다.

### 2.6 New Input System 마우스 위치

**권장 방법**

```csharp
using UnityEngine.InputSystem;

// 방법 1: Mouse.current (직접 디바이스 폴링)
Vector2 mousePos = Mouse.current.position.ReadValue();

// 방법 2: Pointer.current (마우스/터치 추상화)
Vector2 pointerPos = Pointer.current.position.ReadValue();
```

`Mouse.current.position.ReadValue()`는 Unity의 `Input.mousePosition`과 동일한 스크린 좌표를 반환한다 (좌하단 원점, 픽셀 단위). 따라서 `RectTransformUtility.ScreenPointToLocalPointInRectangle`에 그대로 전달할 수 있다.

**주의사항**

- `Mouse.current`는 마우스가 없을 때 null일 수 있다. 터치 디바이스 고려 시 `Pointer.current` 사용을 권장한다.
- IPointerEnterHandler/IPointerExitHandler 안에서는 `eventData.position`을 사용하는 것이 가장 안전하다 (EventSystem이 올바른 좌표를 보장).
- `Mouse.current.position.ReadValue()`가 항상 (0,0)을 반환하는 경우: PlayerInput 컴포넌트가 없거나 InputSystemUIInputModule이 설정되지 않은 경우이므로 EventSystem의 Input Module을 확인해야 한다.

**EventSystem 통합 패턴 (권장)**

```csharp
// IPointerMoveHandler 없이 Update에서 마우스 위치 추적
void Update()
{
    if (Mouse.current == null) return;
    Vector2 mousePos = Mouse.current.position.ReadValue();

    // IPointerEnterHandler가 show 트리거, 여기서는 위치만 업데이트
    if (_tooltipVisible)
        UpdateTooltipPosition(mousePos);
}
```

---

## 3. 베스트 프랙티스

### DO (권장)
- [x] `RectTransformUtility.ScreenPointToLocalPointInRectangle`으로 좌표 변환
- [x] 변환 결과를 `anchoredPosition`에 대입 (localPosition 금지)
- [x] Canvas 모드에 따라 카메라 파라미터 분기 (`canvas.renderMode == ScreenSpaceOverlay ? null : canvas.worldCamera`)
- [x] `eventData.enterEventCamera`를 활용하여 카메라 파라미터 자동 처리
- [x] 툴팁 패널의 Raycast Target을 false로 설정 (호버 이벤트 차단 방지)
- [x] `LateUpdate`에서 위치 업데이트 (Update 이후 기타 오브젝트 이동 완료 보장)
- [x] 화면 경계 클램핑 시 `canvas.scaleFactor`로 픽셀↔Canvas 단위 변환
- [x] ContentSizeFitter 사용 시 `LayoutRebuilder.ForceRebuildLayoutImmediate` 선 호출 후 위치 결정
- [x] 툴팁이 Canvas 루트 직계 자식인지 확인 후 `ScreenPointToLocalPointInRectangle` 첫 인자 결정

### DON'T (금지)
- [ ] Screen Space - Camera 모드에서 카메라 파라미터로 null 전달
- [ ] `localPosition`에 `ScreenPointToLocalPointInRectangle` 결과 직접 대입
- [ ] Scale with Screen Size일 때 마우스 스크린 좌표를 그대로 anchoredPosition에 대입
- [ ] 툴팁 패널에 Raycast Target 활성화 (호버 감지 방해)
- [ ] ContentSizeFitter 크기 확정 전에 경계 클램핑 계산
- [ ] `UnityEngine.Input.mousePosition` 사용 (New Input System 프로젝트)
- [ ] 툴팁을 Canvas 루트가 아닌 부모 기준으로 위치 계산할 때 Canvas 루트를 첫 인자로 전달

### CONSIDER (상황별)
- [ ] Pivot 동적 전환: 자연스러운 UX, 수식이 복잡해지는 트레이드오프
- [ ] `CanvasPositioningExtensions` 헬퍼: Scale with Screen Size 수동 처리 필요 시
- [ ] `ScreenPointToWorldPointInRectangle` + `transform.position` 방식: WorldSpace Canvas 또는 3D 오브젝트 기반 툴팁

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|------|------|------|
| Unity | 2022.3 LTS+ / 6000.x | RectTransformUtility API 안정 |
| Input System | 1.7+ | Mouse.current.position.ReadValue() |
| UGUI 패키지 | 1.0 ~ 2.0 | ScreenPointToLocalPointInRectangle 동일 동작 |
| LayoutRebuilder | 모든 버전 | ForceRebuildLayoutImmediate 주의사항 동일 |

---

## 5. 예제 코드

### 기본 툴팁 위치 업데이트 (모든 Canvas 모드 지원)

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TooltipView : MonoBehaviour
{
    [SerializeField] RectTransform _tooltipRect;
    [SerializeField] Canvas _canvas;

    RectTransform _canvasRect;

    void Awake()
    {
        _canvasRect = _canvas.GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!gameObject.activeSelf) return;
        if (Mouse.current == null) return;

        UpdatePosition(Mouse.current.position.ReadValue());
    }

    void UpdatePosition(Vector2 screenPos)
    {
        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, cam, out Vector2 localPoint);

        _tooltipRect.anchoredPosition = ClampToCanvas(localPoint);
    }

    Vector2 ClampToCanvas(Vector2 proposedPos)
    {
        Vector2 tooltipSize = _tooltipRect.sizeDelta;
        Vector2 canvasSize  = _canvasRect.sizeDelta;

        // Canvas 중앙 기준 (anchorMin=anchorMax=0.5 가정)
        float halfW = canvasSize.x * 0.5f;
        float halfH = canvasSize.y * 0.5f;

        return new Vector2(
            Mathf.Clamp(proposedPos.x, -halfW, halfW - tooltipSize.x),
            Mathf.Clamp(proposedPos.y, -halfH, halfH - tooltipSize.y)
        );
    }
}
```

### ContentSizeFitter가 있는 동적 크기 툴팁 (레이아웃 타이밍 처리)

```csharp
public void ShowTooltip(string text, Vector2 screenPos)
{
    _tooltipText.text = text;

    // 1단계: 레이아웃을 즉시 강제 재계산 (sizeDelta 확정)
    LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);

    // 2단계: 크기 확정 후 위치 계산 및 클램핑
    UpdatePosition(screenPos);

    gameObject.SetActive(true);
}
```

**중첩 LayoutGroup이 있는 경우** (예: VerticalLayoutGroup + ContentSizeFitter):

```csharp
// 자식 → 부모 순서로 재빌드 필요
LayoutRebuilder.ForceRebuildLayoutImmediate(_innerRect);
LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);
UpdatePosition(screenPos);
```

### Pivot 동적 전환 패턴 (edge-aware)

```csharp
void UpdatePositionWithPivotFlip(Vector2 screenPos)
{
    bool rightHalf = screenPos.x > Screen.width  * 0.5f;
    bool topHalf   = screenPos.y > Screen.height * 0.5f;

    _tooltipRect.pivot = new Vector2(
        rightHalf ? 1f : 0f,
        topHalf   ? 1f : 0f
    );

    Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
        ? null : _canvas.worldCamera;

    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _canvasRect, screenPos, cam, out Vector2 local);

    _tooltipRect.anchoredPosition = local;
}
```

### IPointerEnterHandler 통합 패턴

```csharp
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TooltipView _tooltipView;
    [SerializeField] string _tooltipText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // enterEventCamera가 Canvas 모드를 자동 처리
        _tooltipView.ShowAt(_tooltipText, eventData.position, eventData.enterEventCamera);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltipView.Hide();
    }
}
```

```csharp
// TooltipView 측 — camera를 외부에서 주입받을 때
public void ShowAt(string text, Vector2 screenPos, Camera eventCamera)
{
    _tooltipText.text = text;
    LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);

    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _canvasRect, screenPos, eventCamera, out Vector2 local);

    _tooltipRect.anchoredPosition = ClampToCanvas(local);
    gameObject.SetActive(true);
}
```

### Scale with Screen Size 수동 변환 헬퍼 (대안)

```csharp
// RectTransformUtility를 사용하지 않는 대안 (Canvas 루트가 중앙 앵커일 때)
Vector2 ScreenToCanvasLocal(Canvas canvas, Vector2 screenPos)
{
    RectTransform canvasRect = canvas.GetComponent<RectTransform>();
    Vector2 viewportPos = new Vector2(
        screenPos.x / Screen.width,
        screenPos.y / Screen.height);

    Vector2 centerBased = viewportPos - Vector2.one * 0.5f;
    return Vector2.Scale(centerBased, canvasRect.sizeDelta);
}
```

---

## 6. 일반적인 버그와 함정

### 버그 1: Screen Space Camera에서 카메라 null 전달
**증상**: 툴팁이 화면 중앙이나 엉뚱한 위치에 고정됨
**원인**: `ScreenPointToLocalPointInRectangle(canvasRect, pos, null, out local)`
**해결**: `canvas.worldCamera` 전달

### 버그 2: localPosition 대입
**증상**: 앵커 설정에 따라 툴팁 위치 오차 발생, 특히 앵커가 (0.5, 0.5)가 아닐 때
**원인**: `tooltipRect.localPosition = localPoint`
**해결**: `tooltipRect.anchoredPosition = localPoint`

### 버그 3: Scale with Screen Size에서 수동 좌표 대입
**증상**: 툴팁이 마우스 위치와 어긋남 (scaleFactor만큼 오차)
**원인**: `tooltipRect.anchoredPosition = Mouse.current.position.ReadValue()`
**해결**: `RectTransformUtility` 사용 또는 `/ canvas.scaleFactor` 나누기

### 버그 4: 툴팁 자체가 Raycast Target을 차단
**증상**: 마우스를 툴팁 위로 이동하면 OnPointerExit가 바로 발생, 툴팁이 깜빡임
**원인**: 툴팁 패널의 Image/Text에 Raycast Target이 활성화됨
**해결**: 툴팁 구성 오브젝트 전체의 Raycast Target을 false로 설정

### 버그 5: ContentSizeFitter 크기 지연
**증상**: 툴팁 첫 표시 시 경계 클램핑이 잘못 계산됨 (크기가 이전 프레임 값)
**원인**: ContentSizeFitter가 다음 레이아웃 패스에 크기를 업데이트하므로, 위치 계산 시점에 sizeDelta가 구 값
**해결**: `LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect)` 호출 후 위치 계산

### 버그 6: 부모가 Canvas 루트가 아닌 경우 잘못된 첫 번째 인자
**증상**: 툴팁이 예상 위치와 다름
**원인**: `ScreenPointToLocalPointInRectangle`에 Canvas 루트 대신 다른 RectTransform을 전달하거나, 그 반대
**해결**: 툴팁의 부모 RectTransform을 첫 인자로 전달 (부모 로컬 공간에서 anchoredPosition 계산되므로)

### 버그 7: IPointerMoveHandler 미지원 (구버전)
**증상**: 마우스를 UI 요소 위에서 움직여도 툴팁 위치가 업데이트되지 않음
**원인**: 구버전 EventSystem에서 IPointerMoveHandler 미지원
**해결**: Update/LateUpdate에서 Mouse.current.position으로 위치 업데이트

---

## 7. UI_Study 적용 계획

기존 UI_Study 05씬 Tooltip 씬을 기준으로 다음 개선 예제를 설계할 수 있다.

1. **예제 01**: Overlay/Camera/WorldSpace 모드 전환하며 동일 코드로 툴팁이 작동하는지 확인
2. **예제 02**: ContentSizeFitter 동적 크기 + ForceRebuildLayoutImmediate 타이밍 데모
3. **예제 03**: Pivot 동적 전환 vs anchoredPosition 클램핑 두 방식 비교
4. **예제 04**: Scale with Screen Size 1280x720에서 수동 변환 vs RectTransformUtility 비교

---

## 8. 참고 자료

1. [Unity Docs - RectTransformUtility.ScreenPointToLocalPointInRectangle](https://docs.unity3d.com/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html)
2. [UnityCsReference - RectTransformUtility.cs (실제 구현 코드)](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UI/ScriptBindings/RectTransformUtility.cs)
3. [Unity Discussions - RectTransformUtility 올바른 사용법](https://discussions.unity.com/t/how-to-use-correctly-recttransformutility-screenpointtolocalpointinrectangle/683988)
4. [Unity Discussions - RectTransformUtility 이슈](https://discussions.unity.com/t/issues-with-recttransformutility-screenpointtolocalpointinrectangle/643495)
5. [Unity Discussions - ScreenSpaceCamera Tooltip 좌표 문제](https://discussions.unity.com/t/screenspace-camera-tooltip-controller-sweat-and-tears.293991)
6. [GitHub Gist - CanvasPositioningExtensions (FlaShG)](https://gist.github.com/FlaShG/ac3afac0ef65d98411401f2b4d8a43a5)
7. [Code Monkey - Dynamic Tooltip with Edge Detection (영상)](https://unitycodemonkey.com/video.php?v=YUIohCXt_pc)
8. [GitHub - perezromeojohn/unity-tooltips (Screen clamping 구현)](https://github.com/perezromeojohn/unity-tooltips)
9. [GitHub - sinbad/unity-tooltip (DoTween + WorldPointInRectangle 방식)](https://gist.github.com/sinbad/7204edaba7453e48c8c1e837505d054d)
10. [Unity Docs - Canvas ScaleMode](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-CanvasScaler.html)
11. [Unity Docs - IPointerEnterHandler](https://docs.unity3d.com/2017.4/Documentation/ScriptReference/EventSystems.IPointerEnterHandler.html)
12. [Unity Discussions - ForceRebuildLayoutImmediate](https://discussions.unity.com/t/force-immediate-layout-update/608126)
13. [PixelEuphoria - RectTransform 변수 심층 분석](https://pixeleuphoria.com/blog/index.php/2020/05/10/unity-tip-recttransform/)
14. [uGUI Input Issues 가이드](https://unity.huh.how/ugui/input-issues.html)
15. [Anja Haumann - Unity UI Space Conversion](https://anja-haumann.de/unity-how-to-ui-space-conversion/)

---

## 9. 미해결 질문

- [ ] `ScreenPointToWorldPointInRectangle` vs `ScreenPointToLocalPointInRectangle` 중 어느 시나리오에서 전자가 더 적합한가?
- [ ] WorldSpace Canvas에서 툴팁을 월드 오브젝트에 부착할 때의 추가 좌표 변환 흐름 확인 필요
- [ ] DOTween 페이드 애니메이션 중 위치 업데이트 시 Sequence 취소 타이밍 베스트 프랙티스
- [ ] `IPointerMoveHandler`가 New Input System + InputSystemUIInputModule 조합에서 정상 동작하는지 확인
