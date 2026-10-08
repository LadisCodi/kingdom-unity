using Newtonsoft.Json;

namespace Kingdom.Sim.Data
{
    public sealed class MapCell
    {
        [JsonProperty("x")] public int X { get; set; }
        [JsonProperty("y")] public int Y { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
    }
}
