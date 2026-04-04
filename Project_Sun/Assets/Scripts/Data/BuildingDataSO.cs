using UnityEngine;

namespace ProjectSun.V2.Data
{
    [CreateAssetMenu(menuName = "ProjectSun/V2/Data/Building")]
    public class BuildingDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string buildingId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;

        [Header("Classification")]
        public BuildingCategoryV2 category;
        public bool isHeadquarters;

        [Header("Construction")]
        public ResourceCostV2 buildCost;

        [Header("Production")]
        [Tooltip("Basic resource produced per turn (0 if non-production building)")]
        public int basicPerTurn;
        [Tooltip("Advanced resource produced per turn (0 if non-production building)")]
        public int advancedPerTurn;

        [Header("Storage")]
        [Tooltip("Basic cap increase when active")]
        public int basicCapBonus;
        [Tooltip("Advanced cap increase when active")]
        public int advancedCapBonus;

        [Header("Durability")]
        [Min(1)]
        public int maxHP = 100;

        [Header("Upgrade")]
        public UpgradePathType upgradePathType = UpgradePathType.BranchAB;
        public UpgradeBranch branchA;
        public UpgradeBranch branchB;
        [Tooltip("Linear upgrade tiers (wall only): tier costs in order")]
        public ResourceCostV2[] linearUpgradeCosts;

        [Header("Defense Stats")]
        [Tooltip("Attack power per hit (defense buildings only)")]
        public float attackPower;
        [Tooltip("Attack range in world units")]
        public float attackRange;
        [Tooltip("Attacks per second")]
        public float attackSpeed;
    }
}
