using Unity.Entities;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 방어 타워 태그
    /// </summary>
    public struct TowerTag : IComponentData { }

    /// <summary>
    /// 방어 타워 스탯
    /// </summary>
    public struct TowerStats : IComponentData
    {
        public float Range;
        public float Damage;
        public float AttackSpeed; // 초당 공격 횟수
        public bool CanTargetAir;
    }

    /// <summary>
    /// 타워 공격 타이머
    /// </summary>
    public struct TowerAttackTimer : IComponentData
    {
        public float TimeSinceLastAttack;
    }
}
