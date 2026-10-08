using Codigames.Modules.Audio;
using Codigames.Modules.UI;

namespace Codigames.Game.Audio
{
    // The UI module's sounds, played through the game's one sound channel.
    public class UISoundPlayer : IUISoundPlayer
    {
        private readonly ISoundService _sound;

        public UISoundPlayer(ISoundService sound)
        {
            _sound = sound;
        }

        public void Play(string soundId, float volume) => _sound.Play(soundId, volume);
    }
}
