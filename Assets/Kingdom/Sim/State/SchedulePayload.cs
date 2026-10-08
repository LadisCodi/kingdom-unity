namespace Kingdom.Sim.State
{
    // What a scheduled window carries.
    public sealed class SchedulePayload
    {
        public string Kind { get; set; }
        public double Occurrence { get; set; }
    }
}
