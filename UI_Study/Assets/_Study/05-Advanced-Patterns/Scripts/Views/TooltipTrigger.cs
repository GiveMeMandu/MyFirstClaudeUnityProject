using System;
using R3;
using UIStudy.Advanced.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UIStudy.Advanced.Views
{
    /// <summary>
    /// 툴팁 트리거 — 호버 가능한 UI 요소에 부착.
    /// 0.4초 딜레이 후 현재 마우스 위치에서 TooltipService.Show() 호출.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string _tooltipText = "Tooltip text here";
        [SerializeField] private float _delay = 0.4f;

        private TooltipService _tooltipService;
        private IDisposable _delaySubscription;
        private bool _isHovering;

        public void SetService(TooltipService service) => _tooltipService = service;

        private void Start()
        {
            if (_tooltipService == null)
                _tooltipService = FindAnyObjectByType<TooltipService>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            _delaySubscription?.Dispose();
            _delaySubscription = Observable.Timer(TimeSpan.FromSeconds(_delay))
                .Subscribe(_ =>
                {
                    if (_isHovering && _tooltipService != null)
                    {
                        // 딜레이 후 현재 마우스 위치 사용 (New Input System)
                        var mousePos = Mouse.current != null
                            ? Mouse.current.position.ReadValue()
                            : Vector2.zero;
                        _tooltipService.Show(_tooltipText, mousePos);
                    }
                });
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            _delaySubscription?.Dispose();
            _delaySubscription = null;
            _tooltipService?.Hide();
        }

        private void OnDestroy()
        {
            _delaySubscription?.Dispose();
        }
    }
}
