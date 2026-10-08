namespace Codigames.Modules.Audio
{
    // Port: the game's sounds by id.
    public interface ISoundCatalog
    {
        bool TryGet(string soundId, out ISound sound);
    }
}
