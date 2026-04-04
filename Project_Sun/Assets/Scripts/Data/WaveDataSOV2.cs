using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.V2.Data
{
    [Serializable]
    public class EnemyGroup
    {
        [Tooltip("Enemy type ID referencing EnemyDataSOV2.enemyId")]
        public string enemyTypeId;
        [Min(1)]
        public int count;
        public SpawnPattern spawnPattern;
        [Tooltip("Spawn interval in seconds (for Sequential pattern)")]
        [Min(0.01f)]
        public float spawnInterval = 0.1f;
    }

    [Serializable]
    public class SubWave
    {
        public AttackDirection direction;
        [Tooltip("Delay in seconds after battle start before this sub-wave spawns")]
        [Min(0f)]
        public float spawnDelay;
        public List<EnemyGroup> enemies = new();
    }

    [Serializable]
    public class WaveModifier
    {
        public WaveModifierType type;
        [Tooltip("Modifier intensity (e.g. strength multiplier value)")]
        public float value = 1f;
    }

    [CreateAssetMenu(menuName = "ProjectSun/V2/Data/Wave")]
    public class WaveDataSOV2 : ScriptableObject
    {
        [Header("Identification")]
        public int turnNumber;
        [Tooltip("True for the final wave (turn 25)")]
        public bool isFinalWave;

        [Header("Sub-Waves")]
        public List<SubWave> subWaves = new();

        [Header("Special Modifiers")]
        public List<WaveModifier> specialModifiers = new();

        [Header("Scaling")]
        [Tooltip("Stat multiplier applied to all enemies in this wave")]
        [Min(1f)]
        public float enemyStatMultiplier = 1f;

        public int GetTotalEnemyCount()
        {
            int total = 0;
            foreach (var sw in subWaves)
                foreach (var eg in sw.enemies)
                    total += eg.count;
            return total;
        }

        public int GetDirectionCount()
        {
            var directions = new HashSet<AttackDirection>();
            foreach (var sw in subWaves)
                directions.Add(sw.direction);
            return directions.Count;
        }
    }
}
