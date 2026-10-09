namespace Codigames.Kingdom.City
{
    // Why a building may not stand on a spot. One check serves building and moving.
    public enum PlacementProblem
    {
        None,
        OutsideProvince,
        Occupied,
        NeedsLand,
        // Not the kingdom's ground yet: the fog still covers it.
        InFog,
        CountLimit,
        // A lair holds the ground round it while it stands.
        LairZone,
    }
}
