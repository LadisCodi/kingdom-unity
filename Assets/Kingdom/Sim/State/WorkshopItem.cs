namespace Kingdom.Sim.State
{
    // One good being made.
    public sealed class WorkshopItem
    {
        public string Good { get; set; }
        public double WorkMs { get; set; }
        // Priced when it starts.
        public double? NeedMs { get; set; }
    }
}
