using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.AdvancedHUD.Views
{
    /// <summary>
    /// Loading Demo View — "Start Loading" 버튼 + 상태 텍스트.
    /// </summary>
    public class LoadingDemoView : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] private Button _startLoadingButton;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI _statusText;

        public Button StartLoadingButton => _startLoadingButton;

        /// <summary>
        /// 상태 텍스트 갱신.
        /// </summary>
        public void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }

        /// <summary>
        /// 버튼 활성화/비활성화.
        /// </summary>
        public void SetButtonInteractable(bool interactable)
        {
            if (_startLoadingButton != null)
                _startLoadingButton.interactable = interactable;
        }
    }
}
