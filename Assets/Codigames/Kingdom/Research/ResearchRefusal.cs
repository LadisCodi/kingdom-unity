namespace Codigames.Kingdom.Research
{
    // Why a technology cannot be worked on at all — neither poured into nor researched.
    public enum ResearchRefusal
    {
        None,
        AlreadyDone,
        TomeClosed,
        MissingRequirement,
        EraLocked,
    }
}
