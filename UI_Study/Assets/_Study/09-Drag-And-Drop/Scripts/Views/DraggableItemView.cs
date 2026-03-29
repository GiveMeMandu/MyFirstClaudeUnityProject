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
    /// 드래그 가능한 아이템 View.
    /// BeginDrag 시 Canvas 루트로 리페어런팅, EndDrag 시 스냅백 또는 드롭 처리.
    /// </summary>
    public class DraggableItemView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("UI")]
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Canvas _rootCanvas;
        private RectTransform _rectTransform;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Vector2 _originalAnchoredPosition;
        private bool _isDragging;

        /// <summary>드래그 시작 이벤트 (자기 자신 전달).</summary>
        public Subject<DraggableItemView> OnBeginDragEvent { get; } = new();

        /// <summary>드래그 종료 이벤트 (자기 자신 전달).</summary>
        public Subject<DraggableItemView> OnEndDragEvent { get; } = new();

        public string ItemLabel => _label != null ? _label.text : string.Empty;
        public Image Image => _image;
        public bool IsDragging => _isDragging;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 라벨 텍스트 설정.
        /// </summary>
        public void SetLabel(string text)
        {
            if (_label != null) _label.text = text;
        }

        /// <summary>
        /// 배경 색상 설정.
        /// </summary>
        public void SetColor(Color color)
        {
            if (_image != null) _image.color = color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 시각적 피드백(살짝 축소)은 필요 시 추가 가능
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;

            // 원래 위치 저장
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalAnchoredPosition = _rectTransform.anchoredPosition;

            // 루트 캔버스로 리페어런팅 (최상위 렌더링)
            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

            transform.SetParent(_rootCanvas.transform, true);

            // 레이캐스트 차단 해제 → 드롭 존이 이벤트를 받을 수 있도록
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.7f;

            OnBeginDragEvent.OnNext(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 마우스 위치를 캔버스 로컬 좌표로 변환
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                _rectTransform.localPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            OnEndDragEvent.OnNext(this);
        }

        /// <summary>
        /// 원래 부모로 DOTween 스냅백 애니메이션.
        /// </summary>
        public void SnapBack()
        {
            transform.SetParent(_originalParent);
            transform.SetSiblingIndex(_originalSiblingIndex);

            _rectTransform.DOAnchorPos(_originalAnchoredPosition, 0.25f)
                .SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 지정된 부모로 즉시 이동 (드롭 성공 시).
        /// </summary>
        public void SetDropParent(Transform newParent)
        {
            transform.SetParent(newParent);
            _rectTransform.anchoredPosition = Vector2.zero;
        }

        private void OnDestroy()
        {
            OnBeginDragEvent.Dispose();
            OnEndDragEvent.Dispose();
        }
    }
}
