using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Encounter
{
    /// <summary>
    /// 인카운터 풀 — 일상/중요 인카운터 목록을 보유.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEncounterPool", menuName = "ProjectSun/Encounter/Encounter Pool")]
    public class EncounterPoolSO : ScriptableObject
    {
        [Header("일상 인카운터 풀")]
        public List<EncounterDefinitionSO> dailyEncounters = new();

        [Header("중요 인카운터 풀")]
        public List<EncounterDefinitionSO> majorEncounters = new();

        /// <summary>
        /// 가중치 기반으로 일상 인카운터를 랜덤 선택
        /// </summary>
        public EncounterDefinitionSO PickDaily()
        {
            return PickWeighted(dailyEncounters);
        }

        /// <summary>
        /// 가중치 기반으로 중요 인카운터를 랜덤 선택
        /// </summary>
        public EncounterDefinitionSO PickMajor()
        {
            return PickWeighted(majorEncounters);
        }

        private EncounterDefinitionSO PickWeighted(List<EncounterDefinitionSO> pool)
        {
            if (pool == null || pool.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var e in pool)
            {
                if (e != null) totalWeight += e.weight;
            }
            if (totalWeight <= 0f) return pool[0];

            float roll = Random.value * totalWeight;
            float cumulative = 0f;
            foreach (var e in pool)
            {
                if (e == null) continue;
                cumulative += e.weight;
                if (roll <= cumulative) return e;
            }

            return pool[pool.Count - 1];
        }
    }
}
