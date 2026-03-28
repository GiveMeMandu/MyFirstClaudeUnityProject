namespace UIStudy.GameUI.Models
{
    public enum DamageType
    {
        Normal,
        Critical,
        Heal
    }

    /// <summary>
    /// 데미지 넘버 데이터 — 표시할 수치와 타입.
    /// </summary>
    public readonly struct DamageModel
    {
        public readonly int Damage;
        public readonly DamageType Type;

        public DamageModel(int damage, DamageType type)
        {
            Damage = damage;
            Type = type;
        }
    }
}
