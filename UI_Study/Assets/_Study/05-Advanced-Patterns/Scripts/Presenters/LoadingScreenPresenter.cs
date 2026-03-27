using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIStudy.Advanced.Views;
using UnityEngine;

namespace UIStudy.Advanced.Presenters
{
    /// <summary>
    /// 로딩 화면 Presenter — IProgress&lt;float&gt; 구현.
    /// Addressables + UniTask와 연동하여 프로그레스 바를 업데이트.
    ///
    /// 중요:
    /// - System.Progress&lt;T&gt; 사용 금지 (매 Report()마다 할당)
    /// - Cysharp.Threading.Tasks.Progress.CreateOnlyValueChanged 사용 권장
    /// </summary>
    public class LoadingScreenPresenter : IProgress<float>
    {
        private readonly LoadingScreenView _view;

        public LoadingScreenPresenter(LoadingScreenView view)
        {
            _view = view;
        }

        /// <summary>
        /// IProgress&lt;float&gt;.Report — 매 프레임 호출될 수 있으므로 할당 없는 구현.
        /// </summary>
        public void Report(float value)
        {
            _view.SetProgress(value);
        }

        /// <summary>
        /// 시뮬레이션된 로딩 (Addressables 미설정 시 대체).
        /// </summary>
        public async UniTask SimulateLoadingAsync(CancellationToken ct)
        {
            _view.Show();
            _view.SetStatus("Loading...");

            for (float t = 0; t < 1f; t += Time.deltaTime * 0.5f)
            {
                Report(t);
                await UniTask.Yield(ct);
            }

            Report(1f);
            _view.SetStatus("Complete!");
            await UniTask.Delay(500, cancellationToken: ct);
            _view.Hide();
        }
    }
}
