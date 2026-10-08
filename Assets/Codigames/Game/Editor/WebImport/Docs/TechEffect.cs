using Newtonsoft.Json;

namespace Codigames.Game.Editor.WebImport
{
    // A number a bonus technology moves, and what it aims at.
    public sealed class TechEffect
    {
        [JsonProperty("stat")] public string Stat { get; set; }
        [JsonProperty("op")] public string Op { get; set; }
        [JsonProperty("value")] public double Value { get; set; }
        [JsonProperty("target")] public TechTarget Target { get; set; }
    }
}
