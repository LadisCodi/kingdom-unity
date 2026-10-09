using Codigames.Kingdom;
using Codigames.Modules.Saves;
using Newtonsoft.Json.Linq;

namespace Codigames.Game.Saves
{
    // The kingdom's save: its slot, the version this build writes, and every migration from version 1 up — one
    // per version that renamed, reshaped or changed the meaning of something, appended, never edited. An
    // additive change (a new field with a default) needs only the bump.
    public static class KingdomSaves
    {
        public const string SLOT = "kingdom";
        public const int VERSION = 1;

        private static readonly ISaveMigration<JObject>[] MIGRATIONS = { };

        public static SaveSlot<KingdomState, JObject> Slot(ISaveStorage storage)
            => new(storage, new KingdomSaveCodec(), SLOT, VERSION, MIGRATIONS);
    }
}
