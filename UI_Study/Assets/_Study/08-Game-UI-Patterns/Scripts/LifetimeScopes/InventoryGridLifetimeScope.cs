using UIStudy.GameUI.Models;
using UIStudy.GameUI.Presenters;
using UIStudy.GameUI.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.GameUI.LifetimeScopes
{
    public class InventoryGridLifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private InventoryGridView _gridView;

        protected override void Configure(IContainerBuilder builder)
        {
            // Model
            builder.Register<InventoryModel>(Lifetime.Singleton);

            // View
            builder.RegisterComponent(_gridView);

            // Presenter
            builder.RegisterEntryPoint<InventoryPresenter>();
        }
    }
}
