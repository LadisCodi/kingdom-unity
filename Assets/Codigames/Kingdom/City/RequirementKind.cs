namespace Codigames.Kingdom.City
{
    // What a level may ask for before its price.
    public enum RequirementKind
    {
        TownhallLevel,
        Research,
        Population,
        // The Harmony the city must supply for the level: its total demand there.
        Harmony,
    }
}
