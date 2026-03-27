using System;
using R3;

namespace UIStudy.MVRP.Models
{
    /// <summary>
    /// 카운터 Model — ReactiveProperty로 상태를 보유.
    /// UI를 전혀 모르는 순수 데이터 레이어.
    /// </summary>
    public class CounterModel : IDisposable
    {
        public ReactiveProperty<int> Count { get; } = new(0);

        public void Increment() => Count.Value++;
        public void Decrement() => Count.Value = Math.Max(0, Count.Value - 1);
        public void Reset() => Count.Value = 0;

        public void Dispose() => Count.Dispose();
    }
}
