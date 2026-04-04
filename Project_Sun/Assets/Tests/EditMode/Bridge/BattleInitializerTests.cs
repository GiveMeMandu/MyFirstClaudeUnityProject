using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ProjectSun.Defense.ECS;
using ProjectSun.V2.Data;
using ProjectSun.V2.Defense.Bridge;
using ProjectSun.V2.Defense.ECS;
using FlowFieldConfig = ProjectSun.V2.Defense.ECS.FlowFieldConfig;

namespace ProjectSun.Tests.EditMode.Bridge
{
    /// <summary>
    /// SF-WD-018: BattleInitializer MB → ECS 변환 정합성 단위 테스트.
    /// EditMode에서 World.DefaultGameObjectInjectionWorld를 직접 제어.
    /// </summary>
    [TestFixture]
    public class BattleInitializerTests
    {
        GameObject _go;
        BattleInitializer _initializer;
        EntityManager _em;

        [SetUp]
        public void SetUp()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            Assert.IsNotNull(world, "ECS World이 필요합니다. EditMode에서는 DefaultGameObjectInjectionWorld가 존재해야 합니다.");
            _em = world.EntityManager;

            _go = new GameObject("TestBattleInitializer");
            _initializer = _go.AddComponent<BattleInitializer>();
        }

        [TearDown]
        public void TearDown()
        {
            // 생성된 엔티티 정리
            _initializer.CleanupBattleEntities();

            // 남은 Enemy 엔티티도 정리
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var enemyQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>());
                _em.DestroyEntity(enemyQuery);
            }

            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        // -------------------------------------------------------------------------
        // 테스트 1: 건물 엔티티 수 + BuildingData 매핑
        // -------------------------------------------------------------------------

        [Test]
        public void InitializeBattle_CreatesCorrectBuildingEntities()
        {
            // Arrange
            var gameState = CreateGameState_HQPlusTowerPlusWall();

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: 활성 건물(Active/Damaged) 3개 모두 엔티티로 변환되어야 함
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingTag>(),
                ComponentType.ReadOnly<BuildingData>());
            int buildingEntityCount = query.CalculateEntityCount();

            Assert.That(buildingEntityCount, Is.EqualTo(3),
                "3개 활성 건물이 정확히 3개 ECS 엔티티로 변환되어야 합니다.");
        }

        [Test]
        public void InitializeBattle_BuildingData_MapsHPAndSlotIndex()
        {
            // Arrange
            var gameState = CreateGameState_HQPlusTowerPlusWall();

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: BuildingData.MaxHP, CurrentHP, SlotIndex 정합성
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingTag>(),
                ComponentType.ReadOnly<BuildingData>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            bool hqFound = false;
            foreach (var entity in entities)
            {
                var data = _em.GetComponentData<BuildingData>(entity);
                var source = gameState.buildings[data.SlotIndex];

                Assert.That(data.MaxHP, Is.EqualTo(source.maxHP).Within(0.01f),
                    $"슬롯 {data.SlotIndex}: MaxHP 불일치");
                Assert.That(data.CurrentHP, Is.EqualTo(source.currentHP).Within(0.01f),
                    $"슬롯 {data.SlotIndex}: CurrentHP 불일치");

                if (data.IsHeadquarters)
                    hqFound = true;
            }

            Assert.IsTrue(hqFound, "본부(IsHeadquarters=true) 엔티티가 존재해야 합니다.");
        }

        [Test]
        public void InitializeBattle_SkipsInactiveBuildings()
        {
            // Arrange: 3개 건물 중 1개를 Locked 상태로 설정
            var gameState = CreateGameState_HQPlusTowerPlusWall();
            gameState.buildings[2].state = BuildingSlotStateV2.Locked;

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: Active/Damaged 2개만 변환
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingTag>(),
                ComponentType.ReadOnly<BuildingData>());
            int count = query.CalculateEntityCount();

            Assert.That(count, Is.EqualTo(2),
                "Locked 건물은 엔티티 변환에서 제외되어야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 테스트 2: 타워 컴포넌트 (TowerTag + TowerStats)
        // -------------------------------------------------------------------------

        [Test]
        public void InitializeBattle_CreatesTowerComponents()
        {
            // Arrange: 타워 건물 포함 GameState
            var gameState = CreateGameState_HQPlusTowerPlusWall();

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: TowerTag + TowerStats가 있는 엔티티 1개 (tower_basic)
            var towerQuery = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingTag>(),
                ComponentType.ReadOnly<TowerTag>(),
                ComponentType.ReadOnly<TowerStats>());
            int towerCount = towerQuery.CalculateEntityCount();

            Assert.That(towerCount, Is.EqualTo(1),
                "타워 건물 1개가 TowerTag + TowerStats를 가져야 합니다.");
        }

        [Test]
        public void InitializeBattle_TowerStats_RangeAndDamageScaleWithUpgradeLevel()
        {
            // Arrange: upgradeLevel = 2인 타워
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0",
                buildingId = "tower_basic",
                state = BuildingSlotStateV2.Active,
                currentHP = 150,
                maxHP = 150,
                upgradeLevel = 2
            });

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: Range = 15 + 2*2 = 19, Damage = 10 + 2*3 = 16
            var towerQuery = _em.CreateEntityQuery(
                ComponentType.ReadOnly<TowerTag>(),
                ComponentType.ReadOnly<TowerStats>());
            using var entities = towerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            Assert.That(entities.Length, Is.EqualTo(1));
            var stats = _em.GetComponentData<TowerStats>(entities[0]);

            Assert.That(stats.Range, Is.EqualTo(19f).Within(0.01f),
                "Range = 15 + upgradeLevel*2 이어야 합니다.");
            Assert.That(stats.Damage, Is.EqualTo(16f).Within(0.01f),
                "Damage = 10 + upgradeLevel*3 이어야 합니다.");
            Assert.That(stats.AttackSpeed, Is.EqualTo(2f).Within(0.01f),
                "AttackSpeed 기본값 2f이어야 합니다.");
        }

        [Test]
        public void InitializeBattle_WallBuilding_HasNoTowerTag()
        {
            // Arrange: wall만 있는 GameState
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0",
                buildingId = "wall_basic",
                state = BuildingSlotStateV2.Active,
                currentHP = 300,
                maxHP = 300,
                upgradeLevel = 0
            });

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: BuildingTag 있음, TowerTag 없음
            var buildingQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<BuildingTag>());
            Assert.That(buildingQuery.CalculateEntityCount(), Is.EqualTo(1));

            var towerQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<TowerTag>());
            Assert.That(towerQuery.CalculateEntityCount(), Is.EqualTo(0),
                "벽 건물에는 TowerTag가 부착되지 않아야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 테스트 3: 분대 엔티티 (InCombat 시민 → SquadTag + SquadId + SquadStats)
        // -------------------------------------------------------------------------

        [Test]
        public void InitializeBattle_CreatesSquadEntities()
        {
            // Arrange: InCombat 시민 2명
            var gameState = CreateGameState_TwoCombatCitizens();

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: SquadTag 엔티티 2개
            var squadQuery = _em.CreateEntityQuery(
                ComponentType.ReadOnly<SquadTag>(),
                ComponentType.ReadOnly<SquadId>(),
                ComponentType.ReadOnly<SquadStats>());
            int count = squadQuery.CalculateEntityCount();

            Assert.That(count, Is.EqualTo(2),
                "InCombat 시민 2명이 정확히 2개의 분대 엔티티로 변환되어야 합니다.");
        }

        [Test]
        public void InitializeBattle_SquadStats_CombatPowerScalesWithProficiency()
        {
            // Arrange: proficiencyLevel = 3인 시민 1명
            var gameState = new GameState { currentTurn = 1 };
            gameState.citizens.Add(new CitizenRuntimeState
            {
                citizenId = "c_0",
                displayName = "Veteran",
                proficiencyLevel = 3,
                state = CitizenState.InCombat
            });

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: CombatPower = 10 + 3*5 = 25
            var squadQuery = _em.CreateEntityQuery(
                ComponentType.ReadOnly<SquadTag>(),
                ComponentType.ReadOnly<SquadStats>());
            using var entities = squadQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            Assert.That(entities.Length, Is.EqualTo(1));
            var stats = _em.GetComponentData<SquadStats>(entities[0]);

            Assert.That(stats.CombatPower, Is.EqualTo(25f).Within(0.01f),
                "CombatPower = 10 + proficiencyLevel*5 이어야 합니다.");
            Assert.That(stats.MaxHP, Is.EqualTo(100f).Within(0.01f),
                "MaxHP 기본값은 100f이어야 합니다.");
            Assert.That(stats.CurrentHP, Is.EqualTo(100f).Within(0.01f),
                "CurrentHP 초기값은 MaxHP(100f)이어야 합니다.");
        }

        [Test]
        public void InitializeBattle_SquadId_AssignedSequentially()
        {
            // Arrange: InCombat 시민 3명
            var gameState = new GameState { currentTurn = 1 };
            for (int i = 0; i < 3; i++)
            {
                gameState.citizens.Add(new CitizenRuntimeState
                {
                    citizenId = $"c_{i}",
                    proficiencyLevel = 1,
                    state = CitizenState.InCombat
                });
            }

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: SquadId.Value가 0, 1, 2
            var squadQuery = _em.CreateEntityQuery(
                ComponentType.ReadOnly<SquadTag>(),
                ComponentType.ReadOnly<SquadId>());
            using var entities = squadQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            var ids = new List<int>();
            foreach (var entity in entities)
                ids.Add(_em.GetComponentData<SquadId>(entity).Value);

            ids.Sort();
            Assert.That(ids, Is.EqualTo(new List<int> { 0, 1, 2 }),
                "SquadId는 0부터 순차 부여되어야 합니다.");
        }

        [Test]
        public void InitializeBattle_NonCombatCitizens_NotConverted()
        {
            // Arrange: Idle 시민 + InCombat 시민 혼합
            var gameState = new GameState { currentTurn = 1 };
            gameState.citizens.Add(new CitizenRuntimeState { citizenId = "idle_0", state = CitizenState.Idle });
            gameState.citizens.Add(new CitizenRuntimeState { citizenId = "combat_0", state = CitizenState.InCombat, proficiencyLevel = 1 });
            gameState.citizens.Add(new CitizenRuntimeState { citizenId = "assigned_0", state = CitizenState.Assigned });

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: InCombat 1명만 분대 엔티티 생성
            var squadQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<SquadTag>());
            Assert.That(squadQuery.CalculateEntityCount(), Is.EqualTo(1),
                "InCombat 상태인 시민만 분대 엔티티로 변환되어야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 테스트 4: 싱글턴 엔티티 (BattleStatistics + FlowFieldConfig + WaveManager)
        // -------------------------------------------------------------------------

        [Test]
        public void InitializeBattle_CreatesSingletons()
        {
            // Arrange
            var gameState = new GameState { currentTurn = 1 };

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: 각 싱글턴 엔티티 정확히 1개씩
            var statsQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<BattleStatistics>());
            Assert.That(statsQuery.CalculateEntityCount(), Is.EqualTo(1),
                "BattleStatistics 싱글턴이 정확히 1개 생성되어야 합니다.");

            var flowFieldQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<FlowFieldConfig>());
            Assert.That(flowFieldQuery.CalculateEntityCount(), Is.EqualTo(1),
                "FlowFieldConfig 싱글턴이 정확히 1개 생성되어야 합니다.");

            var waveManagerQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<WaveManager>());
            Assert.That(waveManagerQuery.CalculateEntityCount(), Is.EqualTo(1),
                "WaveManager 싱글턴이 정확히 1개 생성되어야 합니다.");
        }

        [Test]
        public void InitializeBattle_FlowFieldConfig_HasCorrectGridSettings()
        {
            // Arrange
            var gameState = new GameState { currentTurn = 1 };

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: GridWidth=100, GridHeight=100, CellSize=1f, NeedsRecompute=true
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<FlowFieldConfig>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var config = _em.GetComponentData<FlowFieldConfig>(entities[0]);

            Assert.That(config.GridWidth, Is.EqualTo(100),
                "FlowFieldConfig.GridWidth = 100 이어야 합니다.");
            Assert.That(config.GridHeight, Is.EqualTo(100),
                "FlowFieldConfig.GridHeight = 100 이어야 합니다.");
            Assert.That(config.CellSize, Is.EqualTo(1f).Within(0.001f),
                "FlowFieldConfig.CellSize = 1f 이어야 합니다.");
            Assert.IsTrue(config.NeedsRecompute,
                "FlowFieldConfig.NeedsRecompute는 초기값 true이어야 합니다.");
        }

        [Test]
        public void InitializeBattle_WaveManager_WaveCountScalesWithTurn_EarlyGame()
        {
            // Arrange: 턴 3 (초반 — 1웨이브 기대)
            var gameState = new GameState { currentTurn = 3 };

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<WaveManager>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var wm = _em.GetComponentData<WaveManager>(entities[0]);

            Assert.That(wm.TotalWaves, Is.EqualTo(1),
                "턴 1~5에서는 TotalWaves = 1 이어야 합니다.");
            Assert.IsTrue(wm.WaveActive,
                "WaveManager.WaveActive는 초기값 true이어야 합니다.");
        }

        [Test]
        public void InitializeBattle_WaveManager_WaveCountScalesWithTurn_MidGame()
        {
            // Arrange: 턴 10 (중반 — 2웨이브 기대)
            var gameState = new GameState { currentTurn = 10 };

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<WaveManager>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var wm = _em.GetComponentData<WaveManager>(entities[0]);

            Assert.That(wm.TotalWaves, Is.EqualTo(2),
                "턴 6~15에서는 TotalWaves = 2 이어야 합니다.");
        }

        [Test]
        public void InitializeBattle_WaveManager_WaveCountScalesWithTurn_LateGame()
        {
            // Arrange: 턴 20 (후반 — 3웨이브 기대)
            var gameState = new GameState { currentTurn = 20 };

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<WaveManager>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var wm = _em.GetComponentData<WaveManager>(entities[0]);

            Assert.That(wm.TotalWaves, Is.EqualTo(3),
                "턴 16+ 에서는 TotalWaves = 3 이어야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 테스트 5: SpawnPoint 엔티티 4개
        // -------------------------------------------------------------------------

        [Test]
        public void InitializeBattle_CreatesSpawnPoints()
        {
            // Arrange
            var gameState = new GameState { currentTurn = 1 };

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: SpawnPoint 엔티티 정확히 4개 (동/서/남/북)
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<SpawnPoint>());
            int count = query.CalculateEntityCount();

            Assert.That(count, Is.EqualTo(4),
                "스폰 포인트는 4방향(동/서/남/북) 정확히 4개여야 합니다.");
        }

        [Test]
        public void InitializeBattle_SpawnPoints_HaveUniqueIndices()
        {
            // Arrange
            var gameState = new GameState { currentTurn = 1 };

            // Act
            _initializer.InitializeBattle(gameState);

            // Assert: Index 0~3 고유
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<SpawnPoint>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            var indices = new HashSet<int>();
            foreach (var entity in entities)
            {
                int idx = _em.GetComponentData<SpawnPoint>(entity).Index;
                Assert.IsTrue(indices.Add(idx),
                    $"SpawnPoint.Index {idx}가 중복됩니다.");
            }

            Assert.That(indices.Count, Is.EqualTo(4));
        }

        // -------------------------------------------------------------------------
        // 테스트 6: CleanupBattleEntities — 모든 엔티티 제거
        // -------------------------------------------------------------------------

        [Test]
        public void CleanupBattleEntities_RemovesAllCreatedEntities()
        {
            // Arrange: 건물 + 분대 포함하여 초기화
            var gameState = CreateGameState_HQPlusTowerPlusWall();
            gameState.citizens.Add(new CitizenRuntimeState
            {
                citizenId = "c_0",
                proficiencyLevel = 1,
                state = CitizenState.InCombat
            });
            _initializer.InitializeBattle(gameState);

            // Precondition: 엔티티가 생성되었음을 확인
            Assert.That(_initializer.CreatedEntityCount, Is.GreaterThan(0),
                "초기화 후 생성된 엔티티가 있어야 합니다.");

            // Act
            _initializer.CleanupBattleEntities();

            // Assert: BuildingTag, SquadTag, BattleStatistics 엔티티 0개
            var buildingQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<BuildingTag>());
            Assert.That(buildingQuery.CalculateEntityCount(), Is.EqualTo(0),
                "Cleanup 후 BuildingTag 엔티티가 없어야 합니다.");

            var squadQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<SquadTag>());
            Assert.That(squadQuery.CalculateEntityCount(), Is.EqualTo(0),
                "Cleanup 후 SquadTag 엔티티가 없어야 합니다.");

            var statsQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<BattleStatistics>());
            Assert.That(statsQuery.CalculateEntityCount(), Is.EqualTo(0),
                "Cleanup 후 BattleStatistics 엔티티가 없어야 합니다.");
        }

        [Test]
        public void CleanupBattleEntities_CalledTwice_DoesNotThrow()
        {
            // Arrange
            var gameState = new GameState { currentTurn = 1 };
            _initializer.InitializeBattle(gameState);

            // Act & Assert: 두 번 호출해도 예외 없음
            Assert.DoesNotThrow(() =>
            {
                _initializer.CleanupBattleEntities();
                _initializer.CleanupBattleEntities();
            }, "CleanupBattleEntities를 두 번 호출해도 예외가 발생하지 않아야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 헬퍼 메서드
        // -------------------------------------------------------------------------

        /// <summary>HQ 1개 + tower_basic 1개 + wall_basic 1개를 가진 GameState.</summary>
        static GameState CreateGameState_HQPlusTowerPlusWall()
        {
            var gs = new GameState { currentTurn = 1 };
            gs.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0",
                buildingId = "headquarters",
                state = BuildingSlotStateV2.Active,
                currentHP = 500,
                maxHP = 500,
                upgradeLevel = 0
            });
            gs.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_1",
                buildingId = "tower_basic",
                state = BuildingSlotStateV2.Active,
                currentHP = 150,
                maxHP = 150,
                upgradeLevel = 1
            });
            gs.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_2",
                buildingId = "wall_basic",
                state = BuildingSlotStateV2.Active,
                currentHP = 300,
                maxHP = 300,
                upgradeLevel = 0
            });
            return gs;
        }

        /// <summary>InCombat 시민 2명을 가진 GameState.</summary>
        static GameState CreateGameState_TwoCombatCitizens()
        {
            var gs = new GameState { currentTurn = 1 };
            for (int i = 0; i < 2; i++)
            {
                gs.citizens.Add(new CitizenRuntimeState
                {
                    citizenId = $"c_{i}",
                    displayName = $"Soldier {i}",
                    proficiencyLevel = 2,
                    state = CitizenState.InCombat
                });
            }
            return gs;
        }
    }
}
