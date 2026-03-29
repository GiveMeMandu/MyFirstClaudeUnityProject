using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIStudy.DragDrop.Views
{
    /// <summary>
    /// 정렬 가능 리스트 View — Placeholder 방식 라이브 리오더.
    /// 드래그 중 Placeholder의 SiblingIndex를 실시간 변경하여
    /// VerticalLayoutGroup이 다른 항목을 자동으로 밀어주는 원리.
    /// </summary>
    public class SortableListView : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private RectTransform _listContainer;
        [SerializeField] private SortableItemView[] _items = new SortableItemView[6];

        [Header("Insertion Indicator")]
        [SerializeField] private RectTransform _insertionLine;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI _statusText;

        public SortableItemView[] Items => _items;
        public RectTransform ListContainer => _listContainer;

        private void Awake()
        {
            if (_insertionLine != null)
                _insertionLine.gameObject.SetActive(false);
        }

        public void BindAll(string[] itemTexts)
        {
            for (int i = 0; i < _items.Length && i < itemTexts.Length; i++)
                _items[i].Bind(i, itemTexts[i]);
        }

        /// <summary>
        /// 드래그 중 포인터 Y 위치 → Placeholder의 SiblingIndex를 실시간 갱신.
        /// VerticalLayoutGroup이 다른 항목을 자동으로 밀어줌.
        /// </summary>
        public void UpdatePlaceholderIndex(SortableItemView draggedItem, PointerEventData eventData)
        {
            var placeholder = draggedItem.Placeholder;
            if (placeholder == null) return;

            int newIndex = CalculateInsertIndex(eventData);

            if (placeholder.transform.GetSiblingIndex() != newIndex)
                placeholder.transform.SetSiblingIndex(newIndex);
        }

        /// <summary>
        /// 포인터 Y 좌표 → 삽입 인덱스 계산 (GetWorldCorners 중간점 비교).
        /// </summary>
        public int CalculateInsertIndex(PointerEventData eventData)
        {
            // 스크린 Y를 월드 Y로 변환할 필요 없이, 각 자식의 world position.y와 비교
            float pointerScreenY = eventData.position.y;
            Camera cam = eventData.pressEventCamera;

            for (int i = 0; i < _listContainer.childCount; i++)
            {
                var child = _listContainer.GetChild(i) as RectTransform;
                if (child == null) continue;

                // 자식의 월드 중심 Y를 스크린 좌표로 변환
                Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(cam, child.position);
                float childScreenCenterY = screenPos.y;

                // 포인터가 이 자식의 중심보다 위에 있으면 이 위치에 삽입
                if (pointerScreenY > childScreenCenterY)
                    return i;
            }

            return _listContainer.childCount - 1;
        }

        /// <summary>
        /// 삽입 인디케이터 숨기기.
        /// </summary>
        public void HideInsertionIndicator()
        {
            if (_insertionLine != null)
                _insertionLine.gameObject.SetActive(false);
        }

        /// <summary>
        /// 리오더 후 아이템 인덱스 갱신.
        /// </summary>
        public void RefreshOrder(string[] itemTexts)
        {
            for (int i = 0; i < _items.Length && i < itemTexts.Length; i++)
            {
                _items[i].Bind(i, itemTexts[i]);
                _items[i].transform.SetSiblingIndex(i);
            }
        }

        public void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }
    }
}
