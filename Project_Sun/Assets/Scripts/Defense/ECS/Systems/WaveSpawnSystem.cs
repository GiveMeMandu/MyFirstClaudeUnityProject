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
    /// PoC에서는 SubScene/프리팹 없이 Entity를 직접 생성.
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
            bool hasBattleStats = SystemAPI.HasSingleton<BattleStatistics>();

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

            int totalSpawnedThisFrame = 0;

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
                    // 스폰 위치에 약간의 랜덤 오프셋
                    var random = new Random((uint)(group.RemainingCount * 73856093 + 1));
                    spawnPos.x += random.NextFloat(-3f, 3f);
                    spawnPos.z += random.NextFloat(-3f, 3f);

                    // 공중 유닛은 Y 높이 설정
                    if (group.EnemyType == 2) // Flying
                    {
                        spawnPos.y = 3f;
                    }

                    // Entity를 직접 생성 (프리팹 없이)
                    var enemyEntity = ecb.CreateEntity();

                    ecb.AddComponent(enemyEntity, LocalTransform.FromPosition(spawnPos));
                    ecb.AddComponent(enemyEntity, new EnemyTag());

                    ecb.AddComponent(enemyEntity, new EnemyStats
                    {
                        MaxHP = group.BaseHP * group.StatMultiplier,
                        CurrentHP = group.BaseHP * group.StatMultiplier,
                        Speed = group.BaseSpeed,
                        Damage = group.BaseDamage * group.StatMultiplier,
                        AttackRange = group.BaseAttackRange,
                        AttackInterval = group.BaseAttackInterval,
                        EnemyType = group.EnemyType
                    });

                    ecb.AddComponent(enemyEntity, new EnemyState
                    {
                        Value = (int)Defense.EnemyState.Moving
                    });

                    ecb.AddComponent(enemyEntity, new EnemyTarget
                    {
                        TargetEntity = Entity.Null,
                        TargetPosition = float3.zero,
                        HasTarget = false
                    });

                    ecb.AddComponent(enemyEntity, new AttackTimer
                    {
                        TimeSinceLastAttack = 0f
                    });

                    ecb.AddComponent(enemyEntity, new HealthBarTimer
                    {
                        RemainingTime = 0f
                    });

                    // 특수행동 컴포넌트 (SF-WD-015)
                    bool hasAbility = group.BypassWalls || group.AttemptsWallBypass
                        || group.ExplodesOnDeath || group.WallDamageMultiplier > 1f;
                    if (hasAbility)
                    {
                        ecb.AddComponent(enemyEntity, new EnemyAbilities
                        {
                            BypassWalls = group.BypassWalls,
                            AttemptsWallBypass = group.AttemptsWallBypass,
                            WallDamageMultiplier = group.WallDamageMultiplier,
                            ExplodesOnDeath = group.ExplodesOnDeath,
                            DeathExplosionRadius = group.DeathExplosionRadius,
                            DeathExplosionDamage = group.DeathExplosionDamage
                        });
                    }

                    totalSpawnedThisFrame++;
                }

                // 그룹의 모든 적 스폰 완료 시 Entity 제거
                if (group.RemainingCount <= 0)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            // BattleStatistics 갱신
            if (hasBattleStats && totalSpawnedThisFrame > 0)
            {
                var battleStats = SystemAPI.GetSingletonRW<BattleStatistics>();
                battleStats.ValueRW.TotalEnemiesSpawned += totalSpawnedThisFrame;
                battleStats.ValueRW.RemainingEnemies += totalSpawnedThisFrame;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            spawnPositions.Dispose();
        }
    }
}
