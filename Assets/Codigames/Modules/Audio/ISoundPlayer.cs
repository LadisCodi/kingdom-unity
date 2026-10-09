namespace Codigames.Modules.Audio
{
    // Port: what actually makes noise.
    public interface ISoundPlayer
    {
        void Play(ISound sound, float volume);

        // Starts a sound on a loop, fading in from silence to `volume`.
        ISoundLoop StartLoop(ISound sound, float volume, float fadeSeconds);

        void SetVolume(SoundTrack track, float volume);
    }
}
