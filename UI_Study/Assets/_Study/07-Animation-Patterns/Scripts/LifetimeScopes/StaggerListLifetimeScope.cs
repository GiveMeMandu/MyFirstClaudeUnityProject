using UIStudy.Animation.Models;
using UIStudy.Animation.Presenters;
using UIStudy.Animation.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Animation.LifetimeScopes
{
    public class StaggerListLifetimeScope : LifetimeScope
    {
        [SerializeField] private StaggerListView _staggerListView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<StaggerModel>(Lifetime.Singleton);
            builder.RegisterComponent(_staggerListView);
            builder.RegisterEntryPoint<StaggerPresenter>();
        }
    }
}
