namespace Codigames.Kingdom.City
{
    // Why a build, an upgrade or a move did not happen.
    public enum ConstructionRefusal
    {
        None,
        NotBuildable,
        Placement,
        NoFreeBuilder,
        CannotAfford,
        MaxLevel,
        NeedsTownhallLevel,
        AlreadyUnderWay,
        NotFound,
    }
}
