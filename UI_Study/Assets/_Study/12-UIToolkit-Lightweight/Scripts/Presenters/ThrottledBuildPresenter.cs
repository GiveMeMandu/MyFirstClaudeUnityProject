using System;
using R3;
using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 8: R3 ThrottleFirst 단독 데모 — 빌드 버튼 연타 방지.
    ///
    /// ThrottleFirst vs ThrottleLast:
    /// - ThrottleFirst: 첫 클릭 즉시 실행, 이후 N ms 무시 → 버튼에 적합
    /// - ThrottleLast(=Debounce): 마지막 이벤트 후 N ms 대기 → 검색에 적합
    ///
    /// 이 Presenter는 SearchPresenter_R3와 별도로 ThrottleFirst 패턴만
    /// 단독 시연하기 위한 참고용 코드입니다.
    /// </summary>
    public class ThrottledBuildPresenter : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private int _buildCount;

        /// <summary>
        /// 외부 C# event를 R3 Observable로 변환하여 ThrottleFirst 적용.
        /// </summary>
        /// <param name="subscribe">이벤트 구독 함수 (h => event += h)</param>
        /// <param name="unsubscribe">이벤트 해제 함수 (h => event -= h)</param>
        /// <param name="throttleMs">스로틀 간격 (밀리초)</param>
        /// <param name="onBuild">빌드 실행 콜백</param>
        public void Setup(
            Action<Action> subscribe,
            Action<Action> unsubscribe,
            int throttleMs,
            Action<int> onBuild)
        {
            Observable.FromEvent(subscribe, unsubscribe)
                .ThrottleFirst(TimeSpan.FromMilliseconds(throttleMs))
                .Subscribe(_ =>
                {
                    _buildCount++;
                    onBuild?.Invoke(_buildCount);
                    Debug.Log($"[ThrottledBuild] Build #{_buildCount} (throttle={throttleMs}ms)");
                })
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
