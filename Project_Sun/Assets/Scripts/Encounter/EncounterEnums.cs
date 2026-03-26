namespace ProjectSun.Encounter
{
    public enum EncounterCategory
    {
        Daily,      // 일상 인카운터 (이진 선택)
        Major       // 중요 인카운터 (3선택지)
    }

    public enum EffectType
    {
        ResourceChange,     // 즉발 자원 변동
        WorkerChange,       // 즉발 인력 변동
        WorkerInjury,       // 인력 부상
        Buff,               // N턴 버프
        TriggerBattle       // 전투 트리거
    }

    public enum BuffType
    {
        ProductionBonus,        // 생산력 증가 (%)
        AttackBonus,            // 공격력 증가 (%)
        DefenseResourceHalf     // 방어 자원 절반 소모
    }
}
