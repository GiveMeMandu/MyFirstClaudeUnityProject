namespace ProjectSun.Turn
{
    public enum TurnPhase
    {
        DayPhase,
        DayEnd,
        FadeToNight,
        NightPhase,
        NightEnd,
        FadeToDay,
        DayStart,
        GameOver
    }

    public enum EncounterType
    {
        None,
        FixedBattle,
        RandomEncounter
    }

    public enum RandomEncounterResult
    {
        Nothing,
        Battle,
        Event
    }

    public enum GameOverReason
    {
        Victory,
        Defeat
    }
}
