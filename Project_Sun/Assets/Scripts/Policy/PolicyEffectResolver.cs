using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Policy
{
    public class PolicyEffectResolver : MonoBehaviour
    {
        [SerializeField] private PolicyManager policyManager;

        /// <summary>
        /// 특정 효과 유형의 합산 수정자 값을 반환 (가산).
        /// 예: +0.15 + +0.20 = +0.35 (35% 증가)
        /// </summary>
        public float GetModifier(PolicyEffectType effectType)
        {
            if (policyManager == null) return 0f;

            float total = 0f;
            foreach (var node in policyManager.EnactedNodes)
            {
                foreach (var effect in node.Effects)
                {
                    if (effect.effectType == effectType)
                        total += effect.value;
                }
            }
            return total;
        }

        /// <summary>
        /// 특정 효과 유형 + 대상의 합산 수정자 값을 반환.
        /// </summary>
        public float GetModifier(PolicyEffectType effectType, string target)
        {
            if (policyManager == null) return 0f;

            float total = 0f;
            foreach (var node in policyManager.EnactedNodes)
            {
                foreach (var effect in node.Effects)
                {
                    if (effect.effectType == effectType &&
                        (string.IsNullOrEmpty(effect.target) || effect.target == target))
                        total += effect.value;
                }
            }
            return total;
        }

        /// <summary>
        /// 기본값에 정책 수정자를 적용한 최종 값 반환.
        /// 퍼센트 수정자: baseValue * (1 + modifier)
        /// 절대값 수정자: baseValue + modifier
        /// </summary>
        public float ApplyModifier(float baseValue, PolicyEffectType effectType)
        {
            if (policyManager == null) return baseValue;

            float percentMod = 0f;
            float flatMod = 0f;

            foreach (var node in policyManager.EnactedNodes)
            {
                foreach (var effect in node.Effects)
                {
                    if (effect.effectType != effectType) continue;

                    if (effect.isPercentage)
                        percentMod += effect.value;
                    else
                        flatMod += effect.value;
                }
            }

            return baseValue * (1f + percentMod) + flatMod;
        }

        // ── 자원 시스템 API ──

        public float GetResourceProductionModifier(Resource.ResourceType resourceType)
        {
            float allMod = GetModifier(PolicyEffectType.AllProductionMod);
            float specificMod = resourceType switch
            {
                Resource.ResourceType.Basic => GetModifier(PolicyEffectType.BasicProductionMod),
                Resource.ResourceType.Advanced => GetModifier(PolicyEffectType.AdvancedProductionMod),
                Resource.ResourceType.Defense => GetModifier(PolicyEffectType.DefenseProductionMod),
                _ => 0f
            };
            return allMod + specificMod;
        }

        public float GetBuildCostModifier()
        {
            return GetModifier(PolicyEffectType.BuildCostMod);
        }

        // ── 인력 시스템 API ──

        public float GetWorkerEfficiencyModifier()
        {
            return GetModifier(PolicyEffectType.WorkerEfficiencyMod);
        }

        public float GetHealingSpeedModifier()
        {
            return GetModifier(PolicyEffectType.HealingSpeedMod);
        }

        // ── 전투 시스템 API ──

        public float GetTowerDamageModifier()
        {
            return GetModifier(PolicyEffectType.TowerDamageMod);
        }

        public float GetTowerRangeModifier()
        {
            return GetModifier(PolicyEffectType.TowerRangeMod);
        }

        public float GetWallHPModifier()
        {
            return GetModifier(PolicyEffectType.WallHPMod);
        }

        public float GetDefenseResourceCostModifier()
        {
            return GetModifier(PolicyEffectType.DefenseResourceCostMod);
        }

        // ── 탐사 시스템 API ──

        public float GetExplorationSpeedModifier()
        {
            return GetModifier(PolicyEffectType.ExplorationSpeedMod);
        }

        public float GetExplorationRewardModifier()
        {
            return GetModifier(PolicyEffectType.ExplorationRewardMod);
        }

        // ── 인카운터 시스템 API ──

        public float GetEncounterChanceModifier()
        {
            return GetModifier(PolicyEffectType.EncounterChanceMod);
        }

        // ── 희망/불만 API ──

        public int GetPerTurnHopeDelta()
        {
            return Mathf.RoundToInt(GetModifier(PolicyEffectType.HopePerTurn));
        }

        public int GetPerTurnDiscontentDelta()
        {
            return Mathf.RoundToInt(GetModifier(PolicyEffectType.DiscontentPerTurn));
        }

        /// <summary>
        /// 활성화된 모든 정책 효과를 수집하여 반환 (디버그/UI용)
        /// </summary>
        public List<PolicyEffect> GetAllActiveEffects()
        {
            var effects = new List<PolicyEffect>();
            if (policyManager == null) return effects;

            foreach (var node in policyManager.EnactedNodes)
            {
                effects.AddRange(node.Effects);
            }
            return effects;
        }
    }
}
