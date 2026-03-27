using System;
using R3;

namespace UIStudy.Advanced.Models
{
    /// <summary>
    /// 범용 스탯 모델 — HP, 경험치, 진행률 등에 사용.
    /// </summary>
    public class StatModel : IDisposable
    {
        public ReactiveProperty<int> CurrentValue { get; }
        public ReactiveProperty<int> MaxValue { get; }

        public StatModel(int maxValue, int currentValue = -1)
        {
            MaxValue = new ReactiveProperty<int>(maxValue);
            CurrentValue = new ReactiveProperty<int>(currentValue < 0 ? maxValue : currentValue);
        }

        public void TakeDamage(int amount)
        {
            CurrentValue.Value = Math.Max(0, CurrentValue.Value - amount);
        }

        public void Heal(int amount)
        {
            CurrentValue.Value = Math.Min(MaxValue.Value, CurrentValue.Value + amount);
        }

        public void Dispose()
        {
            CurrentValue.Dispose();
            MaxValue.Dispose();
        }
    }
}
