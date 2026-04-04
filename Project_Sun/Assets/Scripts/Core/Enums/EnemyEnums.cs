namespace ProjectSun.V2.Data
{
    public enum EnemyTier
    {
        Tier1_Basic,
        Tier2_Enhanced,
        Tier3_Elite,
        Tier4_Boss
    }

    public enum EnemyAttackRange
    {
        Melee,
        Ranged
    }

    public enum EnemyMoveSpeed
    {
        VerySlow,
        Slow,
        Medium,
        Fast
    }

    public enum EnemyHealthClass
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum EnemyAttackType
    {
        Single,
        AoE
    }

    public enum EnemyTargetPriority
    {
        Nearest,
        Wall,
        Tower,
        Building
    }

    public enum SpawnPattern
    {
        Batch,
        Sequential,
        Spread
    }
}
