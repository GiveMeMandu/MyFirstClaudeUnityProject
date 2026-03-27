using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UIStudy.MVRP.Views
{
    /// <summary>
    /// DOTween 기반 패널 애니메이션 — Show/Hide를 await 가능하게 제공.
    /// CanvasGroup 페이드 + Scale 애니메이션.
    /// Animator를 사용하지 않는다 (매 프레임 Canvas dirty 방지).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AnimatedPanelView : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _showDuration = 0.25f;
        [SerializeField] private float _hideDuration = 0.15f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InQuad;

        private CanvasGroup _canvasGroup;
        private Sequence _currentSequence;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public async UniTask ShowAsync(CancellationToken ct = default)
        {
            KillCurrentSequence();
            gameObject.SetActive(true);

            transform.localScale = Vector3.one * 0.8f;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;

            _currentSequence = DOTween.Sequence()
                .Join(transform.DOScale(Vector3.one, _showDuration).SetEase(_showEase))
                .Join(_canvasGroup.DOFade(1f, _showDuration))
                .OnComplete(() => _canvasGroup.interactable = true);

            await _currentSequence.AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);
        }

        public async UniTask HideAsync(CancellationToken ct = default)
        {
            KillCurrentSequence();
            _canvasGroup.interactable = false;

            _currentSequence = DOTween.Sequence()
                .Join(transform.DOScale(Vector3.one * 0.8f, _hideDuration).SetEase(_hideEase))
                .Join(_canvasGroup.DOFade(0f, _hideDuration))
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    gameObject.SetActive(false);
                });

            await _currentSequence.AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);
        }

        /// <summary>
        /// 애니메이션 없이 즉시 숨김 (초기화용).
        /// </summary>
        public void HideImmediate()
        {
            KillCurrentSequence();
            _canvasGroup = _canvasGroup != null ? _canvasGroup : GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void KillCurrentSequence()
        {
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
        }

        private void OnDestroy()
        {
            KillCurrentSequence();
        }
    }
}
