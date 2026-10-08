namespace Kingdom.Sim.State
{
    // One simulated purchase.
    public sealed class Purchase
    {
        public string Sku { get; set; }
        public double PriceCents { get; set; }
        // Epoch ms.
        public double At { get; set; }
    }
}
