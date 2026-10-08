using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codigames.Kingdom.Data
{
    // The garrison in a lair and its clock.
    public sealed class GuardDef
    {
        // A unit type, or "Any".
        [JsonProperty("threat")] public string Threat { get; set; }
        [JsonProperty("power")] public double Power { get; set; }
        // Minutes from discovery to the first raid.
        [JsonProperty("warningMinutes")] public double WarningMinutes { get; set; }
        // What it fields, as weights by unit type; null = the threat takes the lion's share.
        [JsonProperty("mix")] public Dictionary<string, double> Mix { get; set; }
    }
}
