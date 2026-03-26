using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Workforce
{
    /// <summary>
    /// 건물 내 개별 인력 슬롯 정의 (SO 내에서 사용)
    /// </summary>
    [Serializable]
    public struct WorkerSlotDefinition
    {
        public string slotName;
        public WorkerSlotType slotType;
        [Min(1)]
        public int maxWorkers;
        [Tooltip("이 슬롯에 배치 시 효과 배율 (인력당)")]
        public float effectPerWorker;
    }

    /// <summary>
    /// 건물의 인력 슬롯 구성을 정의하는 SO.
    /// BuildingData에 참조하여 사용.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWorkerSlots", menuName = "ProjectSun/Workforce/Worker Slot Config")]
    public class WorkerSlotConfig : ScriptableObject
    {
        [Tooltip("이 건물이 가진 인력 슬롯 목록")]
        public List<WorkerSlotDefinition> slots = new();

        /// <summary>
        /// 전체 슬롯의 최대 인력 합계
        /// </summary>
        public int TotalMaxWorkers
        {
            get
            {
                int total = 0;
                foreach (var s in slots) total += s.maxWorkers;
                return total;
            }
        }
    }

    /// <summary>
    /// 런타임 슬롯 상태 (배치된 인력 수 추적)
    /// </summary>
    [Serializable]
    public class WorkerSlotRuntime
    {
        public string SlotName;
        public WorkerSlotType SlotType;
        public int MaxWorkers;
        public float EffectPerWorker;
        public int AssignedWorkers;

        public WorkerSlotRuntime(WorkerSlotDefinition def)
        {
            SlotName = def.slotName;
            SlotType = def.slotType;
            MaxWorkers = def.maxWorkers;
            EffectPerWorker = def.effectPerWorker;
            AssignedWorkers = 0;
        }

        public bool CanAddWorker => AssignedWorkers < MaxWorkers;

        public float TotalEffect => AssignedWorkers * EffectPerWorker;
    }
}
