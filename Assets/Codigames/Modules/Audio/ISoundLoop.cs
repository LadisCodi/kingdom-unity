namespace Codigames.Modules.Audio
{
    // A sound playing on a loop (a song, an ambience bed), until it is stopped.
    public interface ISoundLoop
    {
        void FadeTo(float volume, float seconds);

        // Fades it out and lets it go.
        void Stop(float fadeSeconds);
    }
}
