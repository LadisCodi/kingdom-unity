using Newtonsoft.Json;

namespace Codigames.Game.Editor.WebImport
{
    public sealed class LairDoc
    {
        [JsonProperty("x")] public int X { get; set; }
        [JsonProperty("y")] public int Y { get; set; }
        [JsonProperty("size")] public int? Size { get; set; }
        [JsonProperty("tier")] public double Tier { get; set; }
        [JsonProperty("radius")] public double Radius { get; set; }
        [JsonProperty("sight")] public double Sight { get; set; }
        [JsonProperty("flavour")] public string Flavour { get; set; }
        [JsonProperty("guard")] public GuardDef Guard { get; set; }
    }
}
