using Newtonsoft.Json;

namespace Codigames.Kingdom.Data
{
    public sealed class LandmarkDoc
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("kind")] public string Kind { get; set; }
        [JsonProperty("x")] public int X { get; set; }
        [JsonProperty("y")] public int Y { get; set; }
        [JsonProperty("claimCost")] public double ClaimCost { get; set; }
        [JsonProperty("size")] public int? Size { get; set; }
    }
}
