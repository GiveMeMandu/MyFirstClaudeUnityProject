using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Advanced.Views
{
    /// <summary>
    /// 2-tier 체력바/프로그레스바 — 배경 필(프리뷰) + 전경 필(DOTween 보간).
    /// 배경 필은 즉시 목표값으로 이동, 전경 필은 부드럽게 추격.
    /// 큰 변화 시 스케일 펀치 애니메이션.
    /// </summary>
    public class AnimatedBarView : MonoBehaviour
    {
        [Header("Fill Images")]
        [SerializeField] private Image _foregroundFill;
        [SerializeField] private Image _backgroundFill;

        [Header("Animation")]
        [SerializeField] private float _tweenDuration = 0.5f;
        [SerializeField] private float _punchThreshold = 0.2f;

        private Tween _foregroundTween;
        private float _currentValue = 1f;

        /// <summary>
        /// 값 변화 시 호출 — normalized (0~1).
        /// </summary>
        public void SetValue(float normalizedValue)
        {
            var delta = Mathf.Abs(normalizedValue - _currentValue);
            _currentValue = normalizedValue;

            // 배경 필 — 즉시 목표값 (프리뷰)
            _backgroundFill.fillAmount = normalizedValue;

            // 전경 필 — DOTween 보간
            _foregroundTween?.Kill();
            _foregroundTween = _foregroundFill
                .DOFillAmount(normalizedValue, _tweenDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true); // UnscaledDeltaTime — 일시정지 중에도 동작

            // 큰 변화 시 스케일 펀치
            if (delta >= _punchThreshold)
            {
                transform.DOKill();
                transform.localScale = Vector3.one;
                transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5)
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// 색상 변경 (HP 잔량에 따라).
        /// </summary>
        public void SetForegroundColor(Color color) => _foregroundFill.color = color;

        private void OnDestroy()
        {
            _foregroundTween?.Kill();
            transform.DOKill();
        }
    }
}
