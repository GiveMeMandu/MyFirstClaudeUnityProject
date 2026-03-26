using System;
using System.Collections.Generic;
using ProjectSun.Construction;
using ProjectSun.Resource;
using ProjectSun.Workforce;
using UnityEngine;

namespace ProjectSun.Encounter
{
    /// <summary>
    /// 인카운터 발생, 선택 처리, 효과 적용을 총괄.
    /// Pity 시스템으로 빈도 조절.
    /// </summary>
    public class EncounterManager : MonoBehaviour
    {
        [Header("인카운터 풀")]
        [SerializeField] private EncounterPoolSO encounterPool;

        [Header("Pity 설정")]
        [SerializeField] private float baseEncounterChance = 0.4f;
        [SerializeField] private float pityIncrement = 0.15f;
        [SerializeField] private float pityDecrement = 0.1f;
        [SerializeField] private float minChance = 0.1f;
        [SerializeField] private float maxChance = 0.9f;

        [Header("연동")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private WorkforceManager workforceManager;
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private BuffManager buffManager;

        // Pity 추적
        private int consecutiveMisses;
        private int consecutiveHits;

        // 현재 표시 중인 인카운터
        private EncounterDefinitionSO currentEncounter;
        private bool waitingForChoice;

        public bool IsWaitingForChoice => waitingForChoice;
        public EncounterDefinitionSO CurrentEncounter => currentEncounter;

        public event Action<EncounterDefinitionSO> OnEncounterStarted;
        public event Action<string> OnEffectApplied;
        public event Action OnEncounterEnded;

        /// <summary>
        /// 일상 인카운터 발생 시도 (낮 시작 시 호출).
        /// 확률 판정 후 발생하면 true 반환.
        /// </summary>
        public bool TryTriggerDailyEncounter()
        {
            if (encounterPool == null) return false;

            float chance = CalculateChance();
            float roll = UnityEngine.Random.value;

            if (roll < chance)
            {
                consecutiveHits++;
                consecutiveMisses = 0;

                var encounter = encounterPool.PickDaily();
                if (encounter != null)
                {
                    ShowEncounter(encounter);
                    return true;
                }
            }

            consecutiveMisses++;
            consecutiveHits = 0;
            return false;
        }

        /// <summary>
        /// 중요 인카운터 강제 발생 (밤 페이즈에서 호출).
        /// </summary>
        public bool TriggerMajorEncounter()
        {
            if (encounterPool == null) return false;

            var encounter = encounterPool.PickMajor();
            if (encounter != null)
            {
                ShowEncounter(encounter);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 특정 인카운터 강제 표시
        /// </summary>
        public void ShowEncounter(EncounterDefinitionSO encounter)
        {
            currentEncounter = encounter;
            waitingForChoice = true;
            OnEncounterStarted?.Invoke(encounter);
        }

        /// <summary>
        /// 선택지 선택 처리
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            if (currentEncounter == null || !waitingForChoice) return;
            if (choiceIndex < 0 || choiceIndex >= currentEncounter.choices.Count) return;

            var choice = currentEncounter.choices[choiceIndex];

            // 비용 차감
            if (!string.IsNullOrEmpty(choice.costResourceId) && choice.costAmount > 0)
            {
                var resType = ParseResourceType(choice.costResourceId);
                if (resourceManager != null && !resourceManager.SpendResource(resType, choice.costAmount))
                    return; // 비용 부족
            }

            // 효과 적용
            ApplyEffects(choice.effects);

            waitingForChoice = false;
            currentEncounter = null;
            OnEncounterEnded?.Invoke();
        }

        /// <summary>
        /// 선택지가 선택 가능한지 확인
        /// </summary>
        public bool IsChoiceAvailable(EncounterChoice choice)
        {
            // 비용 체크
            if (!string.IsNullOrEmpty(choice.costResourceId) && choice.costAmount > 0)
            {
                if (resourceManager == null) return false;
                var resType = ParseResourceType(choice.costResourceId);
                if (resourceManager.GetResource(resType) < choice.costAmount) return false;
            }

            // 건물 조건 체크
            if (!string.IsNullOrEmpty(choice.requiredBuildingName))
            {
                if (buildingManager == null) return false;
                bool found = false;
                foreach (var slot in buildingManager.AllSlots)
                {
                    if (slot.State == BuildingSlotState.Active &&
                        slot.CurrentBuildingData != null &&
                        slot.CurrentBuildingData.buildingName == choice.requiredBuildingName)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            return true;
        }

        private float CalculateChance()
        {
            float chance = baseEncounterChance;
            chance += consecutiveMisses * pityIncrement;
            chance -= consecutiveHits * pityDecrement;
            return Mathf.Clamp(chance, minChance, maxChance);
        }

        private void ApplyEffects(List<ChoiceEffect> effects)
        {
            if (effects == null) return;

            foreach (var effect in effects)
            {
                switch (effect.effectType)
                {
                    case EffectType.ResourceChange:
                        if (resourceManager != null)
                        {
                            var resType = ParseResourceType(effect.resourceId);
                            if (effect.resourceAmount >= 0)
                                resourceManager.AddResource(resType, effect.resourceAmount);
                            else
                                resourceManager.SpendResource(resType, -effect.resourceAmount);
                            OnEffectApplied?.Invoke($"{resType} {(effect.resourceAmount >= 0 ? "+" : "")}{effect.resourceAmount}");
                        }
                        break;

                    case EffectType.WorkerChange:
                        if (workforceManager != null && effect.workerAmount > 0)
                        {
                            workforceManager.AddWorkers(effect.workerAmount);
                            OnEffectApplied?.Invoke($"인력 +{effect.workerAmount}");
                        }
                        break;

                    case EffectType.WorkerInjury:
                        if (workforceManager != null && effect.workerAmount > 0)
                        {
                            workforceManager.InjureRandomWorkers(effect.workerAmount);
                            OnEffectApplied?.Invoke($"인력 {effect.workerAmount}명 부상");
                        }
                        break;

                    case EffectType.Buff:
                        if (buffManager != null)
                        {
                            buffManager.AddBuff(effect.buffType, effect.buffValue, effect.buffDuration,
                                currentEncounter != null ? currentEncounter.encounterName : "Event");
                            OnEffectApplied?.Invoke($"{effect.buffType} +{effect.buffValue * 100:F0}% ({effect.buffDuration}턴)");
                        }
                        break;

                    case EffectType.TriggerBattle:
                        OnEffectApplied?.Invoke("전투 발생!");
                        break;
                }
            }
        }

        private Resource.ResourceType ParseResourceType(string id)
        {
            if (string.IsNullOrEmpty(id)) return Resource.ResourceType.Basic;
            return id.ToLower() switch
            {
                "basic" => Resource.ResourceType.Basic,
                "advanced" => Resource.ResourceType.Advanced,
                "defense" => Resource.ResourceType.Defense,
                _ => Resource.ResourceType.Basic
            };
        }
    }
}
