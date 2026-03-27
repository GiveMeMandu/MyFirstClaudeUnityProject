using System;
using R3;
using UIStudy.MVRP.Models;
using UIStudy.MVRP.Views;
using VContainer.Unity;

namespace UIStudy.MVRP.Presenters
{
    /// <summary>
    /// 자원 HUD Presenter — Pure C#.
    /// Model의 ReactiveProperty를 View에 바인딩하고,
    /// View의 버튼 이벤트를 Model 로직에 연결한다.
    /// </summary>
    public class ResourceHUDPresenter : IInitializable, IDisposable
    {
        private readonly ResourceModel _model;
        private readonly ResourceHUDView _view;
        private readonly CompositeDisposable _disposables = new();

        private const int GoldPerClick = 10;
        private const int WoodPerClick = 5;
        private const int GoldCost = 25;
        private const int WoodCost = 15;

        public ResourceHUDPresenter(ResourceModel model, ResourceHUDView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            // Model → View 바인딩: 자원 값 변화 → 텍스트 업데이트
            _model.Gold
                .Subscribe(gold =>
                {
                    _view.SetGold(gold);
                    _view.SetSpendGoldInteractable(gold >= GoldCost);
                })
                .AddTo(_disposables);

            _model.Wood
                .Subscribe(wood =>
                {
                    _view.SetWood(wood);
                    _view.SetSpendWoodInteractable(wood >= WoodCost);
                })
                .AddTo(_disposables);

            _model.Population
                .Subscribe(_view.SetPopulation)
                .AddTo(_disposables);

            // View → Model 바인딩: 버튼 클릭 → Model 로직
            _view.OnAddGoldClick
                .Subscribe(_ => _model.AddGold(GoldPerClick))
                .AddTo(_disposables);

            _view.OnSpendGoldClick
                .Subscribe(_ => _model.TrySpendGold(GoldCost))
                .AddTo(_disposables);

            _view.OnAddWoodClick
                .Subscribe(_ => _model.AddWood(WoodPerClick))
                .AddTo(_disposables);

            _view.OnSpendWoodClick
                .Subscribe(_ => _model.TrySpendWood(WoodCost))
                .AddTo(_disposables);

            _view.OnAddPopulationClick
                .Subscribe(_ => _model.AddPopulation(1))
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
