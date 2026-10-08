using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kingdom.Sim.Data
{
    // One technology as tech-tree.json holds it.
    public sealed class TechNodeDoc
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("icon")] public string Icon { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("kind")] public string Kind { get; set; }
        [JsonProperty("tome")] public string Tome { get; set; }
        [JsonProperty("era")] public double? Era { get; set; }
        [JsonProperty("row")] public double? Row { get; set; }
        [JsonProperty("col")] public double? Col { get; set; }
        [JsonProperty("requires")] public List<string> Requires { get; set; }
        [JsonProperty("gold")] public double Gold { get; set; }
        [JsonProperty("knowledge")] public double? Knowledge { get; set; }
        [JsonProperty("materials")] public Dictionary<string, double> Materials { get; set; }
        [JsonProperty("goods")] public Dictionary<string, double> Goods { get; set; }
        [JsonProperty("anyPrecious")] public double? AnyPrecious { get; set; }
        [JsonProperty("unlocks")] public List<TechUnlock> Unlocks { get; set; }
        [JsonProperty("effects")] public List<TechEffect> Effects { get; set; }
        [JsonProperty("planned")] public bool? Planned { get; set; }

        // On a page at all: an unplaced technology is not drawn, cannot be researched and gates nothing.
        [JsonIgnore] public bool IsPlaced => Tome != null && Era.HasValue && Row.HasValue && Col.HasValue;
    }
}
