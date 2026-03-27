using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// WhenAny + CancelAfterSlim 타임아웃 다이얼로그.
    /// 지정 시간 내 응답 없으면 자동 취소.
    /// </summary>
    public class TimedDialogService
    {
        /// <summary>
        /// 타임아웃이 있는 확인 다이얼로그.
        /// </summary>
        public async UniTask<bool> ShowTimedConfirmAsync(
            Button confirmButton,
            Button cancelButton,
            float timeoutSeconds,
            CancellationToken lifetimeToken)
        {
            using var timeoutCts = new CancellationTokenSource();
            timeoutCts.CancelAfterSlim(TimeSpan.FromSeconds(timeoutSeconds));

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                lifetimeToken, timeoutCts.Token);

            var confirmed = false;
            var tcs = new UniTaskCompletionSource();
            var ctr = linkedCts.Token.Register(() => tcs.TrySetCanceled());

            var confirmSub = confirmButton.OnClickAsObservable()
                .Subscribe(_ => { confirmed = true; tcs.TrySetResult(); });
            var cancelSub = cancelButton.OnClickAsObservable()
                .Subscribe(_ => { confirmed = false; tcs.TrySetResult(); });

            try
            {
                await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                confirmed = false;
                var isTimeout = timeoutCts.IsCancellationRequested;
                Debug.Log(isTimeout
                    ? "[TimedDialog] 타임아웃으로 자동 취소"
                    : "[TimedDialog] 외부 취소");
            }
            finally
            {
                ctr.Dispose();
                confirmSub.Dispose();
                cancelSub.Dispose();
            }

            return confirmed;
        }
    }
}
