using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Policy
{
    [CreateAssetMenu(fileName = "NewPolicyTree", menuName = "ProjectSun/Policy/Policy Tree")]
    public class PolicyTreeSO : ScriptableObject
    {
        [Header("트리 정보")]
        [SerializeField] private string treeName;
        [SerializeField, TextArea(2, 3)] private string treeDescription;

        [Header("카테��리별 노드")]
        [SerializeField] private List<PolicyNodeSO> domesticNodes = new();
        [SerializeField] private List<PolicyNodeSO> explorationNodes = new();
        [SerializeField] private List<PolicyNodeSO> defenseNodes = new();

        public string TreeName => treeName;
        public string TreeDescription => treeDescription;
        public IReadOnlyList<PolicyNodeSO> DomesticNodes => domesticNodes;
        public IReadOnlyList<PolicyNodeSO> ExplorationNodes => explorationNodes;
        public IReadOnlyList<PolicyNodeSO> DefenseNodes => defenseNodes;

        public IReadOnlyList<PolicyNodeSO> GetNodesForCategory(PolicyCategory category)
        {
            return category switch
            {
                PolicyCategory.Domestic => domesticNodes,
                PolicyCategory.Exploration => explorationNodes,
                PolicyCategory.Defense => defenseNodes,
                _ => domesticNodes
            };
        }

        public List<PolicyNodeSO> GetAllNodes()
        {
            var all = new List<PolicyNodeSO>();
            all.AddRange(domesticNodes);
            all.AddRange(explorationNodes);
            all.AddRange(defenseNodes);
            return all;
        }
    }
}
