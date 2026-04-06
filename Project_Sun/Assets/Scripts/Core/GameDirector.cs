using System;
using UnityEngine;
using ProjectSun.V2.Data;
using ProjectSun.V2.Defense;
using ProjectSun.V2.Defense.Bridge;
using ProjectSun.V2.Construction;
using ProjectSun.V2.Workforce;
using ProjectSun.V2.Exploration;
using ProjectSun.V2.UI;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// 전체 게임 루프 오케스트레이터.
    /// 메뉴 → 게임 시작 → 낮/밤 루프 → 게임오버 흐름 관리.
    /// 모든 Presenter/Bridge/Manager를 연결한다.
    /// </summary>
    public class GameDirector : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] PhaseManager phaseManager;
        [SerializeField] TimeScaleController timeScaleController;
        [SerializeField] GameOverManager gameOverManager;
        [SerializeField] AutoSaveHandler autoSaveHandler;
        [SerializeField] ResourceFlowLogger resourceFlowLogger;
        [SerializeField] ExplorationBridge explorationBridge;

        [Header("Bridge")]
        [SerializeField] BattleInitializer battleInitializer;
        [SerializeField] BattleResultCollector resultCollector;
        [SerializeField] BattleUIBridge battleUIBridge;

        [Header("UI Presenters")]
        [SerializeField] MenuScreenPresenter menuPresenter;
        [SerializeField] BattleHUDPresenter battleHUD;
        [SerializeField] ConstructionTabPresenter constructionTab;
        [SerializeField] WorkforceTabPresenter workforceTab;
        [SerializeField] ExplorationTabPresenter explorationTab;
        [SerializeField] WavePreviewPresenter wavePreview;

        [Header("Day Phase Tab")]
        [SerializeField] int activeTab; // 0=건설, 1=관리, 2=탐험

        GameState _gameState;
        bool _gameActive;

        void Start()
        {
            ShowMenu();
        }

        void ShowMenu()
        {
            _gameActive = false;
            HideAllGameUI();
            menuPresenter?.ShowMainMenu();

            if (menuPresenter != null)
            {
                menuPresenter.OnNewGameRequested -= StartNewGame;
                menuPresenter.OnContinueRequested -= ContinueGame;
                menuPresenter.OnRetryRequested -= StartNewGame;

                menuPresenter.OnNewGameRequested += StartNewGame;
                menuPresenter.OnContinueRequested += ContinueGame;
                menuPresenter.OnRetryRequested += StartNewGame;
            }
        }

        void StartNewGame()
        {
            _gameState = CreateDefaultGameState();
            InitializeGame();
        }

        void ContinueGame()
        {
            var loaded = SaveManager.Load();
            if (loaded == null)
            {
                Debug.LogWarning("[GameDirector] No save found, starting new game");
                StartNewGame();
                return;
            }

            _gameState = loaded;
            InitializeGame();
        }

        void InitializeGame()
        {
            // 시스템 초기화
            phaseManager?.Initialize(_gameState);
            autoSaveHandler?.Initialize(_gameState);
            explorationBridge?.Initialize(_gameState);
            constructionTab?.Initialize(_gameState);
            workforceTab?.Initialize(_gameState);
            explorationTab?.Initialize(_gameState);

            // 이벤트 연결
            if (phaseManager != null)
                phaseManager.OnPhaseChanged += OnPhaseChanged;
            if (gameOverManager != null)
                gameOverManager.OnGameOver += OnGameOver;
            if (resultCollector != null)
                resultCollector.OnDefenseResultPublished += OnDefenseResult;
            if (wavePreview != null)
                wavePreview.OnResultClosed += OnResultClosed;

            gameOverManager?.Reset();
            _gameActive = true;

            Debug.Log($"[GameDirector] Game started — Turn {_gameState.currentTurn}");
            EnterDayPhase();
        }

        // ── Day Phase ──

        void EnterDayPhase()
        {
            HideAllGameUI();
            resourceFlowLogger?.OnTurnStart(_gameState);
            explorationBridge?.ProcessTurn();

            // 낮 탭 표시
            ShowDayTab(activeTab);

            Debug.Log($"[GameDirector] === DAY {_gameState.currentTurn} ===");
        }

        /// <summary>낮 탭 전환. 외부에서 호출 가능 (UI 버튼 등).</summary>
        public void ShowDayTab(int tabIndex)
        {
            activeTab = tabIndex;
            constructionTab?.Hide();
            workforceTab?.Hide();
            explorationTab?.Hide();

            switch (tabIndex)
            {
                case 0: constructionTab?.Show(); break;
                case 1: workforceTab?.Show(); break;
                case 2: explorationTab?.Show(); break;
            }
        }

        /// <summary>밤 전환 시작. UI 버튼에서 호출.</summary>
        public void StartNight()
        {
            if (!_gameActive || _gameState.currentPhase != PhaseType.Day) return;

            resourceFlowLogger?.OnDayEnd(_gameState);

            // 웨이브 미리보기 표시
            var scoutLevel = ScoutLevel.Unknown;
            if (explorationBridge?.ScoutData != null)
                explorationBridge.ScoutData.TryGetValue(_gameState.currentTurn, out scoutLevel);

            int estimatedEnemies = Mathf.RoundToInt(10 * Mathf.Pow(1.2f, _gameState.currentTurn - 1));
            int waveCount = _gameState.currentTurn <= 5 ? 1 : _gameState.currentTurn <= 15 ? 2 : 3;
            wavePreview?.ShowPreview(_gameState.currentTurn, estimatedEnemies, waveCount, scoutLevel);
        }

        /// <summary>미리보기 확인 후 실제 전투 시작. 미리보기 닫기 버튼 대신 직접 호출용.</summary>
        public void ConfirmNight()
        {
            if (!_gameActive) return;

            HideAllGameUI();
            wavePreview?.HidePreview();

            // 페이즈 전환
            phaseManager?.StartNightPhase();
            battleInitializer?.InitializeBattle(_gameState);
            phaseManager?.EnterNight();

            // 전투 UI 활성화
            battleUIBridge?.Activate();
            battleHUD?.Show();
            timeScaleController?.ResetToNormal();

            Debug.Log($"[GameDirector] === NIGHT {_gameState.currentTurn} ===");
        }

        // ── Night End ──

        /// <summary>전투 종료. ECS 전투 완료 감지 시 호출.</summary>
        public void EndNight()
        {
            if (!_gameActive || _gameState.currentPhase != PhaseType.Night) return;

            battleHUD?.Hide();
            battleUIBridge?.Deactivate();

            phaseManager?.EndNightPhase();
            var result = resultCollector?.CollectResults(_gameState);
            battleInitializer?.CleanupBattleEntities();

            if (result != null)
            {
                // 보상 적용
                _gameState.resources.Add(result.basicReward, result.advancedReward, result.relicReward);
                _gameState.waveHistory.Add(result);
                resourceFlowLogger?.OnNightReward(_gameState, result);

                // 게임오버 체크
                gameOverManager?.CheckResult(result);

                if (!gameOverManager.IsGameOver)
                {
                    // 결과 화면 표시
                    wavePreview?.ShowResult(result);
                }
            }
        }

        void OnDefenseResult(WaveResult result)
        {
            // BattleResultCollector 이벤트 — 추가 처리 필요 시 여기에
        }

        void OnResultClosed()
        {
            // 결과 화면 닫힘 → 다음 턴
            _gameState.currentTurn++;
            resourceFlowLogger?.OnTurnEnd(_gameState);
            phaseManager?.EnterDay();
            EnterDayPhase();
        }

        // ── Events ──

        void OnPhaseChanged(PhaseType phase)
        {
            Debug.Log($"[GameDirector] Phase → {phase}");
        }

        void OnGameOver(GameOverManager.GameOverReason reason)
        {
            _gameActive = false;
            HideAllGameUI();
            battleHUD?.Hide();
            battleUIBridge?.Deactivate();

            bool isVictory = reason == GameOverManager.GameOverReason.Victory;
            menuPresenter?.ShowGameOver(_gameState, isVictory);

            Debug.Log($"[GameDirector] GAME OVER — {reason}");
        }

        void HideAllGameUI()
        {
            menuPresenter?.HideAll();
            battleHUD?.Hide();
            constructionTab?.Hide();
            workforceTab?.Hide();
            explorationTab?.Hide();
            wavePreview?.HidePreview();
            wavePreview?.HideResult();
        }

        void OnDestroy()
        {
            if (phaseManager != null)
                phaseManager.OnPhaseChanged -= OnPhaseChanged;
            if (gameOverManager != null)
                gameOverManager.OnGameOver -= OnGameOver;
            if (resultCollector != null)
                resultCollector.OnDefenseResultPublished -= OnDefenseResult;
            if (wavePreview != null)
                wavePreview.OnResultClosed -= OnResultClosed;
        }

        GameState CreateDefaultGameState()
        {
            var state = new GameState();

            // 본부
            state.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "headquarters",
                state = BuildingSlotStateV2.Active, currentHP = 500, maxHP = 500
            });

            // 기본 건물 3개
            for (int i = 1; i <= 3; i++)
            {
                state.buildings.Add(new BuildingRuntimeState
                {
                    slotId = $"slot_{i}",
                    buildingId = i <= 2 ? "tower_basic" : "production_basic",
                    state = BuildingSlotStateV2.Active,
                    currentHP = 150, maxHP = 150, upgradeLevel = 0
                });
            }

            // 잠김 슬롯 4개
            for (int i = 4; i <= 7; i++)
            {
                state.buildings.Add(new BuildingRuntimeState
                {
                    slotId = $"slot_{i}", buildingId = "",
                    state = BuildingSlotStateV2.Locked, currentHP = 0, maxHP = 0
                });
            }

            // 초기 시민 4명
            string[] names = { "Kim", "Lee", "Park", "Choi" };
            CitizenAptitude[] aptitudes = {
                CitizenAptitude.Combat, CitizenAptitude.Construction,
                CitizenAptitude.Combat, CitizenAptitude.Exploration
            };
            for (int i = 0; i < 4; i++)
            {
                state.citizens.Add(new CitizenRuntimeState
                {
                    citizenId = $"citizen_{i}",
                    displayName = names[i],
                    aptitude = aptitudes[i],
                    proficiencyLevel = 1,
                    state = CitizenState.Idle
                });
            }

            return state;
        }
    }
}
