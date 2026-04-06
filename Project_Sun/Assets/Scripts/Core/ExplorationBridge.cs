using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// V2 탐사 브릿지. V1 ExplorationManager의 결과를 GameState에 반영.
    /// 원정대 파견/귀환 + 보상 처리 + 시민 합류 + 정찰 정보.
    /// SF-EXP-001~004, SF-ECO-009/010, SF-WF-010.
    /// </summary>
    public class ExplorationBridge : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] int defaultTravelTurns = 2;
        [SerializeField] int bonfireBasicCost = 10;
        [SerializeField] [Range(0f, 1f)] float bonfireBaseChance = 0.3f;

        GameState _gameState;

        // 활성 원정 (턴 기반 추적)
        List<ActiveExpedition> _activeExpeditions = new();

        // 정찰 정보 (ScoutInfo 인터페이스)
        Dictionary<int, ScoutLevel> _scoutData = new();

        /// <summary>탐사 보상 획득 시 발행. 경제 시스템 구독용.</summary>
        public event Action<ExplorationReward> OnExplorationReward;

        /// <summary>생존자 구조 시 발행. 인력 시스템 구독용.</summary>
        public event Action<string> OnSurvivorRescued;

        /// <summary>정찰 완료 시 발행. 웨이브 미리보기 연동.</summary>
        public event Action<int, ScoutLevel> OnScoutComplete;

        public IReadOnlyDictionary<int, ScoutLevel> ScoutData => _scoutData;

        public void Initialize(GameState state)
        {
            _gameState = state;
        }

        /// <summary>
        /// 원정대 파견. Idle 시민을 OnExpedition으로 전환.
        /// SF-EXP-003.
        /// </summary>
        public bool DispatchExpedition(string citizenId, int targetNodeIndex)
        {
            if (_gameState == null) return false;

            var citizen = _gameState.citizens.Find(c => c.citizenId == citizenId);
            if (citizen == null || citizen.state != CitizenState.Idle) return false;

            citizen.state = CitizenState.OnExpedition;

            _activeExpeditions.Add(new ActiveExpedition
            {
                CitizenId = citizenId,
                TargetNodeIndex = targetNodeIndex,
                TurnsRemaining = defaultTravelTurns,
                IsReturning = false
            });

            Debug.Log($"[ExplorationBridge] {citizen.displayName} dispatched to node {targetNodeIndex} ({defaultTravelTurns} turns)");
            return true;
        }

        /// <summary>
        /// 턴 종료 시 원정 진행 처리.
        /// 도착 → 보상 → 귀환 → 기지 복귀.
        /// </summary>
        public void ProcessTurn()
        {
            if (_gameState == null) return;

            for (int i = _activeExpeditions.Count - 1; i >= 0; i--)
            {
                var exp = _activeExpeditions[i];
                exp.TurnsRemaining--;

                if (exp.TurnsRemaining <= 0)
                {
                    if (!exp.IsReturning)
                    {
                        // 노드 도착 → 보상 처리 + 정찰 + 귀환 시작
                        ProcessArrival(exp);
                        exp.IsReturning = true;
                        exp.TurnsRemaining = defaultTravelTurns;
                    }
                    else
                    {
                        // 기지 복귀 → 시민 Idle 복원
                        ProcessReturn(exp);
                        _activeExpeditions.RemoveAt(i);
                    }
                }

                if (i < _activeExpeditions.Count)
                    _activeExpeditions[i] = exp;
            }
        }

        /// <summary>
        /// 모닥불 투자. 기초 자원 소모 → 확률적 생존자 합류.
        /// SF-ECO-009.
        /// </summary>
        public bool InvestBonfire()
        {
            if (_gameState == null) return false;
            if (!_gameState.resources.CanAfford(bonfireBasicCost)) return false;

            _gameState.resources.Spend(bonfireBasicCost);

            if (UnityEngine.Random.value < bonfireBaseChance)
            {
                string newId = $"citizen_{_gameState.citizens.Count + 1}";
                var newCitizen = new CitizenRuntimeState
                {
                    citizenId = newId,
                    displayName = $"Survivor {_gameState.citizens.Count + 1}",
                    aptitude = RandomAptitude(),
                    proficiencyLevel = 0,
                    state = CitizenState.Idle
                };
                _gameState.citizens.Add(newCitizen);

                OnSurvivorRescued?.Invoke(newId);
                Debug.Log($"[ExplorationBridge] Bonfire success! {newCitizen.displayName} joined ({newCitizen.aptitude})");
                return true;
            }

            Debug.Log("[ExplorationBridge] Bonfire: no survivors this turn");
            return false;
        }

        void ProcessArrival(ActiveExpedition exp)
        {
            // 노드 상태 갱신 (안개 해제)
            UpdateNodeState(exp.TargetNodeIndex, ExplorationNodeState.Visited);

            // 정찰 정보 등록 (SF-EXP-006)
            if (!_scoutData.ContainsKey(exp.TargetNodeIndex))
            {
                _scoutData[exp.TargetNodeIndex] = ScoutLevel.Scouted;
                OnScoutComplete?.Invoke(exp.TargetNodeIndex, ScoutLevel.Scouted);
            }

            // 탐사 보상 (SF-EXP-004, SF-ECO-010)
            var reward = GenerateReward(exp.TargetNodeIndex);
            _gameState.resources.Add(reward.Basic, reward.Advanced, reward.Relic);
            OnExplorationReward?.Invoke(reward);

            Debug.Log($"[ExplorationBridge] Arrived at node {exp.TargetNodeIndex} — " +
                      $"Reward: B+{reward.Basic} A+{reward.Advanced} R+{reward.Relic}");

            // 생존자 발견 확률 (SF-WF-010)
            if (UnityEngine.Random.value < 0.25f)
            {
                string newId = $"citizen_{_gameState.citizens.Count + 1}";
                var rescued = new CitizenRuntimeState
                {
                    citizenId = newId,
                    displayName = $"Explorer {_gameState.citizens.Count + 1}",
                    aptitude = CitizenAptitude.Exploration,
                    proficiencyLevel = 1,
                    state = CitizenState.Idle
                };
                _gameState.citizens.Add(rescued);
                OnSurvivorRescued?.Invoke(newId);
                Debug.Log($"[ExplorationBridge] Survivor rescued: {rescued.displayName}");
            }
        }

        void ProcessReturn(ActiveExpedition exp)
        {
            var citizen = _gameState.citizens.Find(c => c.citizenId == exp.CitizenId);
            if (citizen != null && citizen.state == CitizenState.OnExpedition)
            {
                citizen.state = CitizenState.Idle;
                Debug.Log($"[ExplorationBridge] {citizen.displayName} returned from expedition");
            }
        }

        void UpdateNodeState(int nodeIndex, ExplorationNodeState newState)
        {
            var node = _gameState.explorationNodes.Find(n => n.nodeId == nodeIndex.ToString());
            if (node != null)
            {
                node.state = newState;
            }
            else
            {
                _gameState.explorationNodes.Add(new ExplorationNodeRuntimeState
                {
                    nodeId = nodeIndex.ToString(),
                    state = newState
                });
            }
        }

        ExplorationReward GenerateReward(int nodeIndex)
        {
            // 스텁: 노드 인덱스 기반 간단한 보상 (M3에서 SO 기반으로 교체)
            return new ExplorationReward
            {
                NodeIndex = nodeIndex,
                Basic = UnityEngine.Random.Range(3, 8),
                Advanced = UnityEngine.Random.Range(0, 3),
                Relic = nodeIndex >= 3 ? 1 : 0
            };
        }

        static CitizenAptitude RandomAptitude()
        {
            var values = new[] {
                CitizenAptitude.Construction, CitizenAptitude.Combat,
                CitizenAptitude.Research, CitizenAptitude.Exploration
            };
            return values[UnityEngine.Random.Range(0, values.Length)];
        }
    }

    [Serializable]
    public struct ActiveExpedition
    {
        public string CitizenId;
        public int TargetNodeIndex;
        public int TurnsRemaining;
        public bool IsReturning;
    }

    public struct ExplorationReward
    {
        public int NodeIndex;
        public int Basic;
        public int Advanced;
        public int Relic;
    }

    public enum ScoutLevel
    {
        Unknown,   // 미탐사
        Scouted,   // 정찰 완료 (기본 정보)
        Detailed   // 정밀 정찰 (상세 정보)
    }
}
