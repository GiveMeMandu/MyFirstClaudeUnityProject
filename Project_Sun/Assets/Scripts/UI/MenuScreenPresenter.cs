using System;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Core;
using ProjectSun.V2.Data;
using ProjectSun.V2.Construction;

namespace ProjectSun.V2.UI
{
    /// <summary>
    /// 메인메뉴 + 기지선택 + 게임오버 화면 Presenter.
    /// SF-UX-001, SF-UX-002, SF-UX-008.
    /// </summary>
    public class MenuScreenPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] UIDocument uiDocument;

        VisualElement _root;
        VisualElement _mainMenu, _baseSelect, _gameOverScreen;

        // Game Over stats
        Label _gameoverTitle;
        Label _statTurns, _statCitizens, _statBuildings, _statEnemies, _statResources;

        // Continue button
        Button _btnContinue;

        /// <summary>새 게임 시작 요청. 게임 진입점에서 구독.</summary>
        public event Action OnNewGameRequested;

        /// <summary>계속하기 요청.</summary>
        public event Action OnContinueRequested;

        /// <summary>재시도 요청.</summary>
        public event Action OnRetryRequested;

        /// <summary>종료 요청.</summary>
        public event Action OnQuitRequested;

        void OnEnable()
        {
            if (uiDocument == null) return;
            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheElements();
            SetupButtons();
            UpdateContinueButton();
        }

        void CacheElements()
        {
            _mainMenu = _root.Q("main-menu");
            _baseSelect = _root.Q("base-select");
            _gameOverScreen = _root.Q("game-over-screen");

            _btnContinue = _root.Q<Button>("btn-continue");
            _gameoverTitle = _root.Q<Label>("gameover-title");
            _statTurns = _root.Q<Label>("stat-turns");
            _statCitizens = _root.Q<Label>("stat-citizens");
            _statBuildings = _root.Q<Label>("stat-buildings");
            _statEnemies = _root.Q<Label>("stat-enemies");
            _statResources = _root.Q<Label>("stat-resources");
        }

        void SetupButtons()
        {
            _root.Q<Button>("btn-new-game")?.RegisterCallback<ClickEvent>(_ => ShowBaseSelect());
            _btnContinue?.RegisterCallback<ClickEvent>(_ => OnContinueRequested?.Invoke());
            _root.Q<Button>("btn-settings")?.RegisterCallback<ClickEvent>(_ =>
                Debug.Log("[Menu] Settings not yet implemented"));
            _root.Q<Button>("btn-quit")?.RegisterCallback<ClickEvent>(_ =>
            {
                OnQuitRequested?.Invoke();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            _root.Q<Button>("btn-start-game")?.RegisterCallback<ClickEvent>(_ =>
            {
                HideAll();
                OnNewGameRequested?.Invoke();
            });
            _root.Q<Button>("btn-back-menu")?.RegisterCallback<ClickEvent>(_ => ShowMainMenu());

            _root.Q<Button>("btn-retry")?.RegisterCallback<ClickEvent>(_ =>
            {
                HideAll();
                OnRetryRequested?.Invoke();
            });
            _root.Q<Button>("btn-to-menu")?.RegisterCallback<ClickEvent>(_ => ShowMainMenu());
        }

        /// <summary>메인 메뉴 표시.</summary>
        public void ShowMainMenu()
        {
            HideAll();
            _mainMenu?.SetDisplay(true);
            UpdateContinueButton();
        }

        void ShowBaseSelect()
        {
            HideAll();
            _baseSelect?.SetDisplay(true);
        }

        /// <summary>
        /// 게임오버 화면 표시. 통계 데이터 바인딩.
        /// SF-UX-008.
        /// </summary>
        public void ShowGameOver(GameState state, bool isVictory)
        {
            HideAll();
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

        /// <summary>모든 화면 숨김.</summary>
        public void HideAll()
        {
            _mainMenu?.SetDisplay(false);
            _baseSelect?.SetDisplay(false);
            _gameOverScreen?.SetDisplay(false);
        }

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
    }
}
