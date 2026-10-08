using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codigames.Game.Editor.WebImport
{
    // region-map.json: the province, authored by coordinate.
    public sealed class RegionMapDoc
    {
        [JsonProperty("terrain")] public MapLayer Terrain { get; set; }
        [JsonProperty("features")] public MapLayer Features { get; set; }
        [JsonProperty("landmarks")] public List<LandmarkDoc> Landmarks { get; set; }
        [JsonProperty("lairs")] public Dictionary<string, LairDoc> Lairs { get; set; }
        [JsonProperty("abandoned")] public List<AbandonedDoc> Abandoned { get; set; }
    }
}
