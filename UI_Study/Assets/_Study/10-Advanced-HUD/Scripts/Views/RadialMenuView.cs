using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.AdvancedHUD.Views
{
    /// <summary>
    /// Radial Menu View — 원형 배치 8개 버튼 + 중앙 선택 표시 + 열기/닫기 애니메이션.
    /// </summary>
    public class RadialMenuView : MonoBehaviour
    {
        [Header("Menu Container")]
        [SerializeField] private RectTransform _menuContainer;

        [Header("Buttons (8 items, assigned in Awake)")]
        [SerializeField] private Button[] _menuButtons = new Button[8];
        [SerializeField] private TextMeshProUGUI[] _menuLabels = new TextMeshProUGUI[8];

        [Header("Center Display")]
        [SerializeField] private TextMeshProUGUI _centerText;

        [Header("Toggle Button")]
        [SerializeField] private Button _toggleButton;
        [SerializeField] private TextMeshProUGUI _toggleButtonText;

        [Header("Settings")]
        [SerializeField] private float _radius = 150f;
        [SerializeField] private float _animDuration = 0.3f;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color _highlightColor = new(0.3f, 0.6f, 1f, 1f);

        private bool _isOpen;
        private Tween _scaleTween;

        public Button[] MenuButtons => _menuButtons;
        public Button ToggleButton => _toggleButton;

        private void Awake()
        {
            PositionButtons();
            _menuContainer.localScale = Vector3.zero;
            _isOpen = false;
        }

        /// <summary>
        /// Atan2 기반 원형 배치 — 12시 방향부터 시계 방향.
        /// </summary>
        private void PositionButtons()
        {
            int count = _menuButtons.Length;
            for (int i = 0; i < count; i++)
            {
                // 12시 방향(90도)부터 시계 방향(-) 순서
                float angleDeg = 90f - (360f / count) * i;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                float x = Mathf.Cos(angleRad) * _radius;
                float y = Mathf.Sin(angleRad) * _radius;

                var rt = _menuButtons[i].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, y);
            }
        }

        /// <summary>
        /// 메뉴 열기/닫기 토글.
        /// </summary>
        public void ToggleMenu()
        {
            _isOpen = !_isOpen;

            _scaleTween?.Kill();

            if (_isOpen)
            {
                _menuContainer.gameObject.SetActive(true);
                _scaleTween = _menuContainer
                    .DOScale(Vector3.one, _animDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
            else
            {
                _scaleTween = _menuContainer
                    .DOScale(Vector3.zero, _animDuration)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() => _menuContainer.gameObject.SetActive(false));
            }

            if (_toggleButtonText != null)
                _toggleButtonText.text = _isOpen ? "Close Menu" : "Open Menu";
        }

        /// <summary>
        /// 중앙 텍스트 업데이트.
        /// </summary>
        public void SetCenterText(string text)
        {
            if (_centerText != null)
                _centerText.text = text;
        }

        /// <summary>
        /// 선택 하이라이트 갱신.
        /// </summary>
        public void SetHighlight(int selectedIndex)
        {
            for (int i = 0; i < _menuButtons.Length; i++)
            {
                var img = _menuButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    img.color = (i == selectedIndex) ? _highlightColor : _normalColor;
                }
            }
        }

        /// <summary>
        /// 라벨 텍스트 설정.
        /// </summary>
        public void SetLabel(int index, string text)
        {
            if (index >= 0 && index < _menuLabels.Length && _menuLabels[index] != null)
                _menuLabels[index].text = text;
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
        }
    }
}
