using R3;

namespace UIStudy.GameUI.Models
{
    public enum ItemRarity
    {
        Common,
        Rare,
        Epic
    }

    /// <summary>
    /// 인벤토리 슬롯 아이템 데이터.
    /// </summary>
    public class InventoryItem
    {
        public string Name;
        public string IconLabel; // emoji or text like "[S]"
        public int Count;
        public ItemRarity Rarity;
        public string Description;

        public InventoryItem(string name, string iconLabel, int count, ItemRarity rarity, string description = "")
        {
            Name = name;
            IconLabel = iconLabel;
            Count = count;
            Rarity = rarity;
            Description = description;
        }
    }

    /// <summary>
    /// 인벤토리 모델 — 16슬롯 그리드, null = 빈 슬롯.
    /// </summary>
    public class InventoryModel
    {
        public const int SlotCount = 16;

        /// <summary>
        /// 슬롯 배열. null 요소는 빈 슬롯을 의미.
        /// </summary>
        public ReactiveProperty<InventoryItem[]> Items { get; }

        public InventoryModel()
        {
            var slots = new InventoryItem[SlotCount];
            Items = new ReactiveProperty<InventoryItem[]>(slots);
        }

        /// <summary>
        /// 슬롯 변경 후 알림 발행.
        /// </summary>
        public void SetItem(int index, InventoryItem item)
        {
            var arr = Items.Value;
            arr[index] = item;
            Items.ForceNotify();
        }

        /// <summary>
        /// 전체 슬롯 교체.
        /// </summary>
        public void SetAll(InventoryItem[] items)
        {
            Items.Value = items;
        }
    }
}
