using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 2: Bootstrapper — VContainer 대신 수동으로 Model+View+Presenter 조립.
    /// SerializeField로 View 참조, Start()에서 생성+연결.
    /// </summary>
    public class ResourceBootstrapper : MonoBehaviour
    {
        [SerializeField] private ResourcePanelView _view;

        private ResourceModel _model;
        private ResourcePanelPresenter _presenter;

        private void Start()
        {
            _model = new ResourceModel(gold: 50, wood: 30, food: 20);
            _presenter = new ResourcePanelPresenter(_model, _view);
            _presenter.Initialize();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }
    }
}
