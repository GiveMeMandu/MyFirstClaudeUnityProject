using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UIStudy.MVRP.Views;

namespace UIStudy.MVRP.Services
{
    /// <summary>
    /// 다이얼로그 서비스 — UniTask로 다이얼로그 결과를 await 가능하게 제공.
    /// VContainer에서 Singleton으로 등록하여 어디서든 주입받아 사용.
    /// </summary>
    public class DialogService : IDisposable
    {
        private readonly ConfirmDialogView _dialogView;

        public DialogService(ConfirmDialogView dialogView)
        {
            _dialogView = dialogView;
            _dialogView.Hide();
        }

        /// <summary>
        /// 확인 다이얼로그를 표시하고 사용자 응답을 await.
        /// true = 확인, false = 취소.
        /// CancellationToken으로 씬 전환 시 안전하게 정리.
        /// </summary>
        public async UniTask<bool> ShowConfirmAsync(string message, CancellationToken ct = default)
        {
            _dialogView.SetMessage(message);
            _dialogView.Show();

            // 확인 또는 취소 중 먼저 오는 것을 대기
            var confirmed = false;
            var tcs = new UniTaskCompletionSource();

            // CancellationToken 등록
            ct.Register(() => tcs.TrySetCanceled());

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
            finally
            {
                confirmSub.Dispose();
                cancelSub.Dispose();
                _dialogView.Hide();
            }

            return confirmed;
        }

        public void Dispose()
        {
            // 필요 시 추가 정리
        }
    }
}
