using System;
using System.Collections.Generic;
using ProjectSun.Construction;
using ProjectSun.Workforce;
using UnityEngine;

namespace ProjectSun.Exploration
{
    /// <summary>
    /// 탐사 시스템 핵심 매니저.
    /// 맵 상태 관리, 원정대 이동, 안개(Fog) 관리, 도착 이벤트 큐를 처리.
    /// </summary>
    public class ExplorationManager : MonoBehaviour
    {
        [Header("맵 데이터")]
        [SerializeField] private ExplorationMapSO mapData;

        [Header("원정대 설정")]
        [SerializeField] private int minTeamSize = 1;
        [SerializeField] private int maxTeamSize = 3;

        [Header("연동")]
        [SerializeField] private WorkforceManager workforceManager;
        [SerializeField] private BuildingManager buildingManager;

        // 노드별 안개 상태
        private FogState[] fogStates;

        // 노드 방문 여부
        private bool[] visitedNodes;

        // 원정대 목록
        private List<ExpeditionTeam> teams = new();

        // 현재 파견 가능한 최대 팀 수 (탐사 건물 수)
        private int maxTeams;

        // 도착 이벤트 큐 (도착 노드 인덱스 + 팀 ID)
        private Queue<ArrivalEvent> arrivalQueue = new();

        public ExplorationMapSO MapData => mapData;
        public IReadOnlyList<ExpeditionTeam> Teams => teams;
        public int MaxTeams => maxTeams;
        public int MinTeamSize => minTeamSize;
        public int MaxTeamSize => maxTeamSize;
        public int BaseNodeIndex => mapData != null ? mapData.baseNodeIndex : -1;
        public bool HasPendingArrivals => arrivalQueue.Count > 0;

        public event Action OnMapStateChanged;
        public event Action<ArrivalEvent> OnTeamArrived;
        public event Action OnTeamsChanged;

        /// <summary>
        /// 맵 초기화 (게임 시작 시 호출)
        /// </summary>
        public void InitializeMap()
        {
            if (mapData == null)
            {
                Debug.LogWarning("[ExplorationManager] MapData가 설정되지 않았습니다.");
                return;
            }

            int nodeCount = mapData.nodes.Count;
            fogStates = new FogState[nodeCount];
            visitedNodes = new bool[nodeCount];

            // 모든 노드 Hidden으로 초기화
            for (int i = 0; i < nodeCount; i++)
            {
                fogStates[i] = FogState.Hidden;
                visitedNodes[i] = false;
            }

            // 기지 노드 + 인접 노드 공개
            RevealNode(mapData.baseNodeIndex);

            OnMapStateChanged?.Invoke();
        }

        /// <summary>
        /// 탐사 건물 수에 따라 최대 팀 수 설정
        /// </summary>
        public void SetMaxTeams(int count)
        {
            maxTeams = Mathf.Max(0, count);

            // 팀 수가 증가하면 새 팀 추가
            while (teams.Count < maxTeams)
            {
                var team = new ExpeditionTeam(teams.Count);
                if (mapData != null)
                    team.PlaceAtBase(mapData.baseNodeIndex);
                teams.Add(team);
            }

            OnTeamsChanged?.Invoke();
        }

        /// <summary>
        /// 노드의 안개 상태 조회
        /// </summary>
        public FogState GetFogState(int nodeIndex)
        {
            if (fogStates == null || nodeIndex < 0 || nodeIndex >= fogStates.Length)
                return FogState.Hidden;
            return fogStates[nodeIndex];
        }

        /// <summary>
        /// 노드 방문 여부 조회
        /// </summary>
        public bool IsNodeVisited(int nodeIndex)
        {
            if (visitedNodes == null || nodeIndex < 0 || nodeIndex >= visitedNodes.Length)
                return false;
            return visitedNodes[nodeIndex];
        }

        /// <summary>
        /// 원정대에 목적지 설정
        /// </summary>
        public bool SetTeamDestination(int teamIndex, int targetNodeIndex)
        {
            if (teamIndex < 0 || teamIndex >= teams.Count) return false;
            var team = teams[teamIndex];

            if (team.State != ExpeditionState.Arrived && team.State != ExpeditionState.Idle)
                return false;

            if (mapData == null || !mapData.IsValidIndex(targetNodeIndex))
                return false;

            // 이미 방문한 노드 & 재방문 불가
            var nodeEntry = mapData.nodes[targetNodeIndex];
            if (visitedNodes[targetNodeIndex] && (nodeEntry.nodeData == null || !nodeEntry.nodeData.isRevisitable))
                return false;

            // 인접 노드인지 확인
            int fromNode = team.CurrentNodeIndex;
            if (fromNode < 0) fromNode = mapData.baseNodeIndex;

            var neighbors = mapData.GetNeighbors(fromNode);
            int travelTurns = -1;
            foreach (var (neighborIdx, turns) in neighbors)
            {
                if (neighborIdx == targetNodeIndex)
                {
                    travelTurns = turns;
                    break;
                }
            }

            if (travelTurns < 0) return false; // 인접하지 않음

            team.SetDestination(targetNodeIndex, travelTurns);
            OnTeamsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 원정대 귀환 명령
        /// </summary>
        public bool OrderReturn(int teamIndex)
        {
            if (teamIndex < 0 || teamIndex >= teams.Count) return false;
            var team = teams[teamIndex];

            if (team.State == ExpeditionState.Idle || team.State == ExpeditionState.Returning)
                return false;

            if (mapData == null) return false;

            int fromNode = team.CurrentNodeIndex;
            if (team.State == ExpeditionState.Moving)
            {
                // 이동 중이면 목적지에 먼저 도착 후 귀환 (간소화)
                // 또는 현재 출발 노드로 되돌아감 — PoC에서는 도착 대기 후 귀환
                return false; // 이동 중에는 귀환 불가
            }

            // BFS로 기지까지 최단 경로 계산
            var (path, turnsList) = FindShortestPath(fromNode, mapData.baseNodeIndex);
            if (path == null || path.Count == 0) return false;

            team.SetReturnPath(path, turnsList);
            OnTeamsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 턴 종료 시 호출: 모든 원정대 이동 처리
        /// </summary>
        public void ProcessTurnMovement()
        {
            foreach (var team in teams)
            {
                bool arrived = team.ProcessMoveTurn();
                if (arrived)
                {
                    int nodeIndex = team.CurrentNodeIndex;

                    if (team.State == ExpeditionState.Arrived)
                    {
                        // 노드 도착 → 안개 해제 + 방문 처리 + 이벤트 큐
                        RevealNode(nodeIndex);
                        visitedNodes[nodeIndex] = true;

                        arrivalQueue.Enqueue(new ArrivalEvent
                        {
                            teamId = team.TeamId,
                            nodeIndex = nodeIndex
                        });
                    }
                    // 귀환 완료 (Idle 상태) → 인력 복귀는 SF-03에서 처리
                }
            }

            OnMapStateChanged?.Invoke();
            OnTeamsChanged?.Invoke();
        }

        /// <summary>
        /// 도착 이벤트 큐에서 다음 이벤트 꺼내기
        /// </summary>
        public ArrivalEvent? DequeueArrival()
        {
            if (arrivalQueue.Count == 0) return null;
            return arrivalQueue.Dequeue();
        }

        /// <summary>
        /// 원정대 인원 설정 (인력 시스템 연동)
        /// </summary>
        public bool SetTeamMembers(int teamIndex, int memberCount)
        {
            if (teamIndex < 0 || teamIndex >= teams.Count) return false;
            var team = teams[teamIndex];

            if (team.State != ExpeditionState.Idle) return false;
            if (memberCount < minTeamSize || memberCount > maxTeamSize) return false;

            int currentMembers = team.MemberCount;
            int delta = memberCount - currentMembers;

            if (workforceManager != null)
            {
                if (delta > 0)
                {
                    if (!workforceManager.AssignExpeditionWorkers(delta))
                        return false;
                }
                else if (delta < 0)
                {
                    workforceManager.ReturnExpeditionWorkers(-delta);
                }
            }

            team.SetMembers(memberCount);
            OnTeamsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 원정대 해산 (인력 복귀)
        /// </summary>
        public bool DisbandTeam(int teamIndex)
        {
            if (teamIndex < 0 || teamIndex >= teams.Count) return false;
            var team = teams[teamIndex];

            if (team.State != ExpeditionState.Idle) return false;

            int members = team.MemberCount;
            if (members > 0 && workforceManager != null)
            {
                workforceManager.ReturnExpeditionWorkers(members);
            }

            team.Disband();
            if (mapData != null)
                team.PlaceAtBase(mapData.baseNodeIndex);

            OnTeamsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 탐사 건물 수 갱신 (BuildingManager 이벤트에서 호출)
        /// </summary>
        public void RefreshMaxTeams()
        {
            if (buildingManager == null)
            {
                SetMaxTeams(0);
                return;
            }

            int explorationBuildingCount = 0;
            foreach (var slot in buildingManager.AllSlots)
            {
                if (slot.CurrentBuildingData != null &&
                    slot.CurrentBuildingData.category == BuildingCategory.Exploration &&
                    slot.State == BuildingSlotState.Active)
                {
                    explorationBuildingCount++;
                }
            }

            SetMaxTeams(explorationBuildingCount);
        }

        /// <summary>
        /// 귀환 완료된 원정대의 인력을 자동 복귀 처리
        /// </summary>
        public void ProcessReturnedTeams()
        {
            if (mapData == null) return;

            foreach (var team in teams)
            {
                if (team.IsAtBase(mapData.baseNodeIndex) && team.MemberCount > 0 &&
                    team.State == ExpeditionState.Idle)
                {
                    // 기지에 있고 인원이 있는 팀은 유지 (플레이어가 해산할지 결정)
                }
            }
        }

        private void Start()
        {
            // 건설 시스템 이벤트 구독
            if (buildingManager != null)
            {
                buildingManager.OnConstructionCompleted += HandleBuildingChanged;
                buildingManager.OnBuildingDestroyed += HandleBuildingChanged;
                buildingManager.OnUpgradeCompleted += HandleBuildingChanged;
            }

            InitializeMap();
            RefreshMaxTeams();
        }

        private void OnDestroy()
        {
            if (buildingManager != null)
            {
                buildingManager.OnConstructionCompleted -= HandleBuildingChanged;
                buildingManager.OnBuildingDestroyed -= HandleBuildingChanged;
                buildingManager.OnUpgradeCompleted -= HandleBuildingChanged;
            }
        }

        private void HandleBuildingChanged(BuildingSlot slot)
        {
            if (slot.CurrentBuildingData != null &&
                slot.CurrentBuildingData.category == BuildingCategory.Exploration)
            {
                RefreshMaxTeams();
            }
        }

        /// <summary>
        /// 특정 노드의 노드 데이터 반환
        /// </summary>
        public ExplorationNodeSO GetNodeData(int nodeIndex)
        {
            if (mapData == null || !mapData.IsValidIndex(nodeIndex)) return null;
            return mapData.nodes[nodeIndex].nodeData;
        }

        /// <summary>
        /// 특정 팀이 이동 가능한 인접 노드 목록
        /// </summary>
        public List<(int nodeIndex, int travelTurns)> GetAvailableDestinations(int teamIndex)
        {
            var result = new List<(int, int)>();
            if (teamIndex < 0 || teamIndex >= teams.Count) return result;
            if (mapData == null) return result;

            var team = teams[teamIndex];
            int fromNode = team.CurrentNodeIndex;
            if (fromNode < 0) fromNode = mapData.baseNodeIndex;

            var neighbors = mapData.GetNeighbors(fromNode);
            foreach (var (neighborIdx, turns) in neighbors)
            {
                // Hidden 노드도 이동 가능 (이동하면 공개됨)
                var nodeEntry = mapData.nodes[neighborIdx];
                bool canVisit = !visitedNodes[neighborIdx] ||
                                (nodeEntry.nodeData != null && nodeEntry.nodeData.isRevisitable);

                // 기지 노드는 항상 이동 가능 (귀환용)
                if (neighborIdx == mapData.baseNodeIndex)
                    canVisit = true;

                if (canVisit)
                    result.Add((neighborIdx, turns));
            }

            return result;
        }

        /// <summary>
        /// 노드 공개 + 인접 노드 힌트 표시
        /// </summary>
        private void RevealNode(int nodeIndex)
        {
            if (fogStates == null || nodeIndex < 0 || nodeIndex >= fogStates.Length) return;

            fogStates[nodeIndex] = FogState.Revealed;

            // 인접 노드를 Hinted로 전환
            if (mapData != null)
            {
                var neighbors = mapData.GetNeighbors(nodeIndex);
                foreach (var (neighborIdx, _) in neighbors)
                {
                    if (neighborIdx >= 0 && neighborIdx < fogStates.Length &&
                        fogStates[neighborIdx] == FogState.Hidden)
                    {
                        fogStates[neighborIdx] = FogState.Hinted;
                    }
                }
            }
        }

        /// <summary>
        /// BFS로 두 노드 간 최단 경로 계산.
        /// 방문 완료 노드도 경유 가능 (귀환용).
        /// </summary>
        private (List<int> path, List<int> turnsList) FindShortestPath(int from, int to)
        {
            if (mapData == null || from == to)
                return (new List<int>(), new List<int>());

            var visited = new HashSet<int> { from };
            var queue = new Queue<int>();
            var prev = new Dictionary<int, int>();
            var edgeTurns = new Dictionary<(int, int), int>();

            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current == to) break;

                var neighbors = mapData.GetNeighbors(current);
                foreach (var (neighborIdx, turns) in neighbors)
                {
                    if (visited.Contains(neighborIdx)) continue;
                    visited.Add(neighborIdx);
                    prev[neighborIdx] = current;
                    edgeTurns[(current, neighborIdx)] = turns;
                    queue.Enqueue(neighborIdx);
                }
            }

            if (!prev.ContainsKey(to) && from != to)
                return (null, null);

            // 경로 역추적
            var path = new List<int>();
            var turnsList = new List<int>();
            int node = to;
            while (node != from)
            {
                path.Add(node);
                int prevNode = prev[node];
                turnsList.Add(edgeTurns.ContainsKey((prevNode, node))
                    ? edgeTurns[(prevNode, node)]
                    : 1);
                node = prevNode;
            }

            path.Reverse();
            turnsList.Reverse();

            return (path, turnsList);
        }
    }

    /// <summary>
    /// 원정대 도착 이벤트 데이터
    /// </summary>
    [Serializable]
    public struct ArrivalEvent
    {
        public int teamId;
        public int nodeIndex;
    }
}
