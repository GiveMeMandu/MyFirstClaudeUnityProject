using UIStudy.Assets.Presenters;
using UIStudy.Assets.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Assets.LifetimeScopes
{
    public class AtlasGalleryLifetimeScope : LifetimeScope
    {
        [SerializeField] private AtlasGalleryView _atlasGalleryView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_atlasGalleryView);
            builder.RegisterEntryPoint<AtlasGalleryPresenter>();
        }
    }
}
