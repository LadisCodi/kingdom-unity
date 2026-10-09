namespace Codigames.Kingdom.Research
{
    // What the tree holds shut until it is researched. Each answer is the technology that opens it, or null
    // when nothing gates it; Is…Open says whether it is open now.
    public interface IResearchGates
    {
        string DistrictTech(string district);

        // What reaching `level` of a district asks for.
        string LevelTech(string district, int level);

        // What lets one more of a district stand.
        string ExtraCountTech(string district);

        string UnitTech(string unit);

        // What opens a unit's rank (II to V).
        string EvolutionTech(string unit, int rank);

        string HarvestTech(string source);

        string TerrainTech(string terrain);

        bool IsOpen(string tech);
    }
}
