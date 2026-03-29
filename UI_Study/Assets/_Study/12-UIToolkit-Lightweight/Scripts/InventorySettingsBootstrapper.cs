using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 7: Bootstrapper — 인벤토리 + 설정 패널 수동 조립.
    /// VContainer 없이 SerializeField로 View 참조, Start()에서 생성+연결.
    /// </summary>
    public class InventorySettingsBootstrapper : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private InventoryView _inventoryView;

        [Header("Settings")]
        [SerializeField] private SettingsView _settingsView;

        private InventoryModel _inventoryModel;
        private SettingsModel _settingsModel;
        private InventoryPresenter _inventoryPresenter;
        private SettingsPresenter _settingsPresenter;

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
        }

        private void OnDestroy()
        {
            _inventoryPresenter?.Dispose();
            _settingsPresenter?.Dispose();
        }
    }
}
