using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// 싱글톤 툴팁 컨트롤러 — 호버 딜레이 후 표시, 화면 가장자리 클램핑.
    /// TooltipTrigger 컴포넌트가 OnPointerEnter/Exit 시 호출.
    /// </summary>
    public class TooltipService : MonoBehaviour
    {
        [SerializeField] private RectTransform _tooltipPanel;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private float _maxWidth = 300f;
        [SerializeField] private Vector2 _offset = new(10f, -10f);

        private Canvas _parentCanvas;
        private RectTransform _canvasRectTransform;

        private void Awake()
        {
            _parentCanvas = GetComponentInParent<Canvas>();
            _canvasRectTransform = _parentCanvas.GetComponent<RectTransform>();
            Hide();
        }

        public void Show(string text, Vector2 screenPosition)
        {
            _tooltipText.text = text;

            // 텍스트 길이에 따라 LayoutElement 폭 제한
            _layoutElement.enabled = _tooltipText.preferredWidth > _maxWidth;

            // 강제 레이아웃 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipPanel);

            // 위치 설정 + 화면 가장자리 클램핑
            var position = screenPosition + _offset;
            var tooltipSize = _tooltipPanel.sizeDelta;
            var canvasSize = _canvasRectTransform.sizeDelta;

            // 오른쪽 넘침
            if (position.x + tooltipSize.x > canvasSize.x)
                position.x = screenPosition.x - tooltipSize.x - _offset.x;

            // 아래 넘침
            if (position.y - tooltipSize.y < 0)
                position.y = screenPosition.y + tooltipSize.y - _offset.y;

            _tooltipPanel.anchoredPosition = position;
            _tooltipPanel.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _tooltipPanel.gameObject.SetActive(false);
        }
    }
}
