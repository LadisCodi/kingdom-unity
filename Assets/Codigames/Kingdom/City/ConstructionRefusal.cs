namespace Codigames.Kingdom.City
{
    // Why a build, an upgrade or a move did not happen.
    public enum ConstructionRefusal
    {
        None,
        NotBuildable,
        // The Townhall's level allows no more of it.
        AtCap,
        Placement,
        NoFreeBuilder,
        CannotAfford,
        MaxLevel,
        NeedsTownhallLevel,
        // The city has too few villagers for the next level.
        NeedsPopulation,
        AlreadyUnderWay,
        NotFound,
    }
}
