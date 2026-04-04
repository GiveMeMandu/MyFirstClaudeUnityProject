using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// V2 적 전투 시스템 — 공격 범위 내 건물을 공격.
    /// BuildingDamageBuffer(IBufferElementData)에 데미지를 누적하여
    /// BuildingDamageApplySystemV2에서 합산 처리. 동일 프레임 멀티 적 동시 공격 시
    /// 각각 엔트리로 누적 → 데이터 레이스 해소.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystemV2))]
    public partial struct EnemyCombatSystemV2 : ISystem
    {
        [BurstCompile]
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
                    // 타겟이 파괴됨 -> Moving 상태로 전환하여 새 타겟 탐색
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
    /// BattleStatistics.TotalDamageToBuildings에도 누적.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyCombatSystemV2))]
    [UpdateBefore(typeof(TowerAttackSystemV2))]
    public partial struct BuildingDamageApplySystemV2 : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BuildingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            bool hasStats = SystemAPI.HasSingleton<BattleStatistics>();

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

                // BattleStatistics에 건물 데미지 누적
                if (hasStats)
                {
                    var battleStats = SystemAPI.GetSingletonRW<BattleStatistics>();
                    battleStats.ValueRW.TotalDamageToBuildings += total;
                }
            }
        }
    }
}
