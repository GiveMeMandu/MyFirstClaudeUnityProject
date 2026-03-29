using UIStudy.AdvancedHUD.Models;
using UIStudy.AdvancedHUD.Presenters;
using UIStudy.AdvancedHUD.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.AdvancedHUD.LifetimeScopes
{
    public class ResourceHUDLifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private ResourceHUDView _resourceHUDView;

        protected override void Configure(IContainerBuilder builder)
        {
            // Model
            builder.Register<ResourceHUDModel>(Lifetime.Singleton);

            // View
            builder.RegisterComponent(_resourceHUDView);

            // Presenter
            builder.RegisterEntryPoint<ResourceHUDPresenter>();
        }
    }
}
