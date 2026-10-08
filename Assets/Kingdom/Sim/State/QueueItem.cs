namespace Kingdom.Sim.State
{
    // A build or an upgrade waiting for, or using, a builder.
    public sealed class QueueItem
    {
        public string UniqueId { get; set; }
        // build | upgrade.
        public string Kind { get; set; }
        public string DistrictUniqueId { get; set; }
        // Upgrades only.
        public double? TargetLevel { get; set; }
        public double DurationSeconds { get; set; }
        // Epoch ms; null until it enters the active window.
        public double? StartedAt { get; set; }
        public double? CutMs { get; set; }
    }
}
