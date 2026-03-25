using System;
using System.Collections.Generic;
using ProjectSun.Construction;
using ProjectSun.Defense.ECS;
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

        [Header("적 프리팹 (SubScene 내 Entity Prefab)")]
        [SerializeField] private GameObject basicEnemyPrefab;
        [SerializeField] private GameObject heavyEnemyPrefab;
        [SerializeField] private GameObject flyingEnemyPrefab;

        [Header("연동")]
        [SerializeField] private BuildingManager buildingManager;

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

            // 건물 데이터를 DOTS 세계로 브릿지
            BridgeBuildingsToECS();

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
            // 모든 적 Entity 제거
            CleanupECSEntities();
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

        private void Update()
        {
            if (battleState != BattleState.InProgress) return;

            // 웨이브 스폰 타이밍 관리
            UpdateWaveSpawning();

            // 건물 데미지 동기화 (DOTS → MonoBehaviour)
            SyncBuildingDamage();

            // 전투 종료 조건 확인
            CheckBattleEndConditions();
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

                // ECS SpawnGroup Entity 생성
                var prefab = GetEntityPrefab(group.enemyData.enemyType);
                if (prefab == Entity.Null) continue;

                var spawnEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(spawnEntity, new SpawnGroup
                {
                    EnemyPrefab = prefab,
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

        private Entity GetEntityPrefab(EnemyType type)
        {
            // 런타임에 GameObject → Entity 변환은 Baking이 필요.
            // PoC에서는 SubScene에 프리팹을 배치하고 베이크된 Entity를 참조.
            // 이를 위해 PrefabEntityHolder 싱글턴을 사용.
            var query = entityManager.CreateEntityQuery(typeof(PrefabEntityReference));
            if (query.IsEmpty) return Entity.Null;

            var entities = query.ToEntityArray(Allocator.Temp);
            Entity result = Entity.Null;

            for (int i = 0; i < entities.Length; i++)
            {
                var prefRef = entityManager.GetComponentData<PrefabEntityReference>(entities[i]);
                if (prefRef.EnemyType == (int)type)
                {
                    result = prefRef.PrefabEntity;
                    break;
                }
            }

            entities.Dispose();
            return result;
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
                entityManager.AddComponentData(buildingEntity, new BuildingDamageBuffer
                {
                    AccumulatedDamage = 0f
                });

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
                var damageBuffer = entityManager.GetComponentData<BuildingDamageBuffer>(entities[i]);
                if (damageBuffer.AccumulatedDamage <= 0f) continue;

                var buildingData = entityManager.GetComponentData<ECS.BuildingData>(entities[i]);
                var slots = buildingManager.AllSlots;

                if (buildingData.SlotIndex >= 0 && buildingData.SlotIndex < slots.Count)
                {
                    var slot = slots[buildingData.SlotIndex];
                    if (slot.Health != null)
                    {
                        slot.Health.TakeDamage(damageBuffer.AccumulatedDamage);
                        statistics.TotalDamageToBuildings += damageBuffer.AccumulatedDamage;

                        // 건물 피해 기록
                        statistics.RecordBuildingDamage(
                            slot.CurrentBuildingData != null ? slot.CurrentBuildingData.buildingName : $"Building {buildingData.SlotIndex}",
                            damageBuffer.AccumulatedDamage
                        );

                        // ECS 건물 HP 동기화
                        entityManager.SetComponentData(entities[i], new ECS.BuildingData
                        {
                            MaxHP = buildingData.MaxHP,
                            CurrentHP = slot.Health.CurrentHP,
                            IsHeadquarters = buildingData.IsHeadquarters,
                            IsWall = buildingData.IsWall,
                            SlotIndex = buildingData.SlotIndex
                        });
                    }
                }

                // 데미지 버퍼 리셋
                entityManager.SetComponentData(entities[i], new BuildingDamageBuffer { AccumulatedDamage = 0f });
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

                int remainingEnemies = enemyQuery.CalculateEntityCount();
                int remainingSpawnGroups = spawnGroupQuery.CalculateEntityCount();

                if (remainingEnemies == 0 && remainingSpawnGroups == 0)
                {
                    EndBattle(BattleState.Victory);
                }
            }
        }

        private void EndBattle(BattleState result)
        {
            Time.timeScale = 1f;
            timeScale = 1f;

            // 적 수 업데이트
            var enemyQuery = entityManager.CreateEntityQuery(typeof(EnemyTag));
            statistics.TotalKilled = statistics.TotalSpawned - enemyQuery.CalculateEntityCount();

            CleanupECSEntities();
            SetBattleState(result);
            OnBattleEnded?.Invoke(statistics);
        }

        private void CleanupECSEntities()
        {
            if (defaultWorld == null || !defaultWorld.IsCreated) return;

            // 적 Entity 전부 제거
            var enemyQuery = entityManager.CreateEntityQuery(typeof(EnemyTag));
            entityManager.DestroyEntity(enemyQuery);

            // 건물 Entity 제거
            var buildingQuery = entityManager.CreateEntityQuery(typeof(BuildingTag));
            entityManager.DestroyEntity(buildingQuery);

            // SpawnGroup 제거
            var spawnQuery = entityManager.CreateEntityQuery(typeof(SpawnGroup));
            entityManager.DestroyEntity(spawnQuery);

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

    /// <summary>
    /// 전투 통계 데이터 (MonoBehaviour 측)
    /// </summary>
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
