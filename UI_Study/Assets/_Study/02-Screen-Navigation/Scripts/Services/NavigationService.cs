using System;
using UnityScreenNavigator.Runtime.Core.Page;
using UnityScreenNavigator.Runtime.Foundation.Coroutine;

namespace UIStudy.Navigation.Services
{
    /// <summary>
    /// PageContainer를 래핑하는 네비게이션 서비스.
    /// VContainer로 주입하여 Presenter에서 화면 전환을 요청한다.
    /// resourceKey는 Resources 폴더 내 프리팹 경로.
    /// </summary>
    public class NavigationService
    {
        private readonly PageContainer _pageContainer;

        public NavigationService(PageContainer pageContainer)
        {
            _pageContainer = pageContainer;
        }

        public AsyncProcessHandle Push(string resourceKey, bool playAnimation = true,
            bool loadAsync = true,
            Action<(string pageId, Page page)> onLoad = null)
        {
            return _pageContainer.Push(resourceKey, playAnimation, loadAsync: loadAsync, onLoad: onLoad);
        }

        public AsyncProcessHandle Pop(bool playAnimation = true)
        {
            return _pageContainer.Pop(playAnimation);
        }
    }
}
