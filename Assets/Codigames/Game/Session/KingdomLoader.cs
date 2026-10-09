using Codigames.Kingdom;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Map;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using Codigames.Modules.Saves;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Codigames.Game.Session
{
    // The kingdom the game starts with: the one saved on this device, else a new one.
    public static class KingdomLoader
    {
        public static KingdomState Load(SaveSlot<KingdomState, JObject> slot, IProvinceMap map, IConstructionSettings settings,
            ICatalog<ICurrencyDefinition> currencies, IClock clock)
        {
            var load = slot.Load();

            switch (load.Status)
            {
                case SaveLoadStatus.Loaded:
                    Debug.Log($"#Save# Loaded the kingdom (saved at version {load.Version}).");
                    return load.State;
                case SaveLoadStatus.TooNew:
                    Debug.LogWarning($"#Save# The save is from a newer build (version {load.Version}); playing a new kingdom without writing over it.");
                    break;
                case SaveLoadStatus.Unreadable:
                    Debug.LogWarning("#Save# The save could not be read; it was set aside and a new kingdom begins.");
                    break;
            }

            return NewKingdom.Create(map, settings, currencies, clock.NowMs);
        }
    }
}
