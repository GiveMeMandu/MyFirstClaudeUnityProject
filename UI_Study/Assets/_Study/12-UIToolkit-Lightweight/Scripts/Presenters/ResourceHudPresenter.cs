using System;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 6: Presenter — GameResourceModel의 변경을 HUD View에 바인딩.
    /// Pure C# IDisposable. 초기값 표시 + 변경 시 자동 갱신.
    /// </summary>
    public class ResourceHudPresenter : IDisposable
    {
        private readonly GameResourceModel _model;
        private readonly ResourceHudView _view;

        public ResourceHudPresenter(GameResourceModel model, ResourceHudView view)
        {
            _model = model;
            _view = view;

            // Model → View 바인딩
            _model.GoldChanged += _view.SetGold;
            _model.WoodChanged += _view.SetWood;
            _model.FoodChanged += _view.SetFood;
            _model.PopChanged  += _view.SetPop;
        }

        public void Initialize()
        {
            // 초기값 표시 (플래시 없이)
            _view.SetInitialValues(
                _model.Gold,
                _model.Wood,
                _model.Food,
                _model.Pop
            );
        }

        public void Dispose()
        {
            _model.GoldChanged -= _view.SetGold;
            _model.WoodChanged -= _view.SetWood;
            _model.FoodChanged -= _view.SetFood;
            _model.PopChanged  -= _view.SetPop;
        }
    }
}
