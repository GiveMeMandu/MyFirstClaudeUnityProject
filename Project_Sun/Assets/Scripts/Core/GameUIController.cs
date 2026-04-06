using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Data;
using ProjectSun.V2.Construction;
using ProjectSun.V2.Defense;
using ProjectSun.V2.Defense.Bridge;
using ProjectSun.V2.Exploration;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// Single unified UI controller for all game screens.
    /// Replaces MenuScreenPresenter, BattleHUDPresenter, DayTabController,
    /// ConstructionTabPresenter, WorkforceTabPresenter, ExplorationTabPresenter,
    /// WavePreviewPresenter, and EncounterPopupPresenter.
    ///
    /// Uses ONE UIDocument with ALL screens as child containers (display:none by default).
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] UIDocument uiDocument;

        [Header("Core References")]
        [SerializeField] GameDirector director;
        [SerializeField] BattleUIBridge battleUIBridge;
        [SerializeField] TimeScaleController timeScaleController;
        [SerializeField] GameOverManager gameOverManager;
        [SerializeField] ExplorationBridge explorationBridge;
        [SerializeField] EncounterBridge encounterBridge;
        [SerializeField] TechTreeBridge techTreeBridge;
        [SerializeField] PolicyBridge policyBridge;
        [SerializeField] SORegistry soRegistry;

        [Header("Exploration Config")]
        [SerializeField] int stubNodeCount = 20;

        // ── Events (consumed by GameDirector) ──

        /// <summary>NEW GAME requested from menu.</summary>
        public event Action OnNewGameRequested;

        /// <summary>CONTINUE requested from menu.</summary>
        public event Action OnContinueRequested;

        /// <summary>RETRY requested from game-over screen.</summary>
        public event Action OnRetryRequested;

        /// <summary>Wave preview closed — start battle.</summary>
        public event Action OnPreviewClosed;

        /// <summary>Battle result closed — next turn.</summary>
        public event Action OnResultClosed;

        // ── Root ──
        VisualElement _root;

        // ── Section containers ──
        VisualElement _mainMenu;
        VisualElement _baseSelect;
        VisualElement _dayHud;
        VisualElement _dayContent;
        VisualElement _constructionPanel;
        VisualElement _workforcePanel;
        VisualElement _explorationPanel;
        VisualElement _battleHud;
        VisualElement _wavePreviewOverlay;
        VisualElement _battleResultOverlay;
        VisualElement _researchPanel;
        VisualElement _encounterOverlay;
        VisualElement _policyOverlay;
        VisualElement _gameOverScreen;

        // ── Menu elements ──
        Button _btnContinue;
        Label _gameoverTitle;
        Label _statTurns, _statCitizens, _statBuildings, _statEnemies, _statResources;

        // ── Day HUD elements ──
        Button _tabConstruction, _tabWorkforce, _tabExploration;
        Button _btnStartNight;
        Label _resBasic, _resAdvanced, _resRelic, _turnBadge;

        // ── Construction elements ──
        ScrollView _slotList;
        VisualElement _emptyPrompt, _detailContent;
        Label _detailName, _detailCategory, _detailDesc;
        VisualElement _hpSection;
        Label _hpLabel;
        VisualElement _hpFill;
        Label _statProduction, _statStorage, _statAttack, _statRange;
        VisualElement _buildSection;
        Label _buildCostBasic, _buildCostAdvanced;
        Button _btnBuild;
        VisualElement _upgradeSection;
        Label _branchAName, _branchADesc, _branchACostBasic, _branchACostAdvanced;
        Label _branchBName, _branchBDesc, _branchBCostBasic, _branchBCostAdvanced;
        VisualElement _branchACard, _branchBCard;
        VisualElement _repairSection;
        Button _btnRepair;

        // ── Workforce elements ──
        Label _citizenSummary;
        ScrollView _citizenList;
        Button _filterAll, _filterIdle, _filterAssigned, _filterInjured;
        VisualElement _wfDetailEmpty, _wfDetailContent;
        VisualElement _wfDetailPortrait;
        Label _wfDetailName, _wfDetailAptitude, _wfDetailLevel;
        Label _wfStatusLabel, _wfStatusDetail;
        VisualElement _assignSection;
        Button _btnAssignCombat, _btnUnassign;

        // ── Exploration elements ──
        VisualElement _nodeGrid;
        Label _expeditionStatus;
        ScrollView _expeditionList;
        Button _btnDispatch, _btnBonfire;

        // ── Battle HUD elements ──
        Label _waveLabel;
        VisualElement _waveProgressFill;
        Label _enemyCountLabel;
        Label _squadCountLabel;
        VisualElement _squadHPFill;
        Label _squadHPLabel;
        VisualElement _hqHPFill;
        Label _hqHPLabel;
        VisualElement _buildingHPFill;
        Label _buildingHPLabel;
        Button _btn1x, _btn2x, _btnPause;
        VisualElement _gameOverOverlay;
        Label _gameOverSub;

        // ── Wave Preview elements ──
        VisualElement _dirNorth, _dirEast, _dirSouth, _dirWest;
        Label _dirNorthCount, _dirEastCount, _dirSouthCount, _dirWestCount;
        Label _previewTotal, _previewWaves, _previewThreat, _scoutInfo;

        // ── Battle Result elements ──
        Label _resultGrade, _rewardBasic, _rewardAdvanced, _rewardRelic;
        Label _resultKilled, _resultDamage;
        VisualElement _damageList;
        Label _damageListItems;

        // ── Encounter elements ──
        Label _encounterTitle, _encounterDesc;
        VisualElement _choiceList;

        // ── Research elements ──
        Button _tabResearch;
        Label _researchStatus;
        ScrollView _techNodeList;
        VisualElement _researchDetailEmpty, _researchDetailContent;
        Label _techName, _techCategory, _techDesc;
        Label _techCostBasic, _techCostAdvanced, _techTurns, _techPrereqs;
        VisualElement _techProgressSection;
        Label _techProgressLabel;
        VisualElement _techProgressFill;
        Button _btnStartResearch;

        // ── Policy elements ──
        Label _policyTitle, _policyDesc;
        Label _policyAName, _policyADesc, _policyAEffects;
        Label _policyBName, _policyBDesc, _policyBEffects;
        Button _btnPolicyA, _btnPolicyB;

        // ── State ──
        GameState _gameState;
        bool _battleHudActive;

        // Construction state
        int _selectedSlotIndex = -1;
        readonly List<VisualElement> _slotElements = new();

        // Workforce state
        int _selectedCitizenIndex = -1;
        string _activeFilter = "all";
        readonly List<int> _filteredIndices = new();
        readonly List<VisualElement> _rowElements = new();

        // Exploration state
        int _selectedNode = -1;
        readonly List<VisualElement> _nodeElements = new();

        // Research state
        int _selectedTechIndex = -1;
        readonly List<VisualElement> _techElements = new();
        TechNode[] _allTechNodes;

        // Policy state
        PolicyData _currentPolicy;

        // ================================================================
        // Lifecycle
        // ================================================================

        void OnEnable()
        {
            if (uiDocument == null) return;
            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheAllElements();
            SetupAllButtons();

            if (timeScaleController != null)
                timeScaleController.OnSpeedChanged += OnSpeedChanged;
            if (gameOverManager != null)
                gameOverManager.OnGameOver += OnBattleGameOver;
            if (encounterBridge != null)
            {
                encounterBridge.OnEncounterStarted += ShowEncounterPopup;
                encounterBridge.OnEncounterEnded += HideEncounterPopup;
            }
            if (techTreeBridge != null)
            {
                techTreeBridge.OnResearchCompleted += OnResearchCompleted;
            }
            if (policyBridge != null)
            {
                policyBridge.OnPolicyAvailable += ShowPolicyPopup;
            }
        }

        void OnDisable()
        {
            if (timeScaleController != null)
                timeScaleController.OnSpeedChanged -= OnSpeedChanged;
            if (gameOverManager != null)
                gameOverManager.OnGameOver -= OnBattleGameOver;
            if (encounterBridge != null)
            {
                encounterBridge.OnEncounterStarted -= ShowEncounterPopup;
                encounterBridge.OnEncounterEnded -= HideEncounterPopup;
            }
            if (techTreeBridge != null)
            {
                techTreeBridge.OnResearchCompleted -= OnResearchCompleted;
            }
            if (policyBridge != null)
            {
                policyBridge.OnPolicyAvailable -= ShowPolicyPopup;
            }
        }

        void Update()
        {
            // Battle HUD polling
            if (_battleHudActive && battleUIBridge != null)
            {
                UpdateBattleWaveInfo();
                UpdateBattleSquadInfo();
                UpdateBattleBuildingInfo();
            }

            // Day resource display
            if (_gameState != null && _dayHud != null &&
                _dayHud.resolvedStyle.display == DisplayStyle.Flex)
            {
                _resBasic?.SetText(_gameState.resources.basicAmount.ToString());
                _resAdvanced?.SetText(_gameState.resources.advancedAmount.ToString());
                _resRelic?.SetText(_gameState.resources.relicAmount.ToString());
                _turnBadge?.SetText($"TURN {_gameState.currentTurn}");
            }
        }

        // ================================================================
        // Initialize
        // ================================================================

        /// <summary>Inject GameState. Called by GameDirector when game starts.</summary>
        public void Initialize(GameState state)
        {
            _gameState = state;
        }

        // ================================================================
        // Cache + Setup
        // ================================================================

        void CacheAllElements()
        {
            // Sections
            _mainMenu = _root.Q("main-menu");
            _baseSelect = _root.Q("base-select");
            _dayHud = _root.Q("day-hud");
            _dayContent = _root.Q("day-content");
            _constructionPanel = _root.Q("construction-panel");
            _workforcePanel = _root.Q("workforce-panel");
            _explorationPanel = _root.Q("exploration-panel");
            _researchPanel = _root.Q("research-panel");
            _battleHud = _root.Q("battle-hud");
            _wavePreviewOverlay = _root.Q("wave-preview-overlay");
            _battleResultOverlay = _root.Q("battle-result-overlay");
            _encounterOverlay = _root.Q("encounter-overlay");
            _policyOverlay = _root.Q("policy-overlay");
            _gameOverScreen = _root.Q("game-over-screen");

            // Menu
            _btnContinue = _root.Q<Button>("btn-continue");
            _gameoverTitle = _root.Q<Label>("gameover-title");
            _statTurns = _root.Q<Label>("stat-turns");
            _statCitizens = _root.Q<Label>("stat-citizens");
            _statBuildings = _root.Q<Label>("stat-buildings");
            _statEnemies = _root.Q<Label>("stat-enemies");
            _statResources = _root.Q<Label>("stat-resources");

            // Day HUD
            _tabConstruction = _root.Q<Button>("tab-construction");
            _tabWorkforce = _root.Q<Button>("tab-workforce");
            _tabExploration = _root.Q<Button>("tab-exploration");
            _btnStartNight = _root.Q<Button>("btn-start-night");
            _resBasic = _root.Q<Label>("res-basic");
            _resAdvanced = _root.Q<Label>("res-advanced");
            _resRelic = _root.Q<Label>("res-relic");
            _turnBadge = _root.Q<Label>("turn-badge");

            // Construction
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

            // Workforce
            _citizenSummary = _root.Q<Label>("citizen-summary");
            _citizenList = _root.Q<ScrollView>("citizen-list");
            _filterAll = _root.Q<Button>("filter-all");
            _filterIdle = _root.Q<Button>("filter-idle");
            _filterAssigned = _root.Q<Button>("filter-assigned");
            _filterInjured = _root.Q<Button>("filter-injured");
            _wfDetailEmpty = _root.Q("wf-detail-empty");
            _wfDetailContent = _root.Q("wf-detail-content");
            _wfDetailPortrait = _root.Q("wf-detail-portrait");
            _wfDetailName = _root.Q<Label>("wf-detail-name");
            _wfDetailAptitude = _root.Q<Label>("wf-detail-aptitude");
            _wfDetailLevel = _root.Q<Label>("wf-detail-level");
            _wfStatusLabel = _root.Q<Label>("wf-status-label");
            _wfStatusDetail = _root.Q<Label>("wf-status-detail");
            _assignSection = _root.Q("assign-section");
            _btnAssignCombat = _root.Q<Button>("btn-assign-combat");
            _btnUnassign = _root.Q<Button>("btn-unassign");

            // Exploration
            _nodeGrid = _root.Q("node-grid");
            _expeditionStatus = _root.Q<Label>("expedition-status");
            _expeditionList = _root.Q<ScrollView>("expedition-list");
            _btnDispatch = _root.Q<Button>("btn-dispatch");
            _btnBonfire = _root.Q<Button>("btn-bonfire");

            // Battle HUD
            _waveLabel = _root.Q<Label>("wave-label");
            _waveProgressFill = _root.Q("wave-progress-fill");
            _enemyCountLabel = _root.Q<Label>("enemy-count-label");
            _squadCountLabel = _root.Q<Label>("squad-count-label");
            _squadHPFill = _root.Q("squad-hp-fill");
            _squadHPLabel = _root.Q<Label>("squad-hp-label");
            _hqHPFill = _root.Q("hq-hp-fill");
            _hqHPLabel = _root.Q<Label>("hq-hp-label");
            _buildingHPFill = _root.Q("building-hp-fill");
            _buildingHPLabel = _root.Q<Label>("building-hp-label");
            _btn1x = _root.Q<Button>("btn-speed-1x");
            _btn2x = _root.Q<Button>("btn-speed-2x");
            _btnPause = _root.Q<Button>("btn-pause");
            _gameOverOverlay = _root.Q("game-over-overlay");
            _gameOverSub = _root.Q<Label>("game-over-sub");

            // Wave Preview
            _dirNorth = _root.Q("dir-north");
            _dirEast = _root.Q("dir-east");
            _dirSouth = _root.Q("dir-south");
            _dirWest = _root.Q("dir-west");
            _dirNorthCount = _root.Q<Label>("dir-north-count");
            _dirEastCount = _root.Q<Label>("dir-east-count");
            _dirSouthCount = _root.Q<Label>("dir-south-count");
            _dirWestCount = _root.Q<Label>("dir-west-count");
            _previewTotal = _root.Q<Label>("preview-total");
            _previewWaves = _root.Q<Label>("preview-waves");
            _previewThreat = _root.Q<Label>("preview-threat");
            _scoutInfo = _root.Q<Label>("scout-info");

            // Battle Result
            _resultGrade = _root.Q<Label>("result-grade");
            _rewardBasic = _root.Q<Label>("reward-basic");
            _rewardAdvanced = _root.Q<Label>("reward-advanced");
            _rewardRelic = _root.Q<Label>("reward-relic");
            _resultKilled = _root.Q<Label>("result-killed");
            _resultDamage = _root.Q<Label>("result-damage");
            _damageList = _root.Q("damage-list");
            _damageListItems = _root.Q<Label>("damage-list-items");

            // Encounter
            _encounterTitle = _root.Q<Label>("encounter-title");
            _encounterDesc = _root.Q<Label>("encounter-desc");
            _choiceList = _root.Q("choice-list");

            // Research
            _tabResearch = _root.Q<Button>("tab-research");
            _researchStatus = _root.Q<Label>("research-status");
            _techNodeList = _root.Q<ScrollView>("tech-node-list");
            _researchDetailEmpty = _root.Q("research-detail-empty");
            _researchDetailContent = _root.Q("research-detail-content");
            _techName = _root.Q<Label>("tech-name");
            _techCategory = _root.Q<Label>("tech-category");
            _techDesc = _root.Q<Label>("tech-desc");
            _techCostBasic = _root.Q<Label>("tech-cost-basic");
            _techCostAdvanced = _root.Q<Label>("tech-cost-advanced");
            _techTurns = _root.Q<Label>("tech-turns");
            _techPrereqs = _root.Q<Label>("tech-prereqs");
            _techProgressSection = _root.Q("tech-progress-section");
            _techProgressLabel = _root.Q<Label>("tech-progress-label");
            _techProgressFill = _root.Q("tech-progress-fill");
            _btnStartResearch = _root.Q<Button>("btn-start-research");

            // Policy
            _policyTitle = _root.Q<Label>("policy-title");
            _policyDesc = _root.Q<Label>("policy-desc");
            _policyAName = _root.Q<Label>("policy-a-name");
            _policyADesc = _root.Q<Label>("policy-a-desc");
            _policyAEffects = _root.Q<Label>("policy-a-effects");
            _policyBName = _root.Q<Label>("policy-b-name");
            _policyBDesc = _root.Q<Label>("policy-b-desc");
            _policyBEffects = _root.Q<Label>("policy-b-effects");
            _btnPolicyA = _root.Q<Button>("btn-policy-a");
            _btnPolicyB = _root.Q<Button>("btn-policy-b");
        }

        void SetupAllButtons()
        {
            // ── Menu ──
            _root.Q<Button>("btn-new-game")?.RegisterCallback<ClickEvent>(_ => ShowBaseSelect());
            _btnContinue?.RegisterCallback<ClickEvent>(_ => OnContinueRequested?.Invoke());
            _root.Q<Button>("btn-settings")?.RegisterCallback<ClickEvent>(_ =>
                Debug.Log("[Menu] Settings not yet implemented"));
            _root.Q<Button>("btn-quit")?.RegisterCallback<ClickEvent>(_ =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            _root.Q<Button>("btn-start-game")?.RegisterCallback<ClickEvent>(_ =>
            {
                HideAllSections();
                OnNewGameRequested?.Invoke();
            });
            _root.Q<Button>("btn-back-menu")?.RegisterCallback<ClickEvent>(_ => ShowMainMenu());
            _root.Q<Button>("btn-retry")?.RegisterCallback<ClickEvent>(_ =>
            {
                HideAllSections();
                OnRetryRequested?.Invoke();
            });
            _root.Q<Button>("btn-to-menu")?.RegisterCallback<ClickEvent>(_ => ShowMainMenu());

            // ── Day HUD ──
            _tabConstruction?.RegisterCallback<ClickEvent>(_ => ShowDayTab(0));
            _tabWorkforce?.RegisterCallback<ClickEvent>(_ => ShowDayTab(1));
            _tabExploration?.RegisterCallback<ClickEvent>(_ => ShowDayTab(2));
            _tabResearch?.RegisterCallback<ClickEvent>(_ => ShowDayTab(3));
            _btnStartNight?.RegisterCallback<ClickEvent>(_ => director?.StartNight());

            // ── Workforce filters ──
            _filterAll?.RegisterCallback<ClickEvent>(_ => SetWorkforceFilter("all"));
            _filterIdle?.RegisterCallback<ClickEvent>(_ => SetWorkforceFilter("idle"));
            _filterAssigned?.RegisterCallback<ClickEvent>(_ => SetWorkforceFilter("assigned"));
            _filterInjured?.RegisterCallback<ClickEvent>(_ => SetWorkforceFilter("injured"));

            // ── Workforce actions ──
            _btnAssignCombat?.RegisterCallback<ClickEvent>(_ => AssignToCombat());
            _btnUnassign?.RegisterCallback<ClickEvent>(_ => Unassign());

            // ── Exploration actions ──
            _btnDispatch?.RegisterCallback<ClickEvent>(_ => DispatchExpedition());
            _btnBonfire?.RegisterCallback<ClickEvent>(_ => InvestBonfire());

            // ── Research actions ──
            _btnStartResearch?.RegisterCallback<ClickEvent>(_ => StartResearchClicked());

            // ── Policy actions ──
            _btnPolicyA?.RegisterCallback<ClickEvent>(_ => ChoosePolicy(true));
            _btnPolicyB?.RegisterCallback<ClickEvent>(_ => ChoosePolicy(false));

            // ── Speed controls ──
            _btn1x?.RegisterCallback<ClickEvent>(_ =>
                timeScaleController?.SetSpeed(TimeScaleController.TimeSpeed.Normal));
            _btn2x?.RegisterCallback<ClickEvent>(_ =>
                timeScaleController?.SetSpeed(TimeScaleController.TimeSpeed.Fast));
            _btnPause?.RegisterCallback<ClickEvent>(_ =>
                timeScaleController?.TogglePause());

            // ── Wave preview / result ──
            _root.Q<Button>("btn-close-preview")?.RegisterCallback<ClickEvent>(_ =>
            {
                HideWavePreview();
                OnPreviewClosed?.Invoke();
            });
            _root.Q<Button>("btn-close-result")?.RegisterCallback<ClickEvent>(_ =>
            {
                HideBattleResult();
                OnResultClosed?.Invoke();
            });
        }

        // ================================================================
        // Public API — Show/Hide screens
        // ================================================================

        /// <summary>Show main menu (initial state).</summary>
        public void ShowMainMenu()
        {
            HideAllSections();
            _mainMenu?.SetDisplay(true);
            UpdateContinueButton();
        }

        void ShowBaseSelect()
        {
            HideAllSections();
            _baseSelect?.SetDisplay(true);
        }

        /// <summary>Show day phase UI with tab content.</summary>
        public void ShowDayPhase(GameState state)
        {
            _gameState = state;
            _battleHudActive = false;

            _dayHud?.SetDisplay(true);
            _dayContent?.SetDisplay(true);
            ShowDayTab(0); // 기본 Construction 탭 표시
        }

        /// <summary>Switch day tab. 0=Construction, 1=Workforce, 2=Exploration, 3=Research.</summary>
        public void ShowDayTab(int tabIndex)
        {
            // Hide all content panels
            _constructionPanel?.SetDisplay(false);
            _workforcePanel?.SetDisplay(false);
            _explorationPanel?.SetDisplay(false);
            _researchPanel?.SetDisplay(false);

            // Tab styling
            _tabConstruction?.RemoveFromClassList("tab-btn--active");
            _tabWorkforce?.RemoveFromClassList("tab-btn--active");
            _tabExploration?.RemoveFromClassList("tab-btn--active");
            _tabResearch?.RemoveFromClassList("tab-btn--active");

            switch (tabIndex)
            {
                case 0:
                    _tabConstruction?.AddToClassList("tab-btn--active");
                    _constructionPanel?.SetDisplay(true);
                    RefreshConstructionSlotList();
                    break;
                case 1:
                    _tabWorkforce?.AddToClassList("tab-btn--active");
                    _workforcePanel?.SetDisplay(true);
                    RefreshWorkforceList();
                    break;
                case 2:
                    _tabExploration?.AddToClassList("tab-btn--active");
                    _explorationPanel?.SetDisplay(true);
                    RefreshExplorationMap();
                    RefreshExpeditions();
                    break;
                case 3:
                    _tabResearch?.AddToClassList("tab-btn--active");
                    _researchPanel?.SetDisplay(true);
                    RefreshResearchList();
                    break;
            }

            director?.ShowDayTab(tabIndex);
        }

        /// <summary>Hide all day-phase UI.</summary>
        public void HideDayUI()
        {
            _dayHud?.SetDisplay(false);
            _dayContent?.SetDisplay(false);
            _constructionPanel?.SetDisplay(false);
            _workforcePanel?.SetDisplay(false);
            _explorationPanel?.SetDisplay(false);
            _researchPanel?.SetDisplay(false);
        }

        /// <summary>Show battle HUD.</summary>
        public void ShowBattleHUD()
        {
            _battleHudActive = true;
            _battleHud?.SetDisplay(true);
            _gameOverOverlay?.SetDisplay(false);
        }

        /// <summary>Hide battle HUD.</summary>
        public void HideBattleHUD()
        {
            _battleHudActive = false;
            _battleHud?.SetDisplay(false);
        }

        /// <summary>Show wave preview overlay.</summary>
        public void ShowWavePreview(int turn, int totalEnemies, int waveCount, ScoutLevel scoutLevel)
        {
            _wavePreviewOverlay?.SetDisplay(true);

            bool showDetails = scoutLevel >= ScoutLevel.Scouted;

            _previewTotal?.SetText(showDetails ? totalEnemies.ToString() : "???");
            _previewWaves?.SetText(showDetails ? waveCount.ToString() : "???");

            string threat = turn switch
            {
                <= 3 => "Low",
                <= 8 => "Medium",
                <= 15 => "High",
                _ => "Extreme"
            };
            _previewThreat?.SetText(showDetails ? threat : "???");

            int perDir = showDetails ? totalEnemies / 4 : 0;
            SetDirection(_dirNorth, _dirNorthCount, perDir, showDetails);
            SetDirection(_dirEast, _dirEastCount, perDir + (showDetails ? totalEnemies % 4 : 0), showDetails);
            SetDirection(_dirSouth, _dirSouthCount, perDir, showDetails);
            SetDirection(_dirWest, _dirWestCount, perDir, showDetails);

            _scoutInfo?.SetText(scoutLevel switch
            {
                ScoutLevel.Unknown => "Scout level: Unknown \u2014 dispatch expedition for details",
                ScoutLevel.Scouted => "Scout level: Scouted \u2014 basic information available",
                ScoutLevel.Detailed => "Scout level: Detailed \u2014 full intelligence report",
                _ => ""
            });
        }

        /// <summary>Hide wave preview overlay.</summary>
        public void HideWavePreview() => _wavePreviewOverlay?.SetDisplay(false);

        /// <summary>Show battle result overlay.</summary>
        public void ShowBattleResult(WaveResult result)
        {
            if (result == null) return;
            _battleResultOverlay?.SetDisplay(true);

            // Grade
            if (_resultGrade != null)
            {
                _resultGrade.ClearClassList();
                _resultGrade.AddToClassList("result-grade");

                string gradeText;
                string gradeClass;
                switch (result.grade)
                {
                    case DefenseResultGrade.PerfectDefense:
                        gradeText = "PERFECT DEFENSE";
                        gradeClass = "grade--perfect";
                        break;
                    case DefenseResultGrade.MinorDamage:
                        gradeText = "MINOR DAMAGE";
                        gradeClass = "grade--minor";
                        break;
                    case DefenseResultGrade.ModerateDamage:
                        gradeText = "MODERATE DAMAGE";
                        gradeClass = "grade--moderate";
                        break;
                    default:
                        gradeText = "MAJOR DAMAGE";
                        gradeClass = "grade--major";
                        break;
                }
                _resultGrade.text = gradeText;
                _resultGrade.AddToClassList(gradeClass);
            }

            _rewardBasic?.SetText($"+{result.basicReward}");
            _rewardAdvanced?.SetText($"+{result.advancedReward}");
            _rewardRelic?.SetText($"+{result.relicReward}");
            _resultKilled?.SetText($"{result.enemiesDefeated} / {result.enemiesTotal}");
            _resultDamage?.SetText($"{result.damageRatio:P0}");

            bool hasDamaged = result.damagedBuildingSlotIds != null && result.damagedBuildingSlotIds.Count > 0;
            _damageList?.SetDisplay(hasDamaged);
            if (hasDamaged)
                _damageListItems?.SetText(string.Join(", ", result.damagedBuildingSlotIds));
        }

        /// <summary>Hide battle result overlay.</summary>
        public void HideBattleResult() => _battleResultOverlay?.SetDisplay(false);

        /// <summary>Show game-over / victory screen with stats.</summary>
        public void ShowGameOver(GameState state, bool isVictory)
        {
            HideAllSections();
            _gameOverScreen?.SetDisplay(true);

            if (_gameoverTitle != null)
            {
                _gameoverTitle.ClearClassList();
                _gameoverTitle.AddToClassList("gameover-title");

                if (isVictory)
                {
                    _gameoverTitle.text = "VICTORY";
                    _gameoverTitle.AddToClassList("gameover-title--victory");
                }
                else
                {
                    _gameoverTitle.text = "GAME OVER";
                    _gameoverTitle.AddToClassList("gameover-title--defeat");
                }
            }

            if (state != null)
            {
                _statTurns?.SetText(state.currentTurn.ToString());
                _statCitizens?.SetText(state.citizens.Count.ToString());
                _statBuildings?.SetText(state.buildings.Count.ToString());

                int totalEnemies = 0;
                foreach (var w in state.waveHistory)
                    totalEnemies += w.enemiesDefeated;
                _statEnemies?.SetText(totalEnemies.ToString());

                int totalRes = state.resources.basicAmount + state.resources.advancedAmount + state.resources.relicAmount;
                _statResources?.SetText(totalRes.ToString());
            }
        }

        /// <summary>Hide all sections.</summary>
        public void HideAllSections()
        {
            _mainMenu?.SetDisplay(false);
            _baseSelect?.SetDisplay(false);
            _dayHud?.SetDisplay(false);
            _dayContent?.SetDisplay(false);
            _constructionPanel?.SetDisplay(false);
            _workforcePanel?.SetDisplay(false);
            _explorationPanel?.SetDisplay(false);
            _researchPanel?.SetDisplay(false);
            _battleHud?.SetDisplay(false);
            _wavePreviewOverlay?.SetDisplay(false);
            _battleResultOverlay?.SetDisplay(false);
            _encounterOverlay?.SetDisplay(false);
            _policyOverlay?.SetDisplay(false);
            _gameOverScreen?.SetDisplay(false);
            _battleHudActive = false;
        }

        // ================================================================
        // Encounter
        // ================================================================

        void ShowEncounterPopup(EncounterData data)
        {
            _encounterTitle?.SetText(data.Title);
            _encounterDesc?.SetText(data.Description);

            _choiceList?.Clear();

            for (int i = 0; i < data.Choices.Length; i++)
            {
                var choice = data.Choices[i];
                int choiceIdx = i;

                var btn = new Button(() => encounterBridge?.ApplyChoice(choiceIdx));
                btn.AddToClassList("choice-btn");

                if (choice.CostBasic > 0 || choice.CostAdvanced > 0)
                    btn.AddToClassList("choice-btn--cost");

                btn.text = choice.Text;
                _choiceList?.Add(btn);
            }

            _encounterOverlay?.SetDisplay(true);
        }

        void HideEncounterPopup()
        {
            _encounterOverlay?.SetDisplay(false);
        }

        // ================================================================
        // Battle HUD updates (per-frame)
        // ================================================================

        void UpdateBattleWaveInfo()
        {
            float progress = battleUIBridge.WaveProgress;
            int killed = battleUIBridge.TotalEnemiesKilled;
            int spawned = battleUIBridge.TotalEnemiesSpawned;
            int alive = battleUIBridge.AliveEnemyCount;

            if (_waveLabel != null)
                _waveLabel.text = $"WAVE {Mathf.CeilToInt(progress * 10f)}/10";
            if (_waveProgressFill != null)
                _waveProgressFill.style.width = Length.Percent(progress * 100f);
            if (_enemyCountLabel != null)
                _enemyCountLabel.text = $"{alive} alive ({killed}/{spawned} killed)";
        }

        void UpdateBattleSquadInfo()
        {
            int count = battleUIBridge.SquadCount;
            float hp = battleUIBridge.TotalSquadHP;
            float maxHP = battleUIBridge.TotalSquadMaxHP;

            if (_squadCountLabel != null)
                _squadCountLabel.text = $"{count} squads";

            float ratio = maxHP > 0f ? hp / maxHP : 0f;
            if (_squadHPFill != null)
                _squadHPFill.style.width = Length.Percent(ratio * 100f);
            if (_squadHPLabel != null)
                _squadHPLabel.text = $"{hp:F0} / {maxHP:F0}";
        }

        void UpdateBattleBuildingInfo()
        {
            float hqHP = battleUIBridge.HeadquartersHP;
            float hqMaxHP = battleUIBridge.HeadquartersMaxHP;

            float hqRatio = hqMaxHP > 0f ? hqHP / hqMaxHP : 0f;
            if (_hqHPFill != null)
                _hqHPFill.style.width = Length.Percent(hqRatio * 100f);
            if (_hqHPLabel != null)
                _hqHPLabel.text = $"{hqHP:F0} / {hqMaxHP:F0}";

            float totalHP = battleUIBridge.TotalBuildingHP;
            float totalMaxHP = battleUIBridge.TotalBuildingMaxHP;

            float buildingRatio = totalMaxHP > 0f ? totalHP / totalMaxHP : 0f;
            if (_buildingHPFill != null)
                _buildingHPFill.style.width = Length.Percent(buildingRatio * 100f);
            if (_buildingHPLabel != null)
                _buildingHPLabel.text = $"{totalHP:F0} / {totalMaxHP:F0}";
        }

        void OnSpeedChanged(TimeScaleController.TimeSpeed speed)
        {
            _btn1x?.RemoveFromClassList("speed-btn--active");
            _btn2x?.RemoveFromClassList("speed-btn--active");
            _btnPause?.RemoveFromClassList("speed-btn--active");

            switch (speed)
            {
                case TimeScaleController.TimeSpeed.Normal:
                    _btn1x?.AddToClassList("speed-btn--active");
                    break;
                case TimeScaleController.TimeSpeed.Fast:
                    _btn2x?.AddToClassList("speed-btn--active");
                    break;
                case TimeScaleController.TimeSpeed.Paused:
                    _btnPause?.AddToClassList("speed-btn--active");
                    break;
            }
        }

        void OnBattleGameOver(GameOverManager.GameOverReason reason)
        {
            if (_gameOverOverlay != null)
                _gameOverOverlay.style.display = DisplayStyle.Flex;

            if (_gameOverSub != null)
            {
                _gameOverSub.text = reason switch
                {
                    GameOverManager.GameOverReason.HeadquartersDestroyed => "Headquarters destroyed",
                    GameOverManager.GameOverReason.Victory => "Victory!",
                    _ => ""
                };
            }
        }

        // ================================================================
        // Construction Tab
        // ================================================================

        void RefreshConstructionSlotList()
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

            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _gameState.buildings.Count)
                SelectSlot(_selectedSlotIndex);
            else
                ClearConstructionSelection();
        }

        VisualElement CreateSlotElement(BuildingRuntimeState building, int index)
        {
            var item = new VisualElement();
            item.AddToClassList("slot-item");

            var stateIcon = new VisualElement();
            stateIcon.AddToClassList("slot-state-icon");
            stateIcon.AddToClassList(GetStateIconClass(building.state));
            item.Add(stateIcon);

            var nameLabel = new Label();
            nameLabel.AddToClassList("slot-name");
            string displayName = GetBuildingDisplayName(building);
            nameLabel.text = $"{building.slotId}: {displayName}";
            item.Add(nameLabel);

            if (building.state == BuildingSlotStateV2.Active ||
                building.state == BuildingSlotStateV2.Damaged)
            {
                var hpLabel = new Label();
                hpLabel.AddToClassList("slot-hp");
                hpLabel.text = $"{building.currentHP}/{building.maxHP}";
                item.Add(hpLabel);
            }

            if (building.state == BuildingSlotStateV2.Locked)
                item.AddToClassList("slot-item--locked");
            if (building.state == BuildingSlotStateV2.Damaged)
                item.AddToClassList("slot-item--damaged");

            int slotIndex = index;
            item.RegisterCallback<ClickEvent>(_ => SelectSlot(slotIndex));

            return item;
        }

        void SelectSlot(int index)
        {
            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _slotElements.Count)
                _slotElements[_selectedSlotIndex].RemoveFromClassList("slot-item--selected");

            _selectedSlotIndex = index;

            if (index >= 0 && index < _slotElements.Count)
                _slotElements[index].AddToClassList("slot-item--selected");

            UpdateConstructionDetailPanel();
        }

        void ClearConstructionSelection()
        {
            _selectedSlotIndex = -1;
            _emptyPrompt?.SetDisplay(true);
            _detailContent?.SetDisplay(false);
        }

        void UpdateConstructionDetailPanel()
        {
            if (_gameState == null || _selectedSlotIndex < 0 ||
                _selectedSlotIndex >= _gameState.buildings.Count)
            {
                ClearConstructionSelection();
                return;
            }

            _emptyPrompt?.SetDisplay(false);
            _detailContent?.SetDisplay(true);

            var building = _gameState.buildings[_selectedSlotIndex];
            var so = soRegistry != null ? soRegistry.GetBuilding(building.buildingId) : null;

            string displayName = so != null ? so.displayName : building.buildingId ?? "Empty Slot";
            string category = so != null ? so.category.ToString() : "";
            string desc = so != null ? so.description : "";

            _detailName?.SetText(displayName);
            _detailCategory?.SetText(category);
            _detailDesc?.SetText(desc);

            bool showHP = building.state == BuildingSlotStateV2.Active ||
                          building.state == BuildingSlotStateV2.Damaged;
            _hpSection?.SetDisplay(showHP);
            if (showHP)
            {
                _hpLabel?.SetText($"HP: {building.currentHP} / {building.maxHP}");
                float ratio = building.maxHP > 0 ? (float)building.currentHP / building.maxHP : 0f;
                _hpFill?.SetWidth(ratio * 100f);
            }

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

            bool isEmpty = building.state == BuildingSlotStateV2.Unlocked;
            bool isActive = building.state == BuildingSlotStateV2.Active;
            bool isDamaged = building.state == BuildingSlotStateV2.Damaged;

            _buildSection?.SetDisplay(isEmpty);
            _upgradeSection?.SetDisplay(isActive && so != null && so.branchA != null);
            _repairSection?.SetDisplay(isDamaged);

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
                return building.state == BuildingSlotStateV2.Locked ? "[Locked]" : "[Empty]";

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

        // ================================================================
        // Workforce Tab
        // ================================================================

        void SetWorkforceFilter(string filter)
        {
            _activeFilter = filter;

            _filterAll?.RemoveFromClassList("filter-btn--active");
            _filterIdle?.RemoveFromClassList("filter-btn--active");
            _filterAssigned?.RemoveFromClassList("filter-btn--active");
            _filterInjured?.RemoveFromClassList("filter-btn--active");

            switch (filter)
            {
                case "all": _filterAll?.AddToClassList("filter-btn--active"); break;
                case "idle": _filterIdle?.AddToClassList("filter-btn--active"); break;
                case "assigned": _filterAssigned?.AddToClassList("filter-btn--active"); break;
                case "injured": _filterInjured?.AddToClassList("filter-btn--active"); break;
            }

            RefreshWorkforceList();
        }

        void RefreshWorkforceList()
        {
            if (_citizenList == null || _gameState == null) return;

            _citizenList.Clear();
            _rowElements.Clear();
            _filteredIndices.Clear();

            int total = _gameState.citizens.Count;
            int idle = 0, assigned = 0, injured = 0;
            foreach (var c in _gameState.citizens)
            {
                switch (c.state)
                {
                    case CitizenState.Idle: idle++; break;
                    case CitizenState.Assigned: case CitizenState.InCombat: assigned++; break;
                    case CitizenState.Injured: case CitizenState.Recovering: injured++; break;
                }
            }
            _citizenSummary?.SetText($"{total} total | {idle} idle | {assigned} assigned | {injured} injured");

            for (int i = 0; i < _gameState.citizens.Count; i++)
            {
                var citizen = _gameState.citizens[i];
                if (!PassesWorkforceFilter(citizen)) continue;

                _filteredIndices.Add(i);
                var row = CreateCitizenRow(citizen, i);
                _citizenList.Add(row);
                _rowElements.Add(row);
            }

            if (_selectedCitizenIndex >= 0)
                UpdateWorkforceDetail();
            else
                ClearWorkforceSelection();
        }

        bool PassesWorkforceFilter(CitizenRuntimeState citizen)
        {
            return _activeFilter switch
            {
                "idle" => citizen.state == CitizenState.Idle,
                "assigned" => citizen.state == CitizenState.Assigned || citizen.state == CitizenState.InCombat,
                "injured" => citizen.state == CitizenState.Injured || citizen.state == CitizenState.Recovering,
                _ => true
            };
        }

        VisualElement CreateCitizenRow(CitizenRuntimeState citizen, int realIndex)
        {
            var row = new VisualElement();
            row.AddToClassList("citizen-row");

            switch (citizen.state)
            {
                case CitizenState.Injured:
                case CitizenState.Recovering:
                    row.AddToClassList("citizen-row--injured"); break;
                case CitizenState.InCombat:
                    row.AddToClassList("citizen-row--combat"); break;
                case CitizenState.Assigned:
                    row.AddToClassList("citizen-row--assigned"); break;
            }

            var portrait = new VisualElement();
            portrait.AddToClassList("citizen-portrait");
            portrait.AddToClassList(GetPortraitClass(citizen.aptitude));
            row.Add(portrait);

            var info = new VisualElement();
            info.AddToClassList("citizen-info");

            var nameLabel = new Label(citizen.displayName ?? citizen.citizenId);
            nameLabel.AddToClassList("citizen-name");
            info.Add(nameLabel);

            var statusLabel = new Label($"{citizen.state} | {citizen.aptitude}");
            statusLabel.AddToClassList("citizen-status");
            info.Add(statusLabel);

            row.Add(info);

            var levelLabel = new Label($"Lv.{citizen.proficiencyLevel}");
            levelLabel.AddToClassList("citizen-level");
            row.Add(levelLabel);

            int idx = realIndex;
            row.RegisterCallback<ClickEvent>(_ => SelectCitizen(idx));

            return row;
        }

        void SelectCitizen(int realIndex)
        {
            int prevFilteredIdx = _filteredIndices.IndexOf(_selectedCitizenIndex);
            if (prevFilteredIdx >= 0 && prevFilteredIdx < _rowElements.Count)
                _rowElements[prevFilteredIdx].RemoveFromClassList("citizen-row--selected");

            _selectedCitizenIndex = realIndex;

            int filteredIdx = _filteredIndices.IndexOf(realIndex);
            if (filteredIdx >= 0 && filteredIdx < _rowElements.Count)
                _rowElements[filteredIdx].AddToClassList("citizen-row--selected");

            UpdateWorkforceDetail();
        }

        void ClearWorkforceSelection()
        {
            _selectedCitizenIndex = -1;
            _wfDetailEmpty?.SetDisplay(true);
            _wfDetailContent?.SetDisplay(false);
        }

        void UpdateWorkforceDetail()
        {
            if (_gameState == null || _selectedCitizenIndex < 0 ||
                _selectedCitizenIndex >= _gameState.citizens.Count)
            {
                ClearWorkforceSelection();
                return;
            }

            _wfDetailEmpty?.SetDisplay(false);
            _wfDetailContent?.SetDisplay(true);

            var citizen = _gameState.citizens[_selectedCitizenIndex];

            _wfDetailName?.SetText(citizen.displayName ?? citizen.citizenId);
            _wfDetailAptitude?.SetText(citizen.aptitude.ToString());
            _wfDetailLevel?.SetText($"Lv. {citizen.proficiencyLevel}");

            _wfDetailPortrait?.ClearClassList();
            _wfDetailPortrait?.AddToClassList("detail-portrait");
            _wfDetailPortrait?.AddToClassList(GetPortraitClass(citizen.aptitude));

            UpdateCitizenStatus(citizen);

            bool isIdle = citizen.state == CitizenState.Idle;
            bool isAssigned = citizen.state == CitizenState.Assigned || citizen.state == CitizenState.InCombat;

            _btnAssignCombat?.SetDisplay(isIdle);
            _btnUnassign?.SetDisplay(isAssigned);
            _assignSection?.SetDisplay(isIdle || isAssigned);
        }

        void UpdateCitizenStatus(CitizenRuntimeState citizen)
        {
            if (_wfStatusLabel == null) return;

            _wfStatusLabel.ClearClassList();
            _wfStatusLabel.AddToClassList("status-label");

            switch (citizen.state)
            {
                case CitizenState.Idle:
                    _wfStatusLabel.text = "IDLE";
                    _wfStatusLabel.AddToClassList("status--idle");
                    _wfStatusDetail?.SetText("Ready for assignment");
                    break;
                case CitizenState.Assigned:
                    _wfStatusLabel.text = "ASSIGNED";
                    _wfStatusLabel.AddToClassList("status--assigned");
                    _wfStatusDetail?.SetText($"Working at {citizen.assignedSlotId ?? "unknown"}");
                    break;
                case CitizenState.InCombat:
                    _wfStatusLabel.text = "IN COMBAT";
                    _wfStatusLabel.AddToClassList("status--combat");
                    _wfStatusDetail?.SetText("Deployed for night defense");
                    break;
                case CitizenState.Injured:
                    _wfStatusLabel.text = "INJURED";
                    _wfStatusLabel.AddToClassList("status--injured");
                    _wfStatusDetail?.SetText("Recovering...");
                    break;
                case CitizenState.Recovering:
                    _wfStatusLabel.text = "RECOVERING";
                    _wfStatusLabel.AddToClassList("status--injured");
                    _wfStatusDetail?.SetText("Under medical care");
                    break;
                case CitizenState.OnExpedition:
                    _wfStatusLabel.text = "ON EXPEDITION";
                    _wfStatusLabel.AddToClassList("status--assigned");
                    _wfStatusDetail?.SetText("Away on exploration mission");
                    break;
            }
        }

        void AssignToCombat()
        {
            if (_gameState == null || _selectedCitizenIndex < 0) return;
            var citizen = _gameState.citizens[_selectedCitizenIndex];
            if (citizen.state != CitizenState.Idle) return;

            citizen.state = CitizenState.InCombat;
            Debug.Log($"[GameUIController] {citizen.displayName} assigned to combat");
            RefreshWorkforceList();
            UpdateWorkforceDetail();
        }

        void Unassign()
        {
            if (_gameState == null || _selectedCitizenIndex < 0) return;
            var citizen = _gameState.citizens[_selectedCitizenIndex];
            if (citizen.state != CitizenState.Assigned && citizen.state != CitizenState.InCombat) return;

            citizen.state = CitizenState.Idle;
            citizen.assignedSlotId = null;
            Debug.Log($"[GameUIController] {citizen.displayName} unassigned");
            RefreshWorkforceList();
            UpdateWorkforceDetail();
        }

        static string GetPortraitClass(CitizenAptitude aptitude)
        {
            return aptitude switch
            {
                CitizenAptitude.Combat => "portrait--combat",
                CitizenAptitude.Construction => "portrait--construction",
                CitizenAptitude.Research => "portrait--research",
                CitizenAptitude.Exploration => "portrait--exploration",
                _ => "portrait--none"
            };
        }

        // ================================================================
        // Exploration Tab
        // ================================================================

        void RefreshExplorationMap()
        {
            if (_nodeGrid == null) return;
            _nodeGrid.Clear();
            _nodeElements.Clear();

            var baseNode = CreateNodeCard(0, "BASE", true);
            _nodeGrid.Add(baseNode);
            _nodeElements.Add(baseNode);

            string[] nodeIcons = { "RES", "SCT", "ENC", "TEC", "RES" };
            for (int i = 1; i <= stubNodeCount; i++)
            {
                string icon = i <= nodeIcons.Length ? nodeIcons[i - 1] : "???";
                var nodeState = GetNodeState(i);
                var node = CreateNodeCard(i, icon, false, nodeState);
                _nodeGrid.Add(node);
                _nodeElements.Add(node);
            }
        }

        VisualElement CreateNodeCard(int index, string icon, bool isBase,
            ExplorationNodeState state = ExplorationNodeState.Hidden)
        {
            var card = new VisualElement();
            card.AddToClassList("node-card");

            if (isBase)
                card.AddToClassList("node-card--base");
            else if (state == ExplorationNodeState.Visited)
                card.AddToClassList("node-card--visited");
            else if (state == ExplorationNodeState.Hidden)
                card.AddToClassList("node-card--hidden");

            var iconLabel = new Label(isBase ? "HQ" : icon);
            iconLabel.AddToClassList("node-icon");
            card.Add(iconLabel);

            var nameLabel = new Label(isBase ? "Base" : $"Node {index}");
            nameLabel.AddToClassList("node-label");
            card.Add(nameLabel);

            int nodeIndex = index;
            card.RegisterCallback<ClickEvent>(_ => SelectNode(nodeIndex));

            return card;
        }

        void SelectNode(int index)
        {
            if (_selectedNode >= 0 && _selectedNode < _nodeElements.Count)
                _nodeElements[_selectedNode].RemoveFromClassList("node-card--selected");

            _selectedNode = index;

            if (index >= 0 && index < _nodeElements.Count)
                _nodeElements[index].AddToClassList("node-card--selected");

            bool canDispatch = index > 0 && GetNodeState(index) != ExplorationNodeState.Visited;
            if (_btnDispatch != null)
            {
                _btnDispatch.SetEnabled(canDispatch);
                if (canDispatch)
                    _btnDispatch.RemoveFromClassList("dispatch-btn--disabled");
                else
                    _btnDispatch.AddToClassList("dispatch-btn--disabled");
            }
        }

        void RefreshExpeditions()
        {
            if (_expeditionList == null || explorationBridge == null) return;
            _expeditionList.Clear();

            int activeCount = 0;
            if (_gameState != null)
            {
                foreach (var citizen in _gameState.citizens)
                {
                    if (citizen.state != CitizenState.OnExpedition) continue;
                    activeCount++;

                    var item = new VisualElement();
                    item.AddToClassList("expedition-item");

                    var nameLabel = new Label(citizen.displayName ?? citizen.citizenId);
                    nameLabel.AddToClassList("expedition-citizen");
                    item.Add(nameLabel);

                    var destLabel = new Label("En route...");
                    destLabel.AddToClassList("expedition-dest");
                    item.Add(destLabel);

                    _expeditionList.Add(item);
                }
            }

            _expeditionStatus?.SetText($"{activeCount} active expedition{(activeCount != 1 ? "s" : "")}");
        }

        void DispatchExpedition()
        {
            if (explorationBridge == null || _gameState == null || _selectedNode <= 0) return;

            var idleCitizen = _gameState.citizens.Find(c => c.state == CitizenState.Idle);
            if (idleCitizen == null)
            {
                Debug.LogWarning("[GameUIController] No idle citizens available");
                return;
            }

            explorationBridge.DispatchExpedition(idleCitizen.citizenId, _selectedNode);
            RefreshExplorationMap();
            RefreshExpeditions();
        }

        void InvestBonfire()
        {
            if (explorationBridge == null) return;
            explorationBridge.InvestBonfire();
            RefreshExpeditions();
        }

        ExplorationNodeState GetNodeState(int index)
        {
            if (_gameState == null) return ExplorationNodeState.Hidden;
            var node = _gameState.explorationNodes.Find(n => n.nodeId == index.ToString());
            return node?.state ?? ExplorationNodeState.Hidden;
        }

        // ================================================================
        // Research Tab
        // ================================================================

        void RefreshResearchList()
        {
            if (_techNodeList == null || techTreeBridge == null) return;

            _techNodeList.Clear();
            _techElements.Clear();
            _allTechNodes = techTreeBridge.GetAllNodes();

            // Update status label
            if (techTreeBridge.CurrentResearchId != null)
            {
                var current = techTreeBridge.GetNode(techTreeBridge.CurrentResearchId);
                if (current != null)
                    _researchStatus?.SetText($"Researching: {current.Name} ({techTreeBridge.Progress}/{current.ResearchTurns})");
            }
            else
            {
                _researchStatus?.SetText("No active research");
            }

            for (int i = 0; i < _allTechNodes.Length; i++)
            {
                var node = _allTechNodes[i];
                var item = CreateTechNodeElement(node, i);
                _techNodeList.Add(item);
                _techElements.Add(item);
            }

            if (_selectedTechIndex >= 0 && _selectedTechIndex < _allTechNodes.Length)
                UpdateResearchDetail();
            else
                ClearResearchSelection();
        }

        VisualElement CreateTechNodeElement(TechNode node, int index)
        {
            var item = new VisualElement();
            item.AddToClassList("tech-node-item");

            bool isCompleted = techTreeBridge.IsResearched(node.Id);
            bool isActive = techTreeBridge.CurrentResearchId == node.Id;
            bool prereqsMet = true;
            if (node.Prerequisites != null)
            {
                foreach (var prereq in node.Prerequisites)
                {
                    if (!techTreeBridge.IsResearched(prereq))
                    {
                        prereqsMet = false;
                        break;
                    }
                }
            }

            if (isCompleted)
                item.AddToClassList("tech-node-item--completed");
            else if (isActive)
                item.AddToClassList("tech-node-item--active");
            else if (!prereqsMet)
                item.AddToClassList("tech-node-item--locked");

            var catIcon = new VisualElement();
            catIcon.AddToClassList("tech-category-icon");
            catIcon.AddToClassList(node.Category switch
            {
                TechCategory.Economy => "tech-cat--economy",
                TechCategory.Defense => "tech-cat--defense",
                _ => "tech-cat--utility"
            });
            item.Add(catIcon);

            var nameLabel = new Label(node.Name);
            nameLabel.AddToClassList("tech-node-name");
            item.Add(nameLabel);

            var statusLabel = new Label(isCompleted ? "DONE" : isActive ? "IN PROGRESS" : "");
            statusLabel.AddToClassList("tech-node-status");
            item.Add(statusLabel);

            int idx = index;
            item.RegisterCallback<ClickEvent>(_ => SelectTechNode(idx));

            return item;
        }

        void SelectTechNode(int index)
        {
            if (_selectedTechIndex >= 0 && _selectedTechIndex < _techElements.Count)
                _techElements[_selectedTechIndex].RemoveFromClassList("tech-node-item--selected");

            _selectedTechIndex = index;

            if (index >= 0 && index < _techElements.Count)
                _techElements[index].AddToClassList("tech-node-item--selected");

            UpdateResearchDetail();
        }

        void ClearResearchSelection()
        {
            _selectedTechIndex = -1;
            _researchDetailEmpty?.SetDisplay(true);
            _researchDetailContent?.SetDisplay(false);
        }

        void UpdateResearchDetail()
        {
            if (techTreeBridge == null || _allTechNodes == null ||
                _selectedTechIndex < 0 || _selectedTechIndex >= _allTechNodes.Length)
            {
                ClearResearchSelection();
                return;
            }

            _researchDetailEmpty?.SetDisplay(false);
            _researchDetailContent?.SetDisplay(true);

            var node = _allTechNodes[_selectedTechIndex];
            bool isCompleted = techTreeBridge.IsResearched(node.Id);
            bool isActive = techTreeBridge.CurrentResearchId == node.Id;
            bool hasActiveResearch = !string.IsNullOrEmpty(techTreeBridge.CurrentResearchId);

            _techName?.SetText(node.Name);
            _techCategory?.SetText(node.Category.ToString());
            _techDesc?.SetText(node.Description);
            _techCostBasic?.SetText(node.CostBasic.ToString());
            _techCostAdvanced?.SetText(node.CostAdvanced.ToString());
            _techTurns?.SetText($"{node.ResearchTurns} turn{(node.ResearchTurns > 1 ? "s" : "")}");

            // Prerequisites
            if (node.Prerequisites == null || node.Prerequisites.Length == 0)
            {
                _techPrereqs?.SetText("None");
            }
            else
            {
                var prereqNames = new List<string>();
                foreach (var prereqId in node.Prerequisites)
                {
                    var prereqNode = techTreeBridge.GetNode(prereqId);
                    prereqNames.Add(prereqNode != null ? prereqNode.Name : prereqId);
                }
                _techPrereqs?.SetText(string.Join(", ", prereqNames));
            }

            // Progress bar (only for active research)
            _techProgressSection?.SetDisplay(isActive);
            if (isActive)
            {
                _techProgressLabel?.SetText($"Progress: {techTreeBridge.Progress} / {node.ResearchTurns}");
                float ratio = node.ResearchTurns > 0
                    ? (float)techTreeBridge.Progress / node.ResearchTurns
                    : 0f;
                _techProgressFill?.SetWidth(ratio * 100f);
            }

            // Button state
            if (_btnStartResearch != null)
            {
                bool canStart = !isCompleted && !hasActiveResearch;
                if (canStart && node.Prerequisites != null)
                {
                    foreach (var prereq in node.Prerequisites)
                    {
                        if (!techTreeBridge.IsResearched(prereq))
                        {
                            canStart = false;
                            break;
                        }
                    }
                }
                if (canStart && _gameState != null)
                    canStart = _gameState.resources.CanAfford(node.CostBasic, node.CostAdvanced);

                _btnStartResearch.SetEnabled(canStart);
                _btnStartResearch.SetDisplay(!isCompleted);

                if (isCompleted)
                    _btnStartResearch.text = "COMPLETED";
                else if (isActive)
                {
                    _btnStartResearch.text = "IN PROGRESS";
                    _btnStartResearch.SetDisplay(true);
                    _btnStartResearch.SetEnabled(false);
                }
                else if (!canStart)
                {
                    _btnStartResearch.text = "START RESEARCH";
                    _btnStartResearch.AddToClassList("action-btn--disabled");
                }
                else
                {
                    _btnStartResearch.text = "START RESEARCH";
                    _btnStartResearch.RemoveFromClassList("action-btn--disabled");
                }
            }
        }

        void StartResearchClicked()
        {
            if (techTreeBridge == null || _allTechNodes == null ||
                _selectedTechIndex < 0 || _selectedTechIndex >= _allTechNodes.Length)
                return;

            var node = _allTechNodes[_selectedTechIndex];
            if (techTreeBridge.StartResearch(node.Id))
            {
                RefreshResearchList();
                UpdateResearchDetail();
            }
        }

        void OnResearchCompleted(TechNode node)
        {
            Debug.Log($"[GameUIController] Research completed: {node.Name}");
            // Refresh if research tab is visible
            if (_researchPanel != null && _researchPanel.resolvedStyle.display == DisplayStyle.Flex)
                RefreshResearchList();
        }

        // ================================================================
        // Policy Popup
        // ================================================================

        /// <summary>정책 팝업 표시.</summary>
        public void ShowPolicyPopup(PolicyData policy)
        {
            if (policy == null) return;
            _currentPolicy = policy;

            _policyTitle?.SetText(policy.Title);
            _policyDesc?.SetText(policy.Description);

            _policyAName?.SetText(policy.OptionAName);
            _policyADesc?.SetText(policy.OptionADescription);
            _policyAEffects?.SetText(policy.OptionAEffects);

            _policyBName?.SetText(policy.OptionBName);
            _policyBDesc?.SetText(policy.OptionBDescription);
            _policyBEffects?.SetText(policy.OptionBEffects);

            _policyOverlay?.SetDisplay(true);
        }

        /// <summary>정책 팝업 숨기기.</summary>
        public void HidePolicyPopup()
        {
            _policyOverlay?.SetDisplay(false);
            _currentPolicy = null;
        }

        void ChoosePolicy(bool isOptionA)
        {
            if (policyBridge == null || _currentPolicy == null) return;

            policyBridge.ChooseOption(_currentPolicy.Id, isOptionA);
            HidePolicyPopup();
        }

        // ================================================================
        // Helpers
        // ================================================================

        void UpdateContinueButton()
        {
            bool hasSave = SaveManager.HasSave();
            if (_btnContinue != null)
            {
                _btnContinue.SetEnabled(hasSave);
                if (hasSave)
                    _btnContinue.RemoveFromClassList("menu-btn--disabled");
                else
                    _btnContinue.AddToClassList("menu-btn--disabled");
            }
        }

        void SetDirection(VisualElement indicator, Label countLabel, int count, bool visible)
        {
            if (indicator == null) return;
            indicator.RemoveFromClassList("direction-indicator--active");
            if (visible && count > 0)
                indicator.AddToClassList("direction-indicator--active");
            countLabel?.SetText(visible ? count.ToString() : "?");
        }
    }
}
