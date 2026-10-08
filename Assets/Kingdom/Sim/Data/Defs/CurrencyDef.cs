namespace Kingdom.Sim.Data
{
    // A currency: who holds it (city, kingdom or player), its cap and its start.
    public sealed class CurrencyDef
    {
        public string Id { get; set; }
        // city | kingdom | player.
        public string Scope { get; set; }
        public double? Cap { get; set; }
        public double Start { get; set; }
        public bool Primary { get; set; }
        public double? GoldValue { get; set; }
    }
}
