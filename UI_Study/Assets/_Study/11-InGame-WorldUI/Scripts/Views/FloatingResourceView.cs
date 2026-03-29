using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UIStudy.InGameUI.Views
{
    /// <summary>
    /// Pool-friendly floating resource text.
    /// Animates: float up 80px + fade out over 1.2s, then deactivates.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FloatingResourceView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        [Header("Animation Settings")]
        [SerializeField] private float _floatDistance = 80f;
        [SerializeField] private float _duration = 1.2f;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Sequence _activeSequence;

        private void EnsureComponents()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        }

        private void Awake() => EnsureComponents();

        /// <summary>
        /// Display floating text at the given position with animation.
        /// After the animation completes, the object deactivates itself (pool return).
        /// </summary>
        public void Show(string text, Color color, Vector2 localPos)
        {
            EnsureComponents();
            _activeSequence?.Kill();

            _rectTransform.anchoredPosition = localPos;
            _canvasGroup.alpha = 1f;
            _rectTransform.localScale = Vector3.one;
            gameObject.SetActive(true);

            // Set text
            _text.text = text;
            _text.color = color;

            // Animate: float up + fade out
            _activeSequence = DOTween.Sequence()
                .Join(
                    _rectTransform.DOAnchorPosY(localPos.y + _floatDistance, _duration)
                        .SetEase(Ease.OutQuad))
                .Join(
                    _canvasGroup.DOFade(0f, _duration)
                        .SetEase(Ease.InQuad))
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
        }

        /// <summary>
        /// Reset for pool reuse.
        /// </summary>
        public void ResetView()
        {
            EnsureComponents();
            _activeSequence?.Kill();
            _canvasGroup.alpha = 1f;
            _rectTransform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _activeSequence?.Kill();
        }
    }
}
