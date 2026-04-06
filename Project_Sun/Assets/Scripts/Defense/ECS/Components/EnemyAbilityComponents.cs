using Unity.Entities;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 적 특수행동 ECS 컴포넌트. SO(EnemySpecialAbility) → ECS 복사.
    /// M1 핵심 4종: Sprinter, Bloater, Charger, Burrower.
    /// SF-WD-015.
    /// </summary>
    public struct EnemyAbilities : IComponentData
    {
        // 방벽 무시 (Burrower): 벽을 타겟에서 완전 제외
        public bool BypassWalls;

        // 방벽 우회 시도 (Sprinter): 벽 아닌 건물 우선 타겟팅
        public bool AttemptsWallBypass;

        // 방벽 추가 피해 (Charger): 벽 공격 시 데미지 배율
        public float WallDamageMultiplier;

        // 사망 폭발 (Bloater): 죽을 때 주변 건물 AoE 데미지
        public bool ExplodesOnDeath;
        public float DeathExplosionRadius;
        public float DeathExplosionDamage;
    }
}
