using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 6: Game UI Bootstrapper — HUD + BuildMenu + Tooltip 조립.
    /// 공유 GameResourceModel로 연결. 데모용 자원 버튼 포함.
    /// </summary>
    public class GameUIBootstrapper : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private ResourceHudView _hudView;
        [SerializeField] private BuildMenuView _buildMenuView;
        [SerializeField] private TooltipView _tooltipView;

        [Header("Data")]
        [SerializeField] private BuildingCatalog _catalog;

        private GameResourceModel _resourceModel;
        private ResourceHudPresenter _hudPresenter;
        private BuildMenuPresenter _buildMenuPresenter;

        private void Start()
        {
            // 공유 자원 모델
            _resourceModel = new GameResourceModel(gold: 500, wood: 300, food: 200, pop: 10);

            // 카탈로그 없으면 기본값 생성
            if (_catalog == null)
            {
                _catalog = BuildingCatalog.CreateDefault();
            }

            // HUD Presenter
            _hudPresenter = new ResourceHudPresenter(_resourceModel, _hudView);
            _hudPresenter.Initialize();

            // Build Menu Presenter
            _buildMenuPresenter = new BuildMenuPresenter(
                _resourceModel, _buildMenuView, _tooltipView);
            _buildMenuPresenter.Initialize(_catalog.Buildings);
        }

        /// <summary>
        /// 데모용: 외부에서 자원 추가 (테스트 버튼 등에서 호출).
        /// </summary>
        public void DebugGainResources()
        {
            _resourceModel.GainResource(ResourceType.Gold, 100);
            _resourceModel.GainResource(ResourceType.Wood, 80);
            _resourceModel.GainResource(ResourceType.Food, 50);
            _resourceModel.GainResource(ResourceType.Pop, 5);
        }

        private void OnDestroy()
        {
            _hudPresenter?.Dispose();
            _buildMenuPresenter?.Dispose();
        }
    }
}
