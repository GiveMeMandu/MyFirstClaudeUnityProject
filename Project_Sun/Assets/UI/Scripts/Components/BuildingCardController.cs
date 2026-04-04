using System;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.UI.Util;

namespace ProjectSun.UI.Components
{
    public enum BuildingCategory { Production, Defense, Support, Special }

    public struct BuildingData
    {
        public int Id;
        public string Name;
        public string Description;
        public BuildingCategory Category;
        public int CostBasic;
        public int CostAdvanced;
        public int SocketCount;
        public bool IsBuilt;
    }

    /// <summary>
    /// Controls a single building card in the construction panel ScrollView.
    /// </summary>
    public class BuildingCardController
    {
        public BuildingData Data { get; private set; }
        public VisualElement Root { get; private set; }
        public event Action<BuildingCardController> OnSelected;

        private readonly Label _titleLabel;
        private readonly Label _categoryLabel;
        private readonly Label _descLabel;
        private readonly Label _costBasicLabel;
        private readonly Label _costAdvancedLabel;
        private readonly VisualElement _icon;
        private bool _isSelected;

        public BuildingCardController(BuildingData data, int currentBasic, int currentAdvanced)
        {
            Data = data;

            // Build the card element tree in code (matching BuildingCard.uxml structure)
            Root = new VisualElement();
            Root.AddToClassList("building-card");
            Root.userData = this;

            // Header
            var header = new VisualElement();
            header.AddToClassList("building-header");

            _icon = new VisualElement();
            _icon.AddToClassList("building-icon");
            string iconClass = data.Category switch
            {
                BuildingCategory.Production => "building-icon--production",
                BuildingCategory.Defense => "building-icon--defense",
                BuildingCategory.Support => "building-icon--support",
                BuildingCategory.Special => "building-icon--special",
                _ => ""
            };
            _icon.AddToClassList(iconClass);

            var titleCol = new VisualElement();
            titleCol.style.flexDirection = FlexDirection.Column;

            _titleLabel = new Label(data.Name);
            _titleLabel.AddToClassList("building-title");

            _categoryLabel = new Label(data.Category.ToString());
            _categoryLabel.AddToClassList("building-category");

            titleCol.Add(_titleLabel);
            titleCol.Add(_categoryLabel);

            header.Add(_icon);
            header.Add(titleCol);
            Root.Add(header);

            // Description
            _descLabel = new Label(data.Description);
            _descLabel.AddToClassList("building-desc");
            Root.Add(_descLabel);

            // Cost row
            var costRow = new VisualElement();
            costRow.AddToClassList("cost-row");

            if (data.CostBasic > 0)
            {
                var basicItem = new VisualElement();
                basicItem.AddToClassList("cost-item");
                var basicIcon = new VisualElement();
                basicIcon.AddToClassList("cost-icon");
                basicIcon.AddToClassList("resource-icon--basic");
                _costBasicLabel = new Label(data.CostBasic.ToString());
                _costBasicLabel.AddToClassList("cost-value");
                _costBasicLabel.AddToClassList(currentBasic >= data.CostBasic
                    ? "cost-value--sufficient" : "cost-value--insufficient");
                basicItem.Add(basicIcon);
                basicItem.Add(_costBasicLabel);
                costRow.Add(basicItem);
            }

            if (data.CostAdvanced > 0)
            {
                var advItem = new VisualElement();
                advItem.AddToClassList("cost-item");
                var advIcon = new VisualElement();
                advIcon.AddToClassList("cost-icon");
                advIcon.AddToClassList("resource-icon--advanced");
                _costAdvancedLabel = new Label(data.CostAdvanced.ToString());
                _costAdvancedLabel.AddToClassList("cost-value");
                _costAdvancedLabel.AddToClassList(currentAdvanced >= data.CostAdvanced
                    ? "cost-value--sufficient" : "cost-value--insufficient");
                advItem.Add(advIcon);
                advItem.Add(_costAdvancedLabel);
                costRow.Add(advItem);
            }

            Root.Add(costRow);

            if (data.IsBuilt)
                Root.AddToClassList("building-card--built");

            // Click handler
            Root.RegisterCallback<ClickEvent>(HandleClicked);
        }

        private void HandleClicked(ClickEvent evt)
        {
            OnSelected?.Invoke(this);
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (selected)
                Root.AddToClassList("building-card--selected");
            else
                Root.RemoveFromClassList("building-card--selected");
        }

        public void UpdateCostColors(int currentBasic, int currentAdvanced)
        {
            UpdateCostLabel(_costBasicLabel, Data.CostBasic, currentBasic);
            UpdateCostLabel(_costAdvancedLabel, Data.CostAdvanced, currentAdvanced);
        }

        public void MarkBuilt()
        {
            var d = Data;
            d.IsBuilt = true;
            Data = d;
            Root.AddToClassList("building-card--built");
        }

        private static void UpdateCostLabel(Label label, int cost, int current)
        {
            if (label == null) return;
            label.RemoveFromClassList("cost-value--sufficient");
            label.RemoveFromClassList("cost-value--insufficient");
            label.AddToClassList(current >= cost
                ? "cost-value--sufficient" : "cost-value--insufficient");
        }
    }
}
