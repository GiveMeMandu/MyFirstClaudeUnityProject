namespace ProjectSun.Policy
{
    public enum PolicyCategory
    {
        Domestic,
        Exploration,
        Defense
    }

    public enum PolicyNodeState
    {
        Locked,
        Unlocked,
        Enacted,
        BranchLocked
    }

    public enum PolicyEffectType
    {
        None,
        // 자원 수정자
        BasicProductionMod,
        AdvancedProductionMod,
        DefenseProductionMod,
        AllProductionMod,
        BuildCostMod,
        // 인력 수정자
        WorkerEfficiencyMod,
        HealingSpeedMod,
        // 전투 수정자
        TowerDamageMod,
        TowerRangeMod,
        WallHPMod,
        WallRepairCostMod,
        DefenseResourceCostMod,
        // 탐사 수정자
        ExplorationSpeedMod,
        ExplorationRewardMod,
        ExplorationDamageMod,
        // 인카운터 수정자
        EncounterChanceMod,
        // 희망/불만
        HopeInstant,
        DiscontentInstant,
        HopePerTurn,
        DiscontentPerTurn
    }
}
