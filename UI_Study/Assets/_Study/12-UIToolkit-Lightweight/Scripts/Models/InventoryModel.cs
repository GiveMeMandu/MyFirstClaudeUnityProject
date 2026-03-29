using System;
using System.Collections.Generic;
using System.Linq;

namespace UIStudy.UIToolkitLightweight
{
    public enum SortMode
    {
        Name,
        RarityAsc,
        RarityDesc
    }

    /// <summary>
    /// Step 7: 인벤토리 모델 — 1000개 더미 아이템 생성, 필터/정렬 제공.
    /// UI 참조 없음, C# event로만 변경 알림.
    /// </summary>
    public class InventoryModel
    {
        private readonly List<ItemData> _items = new();

#pragma warning disable CS0067 // 현재 미사용이나 Model API 계약상 유지
        public event Action ItemsChanged;
#pragma warning restore CS0067

        public IReadOnlyList<ItemData> Items => _items;

        public InventoryModel()
        {
            GenerateDummyItems(1000);
        }

        public List<ItemData> GetFiltered(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return new List<ItemData>(_items);

            string lower = search.ToLowerInvariant();
            return _items.Where(item =>
                item.Name.ToLowerInvariant().Contains(lower)).ToList();
        }

        public List<ItemData> GetSorted(List<ItemData> items, SortMode mode)
        {
            return mode switch
            {
                SortMode.Name => items.OrderBy(i => i.Name).ToList(),
                SortMode.RarityAsc => items.OrderBy(i => i.Rarity).ThenBy(i => i.Name).ToList(),
                SortMode.RarityDesc => items.OrderByDescending(i => i.Rarity).ThenBy(i => i.Name).ToList(),
                _ => items
            };
        }

        private void GenerateDummyItems(int count)
        {
            string[] prefixes = { "Iron", "Steel", "Golden", "Shadow", "Crystal",
                "Ancient", "Mystic", "Flame", "Frost", "Thunder" };
            string[] types = { "Sword", "Shield", "Helm", "Potion", "Ring",
                "Amulet", "Staff", "Bow", "Dagger", "Armor" };
            var rarities = (Rarity[])Enum.GetValues(typeof(Rarity));
            var rng = new Random(42); // deterministic seed

            for (int i = 0; i < count; i++)
            {
                string prefix = prefixes[rng.Next(prefixes.Length)];
                string type = types[rng.Next(types.Length)];
                Rarity rarity = rarities[rng.Next(rarities.Length)];
                string name = $"{prefix} {type} #{i + 1:D4}";
                string desc = $"A {rarity.ToString().ToLowerInvariant()} {type.ToLowerInvariant()} " +
                              $"forged with {prefix.ToLowerInvariant()} essence.";
                string icon = type[..1]; // first letter

                _items.Add(new ItemData(name, rarity, desc, icon));
            }

            // NOTE: 생성자에서 호출되므로 구독자 없음 — 이벤트 발화 제거.
        }
    }
}
