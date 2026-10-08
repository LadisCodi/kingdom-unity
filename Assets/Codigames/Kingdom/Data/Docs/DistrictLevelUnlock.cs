using Newtonsoft.Json;

namespace Codigames.Kingdom.Data
{
    // A building's level a technology opens.
    public sealed class DistrictLevelUnlock
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("level")] public double Level { get; set; }
    }
}
