using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codigames.Game.Editor.WebImport
{
    // tech-tree.json: every technology, whole, and each book's bands.
    public sealed class TechTreeDoc
    {
        // Book → revealed cells each band asks for, band 1 first.
        [JsonProperty("eras")] public Dictionary<string, List<double>> Eras { get; set; }

        // Book → relic fragments finishing each band pays (null = nothing).
        [JsonProperty("eraRewards")] public Dictionary<string, List<double?>> EraRewards { get; set; }

        // In file order, which is reading order. Ids starting with '_' are notes, not technologies.
        [JsonProperty("technologies")] public Dictionary<string, TechNodeDoc> Technologies { get; set; }
    }
}
