using UIStudy.DragDrop.Presenters;
using UIStudy.DragDrop.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.DragDrop.LifetimeScopes
{
    public class BasicDragDropLifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private BasicDragDropDemoView _demoView;

        protected override void Configure(IContainerBuilder builder)
        {
            // View
            builder.RegisterComponent(_demoView);

            // Presenter
            builder.RegisterEntryPoint<BasicDragDropPresenter>();
        }
    }
}
