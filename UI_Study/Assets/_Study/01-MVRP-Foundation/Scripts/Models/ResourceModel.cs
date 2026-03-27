using System;
using R3;

namespace UIStudy.MVRP.Models
{
    /// <summary>
    /// 자원 관리 Model — 3개 자원을 ReactiveProperty로 관리.
    /// UI를 전혀 모르는 순수 도메인 로직.
    /// </summary>
    public class ResourceModel : IDisposable
    {
        public ReactiveProperty<int> Gold { get; } = new(100);
        public ReactiveProperty<int> Wood { get; } = new(50);
        public ReactiveProperty<int> Population { get; } = new(10);

        public bool CanSpend(int gold = 0, int wood = 0)
        {
            return Gold.Value >= gold && Wood.Value >= wood;
        }

        public bool TrySpendGold(int amount)
        {
            if (Gold.Value < amount) return false;
            Gold.Value -= amount;
            return true;
        }

        public bool TrySpendWood(int amount)
        {
            if (Wood.Value < amount) return false;
            Wood.Value -= amount;
            return true;
        }

        public void AddGold(int amount) => Gold.Value += amount;
        public void AddWood(int amount) => Wood.Value += amount;
        public void AddPopulation(int amount) => Population.Value += amount;

        public void Dispose()
        {
            Gold.Dispose();
            Wood.Dispose();
            Population.Dispose();
        }
    }
}
