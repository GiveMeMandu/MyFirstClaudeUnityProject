using R3;
using UIStudy.Navigation.Pages;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.Navigation.Services
{
    /// <summary>
    /// 앱 시작 시 첫 번째 페이지를 Push하고,
    /// 각 페이지의 버튼 이벤트를 네비게이션에 연결하는 부트스트래퍼.
    /// </summary>
    public class NavigationBootstrapper : IStartable
    {
        private readonly NavigationService _navigation;

        public NavigationBootstrapper(NavigationService navigation)
        {
            _navigation = navigation;
        }

        public void Start()
        {
            // 첫 번째 페이지 Push + 이벤트 연결
            _navigation.Push("MainPage", true, onLoad: args =>
            {
                var mainPage = (MainPageView)args.page;
                mainPage.SetTitle("Main Page");
                mainPage.OnGoToDetailClick.Subscribe(_ => PushDetailPage())
                    .AddTo(mainPage);
            });
        }

        private void PushDetailPage()
        {
            _navigation.Push("DetailPage", true, onLoad: args =>
            {
                var detailPage = (DetailPageView)args.page;
                detailPage.SetTitle("Detail Page");
                detailPage.SetContent("This is the detail page.\nNavigated from Main.");

                detailPage.OnGoToSettingsClick.Subscribe(_ => PushSettingsPage())
                    .AddTo(detailPage);

                detailPage.OnBackClick.Subscribe(_ => _navigation.Pop())
                    .AddTo(detailPage);
            });
        }

        private void PushSettingsPage()
        {
            _navigation.Push("SettingsPage", true, onLoad: args =>
            {
                var settingsPage = (SettingsPageView)args.page;
                settingsPage.SetTitle("Settings Page");

                settingsPage.OnBackClick.Subscribe(_ => _navigation.Pop())
                    .AddTo(settingsPage);
            });
        }
    }
}
