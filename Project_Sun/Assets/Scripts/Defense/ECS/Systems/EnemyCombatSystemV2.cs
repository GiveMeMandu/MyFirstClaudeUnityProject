using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// V2 적 전투 시스템 — EnemyCombatSystem과 동일 로직, V2 시스템 오더에 맞게 분리.
    ///
    /// C-01 수정: BuildingDamageBuffer가 IBufferElementData로 변경됨에 따라
    /// GetBuffer&lt;BuildingDamageBuffer&gt;로 접근하여 Damage 이벤트를 Append.
    /// 동일 프레임 멀티 적 동시 공격 시 각각 엔트리로 누적 → 데이터 레이스 해소.
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

                        // IBufferElementData Append — 여러 적이 동시 공격해도 각각 기록됨
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
    /// V2 건물 데미지 적용 시스템.
    /// BuildingDamageBuffer에 누적된 Damage 이벤트를 합산하여 BuildingData.CurrentHP에 반영.
    /// EnemyCombatSystemV2 이후, EnemyDeathSystemV2 이전에 실행.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyCombatSystemV2))]
    [UpdateBefore(typeof(TowerAttackSystemV2))]
    public partial struct BuildingDamageApplySystemV2 : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BuildingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (buildingData, damageBuffer) in
                SystemAPI.Query<RefRW<BuildingData>, DynamicBuffer<BuildingDamageBuffer>>()
                    .WithAll<BuildingTag>())
            {
                if (damageBuffer.Length == 0) continue;

                float total = 0f;
                for (int i = 0; i < damageBuffer.Length; i++)
                    total += damageBuffer[i].Damage;

                buildingData.ValueRW.CurrentHP = math.max(0f, buildingData.ValueRO.CurrentHP - total);
                damageBuffer.Clear();
            }
        }
    }

    /// <summary>
    /// V2 사망 처리 시스템.
    ///
    /// C-02 수정: Allocator.Temp ECB → BeginSimulationEntityCommandBufferSystem.Singleton 사용.
    /// Burst 환경에서 안전한 ECB 패턴.
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

            foreach (var (_, entity) in SystemAPI.Query<RefRO<DeadTag>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
