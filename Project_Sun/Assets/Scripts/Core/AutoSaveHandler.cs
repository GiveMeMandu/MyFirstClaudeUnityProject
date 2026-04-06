using UnityEngine;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// 자동 세이브. 낮 페이즈 진입 시 GameState를 자동 저장.
    /// PhaseManager.OnPhaseChanged를 구독하여 Day 진입 감지.
    /// SF-DATA-005 / SF-TURN-003.
    /// </summary>
    public class AutoSaveHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PhaseManager phaseManager;

        [Header("Settings")]
        [SerializeField] bool enableAutoSave = true;

        GameState _gameState;

        /// <summary>GameState 주입. 게임 시작 시 호출.</summary>
        public void Initialize(GameState state)
        {
            _gameState = state;
        }

        void OnEnable()
        {
            if (phaseManager != null)
                phaseManager.OnPhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            if (phaseManager != null)
                phaseManager.OnPhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(PhaseType phase)
        {
            if (!enableAutoSave) return;
            if (phase != PhaseType.Day) return;
            if (_gameState == null) return;

            // 턴 1 첫 시작은 저장하지 않음 (초기 상태)
            if (_gameState.currentTurn <= 1 && _gameState.waveHistory.Count == 0) return;

            bool success = SaveManager.Save(_gameState);
            if (success)
                Debug.Log($"[AutoSave] Turn {_gameState.currentTurn} auto-saved");
        }
    }
}
