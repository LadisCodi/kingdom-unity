namespace Kingdom.Sim.State
{
    // A recruit or a heal on a building's training line.
    public sealed class TrainingItem
    {
        public string UniqueId { get; set; }
        public string Trainee { get; set; }
        public string BuildingId { get; set; }
        // recruit | heal; absent = recruit.
        public string Kind { get; set; }
        public double? Count { get; set; }
        public double? StartedAt { get; set; }
        // Priced when it starts.
        public double? Seconds { get; set; }
        public double? CutMs { get; set; }
    }
}
