using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ProjectSun.Defense.ECS;
using ProjectSun.V2.Data;
using ProjectSun.V2.Defense.ECS;
using FlowFieldConfig = ProjectSun.V2.Defense.ECS.FlowFieldConfig;
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

            // 싱글턴 엔티티들
            CreateBattleStatistics();
            CreateFlowFieldConfig();
            CreateWaveManager(gameState);

            // 웨이브 인프라
            CreateSpawnPoints();
            CreateInitialSpawnGroups(gameState);

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
            int combatCitizenCount = 0;
            for (int i = 0; i < gameState.citizens.Count; i++)
            {
                if (gameState.citizens[i].state == CitizenState.InCombat)
                    combatCitizenCount++;
            }

            int squadIndex = 0;
            for (int i = 0; i < gameState.citizens.Count; i++)
            {
                var citizen = gameState.citizens[i];
                if (citizen.state != CitizenState.InCombat)
                    continue;

                var entity = _entityManager.CreateEntity();
                _createdEntities.Add(entity);

                // 분대 위치: 기지 내부 방어선 (균등 배치)
                float angle = (float)squadIndex / Mathf.Max(combatCitizenCount, 1) * Mathf.PI * 2f;
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

        /// <summary>
        /// FlowFieldConfig 싱글턴 생성.
        /// FlowFieldSystem이 매 프레임 이 싱글턴을 읽어 경로 계산을 수행한다.
        /// </summary>
        void CreateFlowFieldConfig()
        {
            var entity = _entityManager.CreateEntity();
            _createdEntities.Add(entity);
            _entityManager.AddComponentData(entity, new FlowFieldConfig
            {
                GridWidth = 100,
                GridHeight = 100,
                CellSize = 1f,
                WorldOrigin = float3.zero,
                LastComputedFrame = -1,
                NeedsRecompute = true // 첫 프레임에 계산 트리거
            });
            CreatedEntityCount++;
        }

        /// <summary>
        /// WaveManager 싱글턴 생성.
        /// WaveSpawnSystem이 읽어 웨이브 타이밍과 진행을 관리한다.
        /// </summary>
        void CreateWaveManager(GameState gameState)
        {
            // 턴 번호에 따라 웨이브 수 증가 (초반 1~2, 중반 2~3, 후반 3)
            int totalWaves = gameState.currentTurn <= 5 ? 1
                           : gameState.currentTurn <= 15 ? 2
                           : 3;

            var entity = _entityManager.CreateEntity();
            _createdEntities.Add(entity);
            _entityManager.AddComponentData(entity, new WaveManager
            {
                CurrentWaveIndex = 0,
                TotalWaves = totalWaves,
                WaveTimer = 0f,
                NextWaveDelay = 5f,
                WaveActive = true,
                AllWavesComplete = false
            });
            CreatedEntityCount++;
        }

        /// <summary>
        /// 맵 가장자리 4방향에 스폰 포인트 생성.
        /// WaveSpawnSystem이 랜덤 스폰 포인트를 선택하여 적을 배치한다.
        /// </summary>
        void CreateSpawnPoints()
        {
            float mapEdge = 45f;
            var positions = new float3[]
            {
                new float3(mapEdge, 0, 0),   // 동
                new float3(-mapEdge, 0, 0),  // 서
                new float3(0, 0, mapEdge),   // 북
                new float3(0, 0, -mapEdge)   // 남
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var entity = _entityManager.CreateEntity();
                _createdEntities.Add(entity);
                _entityManager.AddComponentData(entity, LocalTransform.FromPosition(positions[i]));
                _entityManager.AddComponentData(entity, new SpawnPoint { Index = i });
                CreatedEntityCount++;
            }
        }

        /// <summary>
        /// 첫 번째 웨이브의 SpawnGroup 생성.
        /// 턴 번호에 따라 적 수량/스탯이 스케일링된다.
        /// GDD 기준: 적 HP x1.1/턴, 적 수량 x1.2/턴.
        /// </summary>
        void CreateInitialSpawnGroups(GameState gameState)
        {
            int turn = Mathf.Max(1, gameState.currentTurn);
            float hpScale = Mathf.Pow(1.1f, turn - 1);
            float countScale = Mathf.Pow(1.2f, turn - 1);

            // 기본 적 스폰 그룹 (유충 아키타입)
            int baseCount = Mathf.RoundToInt(10 * countScale);
            var basicGroup = _entityManager.CreateEntity();
            _createdEntities.Add(basicGroup);
            _entityManager.AddComponentData(basicGroup, new SpawnGroup
            {
                EnemyPrefab = Entity.Null,
                RemainingCount = baseCount,
                SpawnInterval = 0.5f,
                SpawnTimer = 0f,
                StatMultiplier = hpScale,
                EnemyType = 0, // Basic
                BaseHP = 30f,
                BaseSpeed = 3f,
                BaseDamage = 5f,
                BaseAttackRange = 2f,
                BaseAttackInterval = 1f
            });
            CreatedEntityCount++;

            // 턴 6 이후: 중장갑 적 추가
            if (turn >= 6)
            {
                int heavyCount = Mathf.RoundToInt(3 * countScale);
                var heavyGroup = _entityManager.CreateEntity();
                _createdEntities.Add(heavyGroup);
                _entityManager.AddComponentData(heavyGroup, new SpawnGroup
                {
                    EnemyPrefab = Entity.Null,
                    RemainingCount = heavyCount,
                    SpawnInterval = 1.5f,
                    SpawnTimer = 0f,
                    StatMultiplier = hpScale,
                    EnemyType = 1, // Heavy
                    BaseHP = 100f,
                    BaseSpeed = 1.5f,
                    BaseDamage = 15f,
                    BaseAttackRange = 2f,
                    BaseAttackInterval = 2f
                });
                CreatedEntityCount++;
            }
        }

        static bool IsTowerBuilding(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return false;
            return buildingId.Contains("tower") || buildingId.Contains("turret") || buildingId.Contains("cannon");
        }
    }
}
