using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ProjectSun.Defense.ECS.Benchmark
{
    /// <summary>
    /// ECS 3,000개체 성능 벤치마크 컨트롤러.
    /// PlayMode에서 동작하며 적 3,000 + 타워 10 + 건물 8을 스폰하고
    /// 프레임 시간을 측정/로깅한다.
    ///
    /// 사용법: 빈 씬에 이 컴포넌트를 붙이고 Play.
    /// </summary>
    public class ECSBenchmarkController : MonoBehaviour
    {
        [Header("Benchmark Settings")]
        [SerializeField] int enemyCount = 3000;
        [SerializeField] int towerCount = 10;
        [SerializeField] int wallCount = 4;
        [SerializeField] int buildingCount = 4;
        [SerializeField] float spawnRadius = 80f;
        [SerializeField] float baseRadius = 20f;

        [Header("Frame Time Tracking")]
        [SerializeField] int warmupFrames = 60;
        [SerializeField] int measureFrames = 300;

        // Runtime state
        EntityManager _entityManager;
        int _frameCount;
        float _totalFrameTime;
        float _maxFrameTime;
        float _minFrameTime = float.MaxValue;
        bool _measuring;
        bool _done;
        float[] _frameTimes;
        int _measureIndex;

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _frameTimes = new float[measureFrames];

            // Disable V1 systems to avoid conflicts
            DisableV1Systems();

            // Spawn benchmark entities
            SpawnBuildings();
            SpawnTowers();
            SpawnEnemies();

            Debug.Log($"[ECS Benchmark] Spawned: {enemyCount} enemies, {towerCount} towers, {wallCount} walls, {buildingCount} buildings");
            Debug.Log($"[ECS Benchmark] Warming up for {warmupFrames} frames...");
        }

        void DisableV1Systems()
        {
            var world = World.DefaultGameObjectInjectionWorld;

            TryDisableSystem<EnemyMovementSystem>(world);
            TryDisableSystem<TowerAttackSystem>(world);
            TryDisableSystem<EnemyCombatSystem>(world);
            TryDisableSystem<EnemyDeathSystem>(world);
        }

        void TryDisableSystem<T>(World world) where T : unmanaged, ISystem
        {
            var handle = world.GetExistingSystem<T>();
            if (handle != default)
            {
                world.Unmanaged.ResolveSystemStateRef(handle).Enabled = false;
            }
        }

        void SpawnBuildings()
        {
            // Headquarters at center
            CreateBuilding(float3.zero, 500f, true, false, 0);

            // Regular buildings around HQ
            for (int i = 0; i < buildingCount; i++)
            {
                float angle = (float)i / buildingCount * math.PI * 2f;
                float3 pos = new float3(math.cos(angle) * baseRadius * 0.5f, 0, math.sin(angle) * baseRadius * 0.5f);
                CreateBuilding(pos, 200f, false, false, i + 1);
            }

            // Walls at perimeter
            for (int i = 0; i < wallCount; i++)
            {
                float angle = (float)i / wallCount * math.PI * 2f;
                float3 pos = new float3(math.cos(angle) * baseRadius, 0, math.sin(angle) * baseRadius);
                CreateBuilding(pos, 300f, false, true, buildingCount + i + 1);
            }
        }

        void CreateBuilding(float3 position, float hp, bool isHQ, bool isWall, int slotIndex)
        {
            var entity = _entityManager.CreateEntity();

            _entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
            _entityManager.AddComponentData(entity, new BuildingTag());
            _entityManager.AddComponentData(entity, new BuildingData
            {
                MaxHP = hp,
                CurrentHP = hp,
                IsHeadquarters = isHQ,
                IsWall = isWall,
                SlotIndex = slotIndex
            });
            _entityManager.AddComponentData(entity, new BuildingDamageBuffer
            {
                AccumulatedDamage = 0f
            });
        }

        void SpawnTowers()
        {
            for (int i = 0; i < towerCount; i++)
            {
                float angle = (float)i / towerCount * math.PI * 2f;
                float dist = baseRadius * 0.8f;
                float3 pos = new float3(math.cos(angle) * dist, 0, math.sin(angle) * dist);

                var entity = _entityManager.CreateEntity();

                _entityManager.AddComponentData(entity, LocalTransform.FromPosition(pos));
                _entityManager.AddComponentData(entity, new TowerTag());
                _entityManager.AddComponentData(entity, new BuildingTag());
                _entityManager.AddComponentData(entity, new TowerStats
                {
                    Range = 15f,
                    Damage = 10f,
                    AttackSpeed = 2f,
                    CanTargetAir = (i % 3 == 0) // Every 3rd tower can target air
                });
                _entityManager.AddComponentData(entity, new TowerAttackTimer
                {
                    TimeSinceLastAttack = 0f
                });
                _entityManager.AddComponentData(entity, new BuildingData
                {
                    MaxHP = 150f,
                    CurrentHP = 150f,
                    IsHeadquarters = false,
                    IsWall = false,
                    SlotIndex = 100 + i
                });
                _entityManager.AddComponentData(entity, new BuildingDamageBuffer
                {
                    AccumulatedDamage = 0f
                });
            }
        }

        void SpawnEnemies()
        {
            var random = new Unity.Mathematics.Random(42);

            for (int i = 0; i < enemyCount; i++)
            {
                // Spawn enemies in a ring around the base
                float angle = random.NextFloat(0f, math.PI * 2f);
                float dist = random.NextFloat(spawnRadius * 0.7f, spawnRadius * 1.3f);
                float3 pos = new float3(math.cos(angle) * dist, 0, math.sin(angle) * dist);

                int enemyType;
                float hp, speed, damage, attackRange, attackInterval;

                // Mix of enemy types: 70% basic, 20% heavy, 10% flying
                float typeRoll = random.NextFloat();
                if (typeRoll < 0.7f)
                {
                    enemyType = 0; // Basic
                    hp = 30f;
                    speed = 3f;
                    damage = 5f;
                    attackRange = 2f;
                    attackInterval = 1f;
                }
                else if (typeRoll < 0.9f)
                {
                    enemyType = 1; // Heavy
                    hp = 80f;
                    speed = 1.5f;
                    damage = 12f;
                    attackRange = 2f;
                    attackInterval = 1.5f;
                }
                else
                {
                    enemyType = 2; // Flying
                    hp = 20f;
                    speed = 5f;
                    damage = 3f;
                    attackRange = 3f;
                    attackInterval = 0.8f;
                    pos.y = 3f;
                }

                var entity = _entityManager.CreateEntity();

                _entityManager.AddComponentData(entity, LocalTransform.FromPosition(pos));
                _entityManager.AddComponentData(entity, new EnemyTag());
                _entityManager.AddComponentData(entity, new EnemyStats
                {
                    MaxHP = hp,
                    CurrentHP = hp,
                    Speed = speed,
                    Damage = damage,
                    AttackRange = attackRange,
                    AttackInterval = attackInterval,
                    EnemyType = enemyType
                });
                _entityManager.AddComponentData(entity, new EnemyState
                {
                    Value = (int)Defense.EnemyState.Moving
                });
                _entityManager.AddComponentData(entity, new EnemyTarget
                {
                    TargetEntity = Entity.Null,
                    TargetPosition = float3.zero,
                    HasTarget = false
                });
                _entityManager.AddComponentData(entity, new AttackTimer
                {
                    TimeSinceLastAttack = 0f
                });
                _entityManager.AddComponentData(entity, new HealthBarTimer
                {
                    RemainingTime = 0f
                });
            }
        }

        void Update()
        {
            if (_done) return;

            _frameCount++;

            if (_frameCount <= warmupFrames) return;

            if (!_measuring)
            {
                _measuring = true;
                Debug.Log("[ECS Benchmark] Warmup complete. Measuring...");
            }

            float frameTime = Time.unscaledDeltaTime * 1000f; // ms

            if (_measureIndex < measureFrames)
            {
                _frameTimes[_measureIndex] = frameTime;
                _totalFrameTime += frameTime;
                _maxFrameTime = Mathf.Max(_maxFrameTime, frameTime);
                _minFrameTime = Mathf.Min(_minFrameTime, frameTime);
                _measureIndex++;
            }

            if (_measureIndex >= measureFrames)
            {
                _done = true;
                ReportResults();
            }
        }

        void ReportResults()
        {
            float avgFrameTime = _totalFrameTime / measureFrames;
            float avgFPS = 1000f / avgFrameTime;

            // Calculate P95 and P99
            System.Array.Sort(_frameTimes);
            float p50 = _frameTimes[measureFrames / 2];
            float p95 = _frameTimes[(int)(measureFrames * 0.95f)];
            float p99 = _frameTimes[(int)(measureFrames * 0.99f)];

            // Count alive enemies
            var query = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<EnemyTag>() },
                None = new[] { ComponentType.ReadOnly<DeadTag>() }
            });
            int aliveCount = query.CalculateEntityCount();

            string report = $@"
========================================
  ECS BENCHMARK RESULTS
========================================
  Entities:   {enemyCount} enemies + {towerCount} towers + {wallCount + buildingCount + 1} buildings
  Alive enemies at end: {aliveCount}
  Measured:   {measureFrames} frames (after {warmupFrames} warmup)
----------------------------------------
  Avg Frame Time:  {avgFrameTime:F2} ms ({avgFPS:F1} FPS)
  Min Frame Time:  {_minFrameTime:F2} ms
  Max Frame Time:  {_maxFrameTime:F2} ms
  P50 Frame Time:  {p50:F2} ms
  P95 Frame Time:  {p95:F2} ms
  P99 Frame Time:  {p99:F2} ms
----------------------------------------
  TARGET: ≤ 33.3 ms (30 FPS)
  RESULT: {(avgFrameTime <= 33.3f ? "PASS" : "FAIL")}
========================================";

            Debug.Log(report);
        }
    }
}
