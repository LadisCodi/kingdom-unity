namespace Kingdom.Game.Clock
{
    // The one door to the wall clock. The tick driver asks it and hands `now` to the sim; nothing else in
    // the game reads the time.
    public interface IClock
    {
        // Milliseconds since the Unix epoch, the sim's unit of time.
        long NowMs { get; }
    }
}
