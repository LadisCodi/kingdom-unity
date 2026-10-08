namespace Codigames.Modules.Audio
{
    public interface ISoundService
    {
        void Play(string soundId, float volume = 1f);

        void SetVolume(SoundTrack track, float volume);
    }
}
