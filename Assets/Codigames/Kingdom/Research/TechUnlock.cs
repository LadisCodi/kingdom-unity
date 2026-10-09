namespace Codigames.Kingdom.Research
{
    // One thing a technology opens: a building, a building's level, one more of a building, a unit or a unit's
    // rank, a harvest source, a terrain or a world upgrade.
    public readonly struct TechUnlock
    {
        public TechUnlock(UnlockKind kind, string id, int level = 0)
        {
            Kind = kind;
            Id = id;
            Level = level;
        }

        public UnlockKind Kind { get; }

        // The building, unit, source, terrain or upgrade it names.
        public string Id { get; }

        // The level a DistrictLevel opens, or the rank an Evolution opens; 0 otherwise.
        public int Level { get; }
    }
}
