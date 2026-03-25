using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Defense
{
    [Serializable]
    public struct WaveEnemyGroup
    {
        public EnemyDataSO enemyData;
        [Min(1)]
        public int count;
        [Tooltip("이 그룹 내 스폰 간격 (초)")]
        [Min(0.01f)]
        public float spawnInterval;
    }

    [Serializable]
    public struct WaveDefinition
    {
        [Tooltip("이 웨이브에 포함되는 적 그룹")]
        public List<WaveEnemyGroup> enemyGroups;
        [Tooltip("이 웨이브 시작까지 대기 시간 (초, 첫 웨이브는 0)")]
        [Min(0f)]
        public float delayBeforeWave;
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "ProjectSun/Defense/Wave Data")]
    public class WaveDataSO : ScriptableObject
    {
        [Header("웨이브 구성")]
        public List<WaveDefinition> waves = new();

        [Header("자동 스케일링")]
        [Tooltip("턴 수에 따른 적 수 배율 (턴당)")]
        [Min(1f)]
        public float enemyCountScalePerTurn = 1.15f;

        [Tooltip("턴 수에 따른 적 스탯 배율 (턴당)")]
        [Min(1f)]
        public float enemyStatScalePerTurn = 1.1f;

        /// <summary>
        /// 주어진 턴에 대해 스케일링된 적 수를 반환
        /// </summary>
        public int GetScaledCount(int baseCount, int turnNumber)
        {
            float multiplier = Mathf.Pow(enemyCountScalePerTurn, turnNumber - 1);
            return Mathf.RoundToInt(baseCount * multiplier);
        }

        /// <summary>
        /// 주어진 턴에 대해 스케일링된 스탯 배율을 반환
        /// </summary>
        public float GetStatMultiplier(int turnNumber)
        {
            return Mathf.Pow(enemyStatScalePerTurn, turnNumber - 1);
        }

        /// <summary>
        /// 기본 웨이브 데이터 자동 생성 (빈 SO 생성 시 호출)
        /// </summary>
        public void GenerateDefaultWaves(EnemyDataSO basicEnemy, EnemyDataSO heavyEnemy, EnemyDataSO flyingEnemy)
        {
            waves.Clear();

            // 웨이브 1: 기본 적만
            waves.Add(new WaveDefinition
            {
                delayBeforeWave = 0f,
                enemyGroups = new List<WaveEnemyGroup>
                {
                    new() { enemyData = basicEnemy, count = 20, spawnInterval = 0.1f }
                }
            });

            // 웨이브 2: 기본 + 대형
            waves.Add(new WaveDefinition
            {
                delayBeforeWave = 10f,
                enemyGroups = new List<WaveEnemyGroup>
                {
                    new() { enemyData = basicEnemy, count = 30, spawnInterval = 0.1f },
                    new() { enemyData = heavyEnemy, count = 5, spawnInterval = 0.5f }
                }
            });

            // 웨이브 3: 전 종류
            waves.Add(new WaveDefinition
            {
                delayBeforeWave = 10f,
                enemyGroups = new List<WaveEnemyGroup>
                {
                    new() { enemyData = basicEnemy, count = 40, spawnInterval = 0.08f },
                    new() { enemyData = heavyEnemy, count = 8, spawnInterval = 0.4f },
                    new() { enemyData = flyingEnemy, count = 10, spawnInterval = 0.3f }
                }
            });
        }
    }
}
