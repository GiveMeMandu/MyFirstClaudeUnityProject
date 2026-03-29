using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIStudy.DragDrop.Views
{
    /// <summary>
    /// 그리드 슬롯 View — 드래그 소스 + 드롭 타겟 겸용.
    /// 아이콘 텍스트 + 배경색 + 호버 하이라이트.
    /// </summary>
    public class SwapSlotView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI")]
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _iconText;
        [SerializeField] private Image _highlight;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color _emptyColor = new(0.15f, 0.15f, 0.15f, 0.6f);
        [SerializeField] private Color _filledColor = new(0.25f, 0.35f, 0.5f, 0.9f);
        [SerializeField] private Color _hoverColor = new(0.4f, 0.5f, 0.3f, 0.9f);

        private Canvas _rootCanvas;
        private RectTransform _rectTransform;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Vector2 _originalAnchoredPosition;
        private int _slotIndex;
        private bool _isEmpty = true;
        private bool _isDragging;

        // 드래그 중 아이콘 표시를 위한 별도 오브젝트 (원래 슬롯은 그대로 유지)
        private GameObject _dragGhost;
        private RectTransform _ghostRect;

        /// <summary>드래그 시작 이벤트 (슬롯 인덱스 전달).</summary>
        public Subject<int> OnBeginDragEvent { get; } = new();

        /// <summary>드롭 이벤트 (소스 슬롯 인덱스 전달).</summary>
        public Subject<int> OnDropReceived { get; } = new();

        public int SlotIndex => _slotIndex;
        public bool IsEmpty => _isEmpty;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_highlight != null)
                _highlight.gameObject.SetActive(false);
        }

        /// <summary>
        /// 슬롯 데이터 바인딩. null/빈 문자열이면 빈 슬롯.
        /// </summary>
        public void Bind(int index, string content)
        {
            _slotIndex = index;
            _isEmpty = string.IsNullOrEmpty(content);

            if (_iconText != null)
                _iconText.text = _isEmpty ? "" : content;

            if (_background != null)
                _background.color = _isEmpty ? _emptyColor : _filledColor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isEmpty) return; // 빈 슬롯은 드래그 불가

            _isDragging = true;

            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

            // 고스트 오브젝트 생성 (드래그 비주얼)
            CreateDragGhost();

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.4f;

            OnBeginDragEvent.OnNext(_slotIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _ghostRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                _ghostRect.localPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            DestroyDragGhost();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var sourceSlot = eventData.pointerDrag?.GetComponent<SwapSlotView>();
            if (sourceSlot == null || sourceSlot == this) return;
            if (sourceSlot.IsEmpty) return;

            OnDropReceived.OnNext(sourceSlot.SlotIndex);
            SetHighlight(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null &&
                eventData.pointerDrag.GetComponent<SwapSlotView>() != null)
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
                _highlight.gameObject.SetActive(active);

            if (_background != null && !_isDragging)
                _background.color = active ? _hoverColor : (_isEmpty ? _emptyColor : _filledColor);
        }

        private void CreateDragGhost()
        {
            _dragGhost = new GameObject("DragGhost");
            _ghostRect = _dragGhost.AddComponent<RectTransform>();
            _dragGhost.transform.SetParent(_rootCanvas.transform, false);

            _ghostRect.sizeDelta = _rectTransform.sizeDelta;

            // 배경 이미지
            var ghostImage = _dragGhost.AddComponent<Image>();
            ghostImage.color = new Color(_filledColor.r, _filledColor.g, _filledColor.b, 0.7f);

            // 아이콘 텍스트
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(_dragGhost.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = _iconText != null ? _iconText.text : "";
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            // CanvasGroup (레이캐스트 차단 해제)
            var ghostCG = _dragGhost.AddComponent<CanvasGroup>();
            ghostCG.blocksRaycasts = false;

            // 현재 마우스 위치에 배치
            _ghostRect.position = _rectTransform.position;
        }

        private void DestroyDragGhost()
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost);
                _dragGhost = null;
                _ghostRect = null;
            }
        }

        private void OnDestroy()
        {
            DestroyDragGhost();
            OnBeginDragEvent.Dispose();
            OnDropReceived.Dispose();
        }
    }
}
