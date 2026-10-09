namespace Codigames.Game.Battles
{
    // How the player likes fights shown: the speed they last watched at, kept for the next fight. A screen preference,
    // never the kingdom's state.
    public sealed class PlaybackPreferences
    {
        public double Speed { get; set; } = 1;
    }
}
