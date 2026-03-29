using UIStudy.AdvancedHUD.Models;
using UIStudy.AdvancedHUD.Presenters;
using UIStudy.AdvancedHUD.Services;
using UIStudy.AdvancedHUD.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.AdvancedHUD.LifetimeScopes
{
    public class LoadingScreenLifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private LoadingScreenView _loadingScreenView;
        [SerializeField] private LoadingDemoView _loadingDemoView;

        protected override void Configure(IContainerBuilder builder)
        {
            // Model
            builder.Register<LoadingModel>(Lifetime.Singleton);

            // Views
            builder.RegisterComponent(_loadingScreenView);
            builder.RegisterComponent(_loadingDemoView);

            // Service
            builder.Register<FakeLoadingService>(Lifetime.Singleton);

            // Presenter
            builder.RegisterEntryPoint<LoadingScreenPresenter>();
        }
    }
}
