using System;
using UnityEngine;

namespace ProjectSun.V2.Data
{
    [Serializable]
    public class ResourceState
    {
        public int basicAmount = 60;
        public int basicCap = 100;

        public int advancedAmount = 20;
        public int advancedCap = 40;

        public int relicAmount = 0;

        public void Add(int basic, int advanced = 0, int relic = 0)
        {
            basicAmount = Math.Min(basicAmount + basic, basicCap);
            advancedAmount = Math.Min(advancedAmount + advanced, advancedCap);
            relicAmount += relic;
        }

        public bool CanAfford(int basic, int advanced = 0, int relic = 0)
        {
            return basicAmount >= basic
                && advancedAmount >= advanced
                && relicAmount >= relic;
        }

        // I-06: 잔고 부족 시 false 반환 + 경고 — 음수 방지
        public bool Spend(int basic, int advanced = 0, int relic = 0)
        {
            if (!CanAfford(basic, advanced, relic))
            {
                Debug.LogWarning(
                    $"[ResourceState] Spend rejected: insufficient resources. " +
                    $"Need ({basic},{advanced},{relic}), have ({basicAmount},{advancedAmount},{relicAmount})");
                return false;
            }
            basicAmount -= basic;
            advancedAmount -= advanced;
            relicAmount -= relic;
            return true;
        }
    }
}
