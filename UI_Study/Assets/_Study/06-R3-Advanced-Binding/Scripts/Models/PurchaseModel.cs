using System;
using R3;

namespace UIStudy.R3Advanced.Models
{
    /// <summary>
    /// 구매 모델 — Gold와 ItemPrice를 ReactiveProperty로 관리.
    /// canBuy 파생 상태를 CombineLatest로 제공한다.
    /// </summary>
    public class PurchaseModel : IDisposable
    {
        public ReactiveProperty<int> Gold { get; } = new(100);
        public ReactiveProperty<int> ItemPrice { get; } = new(50);

        /// <summary>
        /// Gold >= ItemPrice 일 때 true.
        /// </summary>
        public Observable<bool> CanBuy =>
            Observable.CombineLatest(Gold, ItemPrice, (g, p) => g >= p);

        /// <summary>
        /// 구매 실행 — Gold을 ItemPrice만큼 차감.
        /// </summary>
        public bool TryBuy()
        {
            if (Gold.Value < ItemPrice.Value) return false;
            Gold.Value -= ItemPrice.Value;
            return true;
        }

        public void AddGold(int amount) => Gold.Value += amount;

        public void Dispose()
        {
            Gold.Dispose();
            ItemPrice.Dispose();
        }
    }
}
