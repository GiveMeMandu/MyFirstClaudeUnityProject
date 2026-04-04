using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 적 유닛을 가장 가까운 건물 방향으로 이동시키는 시스템.
    /// 공중 유닛(EnemyType==2)은 방벽(IsWall) 건물을 타겟에서 제외.
    /// </summary>
    [DisableAutoCreation] // V1 — EnemyMovementSystemV2로 대체
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    public partial struct EnemyMovementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // 건물 위치와 데이터를 수집
            var buildingCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<BuildingTag>>())
            {
                buildingCount++;
            }

            if (buildingCount == 0) return;

            var buildingPositions = new NativeArray<float3>(buildingCount, Allocator.Temp);
            var buildingEntities = new NativeArray<Entity>(buildingCount, Allocator.Temp);
            var buildingIsWall = new NativeArray<bool>(buildingCount, Allocator.Temp);
            var buildingHP = new NativeArray<float>(buildingCount, Allocator.Temp);

            int idx = 0;
            foreach (var (transform, data, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<BuildingData>>()
                    .WithAll<BuildingTag>()
                    .WithEntityAccess())
            {
                buildingPositions[idx] = transform.ValueRO.Position;
                buildingEntities[idx] = entity;
                buildingIsWall[idx] = data.ValueRO.IsWall;
                buildingHP[idx] = data.ValueRO.CurrentHP;
                idx++;
            }

            // 각 적 유닛 처리
            foreach (var (transform, stats, enemyState, target) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemyStats>, RefRO<EnemyState>, RefRW<EnemyTarget>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                // Attacking 또는 Dying 상태면 이동하지 않음
                if (enemyState.ValueRO.Value == (int)Defense.EnemyState.Attacking ||
                    enemyState.ValueRO.Value == (int)Defense.EnemyState.Dying)
                    continue;

                bool isFlying = stats.ValueRO.EnemyType == 2; // Flying

                // 가장 가까운 건물 찾기
                float closestDist = float.MaxValue;
                int closestIdx = -1;

                for (int i = 0; i < buildingCount; i++)
                {
                    // HP가 0 이하인 건물은 무시
                    if (buildingHP[i] <= 0f) continue;

                    // 공중 유닛은 방벽 무시
                    if (isFlying && buildingIsWall[i]) continue;

                    float dist = math.distancesq(transform.ValueRO.Position, buildingPositions[i]);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestIdx = i;
                    }
                }

                if (closestIdx >= 0)
                {
                    target.ValueRW.HasTarget = true;
                    target.ValueRW.TargetEntity = buildingEntities[closestIdx];
                    target.ValueRW.TargetPosition = buildingPositions[closestIdx];

                    float dist = math.sqrt(closestDist);

                    // 공격 범위 밖이면 이동
                    if (dist > stats.ValueRO.AttackRange)
                    {
                        float3 direction = math.normalize(buildingPositions[closestIdx] - transform.ValueRO.Position);
                        float3 newPos = transform.ValueRO.Position + direction * stats.ValueRO.Speed * deltaTime;

                        // 공중 유닛은 Y축 고정 (약간 높게)
                        if (isFlying)
                        {
                            newPos.y = 3f;
                        }
                        else
                        {
                            newPos.y = 0f;
                        }

                        transform.ValueRW = LocalTransform.FromPositionRotation(
                            newPos,
                            quaternion.LookRotationSafe(direction, math.up())
                        );
                    }
                }
                else
                {
                    target.ValueRW.HasTarget = false;
                }
            }

            buildingPositions.Dispose();
            buildingEntities.Dispose();
            buildingIsWall.Dispose();
            buildingHP.Dispose();
        }
    }
}
