using UIStudy.Theme.Presenters;
using UIStudy.Theme.Services;
using UIStudy.Theme.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Theme.LifetimeScopes
{
    public class ThemeDemoLifetimeScope : LifetimeScope
    {
        [SerializeField] private ThemeDemoView _themeDemoView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ThemeService>(Lifetime.Singleton);
            builder.Register<AccessibilityService>(Lifetime.Singleton);
            builder.RegisterComponent(_themeDemoView);
            builder.RegisterEntryPoint<ThemeAccessibilityPresenter>();
        }
    }
}
