using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// 싱글톤 툴팁 컨트롤러.
    /// RectTransformUtility로 Canvas 모드 무관 좌표 변환 + Pivot 동적 전환으로 경계 처리.
    /// </summary>
    public class TooltipService : MonoBehaviour
    {
        [SerializeField] private RectTransform _tooltipPanel;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private float _maxWidth = 300f;
        [SerializeField] private float _cursorGap = 15f;
        [SerializeField] private Color _backgroundColor = new(0f, 0f, 0f, 0.85f);

        private Canvas _rootCanvas;
        private RectTransform _parentRect;
        private Image _backgroundImage;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            _parentRect = _tooltipPanel.parent as RectTransform;

            // Pivot flip을 위해 중앙 앵커 설정 (anchoredPosition = Canvas 로컬 좌표)
            _tooltipPanel.anchorMin = Vector2.one * 0.5f;
            _tooltipPanel.anchorMax = Vector2.one * 0.5f;

            // 반투명 검은 배경 보장 + Raycast Target 전체 비활성화
            EnsureBackground();
            DisableRaycastTargets();

            Hide();
        }

        private void EnsureBackground()
        {
            _backgroundImage = _tooltipPanel.GetComponent<Image>();
            if (_backgroundImage == null)
                _backgroundImage = _tooltipPanel.gameObject.AddComponent<Image>();

            _backgroundImage.color = _backgroundColor;
            _backgroundImage.raycastTarget = false;
        }

        private void DisableRaycastTargets()
        {
            foreach (var graphic in _tooltipPanel.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        public void Show(string text, Vector2 screenPosition)
        {
            _tooltipText.text = text;
            _layoutElement.enabled = _tooltipText.preferredWidth > _maxWidth;

            _tooltipPanel.gameObject.SetActive(true);

            // 최상단 표시 (다른 UI 요소 위로)
            _tooltipPanel.SetAsLastSibling();

            // 1. ContentSizeFitter 크기 확정
            LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipPanel);

            // 2. 화면 사분면에 따라 Pivot 동적 전환
            bool rightHalf = screenPosition.x > Screen.width * 0.5f;
            bool topHalf = screenPosition.y > Screen.height * 0.5f;
            _tooltipPanel.pivot = new Vector2(
                rightHalf ? 1f : 0f,
                topHalf ? 1f : 0f);

            // 3. Canvas 모드 무관 좌표 변환
            Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _rootCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRect, screenPosition, cam, out Vector2 localPoint);

            // 4. Pivot 방향에 맞는 오프셋 적용
            var offset = new Vector2(
                rightHalf ? -_cursorGap : _cursorGap,
                topHalf ? -_cursorGap : _cursorGap);

            _tooltipPanel.anchoredPosition = localPoint + offset;
        }

        public void Hide()
        {
            if (_tooltipPanel != null)
                _tooltipPanel.gameObject.SetActive(false);
        }
    }
}
