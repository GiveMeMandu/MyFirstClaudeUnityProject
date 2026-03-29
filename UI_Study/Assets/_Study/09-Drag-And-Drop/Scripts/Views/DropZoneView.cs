using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIStudy.DragDrop.Views
{
    /// <summary>
    /// 드롭 존 View — 드래그 중인 아이템을 받아들이는 영역.
    /// 호버 시 하이라이트, 드롭 시 이벤트 발행.
    /// </summary>
    public class DropZoneView : MonoBehaviour,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI")]
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Image _highlight;

        [Header("Settings")]
        [SerializeField] private Color _normalColor = new(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _hoverColor = new(0.3f, 0.5f, 0.3f, 0.9f);

        /// <summary>드롭 이벤트 (드롭된 DraggableItemView 전달).</summary>
        public Subject<DraggableItemView> OnItemDropped { get; } = new();

        public string ZoneLabel => _label != null ? _label.text : string.Empty;

        private void Awake()
        {
            if (_highlight != null)
                _highlight.gameObject.SetActive(false);
        }

        /// <summary>
        /// 라벨 텍스트 설정.
        /// </summary>
        public void SetLabel(string text)
        {
            if (_label != null) _label.text = text;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var draggable = eventData.pointerDrag?.GetComponent<DraggableItemView>();
            if (draggable == null) return;

            OnItemDropped.OnNext(draggable);
            SetHighlight(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 드래그 중인 오브젝트가 있을 때만 하이라이트
            if (eventData.pointerDrag != null &&
                eventData.pointerDrag.GetComponent<DraggableItemView>() != null)
            {
                SetHighlight(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        private void SetHighlight(bool active)
        {
            if (_highlight != null)
            {
                _highlight.gameObject.SetActive(active);
            }

            if (_background != null)
            {
                _background.color = active ? _hoverColor : _normalColor;
            }
        }

        private void OnDestroy()
        {
            OnItemDropped.Dispose();
        }
    }
}
