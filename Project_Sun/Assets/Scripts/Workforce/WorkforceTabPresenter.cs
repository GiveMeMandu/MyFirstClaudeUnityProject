using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Data;
using ProjectSun.V2.Construction;

namespace ProjectSun.V2.Workforce
{
    /// <summary>
    /// 낮 페이즈 관리 탭 Presenter (UI Toolkit).
    /// GameState.citizens를 읽어 시민 목록 + 상세 패널 갱신.
    /// 전투 배치/해제 처리.
    /// SF-WF-009.
    /// </summary>
    public class WorkforceTabPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] UIDocument uiDocument;

        VisualElement _root;

        // List
        Label _citizenSummary;
        ScrollView _citizenList;
        Button _filterAll, _filterIdle, _filterAssigned, _filterInjured;

        // Detail
        VisualElement _detailEmpty;
        VisualElement _detailContent;
        VisualElement _detailPortrait;
        Label _detailName, _detailAptitude, _detailLevel;
        Label _statusLabel, _statusDetail;
        VisualElement _assignSection;
        Button _btnAssignCombat, _btnUnassign;

        GameState _gameState;
        int _selectedIndex = -1;
        string _activeFilter = "all";
        List<int> _filteredIndices = new();
        List<VisualElement> _rowElements = new();

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
            SetupFilters();
            SetupActions();
        }

        public void Show()
        {
            if (_root != null) _root.style.display = DisplayStyle.Flex;
            RefreshList();
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        void CacheElements()
        {
            _citizenSummary = _root.Q<Label>("citizen-summary");
            _citizenList = _root.Q<ScrollView>("citizen-list");
            _filterAll = _root.Q<Button>("filter-all");
            _filterIdle = _root.Q<Button>("filter-idle");
            _filterAssigned = _root.Q<Button>("filter-assigned");
            _filterInjured = _root.Q<Button>("filter-injured");

            _detailEmpty = _root.Q("detail-empty");
            _detailContent = _root.Q("detail-content");
            _detailPortrait = _root.Q("detail-portrait");
            _detailName = _root.Q<Label>("detail-name");
            _detailAptitude = _root.Q<Label>("detail-aptitude");
            _detailLevel = _root.Q<Label>("detail-level");
            _statusLabel = _root.Q<Label>("status-label");
            _statusDetail = _root.Q<Label>("status-detail");
            _assignSection = _root.Q("assign-section");
            _btnAssignCombat = _root.Q<Button>("btn-assign-combat");
            _btnUnassign = _root.Q<Button>("btn-unassign");
        }

        void SetupFilters()
        {
            _filterAll?.RegisterCallback<ClickEvent>(_ => SetFilter("all"));
            _filterIdle?.RegisterCallback<ClickEvent>(_ => SetFilter("idle"));
            _filterAssigned?.RegisterCallback<ClickEvent>(_ => SetFilter("assigned"));
            _filterInjured?.RegisterCallback<ClickEvent>(_ => SetFilter("injured"));
        }

        void SetupActions()
        {
            _btnAssignCombat?.RegisterCallback<ClickEvent>(_ => AssignToCombat());
            _btnUnassign?.RegisterCallback<ClickEvent>(_ => Unassign());
        }

        void SetFilter(string filter)
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

            RefreshList();
        }

        void RefreshList()
        {
            if (_citizenList == null || _gameState == null) return;

            _citizenList.Clear();
            _rowElements.Clear();
            _filteredIndices.Clear();

            // Summary
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

            // Filter + populate
            for (int i = 0; i < _gameState.citizens.Count; i++)
            {
                var citizen = _gameState.citizens[i];
                if (!PassesFilter(citizen)) continue;

                _filteredIndices.Add(i);
                var row = CreateCitizenRow(citizen, i);
                _citizenList.Add(row);
                _rowElements.Add(row);
            }

            if (_selectedIndex >= 0)
                UpdateDetail();
            else
                ClearSelection();
        }

        bool PassesFilter(CitizenRuntimeState citizen)
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

            // State indicator
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

            // Portrait
            var portrait = new VisualElement();
            portrait.AddToClassList("citizen-portrait");
            portrait.AddToClassList(GetPortraitClass(citizen.aptitude));
            row.Add(portrait);

            // Info
            var info = new VisualElement();
            info.AddToClassList("citizen-info");

            var nameLabel = new Label(citizen.displayName ?? citizen.citizenId);
            nameLabel.AddToClassList("citizen-name");
            info.Add(nameLabel);

            var statusLabel = new Label($"{citizen.state} | {citizen.aptitude}");
            statusLabel.AddToClassList("citizen-status");
            info.Add(statusLabel);

            row.Add(info);

            // Level
            var levelLabel = new Label($"Lv.{citizen.proficiencyLevel}");
            levelLabel.AddToClassList("citizen-level");
            row.Add(levelLabel);

            // Click
            int idx = realIndex;
            row.RegisterCallback<ClickEvent>(_ => SelectCitizen(idx));

            return row;
        }

        void SelectCitizen(int realIndex)
        {
            // Deselect previous
            int prevFilteredIdx = _filteredIndices.IndexOf(_selectedIndex);
            if (prevFilteredIdx >= 0 && prevFilteredIdx < _rowElements.Count)
                _rowElements[prevFilteredIdx].RemoveFromClassList("citizen-row--selected");

            _selectedIndex = realIndex;

            // Select new
            int filteredIdx = _filteredIndices.IndexOf(realIndex);
            if (filteredIdx >= 0 && filteredIdx < _rowElements.Count)
                _rowElements[filteredIdx].AddToClassList("citizen-row--selected");

            UpdateDetail();
        }

        void ClearSelection()
        {
            _selectedIndex = -1;
            _detailEmpty?.SetDisplay(true);
            _detailContent?.SetDisplay(false);
        }

        void UpdateDetail()
        {
            if (_gameState == null || _selectedIndex < 0 || _selectedIndex >= _gameState.citizens.Count)
            {
                ClearSelection();
                return;
            }

            _detailEmpty?.SetDisplay(false);
            _detailContent?.SetDisplay(true);

            var citizen = _gameState.citizens[_selectedIndex];

            _detailName?.SetText(citizen.displayName ?? citizen.citizenId);
            _detailAptitude?.SetText(citizen.aptitude.ToString());
            _detailLevel?.SetText($"Lv. {citizen.proficiencyLevel}");

            // Portrait border
            _detailPortrait?.ClearClassList();
            _detailPortrait?.AddToClassList("detail-portrait");
            _detailPortrait?.AddToClassList(GetPortraitClass(citizen.aptitude));

            // Status
            UpdateStatus(citizen);

            // Actions
            bool isIdle = citizen.state == CitizenState.Idle;
            bool isAssigned = citizen.state == CitizenState.Assigned || citizen.state == CitizenState.InCombat;

            _btnAssignCombat?.SetDisplay(isIdle);
            _btnUnassign?.SetDisplay(isAssigned);
            _assignSection?.SetDisplay(isIdle || isAssigned);
        }

        void UpdateStatus(CitizenRuntimeState citizen)
        {
            if (_statusLabel == null) return;

            _statusLabel.ClearClassList();
            _statusLabel.AddToClassList("status-label");

            switch (citizen.state)
            {
                case CitizenState.Idle:
                    _statusLabel.text = "IDLE";
                    _statusLabel.AddToClassList("status--idle");
                    _statusDetail?.SetText("Ready for assignment");
                    break;
                case CitizenState.Assigned:
                    _statusLabel.text = "ASSIGNED";
                    _statusLabel.AddToClassList("status--assigned");
                    _statusDetail?.SetText($"Working at {citizen.assignedSlotId ?? "unknown"}");
                    break;
                case CitizenState.InCombat:
                    _statusLabel.text = "IN COMBAT";
                    _statusLabel.AddToClassList("status--combat");
                    _statusDetail?.SetText("Deployed for night defense");
                    break;
                case CitizenState.Injured:
                    _statusLabel.text = "INJURED";
                    _statusLabel.AddToClassList("status--injured");
                    _statusDetail?.SetText("Recovering...");
                    break;
                case CitizenState.Recovering:
                    _statusLabel.text = "RECOVERING";
                    _statusLabel.AddToClassList("status--injured");
                    _statusDetail?.SetText("Under medical care");
                    break;
                case CitizenState.OnExpedition:
                    _statusLabel.text = "ON EXPEDITION";
                    _statusLabel.AddToClassList("status--assigned");
                    _statusDetail?.SetText("Away on exploration mission");
                    break;
            }
        }

        void AssignToCombat()
        {
            if (_gameState == null || _selectedIndex < 0) return;
            var citizen = _gameState.citizens[_selectedIndex];
            if (citizen.state != CitizenState.Idle) return;

            citizen.state = CitizenState.InCombat;
            Debug.Log($"[WorkforceTab] {citizen.displayName} assigned to combat");
            RefreshList();
            UpdateDetail();
        }

        void Unassign()
        {
            if (_gameState == null || _selectedIndex < 0) return;
            var citizen = _gameState.citizens[_selectedIndex];
            if (citizen.state != CitizenState.Assigned && citizen.state != CitizenState.InCombat) return;

            citizen.state = CitizenState.Idle;
            citizen.assignedSlotId = null;
            Debug.Log($"[WorkforceTab] {citizen.displayName} unassigned");
            RefreshList();
            UpdateDetail();
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
    }
}
