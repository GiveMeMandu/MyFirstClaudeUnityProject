using Unity.Entities;
using Unity.Mathematics;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 적 유닛 기본 데이터
    /// </summary>
    public struct EnemyTag : IComponentData { }

    public struct EnemyStats : IComponentData
    {
        public float MaxHP;
        public float CurrentHP;
        public float Speed;
        public float Damage;
        public float AttackRange;
        public float AttackInterval;
        public int EnemyType; // 0=Basic, 1=Heavy, 2=Flying
    }

    public struct EnemyState : IComponentData
    {
        public int Value; // 0=Spawning, 1=Moving, 2=Attacking, 3=Dying
    }

    /// <summary>
    /// 적이 현재 공격 중인 건물 타겟
    /// </summary>
    public struct EnemyTarget : IComponentData
    {
        public Entity TargetEntity;
        public float3 TargetPosition;
        public bool HasTarget;
    }

    /// <summary>
    /// 적 공격 타이머
    /// </summary>
    public struct AttackTimer : IComponentData
    {
        public float TimeSinceLastAttack;
    }

    /// <summary>
    /// 사망 처리 마커
    /// </summary>
    public struct DeadTag : IComponentData { }

    /// <summary>
    /// 체력바 표시 타이머
    /// </summary>
    public struct HealthBarTimer : IComponentData
    {
        public float RemainingTime;
    }
}
