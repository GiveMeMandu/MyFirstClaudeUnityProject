namespace ProjectSun.WallExpansion
{
    /// <summary>
    /// 방벽 확장 레벨에 연동하여 해금 가능한 게임 시스템 열거.
    /// 시나리오별로 WallExpansionLevelData.unlockedFeatures에서 선택적으로 설정.
    /// </summary>
    public enum FeatureUnlockType
    {
        None,
        Exploration,
        Research,
        Policy,
        Trade,
        AdvancedDefense,
        Administration
    }
}
