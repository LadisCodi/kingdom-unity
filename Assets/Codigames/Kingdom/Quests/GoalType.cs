namespace Codigames.Kingdom.Quests
{
    // What a quest asks. Absolute goals are read off the kingdom as it stands (work done before the quest counts);
    // relative ones count what happens from the moment the quest is active.
    public enum GoalType
    {
        BuildDistrict,
        RepairDistrict,
        UpgradeDistrict,
        HoldResource,
        ReachPopulation,
        CompleteTech,
        CompleteTechs,
        AssignWorkers,
        WorkInReach,
        TrainArmy,
        ClaimLandmarks,
        FindLairs,
        ClearLairs,
        OwnArtifacts,
        OwnHeroes,
        DiscoverCells,

        // Relative.
        CollectResource,
        CollectTaps,
        DiscoverFeature,
    }

    public static class GoalTypes
    {
        public static bool IsRelative(this GoalType type)
            => type is GoalType.CollectResource or GoalType.CollectTaps or GoalType.DiscoverFeature;
    }
}
