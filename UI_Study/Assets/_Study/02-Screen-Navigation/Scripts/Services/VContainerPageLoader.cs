using System;
using R3;
using UIStudy.Navigation.Pages;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Navigation.Services
{
    /// <summary>
    /// VContainer + UnityScreenNavigator 통합 부트스트래퍼.
    /// Page Push 시 자식 LifetimeScope를 동적으로 생성하여 DI 스코핑을 실현한다.
    ///
    /// 학습 포인트:
    /// - CreateChildFromPrefab: 부모 스코프의 등록을 상속하는 자식 스코프 생성
    /// - Dispose: Pop 시 자식 스코프 자동 파괴 → Presenter.Dispose() 호출
    /// </summary>
    public class ScopedNavigationBootstrapper : IStartable, IDisposable
    {
        private readonly NavigationService _navigation;
        private readonly LifetimeScope _parentScope;
        private readonly CompositeDisposable _disposables = new();

        private LifetimeScope _childScope;

        public ScopedNavigationBootstrapper(NavigationService navigation, LifetimeScope parentScope)
        {
            _navigation = navigation;
            _parentScope = parentScope;
        }

        public void Start()
        {
            _navigation.Push("MainPage", true, onLoad: args =>
            {
                var mainPage = (MainPageView)args.page;
                mainPage.SetTitle("Main (Scoped Demo)");

                mainPage.OnGoToDetailClick.Subscribe(_ => PushDetailWithScope())
                    .AddTo(mainPage);
            });
        }

        private void PushDetailWithScope()
        {
            // VContainer 자식 스코프를 동적으로 생성
            // EnqueueParent로 부모 스코프를 지정하면,
            // 자식 스코프에서 부모의 NavigationService 등을 주입받을 수 있다
            _navigation.Push("DetailPage", true, onLoad: args =>
            {
                var detailPage = (DetailPageView)args.page;

                // 자식 LifetimeScope를 생성하여 Presenter 주입
                _childScope = _parentScope.CreateChild(builder =>
                {
                    builder.RegisterComponent(detailPage);
                    builder.RegisterEntryPoint<UIStudy.Navigation.Presenters.DetailPagePresenter>();
                });

                Debug.Log("[ScopedNav] 자식 LifetimeScope 생성됨");
            });
        }

        public void Dispose()
        {
            _childScope?.Dispose();
            _disposables.Dispose();
        }
    }
}
