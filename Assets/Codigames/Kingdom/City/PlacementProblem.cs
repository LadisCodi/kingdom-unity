namespace Codigames.Kingdom.City
{
    // Why a building may not stand on a spot. One check serves building and moving.
    public enum PlacementProblem
    {
        None,
        OutsideProvince,
        Occupied,
        NeedsLand,
        CountLimit,
    }
}
