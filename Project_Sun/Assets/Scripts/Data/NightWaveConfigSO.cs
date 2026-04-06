using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.V2.Data
{
    /// <summary>
    /// Turn-to-wave mapping entry.
    /// Maps a range of turns [fromTurn, toTurn] to a specific WaveDataSOV2 asset.
    /// </summary>
    [Serializable]
    public class TurnWaveMapping
    {
        [Min(1)]
        public int fromTurn = 1;
        [Min(1)]
        public int toTurn = 5;
        public WaveDataSOV2 waveData;
    }

    /// <summary>
    /// Night wave configuration: determines which WaveDataSOV2 to use per turn,
    /// provides auto-scaling parameters, and holds the enemy data registry
    /// for resolving enemyTypeId strings to EnemyDataSOV2 stats.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectSun/V2/Data/Night Wave Config")]
    public class NightWaveConfigSO : ScriptableObject
    {
        [Header("Turn-to-Wave Mapping")]
        [Tooltip("Ordered list of turn ranges. First matching range wins.")]
        public List<TurnWaveMapping> turnMappings = new();

        [Header("Fallback")]
        [Tooltip("Used when no turn mapping matches the current turn.")]
        public WaveDataSOV2 fallbackWaveData;

        [Header("Auto-Scaling (applied on top of WaveDataSOV2.enemyStatMultiplier)")]
        [Tooltip("HP multiplier per turn: totalHPMult = pow(1 + hpScalePerTurn, turn - 1)")]
        [Min(0f)]
        public float hpScalePerTurn = 0.1f;

        [Tooltip("Enemy count multiplier per turn: totalCountMult = pow(1 + countScalePerTurn, turn - 1)")]
        [Min(0f)]
        public float countScalePerTurn = 0.15f;

        [Header("Enemy Registry")]
        [Tooltip("All enemy types available for wave composition. Looked up by EnemyDataSOV2.enemyId.")]
        public List<EnemyDataSOV2> enemyRegistry = new();

        /// <summary>
        /// Returns the WaveDataSOV2 for the given turn number.
        /// Searches turnMappings in order; returns fallback if no range matches.
        /// </summary>
        public WaveDataSOV2 GetWaveForTurn(int turn)
        {
            for (int i = 0; i < turnMappings.Count; i++)
            {
                var mapping = turnMappings[i];
                if (mapping.waveData != null && turn >= mapping.fromTurn && turn <= mapping.toTurn)
                    return mapping.waveData;
            }

            return fallbackWaveData;
        }

        /// <summary>
        /// Stat multiplier for the given turn, compounding hpScalePerTurn.
        /// Example: turn 5, hpScalePerTurn 0.1 => pow(1.1, 4) ~ 1.46x
        /// </summary>
        public float GetStatMultiplier(int turn)
        {
            return Mathf.Pow(1f + hpScalePerTurn, Mathf.Max(0, turn - 1));
        }

        /// <summary>
        /// Count multiplier for the given turn, compounding countScalePerTurn.
        /// </summary>
        public float GetCountMultiplier(int turn)
        {
            return Mathf.Pow(1f + countScalePerTurn, Mathf.Max(0, turn - 1));
        }

        /// <summary>
        /// Scales a base enemy count by turn-based count multiplier, rounding to nearest int (min 1).
        /// </summary>
        public int GetScaledCount(int baseCount, int turn)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseCount * GetCountMultiplier(turn)));
        }

        /// <summary>
        /// Looks up an EnemyDataSOV2 by its enemyId.
        /// Returns null if not found.
        /// </summary>
        public EnemyDataSOV2 FindEnemy(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId))
                return null;

            for (int i = 0; i < enemyRegistry.Count; i++)
            {
                if (enemyRegistry[i] != null && enemyRegistry[i].enemyId == enemyId)
                    return enemyRegistry[i];
            }

            return null;
        }

        /// <summary>
        /// Builds a dictionary for fast enemyId lookup.
        /// Call once at init time if doing many lookups.
        /// </summary>
        public Dictionary<string, EnemyDataSOV2> BuildEnemyLookup()
        {
            var lookup = new Dictionary<string, EnemyDataSOV2>(enemyRegistry.Count);
            for (int i = 0; i < enemyRegistry.Count; i++)
            {
                if (enemyRegistry[i] != null && !string.IsNullOrEmpty(enemyRegistry[i].enemyId))
                    lookup[enemyRegistry[i].enemyId] = enemyRegistry[i];
            }

            return lookup;
        }
    }
}
