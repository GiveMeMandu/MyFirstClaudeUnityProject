using System;
using R3;
using UIStudy.Animation.Views;
using VContainer.Unity;

namespace UIStudy.Animation.Presenters
{
    /// <summary>
    /// 패널 전환 Presenter.
    /// 4가지 트랜지션 버튼의 클릭 이벤트를 View의 전환 메서드에 연결한다.
    /// </summary>
    public class PanelTransitionPresenter : IInitializable, IDisposable
    {
        private readonly PanelTransitionView _view;
        private readonly CompositeDisposable _disposables = new();

        public PanelTransitionPresenter(PanelTransitionView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            _view.FadeButton.OnClickAsObservable()
                .Subscribe(_ => _view.TransitionFade())
                .AddTo(_disposables);

            _view.SlideLeftButton.OnClickAsObservable()
                .Subscribe(_ => _view.TransitionSlideLeft())
                .AddTo(_disposables);

            _view.ScalePopButton.OnClickAsObservable()
                .Subscribe(_ => _view.TransitionScalePopIn())
                .AddTo(_disposables);

            _view.FlipYButton.OnClickAsObservable()
                .Subscribe(_ => _view.TransitionFlipY())
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
