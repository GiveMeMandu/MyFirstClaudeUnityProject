using UIStudy.MVRP.Models;
using UIStudy.MVRP.Presenters;
using UIStudy.MVRP.Services;
using UIStudy.MVRP.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.MVRP.LifetimeScopes
{
    public class DialogLifetimeScope : LifetimeScope
    {
        [SerializeField] private ResourceHUDView _resourceHUDView;
        [SerializeField] private ConfirmDialogView _confirmDialogView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ResourceModel>(Lifetime.Singleton);
            builder.Register<DialogService>(Lifetime.Singleton);
            builder.RegisterComponent(_resourceHUDView);
            builder.RegisterComponent(_confirmDialogView);
            builder.RegisterEntryPoint<ResourceWithDialogPresenter>();
        }
    }
}
