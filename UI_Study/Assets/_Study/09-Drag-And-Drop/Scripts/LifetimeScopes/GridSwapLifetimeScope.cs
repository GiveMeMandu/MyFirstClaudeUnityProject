using UIStudy.DragDrop.Models;
using UIStudy.DragDrop.Presenters;
using UIStudy.DragDrop.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.DragDrop.LifetimeScopes
{
    public class GridSwapLifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private GridSwapView _gridView;

        protected override void Configure(IContainerBuilder builder)
        {
            // Model
            builder.Register<GridSwapModel>(Lifetime.Singleton);

            // View
            builder.RegisterComponent(_gridView);

            // Presenter
            builder.RegisterEntryPoint<GridSwapPresenter>();
        }
    }
}
