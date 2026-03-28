using UIStudy.R3Advanced.Models;
using UIStudy.R3Advanced.Presenters;
using UIStudy.R3Advanced.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.R3Advanced.LifetimeScopes
{
    public class ReactiveCommandLifetimeScope : LifetimeScope
    {
        [SerializeField] private PurchaseView _view;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<PurchaseModel>(Lifetime.Singleton);
            builder.RegisterComponent(_view);
            builder.RegisterEntryPoint<PurchasePresenter>();
        }
    }
}
