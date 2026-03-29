using UnityEngine;

namespace UIStudy.AdvancedHUD.Views
{
    /// <summary>
    /// Resource HUD View — 상단 바에 5개 ResourceBarView 인스턴스 배치.
    /// </summary>
    public class ResourceHUDView : MonoBehaviour
    {
        [Header("Resource Bars")]
        [SerializeField] private ResourceBarView _goldBar;
        [SerializeField] private ResourceBarView _woodBar;
        [SerializeField] private ResourceBarView _stoneBar;
        [SerializeField] private ResourceBarView _foodBar;
        [SerializeField] private ResourceBarView _populationBar;

        public ResourceBarView GoldBar => _goldBar;
        public ResourceBarView WoodBar => _woodBar;
        public ResourceBarView StoneBar => _stoneBar;
        public ResourceBarView FoodBar => _foodBar;
        public ResourceBarView PopulationBar => _populationBar;
    }
}
