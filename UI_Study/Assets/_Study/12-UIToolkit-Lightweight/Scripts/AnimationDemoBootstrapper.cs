using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 4: AnimationDemo 씬 조립 — CSS Transition은 코드 불필요,
    /// DOTween 패널/스태거만 버튼으로 트리거.
    /// </summary>
    public class AnimationDemoBootstrapper : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private AnimatedPanelView _panelView;
        [SerializeField] private StaggerListView _staggerView;

        private Button _showBtn;
        private Button _hideBtn;
        private Button _staggerBtn;

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _showBtn    = root.Q<Button>("btn-show-panel");
            _hideBtn    = root.Q<Button>("btn-hide-panel");
            _staggerBtn = root.Q<Button>("btn-stagger");

            _showBtn.clicked    += OnShowPanel;
            _hideBtn.clicked    += OnHidePanel;
            _staggerBtn.clicked += OnPlayStagger;
        }

        private void OnDisable()
        {
            if (_showBtn != null)    _showBtn.clicked    -= OnShowPanel;
            if (_hideBtn != null)    _hideBtn.clicked    -= OnHidePanel;
            if (_staggerBtn != null) _staggerBtn.clicked -= OnPlayStagger;
        }

        private void OnShowPanel()    => _panelView.ShowPanel();
        private void OnHidePanel()    => _panelView.HidePanel();
        private void OnPlayStagger()  => _staggerView.PlayStagger();
    }
}
