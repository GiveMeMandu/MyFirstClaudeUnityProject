using System;
using R3;

namespace UIStudy.Advanced.Models
{
    /// <summary>
    /// ReactiveCommand + CanExecute 패턴.
    /// 자원이 충분할 때만 실행 가능한 건설 명령.
    /// </summary>
    public class BuildActionModel : IDisposable
    {
        public ReactiveProperty<int> Gold { get; } = new(100);
        public ReactiveProperty<int> Wood { get; } = new(50);

        public int GoldCost { get; } = 30;
        public int WoodCost { get; } = 20;

        /// <summary>
        /// CanExecute — Gold과 Wood가 모두 비용 이상일 때만 true.
        /// CombineLatest로 두 값을 합쳐 파생 상태를 만든다.
        /// </summary>
        public Observable<bool> CanBuild =>
            Observable.CombineLatest(Gold, Wood, (g, w) => g >= GoldCost && w >= WoodCost);

        /// <summary>
        /// 건설 실행 — CanBuild가 true일 때만 호출해야 함.
        /// </summary>
        public bool TryBuild()
        {
            if (Gold.Value < GoldCost || Wood.Value < WoodCost) return false;
            Gold.Value -= GoldCost;
            Wood.Value -= WoodCost;
            return true;
        }

        public void AddGold(int amount) => Gold.Value += amount;
        public void AddWood(int amount) => Wood.Value += amount;

        public void Dispose()
        {
            Gold.Dispose();
            Wood.Dispose();
        }
    }
}
