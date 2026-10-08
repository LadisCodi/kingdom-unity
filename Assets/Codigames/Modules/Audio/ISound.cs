namespace Codigames.Modules.Audio
{
    // A sound the game can play. What it is made of (a clip, an event in a sound engine) is the player's
    // business; the module only knows which track it belongs to.
    public interface ISound
    {
        SoundTrack Track { get; }
    }
}
