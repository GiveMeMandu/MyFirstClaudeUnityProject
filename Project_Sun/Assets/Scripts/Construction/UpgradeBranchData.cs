using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Construction
{
    [CreateAssetMenu(fileName = "NewUpgradeBranch", menuName = "ProjectSun/Construction/Upgrade Branch")]
    public class UpgradeBranchData : ScriptableObject
    {
        [Header("분기 정보")]
        [Tooltip("분기 표시 이름")]
        public string branchName;

        [TextArea(2, 4)]
        [Tooltip("분기 설명")]
        public string description;

        [Header("업그레이드 후 건물 데이터")]
        [Tooltip("이 분기 선택 시 변환될 BuildingData")]
        public BuildingData upgradedBuilding;

        [Header("비용")]
        [Tooltip("업그레이드 자원 비용")]
        public List<ResourceCost> upgradeCost = new();

        [Tooltip("업그레이드 소요 턴")]
        [Min(1)]
        public int upgradeTurns = 2;

        [Header("해금 조건")]
        [Tooltip("연구 트리에서 해금되어야 사용 가능한 분기인지")]
        public bool requiresResearch;

        [Tooltip("기본 분기인 경우 true (건물 완성 시 바로 사용 가능)")]
        public bool isDefaultBranch = true;
    }
}
