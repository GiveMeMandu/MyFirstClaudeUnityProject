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
    /// 건물이 받은 데미지 이벤트 버퍼.
    /// 동일 프레임에 여러 적이 공격 시 각각 엔트리로 기록 → 합산 처리.
    /// IBufferElementData로 선언하여 멀티 적 동시 공격 데이터 레이스 해소.
    /// </summary>
    public struct BuildingDamageBuffer : IBufferElementData
    {
        public float Damage;
    }
}
