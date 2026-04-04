using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// V2 타워 공격 시스템 — 성능 최적화:
    /// - Spatial Hash Map으로 O(1) 범위 쿼리 (O(T*E) → O(T*K) where K = nearby enemies)
    /// - Persistent NativeList로 매 프레임 할당 제거
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyCombatSystemV2))]
    public partial struct TowerAttackSystemV2 : ISystem
    {
        NativeList<float3> _enemyPositions;
        NativeList<Entity> _enemyEntities;
        NativeList<int> _enemyTypes;
        SpatialHashMap _spatialHash;
        NativeList<int> _queryResults;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TowerTag>();

            _enemyPositions = new NativeList<float3>(4096, Allocator.Persistent);
            _enemyEntities = new NativeList<Entity>(4096, Allocator.Persistent);
            _enemyTypes = new NativeList<int>(4096, Allocator.Persistent);
            // Cell size 20: typical tower range is 10~30 units, so 20 covers most in 1~4 cells
            _spatialHash = new SpatialHashMap(20f, 4096, Allocator.Persistent);
            _queryResults = new NativeList<int>(128, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_enemyPositions.IsCreated) _enemyPositions.Dispose();
            if (_enemyEntities.IsCreated) _enemyEntities.Dispose();
            if (_enemyTypes.IsCreated) _enemyTypes.Dispose();
            _spatialHash.Dispose();
            if (_queryResults.IsCreated) _queryResults.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Collect enemy data
            _enemyPositions.Clear();
            _enemyEntities.Clear();
            _enemyTypes.Clear();

            foreach (var (transform, stats, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyStats>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                _enemyPositions.Add(transform.ValueRO.Position);
                _enemyEntities.Add(entity);
                _enemyTypes.Add(stats.ValueRO.EnemyType);
            }

            int enemyCount = _enemyPositions.Length;
            if (enemyCount == 0) return;

            // Build spatial hash from enemy positions
            _spatialHash.Build(_enemyPositions.AsArray(), enemyCount);

            // Process each tower
            foreach (var (towerTransform, towerStats, buildingData, attackTimer) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<TowerStats>, RefRO<BuildingData>, RefRW<TowerAttackTimer>>()
                    .WithAll<TowerTag>())
            {
                if (buildingData.ValueRO.CurrentHP <= 0f) continue;
                if (towerStats.ValueRO.AttackSpeed <= 0f || towerStats.ValueRO.Damage <= 0f) continue;

                attackTimer.ValueRW.TimeSinceLastAttack += deltaTime;

                float attackInterval = 1f / towerStats.ValueRO.AttackSpeed;
                if (attackTimer.ValueRO.TimeSinceLastAttack < attackInterval) continue;

                // Use spatial hash to find nearby enemies
                _queryResults.Clear();
                _spatialHash.QueryRange(towerTransform.ValueRO.Position, towerStats.ValueRO.Range, _queryResults);

                float rangeSq = towerStats.ValueRO.Range * towerStats.ValueRO.Range;
                float closestDistSq = float.MaxValue;
                int closestIdx = -1;

                for (int q = 0; q < _queryResults.Length; q++)
                {
                    int i = _queryResults[q];

                    if (!towerStats.ValueRO.CanTargetAir && _enemyTypes[i] == 2)
                        continue;

                    float distSq = math.distancesq(towerTransform.ValueRO.Position, _enemyPositions[i]);

                    if (distSq <= rangeSq && distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestIdx = i;
                    }
                }

                if (closestIdx >= 0)
                {
                    attackTimer.ValueRW.TimeSinceLastAttack = 0f;

                    var enemyStats = SystemAPI.GetComponentRW<EnemyStats>(_enemyEntities[closestIdx]);
                    enemyStats.ValueRW.CurrentHP -= towerStats.ValueRO.Damage;

                    if (SystemAPI.HasComponent<HealthBarTimer>(_enemyEntities[closestIdx]))
                    {
                        var hbTimer = SystemAPI.GetComponentRW<HealthBarTimer>(_enemyEntities[closestIdx]);
                        hbTimer.ValueRW.RemainingTime = 2f;
                    }
                }
            }
        }
    }
}
