namespace Codigames.Kingdom.City
{
    // One order of villagers: how many, and what they cost together.
    public readonly struct TrainPlan
    {
        public TrainPlan(int count, double cost)
        {
            Count = count;
            Cost = cost;
        }

        public int Count { get; }
        public double Cost { get; }
    }
}
