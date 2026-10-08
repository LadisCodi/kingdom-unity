using Newtonsoft.Json;

namespace Codigames.Kingdom.Data
{
    public sealed class AbandonedDoc
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("district")] public string District { get; set; }
        [JsonProperty("x")] public int X { get; set; }
        [JsonProperty("y")] public int Y { get; set; }
        [JsonProperty("sight")] public double Sight { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
    }
}
