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

        /// <summary>
        /// 방어 등급. GDD 4단계: Perfect / Minor / Moderate / Major.
        /// WaveDefense.md 6.2, Economy-Model.md 7.1 참조.
        /// </summary>
        public DefenseResultGrade grade;

        /// <summary>
        /// 건물 총 피해 비율 (0~1).
        /// 0 = 피해 없음, 1 = 모든 건물 HP 소진.
        /// GDD 판정 기준: <=0.10 완벽, <=0.25 경미, <=0.50 중간, >0.50 대규모.
        /// </summary>
        public float damageRatio;
    }
}
