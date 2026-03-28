using UIStudy.Animation.Presenters;
using UIStudy.Animation.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Animation.LifetimeScopes
{
    public class PanelTransitionLifetimeScope : LifetimeScope
    {
        [SerializeField] private PanelTransitionView _panelTransitionView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_panelTransitionView);
            builder.RegisterEntryPoint<PanelTransitionPresenter>();
        }
    }
}
