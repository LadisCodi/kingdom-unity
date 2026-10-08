using Newtonsoft.Json;

namespace Codigames.Kingdom.Data
{
    // What a technology opens: exactly one of these is set.
    public sealed class TechUnlock
    {
        [JsonProperty("district")] public string District { get; set; }
        [JsonProperty("districtLevel")] public DistrictLevelUnlock DistrictLevel { get; set; }
        [JsonProperty("districtCount")] public string DistrictCount { get; set; }
        [JsonProperty("unit")] public string Unit { get; set; }
        [JsonProperty("evolution")] public EvolutionUnlock Evolution { get; set; }
        [JsonProperty("harvest")] public string Harvest { get; set; }
        [JsonProperty("terrain")] public string Terrain { get; set; }
        [JsonProperty("worldUpgrade")] public string WorldUpgrade { get; set; }
    }
}
