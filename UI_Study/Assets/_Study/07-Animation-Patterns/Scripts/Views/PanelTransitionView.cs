using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Animation.Views
{
    /// <summary>
    /// 패널 전환 트랜지션을 시연하는 View.
    /// 두 패널(A, B) 사이를 4가지 방식(Fade, SlideLeft, ScalePopIn, FlipY)으로 전환한다.
    /// </summary>
    public class PanelTransitionView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private RectTransform _panelA;
        [SerializeField] private RectTransform _panelB;

        [Header("Transition Buttons")]
        [SerializeField] private Button _fadeButton;
        [SerializeField] private Button _slideLeftButton;
        [SerializeField] private Button _scalePopButton;
        [SerializeField] private Button _flipYButton;

        [Header("Animation Settings")]
        [SerializeField] private float _transitionDuration = 0.4f;

        private CanvasGroup _cgA;
        private CanvasGroup _cgB;
        private Sequence _currentTransition;
        private bool _showingA = true;

        // Read-only accessors for Presenter
        public Button FadeButton => _fadeButton;
        public Button SlideLeftButton => _slideLeftButton;
        public Button ScalePopButton => _scalePopButton;
        public Button FlipYButton => _flipYButton;

        /// <summary>
        /// 현재 A 패널이 표시 중인지 여부.
        /// </summary>
        public bool IsShowingA => _showingA;

        private void Awake()
        {
            _cgA = GetOrAddCanvasGroup(_panelA);
            _cgB = GetOrAddCanvasGroup(_panelB);

            // 초기 상태: A 표시, B 숨김
            SetPanelVisible(_panelA, _cgA, true);
            SetPanelVisible(_panelB, _cgB, false);
        }

        /// <summary>
        /// Fade 전환 — 현재 패널 페이드 아웃, 새 패널 페이드 인.
        /// </summary>
        public void TransitionFade()
        {
            var (outPanel, outCg, inPanel, inCg) = GetTransitionTargets();
            KillCurrentTransition();

            // 초기 상태 설정
            inPanel.gameObject.SetActive(true);
            inCg.alpha = 0f;
            inPanel.localScale = Vector3.one;
            inPanel.anchoredPosition = Vector2.zero;

            _currentTransition = DOTween.Sequence()
                .Append(outCg.DOFade(0f, _transitionDuration * 0.5f))
                .AppendCallback(() => outPanel.gameObject.SetActive(false))
                .Append(inCg.DOFade(1f, _transitionDuration * 0.5f))
                .SetAutoKill(true);

            _showingA = !_showingA;
        }

        /// <summary>
        /// Slide Left 전환 — 현재 패널이 왼쪽으로 빠지고 새 패널이 오른쪽에서 들어옴.
        /// 뷰포트(부모) 너비 기준 슬라이드 + RectMask2D 클리핑으로 영역 밖 자동 숨김.
        /// </summary>
        public void TransitionSlideLeft()
        {
            var (outPanel, outCg, inPanel, inCg) = GetTransitionTargets();
            KillCurrentTransition();

            // 뷰포트(패널 부모) 너비 기준 — 해상도 독립적
            var viewport = _panelA.parent as RectTransform ?? (RectTransform)transform;
            float slideDistance = viewport.rect.width;
            if (slideDistance <= 0f) slideDistance = 800f;

            // 위치 초기화 BEFORE SetActive (깜빡임 방지)
            inPanel.anchoredPosition = new Vector2(slideDistance, 0f);
            inCg.alpha = 1f;
            inPanel.localScale = Vector3.one;
            inPanel.gameObject.SetActive(true);

            _currentTransition = DOTween.Sequence()
                .Join(outPanel.DOAnchorPos(new Vector2(-slideDistance, 0f), _transitionDuration)
                    .SetEase(Ease.InOutCubic))
                .Join(inPanel.DOAnchorPos(Vector2.zero, _transitionDuration)
                    .SetEase(Ease.InOutCubic))
                .AppendCallback(() =>
                {
                    outPanel.gameObject.SetActive(false);
                    outPanel.anchoredPosition = Vector2.zero;
                })
                .SetAutoKill(true);

            _showingA = !_showingA;
        }

        /// <summary>
        /// Scale Pop In 전환 — 현재 패널 축소 사라짐, 새 패널 팝업 등장.
        /// </summary>
        public void TransitionScalePopIn()
        {
            var (outPanel, outCg, inPanel, inCg) = GetTransitionTargets();
            KillCurrentTransition();

            inPanel.gameObject.SetActive(true);
            inCg.alpha = 0f;
            inPanel.localScale = Vector3.one * 0.5f;
            inPanel.anchoredPosition = Vector2.zero;

            _currentTransition = DOTween.Sequence()
                // Out: 축소 + 페이드
                .Append(outPanel.DOScale(0.8f, _transitionDuration * 0.4f).SetEase(Ease.InBack))
                .Join(outCg.DOFade(0f, _transitionDuration * 0.4f))
                .AppendCallback(() =>
                {
                    outPanel.gameObject.SetActive(false);
                    outPanel.localScale = Vector3.one;
                })
                // In: 팝업 + 페이드
                .Append(inPanel.DOScale(1f, _transitionDuration * 0.6f).SetEase(Ease.OutBack))
                .Join(inCg.DOFade(1f, _transitionDuration * 0.4f))
                .SetAutoKill(true);

            _showingA = !_showingA;
        }

        /// <summary>
        /// Flip Y 전환 — Y축 기준 플립 효과.
        /// 전반: 현재 패널이 Y축 90도까지 회전하며 사라짐.
        /// 후반: 새 패널이 -90도에서 0도까지 회전하며 등장.
        /// </summary>
        public void TransitionFlipY()
        {
            var (outPanel, outCg, inPanel, inCg) = GetTransitionTargets();
            KillCurrentTransition();

            float halfDuration = _transitionDuration * 0.5f;

            inPanel.anchoredPosition = Vector2.zero;
            inPanel.localScale = Vector3.one;

            _currentTransition = DOTween.Sequence()
                // 전반: 현재 패널 90도 회전
                .Append(outPanel.DORotate(new Vector3(0f, 90f, 0f), halfDuration)
                    .SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    outPanel.gameObject.SetActive(false);
                    outPanel.rotation = Quaternion.identity;

                    inPanel.gameObject.SetActive(true);
                    inCg.alpha = 1f;
                    inPanel.rotation = Quaternion.Euler(0f, -90f, 0f);
                })
                // 후반: 새 패널 -90도에서 0도로
                .Append(inPanel.DORotate(Vector3.zero, halfDuration)
                    .SetEase(Ease.OutQuad))
                .SetAutoKill(true);

            _showingA = !_showingA;
        }

        private (RectTransform outPanel, CanvasGroup outCg, RectTransform inPanel, CanvasGroup inCg)
            GetTransitionTargets()
        {
            if (_showingA)
                return (_panelA, _cgA, _panelB, _cgB);
            return (_panelB, _cgB, _panelA, _cgA);
        }

        private void KillCurrentTransition()
        {
            if (_currentTransition != null && _currentTransition.IsActive())
            {
                _currentTransition.Kill();
            }
            _currentTransition = null;
        }

        private static void SetPanelVisible(RectTransform panel, CanvasGroup cg, bool visible)
        {
            cg.alpha = visible ? 1f : 0f;
            panel.localScale = Vector3.one;
            panel.anchoredPosition = Vector2.zero;
            panel.rotation = Quaternion.identity;
            panel.gameObject.SetActive(visible);
        }

        private static CanvasGroup GetOrAddCanvasGroup(RectTransform rt)
        {
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        private void OnDestroy()
        {
            KillCurrentTransition();
            if (_panelA != null)
            {
                _panelA.DOKill();
                if (_cgA != null) _cgA.DOKill();
            }
            if (_panelB != null)
            {
                _panelB.DOKill();
                if (_cgB != null) _cgB.DOKill();
            }
        }
    }
}
