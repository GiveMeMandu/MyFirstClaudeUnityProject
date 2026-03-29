using System;
using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 5: 건물 데이터 — 이름, 설명, 비용, 레벨 정보를 담는 순수 데이터 클래스.
    /// ScriptableObject가 아닌 plain C# class — 카탈로그에서 직렬화용.
    /// </summary>
    [Serializable]
    public class BuildingData
    {
        [field: SerializeField] public string Name { get; private set; } = "Building";
        [field: SerializeField] public string Description { get; private set; } = "A building.";
        [field: SerializeField] public int GoldCost { get; private set; } = 100;
        [field: SerializeField] public int WoodCost { get; private set; } = 50;
        [field: SerializeField] public int Level { get; private set; } = 1;
        [field: SerializeField] public int MaxLevel { get; private set; } = 5;

        public bool IsMaxLevel => Level >= MaxLevel;

        /// <summary>레벨을 1 증가시킨다. MaxLevel 이상이면 무시.</summary>
        public void LevelUp()
        {
            if (!IsMaxLevel) Level++;
        }

        public BuildingData() { }

        public BuildingData(string name, string description, int goldCost, int woodCost,
            int level = 1, int maxLevel = 5)
        {
            Name = name;
            Description = description;
            GoldCost = goldCost;
            WoodCost = woodCost;
            Level = level;
            MaxLevel = maxLevel;
        }
    }
}
