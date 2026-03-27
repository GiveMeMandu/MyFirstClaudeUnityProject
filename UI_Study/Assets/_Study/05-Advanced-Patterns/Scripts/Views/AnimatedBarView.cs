using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Advanced.Views
{
    /// <summary>
    /// 2-tier 체력바 — Image.fillAmount 기반.
    ///
    /// 데미지 시: 전경(메인)이 즉시 줄어듦, 배경(잔상)이 천천히 따라감
    /// 힐 시: 배경(프리뷰)이 즉시 늘어남, 전경(메인)이 천천히 따라감
    ///
    /// 사용 조건: 두 Image 모두 Type=Filled, FillMethod=Horizontal, Source Image 할당 필수.
    /// </summary>
    public class AnimatedBarView : MonoBehaviour
    {
        [Header("Fill Images (Type=Filled, Horizontal)")]
        [SerializeField] private Image _foregroundFill;
        [SerializeField] private Image _backgroundFill;

        [Header("Animation")]
        [SerializeField] private float _tweenDuration = 0.5f;
        [SerializeField] private float _punchThreshold = 0.2f;

        private Tween _trailingTween;
        private float _currentValue = 1f;

        /// <summary>
        /// 값 변화 시 호출 — normalized (0~1).
        /// </summary>
        public void SetValue(float normalizedValue)
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);
            var delta = normalizedValue - _currentValue;
            _currentValue = normalizedValue;

            _trailingTween?.Kill();

            if (delta < 0)
            {
                // === 데미지 (값 감소) ===
                // 전경(메인)이 즉시 줄어듦
                _foregroundFill.fillAmount = normalizedValue;
                // 배경(잔상)이 천천히 따라감
                _trailingTween = _backgroundFill
                    .DOFillAmount(normalizedValue, _tweenDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true);
            }
            else
            {
                // === 힐 (값 증가) ===
                // 배경(프리뷰)이 즉시 늘어남
                _backgroundFill.fillAmount = normalizedValue;
                // 전경(메인)이 천천히 따라감
                _trailingTween = _foregroundFill
                    .DOFillAmount(normalizedValue, _tweenDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }

            // 큰 변화 시 스케일 펀치
            if (Mathf.Abs(delta) >= _punchThreshold)
            {
                transform.DOKill();
                transform.localScale = Vector3.one;
                transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5)
                    .SetUpdate(true);
            }
        }

        public void SetForegroundColor(Color color) => _foregroundFill.color = color;

        private void OnDestroy()
        {
            _trailingTween?.Kill();
            transform.DOKill();
        }
    }
}
