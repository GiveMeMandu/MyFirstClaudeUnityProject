using System;
using UnityEngine;

namespace ProjectSun.V2.Data
{
    [Serializable]
    public class UpgradeBranch
    {
        public string branchId;
        public string displayName;
        [TextArea(1, 3)]
        public string description;
        public ResourceCostV2 upgradeCost;
        public int basicPerTurnOverride = -1;
        public int advancedPerTurnOverride = -1;
        public int maxHPOverride = -1;
        public float attackPowerOverride = -1f;
        public float attackRangeOverride = -1f;
        public float attackSpeedOverride = -1f;
        public int basicCapBonus;
        public int advancedCapBonus;
        public bool requiresResearch;
        public string requiredResearchId;
    }
}
