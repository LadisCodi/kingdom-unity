using Kingdom.Sim.Core;

namespace Kingdom.Sim.State
{
    // A villager at work for a building.
    public sealed class Worker
    {
        public string Id { get; set; }
        public string BuildingId { get; set; }
        // Idle | MovingToCell | Working | MovingHome.
        public string Activity { get; set; }
        public Coord ClaimedCell { get; set; }
        public double Carrying { get; set; }
        public string CarriedSource { get; set; }
        public double StrikeCarry { get; set; }
        public double StateStartedAt { get; set; }
        // Null while idle.
        public double? StateUntil { get; set; }
    }
}
