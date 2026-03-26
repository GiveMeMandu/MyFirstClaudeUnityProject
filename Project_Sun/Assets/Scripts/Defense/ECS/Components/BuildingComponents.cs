using Unity.Entities;
using Unity.Mathematics;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 건물 Entity 태그 (DOTS 세계에 등록된 건물)
    /// </summary>
    public struct BuildingTag : IComponentData { }

    /// <summary>
    /// 건물 데이터 (MonoBehaviour에서 브릿지)
    /// </summary>
    public struct BuildingData : IComponentData
    {
        public float MaxHP;
        public float CurrentHP;
        public bool IsHeadquarters;
        public bool IsWall;
        public int SlotIndex; // BuildingManager.AllSlots에서의 인덱스
    }

    /// <summary>
    /// 건물이 받은 데미지 누적 (프레임 종료 시 MonoBehaviour에 전달)
    /// </summary>
    public struct BuildingDamageBuffer : IComponentData
    {
        public float AccumulatedDamage;
    }
}
