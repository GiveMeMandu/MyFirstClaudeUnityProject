using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 5: Bootstrapper — 건물 상세 팝업 씬 조립.
    /// ResourceModel(Step 2) + BuildingDetailView + Presenter 조합.
    /// </summary>
    public class BuildingDetailBootstrapper : MonoBehaviour
    {
        [SerializeField] private BuildingDetailView _detailView;

        private ResourceModel _resourceModel;
        private BuildingDetailPresenter _presenter;

        private void Start()
        {
            // 리소스 모델 생성 (데모용 초기값)
            _resourceModel = new ResourceModel(gold: 500, wood: 300, food: 200);

            _presenter = new BuildingDetailPresenter(_resourceModel, _detailView);

            // 데모용: 시작 시 샘플 건물 팝업 표시
            var sampleBuilding = new BuildingData(
                name: "Barracks",
                description: "Trains infantry units. Higher levels unlock elite soldiers and increase training speed.",
                goldCost: 120,
                woodCost: 80,
                level: 1,
                maxLevel: 5
            );

            _presenter.ShowBuilding(sampleBuilding);
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }
    }
}
