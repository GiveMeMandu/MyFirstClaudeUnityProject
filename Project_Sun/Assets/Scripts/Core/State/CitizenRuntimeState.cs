using System;

namespace ProjectSun.V2.Data
{
    [Serializable]
    public class CitizenRuntimeState
    {
        public string citizenId;
        public string displayName;
        public CitizenAptitude aptitude;
        public int proficiencyLevel;
        public CitizenState state;
        public string assignedSlotId;
    }
}
