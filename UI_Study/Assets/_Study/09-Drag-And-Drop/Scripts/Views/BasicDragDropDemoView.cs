using TMPro;
using UnityEngine;

namespace UIStudy.DragDrop.Views
{
    /// <summary>
    /// 기본 드래그앤드롭 데모 View — 3개 드래그 아이템 + 2개 드롭 존 + 상태 텍스트.
    /// </summary>
    public class BasicDragDropDemoView : MonoBehaviour
    {
        [Header("Draggable Items")]
        [SerializeField] private DraggableItemView[] _draggableItems = new DraggableItemView[3];

        [Header("Drop Zones")]
        [SerializeField] private DropZoneView _equipZone;
        [SerializeField] private DropZoneView _discardZone;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI _statusText;

        public DraggableItemView[] DraggableItems => _draggableItems;
        public DropZoneView EquipZone => _equipZone;
        public DropZoneView DiscardZone => _discardZone;

        /// <summary>
        /// 상태 텍스트 갱신.
        /// </summary>
        public void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }

        /// <summary>
        /// 드래그 아이템 비활성화 (드롭 완료 후).
        /// </summary>
        public void DisableItem(DraggableItemView item)
        {
            item.gameObject.SetActive(false);
        }
    }
}
