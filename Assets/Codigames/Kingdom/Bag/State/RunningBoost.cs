using Codigames.Kingdom.Economy;

namespace Codigames.Kingdom.Bag.State
{
    // A boost running: what it raises, by how much (1.25 = a quarter more), until when (epoch milliseconds).
    public class RunningBoost
    {
        public BoostKind Kind { get; set; }
        public double Multiplier { get; set; }
        public double EndsAt { get; set; }
    }
}
