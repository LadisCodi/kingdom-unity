namespace Kingdom.Sim.State
{
    // What a product hands over the day after it is bought.
    public sealed class NextDayDelivery
    {
        public string Sku { get; set; }
        public double ClaimableAt { get; set; }
    }
}
