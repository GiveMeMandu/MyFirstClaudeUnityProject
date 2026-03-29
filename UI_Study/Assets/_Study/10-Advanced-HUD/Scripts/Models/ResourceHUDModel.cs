using R3;

namespace UIStudy.AdvancedHUD.Models
{
    /// <summary>
    /// Resource HUD 모델 — 5개 자원(Gold, Wood, Stone, Food, Population).
    /// </summary>
    public class ResourceHUDModel
    {
        public const int MaxPopulation = 50;

        public ReactiveProperty<int> Gold { get; } = new(500);
        public ReactiveProperty<int> Wood { get; } = new(200);
        public ReactiveProperty<int> Stone { get; } = new(150);
        public ReactiveProperty<int> Food { get; } = new(300);
        public ReactiveProperty<int> Population { get; } = new(10);
    }
}
