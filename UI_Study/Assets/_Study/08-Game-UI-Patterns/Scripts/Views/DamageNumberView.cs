using DG.Tweening;
using TMPro;
using UIStudy.GameUI.Models;
using UnityEngine;

namespace UIStudy.GameUI.Views
{
    /// <summary>
    /// 플로팅 데미지 넘버 — 오브젝트 풀 친화적 (Reset + Animate).
    /// TMP 텍스트 + CanvasGroup 기반.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DamageNumberView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        [Header("Animation Settings")]
        [SerializeField] private float _floatDistance = 80f;
        [SerializeField] private float _duration = 0.8f;
        [SerializeField] private float _criticalScale = 1.5f;

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
        /// 풀에서 꺼낼 때 초기화.
        /// </summary>
        public void ResetView()
        {
            EnsureComponents();
            _activeSequence?.Kill();
            _canvasGroup.alpha = 1f;
            _rectTransform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 데미지 표시 + 애니메이션 시작. 완료 시 onComplete 콜백.
        /// </summary>
        public void Animate(DamageModel data, Vector2 anchoredPosition, System.Action onComplete)
        {
            EnsureComponents();
            _rectTransform.anchoredPosition = anchoredPosition;
            _canvasGroup.alpha = 1f;
            _rectTransform.localScale = Vector3.one;
            gameObject.SetActive(true);

            // 타입별 텍스트/색상 설정
            switch (data.Type)
            {
                case DamageType.Normal:
                    _text.text = data.Damage.ToString();
                    _text.color = Color.white;
                    _text.fontSize = 36f;
                    AnimateNormal(anchoredPosition, onComplete);
                    break;

                case DamageType.Critical:
                    _text.text = data.Damage + "!";
                    _text.color = new Color(1f, 0.85f, 0f); // yellow
                    _text.fontSize = 36f * _criticalScale;
                    AnimateCritical(anchoredPosition, onComplete);
                    break;

                case DamageType.Heal:
                    _text.text = "+" + data.Damage;
                    _text.color = new Color(0.3f, 0.9f, 0.3f); // green
                    _text.fontSize = 36f;
                    AnimateNormal(anchoredPosition, onComplete);
                    break;
            }
        }

        /// <summary>
        /// 일반 데미지/힐: 위로 떠오르며 페이드아웃.
        /// </summary>
        private void AnimateNormal(Vector2 startPos, System.Action onComplete)
        {
            _activeSequence?.Kill();
            _activeSequence = DOTween.Sequence()
                .Join(_rectTransform.DOAnchorPosY(startPos.y + _floatDistance, _duration).SetEase(Ease.OutQuad))
                .Join(_canvasGroup.DOFade(0f, _duration).SetEase(Ease.InQuad))
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// 크리티컬: 펀치 스케일 + 위로 떠오르며 페이드아웃.
        /// </summary>
        private void AnimateCritical(Vector2 startPos, System.Action onComplete)
        {
            _activeSequence?.Kill();
            _rectTransform.localScale = Vector3.one * _criticalScale;

            _activeSequence = DOTween.Sequence()
                .Append(_rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 6))
                .Join(_rectTransform.DOAnchorPosY(startPos.y + _floatDistance, _duration).SetEase(Ease.OutQuad))
                .Insert(0.3f, _canvasGroup.DOFade(0f, _duration - 0.3f).SetEase(Ease.InQuad))
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void OnDestroy()
        {
            _activeSequence?.Kill();
        }
    }
}
