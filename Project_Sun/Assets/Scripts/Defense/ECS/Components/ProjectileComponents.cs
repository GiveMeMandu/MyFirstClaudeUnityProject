using Unity.Entities;
using Unity.Mathematics;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 투사체 엔티티 태그
    /// </summary>
    public struct ProjectileTag : IComponentData { }

    /// <summary>
    /// 투사체 데이터 — 타겟 추적, 데미지, 수명 관리
    /// </summary>
    public struct ProjectileData : IComponentData
    {
        public Entity TargetEntity;
        public float3 TargetLastPosition; // 타겟 소실 시 폴백 위치
        public float Speed;
        public float Damage;
        public float LifeTime; // 최대 수명 (초)
    }
}
