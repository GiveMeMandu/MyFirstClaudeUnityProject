using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 4: DOTween Sequence 기반 패널 Show/Hide.
    /// Scale(0→1) + Fade(0→1) 동시 재생, 역순으로 닫힘.
    /// </summary>
    public class AnimatedPanelView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _panel;
        private Sequence _currentSequence;

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _panel = root.Q<VisualElement>("animated-panel");

            // 초기 상태: 숨김
            _panel.style.opacity = 0f;
            _panel.style.scale = new Scale(new Vector3(0.5f, 0.5f, 1f));
            _panel.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            _currentSequence?.Kill();
        }

        public void ShowPanel()
        {
            _currentSequence?.Kill();

            _panel.style.display = DisplayStyle.Flex;
            _panel.style.opacity = 0f;
            _panel.style.scale = new Scale(new Vector3(0.5f, 0.5f, 1f));

            _currentSequence = DOTween.Sequence()
                .Join(_panel.DOFade(1f, 0.25f))
                .Join(_panel.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        }

        public void HidePanel()
        {
            _currentSequence?.Kill();

            _currentSequence = DOTween.Sequence()
                .Join(_panel.DOFade(0f, 0.2f))
                .Join(_panel.DOScale(0.5f, 0.2f).SetEase(Ease.InBack))
                .OnComplete(() => _panel.style.display = DisplayStyle.None);
        }
    }
}
