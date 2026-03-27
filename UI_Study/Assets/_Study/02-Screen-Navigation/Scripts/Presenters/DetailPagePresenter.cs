using System;
using R3;
using UIStudy.Navigation.Pages;
using UIStudy.Navigation.Services;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.Navigation.Presenters
{
    /// <summary>
    /// 상세 페이지 Presenter — 자식 LifetimeScope에서 생성되는 Pure C# Presenter.
    /// Page Push 시 자식 스코프에서 생성되고, Pop 시 Dispose가 호출된다.
    /// </summary>
    public class DetailPagePresenter : IInitializable, IDisposable
    {
        private readonly DetailPageView _view;
        private readonly NavigationService _navigation;
        private readonly CompositeDisposable _disposables = new();

        public DetailPagePresenter(DetailPageView view, NavigationService navigation)
        {
            _view = view;
            _navigation = navigation;
        }

        public void Initialize()
        {
            Debug.Log("[DetailPagePresenter] Initialize — 자식 스코프에서 생성됨");

            _view.SetTitle("Detail (Scoped)");
            _view.SetContent("이 Presenter는 자식 LifetimeScope에서 주입됨.\nPop 시 Dispose 로그 확인.");

            _view.OnGoToSettingsClick
                .Subscribe(_ => _navigation.Push("SettingsPage"))
                .AddTo(_disposables);

            _view.OnBackClick
                .Subscribe(_ => _navigation.Pop())
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            Debug.Log("[DetailPagePresenter] Dispose — 자식 스코프 파괴됨");
            _disposables.Dispose();
        }
    }
}
