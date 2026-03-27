using System;
using R3;
using UIStudy.Advanced.Services;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIStudy.Advanced.Views
{
    /// <summary>
    /// 툴팁 트리거 — 호버 가능한 UI 요소에 부착.
    /// 0.4초 딜레이 후 TooltipService.Show() 호출.
    /// 마우스 이탈 시 즉시 숨김.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string _tooltipText = "Tooltip text here";
        [SerializeField] private float _delay = 0.4f;

        private TooltipService _tooltipService;
        private IDisposable _delaySubscription;

        /// <summary>
        /// VContainer로 주입하거나 FindObjectOfType으로 설정.
        /// </summary>
        public void SetService(TooltipService service) => _tooltipService = service;

        private void Start()
        {
            if (_tooltipService == null)
                _tooltipService = FindAnyObjectByType<TooltipService>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _delaySubscription?.Dispose();
            _delaySubscription = Observable.Timer(TimeSpan.FromSeconds(_delay))
                .Subscribe(_ =>
                {
                    _tooltipService?.Show(_tooltipText, eventData.position);
                });
        }

        public void OnPointerExit(PointerEventData eventData)
        {
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
