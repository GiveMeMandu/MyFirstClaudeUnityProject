namespace ProjectSun.V2.Data
{
    public enum AttackDirection
    {
        N, S, E, W,
        NE, NW, SE, SW
    }

    public enum WaveModifierType
    {
        None,
        Fog,
        Storm,
        Enrage,
        StrengthMultiplier
    }

    public enum DefenseResultGrade
    {
        PerfectDefense,
        MinorDamage,
        ModerateDamage,
        MajorDamage
    }
}
