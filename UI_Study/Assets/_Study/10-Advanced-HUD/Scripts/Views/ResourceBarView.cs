using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.AdvancedHUD.Views
{
    /// <summary>
    /// 단일 자원 표시 View — 아이콘 + 값 텍스트 + 선택적 바(인구용).
    /// </summary>
    public class ResourceBarView : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI _iconText;
        [SerializeField] private TextMeshProUGUI _valueText;

        [Header("Optional Bar (Population)")]
        [SerializeField] private Image _barFill;
        [SerializeField] private GameObject _barContainer;

        [Header("Buttons")]
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _subtractButton;

        [Header("Colors")]
        [SerializeField] private Color _gainFlashColor = new(0.2f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color _lossFlashColor = new(0.9f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _normalColor = Color.white;

        private Tween _counterTween;
        private Tween _colorTween;
        private Tween _barTween;
        private int _displayedValue;

        public Button AddButton => _addButton;
        public Button SubtractButton => _subtractButton;

        /// <summary>
        /// 아이콘 텍스트 설정 (emoji).
        /// </summary>
        public void SetIcon(string icon)
        {
            if (_iconText != null)
                _iconText.text = icon;
        }

        /// <summary>
        /// 바 표시 여부 설정 (인구만 바 사용).
        /// </summary>
        public void SetBarVisible(bool visible)
        {
            if (_barContainer != null)
                _barContainer.SetActive(visible);
        }

        /// <summary>
        /// 값 애니메이션 갱신 — DOTween DOVirtual.Float로 카운터 애니메이션.
        /// </summary>
        public void SetValue(int newValue, int maxValue = -1)
        {
            int oldValue = _displayedValue;
            bool isGain = newValue > oldValue;

            _counterTween?.Kill();
            _colorTween?.Kill();

            // 카운터 애니메이션
            _counterTween = DOVirtual.Float(oldValue, newValue, 0.4f, v =>
            {
                _displayedValue = Mathf.RoundToInt(v);
                UpdateValueText(maxValue);
            })
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

            // 바 갱신 (인구용)
            if (_barFill != null && maxValue > 0)
            {
                _barTween?.Kill();
                float normalized = Mathf.Clamp01((float)newValue / maxValue);
                _barTween = _barFill
                    .DOFillAmount(normalized, 0.4f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }

            // 변화 시 색상 플래시
            if (oldValue != newValue && _valueText != null)
            {
                _valueText.color = isGain ? _gainFlashColor : _lossFlashColor;
                _colorTween = DOVirtual.Color(
                    isGain ? _gainFlashColor : _lossFlashColor,
                    _normalColor,
                    0.5f,
                    c => { if (_valueText != null) _valueText.color = c; })
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// 즉시 값 설정 (초기화용, 애니메이션 없음).
        /// </summary>
        public void SetValueImmediate(int value, int maxValue = -1)
        {
            _displayedValue = value;
            UpdateValueText(maxValue);

            if (_barFill != null && maxValue > 0)
            {
                _barFill.fillAmount = Mathf.Clamp01((float)value / maxValue);
            }
        }

        private void UpdateValueText(int maxValue)
        {
            if (_valueText == null) return;

            if (maxValue > 0)
                _valueText.text = $"{_displayedValue}/{maxValue}";
            else
                _valueText.text = _displayedValue.ToString();
        }

        private void OnDestroy()
        {
            _counterTween?.Kill();
            _colorTween?.Kill();
            _barTween?.Kill();
        }
    }
}
