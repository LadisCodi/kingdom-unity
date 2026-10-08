namespace Codigames.Modules.Audio
{
    // Plays the game's sounds by id: the catalog says which sound, the player plays it.
    public class SoundService : ISoundService
    {
        private readonly ISoundCatalog _catalog;
        private readonly ISoundPlayer _player;

        public SoundService(ISoundCatalog catalog, ISoundPlayer player)
        {
            _catalog = catalog;
            _player = player;
        }

        public bool Play(string soundId, float volume = 1f)
        {
            if (string.IsNullOrEmpty(soundId) || !_catalog.TryGet(soundId, out var sound)) return false;

            _player.Play(sound, volume);
            return true;
        }

        public void SetVolume(SoundTrack track, float volume) => _player.SetVolume(track, volume);
    }
}
