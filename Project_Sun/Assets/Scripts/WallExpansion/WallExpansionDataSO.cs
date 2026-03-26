using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.WallExpansion
{
    /// <summary>
    /// 시나리오별 방벽 확장 정적 데이터.
    /// 기획자가 시나리오마다 하나씩 생성하여 확장 레벨/비용/해금 내용을 제어.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWallExpansionData", menuName = "ProjectSun/WallExpansion/Expansion Data")]
    public class WallExpansionDataSO : ScriptableObject
    {
        [Header("기본 설정")]
        [Tooltip("이 시나리오에서의 최대 방벽 레벨")]
        [Min(1)]
        [SerializeField] private int maxLevel = 4;

        [Tooltip("확장 연출 시간 (초)")]
        [Min(0.1f)]
        [SerializeField] private float expansionAnimDuration = 1.5f;

        [Header("레벨별 데이터")]
        [Tooltip("레벨 1부터 maxLevel까지의 확장 데이터 (인덱스 0 = Lv.0→Lv.1)")]
        [SerializeField] private List<WallExpansionLevelData> levels = new();

        public int MaxLevel => maxLevel;
        public float ExpansionAnimDuration => expansionAnimDuration;
        public IReadOnlyList<WallExpansionLevelData> Levels => levels;

        /// <summary>
        /// 목표 레벨의 확장 데이터를 반환. 없으면 null.
        /// </summary>
        public WallExpansionLevelData GetLevelData(int targetLevel)
        {
            foreach (var data in levels)
            {
                if (data.level == targetLevel) return data;
            }
            return null;
        }

        /// <summary>
        /// 기본 시나리오 데이터 자동 생성 (테스트/폴백용)
        /// </summary>
        public void GenerateDefault()
        {
            maxLevel = 4;
            expansionAnimDuration = 1.5f;
            levels.Clear();

            levels.Add(new WallExpansionLevelData
            {
                level = 1,
                basicCost = 60,
                advancedCost = 0,
                additionalSpawnPoints = 1,
                minTurn = 0
            });

            levels.Add(new WallExpansionLevelData
            {
                level = 2,
                basicCost = 100,
                advancedCost = 20,
                additionalSpawnPoints = 1,
                minTurn = 0
            });

            levels.Add(new WallExpansionLevelData
            {
                level = 3,
                basicCost = 150,
                advancedCost = 50,
                additionalSpawnPoints = 1,
                minTurn = 0
            });

            levels.Add(new WallExpansionLevelData
            {
                level = 4,
                basicCost = 200,
                advancedCost = 80,
                additionalSpawnPoints = 1,
                minTurn = 0
            });
        }
    }
}
