using Unity.Entities;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 웨이브 스폰 관리 싱글턴
    /// </summary>
    public struct WaveManager : IComponentData
    {
        public int CurrentWaveIndex;
        public int TotalWaves;
        public float WaveTimer;
        public float NextWaveDelay;
        public bool WaveActive;
        public bool AllWavesComplete;
    }

    /// <summary>
    /// 현재 활성 스폰 그룹
    /// </summary>
    public struct SpawnGroup : IComponentData
    {
        public Entity EnemyPrefab;
        public int RemainingCount;
        public float SpawnInterval;
        public float SpawnTimer;
        public float StatMultiplier;
        public int EnemyType;
        public float BaseHP;
        public float BaseSpeed;
        public float BaseDamage;
        public float BaseAttackRange;
        public float BaseAttackInterval;

        // 특수행동 데이터 (SO에서 복사, SF-WD-015)
        public bool BypassWalls;
        public bool AttemptsWallBypass;
        public float WallDamageMultiplier;
        public bool ExplodesOnDeath;
        public float DeathExplosionRadius;
        public float DeathExplosionDamage;
    }

    /// <summary>
    /// 스폰 포인트 위치
    /// </summary>
    public struct SpawnPoint : IComponentData
    {
        public int Index;
    }

    /// <summary>
    /// 전투 통계 싱글턴
    /// </summary>
    public struct BattleStatistics : IComponentData
    {
        public int TotalEnemiesSpawned;
        public int TotalEnemiesKilled;
        public int RemainingEnemies;
        public float TotalDamageToBuildings;
    }
}
