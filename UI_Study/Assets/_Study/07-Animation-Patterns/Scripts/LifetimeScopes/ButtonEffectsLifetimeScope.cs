using UIStudy.Animation.Presenters;
using UIStudy.Animation.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Animation.LifetimeScopes
{
    public class ButtonEffectsLifetimeScope : LifetimeScope
    {
        [SerializeField] private ButtonEffectsView _buttonEffectsView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_buttonEffectsView);
            builder.RegisterEntryPoint<ButtonEffectsPresenter>();
        }
    }
}
