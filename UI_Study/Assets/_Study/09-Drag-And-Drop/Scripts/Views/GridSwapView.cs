using TMPro;
using UnityEngine;

namespace UIStudy.DragDrop.Views
{
    /// <summary>
    /// 그리드 스왑 View — 3x3 GridLayoutGroup 기반 슬롯 그리드 + 상태 텍스트.
    /// </summary>
    public class GridSwapView : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private SwapSlotView[] _slots = new SwapSlotView[9];

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI _statusText;

        public SwapSlotView[] Slots => _slots;

        /// <summary>
        /// 전체 슬롯 바인딩.
        /// </summary>
        public void BindAll(string[] contents)
        {
            for (int i = 0; i < _slots.Length && i < contents.Length; i++)
            {
                _slots[i].Bind(i, contents[i]);
            }
        }

        /// <summary>
        /// 상태 텍스트 갱신.
        /// </summary>
        public void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }
    }
}
