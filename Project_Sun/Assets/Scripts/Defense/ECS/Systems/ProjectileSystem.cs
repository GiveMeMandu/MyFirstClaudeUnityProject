using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 투사체 이동 시스템 — 타겟 추적 이동.
    /// 타겟이 살아있으면 현재 위치를 추적, 타겟이 없으면 TargetLastPosition으로 이동.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TowerAttackSystemV2))]
    public partial struct ProjectileMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectileTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, projectileData) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRW<ProjectileData>>()
                    .WithAll<ProjectileTag>())
            {
                // 수명 감소
                projectileData.ValueRW.LifeTime -= deltaTime;

                // 타겟이 아직 존재하면 현재 위치로 갱신
                if (projectileData.ValueRO.TargetEntity != Entity.Null &&
                    SystemAPI.HasComponent<LocalTransform>(projectileData.ValueRO.TargetEntity) &&
                    !SystemAPI.HasComponent<DeadTag>(projectileData.ValueRO.TargetEntity))
                {
                    var targetTransform = SystemAPI.GetComponentRO<LocalTransform>(projectileData.ValueRO.TargetEntity);
                    projectileData.ValueRW.TargetLastPosition = targetTransform.ValueRO.Position;
                }

                // 타겟 위치 방향으로 이동
                float3 currentPos = transform.ValueRO.Position;
                float3 targetPos = projectileData.ValueRO.TargetLastPosition;
                float3 direction = targetPos - currentPos;
                float distance = math.length(direction);

                if (distance > 0.01f)
                {
                    float3 normalizedDir = direction / distance;
                    float moveAmount = projectileData.ValueRO.Speed * deltaTime;

                    // 오버슈팅 방지: 남은 거리보다 많이 이동하지 않음
                    if (moveAmount > distance)
                    {
                        moveAmount = distance;
                    }

                    float3 newPos = currentPos + normalizedDir * moveAmount;
                    transform.ValueRW = LocalTransform.FromPositionRotation(
                        newPos,
                        quaternion.LookRotationSafe(normalizedDir, math.up())
                    );
                }
            }
        }
    }

    /// <summary>
    /// 투사체 충돌 판정 시스템 — 타겟 도달 시 데미지 적용 + 엔티티 파괴.
    /// 거리 < 0.5f 또는 LifeTime 초과 시 투사체 파괴.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial struct ProjectileHitSystem : ISystem
    {
        private const float HitDistanceThreshold = 0.5f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectileTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, projectileData, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<ProjectileData>>()
                    .WithAll<ProjectileTag>()
                    .WithEntityAccess())
            {
                // 수명 초과 시 파괴 (데미지 없이)
                if (projectileData.ValueRO.LifeTime <= 0f)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // 타겟 위치와의 거리 계산
                float3 currentPos = transform.ValueRO.Position;
                float3 targetPos = projectileData.ValueRO.TargetLastPosition;
                float distance = math.distance(currentPos, targetPos);

                if (distance < HitDistanceThreshold)
                {
                    // 타겟이 아직 살아있으면 데미지 적용
                    Entity targetEntity = projectileData.ValueRO.TargetEntity;
                    if (targetEntity != Entity.Null &&
                        SystemAPI.HasComponent<EnemyStats>(targetEntity) &&
                        !SystemAPI.HasComponent<DeadTag>(targetEntity))
                    {
                        var enemyStats = SystemAPI.GetComponentRW<EnemyStats>(targetEntity);
                        enemyStats.ValueRW.CurrentHP -= projectileData.ValueRO.Damage;

                        // 체력바 표시 트리거
                        if (SystemAPI.HasComponent<HealthBarTimer>(targetEntity))
                        {
                            var hbTimer = SystemAPI.GetComponentRW<HealthBarTimer>(targetEntity);
                            hbTimer.ValueRW.RemainingTime = 2f;
                        }
                    }

                    // 투사체 파괴
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
