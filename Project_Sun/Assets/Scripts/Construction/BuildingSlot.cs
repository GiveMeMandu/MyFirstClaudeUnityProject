using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Construction
{
    public class BuildingSlot : MonoBehaviour
    {
        [Header("슬롯 설정")]
        [SerializeField] private BuildingData assignedBuilding;
        [SerializeField] private BuildingSlotState state = BuildingSlotState.Hidden;
        [SerializeField] private List<BuildingSlot> adjacentSlots = new();

        [Header("런타임 상태")]
        [SerializeField] private int constructionProgress;
        [SerializeField] private int repairProgress;
        [SerializeField] private int assignedWorkers;
        [SerializeField] private int currentTier = 1;
        [SerializeField] private UpgradeBranchData selectedBranch;

        private BuildingHealth health;
        private BuildingData currentBuildingData;

        public BuildingData AssignedBuilding => assignedBuilding;
        public BuildingData CurrentBuildingData => currentBuildingData ?? assignedBuilding;
        public BuildingSlotState State => state;
        public int AssignedWorkers => assignedWorkers;
        public int CurrentTier => currentTier;
        public UpgradeBranchData SelectedBranch => selectedBranch;
        public BuildingHealth Health => health;
        public bool IsTargetable => state == BuildingSlotState.Active || state == BuildingSlotState.Damaged;

        public event Action<BuildingSlot, BuildingSlotState> OnStateChanged;
        public event Action<BuildingSlot> OnConstructionCompleted;
        public event Action<BuildingSlot> OnUpgradeCompleted;
        public event Action<BuildingSlot> OnBuildingDestroyed;

        private void Awake()
        {
            health = GetComponent<BuildingHealth>();
            if (health != null)
            {
                health.OnDestroyed += HandleDestroyed;
                health.OnRepaired += HandleRepaired;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDestroyed -= HandleDestroyed;
                health.OnRepaired -= HandleRepaired;
            }
        }

        /// <summary>
        /// 슬롯을 보이게 만듦 (방벽 확장 or 선행 건물 건설 시)
        /// </summary>
        public void Reveal()
        {
            if (state != BuildingSlotState.Hidden) return;
            SetState(BuildingSlotState.Empty);
        }

        /// <summary>
        /// 건설 시작. 자원은 BuildingManager에서 선차감 후 호출.
        /// </summary>
        public bool StartConstruction()
        {
            if (state != BuildingSlotState.Empty) return false;

            currentBuildingData = assignedBuilding;
            constructionProgress = 0;
            SetState(BuildingSlotState.Constructing);
            return true;
        }

        /// <summary>
        /// 건설/업그레이드 취소. BuildingManager에서 자원 반환 처리.
        /// </summary>
        public void CancelConstruction()
        {
            if (state == BuildingSlotState.Constructing)
            {
                constructionProgress = 0;
                assignedWorkers = 0;
                SetState(BuildingSlotState.Empty);
            }
            else if (state == BuildingSlotState.Upgrading)
            {
                constructionProgress = 0;
                assignedWorkers = 0;
                selectedBranch = null;
                SetState(BuildingSlotState.Active);
            }
        }

        /// <summary>
        /// 업그레이드 분기 선택. 비가역. 자원은 BuildingManager에서 선차감.
        /// </summary>
        public bool StartUpgrade(UpgradeBranchData branch)
        {
            if (state != BuildingSlotState.Active) return false;
            if (branch == null || branch.upgradedBuilding == null) return false;

            selectedBranch = branch;
            constructionProgress = 0;
            SetState(BuildingSlotState.Upgrading);
            return true;
        }

        /// <summary>
        /// 수리 명령 (파괴 상태에서만 가능)
        /// </summary>
        public bool StartRepair()
        {
            if (state != BuildingSlotState.Destroyed) return false;

            repairProgress = 0;
            SetState(BuildingSlotState.Repairing);
            return true;
        }

        /// <summary>
        /// 인력 배치 수 설정. 매 턴 재배치 가능.
        /// </summary>
        public void SetWorkers(int count)
        {
            int maxWorkers = CurrentBuildingData != null ? CurrentBuildingData.maxConstructionWorkers : 1;
            assignedWorkers = Mathf.Clamp(count, 0, maxWorkers);
        }

        /// <summary>
        /// 턴 종료 시 호출. 건설/업그레이드/수리 진행도 처리.
        /// </summary>
        public void ProcessTurn()
        {
            switch (state)
            {
                case BuildingSlotState.Constructing:
                    ProcessConstruction();
                    break;
                case BuildingSlotState.Upgrading:
                    ProcessUpgrade();
                    break;
                case BuildingSlotState.Repairing:
                    ProcessRepair();
                    break;
                case BuildingSlotState.Damaged:
                    health?.ApplyAutoRepair();
                    if (health != null && health.IsFullHealth)
                    {
                        SetState(BuildingSlotState.Active);
                    }
                    break;
            }
        }

        private void ProcessConstruction()
        {
            if (assignedWorkers <= 0) return;

            constructionProgress += assignedWorkers;

            if (constructionProgress >= CurrentBuildingData.constructionTurns)
            {
                CompleteConstruction();
            }
        }

        private void ProcessUpgrade()
        {
            if (assignedWorkers <= 0) return;

            constructionProgress += assignedWorkers;

            if (selectedBranch != null && constructionProgress >= selectedBranch.upgradeTurns)
            {
                CompleteUpgrade();
            }
        }

        private void ProcessRepair()
        {
            if (assignedWorkers <= 0) return;

            repairProgress += assignedWorkers;
            int requiredTurns = CurrentBuildingData != null ? CurrentBuildingData.repairTurns : 2;

            if (repairProgress >= requiredTurns)
            {
                health?.FullRestore();
                SetState(BuildingSlotState.Active);
            }
        }

        private void CompleteConstruction()
        {
            assignedWorkers = 0;
            constructionProgress = 0;

            if (health != null)
            {
                health.Initialize(CurrentBuildingData.maxHP, CurrentBuildingData.autoRepairRate);
            }

            SetState(BuildingSlotState.Active);
            OnConstructionCompleted?.Invoke(this);
            RevealAdjacentSlots();
        }

        private void CompleteUpgrade()
        {
            if (selectedBranch?.upgradedBuilding != null)
            {
                currentBuildingData = selectedBranch.upgradedBuilding;
                currentTier++;
            }

            assignedWorkers = 0;
            constructionProgress = 0;

            if (health != null)
            {
                health.Initialize(CurrentBuildingData.maxHP, CurrentBuildingData.autoRepairRate);
            }

            SetState(BuildingSlotState.Active);
            OnUpgradeCompleted?.Invoke(this);
        }

        private void HandleDestroyed()
        {
            assignedWorkers = 0;
            SetState(BuildingSlotState.Destroyed);
            OnBuildingDestroyed?.Invoke(this);
        }

        private void HandleRepaired()
        {
            if (state == BuildingSlotState.Damaged)
            {
                SetState(BuildingSlotState.Active);
            }
        }

        private void RevealAdjacentSlots()
        {
            foreach (var slot in adjacentSlots)
            {
                if (slot != null && slot.State == BuildingSlotState.Hidden)
                {
                    slot.Reveal();
                }
            }
        }

        private void SetState(BuildingSlotState newState)
        {
            if (state == newState) return;
            state = newState;
            OnStateChanged?.Invoke(this, newState);
        }

        /// <summary>
        /// 방어 타워의 이번 턴 운영 자원 소모량 계산
        /// </summary>
        public float GetDefenseResourceCost()
        {
            if (CurrentBuildingData == null) return 0f;
            if (CurrentBuildingData.category != BuildingCategory.Defense) return 0f;
            if (state != BuildingSlotState.Active && state != BuildingSlotState.Damaged) return 0f;

            return assignedWorkers * CurrentBuildingData.defenseResourceCostPerSlot;
        }
    }
}
