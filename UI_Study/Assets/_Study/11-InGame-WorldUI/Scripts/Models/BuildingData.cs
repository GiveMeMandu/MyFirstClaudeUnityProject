using UnityEngine;

namespace UIStudy.InGameUI.Models
{
    /// <summary>
    /// Resource type produced by each building.
    /// </summary>
    public enum ResourceType
    {
        Gold,
        Wood,
        Stone,
        Food
    }

    /// <summary>
    /// Immutable data describing a building — resource type, yield per click, and visual color.
    /// </summary>
    public class BuildingData
    {
        public string Name { get; }
        public ResourceType Type { get; }
        public int AmountPerClick { get; }
        public Color BuildingColor { get; }

        public BuildingData(string name, ResourceType type, int amountPerClick, Color buildingColor)
        {
            Name = name;
            Type = type;
            AmountPerClick = amountPerClick;
            BuildingColor = buildingColor;
        }
    }
}
