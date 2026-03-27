using UIStudy.MVRP.Models;
using UIStudy.MVRP.Presenters;
using UIStudy.MVRP.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.MVRP.LifetimeScopes
{
    public class ResourceLifetimeScope : LifetimeScope
    {
        [SerializeField] private ResourceHUDView _resourceHUDView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ResourceModel>(Lifetime.Singleton);
            builder.RegisterComponent(_resourceHUDView);
            builder.RegisterEntryPoint<ResourceHUDPresenter>();
        }
    }
}
