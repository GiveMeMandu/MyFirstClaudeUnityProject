using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.AdvancedHUD.Views
{
    /// <summary>
    /// Loading Screen View — 풀스크린 오버레이 + 프로그레스 바 + 팁 순환 + 스피너.
    /// </summary>
    public class LoadingScreenView : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private CanvasGroup _overlayCanvasGroup;
        [SerializeField] private GameObject _overlayRoot;

        [Header("Progress Bar")]
        [SerializeField] private Image _progressBarFill;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI _percentageText;
        [SerializeField] private TextMeshProUGUI _currentTaskText;
        [SerializeField] private TextMeshProUGUI _tipsText;

        [Header("Spinner")]
        [SerializeField] private RectTransform _spinnerTransform;

        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 0.5f;

        private static readonly string[] Tips =
        {
            "Tip: Press F1 for help at any time.",
            "Tip: Upgrading your base increases resource capacity.",
            "Tip: Units heal automatically when idle near a camp.",
            "Tip: You can queue multiple build orders with Shift+Click.",
            "Tip: Scout the map early to find rare resource nodes."
        };

        private Tween _fadeTween;
        private Tween _spinnerTween;
        private Tween _progressTween;
        private int _currentTipIndex;
        private float _tipTimer;

        private const float TipCycleInterval = 3f;

        private void OnEnable()
        {
            _currentTipIndex = 0;
            _tipTimer = 0f;
            ShowNextTip();
            StartSpinner();
        }

        private void Update()
        {
            if (!_overlayRoot.activeSelf) return;

            _tipTimer += Time.unscaledDeltaTime;
            if (_tipTimer >= TipCycleInterval)
            {
                _tipTimer = 0f;
                ShowNextTip();
            }
        }

        private void ShowNextTip()
        {
            if (_tipsText != null)
            {
                _tipsText.text = Tips[_currentTipIndex % Tips.Length];
                _currentTipIndex++;
            }
        }

        private void StartSpinner()
        {
            _spinnerTween?.Kill();
            if (_spinnerTransform != null)
            {
                _spinnerTransform.localRotation = Quaternion.identity;
                _spinnerTween = _spinnerTransform
                    .DOLocalRotate(new Vector3(0, 0, -360f), 1.5f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Restart)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// 프로그레스 바 + 퍼센트 텍스트 갱신.
        /// </summary>
        public void SetProgress(float normalizedProgress)
        {
            normalizedProgress = Mathf.Clamp01(normalizedProgress);

            _progressTween?.Kill();
            _progressTween = _progressBarFill
                .DOFillAmount(normalizedProgress, 0.2f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            if (_percentageText != null)
                _percentageText.text = $"{Mathf.RoundToInt(normalizedProgress * 100)}%";
        }

        /// <summary>
        /// 현재 태스크 텍스트 갱신.
        /// </summary>
        public void SetCurrentTask(string taskName)
        {
            if (_currentTaskText != null)
                _currentTaskText.text = taskName;
        }

        /// <summary>
        /// 로딩 스크린 페이드 인 표시.
        /// </summary>
        public void Show()
        {
            _fadeTween?.Kill();
            _overlayRoot.SetActive(true);
            _overlayCanvasGroup.alpha = 0f;

            if (_progressBarFill != null)
                _progressBarFill.fillAmount = 0f;

            _fadeTween = _overlayCanvasGroup
                .DOFade(1f, _fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        /// <summary>
        /// 로딩 스크린 페이드 아웃 숨김.
        /// </summary>
        public void Hide()
        {
            _fadeTween?.Kill();
            _fadeTween = _overlayCanvasGroup
                .DOFade(0f, _fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() => _overlayRoot.SetActive(false));
        }

        private void OnDestroy()
        {
            _fadeTween?.Kill();
            _spinnerTween?.Kill();
            _progressTween?.Kill();
        }
    }
}
