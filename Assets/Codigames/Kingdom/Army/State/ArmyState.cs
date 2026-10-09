using System.Collections.Generic;

namespace Codigames.Kingdom.Army.State
{
    // The city's soldiers by troop, the wounded in the Infirmary's beds, and every hall's line.
    public class ArmyState
    {
        public Dictionary<string, int> Troops { get; set; } = new();
        public Dictionary<string, int> Wounded { get; set; } = new();
        public List<HallItem> Lines { get; set; } = new();
    }

    public enum HallItemKind
    {
        Recruit,
        Heal,
    }

    // One soldier being trained, or a batch of wounded being mended: its hall, and once it is the head of the line,
    // when it started and how long it takes — priced then, never again.
    public class HallItem
    {
        public string Id { get; set; }
        public string Troop { get; set; }
        public string BuildingId { get; set; }
        public HallItemKind Kind { get; set; }
        // A heal's batch; 1 for a recruit.
        public int Count { get; set; } = 1;
        // Epoch milliseconds; null until it heads its line.
        public double? StartedAt { get; set; }
        public double Seconds { get; set; }
        // Milliseconds speed-ups took off it.
        public double CutMs { get; set; }

        public double CompletesAt => (StartedAt ?? 0) + Seconds * 1000 - CutMs;
    }
}
