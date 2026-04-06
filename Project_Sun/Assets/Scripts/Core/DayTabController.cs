using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Data;
using ProjectSun.V2.Construction;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// 낮 페이즈 HUD — UI Toolkit 탭 바 + 자원 표시 + 밤 시작 버튼.
    /// GameDirector와 연결하여 탭 전환/밤 전환을 제어.
    /// </summary>
    public class DayTabController : MonoBehaviour
    {
        [SerializeField] GameDirector director;
        [SerializeField] UIDocument uiDocument;

        VisualElement _root;
        Button _tabConstruction, _tabWorkforce, _tabExploration;
        Button _btnStartNight;
        Label _resBasic, _resAdvanced, _resRelic, _turnBadge;

        int _selectedTab;
        GameState _gameState;

        public void Initialize(GameState state)
        {
            _gameState = state;
        }

        void OnEnable()
        {
            if (uiDocument == null) return;
            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            _tabConstruction = _root.Q<Button>("tab-construction");
            _tabWorkforce = _root.Q<Button>("tab-workforce");
            _tabExploration = _root.Q<Button>("tab-exploration");
            _btnStartNight = _root.Q<Button>("btn-start-night");
            _resBasic = _root.Q<Label>("res-basic");
            _resAdvanced = _root.Q<Label>("res-advanced");
            _resRelic = _root.Q<Label>("res-relic");
            _turnBadge = _root.Q<Label>("turn-badge");

            _tabConstruction?.RegisterCallback<ClickEvent>(_ => SelectTab(0));
            _tabWorkforce?.RegisterCallback<ClickEvent>(_ => SelectTab(1));
            _tabExploration?.RegisterCallback<ClickEvent>(_ => SelectTab(2));
            _btnStartNight?.RegisterCallback<ClickEvent>(_ => director?.StartNight());
        }

        void SelectTab(int index)
        {
            _selectedTab = index;

            _tabConstruction?.RemoveFromClassList("tab-btn--active");
            _tabWorkforce?.RemoveFromClassList("tab-btn--active");
            _tabExploration?.RemoveFromClassList("tab-btn--active");

            switch (index)
            {
                case 0: _tabConstruction?.AddToClassList("tab-btn--active"); break;
                case 1: _tabWorkforce?.AddToClassList("tab-btn--active"); break;
                case 2: _tabExploration?.AddToClassList("tab-btn--active"); break;
            }

            director?.ShowDayTab(index);
        }

        void Update()
        {
            if (_gameState == null) return;

            _resBasic?.SetText(_gameState.resources.basicAmount.ToString());
            _resAdvanced?.SetText(_gameState.resources.advancedAmount.ToString());
            _resRelic?.SetText(_gameState.resources.relicAmount.ToString());
            _turnBadge?.SetText($"TURN {_gameState.currentTurn}");
        }

        public void Show()
        {
            if (_root != null) _root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }
    }
}
