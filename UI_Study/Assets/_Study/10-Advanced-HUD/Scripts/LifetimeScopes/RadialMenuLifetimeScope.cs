using UIStudy.AdvancedHUD.Models;
using UIStudy.AdvancedHUD.Presenters;
using UIStudy.AdvancedHUD.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.AdvancedHUD.LifetimeScopes
{
    public class RadialMenuLifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private RadialMenuView _radialMenuView;

        protected override void Configure(IContainerBuilder builder)
        {
            // Model
            builder.Register<RadialMenuModel>(Lifetime.Singleton);

            // View
            builder.RegisterComponent(_radialMenuView);

            // Presenter
            builder.RegisterEntryPoint<RadialMenuPresenter>();
        }
    }
}
