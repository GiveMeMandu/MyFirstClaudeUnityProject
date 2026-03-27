using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UIStudy.Advanced.Models;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Advanced.Views
{
    /// <summary>
    /// 토스트 알림 View — 슬라이드 인/아웃 + 타입별 색상.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _background;
        [SerializeField] private float _slideDistance = 100f;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            gameObject.SetActive(false);
        }

        public async UniTask ShowAsync(ToastData data, System.Threading.CancellationToken ct)
        {
            _messageText.text = data.Message;
            _background.color = GetColorForType(data.Type);
            gameObject.SetActive(true);

            // 초기 상태
            _canvasGroup.alpha = 0f;
            var startPos = _rectTransform.anchoredPosition;
            _rectTransform.anchoredPosition = startPos + Vector2.up * _slideDistance;

            // 슬라이드 인
            var seq = DOTween.Sequence()
                .Join(_rectTransform.DOAnchorPos(startPos, 0.3f).SetEase(Ease.OutBack))
                .Join(_canvasGroup.DOFade(1f, 0.2f))
                .SetUpdate(true);
            await seq.AsyncWaitForCompletion();

            // 표시 유지
            await UniTask.Delay(
                (int)(data.Duration * 1000),
                delayType: DelayType.UnscaledDeltaTime,
                cancellationToken: ct);

            // 슬라이드 아웃
            var outSeq = DOTween.Sequence()
                .Join(_rectTransform.DOAnchorPos(startPos + Vector2.up * _slideDistance, 0.2f))
                .Join(_canvasGroup.DOFade(0f, 0.2f))
                .SetUpdate(true);
            await outSeq.AsyncWaitForCompletion();

            gameObject.SetActive(false);
            _rectTransform.anchoredPosition = startPos;
        }

        private static Color GetColorForType(ToastType type) => type switch
        {
            ToastType.Success => new Color(0.2f, 0.7f, 0.3f, 0.9f),
            ToastType.Warning => new Color(0.9f, 0.7f, 0.1f, 0.9f),
            ToastType.Error => new Color(0.8f, 0.2f, 0.2f, 0.9f),
            ToastType.Info => new Color(0.2f, 0.4f, 0.8f, 0.9f),
            _ => new Color(0.3f, 0.3f, 0.3f, 0.9f)
        };

        private void OnDestroy()
        {
            _rectTransform.DOKill();
            _canvasGroup.DOKill();
        }
    }
}
