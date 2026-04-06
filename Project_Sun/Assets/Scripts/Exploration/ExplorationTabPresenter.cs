using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Core;
using ProjectSun.V2.Data;
using ProjectSun.V2.Construction;

namespace ProjectSun.V2.Exploration
{
    /// <summary>
    /// 탐험 탭 Presenter (UI Toolkit).
    /// 노드 그래프 시각화 + 원정대 파견/모닥불 투자.
    /// SF-EXP-005, SF-EXP-006.
    /// </summary>
    public class ExplorationTabPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] UIDocument uiDocument;
        [SerializeField] ExplorationBridge explorationBridge;

        [Header("Map Stub (5 nodes)")]
        [SerializeField] int stubNodeCount = 5;

        VisualElement _root;
        VisualElement _nodeGrid;
        Label _expeditionStatus;
        ScrollView _expeditionList;
        Button _btnDispatch, _btnBonfire;

        GameState _gameState;
        int _selectedNode = -1;
        List<VisualElement> _nodeElements = new();

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

            _btnDispatch?.RegisterCallback<ClickEvent>(_ => DispatchExpedition());
            _btnBonfire?.RegisterCallback<ClickEvent>(_ => InvestBonfire());
        }

        public void Show()
        {
            if (_root != null) _root.style.display = DisplayStyle.Flex;
            RefreshMap();
            RefreshExpeditions();
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        void CacheElements()
        {
            _nodeGrid = _root.Q("node-grid");
            _expeditionStatus = _root.Q<Label>("expedition-status");
            _expeditionList = _root.Q<ScrollView>("expedition-list");
            _btnDispatch = _root.Q<Button>("btn-dispatch");
            _btnBonfire = _root.Q<Button>("btn-bonfire");
        }

        void RefreshMap()
        {
            if (_nodeGrid == null) return;
            _nodeGrid.Clear();
            _nodeElements.Clear();

            // 기지 노드
            var baseNode = CreateNodeCard(0, "BASE", true);
            _nodeGrid.Add(baseNode);
            _nodeElements.Add(baseNode);

            // 스텁 노드
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

        VisualElement CreateNodeCard(int index, string icon, bool isBase, ExplorationNodeState state = ExplorationNodeState.Hidden)
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
            // Deselect previous
            if (_selectedNode >= 0 && _selectedNode < _nodeElements.Count)
                _nodeElements[_selectedNode].RemoveFromClassList("node-card--selected");

            _selectedNode = index;

            if (index >= 0 && index < _nodeElements.Count)
                _nodeElements[index].AddToClassList("node-card--selected");

            // 기지 노드는 파견 불가
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

            // ExplorationBridge의 활성 원정 표시는 내부 리스트 접근 필요
            // 현재는 GameState의 OnExpedition 시민으로 표시
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

            // 첫 번째 Idle 시민을 파견
            var idleCitizen = _gameState.citizens.Find(c => c.state == CitizenState.Idle);
            if (idleCitizen == null)
            {
                Debug.LogWarning("[ExplorationTab] No idle citizens available");
                return;
            }

            explorationBridge.DispatchExpedition(idleCitizen.citizenId, _selectedNode);
            RefreshMap();
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
    }
}
