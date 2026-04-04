using System;
using System.Collections.Generic;
using ProjectSun.Construction;
using ProjectSun.Defense.ECS;
using ProjectSun.Workforce;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ProjectSun.Defense
{
    /// <summary>
    /// 밤 전투 전체 흐름을 관리하는 MonoBehaviour.
    /// MonoBehaviour ↔ DOTS 브릿지 역할.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("전투 설정")]
        [SerializeField] private WaveDataSO waveData;
        [SerializeField] private int currentTurnNumber = 1;

        [Header("스폰 포인트")]
        [SerializeField] private List<Transform> spawnPoints = new();

        [Header("연동")]
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private WorkforceManager workforceManager;

        [Header("런타임 상태")]
        [SerializeField] private BattleState battleState = BattleState.Idle;
        [SerializeField] private float timeScale = 1f;

        private EntityManager entityManager;
        private World defaultWorld;

        // 건물 Entity 매핑 (SlotIndex → Entity)
        private Dictionary<int, Entity> buildingEntityMap = new();

        // 전투 통계
        private BattleStatisticsData statistics;

        public BattleState State => battleState;
        public float TimeScale => timeScale;
        public BattleStatisticsData Statistics => statistics;

        public event Action<BattleState> OnBattleStateChanged;
        public event Action<BattleStatisticsData> OnBattleEnded;

        private int currentWaveIndex;
        private float waveTimer;
        private bool allWavesSpawned;

        private void Start()
        {
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null)
            {
                entityManager = defaultWorld.EntityManager;
            }
        }

        /// <summary>
        /// 밤 전투 시작 (UI 버튼에서 호출)
        /// </summary>
        public void StartBattle()
        {
            if (battleState != BattleState.Idle) return;
            if (defaultWorld == null || !defaultWorld.IsCreated) return;

            entityManager = defaultWorld.EntityManager;
            statistics = new BattleStatisticsData();
            currentWaveIndex = 0;
            waveTimer = 0f;
            allWavesSpawned = false;

            // 스폰 포인트를 ECS에 등록
            RegisterSpawnPoints();

            // 건물 데이터를 DOTS 세계로 브릿지
            BridgeBuildingsToECS();

            // BattleStatistics 싱글턴 엔티티 생성
            CreateBattleStatisticsEntity();

            SetBattleState(BattleState.InProgress);

            // 첫 웨이브 즉시 시작
            if (waveData != null && waveData.waves.Count > 0)
            {
                SpawnWave(currentWaveIndex);
                currentWaveIndex++;
            }
        }

        /// <summary>
        /// 전투 중지 (디버그/테스트용)
        /// </summary>
        public void StopBattle()
        {
            CleanupECSEntities();
            Time.timeScale = 1f;
            timeScale = 1f;
            SetBattleState(BattleState.Idle);
        }

        /// <summary>
        /// 전투 종료 후 Idle 상태로 리셋 (TurnManager에서 호출).
        /// 다음 전투를 시작하려면 반드시 호출해야 함.
        /// </summary>
        public void ResetToIdle()
        {
            Time.timeScale = 1f;
            timeScale = 1f;
            SetBattleState(BattleState.Idle);
        }

        /// <summary>
        /// 배속 설정 (1x 또는 2x)
        /// </summary>
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Clamp(scale, 1f, 2f);
            Time.timeScale = timeScale;
        }

        /// <summary>
        /// 현재 남은 적 수 (UI용)
        /// </summary>
        public int GetRemainingEnemyCount()
        {
            if (defaultWorld == null || !defaultWorld.IsCreated) return 0;
            var query = entityManager.CreateEntityQuery(typeof(EnemyTag));
            return query.CalculateEntityCount();
        }

        private void Update()
        {
            if (battleState != BattleState.InProgress) return;

            UpdateWaveSpawning();
            SyncBuildingDamage();
            CheckBattleEndConditions();
        }

        private void RegisterSpawnPoints()
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] == null) continue;
                var spEntity = entityManager.CreateEntity();
                var pos = spawnPoints[i].position;
                entityManager.AddComponentData(spEntity, new SpawnPoint { Index = i });
                entityManager.AddComponentData(spEntity, LocalTransform.FromPosition(
                    new float3(pos.x, pos.y, pos.z)));
            }
        }

        private void UpdateWaveSpawning()
        {
            if (allWavesSpawned || waveData == null) return;
            if (currentWaveIndex >= waveData.waves.Count)
            {
                allWavesSpawned = true;
                return;
            }

            waveTimer += Time.deltaTime;

            var nextWave = waveData.waves[currentWaveIndex];
            if (waveTimer >= nextWave.delayBeforeWave)
            {
                SpawnWave(currentWaveIndex);
                currentWaveIndex++;
                waveTimer = 0f;
            }
        }

        private void SpawnWave(int waveIndex)
        {
            if (waveData == null || waveIndex >= waveData.waves.Count) return;

            var wave = waveData.waves[waveIndex];
            float statMultiplier = waveData.GetStatMultiplier(currentTurnNumber);

            foreach (var group in wave.enemyGroups)
            {
                if (group.enemyData == null) continue;

                int scaledCount = waveData.GetScaledCount(group.count, currentTurnNumber);
                statistics.TotalSpawned += scaledCount;

                // SpawnGroup Entity 생성 — WaveSpawnSystem이 실제 적을 스폰
                var spawnEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(spawnEntity, new SpawnGroup
                {
                    EnemyPrefab = Entity.Null, // 런타임 생성이므로 프리팹 불필요
                    RemainingCount = scaledCount,
                    SpawnInterval = group.spawnInterval,
                    SpawnTimer = 0f,
                    StatMultiplier = statMultiplier,
                    EnemyType = (int)group.enemyData.enemyType,
                    BaseHP = group.enemyData.hp,
                    BaseSpeed = group.enemyData.speed,
                    BaseDamage = group.enemyData.damage,
                    BaseAttackRange = group.enemyData.attackRange,
                    BaseAttackInterval = group.enemyData.attackInterval
                });
            }
        }

        private void BridgeBuildingsToECS()
        {
            buildingEntityMap.Clear();

            if (buildingManager == null) return;

            var slots = buildingManager.AllSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsTargetable) continue;

                var buildingEntity = entityManager.CreateEntity();
                var worldPos = slot.transform.position;

                entityManager.AddComponentData(buildingEntity, new BuildingTag());
                entityManager.AddComponentData(buildingEntity, LocalTransform.FromPosition(
                    new float3(worldPos.x, worldPos.y, worldPos.z)));
                entityManager.AddComponentData(buildingEntity, new ECS.BuildingData
                {
                    MaxHP = slot.Health != null ? slot.Health.MaxHP : 100f,
                    CurrentHP = slot.Health != null ? slot.Health.CurrentHP : 100f,
                    IsHeadquarters = slot.CurrentBuildingData != null && slot.CurrentBuildingData.isHeadquarters,
                    IsWall = slot.CurrentBuildingData != null && slot.CurrentBuildingData.category == BuildingCategory.Wall,
                    SlotIndex = i
                });
                entityManager.AddBuffer<BuildingDamageBuffer>(buildingEntity);

                // Defense 카테고리 건물에 타워 컴포넌트 추가
                var buildingData = slot.CurrentBuildingData;
                if (buildingData != null && buildingData.category == BuildingCategory.Defense)
                {
                    // 인력이 배치되어야 타워 활성화 (0명이면 공격 안 함)
                    bool towerActive = workforceManager == null || workforceManager.IsTowerActive(slot);
                    float effectiveDamage = towerActive ? buildingData.towerDamage : 0f;
                    float effectiveAttackSpeed = towerActive ? buildingData.towerAttackSpeed : 0f;

                    entityManager.AddComponentData(buildingEntity, new TowerTag());
                    entityManager.AddComponentData(buildingEntity, new TowerStats
                    {
                        Range = buildingData.towerRange,
                        Damage = effectiveDamage,
                        AttackSpeed = effectiveAttackSpeed,
                        CanTargetAir = buildingData.towerCanTargetAir
                    });
                    entityManager.AddComponentData(buildingEntity, new TowerAttackTimer
                    {
                        TimeSinceLastAttack = 0f
                    });
                }

                buildingEntityMap[i] = buildingEntity;
            }
        }

        private void SyncBuildingDamage()
        {
            if (buildingManager == null) return;

            var query = entityManager.CreateEntityQuery(typeof(BuildingTag), typeof(ECS.BuildingData), typeof(BuildingDamageBuffer));
            var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                // IBufferElementData: 버퍼에서 누적 데미지 합산 후 클리어
                var damageBuffer = entityManager.GetBuffer<BuildingDamageBuffer>(entities[i]);
                if (damageBuffer.Length == 0) continue;

                float accumulatedDamage = 0f;
                for (int j = 0; j < damageBuffer.Length; j++)
                    accumulatedDamage += damageBuffer[j].Damage;
                damageBuffer.Clear();

                var buildingData = entityManager.GetComponentData<ECS.BuildingData>(entities[i]);
                var slots = buildingManager.AllSlots;

                if (buildingData.SlotIndex >= 0 && buildingData.SlotIndex < slots.Count)
                {
                    var slot = slots[buildingData.SlotIndex];
                    if (slot.Health != null)
                    {
                        slot.Health.TakeDamage(accumulatedDamage);
                        statistics.TotalDamageToBuildings += accumulatedDamage;

                        statistics.RecordBuildingDamage(
                            slot.CurrentBuildingData != null ? slot.CurrentBuildingData.buildingName : $"Building {buildingData.SlotIndex}",
                            accumulatedDamage
                        );

                        entityManager.SetComponentData(entities[i], new ECS.BuildingData
                        {
                            MaxHP = buildingData.MaxHP,
                            CurrentHP = slot.Health.CurrentHP,
                            IsHeadquarters = buildingData.IsHeadquarters,
                            IsWall = buildingData.IsWall,
                            SlotIndex = buildingData.SlotIndex
                        });

                        // 건물 파괴 시 배치된 인력 부상 처리
                        if (slot.Health.IsDestroyed && workforceManager != null)
                        {
                            workforceManager.InjureWorkersFromBuilding(slot);
                        }
                    }
                }
            }

            entities.Dispose();
        }

        private void CheckBattleEndConditions()
        {
            // 패배 조건: 본부 파괴
            if (buildingManager != null)
            {
                foreach (var slot in buildingManager.AllSlots)
                {
                    if (slot.CurrentBuildingData != null && slot.CurrentBuildingData.isHeadquarters)
                    {
                        if (slot.Health != null && slot.Health.IsDestroyed)
                        {
                            EndBattle(BattleState.Defeat);
                            return;
                        }
                    }
                }
            }

            // 승리 조건: 모든 웨이브 완료 + 남은 적 없음
            if (allWavesSpawned)
            {
                var enemyQuery = entityManager.CreateEntityQuery(typeof(EnemyTag));
                var spawnGroupQuery = entityManager.CreateEntityQuery(typeof(SpawnGroup));

                if (enemyQuery.CalculateEntityCount() == 0 && spawnGroupQuery.CalculateEntityCount() == 0)
                {
                    EndBattle(BattleState.Victory);
                }
            }
        }

        private void EndBattle(BattleState result)
        {
            Time.timeScale = 1f;
            timeScale = 1f;

            var enemyQuery = entityManager.CreateEntityQuery(typeof(EnemyTag));
            statistics.TotalKilled = statistics.TotalSpawned - enemyQuery.CalculateEntityCount();

            CleanupECSEntities();
            SetBattleState(result);
            OnBattleEnded?.Invoke(statistics);
        }

        private void CreateBattleStatisticsEntity()
        {
            var statsEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(statsEntity, new ECS.BattleStatistics
            {
                TotalEnemiesSpawned = 0,
                TotalEnemiesKilled = 0,
                RemainingEnemies = 0,
                TotalDamageToBuildings = 0f
            });
        }

        private void CleanupECSEntities()
        {
            if (defaultWorld == null || !defaultWorld.IsCreated) return;

            var enemyQuery = entityManager.CreateEntityQuery(typeof(EnemyTag));
            entityManager.DestroyEntity(enemyQuery);

            var buildingQuery = entityManager.CreateEntityQuery(typeof(BuildingTag));
            entityManager.DestroyEntity(buildingQuery);

            var spawnQuery = entityManager.CreateEntityQuery(typeof(SpawnGroup));
            entityManager.DestroyEntity(spawnQuery);

            var spawnPointQuery = entityManager.CreateEntityQuery(typeof(SpawnPoint));
            entityManager.DestroyEntity(spawnPointQuery);

            // 투사체 엔티티 정리
            var projectileQuery = entityManager.CreateEntityQuery(typeof(ProjectileTag));
            entityManager.DestroyEntity(projectileQuery);

            // BattleStatistics 싱글턴 정리
            var statsQuery = entityManager.CreateEntityQuery(typeof(ECS.BattleStatistics));
            entityManager.DestroyEntity(statsQuery);

            buildingEntityMap.Clear();
        }

        private void SetBattleState(BattleState newState)
        {
            battleState = newState;
            OnBattleStateChanged?.Invoke(newState);
        }

        private void OnDestroy()
        {
            if (battleState == BattleState.InProgress)
            {
                StopBattle();
            }
        }
    }

    [Serializable]
    public class BattleStatisticsData
    {
        public int TotalSpawned;
        public int TotalKilled;
        public float TotalDamageToBuildings;
        public Dictionary<string, float> BuildingDamageMap = new();

        public void RecordBuildingDamage(string buildingName, float damage)
        {
            if (BuildingDamageMap.ContainsKey(buildingName))
                BuildingDamageMap[buildingName] += damage;
            else
                BuildingDamageMap[buildingName] = damage;
        }
    }
}
