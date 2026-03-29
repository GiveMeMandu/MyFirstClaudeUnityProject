using System;
using System.Collections.Generic;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 6: Presenter — 건설 메뉴 로직.
    /// 카탈로그로부터 카드 그리드 구성, 어포더빌리티 체크, 건설 액션 처리.
    /// Pure C# IDisposable.
    /// </summary>
    public class BuildMenuPresenter : IDisposable
    {
        private readonly GameResourceModel _resourceModel;
        private readonly BuildMenuView _menuView;
        private readonly TooltipView _tooltipView;
        private IReadOnlyList<BuildingData> _buildings;

        /// <summary>
        /// 건물 선택 시 외부(Bootstrapper 등)에 알림.
        /// </summary>
        public event Action<BuildingData> OnBuildingBuilt;

        public BuildMenuPresenter(
            GameResourceModel resourceModel,
            BuildMenuView menuView,
            TooltipView tooltipView)
        {
            _resourceModel = resourceModel;
            _menuView = menuView;
            _tooltipView = tooltipView;

            // View → Presenter 이벤트 구독
            _menuView.OnBuildingSelected += HandleBuildingSelected;
            _menuView.OnCardHoverEnter   += HandleCardHoverEnter;
            _menuView.OnCardHoverExit    += HandleCardHoverExit;

            // 자원 변경 시 어포더빌리티 갱신
            _resourceModel.GoldChanged += HandleResourceChanged;
            _resourceModel.WoodChanged += HandleResourceChanged;
        }

        public void Initialize(IReadOnlyList<BuildingData> buildings)
        {
            _buildings = buildings;
            _menuView.BuildCards(buildings);
            RefreshAllCardStates();
        }

        private void HandleBuildingSelected(int index)
        {
            if (index < 0 || index >= _buildings.Count) return;

            var building = _buildings[index];

            if (building.IsMaxLevel)
            {
                _menuView.SetStatus($"{building.Name} is already at max level.");
                return;
            }

            if (!_resourceModel.CanAfford(building.GoldCost, building.WoodCost))
            {
                _menuView.SetStatus($"Cannot afford {building.Name}!");
                return;
            }

            // 비용 차감
            _resourceModel.SpendBuildCost(building.GoldCost, building.WoodCost);
            building.LevelUp();

            _menuView.SetStatus($"Built {building.Name} (Lv.{building.Level})!");
            RefreshAllCardStates();

            OnBuildingBuilt?.Invoke(building);
        }

        private void HandleCardHoverEnter(int index, UnityEngine.Vector2 position)
        {
            if (index < 0 || index >= _buildings.Count) return;
            _tooltipView.ShowTooltip(_buildings[index], position);
        }

        private void HandleCardHoverExit()
        {
            _tooltipView.HideTooltip();
        }

        private void HandleResourceChanged(int _)
        {
            RefreshAllCardStates();
        }

        private void RefreshAllCardStates()
        {
            if (_buildings == null) return;

            for (int i = 0; i < _buildings.Count; i++)
            {
                var building = _buildings[i];
                CardState state;

                if (building.IsMaxLevel)
                {
                    state = CardState.Locked;
                }
                else if (_resourceModel.CanAfford(building.GoldCost, building.WoodCost))
                {
                    state = CardState.Affordable;
                }
                else
                {
                    state = CardState.TooExpensive;
                }

                _menuView.UpdateCardState(i, state);
            }
        }

        public void Dispose()
        {
            _menuView.OnBuildingSelected -= HandleBuildingSelected;
            _menuView.OnCardHoverEnter   -= HandleCardHoverEnter;
            _menuView.OnCardHoverExit    -= HandleCardHoverExit;

            _resourceModel.GoldChanged -= HandleResourceChanged;
            _resourceModel.WoodChanged -= HandleResourceChanged;
        }
    }
}
