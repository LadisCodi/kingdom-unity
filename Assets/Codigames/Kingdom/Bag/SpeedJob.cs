namespace Codigames.Kingdom.Bag
{
    // A running timer a speed-up can be used on: a builder's job, or the Townhall's training line.
    public readonly struct SpeedJob
    {
        private SpeedJob(SpeedupKind kind, string jobId)
        {
            Kind = kind;
            JobId = jobId;
        }

        // The typed speed-up that fits it.
        public SpeedupKind Kind { get; }

        // A builder's job, or a workshop's district; null for the training line.
        public string JobId { get; }

        public static SpeedJob Construction(string jobId) => new(SpeedupKind.Construction, jobId);

        public static SpeedJob Training() => new(SpeedupKind.Training, null);

        // A military hall's line, or the Infirmary's.
        public static SpeedJob Hall(string districtId) => new(SpeedupKind.Training, districtId);

        // The item at the front of a workshop's queue.
        public static SpeedJob Workshop(string districtId) => new(SpeedupKind.Workshop, districtId);
    }
}
