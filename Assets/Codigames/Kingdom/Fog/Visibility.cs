namespace Codigames.Kingdom.Fog
{
    public enum Visibility
    {
        // Under the cloud bank: not drawn.
        Undiscovered,
        // Seen through the mist: its ground and what stands on it show; it may be paid to clear.
        Discovered,
        // The kingdom's own: buildable, tappable, workable.
        Revealed,
    }
}
