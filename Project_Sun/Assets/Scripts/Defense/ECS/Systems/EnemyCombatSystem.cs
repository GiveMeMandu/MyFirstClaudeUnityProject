using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 공격 범위 내 건물을 공격하는 적 전투 시스템.
    /// 공격 시 BuildingDamageBuffer에 데미지를 누적.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial struct EnemyCombatSystem : ISystem
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
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyStats>, RefRW<EnemyState>, RefRO<EnemyTarget>, RefRW<AttackTimer>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                if (!target.ValueRO.HasTarget) continue;

                float dist = math.distance(transform.ValueRO.Position, target.ValueRO.TargetPosition);

                if (dist <= stats.ValueRO.AttackRange)
                {
                    // 공격 범위 안 → Attacking 상태
                    enemyState.ValueRW.Value = (int)Defense.EnemyState.Attacking;

                    attackTimer.ValueRW.TimeSinceLastAttack += deltaTime;

                    if (attackTimer.ValueRW.TimeSinceLastAttack >= stats.ValueRO.AttackInterval)
                    {
                        attackTimer.ValueRW.TimeSinceLastAttack = 0f;

                        // 타겟 건물에 데미지 누적
                        if (SystemAPI.HasComponent<BuildingDamageBuffer>(target.ValueRO.TargetEntity))
                        {
                            var damageBuffer = SystemAPI.GetComponentRW<BuildingDamageBuffer>(target.ValueRO.TargetEntity);
                            damageBuffer.ValueRW.AccumulatedDamage += stats.ValueRO.Damage;
                        }
                    }
                }
                else
                {
                    // 공격 범위 밖 → Moving 상태
                    enemyState.ValueRW.Value = (int)Defense.EnemyState.Moving;
                }
            }
        }
    }

    /// <summary>
    /// HP가 0 이하인 적을 제거하는 시스템.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyCombatSystem))]
    public partial struct EnemyDeathSystem : ISystem
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

            // DeadTag가 있는 Entity 제거 (다음 프레임에 처리하여 이펙트 재생 시간 확보)
            foreach (var (_, entity) in SystemAPI.Query<RefRO<DeadTag>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
