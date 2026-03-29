using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.DragDrop.Views
{
    /// <summary>
    /// 정렬 가능 리스트 View — VerticalLayoutGroup 기반 리스트 + 삽입 인디케이터.
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

        /// <summary>
        /// 전체 아이템 바인딩.
        /// </summary>
        public void BindAll(string[] itemTexts)
        {
            for (int i = 0; i < _items.Length && i < itemTexts.Length; i++)
            {
                _items[i].Bind(i, itemTexts[i]);
            }
        }

        /// <summary>
        /// 삽입 인디케이터를 지정 인덱스 위치에 표시.
        /// </summary>
        public void ShowInsertionIndicator(int index)
        {
            if (_insertionLine == null || _listContainer == null) return;

            _insertionLine.gameObject.SetActive(true);

            // 인덱스에 해당하는 Y 위치 계산
            if (index >= 0 && index <= _items.Length)
            {
                float yPos;
                if (index < _items.Length)
                {
                    // 해당 아이템의 상단에 배치
                    var itemRect = _items[index].RectTransform;
                    var itemPos = itemRect.localPosition;
                    yPos = itemPos.y + itemRect.rect.height * 0.5f;
                }
                else
                {
                    // 마지막 아이템 하단에 배치
                    var lastItemRect = _items[_items.Length - 1].RectTransform;
                    var lastPos = lastItemRect.localPosition;
                    yPos = lastPos.y - lastItemRect.rect.height * 0.5f;
                }

                _insertionLine.localPosition = new Vector3(
                    _insertionLine.localPosition.x, yPos, 0f);
            }
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
        /// 드래그 중 포인터 Y 위치로부터 삽입 인덱스 계산.
        /// </summary>
        public int CalculateInsertIndex(Vector2 screenPosition, Camera eventCamera)
        {
            for (int i = 0; i < _items.Length; i++)
            {
                var itemRect = _items[i].RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _listContainer, screenPosition, eventCamera, out var localPoint);

                var itemLocalPos = itemRect.localPosition;
                if (localPoint.y > itemLocalPos.y)
                    return i;
            }

            return _items.Length - 1;
        }

        /// <summary>
        /// 리오더 후 아이템 sibling 순서 + 인덱스 갱신.
        /// </summary>
        public void RefreshOrder(string[] itemTexts)
        {
            for (int i = 0; i < _items.Length && i < itemTexts.Length; i++)
            {
                _items[i].Bind(i, itemTexts[i]);
                _items[i].transform.SetSiblingIndex(i);
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
