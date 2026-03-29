using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIStudy.AdvancedHUD.Models;

namespace UIStudy.AdvancedHUD.Services
{
    /// <summary>
    /// Fake Loading Service — UniTask로 비동기 로딩 시뮬레이션.
    /// 5단계: Assets(0-20%) -> Systems(20-50%) -> Map(50-70%) -> Entities(70-90%) -> Finalize(90-100%)
    /// </summary>
    public class FakeLoadingService
    {
        private readonly LoadingModel _model;

        private static readonly LoadingStep[] Steps =
        {
            new("Loading Assets...", 0f, 0.2f, 800),
            new("Initializing Systems...", 0.2f, 0.5f, 1200),
            new("Loading Map...", 0.5f, 0.7f, 1000),
            new("Spawning Entities...", 0.7f, 0.9f, 1500),
            new("Finalizing...", 0.9f, 1.0f, 500)
        };

        public FakeLoadingService(LoadingModel model)
        {
            _model = model;
        }

        /// <summary>
        /// 비동기 로딩 시뮬레이션 실행.
        /// </summary>
        public async UniTask RunLoadingAsync(CancellationToken ct = default)
        {
            _model.IsLoading.Value = true;
            _model.Progress.Value = 0f;
            _model.CurrentTask.Value = string.Empty;

            foreach (var step in Steps)
            {
                ct.ThrowIfCancellationRequested();

                _model.CurrentTask.Value = step.TaskName;

                // 부드러운 진행률 보간
                float elapsed = 0f;
                float duration = step.DurationMs / 1000f;

                while (elapsed < duration)
                {
                    ct.ThrowIfCancellationRequested();

                    elapsed += 0.016f; // ~60fps 시뮬레이션
                    float t = Math.Min(elapsed / duration, 1f);
                    // EaseOutQuad 보간
                    float eased = 1f - (1f - t) * (1f - t);
                    _model.Progress.Value = step.StartProgress + (step.EndProgress - step.StartProgress) * eased;

                    await UniTask.Delay(16, cancellationToken: ct);
                }

                _model.Progress.Value = step.EndProgress;
            }

            _model.CurrentTask.Value = "Complete!";
            _model.IsLoading.Value = false;
        }

        private readonly struct LoadingStep
        {
            public readonly string TaskName;
            public readonly float StartProgress;
            public readonly float EndProgress;
            public readonly int DurationMs;

            public LoadingStep(string taskName, float startProgress, float endProgress, int durationMs)
            {
                TaskName = taskName;
                StartProgress = startProgress;
                EndProgress = endProgress;
                DurationMs = durationMs;
            }
        }
    }
}
