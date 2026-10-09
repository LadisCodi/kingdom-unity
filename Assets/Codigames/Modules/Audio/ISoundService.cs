namespace Codigames.Modules.Audio
{
    public interface ISoundService
    {
        // False when the catalog has no sound with that id.
        bool Play(string soundId, float volume = 1f);

        void SetVolume(SoundTrack track, float volume);

        // Null when the catalog has no sound with that id.
        ISoundLoop StartLoop(string soundId, float volume, float fadeSeconds);
    }
}
