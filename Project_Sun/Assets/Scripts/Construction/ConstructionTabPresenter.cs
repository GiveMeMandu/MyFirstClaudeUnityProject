using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Core;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Construction
{
    /// <summary>
    /// 낮 페이즈 건설 탭 Presenter (UI Toolkit).
    /// GameState.buildings를 읽어 슬롯 리스트 + 상세 패널 갱신.
    /// BuildingDataSO 레지스트리에서 건물 정보 조회.
    /// SF-CON-009.
    /// </summary>
    public class ConstructionTabPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] UIDocument uiDocument;
        [SerializeField] SORegistry soRegistry;

        VisualElement _root;

        // Slot list
        ScrollView _slotList;

        // Detail panel
        VisualElement _emptyPrompt;
        VisualElement _detailContent;
        Label _detailName;
        Label _detailCategory;
        Label _detailDesc;

        // HP
        VisualElement _hpSection;
        Label _hpLabel;
        VisualElement _hpFill;

        // Stats
        Label _statProduction;
        Label _statStorage;
        Label _statAttack;
        Label _statRange;

        // Build
        VisualElement _buildSection;
        Label _buildCostBasic;
        Label _buildCostAdvanced;
        Button _btnBuild;

        // Upgrade
        VisualElement _upgradeSection;
        Label _branchAName, _branchADesc, _branchACostBasic, _branchACostAdvanced;
        Label _branchBName, _branchBDesc, _branchBCostBasic, _branchBCostAdvanced;
        VisualElement _branchACard, _branchBCard;

        // Repair
        VisualElement _repairSection;
        Button _btnRepair;

        GameState _gameState;
        int _selectedSlotIndex = -1;
        List<VisualElement> _slotElements = new();

        /// <summary>GameState 주입.</summary>
        public void Initialize(GameState state)
        {
            _gameState = state;
        }

        void OnEnable()
        {
            if (uiDocument == null) return;
            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheElements();
        }

        /// <summary>탭 표시. 낮 페이즈에 호출.</summary>
        public void Show()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.Flex;
            RefreshSlotList();
        }

        /// <summary>탭 숨김.</summary>
        public void Hide()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.None;
        }

        void CacheElements()
        {
            _slotList = _root.Q<ScrollView>("slot-list");
            _emptyPrompt = _root.Q("empty-prompt");
            _detailContent = _root.Q("detail-content");
            _detailName = _root.Q<Label>("detail-name");
            _detailCategory = _root.Q<Label>("detail-category");
            _detailDesc = _root.Q<Label>("detail-desc");

            _hpSection = _root.Q("hp-section");
            _hpLabel = _root.Q<Label>("hp-label");
            _hpFill = _root.Q("hp-fill");

            _statProduction = _root.Q<Label>("stat-production");
            _statStorage = _root.Q<Label>("stat-storage");
            _statAttack = _root.Q<Label>("stat-attack");
            _statRange = _root.Q<Label>("stat-range");

            _buildSection = _root.Q("build-section");
            _buildCostBasic = _root.Q<Label>("build-cost-basic");
            _buildCostAdvanced = _root.Q<Label>("build-cost-advanced");
            _btnBuild = _root.Q<Button>("btn-build");

            _upgradeSection = _root.Q("upgrade-section");
            _branchACard = _root.Q("branch-a");
            _branchBCard = _root.Q("branch-b");
            _branchAName = _root.Q<Label>("branch-a-name");
            _branchADesc = _root.Q<Label>("branch-a-desc");
            _branchACostBasic = _root.Q<Label>("branch-a-cost-basic");
            _branchACostAdvanced = _root.Q<Label>("branch-a-cost-advanced");
            _branchBName = _root.Q<Label>("branch-b-name");
            _branchBDesc = _root.Q<Label>("branch-b-desc");
            _branchBCostBasic = _root.Q<Label>("branch-b-cost-basic");
            _branchBCostAdvanced = _root.Q<Label>("branch-b-cost-advanced");

            _repairSection = _root.Q("repair-section");
            _btnRepair = _root.Q<Button>("btn-repair");
        }

        void RefreshSlotList()
        {
            if (_slotList == null || _gameState == null) return;

            _slotList.Clear();
            _slotElements.Clear();

            for (int i = 0; i < _gameState.buildings.Count; i++)
            {
                var building = _gameState.buildings[i];
                var slotElement = CreateSlotElement(building, i);
                _slotList.Add(slotElement);
                _slotElements.Add(slotElement);
            }

            // 선택 초기화
            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _gameState.buildings.Count)
                SelectSlot(_selectedSlotIndex);
            else
                ClearSelection();
        }

        VisualElement CreateSlotElement(BuildingRuntimeState building, int index)
        {
            var item = new VisualElement();
            item.AddToClassList("slot-item");

            // 상태 아이콘
            var stateIcon = new VisualElement();
            stateIcon.AddToClassList("slot-state-icon");
            stateIcon.AddToClassList(GetStateIconClass(building.state));
            item.Add(stateIcon);

            // 이름
            var nameLabel = new Label();
            nameLabel.AddToClassList("slot-name");
            string displayName = GetBuildingDisplayName(building);
            nameLabel.text = $"{building.slotId}: {displayName}";
            item.Add(nameLabel);

            // HP (활성 건물만)
            if (building.state == BuildingSlotStateV2.Active ||
                building.state == BuildingSlotStateV2.Damaged)
            {
                var hpLabel = new Label();
                hpLabel.AddToClassList("slot-hp");
                hpLabel.text = $"{building.currentHP}/{building.maxHP}";
                item.Add(hpLabel);
            }

            // 상태별 스타일
            if (building.state == BuildingSlotStateV2.Locked)
                item.AddToClassList("slot-item--locked");
            if (building.state == BuildingSlotStateV2.Damaged)
                item.AddToClassList("slot-item--damaged");

            // 클릭 이벤트
            int slotIndex = index;
            item.RegisterCallback<ClickEvent>(_ => SelectSlot(slotIndex));

            return item;
        }

        void SelectSlot(int index)
        {
            // 이전 선택 해제
            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _slotElements.Count)
                _slotElements[_selectedSlotIndex].RemoveFromClassList("slot-item--selected");

            _selectedSlotIndex = index;

            // 새 선택 표시
            if (index >= 0 && index < _slotElements.Count)
                _slotElements[index].AddToClassList("slot-item--selected");

            UpdateDetailPanel();
        }

        void ClearSelection()
        {
            _selectedSlotIndex = -1;
            _emptyPrompt?.SetDisplay(true);
            _detailContent?.SetDisplay(false);
        }

        void UpdateDetailPanel()
        {
            if (_gameState == null || _selectedSlotIndex < 0 ||
                _selectedSlotIndex >= _gameState.buildings.Count)
            {
                ClearSelection();
                return;
            }

            _emptyPrompt?.SetDisplay(false);
            _detailContent?.SetDisplay(true);

            var building = _gameState.buildings[_selectedSlotIndex];
            var so = soRegistry != null ? soRegistry.GetBuilding(building.buildingId) : null;

            // 기본 정보
            string displayName = so != null ? so.displayName : building.buildingId ?? "Empty Slot";
            string category = so != null ? so.category.ToString() : "";
            string desc = so != null ? so.description : "";

            _detailName?.SetText(displayName);
            _detailCategory?.SetText(category);
            _detailDesc?.SetText(desc);

            // HP
            bool showHP = building.state == BuildingSlotStateV2.Active ||
                          building.state == BuildingSlotStateV2.Damaged;
            _hpSection?.SetDisplay(showHP);
            if (showHP)
            {
                _hpLabel?.SetText($"HP: {building.currentHP} / {building.maxHP}");
                float ratio = building.maxHP > 0 ? (float)building.currentHP / building.maxHP : 0f;
                _hpFill?.SetWidth(ratio * 100f);
            }

            // Stats
            if (so != null)
            {
                _statProduction?.SetText(so.basicPerTurn > 0 || so.advancedPerTurn > 0
                    ? $"B+{so.basicPerTurn} A+{so.advancedPerTurn}/turn"
                    : "-");
                _statStorage?.SetText(so.basicCapBonus > 0 || so.advancedCapBonus > 0
                    ? $"B+{so.basicCapBonus} A+{so.advancedCapBonus}"
                    : "-");
                _statAttack?.SetText(so.attackPower > 0 ? $"{so.attackPower}" : "-");
                _statRange?.SetText(so.attackRange > 0 ? $"{so.attackRange}" : "-");
            }

            // Sections: 상태에 따라 표시/숨김
            bool isEmpty = building.state == BuildingSlotStateV2.Unlocked;
            bool isActive = building.state == BuildingSlotStateV2.Active;
            bool isDamaged = building.state == BuildingSlotStateV2.Damaged;

            _buildSection?.SetDisplay(isEmpty);
            _upgradeSection?.SetDisplay(isActive && so != null && so.branchA != null);
            _repairSection?.SetDisplay(isDamaged);

            // Build cost
            if (isEmpty && so != null)
            {
                _buildCostBasic?.SetText(so.buildCost.basic.ToString());
                _buildCostAdvanced?.SetText(so.buildCost.advanced.ToString());

                bool canAfford = _gameState.resources.CanAfford(so.buildCost.basic, so.buildCost.advanced);
                _btnBuild?.SetEnabled(canAfford);
                if (canAfford)
                    _btnBuild?.RemoveFromClassList("action-btn--disabled");
                else
                    _btnBuild?.AddToClassList("action-btn--disabled");
            }

            // Upgrade branches
            if (isActive && so != null)
            {
                PopulateBranch(so.branchA, _branchAName, _branchADesc, _branchACostBasic, _branchACostAdvanced, _branchACard);
                PopulateBranch(so.branchB, _branchBName, _branchBDesc, _branchBCostBasic, _branchBCostAdvanced, _branchBCard);
            }
        }

        void PopulateBranch(UpgradeBranch branch, Label name, Label desc,
            Label costBasic, Label costAdvanced, VisualElement card)
        {
            if (branch == null || card == null)
            {
                card?.SetDisplay(false);
                return;
            }

            card.SetDisplay(true);
            name?.SetText(branch.displayName ?? "Upgrade");
            desc?.SetText(branch.description ?? "");
            costBasic?.SetText(branch.upgradeCost.basic.ToString());
            costAdvanced?.SetText(branch.upgradeCost.advanced.ToString());
        }

        string GetBuildingDisplayName(BuildingRuntimeState building)
        {
            if (string.IsNullOrEmpty(building.buildingId))
            {
                return building.state == BuildingSlotStateV2.Locked ? "[Locked]" : "[Empty]";
            }

            var so = soRegistry != null ? soRegistry.GetBuilding(building.buildingId) : null;
            return so != null ? so.displayName : building.buildingId;
        }

        static string GetStateIconClass(BuildingSlotStateV2 state)
        {
            return state switch
            {
                BuildingSlotStateV2.Active => "slot-state--active",
                BuildingSlotStateV2.Unlocked => "slot-state--empty",
                BuildingSlotStateV2.Locked => "slot-state--locked",
                BuildingSlotStateV2.Damaged => "slot-state--damaged",
                BuildingSlotStateV2.UnderConstruction => "slot-state--constructing",
                BuildingSlotStateV2.Repairing => "slot-state--constructing",
                _ => "slot-state--empty"
            };
        }
    }

    /// <summary>VisualElement 헬퍼 확장.</summary>
    static class VisualElementExtensions
    {
        public static void SetDisplay(this VisualElement el, bool visible)
        {
            if (el != null) el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetText(this Label label, string text)
        {
            if (label != null) label.text = text;
        }

        public static void SetWidth(this VisualElement el, float percent)
        {
            if (el != null) el.style.width = Length.Percent(percent);
        }
    }
}
