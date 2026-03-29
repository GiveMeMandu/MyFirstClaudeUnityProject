using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 7: 인벤토리 View — ListView 가상화 + 검색/정렬/상세 패널.
    /// makeItem/bindItem에서 userData 캐싱으로 Q&lt;T&gt; 반복 방지.
    /// </summary>
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private VisualTreeAsset _itemTemplate;

        // Cached elements
        private TextField _searchField;
        private DropdownField _sortDropdown;
        private ListView _listView;
        private Label _itemCountLabel;
        private Label _detailIcon;
        private Label _detailName;
        private Label _detailRarity;
        private Label _detailDesc;

        // Current items backing the ListView
        private List<ItemData> _items = new();

        // Events for Presenter
        public event Action<string> OnSearchChanged;
        public event Action<int> OnSortChanged;
        public event Action<int> OnItemSelected;

        // Named handlers
        private void HandleSearchChanged(ChangeEvent<string> evt) => OnSearchChanged?.Invoke(evt.newValue);
        private void HandleSortChanged(ChangeEvent<string> evt) => OnSortChanged?.Invoke(_sortDropdown.index);
        private void HandleSelectionChanged(IEnumerable<object> selection) => HandleSelectionChangedInternal();

        private void HandleSelectionChangedInternal()
        {
            if (_listView.selectedIndex >= 0)
                OnItemSelected?.Invoke(_listView.selectedIndex);
        }

        private void OnEnable()
        {
            var root = _document.rootVisualElement;

            _searchField   = root.Q<TextField>("search-field");
            _sortDropdown  = root.Q<DropdownField>("sort-dropdown");
            _listView      = root.Q<ListView>("inventory-list");
            _itemCountLabel = root.Q<Label>("item-count-label");
            _detailIcon    = root.Q<Label>("detail-icon");
            _detailName    = root.Q<Label>("detail-name");
            _detailRarity  = root.Q<Label>("detail-rarity");
            _detailDesc    = root.Q<Label>("detail-desc");

            SetupListView();

            _searchField.RegisterValueChangedCallback(HandleSearchChanged);
            _sortDropdown.RegisterValueChangedCallback(HandleSortChanged);
            _listView.selectionChanged += HandleSelectionChanged;
        }

        private void OnDisable()
        {
            if (_searchField != null) _searchField.UnregisterValueChangedCallback(HandleSearchChanged);
            if (_sortDropdown != null) _sortDropdown.UnregisterValueChangedCallback(HandleSortChanged);
            if (_listView != null) _listView.selectionChanged -= HandleSelectionChanged;
        }

        private void SetupListView()
        {
            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;
            _listView.itemsSource = _items;
            _listView.selectionType = SelectionType.Single;
        }

        private VisualElement MakeItem()
        {
            var item = _itemTemplate.Instantiate();
            // Cache references in userData to avoid repeated Q<T> calls
            item.userData = new ItemViewCache
            {
                IconLabel = item.Q<Label>("item-icon"),
                NameLabel = item.Q<Label>("item-name"),
                RarityLabel = item.Q<Label>("item-rarity")
            };
            return item;
        }

        private void BindItem(VisualElement element, int index)
        {
            var cache = (ItemViewCache)element.userData;
            var data = _items[index];
            cache.IconLabel.text = data.IconLetter;
            cache.NameLabel.text = data.Name;
            cache.RarityLabel.text = data.Rarity.ToString();
            ApplyRarityStyle(cache.RarityLabel, data.Rarity);
        }

        // Display methods
        public void SetItems(List<ItemData> items)
        {
            _items.Clear();
            _items.AddRange(items);
            _listView.itemsSource = _items;
            _listView.RefreshItems();
            _itemCountLabel.text = $"{_items.Count} items";
        }

        public void SetDetail(ItemData item)
        {
            _detailIcon.text = item.IconLetter;
            _detailName.text = item.Name;
            _detailRarity.text = item.Rarity.ToString();
            _detailDesc.text = item.Description;
            ApplyRarityStyle(_detailRarity, item.Rarity);
        }

        public void ClearDetail()
        {
            _detailIcon.text = "?";
            _detailName.text = "Select an item";
            _detailRarity.text = "";
            _detailDesc.text = "Choose an item from the list to view details.";
        }

        private static void ApplyRarityStyle(Label label, Rarity rarity)
        {
            // Remove all rarity classes first
            label.RemoveFromClassList("rarity--common");
            label.RemoveFromClassList("rarity--uncommon");
            label.RemoveFromClassList("rarity--rare");
            label.RemoveFromClassList("rarity--epic");
            label.RemoveFromClassList("rarity--legendary");

            string className = rarity switch
            {
                Rarity.Common => "rarity--common",
                Rarity.Uncommon => "rarity--uncommon",
                Rarity.Rare => "rarity--rare",
                Rarity.Epic => "rarity--epic",
                Rarity.Legendary => "rarity--legendary",
                _ => "rarity--common"
            };
            label.AddToClassList(className);
        }

        /// <summary>
        /// ListView 아이템별 캐싱 구조 — userData에 저장하여 bindItem에서 Q 반복 방지.
        /// </summary>
        private class ItemViewCache
        {
            public Label IconLabel;
            public Label NameLabel;
            public Label RarityLabel;
        }
    }
}
