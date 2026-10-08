using Newtonsoft.Json;

namespace Kingdom.Sim.Data
{
    // A unit's rank a technology opens.
    public sealed class EvolutionUnlock
    {
        [JsonProperty("unit")] public string Unit { get; set; }
        [JsonProperty("rank")] public double Rank { get; set; }
    }
}
