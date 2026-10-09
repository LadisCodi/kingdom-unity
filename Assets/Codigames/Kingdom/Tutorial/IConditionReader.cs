namespace Codigames.Kingdom.Tutorial
{
    // One source of answers: the kingdom's facts, or what is on the screen. False from TryHolds means "not mine".
    public interface IConditionReader
    {
        bool TryHolds(Condition condition, int tapsAtStart, out bool holds);
    }
}
