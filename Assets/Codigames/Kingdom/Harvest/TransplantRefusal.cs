namespace Codigames.Kingdom.Harvest
{
    public enum TransplantRefusal
    {
        None,
        // Nothing that moves stands there: only a tree or a crop plot does.
        NotMovable,
        NotRevealed,
        // A tree moves once Transplanting is researched.
        NeedsResearch,
        // The cell it would land on will not take it.
        Placement,
    }
}
