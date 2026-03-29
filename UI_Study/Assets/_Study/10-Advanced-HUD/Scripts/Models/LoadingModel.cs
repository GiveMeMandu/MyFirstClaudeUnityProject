using R3;

namespace UIStudy.AdvancedHUD.Models
{
    /// <summary>
    /// Loading 모델 — 진행률, 현재 태스크, 로딩 상태.
    /// </summary>
    public class LoadingModel
    {
        /// <summary>
        /// 로딩 진행률 (0 ~ 1).
        /// </summary>
        public ReactiveProperty<float> Progress { get; } = new(0f);

        /// <summary>
        /// 현재 실행 중인 태스크 이름.
        /// </summary>
        public ReactiveProperty<string> CurrentTask { get; } = new(string.Empty);

        /// <summary>
        /// 로딩 중 여부.
        /// </summary>
        public ReactiveProperty<bool> IsLoading { get; } = new(false);
    }
}
