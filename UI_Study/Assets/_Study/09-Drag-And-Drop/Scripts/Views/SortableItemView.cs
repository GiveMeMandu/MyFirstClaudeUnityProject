using System;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIStudy.DragDrop.Views
{
    /// <summary>
    /// 정렬 가능 리스트 아이템 View — 드래그로 순서 변경 가능.
    /// 드래그 핸들 영역 + 라벨 텍스트 + 교대 배경색.
    /// </summary>
    public class SortableItemView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI")]
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _handleIcon;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Canvas _rootCanvas;
        private RectTransform _rectTransform;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Vector2 _originalAnchoredPosition;
        private int _itemIndex;

        /// <summary>드래그 시작 이벤트 (자기 인덱스 전달).</summary>
        public Subject<int> OnBeginDragEvent { get; } = new();

        /// <summary>드래그 중 이벤트 (포인터 이벤트 데이터 전달).</summary>
        public Subject<PointerEventData> OnDragEvent { get; } = new();

        /// <summary>드래그 종료 이벤트 (자기 인덱스 전달).</summary>
        public Subject<int> OnEndDragEvent { get; } = new();

        public int ItemIndex => _itemIndex;
        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 아이템 데이터 바인딩.
        /// </summary>
        public void Bind(int index, string text)
        {
            _itemIndex = index;
            if (_label != null) _label.text = text;
            if (_handleIcon != null) _handleIcon.text = "::";

            // 교대 배경색
            var isEven = index % 2 == 0;
            if (_background != null)
            {
                _background.color = isEven
                    ? new Color(0.2f, 0.2f, 0.25f, 0.9f)
                    : new Color(0.25f, 0.25f, 0.3f, 0.9f);
            }
        }

        /// <summary>
        /// 인덱스만 갱신 (리오더 후).
        /// </summary>
        public void SetIndex(int index)
        {
            _itemIndex = index;

            // 교대 배경색 갱신
            var isEven = index % 2 == 0;
            if (_background != null)
            {
                _background.color = isEven
                    ? new Color(0.2f, 0.2f, 0.25f, 0.9f)
                    : new Color(0.25f, 0.25f, 0.3f, 0.9f);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalAnchoredPosition = _rectTransform.anchoredPosition;

            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

            transform.SetParent(_rootCanvas.transform, true);

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.7f;

            OnBeginDragEvent.OnNext(_itemIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                _rectTransform.localPosition = localPoint;
            }

            OnDragEvent.OnNext(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            OnEndDragEvent.OnNext(_itemIndex);
        }

        /// <summary>
        /// 원래 부모로 복원 후 지정 sibling index에 배치.
        /// </summary>
        public void ReturnToParent(Transform parent, int siblingIndex)
        {
            transform.SetParent(parent);
            transform.SetSiblingIndex(siblingIndex);
            _rectTransform.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 스냅백 애니메이션.
        /// </summary>
        public void SnapBack()
        {
            transform.SetParent(_originalParent);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.DOAnchorPos(_originalAnchoredPosition, 0.2f)
                .SetEase(Ease.OutBack);
        }

        private void OnDestroy()
        {
            OnBeginDragEvent.Dispose();
            OnDragEvent.Dispose();
            OnEndDragEvent.Dispose();
        }
    }
}
