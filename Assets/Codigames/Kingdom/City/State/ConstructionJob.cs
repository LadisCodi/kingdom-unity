namespace Codigames.Kingdom.City.State
{
    // A builder at work: a build (target level 1) or an upgrade. Its wait is priced when it starts.
    public class ConstructionJob
    {
        public string Id { get; set; }
        public string DistrictId { get; set; }
        public int TargetLevel { get; set; }

        // Epoch milliseconds.
        public double StartedAt { get; set; }
        public double Seconds { get; set; }

        public double CompletesAt => StartedAt + Seconds * 1000;
    }
}
