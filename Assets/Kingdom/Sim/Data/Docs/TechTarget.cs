using Newtonsoft.Json;

namespace Kingdom.Sim.Data
{
    // What a technology's effect aims at: at most one of these is set; none = everything.
    public sealed class TechTarget
    {
        [JsonProperty("district")] public string District { get; set; }
        [JsonProperty("unit")] public string Unit { get; set; }
        [JsonProperty("unitTag")] public string UnitTag { get; set; }
        [JsonProperty("harvest")] public string Harvest { get; set; }
        [JsonProperty("tome")] public string Tome { get; set; }
        [JsonProperty("worldDistrict")] public string WorldDistrict { get; set; }
    }
}
