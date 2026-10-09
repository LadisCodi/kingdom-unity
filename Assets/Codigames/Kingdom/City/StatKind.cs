namespace Codigames.Kingdom.City
{
    // A number a building is judged on (the web's upgradeStats.ts): append only, a save never stores one but a
    // screen keys its words and icon on it.
    public enum StatKind
    {
        // Villagers a house holds.
        Beds,
        // How far round it its crew reaches, in rings.
        Range,
        // How many may work for it.
        Crew,
        // Units each delivery carries on top of the ground's.
        Haul,
        // How fast its crew swings, ×1 at level 1.
        Speed,
        // What its store holds uncollected.
        Storage,
        // How much more its residents pay, in percent.
        Rent,
        // Gold an hour it makes of its own (the Townhall).
        Income,
        // The Townhall's reach into the fog, in rings.
        Fog,
        // Seconds one of its trainees takes.
        TrainTime,
        // The army a military hall allows.
        ArmyCap,
        // The wounded an Infirmary keeps.
        Wards,
    }
}
