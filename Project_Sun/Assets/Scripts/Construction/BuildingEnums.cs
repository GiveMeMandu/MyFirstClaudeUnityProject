namespace ProjectSun.Construction
{
    public enum BuildingCategory
    {
        Resource,
        Defense,
        Research,
        Administration,
        Exploration,
        Wall
    }

    public enum BuildingSlotState
    {
        Hidden,
        Empty,
        Constructing,
        Active,
        Upgrading,
        Damaged,
        Destroyed,
        Repairing
    }
}
