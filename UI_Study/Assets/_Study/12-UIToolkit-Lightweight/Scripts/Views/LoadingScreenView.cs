using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 3: ProgressBar + IProgress&lt;float&gt; 로딩 화면.
    /// Show/Hide + SetProgress로 진행률 표시.
    /// </summary>
    public class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _overlay;
        private ProgressBar _progressBar;
        private Label _progressLabel;

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _overlay      = root.Q<VisualElement>("loading-overlay");
            _progressBar  = root.Q<ProgressBar>("progress-bar");
            _progressLabel = root.Q<Label>("progress-label");
        }

        public void Show()
        {
            _overlay.style.display = DisplayStyle.Flex;
            SetProgress(0f);
        }

        public void Hide()
        {
            _overlay.style.display = DisplayStyle.None;
        }

        public void SetProgress(float normalized)
        {
            float pct = Mathf.Clamp01(normalized) * 100f;
            _progressBar.value = pct;
            _progressLabel.text = $"{pct:F0}%";
        }
    }
}
