using UIStudy.Navigation.Pages;
using UIStudy.Navigation.Presenters;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Navigation.LifetimeScopes
{
    public class DetailPageLifetimeScope : LifetimeScope
    {
        [SerializeField] private DetailPageView _detailPageView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_detailPageView);
            builder.RegisterEntryPoint<DetailPagePresenter>();
        }
    }
}
