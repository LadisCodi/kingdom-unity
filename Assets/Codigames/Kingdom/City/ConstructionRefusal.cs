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
        // A technology opens it and is not researched yet.
        NeedsResearch,
        AlreadyUnderWay,
        // The build or level asks more Harmony than the city's decorations supply.
        NeedsHarmony,
        // The level or the build asks refined goods the stockpile does not hold.
        NotEnoughGoods,
        NotFound,
        // Its ruin stands in the province: repair that one first.
        RuinFirst,
    }
}
