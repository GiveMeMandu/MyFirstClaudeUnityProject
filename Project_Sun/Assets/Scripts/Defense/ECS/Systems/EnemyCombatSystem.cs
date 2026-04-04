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
    [DisableAutoCreation] // V1 — EnemyCombatSystemV2로 대체
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
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyStats>, RefRW<EnemyState>, RefRW<EnemyTarget>, RefRW<AttackTimer>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                if (!target.ValueRO.HasTarget) continue;

                // 타겟 건물이 아직 존재하고 HP가 남아있는지 확인
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
                    // 타겟이 파괴됨 → Moving 상태로 전환하여 새 타겟 탐색
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

                        if (SystemAPI.HasBuffer<BuildingDamageBuffer>(target.ValueRO.TargetEntity))
                        {
                            var damageBuffer = SystemAPI.GetBuffer<BuildingDamageBuffer>(target.ValueRO.TargetEntity);
                            damageBuffer.Add(new BuildingDamageBuffer { Damage = stats.ValueRO.Damage });
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
    /// HP가 0 이하인 적을 제거하는 시스템.
    /// </summary>
    [DisableAutoCreation] // V1 — EnemyDeathSystemV2로 대체
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
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

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

            // DeadTag가 있는 Entity 제거
            foreach (var (_, entity) in SystemAPI.Query<RefRO<DeadTag>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
