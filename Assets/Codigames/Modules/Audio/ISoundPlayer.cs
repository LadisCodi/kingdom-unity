namespace Codigames.Modules.Audio
{
    // Port: what actually makes noise.
    public interface ISoundPlayer
    {
        void Play(ISound sound, float volume);

        void SetVolume(SoundTrack track, float volume);
    }
}
