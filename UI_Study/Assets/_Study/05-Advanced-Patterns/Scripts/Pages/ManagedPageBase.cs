using System;
using System.Collections;
using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Page;

namespace UIStudy.Advanced.Pages
{
    /// <summary>
    /// Page + Presenter 통합 베이스 클래스.
    /// AddLifecycleEvent를 사용하여 외부에서 수명주기 콜백을 주입.
    /// VContainer의 Presenter가 이 메커니즘으로 Page에 연결됨.
    ///
    /// 사용법:
    /// page.AddLifecycleEvent(
    ///     onDidPushEnter: () => presenter.OnPageEntered(),
    ///     onWillPopExit: () => presenter.OnPageExiting()
    /// );
    /// </summary>
    public class ManagedPageBase : Page
    {
        /// <summary>
        /// Presenter를 이 Page의 수명주기에 연결.
        /// Presenter는 IPagePresenter를 구현해야 함.
        /// </summary>
        public void AttachPresenter(IPagePresenter presenter)
        {
            AddLifecycleEvent(new PagePresenterLifecycleAdapter(presenter));
        }

        public override IEnumerator Initialize()
        {
            yield break;
        }
    }

    /// <summary>
    /// Page Presenter 인터페이스 — Page 수명주기에 반응하는 메서드.
    /// </summary>
    public interface IPagePresenter : IDisposable
    {
        void OnPageEntered();
        void OnPageExiting();
    }

    /// <summary>
    /// IPageLifecycleEvent 어댑터 — IPagePresenter를 USN 수명주기에 연결.
    /// </summary>
    internal class PagePresenterLifecycleAdapter : IPageLifecycleEvent
    {
        private readonly IPagePresenter _presenter;

        public PagePresenterLifecycleAdapter(IPagePresenter presenter)
        {
            _presenter = presenter;
        }

        public IEnumerator Initialize() { yield break; }
        public IEnumerator WillPushEnter() { yield break; }
        public void DidPushEnter() => _presenter.OnPageEntered();
        public IEnumerator WillPushExit() { yield break; }
        public void DidPushExit() { }
        public IEnumerator WillPopEnter() { yield break; }
        public void DidPopEnter() => _presenter.OnPageEntered();
        public IEnumerator WillPopExit()
        {
            _presenter.OnPageExiting();
            yield break;
        }
        public void DidPopExit() => _presenter.Dispose();
        public IEnumerator Cleanup() { yield break; }
    }
}
