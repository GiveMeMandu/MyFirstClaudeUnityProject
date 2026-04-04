using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ProjectSun.Defense.ECS;
using ProjectSun.V2.Core;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Defense.Bridge
{
    /// <summary>
    /// 페이즈 전환 테스트 컨트롤러.
    /// Day→Night→Day 전환 + 데이터 라운드트립을 자동 검증한다.
    /// </summary>
    public class PhaseTransitionTest : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PhaseManager phaseManager;
        [SerializeField] BattleInitializer battleInitializer;
        [SerializeField] BattleResultCollector resultCollector;
        [SerializeField] BattleUIBridge uiBridge;

        [Header("Test Settings")]
        [SerializeField] int testBuildingCount = 5;
        [SerializeField] int testSquadCount = 2;
        [SerializeField] bool autoRun;

        GameState _testGameState;
        bool _nightActive;
        int _uiPollCount;
        float _nightTimer;

        // 라운드트립 검증용 기록
        Dictionary<int, int> _preBattleHP = new();

        void Start()
        {
            SetupTestGameState();

            phaseManager.Initialize(_testGameState);
            phaseManager.OnPhaseChanged += phase =>
                Debug.Log($"[PhaseTransitionTest] Phase changed: {phase}");

            if (autoRun)
                StartNight();
        }

        void Update()
        {
            if (_nightActive)
            {
                _nightTimer += Time.deltaTime;
                _uiPollCount++;

                // 매 60프레임마다 UI Bridge 상태 로깅
                if (_uiPollCount % 60 == 0)
                {
                    Debug.Log($"[UIBridge] Enemies: {uiBridge.AliveEnemyCount}/{uiBridge.TotalEnemyCount}, " +
                              $"HQ HP: {uiBridge.HeadquartersHP:F0}/{uiBridge.HeadquartersMaxHP:F0}, " +
                              $"Buildings: {uiBridge.TotalBuildingHP:F0}/{uiBridge.TotalBuildingMaxHP:F0}, " +
                              $"Squads: {uiBridge.SquadCount} ({uiBridge.TotalSquadHP:F0}/{uiBridge.TotalSquadMaxHP:F0})");
                }

                // 3초 후 자동 종료 (autoRun 시)
                if (autoRun && _nightTimer >= 3f)
                    EndNight();
            }
        }

        /// <summary>밤 전환 시작 (테스트 버튼).</summary>
        public void StartNight()
        {
            if (_nightActive) return;

            Debug.Log("=== [PhaseTransitionTest] START NIGHT ===");

            // 전투 전 HP 기록
            _preBattleHP.Clear();
            for (int i = 0; i < _testGameState.buildings.Count; i++)
                _preBattleHP[i] = _testGameState.buildings[i].currentHP;

            // 1) 페이즈 전환 시작
            phaseManager.StartNightPhase();

            // 2) ECS 엔티티 생성
            battleInitializer.InitializeBattle(_testGameState);

            // 3) 테스트용 적 스폰 (전투 시뮬레이션)
            SpawnTestEnemies(20);

            // 4) 건물에 테스트 데미지 적용 (라운드트립 검증용)
            ApplyTestDamage();

            // 5) Night 진입
            phaseManager.EnterNight();

            // 6) UI Bridge 활성화
            uiBridge.Activate();

            _nightActive = true;
            _nightTimer = 0f;
            _uiPollCount = 0;

            Debug.Log($"[PhaseTransitionTest] Night entered. Transition: {phaseManager.LastTransitionMs}ms, " +
                      $"Entities: {battleInitializer.CreatedEntityCount}");
        }

        /// <summary>밤 종료 (테스트 버튼).</summary>
        public void EndNight()
        {
            if (!_nightActive) return;
            _nightActive = false;

            Debug.Log("=== [PhaseTransitionTest] END NIGHT ===");

            // 1) UI Bridge 비활성화
            uiBridge.Deactivate();

            // 2) Dawn 전환 시작
            phaseManager.EndNightPhase();

            // 3) 결과 수집
            var result = resultCollector.CollectResults(_testGameState);

            // 4) ECS 정리
            battleInitializer.CleanupBattleEntities();

            // 5) Day 진입
            phaseManager.EnterDay();

            // 6) 결과 로깅
            LogResults(result);

            // 7) 라운드트립 검증
            VerifyRoundTrip();

            Debug.Log($"[PhaseTransitionTest] Day returned. Transition: {phaseManager.LastTransitionMs}ms, " +
                      $"Collect: {resultCollector.CollectTimeMs}ms");
        }

        void SetupTestGameState()
        {
            _testGameState = new GameState();

            // 본부
            _testGameState.buildings.Add(new BuildingRuntimeState
            {
                slotId = "slot_0",
                buildingId = "headquarters",
                state = BuildingSlotStateV2.Active,
                currentHP = 500,
                maxHP = 500,
                upgradeLevel = 0
            });

            // 방어 타워
            for (int i = 1; i <= 2; i++)
            {
                _testGameState.buildings.Add(new BuildingRuntimeState
                {
                    slotId = $"slot_{i}",
                    buildingId = "tower_basic",
                    state = BuildingSlotStateV2.Active,
                    currentHP = 150,
                    maxHP = 150,
                    upgradeLevel = 1
                });
            }

            // 일반 건물
            for (int i = 3; i < testBuildingCount; i++)
            {
                _testGameState.buildings.Add(new BuildingRuntimeState
                {
                    slotId = $"slot_{i}",
                    buildingId = $"production_{i}",
                    state = BuildingSlotStateV2.Active,
                    currentHP = 200,
                    maxHP = 200,
                    upgradeLevel = 0
                });
            }

            // 전투 분대
            for (int i = 0; i < testSquadCount; i++)
            {
                _testGameState.citizens.Add(new CitizenRuntimeState
                {
                    citizenId = $"citizen_{i}",
                    displayName = $"Soldier {i}",
                    aptitude = CitizenAptitude.Combat,
                    proficiencyLevel = 2,
                    state = CitizenState.InCombat
                });
            }

            Debug.Log($"[PhaseTransitionTest] GameState: {_testGameState.buildings.Count} buildings, " +
                      $"{_testGameState.citizens.Count} combat citizens");
        }

        void SpawnTestEnemies(int count)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var random = new Unity.Mathematics.Random(42);

            for (int i = 0; i < count; i++)
            {
                float angle = random.NextFloat(0f, math.PI * 2f);
                float dist = random.NextFloat(40f, 60f);
                float3 pos = new float3(math.cos(angle) * dist, 0, math.sin(angle) * dist);

                var entity = em.CreateEntity();
                em.AddComponentData(entity, LocalTransform.FromPosition(pos));
                em.AddComponentData(entity, new EnemyTag());
                em.AddComponentData(entity, new EnemyStats
                {
                    MaxHP = 30f,
                    CurrentHP = 30f,
                    Speed = 3f,
                    Damage = 5f,
                    AttackRange = 2f,
                    AttackInterval = 1f,
                    EnemyType = 0
                });
                em.AddComponentData(entity, new EnemyState { Value = 1 });
                em.AddComponentData(entity, new EnemyTarget
                {
                    TargetEntity = Entity.Null,
                    TargetPosition = float3.zero,
                    HasTarget = false
                });
                em.AddComponentData(entity, new AttackTimer { TimeSinceLastAttack = 0f });
            }

            // BattleStatistics 업데이트
            var statsQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BattleStatistics>());
            if (statsQuery.CalculateEntityCount() > 0)
            {
                using var statsEntities = statsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                var stats = em.GetComponentData<BattleStatistics>(statsEntities[0]);
                stats.TotalEnemiesSpawned = count;
                stats.RemainingEnemies = count;
                em.SetComponentData(statsEntities[0], stats);
            }

            Debug.Log($"[PhaseTransitionTest] Spawned {count} test enemies");
        }

        void ApplyTestDamage()
        {
            // 라운드트립 검증: 건물 1, 2에 50 데미지 직접 적용
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingTag>(),
                ComponentType.ReadWrite<BuildingData>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            int damaged = 0;
            for (int i = 0; i < entities.Length && damaged < 2; i++)
            {
                var data = em.GetComponentData<BuildingData>(entities[i]);
                if (!data.IsHeadquarters)
                {
                    data.CurrentHP -= 50f;
                    em.SetComponentData(entities[i], data);
                    damaged++;
                    Debug.Log($"[PhaseTransitionTest] Applied 50 damage to building slot {data.SlotIndex} " +
                              $"(HP: {data.CurrentHP + 50f} → {data.CurrentHP})");
                }
            }
        }

        void LogResults(WaveResult result)
        {
            if (result == null)
            {
                Debug.LogError("[PhaseTransitionTest] WaveResult is null!");
                return;
            }

            Debug.Log($"[WaveResult] Turn: {result.turnNumber}, " +
                      $"Killed: {result.enemiesDefeated}/{result.enemiesTotal}, " +
                      $"Perfect: {result.isPerfectDefense}, " +
                      $"Damaged: [{string.Join(", ", result.damagedBuildingSlotIds)}], " +
                      $"Reward: Basic={result.basicReward} Advanced={result.advancedReward}");
        }

        void VerifyRoundTrip()
        {
            Debug.Log("=== [PhaseTransitionTest] ROUND-TRIP VERIFICATION ===");

            bool allPassed = true;
            for (int i = 0; i < _testGameState.buildings.Count; i++)
            {
                var building = _testGameState.buildings[i];
                int preBattleHP = _preBattleHP.ContainsKey(i) ? _preBattleHP[i] : -1;
                int postBattleHP = building.currentHP;

                string status;
                if (preBattleHP == postBattleHP)
                    status = "UNCHANGED";
                else if (postBattleHP < preBattleHP)
                    status = $"DAMAGED (-{preBattleHP - postBattleHP})";
                else
                {
                    status = "ERROR: HP increased!";
                    allPassed = false;
                }

                Debug.Log($"  [{building.slotId}] {building.buildingId}: " +
                          $"HP {preBattleHP} → {postBattleHP} [{status}]");
            }

            Debug.Log(allPassed
                ? "=== ROUND-TRIP: PASS ==="
                : "=== ROUND-TRIP: FAIL (see errors above) ===");
        }
    }
}
