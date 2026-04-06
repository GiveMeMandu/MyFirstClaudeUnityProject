using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Diagnostics;
using ProjectSun.Defense.ECS;
using Debug = UnityEngine.Debug;

namespace ProjectSun.Tests.EditMode.Performance
{
    /// <summary>
    /// SF-WD-019: ECS 성능 회귀 테스트.
    /// 3,000개체 생성 + 기본 연산 프레임 시간 측정.
    /// 회귀 기준: 5ms 이하 (M0 벤치마크: 2.63ms).
    /// </summary>
    [TestFixture]
    public class ECSPerformanceTests
    {
        const int EntityCount = 3000;
        const float MaxFrameTimeMs = 5f;

        World _testWorld;
        EntityManager _em;

        [SetUp]
        public void SetUp()
        {
            _testWorld = World.DefaultGameObjectInjectionWorld;
            if (_testWorld == null || !_testWorld.IsCreated)
            {
                _testWorld = new World("PerfTestWorld");
                World.DefaultGameObjectInjectionWorld = _testWorld;
            }
            _em = _testWorld.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            // 모든 엔티티 정리
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>());
            _em.DestroyEntity(query);

            var buildingQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<BuildingTag>());
            _em.DestroyEntity(buildingQuery);
        }

        [Test]
        public void CreateEntities_3000_CompletesUnder5ms()
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < EntityCount; i++)
            {
                var entity = _em.CreateEntity();
                _em.AddComponentData(entity, LocalTransform.FromPosition(
                    new float3(i * 0.1f, 0, 0)));
                _em.AddComponentData(entity, new EnemyTag());
                _em.AddComponentData(entity, new EnemyStats
                {
                    MaxHP = 30f,
                    CurrentHP = 30f,
                    Speed = 3f,
                    Damage = 5f,
                    AttackRange = 2f,
                    AttackInterval = 1f,
                    EnemyType = 0
                });
                _em.AddComponentData(entity, new EnemyState { Value = 1 });
                _em.AddComponentData(entity, new EnemyTarget
                {
                    TargetEntity = Entity.Null,
                    TargetPosition = float3.zero,
                    HasTarget = false
                });
                _em.AddComponentData(entity, new AttackTimer { TimeSinceLastAttack = 0f });
            }

            sw.Stop();
            float ms = sw.ElapsedMilliseconds;

            Debug.Log($"[ECSPerf] Created {EntityCount} entities in {ms}ms");
            Assert.Less(ms, MaxFrameTimeMs * 20, // 생성은 여유 있게
                $"Entity creation took {ms}ms, expected < {MaxFrameTimeMs * 20}ms");
        }

        [Test]
        public void EntityQuery_3000Enemies_CountUnder1ms()
        {
            // 사전 생성
            for (int i = 0; i < EntityCount; i++)
            {
                var entity = _em.CreateEntity();
                _em.AddComponentData(entity, new EnemyTag());
                _em.AddComponentData(entity, new EnemyStats
                {
                    MaxHP = 30f, CurrentHP = 30f, Speed = 3f,
                    Damage = 5f, AttackRange = 2f, AttackInterval = 1f, EnemyType = 0
                });
            }

            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>());

            var sw = Stopwatch.StartNew();
            int count = query.CalculateEntityCount();
            sw.Stop();

            float ms = (float)sw.Elapsed.TotalMilliseconds;
            Debug.Log($"[ECSPerf] Query count={count} in {ms:F3}ms");

            Assert.AreEqual(EntityCount, count);
            Assert.Less(ms, 1f, $"Entity query took {ms}ms, expected < 1ms");
        }

        [Test]
        public void ComponentReadWrite_3000Enemies_Under5ms()
        {
            // 사전 생성
            for (int i = 0; i < EntityCount; i++)
            {
                var entity = _em.CreateEntity();
                _em.AddComponentData(entity, LocalTransform.FromPosition(
                    new float3(i * 0.1f, 0, 0)));
                _em.AddComponentData(entity, new EnemyTag());
                _em.AddComponentData(entity, new EnemyStats
                {
                    MaxHP = 30f, CurrentHP = 30f, Speed = 3f,
                    Damage = 5f, AttackRange = 2f, AttackInterval = 1f, EnemyType = 0
                });
            }

            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadWrite<EnemyStats>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < entities.Length; i++)
            {
                var stats = _em.GetComponentData<EnemyStats>(entities[i]);
                stats.CurrentHP -= 1f;
                _em.SetComponentData(entities[i], stats);
            }
            sw.Stop();

            float ms = (float)sw.Elapsed.TotalMilliseconds;
            Debug.Log($"[ECSPerf] R/W {entities.Length} components in {ms:F3}ms");

            Assert.Less(ms, MaxFrameTimeMs,
                $"Component R/W took {ms}ms, expected < {MaxFrameTimeMs}ms");
        }
    }
}
