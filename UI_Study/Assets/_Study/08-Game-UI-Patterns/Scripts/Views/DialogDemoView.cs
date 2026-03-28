using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.GameUI.Views
{
    /// <summary>
    /// 대화 데모 View — 대화 시작 버튼 + 진행 상태 표시.
    /// </summary>
    public class DialogDemoView : MonoBehaviour
    {
        [SerializeField] private Button _startDialogButton;
        [SerializeField] private TextMeshProUGUI _statusText;

        public Button StartDialogButton => _startDialogButton;

        public void SetStatus(string status)
        {
            if (_statusText != null)
                _statusText.text = status;
        }

        public void SetStartButtonInteractable(bool interactable)
        {
            _startDialogButton.interactable = interactable;
        }
    }
}
