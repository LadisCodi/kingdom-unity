using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kingdom.Sim.Data
{
    // One painted layer of the map: an id per cell.
    public sealed class MapLayer
    {
        [JsonProperty("cells")] public List<MapCell> Cells { get; set; }
    }
}
