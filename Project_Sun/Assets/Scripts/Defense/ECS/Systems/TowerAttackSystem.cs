using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 방어 타워가 사거리 내 적을 자동 공격하는 시스템.
    /// canTargetAir가 false이면 Flying(EnemyType==2) 적을 무시.
    /// </summary>
    [DisableAutoCreation] // V1 — TowerAttackSystemV2로 대체
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyCombatSystem))]
    public partial struct TowerAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TowerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // 살아있는 적 유닛 정보 수집
            int enemyCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<EnemyTag>>().WithNone<DeadTag>())
            {
                enemyCount++;
            }

            if (enemyCount == 0) return;

            var enemyPositions = new NativeArray<float3>(enemyCount, Allocator.Temp);
            var enemyEntities = new NativeArray<Entity>(enemyCount, Allocator.Temp);
            var enemyTypes = new NativeArray<int>(enemyCount, Allocator.Temp);

            int idx = 0;
            foreach (var (transform, stats, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyStats>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                enemyPositions[idx] = transform.ValueRO.Position;
                enemyEntities[idx] = entity;
                enemyTypes[idx] = stats.ValueRO.EnemyType;
                idx++;
            }

            // 각 타워 처리
            foreach (var (towerTransform, towerStats, buildingData, attackTimer) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<TowerStats>, RefRO<BuildingData>, RefRW<TowerAttackTimer>>()
                    .WithAll<TowerTag>())
            {
                // 파괴된 건물 또는 비활성 타워(인력 미배치)는 스킵
                if (buildingData.ValueRO.CurrentHP <= 0f) continue;
                if (towerStats.ValueRO.AttackSpeed <= 0f || towerStats.ValueRO.Damage <= 0f) continue;

                attackTimer.ValueRW.TimeSinceLastAttack += deltaTime;

                float attackInterval = 1f / towerStats.ValueRO.AttackSpeed;

                if (attackTimer.ValueRO.TimeSinceLastAttack < attackInterval) continue;

                // 사거리 내 가장 가까운 적 찾기
                float rangeSq = towerStats.ValueRO.Range * towerStats.ValueRO.Range;
                float closestDistSq = float.MaxValue;
                int closestIdx = -1;

                for (int i = 0; i < enemyCount; i++)
                {
                    // 대공 불가이면 공중 유닛 무시
                    if (!towerStats.ValueRO.CanTargetAir && enemyTypes[i] == 2)
                        continue;

                    float distSq = math.distancesq(towerTransform.ValueRO.Position, enemyPositions[i]);

                    if (distSq <= rangeSq && distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestIdx = i;
                    }
                }

                if (closestIdx >= 0)
                {
                    attackTimer.ValueRW.TimeSinceLastAttack = 0f;

                    // 적 HP 직접 감소
                    var enemyStats = SystemAPI.GetComponentRW<EnemyStats>(enemyEntities[closestIdx]);
                    enemyStats.ValueRW.CurrentHP -= towerStats.ValueRO.Damage;

                    // 체력바 표시 트리거
                    if (SystemAPI.HasComponent<HealthBarTimer>(enemyEntities[closestIdx]))
                    {
                        var hbTimer = SystemAPI.GetComponentRW<HealthBarTimer>(enemyEntities[closestIdx]);
                        hbTimer.ValueRW.RemainingTime = 2f;
                    }
                }
            }

            enemyPositions.Dispose();
            enemyEntities.Dispose();
            enemyTypes.Dispose();
        }
    }
}
