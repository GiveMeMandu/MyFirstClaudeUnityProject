using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// V2 적 전투 시스템 — EnemyCombatSystem과 동일 로직, V2 시스템 오더에 맞게 분리.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystemV2))]
    public partial struct EnemyCombatSystemV2 : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, stats, enemyState, target, attackTimer) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyStats>, RefRW<EnemyState>, RefRW<EnemyTarget>, RefRW<AttackTimer>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                if (!target.ValueRO.HasTarget) continue;

                bool targetValid = SystemAPI.HasComponent<BuildingData>(target.ValueRO.TargetEntity);
                if (targetValid)
                {
                    var buildingData = SystemAPI.GetComponentRO<BuildingData>(target.ValueRO.TargetEntity);
                    if (buildingData.ValueRO.CurrentHP <= 0f)
                    {
                        targetValid = false;
                    }
                }

                if (!targetValid)
                {
                    enemyState.ValueRW.Value = (int)Defense.EnemyState.Moving;
                    target.ValueRW.HasTarget = false;
                    attackTimer.ValueRW.TimeSinceLastAttack = 0f;
                    continue;
                }

                float dist = math.distance(transform.ValueRO.Position, target.ValueRO.TargetPosition);

                if (dist <= stats.ValueRO.AttackRange)
                {
                    enemyState.ValueRW.Value = (int)Defense.EnemyState.Attacking;
                    attackTimer.ValueRW.TimeSinceLastAttack += deltaTime;

                    if (attackTimer.ValueRW.TimeSinceLastAttack >= stats.ValueRO.AttackInterval)
                    {
                        attackTimer.ValueRW.TimeSinceLastAttack = 0f;

                        if (SystemAPI.HasComponent<BuildingDamageBuffer>(target.ValueRO.TargetEntity))
                        {
                            var damageBuffer = SystemAPI.GetComponentRW<BuildingDamageBuffer>(target.ValueRO.TargetEntity);
                            damageBuffer.ValueRW.AccumulatedDamage += stats.ValueRO.Damage;
                        }
                    }
                }
                else
                {
                    enemyState.ValueRW.Value = (int)Defense.EnemyState.Moving;
                }
            }
        }
    }

    /// <summary>
    /// V2 사망 처리 시스템 — EntityCommandBuffer Persistent 사용.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TowerAttackSystemV2))]
    public partial struct EnemyDeathSystemV2 : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

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
                }
            }

            foreach (var (_, entity) in SystemAPI.Query<RefRO<DeadTag>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
