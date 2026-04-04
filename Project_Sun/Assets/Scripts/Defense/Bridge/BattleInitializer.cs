using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ProjectSun.Defense.ECS;
using ProjectSun.V2.Data;
using ProjectSun.V2.Defense.ECS;
using Debug = UnityEngine.Debug;

namespace ProjectSun.V2.Defense.Bridge
{
    /// <summary>
    /// 낮→밤 브릿지: MonoBehaviour 건물/분대 데이터를 ECS Entity로 변환.
    /// Interface Contract: DefenseBuildingStats, SquadDeployed 이행.
    /// </summary>
    public class BattleInitializer : MonoBehaviour
    {
        EntityManager _entityManager;
        NativeList<Entity> _createdEntities;

        /// <summary>생성된 ECS 엔티티 수 (검증용).</summary>
        public int CreatedEntityCount { get; private set; }

        /// <summary>초기화 소요 시간(ms).</summary>
        public long InitializeTimeMs { get; private set; }

        /// <summary>
        /// GameState의 건물/분대 데이터를 ECS World에 엔티티로 변환.
        /// </summary>
        public void InitializeBattle(GameState gameState)
        {
            var sw = Stopwatch.StartNew();

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("[BattleInitializer] ECS World not available");
                return;
            }

            _entityManager = world.EntityManager;

            if (_createdEntities.IsCreated)
                _createdEntities.Dispose();
            _createdEntities = new NativeList<Entity>(64, Allocator.Persistent);

            CreatedEntityCount = 0;

            // 건물 → ECS Entity
            CreateBuildingEntities(gameState);

            // 분대 → ECS Entity
            CreateSquadEntities(gameState);

            // BattleStatistics 싱글턴
            CreateBattleStatistics();

            sw.Stop();
            InitializeTimeMs = sw.ElapsedMilliseconds;

            Debug.Log($"[BattleInitializer] Created {CreatedEntityCount} entities in {InitializeTimeMs}ms");
        }

        void CreateBuildingEntities(GameState gameState)
        {
            for (int i = 0; i < gameState.buildings.Count; i++)
            {
                var building = gameState.buildings[i];

                // 비활성 건물은 스킵
                if (building.state != BuildingSlotStateV2.Active &&
                    building.state != BuildingSlotStateV2.Damaged)
                    continue;

                var entity = _entityManager.CreateEntity();
                _createdEntities.Add(entity);

                // 원형 배치 (슬롯 인덱스 기반)
                float angle = (float)i / Mathf.Max(gameState.buildings.Count, 1) * Mathf.PI * 2f;
                float radius = 15f;
                float3 pos = new float3(math.cos(angle) * radius, 0, math.sin(angle) * radius);

                _entityManager.AddComponentData(entity, LocalTransform.FromPosition(pos));
                _entityManager.AddComponentData(entity, new BuildingTag());

                bool isHQ = building.buildingId == "headquarters";
                bool isWall = building.buildingId != null && building.buildingId.Contains("wall");

                _entityManager.AddComponentData(entity, new BuildingData
                {
                    MaxHP = building.maxHP,
                    CurrentHP = building.currentHP,
                    IsHeadquarters = isHQ,
                    IsWall = isWall,
                    SlotIndex = i
                });

                _entityManager.AddBuffer<BuildingDamageBuffer>(entity);

                // 방어 건물 → TowerStats 추가 (DefenseBuildingStats 계약)
                if (IsTowerBuilding(building.buildingId))
                {
                    _entityManager.AddComponentData(entity, new TowerTag());
                    _entityManager.AddComponentData(entity, new TowerStats
                    {
                        Range = 15f + building.upgradeLevel * 2f,
                        Damage = 10f + building.upgradeLevel * 3f,
                        AttackSpeed = 2f,
                        CanTargetAir = false
                    });
                    _entityManager.AddComponentData(entity, new TowerAttackTimer
                    {
                        TimeSinceLastAttack = 0f
                    });
                }

                CreatedEntityCount++;
            }
        }

        void CreateSquadEntities(GameState gameState)
        {
            // 전투 배치된 시민 → 분대 엔티티 (SquadDeployed 계약)
            int squadIndex = 0;
            for (int i = 0; i < gameState.citizens.Count; i++)
            {
                var citizen = gameState.citizens[i];
                if (citizen.state != CitizenState.InCombat)
                    continue;

                var entity = _entityManager.CreateEntity();
                _createdEntities.Add(entity);

                // 분대 위치: 기지 내부 방어선
                float angle = (float)squadIndex / Mathf.Max(1, 1) * Mathf.PI * 2f;
                float3 pos = new float3(math.cos(angle) * 10f, 0, math.sin(angle) * 10f);

                _entityManager.AddComponentData(entity, LocalTransform.FromPosition(pos));
                _entityManager.AddComponentData(entity, new SquadTag());
                _entityManager.AddComponentData(entity, new SquadId { Value = squadIndex });

                float combatPower = 10f + citizen.proficiencyLevel * 5f;
                _entityManager.AddComponentData(entity, new SquadStats
                {
                    CombatPower = combatPower,
                    AttackRange = 8f,
                    AttackSpeed = 1.5f,
                    MoveSpeed = 4f,
                    MaxHP = 100f,
                    CurrentHP = 100f,
                    MemberCount = 5
                });
                _entityManager.AddComponentData(entity, new SquadCommand
                {
                    Type = SquadCommandType.HoldPosition,
                    TargetPosition = pos,
                    TargetEntity = Entity.Null,
                    IssuedTime = 0
                });
                _entityManager.AddComponentData(entity, new SquadAttackTimer
                {
                    TimeSinceLastAttack = 0f
                });
                _entityManager.AddComponentData(entity, new SquadSelected { Value = false });

                CreatedEntityCount++;
                squadIndex++;
            }
        }

        void CreateBattleStatistics()
        {
            var entity = _entityManager.CreateEntity();
            _createdEntities.Add(entity);
            _entityManager.AddComponentData(entity, new BattleStatistics
            {
                TotalEnemiesSpawned = 0,
                TotalEnemiesKilled = 0,
                RemainingEnemies = 0,
                TotalDamageToBuildings = 0f
            });
        }

        /// <summary>밤 종료 시 생성한 엔티티 정리.</summary>
        public void CleanupBattleEntities()
        {
            if (!_createdEntities.IsCreated) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            for (int i = 0; i < _createdEntities.Length; i++)
            {
                if (em.Exists(_createdEntities[i]))
                    em.DestroyEntity(_createdEntities[i]);
            }

            // 남은 적 엔티티도 정리
            var enemyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>());
            em.DestroyEntity(enemyQuery);

            _createdEntities.Dispose();
            Debug.Log("[BattleInitializer] Battle entities cleaned up");
        }

        void OnDestroy()
        {
            if (_createdEntities.IsCreated)
                _createdEntities.Dispose();
        }

        static bool IsTowerBuilding(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return false;
            return buildingId.Contains("tower") || buildingId.Contains("turret") || buildingId.Contains("cannon");
        }
    }
}
