using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 웨이브 타이밍에 따라 적 Entity를 스폰하는 시스템.
    /// BattleManager(MonoBehaviour)가 SpawnGroup 엔티티를 생성하면 이 시스템이 처리.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WaveSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnGroup>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 스폰 포인트 위치 수집
            var spawnPositions = new NativeList<float3>(Allocator.Temp);
            foreach (var (transform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<SpawnPoint>>())
            {
                spawnPositions.Add(transform.ValueRO.Position);
            }

            if (spawnPositions.Length == 0)
            {
                spawnPositions.Dispose();
                ecb.Dispose();
                return;
            }

            foreach (var (spawnGroup, entity) in SystemAPI.Query<RefRW<SpawnGroup>>().WithEntityAccess())
            {
                ref var group = ref spawnGroup.ValueRW;
                group.SpawnTimer += deltaTime;

                while (group.SpawnTimer >= group.SpawnInterval && group.RemainingCount > 0)
                {
                    group.SpawnTimer -= group.SpawnInterval;
                    group.RemainingCount--;

                    // 랜덤 스폰 포인트 선택
                    var spawnPos = spawnPositions[group.RemainingCount % spawnPositions.Length];
                    // 스폰 위치에 약간의 랜덤 오프셋 추가
                    var random = new Random((uint)(group.RemainingCount * 73856093 + 1));
                    spawnPos.x += random.NextFloat(-2f, 2f);
                    spawnPos.z += random.NextFloat(-2f, 2f);

                    var enemyEntity = ecb.Instantiate(group.EnemyPrefab);

                    ecb.SetComponent(enemyEntity, LocalTransform.FromPosition(spawnPos));

                    // 스케일링된 스탯 적용
                    ecb.SetComponent(enemyEntity, new EnemyStats
                    {
                        MaxHP = group.BaseHP * group.StatMultiplier,
                        CurrentHP = group.BaseHP * group.StatMultiplier,
                        Speed = group.BaseSpeed,
                        Damage = group.BaseDamage * group.StatMultiplier,
                        AttackRange = group.BaseAttackRange,
                        AttackInterval = group.BaseAttackInterval,
                        EnemyType = group.EnemyType
                    });
                }

                // 그룹의 모든 적 스폰 완료 시 Entity 제거
                if (group.RemainingCount <= 0)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            spawnPositions.Dispose();
        }
    }
}
