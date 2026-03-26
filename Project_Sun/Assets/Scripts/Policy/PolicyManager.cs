using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Policy
{
    public class PolicyManager : MonoBehaviour
    {
        [Header("정책 트리")]
        [SerializeField] private PolicyTreeSO policyTree;

        [Header("런타임 상태")]
        [SerializeField] private int lastCheckedTurn;

        private readonly Dictionary<string, PolicyNodeState> nodeStates = new();
        private readonly List<PolicyNodeSO> enactedNodes = new();
        private readonly List<PolicyNodeSO> newlyUnlockedNodes = new();

        public PolicyTreeSO PolicyTree => policyTree;
        public IReadOnlyList<PolicyNodeSO> EnactedNodes => enactedNodes;
        public IReadOnlyList<PolicyNodeSO> NewlyUnlockedNodes => newlyUnlockedNodes;

        public event Action<PolicyNodeSO> OnNodeUnlocked;
        public event Action<PolicyNodeSO> OnNodeEnacted;
        public event Action<PolicyNodeSO> OnNodeBranchLocked;
        public event Action<List<PolicyNodeSO>> OnNewNodesAvailable;

        private void Awake()
        {
            InitializeNodeStates();
        }

        private void InitializeNodeStates()
        {
            nodeStates.Clear();
            enactedNodes.Clear();

            if (policyTree == null) return;

            foreach (var node in policyTree.GetAllNodes())
            {
                if (node == null) continue;
                nodeStates[node.NodeId] = PolicyNodeState.Locked;
            }
        }

        public PolicyNodeState GetNodeState(PolicyNodeSO node)
        {
            if (node == null) return PolicyNodeState.Locked;
            return nodeStates.TryGetValue(node.NodeId, out var state)
                ? state
                : PolicyNodeState.Locked;
        }

        public PolicyNodeState GetNodeState(string nodeId)
        {
            return nodeStates.TryGetValue(nodeId, out var state)
                ? state
                : PolicyNodeState.Locked;
        }

        /// <summary>
        /// 턴 시작 시 ���출. 해금 조건 충족된 노드를 Unlocked로 전환.
        /// </summary>
        public void OnNewTurn(int turnNumber)
        {
            if (policyTree == null) return;

            lastCheckedTurn = turnNumber;
            newlyUnlockedNodes.Clear();

            foreach (var node in policyTree.GetAllNodes())
            {
                if (node == null) continue;
                if (GetNodeState(node) != PolicyNodeState.Locked) continue;

                if (CanUnlock(node, turnNumber))
                {
                    SetNodeState(node, PolicyNodeState.Unlocked);
                    newlyUnlockedNodes.Add(node);
                    OnNodeUnlocked?.Invoke(node);
                }
            }

            if (newlyUnlockedNodes.Count > 0)
            {
                OnNewNodesAvailable?.Invoke(newlyUnlockedNodes);
            }
        }

        private bool CanUnlock(PolicyNodeSO node, int turnNumber)
        {
            // 턴 조건
            if (turnNumber < node.UnlockTurn) return false;

            // 선행 노드 조건
            if (node.PrerequisiteNode != null)
            {
                if (GetNodeState(node.PrerequisiteNode) != PolicyNodeState.Enacted)
                    return false;
            }

            // 분기 노드: 상대방이 이미 Enacted면 이 노드는 BranchLocked
            if (node.IsBranchNode && node.BranchPairNode != null)
            {
                if (GetNodeState(node.BranchPairNode) == PolicyNodeState.Enacted)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 정책 노드를 제정. 성공 시 true.
        /// </summary>
        public bool EnactNode(PolicyNodeSO node)
        {
            if (node == null) return false;

            var state = GetNodeState(node);
            if (state != PolicyNodeState.Unlocked) return false;

            // 제정
            SetNodeState(node, PolicyNodeState.Enacted);
            enactedNodes.Add(node);
            OnNodeEnacted?.Invoke(node);

            // 분기 상대 노드 영구 잠김
            if (node.IsBranchNode && node.BranchPairNode != null)
            {
                LockBranch(node.BranchPairNode);
            }

            return true;
        }

        private void LockBranch(PolicyNodeSO node)
        {
            var currentState = GetNodeState(node);
            if (currentState == PolicyNodeState.Enacted) return;

            SetNodeState(node, PolicyNodeState.BranchLocked);
            OnNodeBranchLocked?.Invoke(node);

            // 분기 잠김 노드의 후속 노드도 잠금
            if (policyTree == null) return;
            foreach (var otherNode in policyTree.GetAllNodes())
            {
                if (otherNode == null) continue;
                if (otherNode.PrerequisiteNode == node)
                {
                    LockBranch(otherNode);
                }
            }
        }

        private void SetNodeState(PolicyNodeSO node, PolicyNodeState state)
        {
            nodeStates[node.NodeId] = state;
        }

        /// <summary>
        /// 특정 카테고리에서 선택 가능한(Unlocked) 노드 목록
        /// </summary>
        public List<PolicyNodeSO> GetUnlockedNodes(PolicyCategory category)
        {
            var result = new List<PolicyNodeSO>();
            if (policyTree == null) return result;

            foreach (var node in policyTree.GetNodesForCategory(category))
            {
                if (node != null && GetNodeState(node) == PolicyNodeState.Unlocked)
                    result.Add(node);
            }
            return result;
        }

        /// <summary>
        /// 선택 가능한(Unlocked) 노드가 있는지
        /// </summary>
        public bool HasUnlockedNodes()
        {
            if (policyTree == null) return false;

            foreach (var node in policyTree.GetAllNodes())
            {
                if (node != null && GetNodeState(node) == PolicyNodeState.Unlocked)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 모든 정책이 Enacted 또는 BranchLocked 상태인지
        /// </summary>
        public bool AreAllPoliciesResolved()
        {
            if (policyTree == null) return true;

            foreach (var node in policyTree.GetAllNodes())
            {
                if (node == null) continue;
                var state = GetNodeState(node);
                if (state == PolicyNodeState.Locked || state == PolicyNodeState.Unlocked)
                    return false;
            }
            return true;
        }

        public void SetPolicyTree(PolicyTreeSO tree)
        {
            policyTree = tree;
            InitializeNodeStates();
        }
    }
}
