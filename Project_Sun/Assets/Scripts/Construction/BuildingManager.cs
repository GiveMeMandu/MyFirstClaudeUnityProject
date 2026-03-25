using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectSun.Construction
{
    public class BuildingManager : MonoBehaviour
    {
        [Header("슬롯 관리")]
        [SerializeField] private List<BuildingSlot> allSlots = new();

        [Header("방어 운영 자원")]
        [SerializeField] private string defenseResourceId = "ammo";
        [SerializeField] private float currentDefenseResource = 100f;

        public IReadOnlyList<BuildingSlot> AllSlots => allSlots;
        public float CurrentDefenseResource => currentDefenseResource;

        public event Action<BuildingSlot> OnConstructionStarted;
        public event Action<BuildingSlot> OnConstructionCompleted;
        public event Action<BuildingSlot> OnUpgradeStarted;
        public event Action<BuildingSlot> OnUpgradeCompleted;
        public event Action<BuildingSlot> OnBuildingDestroyed;
        public event Action OnHeadquartersDestroyed;

        private void Awake()
        {
            foreach (var slot in allSlots)
            {
                slot.OnConstructionCompleted += HandleConstructionCompleted;
                slot.OnUpgradeCompleted += HandleUpgradeCompleted;
                slot.OnBuildingDestroyed += HandleBuildingDestroyed;
            }
        }

        private void OnDestroy()
        {
            foreach (var slot in allSlots)
            {
                slot.OnConstructionCompleted -= HandleConstructionCompleted;
                slot.OnUpgradeCompleted -= HandleUpgradeCompleted;
                slot.OnBuildingDestroyed -= HandleBuildingDestroyed;
            }
        }

        /// <summary>
        /// 건설 명령. 자원 선차감 후 건설 시작.
        /// </summary>
        public bool RequestConstruction(BuildingSlot slot)
        {
            if (slot == null || slot.State != BuildingSlotState.Empty) return false;
            if (slot.AssignedBuilding == null) return false;

            if (!CanAfford(slot.AssignedBuilding.constructionCost)) return false;

            SpendResources(slot.AssignedBuilding.constructionCost);

            if (slot.StartConstruction())
            {
                OnConstructionStarted?.Invoke(slot);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 건설/업그레이드 취소. 자원 전액 반환.
        /// </summary>
        public void CancelConstruction(BuildingSlot slot)
        {
            if (slot == null) return;

            if (slot.State == BuildingSlotState.Constructing)
            {
                RefundResources(slot.CurrentBuildingData.constructionCost);
                slot.CancelConstruction();
            }
            else if (slot.State == BuildingSlotState.Upgrading && slot.SelectedBranch != null)
            {
                RefundResources(slot.SelectedBranch.upgradeCost);
                slot.CancelConstruction();
            }
        }

        /// <summary>
        /// 업그레이드 분기 선택. 자원 선차감 후 업그레이드 시작.
        /// </summary>
        public bool RequestUpgrade(BuildingSlot slot, UpgradeBranchData branch)
        {
            if (slot == null || slot.State != BuildingSlotState.Active) return false;
            if (branch == null) return false;
            if (branch.requiresResearch) return false; // 연구 해금 안 된 분기

            if (!CanAfford(branch.upgradeCost)) return false;

            SpendResources(branch.upgradeCost);

            if (slot.StartUpgrade(branch))
            {
                OnUpgradeStarted?.Invoke(slot);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 수리 명령 (파괴 상태에서만)
        /// </summary>
        public bool RequestRepair(BuildingSlot slot)
        {
            if (slot == null || slot.State != BuildingSlotState.Destroyed) return false;
            return slot.StartRepair();
        }

        /// <summary>
        /// 방어 타워에 인력 추가 배치 가능 여부 확인.
        /// 현재 배치된 모든 방어 타워 소모량 + 추가 슬롯 소모량 <= 보유량
        /// </summary>
        public bool CanAssignDefenseWorker(BuildingSlot slot)
        {
            if (slot == null) return false;
            if (slot.CurrentBuildingData == null) return false;
            if (slot.CurrentBuildingData.category != BuildingCategory.Defense) return true;

            float totalCost = GetTotalDefenseResourceCost();
            float additionalCost = slot.CurrentBuildingData.defenseResourceCostPerSlot;

            return totalCost + additionalCost <= currentDefenseResource;
        }

        /// <summary>
        /// 현재 모든 방어 타워의 총 운영 자원 소모량
        /// </summary>
        public float GetTotalDefenseResourceCost()
        {
            float total = 0f;
            foreach (var slot in allSlots)
            {
                total += slot.GetDefenseResourceCost();
            }
            return total;
        }

        /// <summary>
        /// 다음 낮 시작 시 호출. 모든 슬롯의 건설/수리 진행도 처리.
        /// 자동 회복은 포함하지 않음 (ProcessAutoRepair로 분리).
        /// </summary>
        public void ProcessTurn()
        {
            foreach (var slot in allSlots)
            {
                slot.ProcessTurn();
            }
        }

        /// <summary>
        /// 낮 종료 시 호출. 손상 건물의 자동 회복만 처리.
        /// </summary>
        public void ProcessAutoRepair()
        {
            foreach (var slot in allSlots)
            {
                slot.ProcessAutoRepair();
            }
        }

        /// <summary>
        /// 방어 운영 자원 설정 (자원 시스템에서 호출)
        /// </summary>
        public void SetDefenseResource(float amount)
        {
            currentDefenseResource = Mathf.Max(0f, amount);
        }

        public List<BuildingSlot> GetSlotsByState(BuildingSlotState state)
        {
            return allSlots.Where(s => s.State == state).ToList();
        }

        public List<BuildingSlot> GetSlotsByCategory(BuildingCategory category)
        {
            return allSlots.Where(s =>
                s.CurrentBuildingData != null &&
                s.CurrentBuildingData.category == category
            ).ToList();
        }

        private void HandleConstructionCompleted(BuildingSlot slot)
        {
            OnConstructionCompleted?.Invoke(slot);
        }

        private void HandleUpgradeCompleted(BuildingSlot slot)
        {
            OnUpgradeCompleted?.Invoke(slot);
        }

        private void HandleBuildingDestroyed(BuildingSlot slot)
        {
            OnBuildingDestroyed?.Invoke(slot);

            if (slot.CurrentBuildingData != null && slot.CurrentBuildingData.isHeadquarters)
            {
                OnHeadquartersDestroyed?.Invoke();
            }
        }

        private bool CanAfford(List<ResourceCost> costs)
        {
            // TODO: 자원 시스템 연동 후 실제 보유량 체크
            return true;
        }

        private void SpendResources(List<ResourceCost> costs)
        {
            // TODO: 자원 시스템 연동 후 실제 차감
        }

        private void RefundResources(List<ResourceCost> costs)
        {
            // TODO: 자원 시스템 연동 후 실제 반환
        }
    }
}
