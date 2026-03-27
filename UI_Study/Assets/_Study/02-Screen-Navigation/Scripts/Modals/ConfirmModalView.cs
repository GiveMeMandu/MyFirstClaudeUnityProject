using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace UIStudy.Navigation.Modals
{
    /// <summary>
    /// 확인/취소 모달 — Modal을 상속.
    /// ModalContainer.Push로 열리고, Pop으로 닫힌다.
    /// </summary>
    public class ConfirmModalView : Modal
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        public Observable<Unit> OnConfirmClick => _confirmButton.OnClickAsObservable();
        public Observable<Unit> OnCancelClick => _cancelButton.OnClickAsObservable();

        public void SetMessage(string message) => _messageText.text = message;
    }
}
