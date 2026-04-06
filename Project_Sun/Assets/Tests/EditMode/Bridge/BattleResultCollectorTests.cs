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

namespace ProjectSun.Tests.EditMode.Bridge
{
    /// <summary>
    /// SF-WD-018: BattleResultCollector ECS → MB 결과 수집 정합성 단위 테스트.
    /// ECS World에 수동으로 엔티티를 세팅하고 CollectResults의 반영 로직을 검증한다.
    /// </summary>
    [TestFixture]
    public class BattleResultCollectorTests
    {
        World _testWorld;
        GameObject _go;
        BattleResultCollector _collector;
        EntityManager _em;

        // 테스트 중 생성한 엔티티를 추적하여 TearDown에서 정리
        readonly List<Entity> _entities = new();

        [SetUp]
        public void SetUp()
        {
            // EditMode에서는 DefaultGameObjectInjectionWorld가 null이므로 자체 World 생성
            _testWorld = World.DefaultGameObjectInjectionWorld;
            if (_testWorld == null || !_testWorld.IsCreated)
            {
                _testWorld = new World("TestWorld");
                World.DefaultGameObjectInjectionWorld = _testWorld;
            }
            _em = _testWorld.EntityManager;
            _entities.Clear();

            _go = new GameObject("TestBattleResultCollector");
            _collector = _go.AddComponent<BattleResultCollector>();
        }

        [TearDown]
        public void TearDown()
        {
            // 테스트에서 직접 생성한 엔티티 정리
            foreach (var entity in _entities)
            {
                if (_em.Exists(entity))
                    _em.DestroyEntity(entity);
            }
            _entities.Clear();

            if (_go != null)
                Object.DestroyImmediate(_go);

            // 테스트용 World 정리
            if (_testWorld != null && _testWorld.IsCreated)
            {
                _testWorld.Dispose();
                World.DefaultGameObjectInjectionWorld = null;
            }
        }

        // -------------------------------------------------------------------------
        // 테스트 7: 건물 HP 변동 → GameState 반영
        // -------------------------------------------------------------------------

        [Test]
        public void CollectResults_SyncsBuildings_HP()
        {
            // Arrange
            var gameState = CreateGameState_SingleBuilding(slotId: "slot_0", isHQ: false,
                maxHP: 200, currentHP: 200);

            // ECS 엔티티: HP를 150으로 낮춘 상태 (50 데미지)
            CreateBuildingEntity(slotIndex: 0, maxHP: 200f, currentHP: 150f, isHQ: false);
            CreateBattleStatisticsSingleton(spawned: 0, killed: 0);

            // Act
            _collector.CollectResults(gameState);

            // Assert: GameState.buildings[0].currentHP == 150
            Assert.That(gameState.buildings[0].currentHP, Is.EqualTo(150),
                "ECS BuildingData.CurrentHP(150)이 GameState.buildings[0].currentHP에 반영되어야 합니다.");
        }

        [Test]
        public void CollectResults_UnchangedHP_NotMarkedAsDamaged()
        {
            // Arrange: HP 변동 없음
            var gameState = CreateGameState_SingleBuilding("slot_0", false, 200, 200);
            CreateBuildingEntity(slotIndex: 0, maxHP: 200f, currentHP: 200f, isHQ: false);
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert: damagedBuildingSlotIds 비어있어야 함
            Assert.That(result.damagedBuildingSlotIds, Is.Empty,
                "HP 변동이 없으면 손상 슬롯 목록이 비어 있어야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 테스트 8: 손상 건물 → damagedBuildingSlotIds 추가
        // -------------------------------------------------------------------------

        [Test]
        public void CollectResults_DetectsDamagedBuildings()
        {
            // Arrange: 2개 건물, 1개만 HP 감소
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "tower_basic",
                state = BuildingSlotStateV2.Active,
                currentHP = 150, maxHP = 150
            });
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_1", buildingId = "barracks",
                state = BuildingSlotStateV2.Active,
                currentHP = 200, maxHP = 200
            });

            // ECS: slot_0만 데미지 받음 (100 → 80)
            CreateBuildingEntity(slotIndex: 0, maxHP: 150f, currentHP: 80f, isHQ: false);
            CreateBuildingEntity(slotIndex: 1, maxHP: 200f, currentHP: 200f, isHQ: false);
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert
            Assert.That(result.damagedBuildingSlotIds, Contains.Item("slot_0"),
                "HP가 감소한 슬롯 ID는 damagedBuildingSlotIds에 포함되어야 합니다.");
            Assert.That(result.damagedBuildingSlotIds, Does.Not.Contain("slot_1"),
                "HP 변동 없는 슬롯은 damagedBuildingSlotIds에 포함되면 안 됩니다.");
        }

        [Test]
        public void CollectResults_MultipleDamagedBuildings_AllReported()
        {
            // Arrange: 3개 건물, 2개 손상
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "hq",
                state = BuildingSlotStateV2.Active, currentHP = 500, maxHP = 500
            });
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_1", buildingId = "tower_a",
                state = BuildingSlotStateV2.Active, currentHP = 150, maxHP = 150
            });
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_2", buildingId = "tower_b",
                state = BuildingSlotStateV2.Active, currentHP = 150, maxHP = 150
            });

            CreateBuildingEntity(0, 500f, 400f, isHQ: false); // 100 데미지
            CreateBuildingEntity(1, 150f, 100f, isHQ: false); // 50 데미지
            CreateBuildingEntity(2, 150f, 150f, isHQ: false); // 데미지 없음
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert
            Assert.That(result.damagedBuildingSlotIds.Count, Is.EqualTo(2),
                "손상된 건물 2개가 모두 보고되어야 합니다.");
            Assert.That(result.damagedBuildingSlotIds, Contains.Item("slot_0"));
            Assert.That(result.damagedBuildingSlotIds, Contains.Item("slot_1"));
        }

        // -------------------------------------------------------------------------
        // 테스트 9: HP = 0 → 건물 상태 Damaged 전환
        // -------------------------------------------------------------------------

        [Test]
        public void CollectResults_BuildingHPZero_StateTransitionsToDamaged()
        {
            // Arrange: HP=200인 Active 건물이 HP=0으로 감소
            var gameState = CreateGameState_SingleBuilding("slot_0", false, 200, 200);

            CreateBuildingEntity(slotIndex: 0, maxHP: 200f, currentHP: 0f, isHQ: false);
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            _collector.CollectResults(gameState);

            // Assert: 상태가 Damaged로 전환 (BuildingSlotStateV2에 Destroyed 없음)
            Assert.That(gameState.buildings[0].state, Is.EqualTo(BuildingSlotStateV2.Damaged),
                "HP가 0으로 떨어진 Active 건물은 Damaged 상태로 전환되어야 합니다.");
        }

        [Test]
        public void CollectResults_BuildingHPNegative_ClampedToZero()
        {
            // Arrange: ECS에서 HP가 음수로 내려간 엣지 케이스
            var gameState = CreateGameState_SingleBuilding("slot_0", false, 200, 200);

            CreateBuildingEntity(slotIndex: 0, maxHP: 200f, currentHP: -50f, isHQ: false);
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            _collector.CollectResults(gameState);

            // Assert: currentHP는 0 이하로 내려가지 않음
            Assert.That(gameState.buildings[0].currentHP, Is.GreaterThanOrEqualTo(0),
                "건물 HP는 0 미만으로 내려가지 않아야 합니다 (하한 클램핑).");
        }

        [Test]
        public void CollectResults_AlreadyDamagedBuilding_StateNotDowngraded()
        {
            // Arrange: 이미 Damaged 상태인 건물 — 추가 피해를 받아도 상태는 유지
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0",
                buildingId = "tower_a",
                state = BuildingSlotStateV2.Damaged, // 이미 Damaged
                currentHP = 80,
                maxHP = 150
            });

            CreateBuildingEntity(0, 150f, 50f, isHQ: false);
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            _collector.CollectResults(gameState);

            // Assert: Damaged 상태 유지 (Active → Damaged 전환 로직은 Active일 때만 실행)
            Assert.That(gameState.buildings[0].state, Is.EqualTo(BuildingSlotStateV2.Damaged),
                "이미 Damaged 상태인 건물은 상태가 변하지 않아야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 테스트 10: 본부 파괴 → headquartersDestroyed = true
        // -------------------------------------------------------------------------

        [Test]
        public void CollectResults_DetectsHQDestruction()
        {
            // Arrange: 본부 HP = 0
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_hq",
                buildingId = "headquarters",
                state = BuildingSlotStateV2.Active,
                currentHP = 500,
                maxHP = 500
            });

            CreateBuildingEntity(slotIndex: 0, maxHP: 500f, currentHP: 0f, isHQ: true);
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert
            Assert.IsTrue(result.headquartersDestroyed,
                "본부 HP가 0이 되면 WaveResult.headquartersDestroyed = true 이어야 합니다.");
        }

        [Test]
        public void CollectResults_HQIntact_HeadquartersDestroyedFalse()
        {
            // Arrange: 본부 HP 유지
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_hq",
                buildingId = "headquarters",
                state = BuildingSlotStateV2.Active,
                currentHP = 500,
                maxHP = 500
            });

            CreateBuildingEntity(slotIndex: 0, maxHP: 500f, currentHP: 450f, isHQ: true);
            CreateBattleStatisticsSingleton(10, 8);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert
            Assert.IsFalse(result.headquartersDestroyed,
                "본부 HP가 남아있으면 headquartersDestroyed = false 이어야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 테스트 11: 완벽 방어 (isPerfectDefense)
        // -------------------------------------------------------------------------

        [Test]
        public void CollectResults_PerfectDefense_NoDamageToAnyBuilding()
        {
            // Arrange: 건물 HP 전혀 변하지 않음
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "tower_a",
                state = BuildingSlotStateV2.Active, currentHP = 200, maxHP = 200
            });
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_1", buildingId = "tower_b",
                state = BuildingSlotStateV2.Active, currentHP = 200, maxHP = 200
            });

            CreateBuildingEntity(0, 200f, 200f, isHQ: false);
            CreateBuildingEntity(1, 200f, 200f, isHQ: false);
            CreateBattleStatisticsSingleton(10, 10);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert
            Assert.IsTrue(result.isPerfectDefense,
                "건물 데미지 비율 0% (무피해)이면 isPerfectDefense = true 이어야 합니다.");
        }

        [Test]
        public void CollectResults_PerfectDefense_DamageRatioBelowThreshold()
        {
            // Arrange: 총 MaxHP=1000, 피해=50 → damageRatio=5% (≤10%)
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "hq",
                state = BuildingSlotStateV2.Active, currentHP = 1000, maxHP = 1000
            });

            CreateBuildingEntity(0, 1000f, 950f, isHQ: false); // 50 데미지 (5%)
            CreateBattleStatisticsSingleton(10, 10);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert: damageRatio 5% ≤ 10% → 완벽 방어
            Assert.IsTrue(result.isPerfectDefense,
                "데미지 비율 5%(≤10%)이면 isPerfectDefense = true 이어야 합니다.");
        }

        [Test]
        public void CollectResults_NotPerfectDefense_DamageRatioAboveThreshold()
        {
            // Arrange: 총 MaxHP=200, 피해=40 → damageRatio=20% (>10%)
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "tower_a",
                state = BuildingSlotStateV2.Active, currentHP = 200, maxHP = 200
            });

            CreateBuildingEntity(0, 200f, 160f, isHQ: false); // 40 데미지 (20%)
            CreateBattleStatisticsSingleton(10, 5);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert: damageRatio 20% > 10% → 완벽 방어 아님
            Assert.IsFalse(result.isPerfectDefense,
                "데미지 비율 20%(>10%)이면 isPerfectDefense = false 이어야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 테스트 12: 보상 계산
        // -------------------------------------------------------------------------

        [Test]
        public void CollectResults_RewardCalculation_FullKillRatio_MaxBasicReward()
        {
            // Arrange: 10/10 적 처치 (defenseRatio = 1.0)
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "hq",
                state = BuildingSlotStateV2.Active, currentHP = 500, maxHP = 500
            });

            CreateBuildingEntity(0, 500f, 500f, isHQ: true);
            CreateBattleStatisticsSingleton(spawned: 10, killed: 10);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert: PerfectDefense + defenseRatio=1.0 → Lerp(10,15,1.0) = 15
            Assert.That(result.basicReward, Is.EqualTo(15),
                "적 100% 처치 + 완벽 방어 시 basicReward = 15 이어야 합니다 (Economy-Model).");
        }

        [Test]
        public void CollectResults_RewardCalculation_HalfKillRatio_HalfBasicReward()
        {
            // Arrange: 5/10 적 처치 (defenseRatio = 0.5)
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "hq",
                state = BuildingSlotStateV2.Active, currentHP = 500, maxHP = 500
            });

            CreateBuildingEntity(0, 500f, 500f, isHQ: true);
            CreateBattleStatisticsSingleton(spawned: 10, killed: 5);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert: PerfectDefense + defenseRatio=0.5 → Lerp(10,15,0.5) = 12~13
            Assert.That(result.basicReward, Is.InRange(12, 13),
                "적 50% 처치 + 완벽 방어 시 basicReward ≈ 12~13 이어야 합니다 (Economy-Model).");
        }

        [Test]
        public void CollectResults_RewardCalculation_PerfectDefense_GrantsAdvancedReward()
        {
            // Arrange: 무피해 + 10/10 처치
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "hq",
                state = BuildingSlotStateV2.Active, currentHP = 500, maxHP = 500
            });

            CreateBuildingEntity(0, 500f, 500f, isHQ: true);
            CreateBattleStatisticsSingleton(10, 10);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert: isPerfectDefense=true → advancedReward = Lerp(4,6,1.0) = 6
            Assert.IsTrue(result.isPerfectDefense);
            Assert.That(result.advancedReward, Is.EqualTo(6),
                "완벽 방어 + 100% 처치 시 advancedReward = 6 이어야 합니다 (Economy-Model).");
        }

        [Test]
        public void CollectResults_RewardCalculation_ImperfectDefense_NoAdvancedReward()
        {
            // Arrange: 피해 받음 (>10%)
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "tower_a",
                state = BuildingSlotStateV2.Active, currentHP = 200, maxHP = 200
            });

            CreateBuildingEntity(0, 200f, 100f, isHQ: false); // 50% 데미지
            CreateBattleStatisticsSingleton(10, 10);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert: isPerfectDefense=false → advancedReward = 0
            Assert.IsFalse(result.isPerfectDefense);
            Assert.That(result.advancedReward, Is.EqualTo(0),
                "완벽 방어 아닌 경우 advancedReward = 0 이어야 합니다.");
        }

        [Test]
        public void CollectResults_RewardCalculation_NoEnemiesSpawned_DefenseRatioIsOne()
        {
            // Arrange: 적이 0명 스폰된 경우 (defenseRatio = 1f 폴백)
            var gameState = new GameState { currentTurn = 1 };
            gameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0", buildingId = "hq",
                state = BuildingSlotStateV2.Active, currentHP = 500, maxHP = 500
            });

            CreateBuildingEntity(0, 500f, 500f, isHQ: true);
            CreateBattleStatisticsSingleton(spawned: 0, killed: 0);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert: enemiesTotal=0 → defenseRatio=1f → PerfectDefense Lerp(10,15,1.0)=15
            Assert.That(result.basicReward, Is.EqualTo(15),
                "적이 없는 경우 defenseRatio=1(폴백) → basicReward=15 이어야 합니다 (Economy-Model).");
        }

        // -------------------------------------------------------------------------
        // 테스트: 분대 HP 50% 미만 → 시민 Injured
        // -------------------------------------------------------------------------

        [Test]
        public void CollectResults_SquadHPBelowHalf_CitizenBecomesInjured()
        {
            // Arrange: InCombat 시민 1명 + 해당 분대 HP 30/100 (30%)
            var gameState = new GameState { currentTurn = 1 };
            gameState.citizens.Add(new CitizenRuntimeState
            {
                citizenId = "c_0",
                displayName = "Fighter",
                proficiencyLevel = 1,
                state = CitizenState.InCombat
            });

            CreateSquadEntity(squadIndex: 0, maxHP: 100f, currentHP: 30f);
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            _collector.CollectResults(gameState);

            // Assert
            Assert.That(gameState.citizens[0].state, Is.EqualTo(CitizenState.Injured),
                "분대 HP가 50% 미만이면 해당 시민은 Injured 상태가 되어야 합니다.");
        }

        [Test]
        public void CollectResults_SquadHPAboveHalf_CitizenBecomesIdle()
        {
            // Arrange: InCombat 시민 1명 + 분대 HP 80/100 (80%)
            var gameState = new GameState { currentTurn = 1 };
            gameState.citizens.Add(new CitizenRuntimeState
            {
                citizenId = "c_0",
                displayName = "Fighter",
                proficiencyLevel = 1,
                state = CitizenState.InCombat
            });

            CreateSquadEntity(squadIndex: 0, maxHP: 100f, currentHP: 80f);
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            _collector.CollectResults(gameState);

            // Assert: HP≥50% → Idle 복귀
            Assert.That(gameState.citizens[0].state, Is.EqualTo(CitizenState.Idle),
                "분대 HP가 50% 이상이면 시민은 전투 후 Idle 상태로 복귀해야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 테스트: turnNumber 반영
        // -------------------------------------------------------------------------

        [Test]
        public void CollectResults_WaveResult_TurnNumberMatchesGameState()
        {
            // Arrange
            var gameState = new GameState { currentTurn = 7 };
            CreateBattleStatisticsSingleton(0, 0);

            // Act
            var result = _collector.CollectResults(gameState);

            // Assert
            Assert.That(result.turnNumber, Is.EqualTo(7),
                "WaveResult.turnNumber는 GameState.currentTurn과 일치해야 합니다.");
        }

        // -------------------------------------------------------------------------
        // 헬퍼 메서드
        // -------------------------------------------------------------------------

        /// <summary>단일 건물을 가진 GameState 생성.</summary>
        static GameState CreateGameState_SingleBuilding(string slotId, bool isHQ, int maxHP, int currentHP)
        {
            var gs = new GameState { currentTurn = 1 };
            gs.buildings.Add(new BuildingRuntimeState
            {
                slotId = slotId,
                buildingId = isHQ ? "headquarters" : "tower_basic",
                state = BuildingSlotStateV2.Active,
                currentHP = currentHP,
                maxHP = maxHP
            });
            return gs;
        }

        /// <summary>BuildingTag + BuildingData 엔티티를 ECS World에 직접 생성.</summary>
        Entity CreateBuildingEntity(int slotIndex, float maxHP, float currentHP, bool isHQ)
        {
            var entity = _em.CreateEntity();
            _entities.Add(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            _em.AddComponentData(entity, new BuildingTag());
            _em.AddComponentData(entity, new BuildingData
            {
                MaxHP = maxHP,
                CurrentHP = currentHP,
                IsHeadquarters = isHQ,
                IsWall = false,
                SlotIndex = slotIndex
            });
            _em.AddBuffer<BuildingDamageBuffer>(entity);
            return entity;
        }

        /// <summary>BattleStatistics 싱글턴 엔티티를 ECS World에 직접 생성.</summary>
        Entity CreateBattleStatisticsSingleton(int spawned, int killed)
        {
            var entity = _em.CreateEntity();
            _entities.Add(entity);
            _em.AddComponentData(entity, new BattleStatistics
            {
                TotalEnemiesSpawned = spawned,
                TotalEnemiesKilled = killed,
                RemainingEnemies = spawned - killed,
                TotalDamageToBuildings = 0f
            });
            return entity;
        }

        /// <summary>SquadTag + SquadId + SquadStats 엔티티를 ECS World에 직접 생성.</summary>
        Entity CreateSquadEntity(int squadIndex, float maxHP, float currentHP)
        {
            var entity = _em.CreateEntity();
            _entities.Add(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            _em.AddComponentData(entity, new ProjectSun.V2.Defense.ECS.SquadTag());
            _em.AddComponentData(entity, new ProjectSun.V2.Defense.ECS.SquadId { Value = squadIndex });
            _em.AddComponentData(entity, new ProjectSun.V2.Defense.ECS.SquadStats
            {
                CombatPower = 20f,
                AttackRange = 8f,
                AttackSpeed = 1.5f,
                MoveSpeed = 4f,
                MaxHP = maxHP,
                CurrentHP = currentHP,
                MemberCount = 5
            });
            return entity;
        }
    }
}
