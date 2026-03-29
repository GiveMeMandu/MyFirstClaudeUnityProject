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
    /// 정렬 가능 리스트 아이템 — Placeholder 패턴 기반 라이브 리오더.
    /// 드래그 시 Canvas 루트로 reparent + Placeholder로 갭 유지.
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
        private RectTransform _dragLayer;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Vector2 _pointerOffset;
        private int _itemIndex;

        // Placeholder — 드래그 중 원위치에 갭을 유지하는 더미 오브젝트
        private GameObject _placeholder;

        /// <summary>드래그 시작 이벤트 (자기 인덱스 전달).</summary>
        public Subject<int> OnBeginDragEvent { get; } = new();

        /// <summary>드래그 중 이벤트 (포인터 이벤트 데이터 전달).</summary>
        public Subject<PointerEventData> OnDragEvent { get; } = new();

        /// <summary>드래그 종료 이벤트 (자기 인덱스 전달).</summary>
        public Subject<int> OnEndDragEvent { get; } = new();

        public int ItemIndex => _itemIndex;
        public RectTransform RectTransform => _rectTransform;
        public GameObject Placeholder => _placeholder;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Bind(int index, string text)
        {
            _itemIndex = index;
            if (_label != null) _label.text = text;
            if (_handleIcon != null) _handleIcon.text = "≡";
            UpdateBackground(index);
        }

        public void SetIndex(int index)
        {
            _itemIndex = index;
            UpdateBackground(index);
        }

        private void UpdateBackground(int index)
        {
            if (_background == null) return;
            _background.color = index % 2 == 0
                ? new Color(0.2f, 0.2f, 0.25f, 0.9f)
                : new Color(0.25f, 0.25f, 0.3f, 0.9f);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();

            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            _dragLayer = _rootCanvas.transform as RectTransform;

            // 1. Placeholder 생성 — 원위치에 동일 크기의 빈 공간 유지
            _placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(LayoutElement));
            _placeholder.transform.SetParent(_originalParent, false);
            _placeholder.transform.SetSiblingIndex(_originalSiblingIndex);
            var placeholderLE = _placeholder.GetComponent<LayoutElement>();
            placeholderLE.preferredHeight = _rectTransform.rect.height;
            placeholderLE.flexibleWidth = 1;
            // 반투명 표시로 드롭 위치 시각적 피드백
            var placeholderImg = _placeholder.AddComponent<Image>();
            placeholderImg.color = new Color(1f, 1f, 1f, 0.1f);
            placeholderImg.raycastTarget = false;

            // 2. 드래그 항목을 Canvas 루트로 reparent (렌��� 최상위)
            Vector3 worldPos = _rectTransform.position;
            transform.SetParent(_dragLayer, true);
            transform.SetAsLastSibling();

            // 포인터 ���프셋 계산 (항목 중심과 포인터 사이 거리 보존)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragLayer, eventData.position, eventData.pressEventCamera, out var localPoint);
            _pointerOffset = _rectTransform.anchoredPosition - localPoint;

            // 3. 시각적 피드백 — 떠 있는 느낌
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.85f;
            _rectTransform.DOScale(1.05f, 0.15f).SetEase(Ease.OutQuad);

            OnBeginDragEvent.OnNext(_itemIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 드래그 항목이 포인터를 따라 이동
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _dragLayer, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                _rectTransform.anchoredPosition = localPoint + _pointerOffset;
            }

            OnDragEvent.OnNext(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 시각적 피드백 복원
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
            _rectTransform.DOScale(1f, 0.15f).SetEase(Ease.OutQuad);

            OnEndDragEvent.OnNext(_itemIndex);
        }

        /// <summary>
        /// Placeholder 위치에 복귀 후 Placeholder 제거.
        /// </summary>
        public void ReturnToPlaceholder()
        {
            if (_placeholder == null) return;

            var parent = _placeholder.transform.parent;
            int siblingIndex = _placeholder.transform.GetSiblingIndex();

            transform.SetParent(parent, false);
            transform.SetSiblingIndex(siblingIndex);

            DestroyPlaceholder();
        }

        /// <summary>
        /// 원래 위치로 스냅백 (드롭 취소 시).
        /// </summary>
        public void SnapBack()
        {
            if (_placeholder != null)
            {
                var parent = _placeholder.transform.parent;
                transform.SetParent(parent, false);
                transform.SetSiblingIndex(_originalSiblingIndex);
                DestroyPlaceholder();
            }
            else
            {
                transform.SetParent(_originalParent, false);
                transform.SetSiblingIndex(_originalSiblingIndex);
            }
        }

        /// <summary>
        /// 원래 부모로 복원 후 지정 sibling index에 배치.
        /// </summary>
        public void ReturnToParent(Transform parent, int siblingIndex)
        {
            DestroyPlaceholder();
            transform.SetParent(parent, false);
            transform.SetSiblingIndex(siblingIndex);
        }

        public void DestroyPlaceholder()
        {
            if (_placeholder != null)
            {
                Destroy(_placeholder);
                _placeholder = null;
            }
        }

        private void OnDestroy()
        {
            DestroyPlaceholder();
            _rectTransform.DOKill();
            OnBeginDragEvent.Dispose();
            OnDragEvent.Dispose();
            OnEndDragEvent.Dispose();
        }
    }
}
