using System;
using R3;
using UIStudy.Animation.Views;
using VContainer.Unity;

namespace UIStudy.Animation.Presenters
{
    /// <summary>
    /// 버튼 마이크로-인터랙션 Presenter.
    /// 각 버튼의 클릭 이벤트를 View의 애니메이션 메서드에 연결하고, 클릭 수를 카운트한다.
    /// </summary>
    public class ButtonEffectsPresenter : IInitializable, IDisposable
    {
        private readonly ButtonEffectsView _view;
        private readonly CompositeDisposable _disposables = new();
        private readonly int[] _clickCounts = new int[4];

        public ButtonEffectsPresenter(ButtonEffectsView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            // 0: Scale Punch
            _view.ScalePunchButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _view.PlayScalePunch();
                    _view.UpdateCountText(0, ++_clickCounts[0]);
                })
                .AddTo(_disposables);

            // 1: Color Flash
            _view.ColorFlashButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _view.PlayColorFlash();
                    _view.UpdateCountText(1, ++_clickCounts[1]);
                })
                .AddTo(_disposables);

            // 2: Shake
            _view.ShakeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _view.PlayShake();
                    _view.UpdateCountText(2, ++_clickCounts[2]);
                })
                .AddTo(_disposables);

            // 3: Bounce
            _view.BounceButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _view.PlayBounce();
                    _view.UpdateCountText(3, ++_clickCounts[3]);
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
