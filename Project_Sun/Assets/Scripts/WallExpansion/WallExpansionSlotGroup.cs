using System.Collections.Generic;
using ProjectSun.Construction;
using UnityEngine;

namespace ProjectSun.WallExpansion
{
    /// <summary>
    /// 씬에 배치하여 특정 방벽 레벨에 해금되는 슬롯 그룹을 시각적으로 관리.
    /// WallExpansionManager가 레벨 업 시 이 컴포넌트를 통해 슬롯을 일괄 해금.
    /// SO의 slotIds 방식과 이 씬 기반 방식 모두 지원.
    /// </summary>
    public class WallExpansionSlotGroup : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("이 그룹이 해금되는 방벽 레벨")]
        [Min(1)]
        [SerializeField] private int unlockLevel = 1;

        [Tooltip("이 레벨에서 해금되는 BuildingSlot 목록 (씬 레퍼런스)")]
        [SerializeField] private List<BuildingSlot> slots = new();

        public int UnlockLevel => unlockLevel;
        public IReadOnlyList<BuildingSlot> Slots => slots;

        /// <summary>
        /// 그룹 내 모든 Hidden 슬롯을 Empty로 전환.
        /// WallExpansionManager.OnExpansionCompleted 이벤트에서 호출.
        /// </summary>
        public void RevealAll()
        {
            foreach (var slot in slots)
            {
                if (slot != null && slot.State == BuildingSlotState.Hidden)
                {
                    slot.Reveal();
                }
            }
        }

        /// <summary>
        /// 그룹 내 Hidden 슬롯 수 반환. UI에서 "N개 슬롯 해금" 표시용.
        /// </summary>
        public int GetHiddenSlotCount()
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (slot != null && slot.State == BuildingSlotState.Hidden)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
