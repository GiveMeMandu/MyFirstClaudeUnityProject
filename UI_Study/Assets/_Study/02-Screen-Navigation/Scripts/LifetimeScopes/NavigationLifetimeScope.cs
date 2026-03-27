using UIStudy.Navigation.Services;
using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Page;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Navigation.LifetimeScopes
{
    public class NavigationLifetimeScope : LifetimeScope
    {
        [SerializeField] private PageContainer _pageContainer;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_pageContainer);
            builder.Register<NavigationService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<NavigationBootstrapper>();
        }
    }
}
