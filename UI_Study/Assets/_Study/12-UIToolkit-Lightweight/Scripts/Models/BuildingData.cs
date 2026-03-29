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
        [field: SerializeField] public string Name { get; set; } = "Building";
        [field: SerializeField] public string Description { get; set; } = "A building.";
        [field: SerializeField] public int GoldCost { get; set; } = 100;
        [field: SerializeField] public int WoodCost { get; set; } = 50;
        [field: SerializeField] public int Level { get; set; } = 1;
        [field: SerializeField] public int MaxLevel { get; set; } = 5;

        public bool IsMaxLevel => Level >= MaxLevel;

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
