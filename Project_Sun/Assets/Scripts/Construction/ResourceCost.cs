using System;
using UnityEngine;

namespace ProjectSun.Construction
{
    [Serializable]
    public struct ResourceCost
    {
        [Tooltip("자원 타입 식별자 (추후 ResourceType enum으로 교체)")]
        public string resourceId;

        [Tooltip("필요 수량")]
        public int amount;
    }
}
