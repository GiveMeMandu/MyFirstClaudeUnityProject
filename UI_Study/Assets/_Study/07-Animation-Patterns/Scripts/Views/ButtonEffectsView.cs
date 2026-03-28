using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Animation.Views
{
    /// <summary>
    /// 4가지 버튼 마이크로-인터랙션을 시연하는 View.
    /// 각 버튼에 대해 DOTween 기반 피드백 애니메이션을 제공한다.
    /// </summary>
    public class ButtonEffectsView : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _scalePunchButton;
        [SerializeField] private Button _colorFlashButton;
        [SerializeField] private Button _shakeButton;
        [SerializeField] private Button _bounceButton;

        [Header("Counter Labels")]
        [SerializeField] private TextMeshProUGUI _scalePunchCountText;
        [SerializeField] private TextMeshProUGUI _colorFlashCountText;
        [SerializeField] private TextMeshProUGUI _shakeCountText;
        [SerializeField] private TextMeshProUGUI _bounceCountText;

        [Header("Animation Settings")]
        [SerializeField] private float _punchScaleIntensity = 0.2f;
        [SerializeField] private float _punchDuration = 0.3f;
        [SerializeField] private Color _flashColor = Color.yellow;
        [SerializeField] private float _flashDuration = 0.15f;
        [SerializeField] private float _shakeStrength = 10f;
        [SerializeField] private float _shakeDuration = 0.3f;
        [SerializeField] private float _bounceDuration = 0.4f;

        // Read-only accessors for Presenter
        public Button ScalePunchButton => _scalePunchButton;
        public Button ColorFlashButton => _colorFlashButton;
        public Button ShakeButton => _shakeButton;
        public Button BounceButton => _bounceButton;

        /// <summary>
        /// Scale Punch — 버튼을 "톡" 치는 스케일 효과.
        /// </summary>
        public void PlayScalePunch()
        {
            var t = _scalePunchButton.transform;
            t.DOKill();
            t.localScale = Vector3.one;
            t.DOPunchScale(Vector3.one * _punchScaleIntensity, _punchDuration, 6, 0.6f)
                .SetAutoKill(true);
        }

        /// <summary>
        /// Color Flash — 버튼 이미지가 순간 밝아졌다 원래로 돌아옴.
        /// </summary>
        public void PlayColorFlash()
        {
            var image = _colorFlashButton.GetComponent<Image>();
            if (image == null) return;

            image.DOKill();
            var originalColor = Color.white;
            image.color = originalColor;
            image.DOColor(_flashColor, _flashDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetAutoKill(true);
        }

        /// <summary>
        /// Shake — 버튼 위치를 흔드는 효과.
        /// </summary>
        public void PlayShake()
        {
            var rt = _shakeButton.GetComponent<RectTransform>();
            rt.DOKill();
            rt.DOShakeAnchorPos(_shakeDuration, _shakeStrength, 12, 90f, false, true, ShakeRandomnessMode.Harmonic)
                .SetAutoKill(true);
        }

        /// <summary>
        /// Bounce — 축소 후 오버슈트 바운스로 복귀하는 시퀀스.
        /// </summary>
        public void PlayBounce()
        {
            var t = _bounceButton.transform;
            t.DOKill();
            t.localScale = Vector3.one;

            DOTween.Sequence()
                .Append(t.DOScale(0.7f, _bounceDuration * 0.3f).SetEase(Ease.InBack))
                .Append(t.DOScale(1f, _bounceDuration * 0.7f).SetEase(Ease.OutBack))
                .SetAutoKill(true);
        }

        /// <summary>
        /// 카운터 텍스트 업데이트.
        /// </summary>
        public void UpdateCountText(int index, int count)
        {
            var label = index switch
            {
                0 => _scalePunchCountText,
                1 => _colorFlashCountText,
                2 => _shakeCountText,
                3 => _bounceCountText,
                _ => null
            };
            if (label != null) label.text = $"Clicks: {count}";
        }

        private void OnDestroy()
        {
            // 모든 버튼의 트윈 정리
            if (_scalePunchButton != null) _scalePunchButton.transform.DOKill();
            if (_colorFlashButton != null)
            {
                _colorFlashButton.transform.DOKill();
                var img = _colorFlashButton.GetComponent<Image>();
                if (img != null) img.DOKill();
            }
            if (_shakeButton != null) _shakeButton.GetComponent<RectTransform>().DOKill();
            if (_bounceButton != null) _bounceButton.transform.DOKill();
        }
    }
}
