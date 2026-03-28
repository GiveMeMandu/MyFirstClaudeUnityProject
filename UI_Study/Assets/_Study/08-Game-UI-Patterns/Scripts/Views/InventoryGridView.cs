using TMPro;
using UIStudy.GameUI.Models;
using UnityEngine;

namespace UIStudy.GameUI.Views
{
    /// <summary>
    /// 인벤토리 그리드 View — 4x4 슬롯 + 디테일 패널 + 골드 표시.
    /// </summary>
    public class InventoryGridView : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private InventorySlotView[] _slots = new InventorySlotView[16];

        [Header("Detail Panel")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailRarity;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailIcon;

        [Header("Gold")]
        [SerializeField] private TextMeshProUGUI _goldText;

        public InventorySlotView[] Slots => _slots;

        /// <summary>
        /// 전체 슬롯 데이터 바인딩.
        /// </summary>
        public void BindAll(InventoryItem[] items)
        {
            for (int i = 0; i < _slots.Length && i < items.Length; i++)
            {
                _slots[i].SetSlotIndex(i);
                _slots[i].Bind(items[i]);
            }
        }

        /// <summary>
        /// 선택된 슬롯 하이라이트 갱신.
        /// </summary>
        public void SetSelectedSlot(int selectedIndex)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].SetSelected(i == selectedIndex);
            }
        }

        /// <summary>
        /// 디테일 패널에 아이템 정보 표시. null이면 패널 숨김.
        /// </summary>
        public void ShowDetail(InventoryItem item)
        {
            if (item == null)
            {
                if (_detailPanel != null)
                    _detailPanel.SetActive(false);
                return;
            }

            if (_detailPanel != null)
                _detailPanel.SetActive(true);

            _detailName.text = item.Name;
            _detailRarity.text = item.Rarity.ToString();
            _detailRarity.color = GetRarityTextColor(item.Rarity);
            _detailDescription.text = string.IsNullOrEmpty(item.Description)
                ? "No description."
                : item.Description;

            if (_detailIcon != null)
                _detailIcon.text = item.IconLabel;
        }

        /// <summary>
        /// 골드 텍스트 갱신.
        /// </summary>
        public void SetGold(int gold)
        {
            if (_goldText != null)
                _goldText.text = $"Gold: {gold}";
        }

        /// <summary>
        /// 디테일 패널 숨김.
        /// </summary>
        public void HideDetail()
        {
            if (_detailPanel != null)
                _detailPanel.SetActive(false);
        }

        private static Color GetRarityTextColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common => Color.white,
            ItemRarity.Rare => new Color(0.4f, 0.6f, 1f),
            ItemRarity.Epic => new Color(0.8f, 0.4f, 1f),
            _ => Color.white
        };
    }
}
