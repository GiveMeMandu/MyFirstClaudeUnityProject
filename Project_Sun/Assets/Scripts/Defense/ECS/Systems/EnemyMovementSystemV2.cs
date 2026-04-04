using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// V2 적 이동 시스템 — 성능 최적화:
    /// - Persistent NativeList로 매 프레임 할당 제거
    /// - 건물 데이터 한 번 수집 후 재사용
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    public partial struct EnemyMovementSystemV2 : ISystem
    {
        NativeList<float3> _buildingPositions;
        NativeList<Entity> _buildingEntities;
        NativeList<bool> _buildingIsWall;
        NativeList<float> _buildingHP;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();

            _buildingPositions = new NativeList<float3>(32, Allocator.Persistent);
            _buildingEntities = new NativeList<Entity>(32, Allocator.Persistent);
            _buildingIsWall = new NativeList<bool>(32, Allocator.Persistent);
            _buildingHP = new NativeList<float>(32, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_buildingPositions.IsCreated) _buildingPositions.Dispose();
            if (_buildingEntities.IsCreated) _buildingEntities.Dispose();
            if (_buildingIsWall.IsCreated) _buildingIsWall.Dispose();
            if (_buildingHP.IsCreated) _buildingHP.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Collect building data into persistent lists (clear + refill, no alloc)
            _buildingPositions.Clear();
            _buildingEntities.Clear();
            _buildingIsWall.Clear();
            _buildingHP.Clear();

            foreach (var (transform, data, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<BuildingData>>()
                    .WithAll<BuildingTag>()
                    .WithEntityAccess())
            {
                _buildingPositions.Add(transform.ValueRO.Position);
                _buildingEntities.Add(entity);
                _buildingIsWall.Add(data.ValueRO.IsWall);
                _buildingHP.Add(data.ValueRO.CurrentHP);
            }

            int buildingCount = _buildingPositions.Length;
            if (buildingCount == 0) return;

            foreach (var (transform, stats, enemyState, target) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemyStats>, RefRO<EnemyState>, RefRW<EnemyTarget>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                if (enemyState.ValueRO.Value == (int)Defense.EnemyState.Attacking ||
                    enemyState.ValueRO.Value == (int)Defense.EnemyState.Dying)
                    continue;

                bool isFlying = stats.ValueRO.EnemyType == 2;

                float closestDist = float.MaxValue;
                int closestIdx = -1;

                for (int i = 0; i < buildingCount; i++)
                {
                    if (_buildingHP[i] <= 0f) continue;
                    if (isFlying && _buildingIsWall[i]) continue;

                    float dist = math.distancesq(transform.ValueRO.Position, _buildingPositions[i]);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestIdx = i;
                    }
                }

                if (closestIdx >= 0)
                {
                    target.ValueRW.HasTarget = true;
                    target.ValueRW.TargetEntity = _buildingEntities[closestIdx];
                    target.ValueRW.TargetPosition = _buildingPositions[closestIdx];

                    float dist = math.sqrt(closestDist);

                    if (dist > stats.ValueRO.AttackRange)
                    {
                        float3 direction = math.normalize(_buildingPositions[closestIdx] - transform.ValueRO.Position);
                        float3 newPos = transform.ValueRO.Position + direction * stats.ValueRO.Speed * deltaTime;

                        newPos.y = isFlying ? 3f : 0f;

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
        }
    }
}
