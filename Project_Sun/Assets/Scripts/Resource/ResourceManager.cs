using System;
using System.Collections.Generic;
using ProjectSun.Construction;
using ProjectSun.Workforce;
using UnityEngine;

namespace ProjectSun.Resource
{
    /// <summary>
    /// 자원 풀 관리. 3종 자원(기초/고급/방어)의 생산, 소비, 검증을 처리.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        [Header("초기 자원")]
        [SerializeField] private int basicResource = 100;
        [SerializeField] private int advancedResource = 30;
        [SerializeField] private int defenseResource = 20;

        [Header("연동")]
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private WorkforceManager workforceManager;

        public int BasicResource => basicResource;
        public int AdvancedResource => advancedResource;
        public int DefenseResource => defenseResource;

        public event Action OnResourceChanged;

        /// <summary>
        /// 자원 보유량 조회
        /// </summary>
        public int GetResource(ResourceType type)
        {
            return type switch
            {
                ResourceType.Basic => basicResource,
                ResourceType.Advanced => advancedResource,
                ResourceType.Defense => defenseResource,
                _ => 0
            };
        }

        /// <summary>
        /// 자원 추가 (생산, 보상 등)
        /// </summary>
        public void AddResource(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Basic: basicResource += amount; break;
                case ResourceType.Advanced: advancedResource += amount; break;
                case ResourceType.Defense: defenseResource += amount; break;
            }
            OnResourceChanged?.Invoke();
        }

        /// <summary>
        /// 자원 차감. 부족하면 false 반환하고 차감하지 않음.
        /// </summary>
        public bool SpendResource(ResourceType type, int amount)
        {
            int current = GetResource(type);
            if (current < amount) return false;

            switch (type)
            {
                case ResourceType.Basic: basicResource -= amount; break;
                case ResourceType.Advanced: advancedResource -= amount; break;
                case ResourceType.Defense: defenseResource -= amount; break;
            }
            ClampResources();
            OnResourceChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// ResourceCost 리스트로 비용 검증
        /// </summary>
        public bool CanAfford(List<ResourceCost> costs)
        {
            foreach (var cost in costs)
            {
                var type = ParseResourceType(cost.resourceId);
                if (GetResource(type) < cost.amount) return false;
            }
            return true;
        }

        /// <summary>
        /// ResourceCost 리스트로 비용 차감
        /// </summary>
        public bool SpendCosts(List<ResourceCost> costs)
        {
            if (!CanAfford(costs)) return false;

            foreach (var cost in costs)
            {
                var type = ParseResourceType(cost.resourceId);
                SpendResource(type, cost.amount);
            }
            return true;
        }

        /// <summary>
        /// ResourceCost 리스트로 자원 환불
        /// </summary>
        public void RefundCosts(List<ResourceCost> costs)
        {
            foreach (var cost in costs)
            {
                var type = ParseResourceType(cost.resourceId);
                AddResource(type, cost.amount);
            }
        }

        /// <summary>
        /// 턴 종료 시 호출: 자원 건물의 인력 배치에 따라 자원 생산.
        /// 생산된 자원 요약을 반환.
        /// </summary>
        public Dictionary<ResourceType, int> ProcessProduction()
        {
            var produced = new Dictionary<ResourceType, int>
            {
                { ResourceType.Basic, 0 },
                { ResourceType.Advanced, 0 },
                { ResourceType.Defense, 0 }
            };

            if (buildingManager == null || workforceManager == null) return produced;

            foreach (var slot in buildingManager.AllSlots)
            {
                if (slot.State != BuildingSlotState.Active && slot.State != BuildingSlotState.Damaged)
                    continue;

                var data = slot.CurrentBuildingData;
                if (data == null) continue;
                if (data.category != BuildingCategory.Resource) continue;

                var workerSlots = workforceManager.GetBuildingSlots(slot);
                if (workerSlots == null) continue;

                foreach (var ws in workerSlots)
                {
                    if (ws.AssignedWorkers <= 0) continue;

                    int amount = Mathf.RoundToInt(ws.AssignedWorkers * ws.EffectPerWorker);
                    var resType = SlotTypeToResourceType(ws.SlotType);
                    AddResource(resType, amount);
                    produced[resType] += amount;
                }
            }

            return produced;
        }

        /// <summary>
        /// 방어 자원 소모 예정량 계산 (전투 시작 전 검증용)
        /// </summary>
        public int CalculateDefenseResourceCost()
        {
            if (buildingManager == null || workforceManager == null) return 0;

            int total = 0;
            foreach (var slot in buildingManager.AllSlots)
            {
                var data = slot.CurrentBuildingData;
                if (data == null || data.category != BuildingCategory.Defense) continue;

                var workerSlots = workforceManager.GetBuildingSlots(slot);
                if (workerSlots == null) continue;

                foreach (var ws in workerSlots)
                {
                    total += ws.AssignedWorkers; // 슬롯당 소모량 1 기본
                }
            }
            return total;
        }

        /// <summary>
        /// 방어 자원이 충분한지 검증
        /// </summary>
        public bool HasEnoughDefenseResource()
        {
            return defenseResource >= CalculateDefenseResourceCost();
        }

        /// <summary>
        /// 전투 시작 시 방어 자원 차감
        /// </summary>
        public void DeductDefenseResource()
        {
            int cost = CalculateDefenseResourceCost();
            defenseResource = Mathf.Max(0, defenseResource - cost);
            OnResourceChanged?.Invoke();
        }

        /// <summary>
        /// 전투 보상 자원 지급
        /// </summary>
        public void GrantBattleReward(int basic, int advanced)
        {
            if (basic > 0) AddResource(ResourceType.Basic, basic);
            if (advanced > 0) AddResource(ResourceType.Advanced, advanced);
        }

        private void ClampResources()
        {
            basicResource = Mathf.Max(0, basicResource);
            advancedResource = Mathf.Max(0, advancedResource);
            defenseResource = Mathf.Max(0, defenseResource);
        }

        private ResourceType ParseResourceType(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return ResourceType.Basic;
            return resourceId.ToLower() switch
            {
                "basic" => ResourceType.Basic,
                "advanced" => ResourceType.Advanced,
                "defense" => ResourceType.Defense,
                _ => ResourceType.Basic
            };
        }

        private ResourceType SlotTypeToResourceType(WorkerSlotType slotType)
        {
            // 자원 건물의 슬롯 타입 → 생산하는 자원 타입 매핑
            // Production 슬롯은 건물에 따라 다르지만, PoC에서는 기본적으로 Basic 생산
            return slotType switch
            {
                WorkerSlotType.Production => ResourceType.Basic,
                _ => ResourceType.Basic
            };
        }
    }
}
