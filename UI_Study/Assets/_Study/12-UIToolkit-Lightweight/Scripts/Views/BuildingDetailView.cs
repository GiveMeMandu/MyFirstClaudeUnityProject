using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 5: 건물 상세 팝업 View — UIDocument 보유 MonoBehaviour.
    /// 건물 데이터를 표시하고, 업그레이드/닫기 이벤트를 노출.
    /// </summary>
    public class BuildingDetailView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        // 캐싱 요소
        private VisualElement _backdrop;
        private Label _nameLabel;
        private Label _levelLabel;
        private Label _descLabel;
        private Label _goldCostLabel;
        private Label _woodCostLabel;
        private Button _upgradeBtn;
        private Button _closeBtn;
        private Label _statusLabel;

        // View 이벤트
        public event Action OnUpgradeClicked;
        public event Action OnCloseClicked;

        // named methods for subscription
        private void HandleUpgradeClicked() => OnUpgradeClicked?.Invoke();
        private void HandleCloseClicked() => OnCloseClicked?.Invoke();

        private void OnEnable()
        {
            var root = _document.rootVisualElement;

            _backdrop      = root.Q<VisualElement>("backdrop");
            _nameLabel     = root.Q<Label>("building-name");
            _levelLabel    = root.Q<Label>("building-level");
            _descLabel     = root.Q<Label>("building-desc");
            _goldCostLabel = root.Q<Label>("gold-cost");
            _woodCostLabel = root.Q<Label>("wood-cost");
            _upgradeBtn    = root.Q<Button>("btn-upgrade");
            _closeBtn      = root.Q<Button>("btn-close");
            _statusLabel   = root.Q<Label>("status-label");

            _upgradeBtn.clicked += HandleUpgradeClicked;
            _closeBtn.clicked   += HandleCloseClicked;
        }

        private void OnDisable()
        {
            if (_upgradeBtn != null) _upgradeBtn.clicked -= HandleUpgradeClicked;
            if (_closeBtn != null)   _closeBtn.clicked   -= HandleCloseClicked;
        }

        // Display methods
        public void DisplayBuilding(BuildingData data)
        {
            _nameLabel.text     = data.Name;
            _levelLabel.text    = $"Level {data.Level} / {data.MaxLevel}";
            _descLabel.text     = data.Description;
            _goldCostLabel.text = data.GoldCost.ToString();
            _woodCostLabel.text = data.WoodCost.ToString();

            _upgradeBtn.SetEnabled(!data.IsMaxLevel);
            _upgradeBtn.text = data.IsMaxLevel ? "MAX" : "Upgrade";
        }

        public void SetUpgradeEnabled(bool enabled)
        {
            _upgradeBtn.SetEnabled(enabled);
        }

        public void SetStatus(string message)
        {
            _statusLabel.text = message;
        }

        public void Show()
        {
            _backdrop.style.display = DisplayStyle.Flex;
            _statusLabel.text = "";
        }

        public void Hide()
        {
            _backdrop.style.display = DisplayStyle.None;
        }
    }
}
