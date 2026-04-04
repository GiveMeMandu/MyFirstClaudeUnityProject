using System.Collections.Generic;
using ProjectSun.V2.Data;
using UnityEngine;

namespace ProjectSun.V2.Core
{
    [CreateAssetMenu(menuName = "ProjectSun/V2/Core/SO Registry")]
    public class SORegistry : ScriptableObject
    {
        [Header("Buildings")]
        [SerializeField] private List<BuildingDataSO> buildings = new();

        [Header("Enemies")]
        [SerializeField] private List<EnemyDataSOV2> enemies = new();

        [Header("Waves")]
        [SerializeField] private List<WaveDataSOV2> waves = new();

        private Dictionary<string, BuildingDataSO> _buildingMap;
        private Dictionary<string, EnemyDataSOV2> _enemyMap;
        private Dictionary<int, WaveDataSOV2> _waveMap;

        public void Initialize()
        {
            _buildingMap = new Dictionary<string, BuildingDataSO>();
            foreach (var b in buildings)
            {
                if (b != null && !string.IsNullOrEmpty(b.buildingId))
                    _buildingMap[b.buildingId] = b;
            }

            _enemyMap = new Dictionary<string, EnemyDataSOV2>();
            foreach (var e in enemies)
            {
                if (e != null && !string.IsNullOrEmpty(e.enemyId))
                    _enemyMap[e.enemyId] = e;
            }

            _waveMap = new Dictionary<int, WaveDataSOV2>();
            foreach (var w in waves)
            {
                if (w != null)
                    _waveMap[w.turnNumber] = w;
            }
        }

        public BuildingDataSO GetBuilding(string buildingId)
        {
            if (_buildingMap == null) Initialize();
            _buildingMap.TryGetValue(buildingId, out var result);
            return result;
        }

        public EnemyDataSOV2 GetEnemy(string enemyId)
        {
            if (_enemyMap == null) Initialize();
            _enemyMap.TryGetValue(enemyId, out var result);
            return result;
        }

        public WaveDataSOV2 GetWave(int turnNumber)
        {
            if (_waveMap == null) Initialize();
            _waveMap.TryGetValue(turnNumber, out var result);
            return result;
        }
    }
}
