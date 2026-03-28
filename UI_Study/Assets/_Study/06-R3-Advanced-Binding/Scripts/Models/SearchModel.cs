using System;
using System.Collections.Generic;
using System.Linq;
using R3;

namespace UIStudy.R3Advanced.Models
{
    /// <summary>
    /// 검색 모델 — 하드코딩된 아이템 목록 + 검색어 ReactiveProperty.
    /// </summary>
    public class SearchModel : IDisposable
    {
        public ReactiveProperty<string> SearchQuery { get; } = new(string.Empty);

        public IReadOnlyList<string> AllItems { get; } = new List<string>
        {
            "Sword", "Shield", "Bow", "Arrow", "Staff",
            "Helmet", "Armor", "Boots", "Gloves", "Ring",
            "Amulet", "Potion", "Scroll", "Gem", "Rune",
            "Dagger", "Spear", "Axe", "Mace", "Wand"
        };

        /// <summary>
        /// 검색어로 AllItems를 필터링하여 결과를 반환.
        /// </summary>
        public List<string> FilterItems(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return AllItems.ToList();

            return AllItems
                .Where(item => item.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        public void Dispose()
        {
            SearchQuery.Dispose();
        }
    }
}
