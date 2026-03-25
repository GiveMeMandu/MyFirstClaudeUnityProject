using System;
using System.Collections.Generic;
using ProjectSun.Construction;
using UnityEngine;

namespace ProjectSun.Workforce
{
    /// <summary>
    /// 전체 인력 풀 관리. 건물 슬롯에 인력 배치/회수, 부상/치료 처리.
    /// </summary>
    public class WorkforceManager : MonoBehaviour
    {
        [Header("인력 풀")]
        [SerializeField] private int totalWorkers = 4;

        [Header("부상/치료 설정")]
        [SerializeField] private int naturalHealTurns = 4;
        [SerializeField] private int healingBoostTurns = 2;

        [Header("연동")]
        [SerializeField] private BuildingManager buildingManager;

        // 부상자 목록 (남은 회복 턴 수)
        private List<int> injuredWorkerHealTimers = new();

        // 건물별 런타임 슬롯 매핑 (BuildingSlot → 슬롯 리스트)
        private Dictionary<BuildingSlot, List<WorkerSlotRuntime>> buildingSlots = new();

        // 본부 치료 슬롯
        private WorkerSlotRuntime healingSlot;

        public int TotalWorkers => totalWorkers;
        public int InjuredCount => injuredWorkerHealTimers.Count;
        public int HealthyCount => totalWorkers - InjuredCount;
        public int AssignedCount => GetTotalAssigned();
        public int IdleCount => HealthyCount - AssignedCount;

        public event Action OnWorkforceChanged;

        private void Start()
        {
            InitializeSlots();
        }

        /// <summary>
        /// 인력 추가 (이벤트 보상 등)
        /// </summary>
        public void AddWorkers(int count)
        {
            totalWorkers += count;
            OnWorkforceChanged?.Invoke();
        }

        /// <summary>
        /// 건물 슬롯에 인력 1명 추가 배치
        /// </summary>
        public bool AssignWorker(BuildingSlot building, int slotIndex)
        {
            if (IdleCount <= 0) return false;
            if (!buildingSlots.TryGetValue(building, out var slots)) return false;
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;

            var slot = slots[slotIndex];
            if (!slot.CanAddWorker) return false;

            slot.AssignedWorkers++;
            SyncBuildingWorkers(building);
            OnWorkforceChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 건물 슬롯에서 인력 1명 회수
        /// </summary>
        public bool UnassignWorker(BuildingSlot building, int slotIndex)
        {
            if (!buildingSlots.TryGetValue(building, out var slots)) return false;
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;

            var slot = slots[slotIndex];
            if (slot.AssignedWorkers <= 0) return false;

            slot.AssignedWorkers--;
            SyncBuildingWorkers(building);
            OnWorkforceChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 치료 슬롯에 인력 1명 배치
        /// </summary>
        public bool AssignHealer()
        {
            if (IdleCount <= 0) return false;
            if (healingSlot == null) return false;
            if (!healingSlot.CanAddWorker) return false;

            healingSlot.AssignedWorkers++;
            OnWorkforceChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 치료 슬롯에서 인력 1명 회수
        /// </summary>
        public bool UnassignHealer()
        {
            if (healingSlot == null) return false;
            if (healingSlot.AssignedWorkers <= 0) return false;

            healingSlot.AssignedWorkers--;
            OnWorkforceChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 건물의 런타임 슬롯 목록 반환
        /// </summary>
        public List<WorkerSlotRuntime> GetBuildingSlots(BuildingSlot building)
        {
            return buildingSlots.TryGetValue(building, out var slots) ? slots : null;
        }

        public WorkerSlotRuntime HealingSlot => healingSlot;

        /// <summary>
        /// 인력 부상 처리 (건물 파괴 시 호출)
        /// </summary>
        public void InjureWorkersFromBuilding(BuildingSlot building)
        {
            if (!buildingSlots.TryGetValue(building, out var slots)) return;

            int injuredCount = 0;
            foreach (var slot in slots)
            {
                injuredCount += slot.AssignedWorkers;
                slot.AssignedWorkers = 0;
            }

            for (int i = 0; i < injuredCount; i++)
            {
                injuredWorkerHealTimers.Add(naturalHealTurns);
            }

            SyncBuildingWorkers(building);
            OnWorkforceChanged?.Invoke();
        }

        /// <summary>
        /// 이벤트에 의한 인력 부상 (방어/건설 중 건물 배치 인력 제외)
        /// </summary>
        public void InjureRandomWorkers(int count)
        {
            // 이벤트 부상 면제 대상: 방어 건물, 건설 중 건물에 배치된 인력
            // 유휴 인력 + 자원/연구/행정 건물에 배치된 인력 중에서 부상
            int vulnerable = GetVulnerableWorkerCount();
            int actualInjured = Mathf.Min(count, vulnerable);

            // 유휴 인력 우선 부상
            int fromIdle = Mathf.Min(actualInjured, IdleCount);
            for (int i = 0; i < fromIdle; i++)
            {
                injuredWorkerHealTimers.Add(naturalHealTurns);
            }

            int remaining = actualInjured - fromIdle;

            // 남은 수는 취약 건물에서 회수 후 부상
            if (remaining > 0)
            {
                foreach (var kvp in buildingSlots)
                {
                    if (remaining <= 0) break;
                    var building = kvp.Key;
                    if (IsExemptFromEventInjury(building)) continue;

                    foreach (var slot in kvp.Value)
                    {
                        while (remaining > 0 && slot.AssignedWorkers > 0)
                        {
                            slot.AssignedWorkers--;
                            injuredWorkerHealTimers.Add(naturalHealTurns);
                            remaining--;
                        }
                    }

                    SyncBuildingWorkers(building);
                }
            }

            OnWorkforceChanged?.Invoke();
        }

        /// <summary>
        /// 턴 시작 시 호출: 부상 회복 처리
        /// </summary>
        public void ProcessHealingTurn()
        {
            bool hasHealer = healingSlot != null && healingSlot.AssignedWorkers > 0;
            int boost = hasHealer ? healingBoostTurns : 0;

            for (int i = injuredWorkerHealTimers.Count - 1; i >= 0; i--)
            {
                injuredWorkerHealTimers[i] -= (1 + boost);
                if (injuredWorkerHealTimers[i] <= 0)
                {
                    injuredWorkerHealTimers.RemoveAt(i);
                }
            }

            OnWorkforceChanged?.Invoke();
        }

        /// <summary>
        /// 모든 건물의 인력 배치 초기화 (유휴로 복귀)
        /// </summary>
        public void UnassignAll()
        {
            foreach (var kvp in buildingSlots)
            {
                foreach (var slot in kvp.Value)
                    slot.AssignedWorkers = 0;
                SyncBuildingWorkers(kvp.Key);
            }

            if (healingSlot != null) healingSlot.AssignedWorkers = 0;
            OnWorkforceChanged?.Invoke();
        }

        /// <summary>
        /// 건물에 총 배치된 인력 수
        /// </summary>
        public int GetBuildingTotalWorkers(BuildingSlot building)
        {
            if (!buildingSlots.TryGetValue(building, out var slots)) return 0;
            int total = 0;
            foreach (var s in slots) total += s.AssignedWorkers;
            return total;
        }

        /// <summary>
        /// 방어 건물 활성 여부 (인력 ≥ 1)
        /// </summary>
        public bool IsTowerActive(BuildingSlot building)
        {
            return GetBuildingTotalWorkers(building) > 0;
        }

        /// <summary>
        /// 미배치 인력 있는지 (턴 종료 경고용)
        /// </summary>
        public bool HasIdleWorkers => IdleCount > 0;

        private void InitializeSlots()
        {
            buildingSlots.Clear();

            // 본부 치료 슬롯 생성
            healingSlot = new WorkerSlotRuntime(new WorkerSlotDefinition
            {
                slotName = "치료 지원",
                slotType = WorkerSlotType.Healing,
                maxWorkers = 2,
                effectPerWorker = 1f
            });

            if (buildingManager == null) return;

            foreach (var slot in buildingManager.AllSlots)
            {
                var data = slot.CurrentBuildingData;
                if (data == null) continue;

                var runtimeSlots = new List<WorkerSlotRuntime>();

                if (data.workerSlotConfig != null)
                {
                    foreach (var def in data.workerSlotConfig.slots)
                    {
                        runtimeSlots.Add(new WorkerSlotRuntime(def));
                    }
                }
                else
                {
                    // WorkerSlotConfig가 없으면 카테고리 기반 기본 슬롯 생성
                    runtimeSlots.AddRange(CreateDefaultSlots(data));
                }

                buildingSlots[slot] = runtimeSlots;
            }
        }

        /// <summary>
        /// 슬롯을 다시 초기화 (건물 상태 변경 시)
        /// </summary>
        public void RefreshSlots()
        {
            InitializeSlots();
        }

        private List<WorkerSlotRuntime> CreateDefaultSlots(BuildingData data)
        {
            var result = new List<WorkerSlotRuntime>();

            if (data.category == BuildingCategory.Defense)
            {
                // 방어 건물: 공격력 슬롯 + 공격속도 슬롯
                result.Add(new WorkerSlotRuntime(new WorkerSlotDefinition
                {
                    slotName = "공격력",
                    slotType = WorkerSlotType.AttackPower,
                    maxWorkers = 2,
                    effectPerWorker = 1f
                }));
                result.Add(new WorkerSlotRuntime(new WorkerSlotDefinition
                {
                    slotName = "공격속도",
                    slotType = WorkerSlotType.AttackSpeed,
                    maxWorkers = 2,
                    effectPerWorker = 1f
                }));
            }
            else
            {
                var slotType = data.category switch
                {
                    BuildingCategory.Resource => WorkerSlotType.Production,
                    BuildingCategory.Research => WorkerSlotType.Research,
                    _ => WorkerSlotType.Production
                };

                result.Add(new WorkerSlotRuntime(new WorkerSlotDefinition
                {
                    slotName = data.category.ToString(),
                    slotType = slotType,
                    maxWorkers = data.maxConstructionWorkers,
                    effectPerWorker = 1f
                }));
            }

            return result;
        }

        private void SyncBuildingWorkers(BuildingSlot building)
        {
            int total = GetBuildingTotalWorkers(building);
            building.SetWorkers(total);
        }

        private int GetTotalAssigned()
        {
            int total = 0;
            foreach (var kvp in buildingSlots)
            {
                foreach (var s in kvp.Value)
                    total += s.AssignedWorkers;
            }
            if (healingSlot != null) total += healingSlot.AssignedWorkers;
            return total;
        }

        private int GetVulnerableWorkerCount()
        {
            int count = IdleCount;
            foreach (var kvp in buildingSlots)
            {
                if (IsExemptFromEventInjury(kvp.Key)) continue;
                foreach (var s in kvp.Value)
                    count += s.AssignedWorkers;
            }
            return count;
        }

        private bool IsExemptFromEventInjury(BuildingSlot building)
        {
            if (building.CurrentBuildingData == null) return false;
            var cat = building.CurrentBuildingData.category;

            // 방어 건물과 건설 중 건물은 이벤트 부상 면제
            if (cat == BuildingCategory.Defense) return true;
            if (building.State == BuildingSlotState.Constructing) return true;

            // 본부(치료소 포함)도 면제
            if (building.CurrentBuildingData.isHeadquarters) return true;

            return false;
        }
    }
}
