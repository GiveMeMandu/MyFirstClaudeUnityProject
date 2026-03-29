using System;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 5: Presenter — 건물 상세 팝업 로직.
    /// ResourceModel과 연동하여 비용 체크 + 업그레이드 처리.
    /// Pure C# IDisposable (MonoBehaviour 아님).
    /// </summary>
    public class BuildingDetailPresenter : IDisposable
    {
        private readonly ResourceModel _resourceModel;
        private readonly BuildingDetailView _view;
        private BuildingData _currentBuilding;

        public BuildingDetailPresenter(ResourceModel resourceModel, BuildingDetailView view)
        {
            _resourceModel = resourceModel;
            _view = view;

            _view.OnUpgradeClicked += HandleUpgrade;
            _view.OnCloseClicked   += HandleClose;
        }

        public void ShowBuilding(BuildingData building)
        {
            _currentBuilding = building;
            _view.DisplayBuilding(building);
            UpdateAffordability();
            _view.Show();
        }

        private void HandleUpgrade()
        {
            if (_currentBuilding == null || _currentBuilding.IsMaxLevel) return;

            int goldCost = _currentBuilding.GoldCost;
            int woodCost = _currentBuilding.WoodCost;

            if (_resourceModel.Gold < goldCost || _resourceModel.Wood < woodCost)
            {
                _view.SetStatus("Not enough resources!");
                return;
            }

            // Spend resources individually
            _resourceModel.SpendGold(goldCost);
            _resourceModel.SpendWood(woodCost);

            _currentBuilding.Level++;

            _view.DisplayBuilding(_currentBuilding);
            UpdateAffordability();
            _view.SetStatus($"Upgraded to Level {_currentBuilding.Level}!");
        }

        private void HandleClose()
        {
            _currentBuilding = null;
            _view.Hide();
        }

        private void UpdateAffordability()
        {
            if (_currentBuilding == null || _currentBuilding.IsMaxLevel)
            {
                _view.SetUpgradeEnabled(false);
                return;
            }

            bool canAfford = _resourceModel.Gold >= _currentBuilding.GoldCost
                          && _resourceModel.Wood >= _currentBuilding.WoodCost;
            _view.SetUpgradeEnabled(canAfford);
        }

        public void Dispose()
        {
            _view.OnUpgradeClicked -= HandleUpgrade;
            _view.OnCloseClicked   -= HandleClose;
        }
    }
}
