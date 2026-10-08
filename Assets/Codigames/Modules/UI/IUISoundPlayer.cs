namespace Codigames.Modules.UI
{
    // Port: how the UI plays a sound (a button press). Implemented outside the module.
    public interface IUISoundPlayer
    {
        void Play(string soundId, float volume);
    }
}
