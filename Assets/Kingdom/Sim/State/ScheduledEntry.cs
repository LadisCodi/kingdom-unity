namespace Kingdom.Sim.State
{
    // A window on the timeline.
    public sealed class ScheduledEntry
    {
        public string Id { get; set; }
        public string TemplateId { get; set; }
        public double StartsAt { get; set; }
        public double? EndsAt { get; set; }
        public SchedulePayload Payload { get; set; }
        // pending | active | done.
        public string Phase { get; set; }
    }
}
