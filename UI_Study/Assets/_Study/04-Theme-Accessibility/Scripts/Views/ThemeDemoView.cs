using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Theme.Views
{
    /// <summary>
    /// 테마 + 접근성 데모 View.
    /// 테마 전환 버튼, 폰트 스케일 슬라이더, 색약 모드 토글, 미리보기 패널.
    /// </summary>
    public class ThemeDemoView : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private Button _cycleThemeButton;
        [SerializeField] private TextMeshProUGUI _themeLabel;

        [Header("Accessibility")]
        [SerializeField] private Slider _fontScaleSlider;
        [SerializeField] private TextMeshProUGUI _fontScaleLabel;
        [SerializeField] private Button _colorBlindToggle;
        [SerializeField] private TextMeshProUGUI _colorBlindLabel;

        [Header("Preview")]
        [SerializeField] private Image _backgroundPanel;
        [SerializeField] private TextMeshProUGUI _previewTitle;
        [SerializeField] private TextMeshProUGUI _previewBody;
        [SerializeField] private Image _accentBar;

        [Header("All TMP Texts (for font scaling)")]
        [SerializeField] private TextMeshProUGUI[] _allTexts;

        // Events
        public Observable<Unit> OnCycleThemeClick => _cycleThemeButton.OnClickAsObservable();
        public Observable<float> OnFontScaleChanged => _fontScaleSlider.OnValueChangedAsObservable();
        public Observable<Unit> OnColorBlindToggleClick => _colorBlindToggle.OnClickAsObservable();

        // Theme application
        public void SetThemeLabel(string label) => _themeLabel.text = label;
        public void SetBackgroundColor(Color color) => _backgroundPanel.color = color;
        public void SetPrimaryTextColor(Color color)
        {
            _previewTitle.color = color;
            _previewBody.color = color;
        }
        public void SetAccentColor(Color color) => _accentBar.color = color;
        public void SetPreviewTitle(string title) => _previewTitle.text = title;
        public void SetPreviewBody(string body) => _previewBody.text = body;

        // Font scaling
        public void SetFontScaleLabel(string label) => _fontScaleLabel.text = label;
        public void ApplyFontScale(float scale)
        {
            foreach (var text in _allTexts)
            {
                if (text != null)
                {
                    // 기본 크기를 유지하면서 스케일 적용
                    text.transform.localScale = Vector3.one * scale;
                }
            }
        }

        // Color blind
        public void SetColorBlindLabel(string label) => _colorBlindLabel.text = label;
    }
}
