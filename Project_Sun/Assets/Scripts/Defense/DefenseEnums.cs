namespace ProjectSun.Defense
{
    public enum EnemyType
    {
        Basic,
        Heavy,
        Flying
    }

    public enum BattleState
    {
        Idle,
        Preparing,
        InProgress,
        Victory,
        Defeat
    }

    public enum EnemyState
    {
        Spawning,
        Moving,
        Attacking,
        Dying
    }
}
