using UIStudy.GameUI.Presenters;
using UIStudy.GameUI.Services;
using UIStudy.GameUI.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.GameUI.LifetimeScopes
{
    public class DamageNumberLifetimeScope : LifetimeScope
    {
        [Header("Views")]
        [SerializeField] private DamageNumberDemoView _demoView;

        [Header("Pool")]
        [SerializeField] private DamageNumberView _damageNumberPrefab;
        [SerializeField] private RectTransform _numberContainer;

        protected override void Configure(IContainerBuilder builder)
        {
            // View
            builder.RegisterComponent(_demoView);

            // Service (object pool)
            builder.Register<DamageNumberService>(Lifetime.Singleton)
                .WithParameter("prefab", _damageNumberPrefab)
                .WithParameter("container", _numberContainer);

            // Presenter
            builder.RegisterEntryPoint<DamageNumberDemoPresenter>();
        }
    }
}
