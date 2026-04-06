using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// V2 적 사망 시스템 — HP <= 0인 적을 DeadTag 마킹 후 파괴.
    /// BattleStatistics에 킬 수를 반영.
    /// SF-WD-015: Bloater 사망 폭발 AoE 처리.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileHitSystem))]
    public partial struct EnemyDeathSystemV2 : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            bool hasBattleStats = SystemAPI.HasSingleton<BattleStatistics>();

            // 사망 폭발 대상 수집 (Bloater)
            var explosions = new NativeList<DeathExplosion>(Allocator.Temp);

            // Phase 1: HP <= 0인 적에 DeadTag 추가 (다음 프레임 시작 시 반영)
            int killCount = 0;
            foreach (var (stats, enemyState, transform, entity) in
                SystemAPI.Query<RefRO<EnemyStats>, RefRW<EnemyState>, RefRO<LocalTransform>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                if (stats.ValueRO.CurrentHP <= 0f)
                {
                    enemyState.ValueRW.Value = (int)Defense.EnemyState.Dying;
                    ecb.AddComponent<DeadTag>(entity);
                    killCount++;

                    // SF-WD-015: Bloater 사망 폭발
                    if (SystemAPI.HasComponent<EnemyAbilities>(entity))
                    {
                        var abilities = SystemAPI.GetComponentRO<EnemyAbilities>(entity);
                        if (abilities.ValueRO.ExplodesOnDeath && abilities.ValueRO.DeathExplosionRadius > 0f)
                        {
                            explosions.Add(new DeathExplosion
                            {
                                Position = transform.ValueRO.Position,
                                Radius = abilities.ValueRO.DeathExplosionRadius,
                                Damage = abilities.ValueRO.DeathExplosionDamage
                            });
                        }
                    }
                }
            }

            // 사망 폭발 AoE 적용: 범위 내 건물에 데미지
            for (int e = 0; e < explosions.Length; e++)
            {
                var explosion = explosions[e];
                float radiusSq = explosion.Radius * explosion.Radius;

                foreach (var (buildingData, buildingTransform, damageBuffer) in
                    SystemAPI.Query<RefRO<BuildingData>, RefRO<LocalTransform>, DynamicBuffer<BuildingDamageBuffer>>()
                        .WithAll<BuildingTag>())
                {
                    float distSq = math.distancesq(explosion.Position, buildingTransform.ValueRO.Position);
                    if (distSq <= radiusSq && buildingData.ValueRO.CurrentHP > 0f)
                    {
                        damageBuffer.Add(new BuildingDamageBuffer { Damage = explosion.Damage });
                    }
                }
            }
            explosions.Dispose();

            // BattleStatistics 갱신
            if (hasBattleStats && killCount > 0)
            {
                var battleStats = SystemAPI.GetSingletonRW<BattleStatistics>();
                battleStats.ValueRW.TotalEnemiesKilled += killCount;
                battleStats.ValueRW.RemainingEnemies = math.max(0, battleStats.ValueRO.RemainingEnemies - killCount);
            }

            // Phase 2: 이전 프레임에서 DeadTag가 붙은 Entity 파괴
            foreach (var (_, entity) in SystemAPI.Query<RefRO<DeadTag>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }

    /// <summary>사망 폭발 데이터 (Bloater). 임시 수집용.</summary>
    struct DeathExplosion
    {
        public float3 Position;
        public float Radius;
        public float Damage;
    }
}
