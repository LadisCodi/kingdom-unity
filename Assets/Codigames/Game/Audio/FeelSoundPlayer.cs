using Codigames.Modules.Audio;
using MoreMountains.Tools;

namespace Codigames.Game.Audio
{
    // Plays the game's sounds through Feel's MMSoundManager, the one sound channel: feedbacks play through it
    // too, so a track's volume reaches every sound.
    public class FeelSoundPlayer : ISoundPlayer
    {
        private readonly MMSoundManager _manager;

        public FeelSoundPlayer(MMSoundManager manager)
        {
            _manager = manager;
        }

        public void Play(ISound sound, float volume)
        {
            if (sound is not UnitySound unitySound || unitySound.Clip == null) return;

            var options = MMSoundManagerPlayOptions.Default;
            options.MmSoundManagerTrack = ToManagerTrack(sound.Track);
            options.Volume = volume;
            _manager.PlaySound(unitySound.Clip, options);
        }

        public void SetVolume(SoundTrack track, float volume) => _manager.SetTrackVolume(ToManagerTrack(track), volume);

        private static MMSoundManager.MMSoundManagerTracks ToManagerTrack(SoundTrack track) => track switch
        {
            SoundTrack.Ui => MMSoundManager.MMSoundManagerTracks.UI,
            SoundTrack.Music => MMSoundManager.MMSoundManagerTracks.Music,
            SoundTrack.Ambience => MMSoundManager.MMSoundManagerTracks.Other,
            _ => MMSoundManager.MMSoundManagerTracks.Sfx,
        };
    }
}
