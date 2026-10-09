namespace Codigames.Kingdom.Army
{
    public enum ArmyRefusal
    {
        None,
        TechRequired,
        NoBuilding,
        HallLevel,
        // A hall trains one rank at a time.
        OtherRank,
        AtCapacity,
        NotEnoughResources,
        NoneWounded,
        NothingTraining,
        NotEnoughGems,
    }
}
