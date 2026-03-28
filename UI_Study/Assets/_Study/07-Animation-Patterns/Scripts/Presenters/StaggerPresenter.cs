using System;
using R3;
using UIStudy.Animation.Models;
using UIStudy.Animation.Views;
using VContainer.Unity;

namespace UIStudy.Animation.Presenters
{
    /// <summary>
    /// Stagger 리스트 Presenter.
    /// Toggle 버튼으로 IsVisible 상태를 전환하고,
    /// Speed 슬라이더 값에 따라 View의 Show/Hide 애니메이션을 트리거한다.
    /// </summary>
    public class StaggerPresenter : IInitializable, IDisposable
    {
        private readonly StaggerModel _model;
        private readonly StaggerListView _view;
        private readonly CompositeDisposable _disposables = new();

        public StaggerPresenter(StaggerModel model, StaggerListView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            // Toggle 버튼 클릭 → 모델 상태 전환
            _view.ToggleButton.OnClickAsObservable()
                .Subscribe(_ => _model.Toggle())
                .AddTo(_disposables);

            // 모델 상태 변화 → View 애니메이션 트리거
            _model.IsVisible
                .Skip(1) // 초기값 무시
                .Subscribe(isVisible =>
                {
                    // 슬라이더 값: 0.5 ~ 2.0 범위
                    float speed = _view.SpeedSlider.value;
                    if (isVisible)
                        _view.ShowItems(speed);
                    else
                        _view.HideItems(speed);
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
