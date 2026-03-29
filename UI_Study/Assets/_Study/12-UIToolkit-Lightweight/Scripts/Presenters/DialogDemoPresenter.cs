using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 3: Presenter — 다이얼로그 호출 + 결과 처리 + 로딩 데모.
    /// </summary>
    public class DialogDemoPresenter : IDisposable
    {
        private readonly ConfirmDialogView _dialog;
        private readonly LoadingScreenView _loading;
        private readonly DialogDemoView _demoView;
        private CancellationTokenSource _cts;

        public DialogDemoPresenter(DialogDemoView demoView, ConfirmDialogView dialog,
            LoadingScreenView loading, CancellationToken destroyToken)
        {
            _demoView = demoView;
            _dialog = dialog;
            _loading = loading;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);

            _demoView.OnTriggerDialogClicked += HandleTriggerDialog;
            _demoView.OnTriggerLoadingClicked += HandleTriggerLoading;
        }

        private void HandleTriggerDialog()
        {
            ShowDialogAsync(_cts.Token).Forget();
        }

        private void HandleTriggerLoading()
        {
            SimulateLoadingAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid ShowDialogAsync(CancellationToken ct)
        {
            bool confirmed = await _dialog.ShowAsync(
                "Do you want to spend 50 gold?", ct, "Confirm Purchase");

            _demoView.SetResult(confirmed
                ? "Confirmed! Gold spent."
                : "Cancelled.");
        }

        private async UniTaskVoid SimulateLoadingAsync(CancellationToken ct)
        {
            _loading.Show();

            try
            {
                // IProgress<float> 패턴으로 진행률 보고
                var progress = new Progress<float>(v => _loading.SetProgress(v));

                await SimulateWorkAsync(progress, ct);

                _demoView.SetResult("Loading complete!");
            }
            finally
            {
                _loading.Hide();
            }
        }

        private async UniTask SimulateWorkAsync(IProgress<float> progress,
            CancellationToken ct)
        {
            const int steps = 20;
            for (int i = 0; i <= steps; i++)
            {
                ct.ThrowIfCancellationRequested();
                progress.Report((float)i / steps);
                await UniTask.Delay(100, cancellationToken: ct);
            }
        }

        public void Dispose()
        {
            _demoView.OnTriggerDialogClicked -= HandleTriggerDialog;
            _demoView.OnTriggerLoadingClicked -= HandleTriggerLoading;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
