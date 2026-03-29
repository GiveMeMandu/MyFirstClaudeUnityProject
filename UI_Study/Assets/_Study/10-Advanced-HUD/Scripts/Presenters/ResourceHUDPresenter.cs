using System;
using R3;
using UIStudy.AdvancedHUD.Models;
using UIStudy.AdvancedHUD.Views;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.AdvancedHUD.Presenters
{
    /// <summary>
    /// Resource HUD Presenter — 모델 속성 -> 뷰 바인딩 + +/- 버튼 와이어링.
    /// </summary>
    public class ResourceHUDPresenter : IInitializable, IDisposable
    {
        private readonly ResourceHUDModel _model;
        private readonly ResourceHUDView _view;
        private readonly CompositeDisposable _disposables = new();

        private const int ResourceDelta = 25;
        private const int PopulationDelta = 5;

        public ResourceHUDPresenter(ResourceHUDModel model, ResourceHUDView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            // 아이콘 설정
            _view.GoldBar.SetIcon("\u26c0"); // coin-like symbol
            _view.WoodBar.SetIcon("\u2692"); // hammer/pick
            _view.StoneBar.SetIcon("\u26f0"); // mountain
            _view.FoodBar.SetIcon("\u2615"); // food-like symbol
            _view.PopulationBar.SetIcon("\u263a"); // smiley face

            // 인구만 바 표시
            _view.GoldBar.SetBarVisible(false);
            _view.WoodBar.SetBarVisible(false);
            _view.StoneBar.SetBarVisible(false);
            _view.FoodBar.SetBarVisible(false);
            _view.PopulationBar.SetBarVisible(true);

            // 초기값 즉시 설정
            _view.GoldBar.SetValueImmediate(_model.Gold.Value);
            _view.WoodBar.SetValueImmediate(_model.Wood.Value);
            _view.StoneBar.SetValueImmediate(_model.Stone.Value);
            _view.FoodBar.SetValueImmediate(_model.Food.Value);
            _view.PopulationBar.SetValueImmediate(_model.Population.Value, ResourceHUDModel.MaxPopulation);

            // 모델 -> 뷰 구독 (Skip(1)로 초기값 스킵)
            _model.Gold
                .Skip(1)
                .Subscribe(v => _view.GoldBar.SetValue(v))
                .AddTo(_disposables);

            _model.Wood
                .Skip(1)
                .Subscribe(v => _view.WoodBar.SetValue(v))
                .AddTo(_disposables);

            _model.Stone
                .Skip(1)
                .Subscribe(v => _view.StoneBar.SetValue(v))
                .AddTo(_disposables);

            _model.Food
                .Skip(1)
                .Subscribe(v => _view.FoodBar.SetValue(v))
                .AddTo(_disposables);

            _model.Population
                .Skip(1)
                .Subscribe(v => _view.PopulationBar.SetValue(v, ResourceHUDModel.MaxPopulation))
                .AddTo(_disposables);

            // +/- 버튼 바인딩
            BindResourceButtons(_view.GoldBar, _model.Gold, ResourceDelta);
            BindResourceButtons(_view.WoodBar, _model.Wood, ResourceDelta);
            BindResourceButtons(_view.StoneBar, _model.Stone, ResourceDelta);
            BindResourceButtons(_view.FoodBar, _model.Food, ResourceDelta);
            BindResourceButtons(_view.PopulationBar, _model.Population, PopulationDelta, ResourceHUDModel.MaxPopulation);
        }

        private void BindResourceButtons(
            ResourceBarView bar,
            ReactiveProperty<int> property,
            int delta,
            int max = int.MaxValue)
        {
            bar.AddButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    property.Value = Mathf.Min(property.Value + delta, max);
                })
                .AddTo(_disposables);

            bar.SubtractButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    property.Value = Mathf.Max(property.Value - delta, 0);
                })
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
