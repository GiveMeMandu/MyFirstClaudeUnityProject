using System;
using System.Diagnostics;
using UnityEngine;
using ProjectSun.V2.Data;
using Debug = UnityEngine.Debug;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// 낮/밤 페이즈 상태 머신.
    /// Day → NightTransition → Night → DawnTransition → Day 순환.
    /// </summary>
    public class PhaseManager : MonoBehaviour
    {
        public event Action<PhaseType> OnPhaseChanged;

        [SerializeField] GameState gameState;

        PhaseType _currentPhase = PhaseType.Day;
        Stopwatch _transitionStopwatch;

        public PhaseType CurrentPhase => _currentPhase;

        /// <summary>외부에서 GameState를 주입 (테스트용).</summary>
        public void Initialize(GameState state)
        {
            gameState = state;
            _currentPhase = state.currentPhase;
        }

        /// <summary>
        /// 낮 → 밤 전환 시작.
        /// BattleInitializer가 ECS 엔티티를 생성한 뒤 호출해야 한다.
        /// </summary>
        public void StartNightPhase()
        {
            if (_currentPhase != PhaseType.Day)
            {
                Debug.LogWarning($"[PhaseManager] Cannot start night: current phase is {_currentPhase}");
                return;
            }

            _transitionStopwatch = Stopwatch.StartNew();
            SetPhase(PhaseType.Transition);

            Debug.Log("[PhaseManager] Day → NightTransition");
        }

        /// <summary>
        /// NightTransition 완료 → Night 진입.
        /// BattleInitializer.InitializeBattle 완료 후 호출.
        /// </summary>
        public void EnterNight()
        {
            if (_currentPhase != PhaseType.Transition)
            {
                Debug.LogWarning($"[PhaseManager] Cannot enter night: current phase is {_currentPhase}");
                return;
            }

            _transitionStopwatch.Stop();
            Debug.Log($"[PhaseManager] NightTransition complete in {_transitionStopwatch.ElapsedMilliseconds}ms");

            SetPhase(PhaseType.Night);
            Debug.Log("[PhaseManager] Night phase started");
        }

        /// <summary>
        /// 밤 → 낮 전환 시작.
        /// BattleResultCollector가 결과 수집 후 호출.
        /// </summary>
        public void EndNightPhase()
        {
            if (_currentPhase != PhaseType.Night)
            {
                Debug.LogWarning($"[PhaseManager] Cannot end night: current phase is {_currentPhase}");
                return;
            }

            _transitionStopwatch = Stopwatch.StartNew();
            SetPhase(PhaseType.Transition);

            Debug.Log("[PhaseManager] Night → DawnTransition");
        }

        /// <summary>
        /// DawnTransition 완료 → Day 진입.
        /// BattleResultCollector 결과 적용 완료 후 호출.
        /// </summary>
        public void EnterDay()
        {
            if (_currentPhase != PhaseType.Transition)
            {
                Debug.LogWarning($"[PhaseManager] Cannot enter day: current phase is {_currentPhase}");
                return;
            }

            _transitionStopwatch.Stop();
            Debug.Log($"[PhaseManager] DawnTransition complete in {_transitionStopwatch.ElapsedMilliseconds}ms");

            SetPhase(PhaseType.Day);
            Debug.Log("[PhaseManager] Day phase started");
        }

        /// <summary>마지막 전환에 걸린 시간(ms). 검증용.</summary>
        public long LastTransitionMs => _transitionStopwatch?.ElapsedMilliseconds ?? 0;

        void SetPhase(PhaseType phase)
        {
            _currentPhase = phase;
            if (gameState != null)
                gameState.currentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
