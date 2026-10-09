namespace Codigames.Kingdom.Research
{
    // Where a technology stands. There is no tree fog: every card is on its page from the first minute.
    public enum TechState
    {
        // A requirement is not researched, its band is shut or its book is.
        Locked,

        // It can be worked on: Knowledge poured into it, full or not.
        Progress,

        Done,
    }
}
