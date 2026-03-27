using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// 싱글톤 툴팁 컨트롤러.
    /// TooltipPanel: anchor=(0,0) pivot=(0,1) — 좌하단 기준, 좌상단 피벗.
    /// anchoredPosition에 스크린 좌표를 Canvas 스케일로 나눈 값을 대입.
    /// </summary>
    public class TooltipService : MonoBehaviour
    {
        [SerializeField] private RectTransform _tooltipPanel;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private float _maxWidth = 300f;
        [SerializeField] private Vector2 _offset = new(15f, -15f);

        private Canvas _rootCanvas;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            Hide();
        }

        public void Show(string text, Vector2 screenPosition)
        {
            _tooltipText.text = text;
            _layoutElement.enabled = _tooltipText.preferredWidth > _maxWidth;

            _tooltipPanel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipPanel);

            // ScreenSpaceOverlay: 스크린 좌표를 Canvas 스케일로 나누면 anchoredPosition
            var scaleFactor = _rootCanvas.scaleFactor;
            var pos = screenPosition / scaleFactor + _offset;

            var tooltipSize = _tooltipPanel.rect.size;
            var screenW = Screen.width / scaleFactor;
            var screenH = Screen.height / scaleFactor;

            // 오른쪽 넘침
            if (pos.x + tooltipSize.x > screenW)
                pos.x = screenPosition.x / scaleFactor - tooltipSize.x - _offset.x;

            // 위 넘침 (pivot이 좌상단이므로 pos.y가 높을수록 위)
            if (pos.y > screenH)
                pos.y = screenH;

            // 아래 넘침 (pos.y - tooltipSize 가 0 이하)
            if (pos.y - tooltipSize.y < 0)
                pos.y = tooltipSize.y;

            _tooltipPanel.anchoredPosition = pos;
        }

        public void Hide()
        {
            if (_tooltipPanel != null)
                _tooltipPanel.gameObject.SetActive(false);
        }
    }
}
