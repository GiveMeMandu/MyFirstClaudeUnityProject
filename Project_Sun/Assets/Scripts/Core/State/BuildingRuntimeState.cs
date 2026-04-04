using System;

namespace ProjectSun.V2.Data
{
    [Serializable]
    public class BuildingRuntimeState
    {
        public string slotId;
        public string buildingId;
        public BuildingSlotStateV2 state;
        public int currentHP;
        public int maxHP;
        public int upgradeLevel;
        public string selectedBranchId;
        public string assignedCitizenId;
    }
}
