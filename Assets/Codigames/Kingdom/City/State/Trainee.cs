namespace Codigames.Kingdom.City.State
{
    // A villager in the Townhall's queue. Only the first is being trained; its wait is stamped when its clock
    // starts.
    public class Trainee
    {
        // Epoch milliseconds; null while it waits behind another.
        public double? StartedAt { get; set; }

        public double Seconds { get; set; }

        public double? ArrivesAt => StartedAt + Seconds * 1000;
    }
}
