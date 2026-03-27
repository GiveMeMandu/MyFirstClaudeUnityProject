using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// 싱글톤 툴팁 컨트롤러 — 호버 딜레이 후 표시, 화면 가장자리 클램핑.
    /// </summary>
    public class TooltipService : MonoBehaviour
    {
        [SerializeField] private RectTransform _tooltipPanel;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private float _maxWidth = 300f;
        [SerializeField] private Vector2 _offset = new(15f, -15f);

        private Canvas _parentCanvas;
        private RectTransform _canvasRectTransform;
        private Camera _canvasCamera;

        private void Awake()
        {
            _parentCanvas = GetComponentInParent<Canvas>();
            _canvasRectTransform = _parentCanvas.GetComponent<RectTransform>();
            _canvasCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _parentCanvas.worldCamera;
            Hide();
        }

        public void Show(string text, Vector2 screenPosition)
        {
            _tooltipText.text = text;
            _layoutElement.enabled = _tooltipText.preferredWidth > _maxWidth;

            _tooltipPanel.gameObject.SetActive(true);

            // 강제 레이아웃 갱신 (ContentSizeFitter 반영)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipPanel);

            // 스크린 좌표 → Canvas 로컬 좌표 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform, screenPosition, _canvasCamera, out var localPoint);

            var position = localPoint + _offset;
            var tooltipSize = _tooltipPanel.sizeDelta;
            var canvasSize = _canvasRectTransform.rect.size;
            var halfCanvas = canvasSize * 0.5f;

            // 오른쪽 넘침
            if (position.x + tooltipSize.x > halfCanvas.x)
                position.x = localPoint.x - tooltipSize.x - _offset.x;

            // 아래 넘침
            if (position.y - tooltipSize.y < -halfCanvas.y)
                position.y = localPoint.y + tooltipSize.y + Mathf.Abs(_offset.y);

            _tooltipPanel.anchoredPosition = position;
        }

        public void Hide()
        {
            if (_tooltipPanel != null)
                _tooltipPanel.gameObject.SetActive(false);
        }
    }
}
