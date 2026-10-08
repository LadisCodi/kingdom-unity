namespace Kingdom.Sim.State
{
    // A wounded hero.
    public sealed class HeroHurt
    {
        public double Missing { get; set; }
        public double At { get; set; }
        public bool? Exhausted { get; set; }
    }
}
