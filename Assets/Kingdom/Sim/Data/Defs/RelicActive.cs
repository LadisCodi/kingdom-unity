namespace Kingdom.Sim.Data
{
    // A world relic's spell.
    public sealed class RelicActive
    {
        public string Id { get; set; }
        public double ManaCost { get; set; }
        public bool Targeted { get; set; }
        public double DurationSeconds { get; set; }
        public double DurationPerLevel { get; set; }
        public double Radius { get; set; }
        public double Power { get; set; }
        public double PowerPerLevel { get; set; }
        public double Charges { get; set; }
        public double ChargesPerLevel { get; set; }
    }
}
