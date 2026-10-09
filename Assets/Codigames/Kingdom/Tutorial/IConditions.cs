namespace Codigames.Kingdom.Tutorial
{
    // Is a condition true right now? `tapsAtStart` is the tap count when the line began: `taps` is relative.
    public interface IConditions
    {
        bool Holds(Condition condition, int tapsAtStart = 0);
    }
}
