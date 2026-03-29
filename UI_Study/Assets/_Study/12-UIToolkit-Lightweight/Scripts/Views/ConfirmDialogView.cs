using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 3: UniTaskCompletionSource 기반 async 확인 다이얼로그.
    /// await ShowAsync("메시지", ct) → 확인=true, 취소=false.
    /// </summary>
    public class ConfirmDialogView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _overlay;
        private Label _titleLabel;
        private Label _messageLabel;
        private Button _confirmBtn;
        private Button _cancelBtn;

        private UniTaskCompletionSource<bool> _tcs;

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _overlay      = root.Q<VisualElement>("modal-overlay");
            _titleLabel   = root.Q<Label>("dialog-title");
            _messageLabel = root.Q<Label>("dialog-message");
            _confirmBtn   = root.Q<Button>("btn-confirm");
            _cancelBtn    = root.Q<Button>("btn-cancel");

            _confirmBtn.clicked += OnConfirmClicked;
            _cancelBtn.clicked  += OnCancelClicked;
        }

        private void OnDisable()
        {
            if (_confirmBtn != null) _confirmBtn.clicked -= OnConfirmClicked;
            if (_cancelBtn != null)  _cancelBtn.clicked  -= OnCancelClicked;

            // 열린 상태에서 파괴되면 취소 처리
            _tcs?.TrySetCanceled();
        }

        /// <summary>
        /// 다이얼로그를 표시하고 사용자 응답을 await.
        /// </summary>
        public async UniTask<bool> ShowAsync(string message, CancellationToken ct,
            string title = "Confirm")
        {
            _titleLabel.text   = title;
            _messageLabel.text = message;

            _tcs = new UniTaskCompletionSource<bool>();

            // CancellationToken 등록 — 외부 취소 시 TrySetCanceled
            var ctr = ct.CanBeCanceled
                ? ct.RegisterWithoutCaptureExecutionContext(() => _tcs.TrySetCanceled())
                : default;

            // 표시
            _overlay.style.display = DisplayStyle.Flex;

            bool result;
            try
            {
                result = await _tcs.Task;
            }
            catch (System.OperationCanceledException)
            {
                result = false;
            }
            finally
            {
                ctr.Dispose();
                _overlay.style.display = DisplayStyle.None;
                _tcs = null;
            }

            return result;
        }

        private void OnConfirmClicked() => _tcs?.TrySetResult(true);
        private void OnCancelClicked()  => _tcs?.TrySetResult(false);
    }
}
