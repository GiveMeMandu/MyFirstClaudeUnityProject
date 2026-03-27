using UIStudy.MVRP.Models;
using UIStudy.MVRP.Presenters;
using UIStudy.MVRP.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.MVRP.LifetimeScopes
{
    /// <summary>
    /// 카운터 씬의 LifetimeScope.
    /// View(MonoBehaviour)는 RegisterComponent, Presenter는 RegisterEntryPoint로 등록.
    /// </summary>
    public class CounterLifetimeScope : LifetimeScope
    {
        [SerializeField] private CounterView _counterView;

        protected override void Configure(IContainerBuilder builder)
        {
            // Model — Singleton (씬 내 하나)
            builder.Register<CounterModel>(Lifetime.Singleton);

            // View — 씬에 이미 존재하는 MonoBehaviour를 등록
            builder.RegisterComponent(_counterView);

            // Presenter — EntryPoint로 등록 (Initialize/Dispose 자동 호출)
            builder.RegisterEntryPoint<CounterPresenter>();
        }
    }
}
