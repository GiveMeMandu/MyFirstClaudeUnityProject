using UIStudy.GameUI.Presenters;
using UIStudy.GameUI.Services;
using UIStudy.GameUI.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.GameUI.LifetimeScopes
{
    public class DialogTypewriterLifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private DialogDemoView _demoView;
        [SerializeField] private DialogView _dialogView;

        protected override void Configure(IContainerBuilder builder)
        {
            // Views
            builder.RegisterComponent(_demoView);
            builder.RegisterComponent(_dialogView);

            // Service
            builder.Register<DialogService>(Lifetime.Singleton);

            // Presenter
            builder.RegisterEntryPoint<DialogDemoPresenter>();
        }
    }
}
