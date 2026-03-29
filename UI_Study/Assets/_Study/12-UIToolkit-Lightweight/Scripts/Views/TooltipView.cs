using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 6: 툴팁 View — 마우스 근처에 건물 정보 표시.
    /// PointerMoveEvent로 위치 추적, 화면 경계 클램핑.
    /// </summary>
    public class TooltipView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _tooltipRoot;
        private Label _nameLabel;
        private Label _goldLabel;
        private Label _woodLabel;
        private Label _descLabel;
        private VisualElement _panelRoot;

        private const float OffsetX = 16f;
        private const float OffsetY = 16f;

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _tooltipRoot = root.Q<VisualElement>("tooltip-root");
            _nameLabel   = root.Q<Label>("tooltip-name");
            _goldLabel   = root.Q<Label>("tooltip-gold");
            _woodLabel   = root.Q<Label>("tooltip-wood");
            _descLabel   = root.Q<Label>("tooltip-desc");

            // tooltip의 부모 패널 (화면 경계 계산용)
            _panelRoot = root;

            // PointerMove 등록 — 전체 패널에서 마우스 추적
            _panelRoot.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
        }

        private void OnDisable()
        {
            if (_panelRoot != null)
            {
                _panelRoot.UnregisterCallback<PointerMoveEvent>(HandlePointerMove);
            }
        }

        public void ShowTooltip(BuildingData data, Vector2 position)
        {
            _nameLabel.text = data.Name;
            _goldLabel.text = data.GoldCost.ToString();
            _woodLabel.text = data.WoodCost.ToString();
            _descLabel.text = data.Description;

            SetPosition(position);
            _tooltipRoot.style.display = DisplayStyle.Flex;
        }

        public void HideTooltip()
        {
            _tooltipRoot.style.display = DisplayStyle.None;
        }

        private void HandlePointerMove(PointerMoveEvent evt)
        {
            // 툴팁이 보이는 상태에서만 위치 갱신
            if (_tooltipRoot.resolvedStyle.display == DisplayStyle.None) return;

            var pos = new Vector2(evt.position.x, evt.position.y);
            SetPosition(pos);
        }

        private void SetPosition(Vector2 pointerPos)
        {
            float x = pointerPos.x + OffsetX;
            float y = pointerPos.y + OffsetY;

            // 화면 경계 클램핑
            float panelWidth = _panelRoot.resolvedStyle.width;
            float panelHeight = _panelRoot.resolvedStyle.height;
            float tooltipWidth = _tooltipRoot.resolvedStyle.width;
            float tooltipHeight = _tooltipRoot.resolvedStyle.height;

            // resolvedStyle가 아직 계산 안 됐으면 대략적 값 사용
            if (float.IsNaN(tooltipWidth) || tooltipWidth <= 0) tooltipWidth = 200f;
            if (float.IsNaN(tooltipHeight) || tooltipHeight <= 0) tooltipHeight = 80f;

            if (x + tooltipWidth > panelWidth)
                x = pointerPos.x - tooltipWidth - OffsetX;
            if (y + tooltipHeight > panelHeight)
                y = pointerPos.y - tooltipHeight - OffsetY;

            if (x < 0) x = 0;
            if (y < 0) y = 0;

            _tooltipRoot.style.left = x;
            _tooltipRoot.style.top = y;
        }
    }
}
