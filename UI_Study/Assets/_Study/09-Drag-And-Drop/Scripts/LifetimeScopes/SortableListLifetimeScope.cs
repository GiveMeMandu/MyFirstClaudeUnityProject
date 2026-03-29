using UIStudy.DragDrop.Models;
using UIStudy.DragDrop.Presenters;
using UIStudy.DragDrop.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.DragDrop.LifetimeScopes
{
    public class SortableListLifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private SortableListView _listView;

        protected override void Configure(IContainerBuilder builder)
        {
            // Model
            builder.Register<SortableListModel>(Lifetime.Singleton);

            // View
            builder.RegisterComponent(_listView);

            // Presenter
            builder.RegisterEntryPoint<SortableListPresenter>();
        }
    }
}
