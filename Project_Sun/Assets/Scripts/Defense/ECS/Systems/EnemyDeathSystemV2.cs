using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// V2 적 사망 시스템 — HP <= 0인 적을 DeadTag 마킹 후 파괴.
    /// BattleStatistics에 킬 수를 반영.
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

            // Phase 1: HP <= 0인 적에 DeadTag 추가 (다음 프레임 시작 시 반영)
            int killCount = 0;
            foreach (var (stats, enemyState, entity) in
                SystemAPI.Query<RefRO<EnemyStats>, RefRW<EnemyState>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                if (stats.ValueRO.CurrentHP <= 0f)
                {
                    enemyState.ValueRW.Value = (int)Defense.EnemyState.Dying;
                    ecb.AddComponent<DeadTag>(entity);
                    killCount++;
                }
            }

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
}
