using System;
using UnityEngine;

namespace ProjectSun.V2.Data
{
    [Serializable]
    public struct ResourceCostV2
    {
        [Min(0)] public int basic;
        [Min(0)] public int advanced;
        [Min(0)] public int relic;
    }
}
