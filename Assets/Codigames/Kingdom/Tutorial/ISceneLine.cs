namespace Codigames.Kingdom.Tutorial
{
    // One line of a scene: who says it, on which side, in what mood; what it points at, what it locks, and what
    // moves it on. A line with no text is the pointer alone.
    public interface ISceneLine
    {
        string Speaker { get; }
        StageSide Side { get; }
        string Text { get; }
        string Expression { get; }

        // Where the box sits: "auto", or a fixed place (low, bottom, high, top, middle).
        string Box { get; }

        // What it points at: "ui:<key>", "district:<id>", "feature:<id>", "quest", "back"…; empty for nothing.
        string Point { get; }

        LineLock Lock { get; }

        // What moves it on: a tap, or a condition becoming true.
        Condition Until { get; }

        // The speaker leaves the stage after it.
        bool Exit { get; }

        // A building this line makes the purse able to pay for, when it cannot; empty for none.
        string Stocks { get; }
    }
}
