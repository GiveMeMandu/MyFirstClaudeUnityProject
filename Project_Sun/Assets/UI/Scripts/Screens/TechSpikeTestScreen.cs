using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.UI.Components;
using ProjectSun.UI.Util;
using ProjectSun.V2.Data;

namespace ProjectSun.UI.Screens
{
    /// <summary>
    /// PoC integration screen: Resource HUD + Construction Panel + Workforce D&D.
    /// Attach to a GameObject with UIDocument referencing TechSpikeTestScreen.uxml.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TechSpikeTestScreen : MonoBehaviour
    {
        // === State ===
        private int _basic = 150;
        private int _advanced = 50;
        private int _relic = 3;
        private int _turn = 1;

        // === Controllers ===
        private ResourceBarController _resourceBar;
        private DragDropManager _dragDrop;
        private readonly List<BuildingCardController> _buildingCards = new();
        private readonly List<CitizenCardController> _citizenCards = new();
        private BuildingCardController _selectedBuilding;

        // === UI refs ===
        private VisualElement _root;
        private ScrollView _buildingList;
        private Label _detailTitle;
        private Label _detailDesc;
        private VisualElement _detailStats;
        private Label _detailCategory;
        private Label _detailCostBasic;
        private Label _detailCostAdvanced;
        private Label _detailSockets;
        private Button _buildButton;
        private VisualElement _buildFlash;
        private Label _statusLog;

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;

            // Cache UI references
            _buildingList = _root.Q<ScrollView>("building-list");
            _detailTitle = _root.Q<Label>("detail-title");
            _detailDesc = _root.Q<Label>("detail-desc");
            _detailStats = _root.Q("detail-stats");
            _detailCategory = _root.Q<Label>("detail-category");
            _detailCostBasic = _root.Q<Label>("detail-cost-basic");
            _detailCostAdvanced = _root.Q<Label>("detail-cost-advanced");
            _detailSockets = _root.Q<Label>("detail-sockets");
            _buildButton = _root.Q<Button>("build-button");
            _buildFlash = _root.Q("build-flash");
            _statusLog = _root.Q<Label>("status-log");

            // 1. Resource HUD
            _resourceBar = new ResourceBarController(_root);
            _resourceBar.SetResourcesImmediate(_basic, _advanced, _relic, _turn);

            // 2. Construction Panel — populate with 10 buildings from GDD
            PopulateBuildings();

            // 3. Workforce D&D — populate citizens and sockets
            PopulateCitizens();
            PopulateSockets();
            _dragDrop = new DragDropManager(_root);
            _dragDrop.OnCitizenPlaced += HandleCitizenPlaced;
            _dragDrop.OnCitizenReturned += HandleCitizenReturned;

            // 4. Test control buttons
            _root.Q<Button>("btn-add-basic").clicked += HandleAddBasic;
            _root.Q<Button>("btn-add-advanced").clicked += HandleAddAdvanced;
            _root.Q<Button>("btn-add-relic").clicked += HandleAddRelic;
            _root.Q<Button>("btn-next-turn").clicked += HandleNextTurn;
            _root.Q<Button>("btn-reset").clicked += HandleReset;

            _buildButton.clicked += HandleBuild;
        }

        private void OnDisable()
        {
            _dragDrop?.Dispose();

            var btnBasic = _root?.Q<Button>("btn-add-basic");
            if (btnBasic != null) btnBasic.clicked -= HandleAddBasic;
            var btnAdv = _root?.Q<Button>("btn-add-advanced");
            if (btnAdv != null) btnAdv.clicked -= HandleAddAdvanced;
            var btnRelic = _root?.Q<Button>("btn-add-relic");
            if (btnRelic != null) btnRelic.clicked -= HandleAddRelic;
            var btnTurn = _root?.Q<Button>("btn-next-turn");
            if (btnTurn != null) btnTurn.clicked -= HandleNextTurn;
            var btnReset = _root?.Q<Button>("btn-reset");
            if (btnReset != null) btnReset.clicked -= HandleReset;

            if (_buildButton != null) _buildButton.clicked -= HandleBuild;
        }

        // =====================================================================
        //  Construction Panel
        // =====================================================================

        private void PopulateBuildings()
        {
            // 10 buildings from Construction.md v0.2
            var buildings = new[]
            {
                new BuildingData { Id = 0,  Name = "Gathering Post",   Description = "Produces basic resources each turn.",      Category = BuildingCategoryV2.Production, CostBasic = 15, CostAdvanced = 0,  SocketCount = 2 },
                new BuildingData { Id = 1,  Name = "Refinery",         Description = "Produces advanced resources from basic.",   Category = BuildingCategoryV2.Production, CostBasic = 20, CostAdvanced = 5,  SocketCount = 2 },
                new BuildingData { Id = 2,  Name = "Storehouse",       Description = "Increases resource storage capacity.",      Category = BuildingCategoryV2.Support,    CostBasic = 18, CostAdvanced = 0,  SocketCount = 1 },
                new BuildingData { Id = 3,  Name = "Watchtower",       Description = "Ranged defense during night combat.",       Category = BuildingCategoryV2.Defense,    CostBasic = 12, CostAdvanced = 3,  SocketCount = 1 },
                new BuildingData { Id = 4,  Name = "Wall",             Description = "Delays enemy advance with HP barrier.",     Category = BuildingCategoryV2.Defense,    CostBasic = 15, CostAdvanced = 5,  SocketCount = 0 },
                new BuildingData { Id = 5,  Name = "Barracks",         Description = "Enables defense squad formation.",          Category = BuildingCategoryV2.Defense,    CostBasic = 25, CostAdvanced = 10, SocketCount = 3 },
                new BuildingData { Id = 6,  Name = "Research Lab",     Description = "Unlocks technology tree research.",         Category = BuildingCategoryV2.Special,    CostBasic = 30, CostAdvanced = 15, SocketCount = 2 },
                new BuildingData { Id = 7,  Name = "Medical Station",  Description = "Heals injured citizens over turns.",        Category = BuildingCategoryV2.Special,    CostBasic = 20, CostAdvanced = 8,  SocketCount = 1 },
                new BuildingData { Id = 8,  Name = "Campfire",         Description = "Attracts new survivors to the base.",       Category = BuildingCategoryV2.Special,    CostBasic = 10, CostAdvanced = 3,  SocketCount = 1 },
                new BuildingData { Id = 9,  Name = "Outpost",          Description = "Extends exploration range and speed.",      Category = BuildingCategoryV2.Special,    CostBasic = 35, CostAdvanced = 15, SocketCount = 2 },
            };

            _buildingList.Clear();
            _buildingCards.Clear();

            foreach (var data in buildings)
            {
                var card = new BuildingCardController(data, _basic, _advanced);
                card.OnSelected += HandleBuildingSelected;
                _buildingCards.Add(card);
                _buildingList.Add(card.Root);
            }
        }

        private void HandleBuildingSelected(BuildingCardController card)
        {
            // Deselect previous
            _selectedBuilding?.SetSelected(false);
            _selectedBuilding = card;
            card.SetSelected(true);

            var d = card.Data;

            _detailTitle.text = d.Name;
            _detailDesc.text = d.Description;
            _detailStats.style.display = DisplayStyle.Flex;
            _detailCategory.text = d.Category.ToString();
            _detailCostBasic.text = d.CostBasic.ToString();
            _detailCostAdvanced.text = d.CostAdvanced > 0 ? d.CostAdvanced.ToString() : "-";
            _detailSockets.text = d.SocketCount > 0 ? d.SocketCount.ToString() : "None";

            bool canAfford = _basic >= d.CostBasic && _advanced >= d.CostAdvanced;
            _buildButton.style.display = d.IsBuilt ? DisplayStyle.None : DisplayStyle.Flex;
            _buildButton.SetEnabled(canAfford);
            _buildButton.text = canAfford ? "BUILD" : "Insufficient Resources";
        }

        private void HandleBuild()
        {
            if (_selectedBuilding == null) return;
            var d = _selectedBuilding.Data;
            if (d.IsBuilt) return;
            if (_basic < d.CostBasic || _advanced < d.CostAdvanced) return;

            // Deduct resources
            _basic -= d.CostBasic;
            _advanced -= d.CostAdvanced;
            _resourceBar.AnimateResourceChange(_basic, _advanced, _relic);

            // Mark built
            _selectedBuilding.MarkBuilt();

            // Update detail panel
            _buildButton.style.display = DisplayStyle.None;
            _detailTitle.text = $"{d.Name} (Built)";

            // Build complete animation — flash + scale pop
            PlayBuildCompleteAnimation();

            // Update all card cost colors
            foreach (var c in _buildingCards)
                c.UpdateCostColors(_basic, _advanced);

            _statusLog.text = $"Built {d.Name}! (-{d.CostBasic} basic, -{d.CostAdvanced} advanced)";
        }

        private void PlayBuildCompleteAnimation()
        {
            // Flash overlay
            _buildFlash.style.opacity = 0.6f;
            SimpleTween.To(
                () => 0.6f,
                v => _buildFlash.style.opacity = v,
                0f, 0.5f,
                SimpleTween.EaseType.OutQuad);

            // Scale pop on detail panel
            var detail = _root.Q("building-detail");
            detail.style.scale = new Scale(new Vector2(1.08f, 1.08f));
            SimpleTween.To(
                () => 1.08f,
                v => detail.style.scale = new Scale(new Vector2(v, v)),
                1f, 0.35f,
                SimpleTween.EaseType.OutBack);
        }

        // =====================================================================
        //  Workforce D&D
        // =====================================================================

        private void PopulateCitizens()
        {
            var pool = _root.Q("citizen-pool");
            pool.Clear();
            _citizenCards.Clear();

            var citizens = new[]
            {
                new CitizenData { Id = 0, Name = "Kim",     Aptitude = CitizenAptitude.Combat,       Proficiency = 2 },
                new CitizenData { Id = 1, Name = "Park",    Aptitude = CitizenAptitude.Construction,  Proficiency = 1 },
                new CitizenData { Id = 2, Name = "Lee",     Aptitude = CitizenAptitude.Exploration,   Proficiency = 0 },
                new CitizenData { Id = 3, Name = "Choi",    Aptitude = CitizenAptitude.Combat,       Proficiency = 1 },
                new CitizenData { Id = 4, Name = "Jung",    Aptitude = CitizenAptitude.Construction,  Proficiency = 3 },
                new CitizenData { Id = 5, Name = "Kang",    Aptitude = CitizenAptitude.Exploration,   Proficiency = 0 },
                new CitizenData { Id = 6, Name = "Yoon",    Aptitude = CitizenAptitude.Combat,       Proficiency = 2 },
            };

            // Load the CitizenCard UXML template
            foreach (var data in citizens)
            {
                // Create card element tree in code (matching CitizenCard.uxml)
                var cardRoot = new VisualElement();
                cardRoot.AddToClassList("citizen-card");
                cardRoot.name = "citizen-card";

                var portrait = new VisualElement();
                portrait.AddToClassList("citizen-portrait");
                portrait.name = "portrait";

                var nameLabel = new Label(data.Name);
                nameLabel.AddToClassList("citizen-name");
                nameLabel.name = "citizen-name";

                var aptLabel = new Label(data.Aptitude.ToString());
                aptLabel.AddToClassList("citizen-aptitude");
                aptLabel.name = "citizen-aptitude";

                var profLabel = new Label($"Lv.{data.Proficiency}");
                profLabel.AddToClassList("citizen-proficiency");
                profLabel.name = "citizen-proficiency";

                cardRoot.Add(portrait);
                cardRoot.Add(nameLabel);
                cardRoot.Add(aptLabel);
                cardRoot.Add(profLabel);

                var controller = new CitizenCardController(cardRoot, data);
                cardRoot.userData = controller;
                _citizenCards.Add(controller);
                pool.Add(cardRoot);
            }
        }

        private void PopulateSockets()
        {
            var area = _root.Q("socket-area");
            area.Clear();

            string[] socketNames = {
                "Gathering Post #1",
                "Gathering Post #2",
                "Watchtower",
                "Barracks Slot 1",
                "Barracks Slot 2",
            };

            foreach (var sname in socketNames)
            {
                var socket = new VisualElement();
                socket.AddToClassList("socket-zone");
                socket.name = sname;
                socket.userData = null; // null = unoccupied

                var label = new Label(sname);
                label.AddToClassList("socket-label");
                label.name = "socket-label";
                socket.Add(label);

                area.Add(socket);
            }
        }

        private void HandleCitizenPlaced(CitizenData citizen, int socketIndex)
        {
            _statusLog.text = $"{citizen.Name} ({citizen.Aptitude}) placed in socket {socketIndex}.";
        }

        private void HandleCitizenReturned(CitizenData citizen)
        {
            _statusLog.text = $"{citizen.Name} returned to unassigned pool.";
        }

        // =====================================================================
        //  Test Controls
        // =====================================================================

        private void HandleAddBasic()
        {
            _basic += 20;
            _resourceBar.AnimateResourceChange(_basic, _advanced, _relic);
            UpdateBuildingAffordability();
        }

        private void HandleAddAdvanced()
        {
            _advanced += 10;
            _resourceBar.AnimateResourceChange(_basic, _advanced, _relic);
            UpdateBuildingAffordability();
        }

        private void HandleAddRelic()
        {
            _relic += 1;
            _resourceBar.AnimateResourceChange(_basic, _advanced, _relic);
        }

        private void HandleNextTurn()
        {
            _turn++;
            _resourceBar.AnimateTurnChange(_turn);

            // Simulate per-turn production
            _basic += 12;
            _advanced += 4;
            _resourceBar.AnimateResourceChange(_basic, _advanced, _relic);
            UpdateBuildingAffordability();
        }

        private void HandleReset()
        {
            _basic = 150;
            _advanced = 50;
            _relic = 3;
            _turn = 1;
            _selectedBuilding = null;

            _resourceBar.SetResourcesImmediate(_basic, _advanced, _relic, _turn);

            // Repopulate everything
            PopulateBuildings();
            PopulateCitizens();
            PopulateSockets();

            _detailTitle.text = "Select a building";
            _detailDesc.text = "Click a building card to view details.";
            _detailStats.style.display = DisplayStyle.None;
            _buildButton.style.display = DisplayStyle.None;
            _statusLog.text = "Reset complete. Drag a citizen onto a socket to assign.";

            // Rebuild drag drop manager
            _dragDrop?.Dispose();
            _dragDrop = new DragDropManager(_root);
            _dragDrop.OnCitizenPlaced += HandleCitizenPlaced;
            _dragDrop.OnCitizenReturned += HandleCitizenReturned;
        }

        private void UpdateBuildingAffordability()
        {
            foreach (var c in _buildingCards)
                c.UpdateCostColors(_basic, _advanced);

            // Update detail panel if a building is selected
            if (_selectedBuilding != null)
                HandleBuildingSelected(_selectedBuilding);
        }
    }
}
