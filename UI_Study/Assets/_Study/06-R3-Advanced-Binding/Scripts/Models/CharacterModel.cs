using System;
using R3;

namespace UIStudy.R3Advanced.Models
{
    /// <summary>
    /// 캐릭터 모델 — 이름, 체력, 공격력을 ReactiveProperty로 관리.
    /// Two-Way Binding 데모용.
    /// </summary>
    public class CharacterModel : IDisposable
    {
        public ReactiveProperty<string> Name { get; } = new("Hero");
        public ReactiveProperty<float> Health { get; } = new(100f);
        public ReactiveProperty<float> Attack { get; } = new(25f);

        public void Dispose()
        {
            Name.Dispose();
            Health.Dispose();
            Attack.Dispose();
        }
    }
}
