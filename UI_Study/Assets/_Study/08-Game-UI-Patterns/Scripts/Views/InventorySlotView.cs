using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.GameUI.Views
{
    /// <summary>
    /// 인벤토리 슬롯 — 배경(레어리티 색), 아이콘 텍스트, 개수, 선택 하이라이트.
    /// </summary>
    public class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _iconText;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _selectionHighlight;
        [SerializeField] private Button _button;

        private int _slotIndex;

        public Button Button => _button;
        public int SlotIndex => _slotIndex;

        /// <summary>
        /// 슬롯 인덱스 설정 (초기화 시 한 번).
        /// </summary>
        public void SetSlotIndex(int index)
        {
            _slotIndex = index;
        }

        /// <summary>
        /// 아이템 데이터로 슬롯 UI 갱신. null이면 빈 슬롯.
        /// </summary>
        public void Bind(Models.InventoryItem item)
        {
            if (item == null)
            {
                // 빈 슬롯
                _iconText.text = "";
                _countText.text = "";
                _background.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
                return;
            }

            _iconText.text = item.IconLabel;
            _countText.text = item.Count > 1 ? item.Count.ToString() : "";
            _background.color = GetRarityColor(item.Rarity);
        }

        /// <summary>
        /// 선택 하이라이트 토글.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionHighlight != null)
                _selectionHighlight.SetActive(selected);
        }

        private static Color GetRarityColor(Models.ItemRarity rarity) => rarity switch
        {
            Models.ItemRarity.Common => new Color(0.3f, 0.3f, 0.3f, 0.8f),
            Models.ItemRarity.Rare => new Color(0.2f, 0.4f, 0.8f, 0.8f),
            Models.ItemRarity.Epic => new Color(0.6f, 0.2f, 0.8f, 0.8f),
            _ => new Color(0.3f, 0.3f, 0.3f, 0.8f)
        };
    }
}
