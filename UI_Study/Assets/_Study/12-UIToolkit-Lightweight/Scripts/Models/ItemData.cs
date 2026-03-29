namespace UIStudy.UIToolkitLightweight
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Step 7: 아이템 데이터 — UI 참조 없는 Plain C# 클래스.
    /// </summary>
    public class ItemData
    {
        public string Name { get; }
        public Rarity Rarity { get; }
        public string Description { get; }
        public string IconLetter { get; }

        public ItemData(string name, Rarity rarity, string description, string iconLetter)
        {
            Name = name;
            Rarity = rarity;
            Description = description;
            IconLetter = iconLetter;
        }
    }
}
