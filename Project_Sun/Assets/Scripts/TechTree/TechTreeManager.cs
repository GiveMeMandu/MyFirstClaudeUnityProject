using System;
using System.Collections.Generic;
using System.Linq;
using ProjectSun.Construction;
using ProjectSun.Resource;
using ProjectSun.Turn;
using ProjectSun.Workforce;
using UnityEngine;

namespace ProjectSun.TechTree
{
    public class TechTreeManager : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private TechTreeDataSO techTreeData;

        [Header("연동 시스템")]
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private WorkforceManager workforceManager;

        [Header("런타임 상태 (디버그)")]
        [SerializeField] private string currentResearchName;

        // 노드별 런타임 상태
        private readonly Dictionary<TechNodeSO, TechNodeState> nodeStates = new();
        // 노드별 누적 진행도
        private readonly Dictionary<TechNodeSO, float> nodeProgress = new();
        // 착수 비용 지불 여부
        private readonly HashSet<TechNodeSO> costPaidNodes = new();

        // 현재 연구 중인 노드
        private TechNodeSO currentResearch;

        // 이번 턴에 완료된 연구 (DayStart에서 알림용)
        private TechNodeSO pendingCompletion;

        // 이벤트
        public event Action<TechNodeSO> OnResearchStarted;
        public event Action<TechNodeSO> OnResearchSwitched;
        public event Action<TechNodeSO, float> OnResearchProgress;
        public event Action<TechNodeSO> OnResearchCompleted;
        public event Action<TechNodeSO, TechNodeState> OnNodeStateChanged;

        // 프로퍼티
        public TechTreeDataSO TechTreeData => techTreeData;
        public TechNodeSO CurrentResearch => currentResearch;
        public IReadOnlyDictionary<TechNodeSO, TechNodeState> NodeStates => nodeStates;

        private void OnEnable()
        {
            if (turnManager != null)
                turnManager.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (turnManager != null)
                turnManager.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void Start()
        {
            InitializeTree();
        }

        /// <summary>
        /// 트리 초기화 — 모든 노드의 초기 상태를 결정
        /// </summary>
        public void InitializeTree()
        {
            nodeStates.Clear();
            nodeProgress.Clear();
            costPaidNodes.Clear();
            currentResearch = null;
            pendingCompletion = null;

            if (techTreeData == null) return;

            foreach (var category in techTreeData.categories)
            {
                if (category == null) continue;
                foreach (var node in category.nodes)
                {
                    if (node == null) continue;
                    var state = ArePrerequisitesMet(node) ? TechNodeState.Available : TechNodeState.Locked;
                    nodeStates[node] = state;
                    nodeProgress[node] = 0f;
                }
            }
        }

        /// <summary>
        /// 연구 시작 시도
        /// </summary>
        public bool StartResearch(TechNodeSO node)
        {
            if (node == null) return false;
            if (!nodeStates.TryGetValue(node, out var state)) return false;

            // 이미 완료된 노드
            if (state == TechNodeState.Completed) return false;

            // 선행 연구 미충족
            if (state == TechNodeState.Locked) return false;

            // 연구 건물 확인
            if (!HasActiveResearchBuilding()) return false;

            // 같은 노드를 이미 연구 중
            if (currentResearch == node) return false;

            // 착수 비용 처리 (최초 1회)
            if (!costPaidNodes.Contains(node))
            {
                if (node.researchCost.Count > 0)
                {
                    if (!resourceManager.CanAfford(node.researchCost)) return false;
                    resourceManager.SpendCosts(node.researchCost);
                }
                costPaidNodes.Add(node);
            }

            // 기존 연구가 있으면 전환
            if (currentResearch != null)
            {
                SwitchFromCurrent();
            }

            // 새 연구 시작
            currentResearch = node;
            SetNodeState(node, TechNodeState.InProgress);
            currentResearchName = node.nodeName;

            OnResearchStarted?.Invoke(node);
            return true;
        }

        /// <summary>
        /// 현재 연구를 Paused로 전환 (진행도 보존)
        /// </summary>
        private void SwitchFromCurrent()
        {
            if (currentResearch == null) return;

            var prev = currentResearch;
            SetNodeState(prev, TechNodeState.Paused);
            currentResearch = null;
            currentResearchName = "";

            OnResearchSwitched?.Invoke(prev);
        }

        /// <summary>
        /// 턴 종료 시 연구 진행도 증가
        /// </summary>
        public void ProcessResearchProgress()
        {
            if (currentResearch == null) return;
            if (!HasActiveResearchBuilding()) return;

            int workerCount = GetResearchWorkerCount();
            if (workerCount <= 0) return;

            float pointsGained = workerCount * techTreeData.researchPointsPerWorker;
            nodeProgress[currentResearch] += pointsGained;

            OnResearchProgress?.Invoke(currentResearch, GetProgress(currentResearch));

            // 완료 체크 — 완료 알림은 다음 DayStart로 지연
            if (nodeProgress[currentResearch] >= currentResearch.requiredResearchPoints)
            {
                pendingCompletion = currentResearch;
            }
        }

        /// <summary>
        /// 다음 낮 시작 시 완료 처리 + 알림
        /// </summary>
        public void ProcessDayStartCompletion()
        {
            if (pendingCompletion == null) return;

            var completed = pendingCompletion;
            pendingCompletion = null;

            CompleteResearch(completed);
        }

        /// <summary>
        /// 연구 완료 처리
        /// </summary>
        private void CompleteResearch(TechNodeSO node)
        {
            SetNodeState(node, TechNodeState.Completed);

            if (currentResearch == node)
            {
                currentResearch = null;
                currentResearchName = "";
            }

            // 효과 적용
            ApplyEffects(node);

            // 후속 노드 해금 체크
            RefreshNodeAvailability();

            OnResearchCompleted?.Invoke(node);
        }

        /// <summary>
        /// 연구 완료 효과 적용
        /// </summary>
        private void ApplyEffects(TechNodeSO node)
        {
            foreach (var effect in node.effects)
            {
                switch (effect.effectType)
                {
                    case TechEffectType.BuildingUpgrade:
                        // TODO: BuildingManager에 업그레이드 분기 해금 알림
                        Debug.Log($"[TechTree] 건물 업그레이드 해금: {effect.targetId} ({effect.description})");
                        break;

                    case TechEffectType.SlotReveal:
                        // TODO: BuildingManager에 숨겨진 슬롯 공개 요청
                        Debug.Log($"[TechTree] 슬롯 공개: {effect.targetId} ({effect.description})");
                        break;

                    case TechEffectType.StatBonus:
                        Debug.Log($"[TechTree] 능력치 보너스: {effect.targetId} +{effect.value} ({effect.description})");
                        break;

                    case TechEffectType.BuildingSlotAdd:
                        Debug.Log($"[TechTree] 건물 슬롯 추가: {effect.targetId} +{effect.value} ({effect.description})");
                        break;

                    case TechEffectType.FeatureUnlock:
                        Debug.Log($"[TechTree] 기능 해금: {effect.targetId} ({effect.description})");
                        break;
                }
            }
        }

        /// <summary>
        /// 모든 노드의 Available/Locked 상태를 재계산
        /// </summary>
        private void RefreshNodeAvailability()
        {
            foreach (var kvp in nodeStates.ToList())
            {
                if (kvp.Value == TechNodeState.Locked)
                {
                    if (ArePrerequisitesMet(kvp.Key))
                    {
                        SetNodeState(kvp.Key, TechNodeState.Available);
                    }
                }
            }
        }

        // ── 조회 API ──────────────────────────────────────────

        public TechNodeState GetNodeState(TechNodeSO node)
        {
            return nodeStates.TryGetValue(node, out var state) ? state : TechNodeState.Locked;
        }

        public float GetProgress(TechNodeSO node)
        {
            if (!nodeProgress.TryGetValue(node, out var progress)) return 0f;
            return Mathf.Clamp01(progress / node.requiredResearchPoints);
        }

        public float GetCurrentProgressPoints(TechNodeSO node)
        {
            return nodeProgress.TryGetValue(node, out var progress) ? progress : 0f;
        }

        public bool HasPaidCost(TechNodeSO node)
        {
            return costPaidNodes.Contains(node);
        }

        public bool IsResearchComplete(TechNodeSO node)
        {
            return GetNodeState(node) == TechNodeState.Completed;
        }

        public List<TechNodeSO> GetCompletedNodes()
        {
            return nodeStates
                .Where(kvp => kvp.Value == TechNodeState.Completed)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        // ── 내부 헬퍼 ────────────────────────────────────────

        private bool ArePrerequisitesMet(TechNodeSO node)
        {
            if (node.prerequisites == null || node.prerequisites.Count == 0)
                return true;

            return node.prerequisites.All(prereq =>
                prereq != null && nodeStates.TryGetValue(prereq, out var s) && s == TechNodeState.Completed);
        }

        private bool HasActiveResearchBuilding()
        {
            if (buildingManager == null) return true; // 테스트 시 연동 없이 동작

            var researchSlots = buildingManager.GetSlotsByCategory(BuildingCategory.Research);
            return researchSlots.Any(s => s.State == BuildingSlotState.Active);
        }

        private int GetResearchWorkerCount()
        {
            if (buildingManager == null || workforceManager == null) return 1; // 테스트 기본값

            var researchSlots = buildingManager.GetSlotsByCategory(BuildingCategory.Research);
            int total = 0;
            foreach (var slot in researchSlots)
            {
                if (slot.State == BuildingSlotState.Active)
                    total += workforceManager.GetBuildingTotalWorkers(slot);
            }
            return total;
        }

        private void SetNodeState(TechNodeSO node, TechNodeState newState)
        {
            nodeStates[node] = newState;
            OnNodeStateChanged?.Invoke(node, newState);
        }

        private void HandlePhaseChanged(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.DayEnd:
                    ProcessResearchProgress();
                    break;
                case TurnPhase.DayStart:
                    ProcessDayStartCompletion();
                    break;
            }
        }
    }
}
