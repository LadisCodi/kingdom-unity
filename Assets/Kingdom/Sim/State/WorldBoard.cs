namespace Kingdom.Sim.State
{
    // Which board, and which seat on it.
    public sealed class WorldBoard
    {
        public string Id { get; set; }
        public double Seed { get; set; }
        public double Seat { get; set; }
    }
}
