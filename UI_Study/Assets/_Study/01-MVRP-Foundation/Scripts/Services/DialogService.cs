using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UIStudy.MVRP.Views;

namespace UIStudy.MVRP.Services
{
    /// <summary>
    /// 다이얼로그 서비스 — UniTask로 다이얼로그 결과를 await 가능하게 제공.
    /// AnimatedPanelView가 있으면 애니메이션 포함, 없으면 SetActive로 대체.
    /// </summary>
    public class DialogService : IDisposable
    {
        private readonly ConfirmDialogView _dialogView;
        private readonly AnimatedPanelView _animatedPanel;

        public DialogService(ConfirmDialogView dialogView)
        {
            _dialogView = dialogView;
            // AnimatedPanelView가 같은 GameObject에 있으면 사용
            _animatedPanel = dialogView.GetComponent<AnimatedPanelView>();

            if (_animatedPanel != null)
                _animatedPanel.HideImmediate();
            else
                _dialogView.Hide();
        }

        /// <summary>
        /// 확인 다이얼로그를 표시하고 사용자 응답을 await.
        /// true = 확인, false = 취소.
        /// </summary>
        public async UniTask<bool> ShowConfirmAsync(string message, CancellationToken ct = default)
        {
            _dialogView.SetMessage(message);

            // 열기 (애니메이션 유무에 따라 분기)
            if (_animatedPanel != null)
                await _animatedPanel.ShowAsync(ct);
            else
                _dialogView.Show();

            // 버튼 대기
            var confirmed = false;
            var tcs = new UniTaskCompletionSource();

            var ctr = ct.CanBeCanceled
                ? ct.Register(() => tcs.TrySetCanceled())
                : default;

            var confirmSub = _dialogView.OnConfirmClick.Subscribe(_ =>
            {
                confirmed = true;
                tcs.TrySetResult();
            });

            var cancelSub = _dialogView.OnCancelClick.Subscribe(_ =>
            {
                confirmed = false;
                tcs.TrySetResult();
            });

            try
            {
                await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                confirmed = false;
            }
            finally
            {
                ctr.Dispose();
                confirmSub.Dispose();
                cancelSub.Dispose();

                // 닫기 — CancellationToken.None으로 호출 (원본 ct는 이미 취소됐을 수 있음)
                if (_animatedPanel != null)
                    await _animatedPanel.HideAsync(CancellationToken.None);
                else
                    _dialogView.Hide();
            }

            return confirmed;
        }

        public void Dispose()
        {
        }
    }
}
