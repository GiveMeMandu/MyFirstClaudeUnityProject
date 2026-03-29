using UnityEngine;

namespace UIStudy.InGameUI.Models
{
    /// <summary>
    /// Data payload for a single floating resource pop-up.
    /// </summary>
    public readonly struct FloatingResourceData
    {
        public ResourceType Type { get; }
        public int Amount { get; }
        public Vector3 WorldPosition { get; }

        public FloatingResourceData(ResourceType type, int amount, Vector3 worldPosition)
        {
            Type = type;
            Amount = amount;
            WorldPosition = worldPosition;
        }
    }
}
