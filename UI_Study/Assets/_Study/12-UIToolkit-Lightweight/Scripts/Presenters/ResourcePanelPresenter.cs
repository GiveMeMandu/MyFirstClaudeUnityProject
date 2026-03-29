using System;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 2: Presenter — Pure C# class (MonoBehaviour 아님).
    /// View event 구독 → Model 업데이트, Model event 구독 → View 갱신.
    /// IDisposable로 이벤트 해제 보장.
    /// </summary>
    public class ResourcePanelPresenter : IDisposable
    {
        private readonly ResourceModel _model;
        private readonly ResourcePanelView _view;

        private const int GainAmount = 10;
        private const int SpendAmount = 5;

        public ResourcePanelPresenter(ResourceModel model, ResourcePanelView view)
        {
            _model = model;
            _view = view;

            // View → Presenter (사용자 입력)
            _view.OnGainClicked  += HandleGain;
            _view.OnSpendClicked += HandleSpend;

            // Model → View (상태 변경 알림)
            _model.GoldChanged += _view.SetGold;
            _model.WoodChanged += _view.SetWood;
            _model.FoodChanged += _view.SetFood;
        }

        public void Initialize()
        {
            // 초기 표시
            _view.SetGold(_model.Gold);
            _view.SetWood(_model.Wood);
            _view.SetFood(_model.Food);
            _view.SetStatus("Ready");
        }

        private void HandleGain()
        {
            _model.GainAll(GainAmount);
            _view.SetStatus($"+{GainAmount} all resources!");
        }

        private void HandleSpend()
        {
            if (_model.SpendAll(SpendAmount))
            {
                _view.SetStatus($"-{SpendAmount} all resources");
            }
            else
            {
                _view.SetStatus("Not enough resources!");
            }
        }

        public void Dispose()
        {
            _view.OnGainClicked  -= HandleGain;
            _view.OnSpendClicked -= HandleSpend;

            _model.GoldChanged -= _view.SetGold;
            _model.WoodChanged -= _view.SetWood;
            _model.FoodChanged -= _view.SetFood;
        }
    }
}
