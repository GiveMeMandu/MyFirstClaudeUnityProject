using UIStudy.Advanced.Models;
using UIStudy.Advanced.Presenters;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Advanced.LifetimeScopes
{
    public class CombinedStateLifetimeScope : LifetimeScope
    {
        [SerializeField] private CombinedStateView _view;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BuildActionModel>(Lifetime.Singleton);
            builder.RegisterComponent(_view);
            builder.RegisterEntryPoint<CombinedStatePresenter>();
        }
    }
}
