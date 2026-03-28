using System;
using Cysharp.Threading.Tasks;
using R3;
using UIStudy.GameUI.Models;
using UIStudy.GameUI.Services;
using UIStudy.GameUI.Views;
using VContainer.Unity;

namespace UIStudy.GameUI.Presenters
{
    /// <summary>
    /// 대화 데모 Presenter — Start 버튼 -> 샘플 대화 시퀀스 실행.
    /// </summary>
    public class DialogDemoPresenter : IInitializable, IDisposable
    {
        private readonly DialogDemoView _demoView;
        private readonly DialogService _dialogService;
        private readonly CompositeDisposable _disposables = new();

        private static readonly DialogLine[] SampleConversation = new[]
        {
            new DialogLine("Captain", "We've arrived at the outpost. Sensors detect unusual activity nearby."),
            new DialogLine("Engineer", "The power grid is fluctuating. I'll need a few minutes to stabilize it."),
            new DialogLine("Captain", "Do what you can. We don't have much time before the storm hits."),
            new DialogLine("Scout", "Captain! Movement detected on the eastern perimeter. Looks like we have company.")
        };

        public DialogDemoPresenter(DialogDemoView demoView, DialogService dialogService)
        {
            _demoView = demoView;
            _dialogService = dialogService;
        }

        public void Initialize()
        {
            _demoView.SetStatus("Ready. Press Start to begin dialog.");

            _demoView.StartDialogButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (!_dialogService.IsPlaying)
                        RunDialogAsync().Forget();
                })
                .AddTo(_disposables);
        }

        private async UniTaskVoid RunDialogAsync()
        {
            _demoView.SetStartButtonInteractable(false);
            _demoView.SetStatus($"Playing dialog... ({SampleConversation.Length} lines)");

            await _dialogService.ShowDialog(SampleConversation);

            _demoView.SetStatus("Dialog complete. Press Start to replay.");
            _demoView.SetStartButtonInteractable(true);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
