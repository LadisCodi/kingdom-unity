using Codigames.Modules.Core;

namespace Codigames.Kingdom.Crews.State
{
    // A villager working for a building: where it is in its round, the cell it holds, and what it carries.
    public class WorkerState
    {
        public string Id { get; set; }
        public string BuildingId { get; set; }
        public WorkerActivity Activity { get; set; }

        // The cell it works; no other worker may claim it.
        public Vector2Int? ClaimedCell { get; set; }

        public double Carrying { get; set; }
        public string CarriedCurrency { get; set; }

        // The fraction of a unit its strikes owe, paid with a later one.
        public double StrikeCarry { get; set; }

        // Epoch milliseconds: when its current state started and when it ends (null while idle).
        public double StateStartedAt { get; set; }
        public double? StateUntil { get; set; }
    }
}
