using UIStudy.Assets.Presenters;
using UIStudy.Assets.Services;
using UIStudy.Assets.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Assets.LifetimeScopes
{
    public class AssetDemoLifetimeScope : LifetimeScope
    {
        [SerializeField] private AssetDemoView _assetDemoView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<AddressableAssetService>(Lifetime.Scoped);
            builder.RegisterComponent(_assetDemoView);
            builder.RegisterEntryPoint<AssetDemoPresenter>();
        }
    }
}
