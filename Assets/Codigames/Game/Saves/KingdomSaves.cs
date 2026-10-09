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
        public const int VERSION = 10;

        private static readonly ISaveMigration<JObject>[] MIGRATIONS =
        {
            // 1 → 2: the ground worked by hand, the Mana pool's clock and the kingdom's seed.
            new AdditiveMigration<JObject>(1),
            // 2 → 3: every building's store, and the city's population.
            new AdditiveMigration<JObject>(2),
            // 3 → 4: villagers in training.
            new AdditiveMigration<JObject>(3),
            // 4 → 5: the fog (an older kingdom starts with its buildings' rings).
            new AdditiveMigration<JObject>(4),
            // 5 → 6: the crews.
            new AdditiveMigration<JObject>(5),
            // 6 → 7: research and the Knowledge bar.
            new AdditiveMigration<JObject>(6),
            // 7 → 8: the abandoned buildings repaired.
            new AdditiveMigration<JObject>(7),
            // 8 → 9: the fog's treasures.
            new AdditiveMigration<JObject>(8),
            // 9 → 10: the landmarks claimed.
            new AdditiveMigration<JObject>(9),
        };

        public static SaveSlot<KingdomState, JObject> Slot(ISaveStorage storage)
            => new(storage, new KingdomSaveCodec(), SLOT, VERSION, MIGRATIONS);
    }
}
