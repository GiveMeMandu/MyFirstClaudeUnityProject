using UIStudy.InGameUI.Presenters;
using UIStudy.InGameUI.Services;
using UIStudy.InGameUI.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.InGameUI.LifetimeScopes
{
    public class InGameUILifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private InGameUIDemoView _demoView;

        [Header("Floating Resource Pool")]
        [SerializeField] private FloatingResourceView _floatingPrefab;
        [SerializeField] private RectTransform _floatingContainer;

        [Header("Scene References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private BuildingView[] _buildings;

        protected override void Configure(IContainerBuilder builder)
        {
            // View
            builder.RegisterComponent(_demoView);

            // Camera
            builder.RegisterInstance(_mainCamera);

            // Buildings array
            builder.RegisterInstance(_buildings);

            // Floating resource service (object pool)
            builder.Register<FloatingResourceService>(Lifetime.Singleton)
                .WithParameter("prefab", _floatingPrefab)
                .WithParameter("container", _floatingContainer);

            // Presenter
            builder.RegisterEntryPoint<InGameUIDemoPresenter>();
        }
    }
}
