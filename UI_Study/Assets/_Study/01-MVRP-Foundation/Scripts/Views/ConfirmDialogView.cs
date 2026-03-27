using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.MVRP.Views
{
    /// <summary>
    /// 확인 다이얼로그 View — 메시지 + 확인/취소 버튼.
    /// 표시/숨김은 SetActive로 제어 (Step 5에서 애니메이션으로 교체).
    /// </summary>
    public class ConfirmDialogView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        public Observable<Unit> OnConfirmClick => _confirmButton.OnClickAsObservable();
        public Observable<Unit> OnCancelClick => _cancelButton.OnClickAsObservable();

        public void SetMessage(string message) => _messageText.text = message;

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
