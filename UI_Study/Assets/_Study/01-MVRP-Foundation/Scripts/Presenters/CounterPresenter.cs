using System;
using R3;
using UIStudy.MVRP.Models;
using UIStudy.MVRP.Views;
using VContainer.Unity;

namespace UIStudy.MVRP.Presenters
{
    /// <summary>
    /// 카운터 Presenter — Pure C# (MonoBehaviour 아님).
    /// VContainer에서 생성자 주입으로 Model과 View를 받는다.
    /// IInitializable.Initialize()에서 구독을 설정한다.
    /// </summary>
    public class CounterPresenter : IInitializable, IDisposable
    {
        private readonly CounterModel _model;
        private readonly CounterView _view;
        private readonly CompositeDisposable _disposables = new();

        public CounterPresenter(CounterModel model, CounterView view)
        {
            _model = model;
            _view = view;
        }

        // Subscribe는 반드시 Initialize()에서 — 생성자에서 하면 데드락 위험
        public void Initialize()
        {
            // Model → View: 카운트 변화 시 텍스트 업데이트
            _model.Count
                .Subscribe(_view.SetCountText)
                .AddTo(_disposables);

            // View → Model: 버튼 클릭 → Model 로직 실행
            _view.OnIncrementClick
                .Subscribe(_ => _model.Increment())
                .AddTo(_disposables);

            _view.OnDecrementClick
                .Subscribe(_ => _model.Decrement())
                .AddTo(_disposables);

            _view.OnResetClick
                .Subscribe(_ => _model.Reset())
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
