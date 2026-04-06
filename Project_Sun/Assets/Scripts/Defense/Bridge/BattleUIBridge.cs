using Unity.Entities;
using UnityEngine;
using ProjectSun.Defense.ECS;
using ProjectSun.V2.Defense.ECS;

namespace ProjectSun.V2.Defense.Bridge
{
    /// <summary>
    /// 전투 중 단방향 ECS 읽기 브릿지.
    /// 매 프레임 ECS World를 폴링하여 UI가 읽을 프로퍼티로 노출.
    /// EntityQuery는 Activate() 시 1회 생성하여 캐시.
    /// </summary>
    public class BattleUIBridge : MonoBehaviour
    {
        bool _active;
        EntityManager _entityManager;

        // 캐시된 EntityQuery (Activate에서 1회 생성)
        EntityQuery _allEnemyQuery;
        EntityQuery _aliveEnemyQuery;
        EntityQuery _buildingQuery;
        EntityQuery _squadQuery;
        EntityQuery _waveManagerQuery;
        EntityQuery _battleStatsQuery;

        // 폴링 결과 (UI 읽기용)
        public int AliveEnemyCount { get; private set; }
        public int TotalEnemyCount { get; private set; }
        public float HeadquartersHP { get; private set; }
        public float HeadquartersMaxHP { get; private set; }
        public float TotalBuildingHP { get; private set; }
        public float TotalBuildingMaxHP { get; private set; }
        public int SquadCount { get; private set; }
        public float TotalSquadHP { get; private set; }
        public float TotalSquadMaxHP { get; private set; }
        public float WaveProgress { get; private set; }

        // BattleStatistics 싱글턴 폴링 결과
        public int TotalEnemiesSpawned { get; private set; }
        public int TotalEnemiesKilled { get; private set; }
        public int RemainingEnemies { get; private set; }
        public float TotalDamageToBuildings { get; private set; }

        /// <summary>브릿지 활성화. 밤 페이즈 진입 시 호출.</summary>
        public void Activate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("[BattleUIBridge] ECS World not available");
                return;
            }

            _entityManager = world.EntityManager;

            // EntityQuery 1회 생성 후 캐시
            _allEnemyQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>());
            _aliveEnemyQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Exclude<DeadTag>());
            _buildingQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingTag>(),
                ComponentType.ReadOnly<BuildingData>());
            _squadQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SquadTag>(),
                ComponentType.ReadOnly<SquadStats>());
            _waveManagerQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<WaveManager>());
            _battleStatsQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BattleStatistics>());

            _active = true;
            Debug.Log("[BattleUIBridge] Activated");
        }

        /// <summary>브릿지 비활성화. 낮 페이즈 진입 시 호출.</summary>
        public void Deactivate()
        {
            _active = false;
            Debug.Log("[BattleUIBridge] Deactivated");
        }

        void Update()
        {
            if (!_active) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            PollEnemies();
            PollBuildings();
            PollSquads();
            PollWaveProgress();
            PollBattleStatistics();
        }

        void PollEnemies()
        {
            TotalEnemyCount = _allEnemyQuery.CalculateEntityCount();
            AliveEnemyCount = _aliveEnemyQuery.CalculateEntityCount();
        }

        void PollBuildings()
        {
            using var entities = _buildingQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            float totalHP = 0f;
            float totalMaxHP = 0f;
            HeadquartersHP = 0f;
            HeadquartersMaxHP = 0f;

            for (int i = 0; i < entities.Length; i++)
            {
                var data = _entityManager.GetComponentData<BuildingData>(entities[i]);
                totalHP += data.CurrentHP;
                totalMaxHP += data.MaxHP;

                if (data.IsHeadquarters)
                {
                    HeadquartersHP = data.CurrentHP;
                    HeadquartersMaxHP = data.MaxHP;
                }
            }

            TotalBuildingHP = totalHP;
            TotalBuildingMaxHP = totalMaxHP;
        }

        void PollSquads()
        {
            using var entities = _squadQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            SquadCount = entities.Length;
            float totalHP = 0f;
            float totalMaxHP = 0f;

            for (int i = 0; i < entities.Length; i++)
            {
                var stats = _entityManager.GetComponentData<SquadStats>(entities[i]);
                totalHP += stats.CurrentHP;
                totalMaxHP += stats.MaxHP;
            }

            TotalSquadHP = totalHP;
            TotalSquadMaxHP = totalMaxHP;
        }

        void PollWaveProgress()
        {
            if (_waveManagerQuery.CalculateEntityCount() == 0)
            {
                WaveProgress = 0f;
                return;
            }

            using var entities = _waveManagerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var wm = _entityManager.GetComponentData<WaveManager>(entities[0]);

            WaveProgress = wm.TotalWaves > 0
                ? (float)wm.CurrentWaveIndex / wm.TotalWaves
                : 0f;
        }

        void PollBattleStatistics()
        {
            if (_battleStatsQuery.CalculateEntityCount() == 0) return;

            using var entities = _battleStatsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var stats = _entityManager.GetComponentData<BattleStatistics>(entities[0]);

            TotalEnemiesSpawned = stats.TotalEnemiesSpawned;
            TotalEnemiesKilled = stats.TotalEnemiesKilled;
            RemainingEnemies = stats.RemainingEnemies;
            TotalDamageToBuildings = stats.TotalDamageToBuildings;
        }
    }
}
