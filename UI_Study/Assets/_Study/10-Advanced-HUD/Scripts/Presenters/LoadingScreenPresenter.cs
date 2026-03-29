using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UIStudy.AdvancedHUD.Models;
using UIStudy.AdvancedHUD.Services;
using UIStudy.AdvancedHUD.Views;
using VContainer.Unity;

namespace UIStudy.AdvancedHUD.Presenters
{
    /// <summary>
    /// Loading Screen Presenter — Start 버튼 -> FakeLoadingService -> 뷰 갱신.
    /// </summary>
    public class LoadingScreenPresenter : IInitializable, IDisposable
    {
        private readonly LoadingModel _model;
        private readonly LoadingScreenView _loadingView;
        private readonly LoadingDemoView _demoView;
        private readonly FakeLoadingService _loadingService;
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _cts;

        public LoadingScreenPresenter(
            LoadingModel model,
            LoadingScreenView loadingView,
            LoadingDemoView demoView,
            FakeLoadingService loadingService)
        {
            _model = model;
            _loadingView = loadingView;
            _demoView = demoView;
            _loadingService = loadingService;
        }

        public void Initialize()
        {
            // Progress 변경 구독 -> 프로그레스 바 갱신
            _model.Progress
                .Subscribe(progress => _loadingView.SetProgress(progress))
                .AddTo(_disposables);

            // CurrentTask 변경 구독 -> 태스크 텍스트 갱신
            _model.CurrentTask
                .Subscribe(task => _loadingView.SetCurrentTask(task))
                .AddTo(_disposables);

            // IsLoading 변경 구독 -> 로딩 스크린 표시/숨김 + 버튼 상태
            _model.IsLoading
                .Subscribe(isLoading =>
                {
                    if (isLoading)
                    {
                        _loadingView.Show();
                        _demoView.SetButtonInteractable(false);
                        _demoView.SetStatus("Loading in progress...");
                    }
                    else
                    {
                        // 완료 후 잠시 대기 후 숨김
                        HideAfterDelayAsync().Forget();
                    }
                })
                .AddTo(_disposables);

            // Start Loading 버튼 바인딩
            _demoView.StartLoadingButton.OnClickAsObservable()
                .Subscribe(_ => StartLoadingAsync().Forget())
                .AddTo(_disposables);

            _demoView.SetStatus("Press 'Start Loading' to begin.");
        }

        private async UniTaskVoid StartLoadingAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            try
            {
                await _loadingService.RunLoadingAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                _demoView.SetStatus("Loading cancelled.");
                _demoView.SetButtonInteractable(true);
            }
        }

        private async UniTaskVoid HideAfterDelayAsync()
        {
            // 완료 표시를 잠시 보여준 뒤 숨김
            await UniTask.Delay(800);
            _loadingView.Hide();
            _demoView.SetButtonInteractable(true);
            _demoView.SetStatus("Loading complete! Press again to restart.");
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _disposables.Dispose();
        }
    }
}
