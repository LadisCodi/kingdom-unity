using System.Collections.Generic;

namespace Kingdom.Sim.Data
{
    // A building, with the gates the technologies hand it.
    public sealed class DistrictDef
    {
        public string Id { get; set; }
        public BuildingData Data { get; set; }
        // Null when nothing gates building it.
        public string RequiredTech { get; set; }
        // Index n gates level n + 2.
        public IReadOnlyList<string> RequiredTechPerLevel { get; set; }
        // One more of it may stand once this is done.
        public string ExtraCountTech { get; set; }
    }
}
