using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 7: Bootstrapper — 인벤토리 + 설정 패널 수동 조립.
    /// Settings는 기본 숨김, 인벤토리의 Settings 버튼으로 열고 X 버튼으로 닫음.
    /// </summary>
    public class InventorySettingsBootstrapper : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private InventoryView _inventoryView;
        [SerializeField] private UIDocument _inventoryDoc;

        [Header("Settings")]
        [SerializeField] private SettingsView _settingsView;
        [SerializeField] private UIDocument _settingsDoc;

        private InventoryModel _inventoryModel;
        private SettingsModel _settingsModel;
        private InventoryPresenter _inventoryPresenter;
        private SettingsPresenter _settingsPresenter;

        // Settings 열기/닫기 버튼
        private Button _openSettingsBtn;
        private Button _closeSettingsBtn;

        private void Start()
        {
            // Inventory
            _inventoryModel = new InventoryModel();
            _inventoryPresenter = new InventoryPresenter(_inventoryModel, _inventoryView);
            _inventoryPresenter.Initialize();

            // Settings
            _settingsModel = new SettingsModel();
            _settingsPresenter = new SettingsPresenter(_settingsModel, _settingsView);
            _settingsPresenter.Initialize();

            // Settings 기본 숨김
            _settingsView.Hide();

            // 열기 버튼 (인벤토리 UXML에 있음)
            _openSettingsBtn = _inventoryDoc.rootVisualElement.Q<Button>("btn-toggle-settings");
            if (_openSettingsBtn != null)
                _openSettingsBtn.clicked += OnOpenSettings;

            // 닫기 버튼 (설정 UXML에 있음)
            _closeSettingsBtn = _settingsDoc.rootVisualElement.Q<Button>("btn-close-settings");
            if (_closeSettingsBtn != null)
                _closeSettingsBtn.clicked += OnCloseSettings;
        }

        private void OnOpenSettings() => _settingsView.Show();
        private void OnCloseSettings() => _settingsView.Hide();

        private void OnDestroy()
        {
            if (_openSettingsBtn != null)  _openSettingsBtn.clicked  -= OnOpenSettings;
            if (_closeSettingsBtn != null) _closeSettingsBtn.clicked -= OnCloseSettings;
            _inventoryPresenter?.Dispose();
            _settingsPresenter?.Dispose();
        }
    }
}
