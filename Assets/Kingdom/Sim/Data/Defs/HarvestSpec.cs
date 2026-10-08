namespace Kingdom.Sim.Data
{
    // A kind of cell a tap or a crew draws from: what it pays, and the technology it waits on.
    public sealed class HarvestSpec
    {
        public string Id { get; set; }
        public string CurrencyId { get; set; }
        public HarvestSourceData Data { get; set; }
        public string RequiredTech { get; set; }
    }
}
