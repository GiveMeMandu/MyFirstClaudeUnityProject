using System;
using System.Collections.Generic;

namespace ProjectSun.V2.Data
{
    [Serializable]
    public class WaveResult
    {
        public int turnNumber;
        public int enemiesDefeated;
        public int enemiesTotal;
        public bool isPerfectDefense;
        public bool headquartersDestroyed;
        public int basicReward;
        public int advancedReward;
        public int relicReward;
        public List<string> damagedBuildingSlotIds = new();
    }
}
