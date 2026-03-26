using System;
using UnityEngine;

namespace ProjectSun.TechTree
{
    [Serializable]
    public struct TechNodeEffect
    {
        public TechEffectType effectType;

        [Tooltip("효과 대상 식별자 (건물 이름, 슬롯 ID 등)")]
        public string targetId;

        [Tooltip("효과 수치 (보너스 %, 슬롯 수 등)")]
        public float value;

        [Tooltip("효과 설명 (UI 표시용)")]
        public string description;
    }
}
