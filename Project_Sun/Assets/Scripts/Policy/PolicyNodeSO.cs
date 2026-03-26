using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Policy
{
    [Serializable]
    public struct PolicyEffect
    {
        public PolicyEffectType effectType;
        public float value;
        [Tooltip("true면 % 수정자, false면 절대값")]
        public bool isPercentage;
        [Tooltip("효과 대상 (자원 타입, 건물 ID 등). 필요 시 사용")]
        public string target;
    }

    [CreateAssetMenu(fileName = "NewPolicyNode", menuName = "ProjectSun/Policy/Policy Node")]
    public class PolicyNodeSO : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string nodeId;
        [SerializeField] private string nodeName;
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private PolicyCategory category;

        [Header("해금 조건")]
        [SerializeField] private int unlockTurn = 1;
        [SerializeField] private PolicyNodeSO prerequisiteNode;

        [Header("분기 설정")]
        [SerializeField] private bool isBranchNode;
        [SerializeField, Tooltip("분기 상대 노드 (A↔B)")]
        private PolicyNodeSO branchPairNode;

        [Header("효과")]
        [SerializeField] private List<PolicyEffect> effects = new();

        public string NodeId => nodeId;
        public string NodeName => nodeName;
        public string Description => description;
        public PolicyCategory Category => category;
        public int UnlockTurn => unlockTurn;
        public PolicyNodeSO PrerequisiteNode => prerequisiteNode;
        public bool IsBranchNode => isBranchNode;
        public PolicyNodeSO BranchPairNode => branchPairNode;
        public IReadOnlyList<PolicyEffect> Effects => effects;
    }
}
