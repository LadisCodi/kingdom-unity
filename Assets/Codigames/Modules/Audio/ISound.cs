using Codigames.Modules.Core;

namespace Codigames.Modules.Audio
{
    // A sound the game can play, by id. What it is made of (a clip, an event in a sound engine) is the
    // player's business; the module only knows which track it belongs to.
    public interface ISound : IIdentifiable
    {
        SoundTrack Track { get; }
    }
}
