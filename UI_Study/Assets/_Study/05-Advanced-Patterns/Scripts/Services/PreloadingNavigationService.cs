using System;
using System.Collections;
using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Page;
using UnityScreenNavigator.Runtime.Foundation.Coroutine;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// 프리로드 지원 네비게이션 서비스.
    /// WillPushEnter에서 다음 화면 프리팹을 미리 로드하여
    /// 전환 시 로딩 지연을 없앰.
    ///
    /// USN의 Preload API:
    /// - container.Preload(resourceKey) → 프리팹 미리 로드
    /// - container.ReleasePreloaded(resourceKey) → 해제
    /// </summary>
    public class PreloadingNavigationService
    {
        private readonly PageContainer _pageContainer;

        public PreloadingNavigationService(PageContainer pageContainer)
        {
            _pageContainer = pageContainer;
        }

        /// <summary>
        /// 다음 화면을 프리로드하면서 현재 화면을 Push.
        /// onLoad 콜백에서 다음 화면의 프리로드를 시작.
        /// </summary>
        public AsyncProcessHandle PushWithPreload(
            string resourceKey,
            string preloadResourceKey,
            bool playAnimation = true,
            Action<(string pageId, Page page)> onLoad = null)
        {
            return _pageContainer.Push(resourceKey, playAnimation, onLoad: args =>
            {
                // 현재 화면이 로드되면 다음 화면을 프리로드 시작
                if (!string.IsNullOrEmpty(preloadResourceKey))
                {
                    _pageContainer.Preload(preloadResourceKey);
                    Debug.Log($"[PreloadNav] Preloading: {preloadResourceKey}");
                }

                onLoad?.Invoke(args);
            });
        }

        public AsyncProcessHandle Push(string resourceKey, bool playAnimation = true,
            Action<(string pageId, Page page)> onLoad = null)
        {
            return _pageContainer.Push(resourceKey, playAnimation, onLoad: onLoad);
        }

        public AsyncProcessHandle Pop(bool playAnimation = true)
        {
            return _pageContainer.Pop(playAnimation);
        }

        public void ReleasePreloaded(string resourceKey)
        {
            _pageContainer.ReleasePreloaded(resourceKey);
            Debug.Log($"[PreloadNav] Released preloaded: {resourceKey}");
        }
    }
}
