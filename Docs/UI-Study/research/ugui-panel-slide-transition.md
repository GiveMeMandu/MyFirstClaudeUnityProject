# UGUI 패널 슬라이드 트랜지션 — 클리핑, 캐러셀, 깜빡임 방지

- **작성일**: 2026-03-28
- **카테고리**: practice
- **상태**: 조사완료
- **관련 스택**: DOTween + UniTask + RectTransform + RectMask2D

---

## 1. 요약

UGUI 패널 슬라이드 트랜지션에서 클리핑은 RectMask2D를 Viewport 컨테이너에 붙이는 방식이 사실상 표준이다.
DOTween Sequence에서 `Join`으로 두 패널을 동시에 움직이며, 슬라이드 거리는 **부모 컨테이너 rect.width**를 기준으로 계산한다.
`SetActive(false)` 호출은 애니메이션 완료 콜백 안에서만 해야 하며, 전환 중에는 CanvasGroup.interactable/blocksRaycasts로 입력을 막는 것이 플리커링 없는 패턴이다.

---

## 2. 상세 분석

### 2.1 클리핑 — RectMask2D vs Mask

| 항목 | RectMask2D | Mask |
|---|---|---|
| 클리핑 형태 | 사각형만 | 임의 형태(Sprite) |
| 스텐실 버퍼 | 미사용 | 사용 (드로우콜 +2) |
| 추가 드로우콜 | 없음 | 2개 (마스크 앞/뒤) |
| 자식 외부 컬링 | 지원 (배칭 효율↑) | 미지원 |
| 3D 회전 패널 | 비정상 작동 | 정상 |
| Canvas 비활성 시 | **계속 실행됨** (성능 주의) | 정상 비활성화 |

**패널 슬라이드 트랜지션에서의 결론**:
- 사각형 뷰포트 클리핑 → **RectMask2D 사용**
- 단, Canvas 비활성화만으로는 RectMask2D가 꺼지지 않는다. Canvas를 끄는 동시에 RectMask2D도 수동으로 비활성화해야 한다.

```csharp
// RectMask2D가 있는 Canvas를 끌 때 반드시 함께 비활성화
public void SetViewportActive(bool active)
{
    _viewportCanvas.enabled = active;
    _viewportMask.enabled = active;   // RectMask2D 명시적 끄기
}
```

### 2.2 캐러셀 슬라이드 계층 구조

```
Canvas
└── Viewport (RectMask2D 부착, 전체 화면 크기)
    ├── PanelA (현재 패널)
    └── PanelB (대기 패널 — 처음엔 Viewport 밖에 위치)
```

- **Viewport** 는 보이는 영역을 정의하고 클리핑 담당
- **PanelA/B** 는 Viewport 내부에서 anchoredPosition으로 이동
- Viewport 밖으로 나간 패널은 RectMask2D가 자동 컬링

### 2.3 슬라이드 거리 계산 — 부모 컨테이너 rect.width 기준

```csharp
// Viewport(부모 컨테이너)의 rect.width를 슬라이드 거리로 사용
// Screen.width나 패널 자체 width가 아닌 컨테이너 기준이어야 해상도 독립적
float slideDistance = ((RectTransform)_viewport.transform).rect.width;
```

**왜 부모 rect.width인가?**
- Screen.width: 픽셀 단위, Canvas Scale에 따라 오차 발생
- 패널 sizeDelta.x: 패널이 stretch 앵커이면 0 반환
- 부모 rect.width: Canvas 좌표계의 실제 논리 폭, 항상 정확

**Canvas.ForceUpdateCanvases 주의사항**:
```csharp
// Start/Awake에서 rect.width는 아직 계산 전일 수 있다.
// Canvas.ForceUpdateCanvases() 또는 yield return null 이후에 읽어야 한다.
private void Start()
{
    Canvas.ForceUpdateCanvases();
    _slideDistance = _viewportRect.rect.width;
}
```

### 2.4 패널 비활성화와 깜빡임

**깜빡임의 원인**:
1. 애니메이션 시작 전에 `SetActive(true)` → 프레임 1회 원위치에서 렌더링
2. 애니메이션 완료 전에 `SetActive(false)` → 마지막 프레임 점프
3. DOTween Kill 없이 중복 트윈 실행

**올바른 패턴**:
```csharp
// SetActive(true)는 anchoredPosition 초기화 이후에 호출
// SetActive(false)는 반드시 OnComplete 콜백 내에서만 호출
public async UniTask SlideIn(RectTransform panel, float distance, CancellationToken ct)
{
    // 1. 보이기 전에 위치 먼저 설정
    panel.anchoredPosition = new Vector2(distance, 0f);
    panel.gameObject.SetActive(true);

    // 2. 이제 트윈 실행
    await panel.DOAnchorPos(Vector2.zero, _duration)
        .SetEase(Ease.OutCubic)
        .AsyncWaitForCompletion()
        .AsUniTask()
        .AttachExternalCancellation(ct);
}

public async UniTask SlideOut(RectTransform panel, float distance, CancellationToken ct)
{
    // 트윈 완료 후 SetActive(false)
    await panel.DOAnchorPos(new Vector2(-distance, 0f), _duration)
        .SetEase(Ease.InCubic)
        .AsyncWaitForCompletion()
        .AsUniTask()
        .AttachExternalCancellation(ct);

    panel.gameObject.SetActive(false);
}
```

**중복 트윈 방지**:
```csharp
// 새 트윈 시작 전 항상 이전 트윈 Kill
panel.DOKill();
panel.DOAnchorPos(...);
```

### 2.5 전환 중 입력 차단 — CanvasGroup 패턴

패널을 off-screen으로 이동만 시키고 `SetActive(false)`를 하지 않는 경우, 보이지 않는 패널에 레이캐스트가 여전히 적중할 수 있다.

```csharp
// 전환 시작 시 입력 차단
_canvasGroup.interactable = false;
_canvasGroup.blocksRaycasts = false;

// 전환 완료 시 복원 (들어오는 패널에만)
_canvasGroup.interactable = true;
_canvasGroup.blocksRaycasts = true;
```

### 2.6 SetActive vs 위치 이동 유지 — 언제 뭘 써야 하나

| 방식 | 장점 | 단점 |
|---|---|---|
| `SetActive(false)` (완료 후) | 렌더링 비용 제거, 가비지 최소 | Awake/Start 재호출 없음 (한번만 호출됨) |
| 위치 유지 (off-screen) | 빠른 재진입 (위치만 이동) | Raycast 차단 필요, 렌더링 비용 존재 |
| `CanvasGroup.alpha=0` + `blocksRaycasts=false` | 렌더링은 남지만 투명, Raycast 차단 | GPU overdraw는 존재 |

**권장**: 캐러셀처럼 자주 전환되는 패널은 off-screen 위치 유지 + CanvasGroup 차단. 드물게 열리는 팝업은 SetActive 패턴.

---

## 3. 베스트 프랙티스

### DO (권장)
- [x] Viewport 컨테이너에만 RectMask2D 부착 (패널 자체에는 붙이지 않음)
- [x] 슬라이드 거리는 `_viewportRect.rect.width`로 계산
- [x] `SetActive(true)` 전에 anchoredPosition 초기화 완료
- [x] `SetActive(false)`는 항상 `OnComplete` / `await` 완료 이후
- [x] 새 트윈 시작 전 `panel.DOKill()` 호출
- [x] DOTween Sequence의 `Join()`으로 두 패널을 동시 이동
- [x] Canvas 비활성화 시 RectMask2D도 명시적으로 비활성화
- [x] `Canvas.ForceUpdateCanvases()` 후 rect.width 읽기

### DON'T (금지)
- [ ] `SetActive(true)` 후 위치 설정 (1프레임 깜빡임 발생)
- [ ] 트윈 도중 `SetActive(false)` 호출 (위치 점프)
- [ ] Screen.width를 슬라이드 거리로 직접 사용 (Canvas Scale 오차)
- [ ] Canvas 비활성화만으로 RectMask2D가 꺼진다고 가정
- [ ] 중복 트윈 없이 새 트윈 시작 (DOKill 미호출)

### CONSIDER (상황별)
- [ ] 3D 회전 트랜지션이 필요하면 RectMask2D 대신 Mask 컴포넌트 검토
- [ ] 자주 토글되는 캐러셀은 SetActive 대신 위치 유지 + CanvasGroup 조합
- [ ] 전환 중 이전 트윈 인터럽트가 필요하면 Sequence 대신 개별 트윈 Kill 패턴

---

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 6000.x (LTS) | uGUI 2.0+ 권장 |
| DOTween | 1.2.765+ | DOTweenModuleUI 필수 |
| UniTask | 2.5.x | AsyncWaitForCompletion().AsUniTask() |
| RectMask2D | uGUI 1.0+ | Canvas 비활성 시 성능 버그 (수동 끄기 필요) |

---

## 5. 예제 코드

### 5.1 캐러셀 슬라이드 컨트롤러 — DOTween Sequence 패턴

```csharp
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

public sealed class CarouselSlideController : MonoBehaviour
{
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private float _duration = 0.35f;
    [SerializeField] private Ease _ease = Ease.OutCubic;

    private float _slideDistance;
    private RectTransform _currentPanel;
    private CancellationTokenSource _cts;

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        _slideDistance = _viewport.rect.width;
    }

    // 패널 A 왼쪽으로 퇴장, 패널 B 오른쪽에서 진입
    public async UniTask SlideNext(RectTransform incoming, CancellationToken ct = default)
    {
        if (_currentPanel == null)
        {
            incoming.anchoredPosition = Vector2.zero;
            incoming.gameObject.SetActive(true);
            _currentPanel = incoming;
            return;
        }

        var outgoing = _currentPanel;
        _currentPanel = incoming;

        // 중복 트윈 방지
        outgoing.DOKill();
        incoming.DOKill();

        // incoming을 오른쪽 밖에 배치하고 활성화
        incoming.anchoredPosition = new Vector2(_slideDistance, 0f);
        incoming.gameObject.SetActive(true);

        // CanvasGroup으로 상호작용 차단
        var outCG = outgoing.GetComponent<CanvasGroup>();
        var inCG = incoming.GetComponent<CanvasGroup>();
        if (outCG != null) { outCG.interactable = false; outCG.blocksRaycasts = false; }
        if (inCG != null) { inCG.interactable = false; inCG.blocksRaycasts = false; }

        // 두 패널 동시 이동
        var seq = DOTween.Sequence()
            .Join(outgoing.DOAnchorPos(new Vector2(-_slideDistance, 0f), _duration).SetEase(_ease))
            .Join(incoming.DOAnchorPos(Vector2.zero, _duration).SetEase(_ease));

        await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);

        // 완료 후 정리
        outgoing.gameObject.SetActive(false);

        if (inCG != null) { inCG.interactable = true; inCG.blocksRaycasts = true; }
    }

    // 패널 B 오른쪽으로 퇴장, 패널 A 왼쪽에서 진입
    public async UniTask SlidePrev(RectTransform incoming, CancellationToken ct = default)
    {
        if (_currentPanel == null)
        {
            incoming.anchoredPosition = Vector2.zero;
            incoming.gameObject.SetActive(true);
            _currentPanel = incoming;
            return;
        }

        var outgoing = _currentPanel;
        _currentPanel = incoming;

        outgoing.DOKill();
        incoming.DOKill();

        incoming.anchoredPosition = new Vector2(-_slideDistance, 0f);
        incoming.gameObject.SetActive(true);

        var outCG = outgoing.GetComponent<CanvasGroup>();
        var inCG = incoming.GetComponent<CanvasGroup>();
        if (outCG != null) { outCG.interactable = false; outCG.blocksRaycasts = false; }
        if (inCG != null) { inCG.interactable = false; inCG.blocksRaycasts = false; }

        var seq = DOTween.Sequence()
            .Join(outgoing.DOAnchorPos(new Vector2(_slideDistance, 0f), _duration).SetEase(_ease))
            .Join(incoming.DOAnchorPos(Vector2.zero, _duration).SetEase(_ease));

        await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);

        outgoing.gameObject.SetActive(false);

        if (inCG != null) { inCG.interactable = true; inCG.blocksRaycasts = true; }
    }
}
```

### 5.2 RectMask2D + Canvas 동시 비활성화 유틸리티

```csharp
// Canvas만 끄면 RectMask2D가 계속 퍼포먼스 클리핑 연산을 수행한다.
// 반드시 함께 끈다.
public static class ViewportExtensions
{
    public static void SetViewportEnabled(this Canvas canvas, bool enabled)
    {
        canvas.enabled = enabled;
        var masks = canvas.GetComponentsInChildren<RectMask2D>(includeInactive: true);
        foreach (var m in masks) m.enabled = enabled;
    }
}
```

### 5.3 슬라이드 거리 안전 계산 — Canvas 준비 대기

```csharp
private async UniTask<float> GetSlideDistanceAsync(RectTransform viewport, CancellationToken ct)
{
    // Start/Awake 직후 rect.width가 0일 수 있으므로 한 프레임 대기
    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, ct);
    Canvas.ForceUpdateCanvases();
    return viewport.rect.width;
}
```

### 5.4 DOTween Sequence 주요 API 요약

```csharp
Sequence seq = DOTween.Sequence();

// Append: 이전 트윈 완료 후 순차
seq.Append(panelA.DOAnchorPos(...));

// Join: 마지막 Append와 동시 실행 (캐러셀에서 핵심)
seq.Join(panelB.DOAnchorPos(...));

// Insert: 특정 시간에 삽입 (다른 Append/Join 무관)
seq.Insert(0.1f, overlay.DOFade(0f, 0.1f));

// AppendInterval: 딜레이
seq.AppendInterval(0.05f);

// AppendCallback: 완료 콜백 (SetActive(false) 위치)
seq.AppendCallback(() => panelA.gameObject.SetActive(false));

// UniTask 연동
await seq.AsyncWaitForCompletion().AsUniTask();

// RectTransform 전용 트윈
panelA.DOAnchorPos(Vector2.zero, 0.35f);       // anchoredPosition
panelA.DOAnchorPosX(-500f, 0.35f);             // X축만
panelA.DOAnchorPosY(0f, 0.35f);               // Y축만
```

---

## 6. UI_Study 적용 계획

이 리서치를 기반으로 다음 UI_Study 예제를 구성할 수 있다:

- **예제 A**: 기본 캐러셀 — 좌우 화살표 버튼으로 3개 패널을 슬라이드
  - 계층: Canvas > Viewport (RectMask2D) > Panel[0..2]
  - SlideNext / SlidePrev 구현

- **예제 B**: 탭 UI — 탭 버튼 클릭 시 대응 패널 슬라이드 진입
  - R3의 ReactiveProperty<int>로 현재 탭 인덱스 관리
  - 방향 자동 결정 (이전 인덱스 비교)

- **예제 C**: 전환 인터럽트 처리 — 애니메이션 중 빠르게 다른 탭 클릭 시
  - CancellationTokenSource로 이전 전환 취소
  - DOKill로 현재 위치에서 즉시 새 방향으로 전환

---

## 7. 참고 자료

1. [RectMask2D — Unity uGUI 2.0 Docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-RectMask2D.html)
2. [Creating Screen Transitions — Unity UI 1.0 Docs](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/HOWTO-UIScreenTransition.html)
3. [Panel Animation in Unity using DOTween — Yarsa DevBlog](https://blog.yarsalabs.com/dotween-unity-animation-part3/)
4. [DOTween Documentation — demigiant.com](https://dotween.demigiant.com/documentation.php)
5. [Fighting Entropy: The Hidden Performance Killer RectMask2D — Medium](https://medium.com/@galbartouv/fighting-entropy-in-unity-the-hidden-performance-killer-rectmask2d-1f81e30c1a7f)
6. [UnityScreenNavigator — GitHub (Haruma-K)](https://github.com/Haruma-K/UnityScreenNavigator)
7. [UI Masking — Unity Learn](https://learn.unity.com/tutorial/ui-masking)

---

## 8. 미해결 질문

- [ ] RectMask2D softness 파라미터를 슬라이드 경계 페이드에 활용할 수 있는가? (페이드아웃 효과)
- [ ] 인터럽트 전환에서 현재 위치를 기준으로 시작 시 `Ease.OutCubic` 커브가 어색해지는 문제 해결법
- [ ] Unity 6 uGUI 2.5에서 RectMask2D Canvas 비활성 버그가 수정되었는가?
