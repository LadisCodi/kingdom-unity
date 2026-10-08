namespace Kingdom.Sim.State
{
    // An offer's window: when it opened, when it closes, how many were bought.
    public sealed class OfferWindow
    {
        public double Opened { get; set; }
        public double? Closes { get; set; }
        public double Bought { get; set; }
    }
}
