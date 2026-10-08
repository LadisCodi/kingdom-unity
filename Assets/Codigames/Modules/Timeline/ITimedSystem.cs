namespace Codigames.Modules.Timeline
{
    // Something that changes with time, as the timeline sees it: discrete work due at absolute moments, and
    // continuous change between them.
    public interface ITimedSystem
    {
        // The earliest moment strictly after `after` at which this system has discrete work due; null = none.
        double? NextBoundary(double after);

        // Does the discrete work due at `time` (a build finishing, a window closing).
        void ApplyDue(double time);

        // Runs the continuous change up to `time` (production filling a store); the system remembers where
        // it last stopped.
        void RunUntil(double time);
    }
}
