using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 7: 설정 View — TabView + 각종 입력 요소.
    /// SetValueWithoutNotify로 Cancel 시 이벤트 발화 없이 복구 가능.
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        // Cached elements
        private DropdownField _qualityDropdown;
        private Toggle _fullscreenToggle;
        private Slider _masterSlider;
        private Slider _sfxSlider;
        private DropdownField _difficultyDropdown;
        private Button _applyBtn;
        private Button _cancelBtn;

        private VisualElement _rootElement;

        // Events for Presenter
        public event Action<int> OnQualityChanged;
        public event Action<bool> OnFullscreenChanged;
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;
        public event Action<int> OnDifficultyChanged;
        public event Action OnApplyClicked;
        public event Action OnCancelClicked;

        // Named handlers
        private void HandleQualityChanged(ChangeEvent<string> evt) => OnQualityChanged?.Invoke(_qualityDropdown.index);
        private void HandleFullscreenChanged(ChangeEvent<bool> evt) => OnFullscreenChanged?.Invoke(evt.newValue);
        private void HandleMasterChanged(ChangeEvent<float> evt) => OnMasterVolumeChanged?.Invoke(evt.newValue);
        private void HandleSfxChanged(ChangeEvent<float> evt) => OnSfxVolumeChanged?.Invoke(evt.newValue);
        private void HandleDifficultyChanged(ChangeEvent<string> evt) => OnDifficultyChanged?.Invoke(_difficultyDropdown.index);
        private void HandleApplyClicked() => OnApplyClicked?.Invoke();
        private void HandleCancelClicked() => OnCancelClicked?.Invoke();

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _rootElement = root;

            _qualityDropdown    = root.Q<DropdownField>("quality-dropdown");
            _fullscreenToggle   = root.Q<Toggle>("fullscreen-toggle");
            _masterSlider       = root.Q<Slider>("master-slider");
            _sfxSlider          = root.Q<Slider>("sfx-slider");
            _difficultyDropdown = root.Q<DropdownField>("difficulty-dropdown");
            _applyBtn           = root.Q<Button>("btn-apply");
            _cancelBtn          = root.Q<Button>("btn-cancel");

            _qualityDropdown.RegisterValueChangedCallback(HandleQualityChanged);
            _fullscreenToggle.RegisterValueChangedCallback(HandleFullscreenChanged);
            _masterSlider.RegisterValueChangedCallback(HandleMasterChanged);
            _sfxSlider.RegisterValueChangedCallback(HandleSfxChanged);
            _difficultyDropdown.RegisterValueChangedCallback(HandleDifficultyChanged);

            _applyBtn.clicked  += HandleApplyClicked;
            _cancelBtn.clicked += HandleCancelClicked;
        }

        private void OnDisable()
        {
            if (_qualityDropdown != null) _qualityDropdown.UnregisterValueChangedCallback(HandleQualityChanged);
            if (_fullscreenToggle != null) _fullscreenToggle.UnregisterValueChangedCallback(HandleFullscreenChanged);
            if (_masterSlider != null) _masterSlider.UnregisterValueChangedCallback(HandleMasterChanged);
            if (_sfxSlider != null) _sfxSlider.UnregisterValueChangedCallback(HandleSfxChanged);
            if (_difficultyDropdown != null) _difficultyDropdown.UnregisterValueChangedCallback(HandleDifficultyChanged);

            if (_applyBtn != null)  _applyBtn.clicked  -= HandleApplyClicked;
            if (_cancelBtn != null) _cancelBtn.clicked -= HandleCancelClicked;
        }

        public void Show() => _rootElement.style.display = DisplayStyle.Flex;
        public void Hide() => _rootElement.style.display = DisplayStyle.None;

        /// <summary>
        /// SetValueWithoutNotify로 이벤트 발화 없이 UI 상태 설정 (Cancel 복구용).
        /// </summary>
        public void SetValues(SettingsModel model)
        {
            _qualityDropdown.SetValueWithoutNotify(_qualityDropdown.choices[model.QualityLevel]);
            _fullscreenToggle.SetValueWithoutNotify(model.Fullscreen);
            _masterSlider.SetValueWithoutNotify(model.MasterVolume);
            _sfxSlider.SetValueWithoutNotify(model.SfxVolume);
            _difficultyDropdown.SetValueWithoutNotify(_difficultyDropdown.choices[model.Difficulty]);
        }
    }
}
