using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tutorial.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Doors
{
    // What opens each door: a fact about the kingdom, mostly how far along the quest chain it is. A door once open
    // is remembered open for good. Doors for systems not in the game yet (heroes, relics, the store, the world, the
    // Bag, friends) stay shut until their facts can happen.
    public class Doors : IMorning
    {
        private const string TAVERN = "Tavern";
        private const string WATCHTOWER = "Watchtower";
        private const string TOWNHALL = "Townhall";

        private readonly TutorialState _tutorial;
        private readonly IChainPosition _chain;
        private readonly ICatalog<IQuestDefinition> _quests;
        private readonly Researching _research;
        private readonly CityState _city;
        private readonly SitesState _sites;
        private readonly IProvinceSites _provinceSites;
        private readonly IConstructionSettings _settings;

        public Doors(TutorialState tutorial, IChainPosition chain, ICatalog<IQuestDefinition> quests, Researching research, CityState city,
            SitesState sites, IProvinceSites provinceSites, IConstructionSettings settings)
        {
            _tutorial = tutorial;
            _chain = chain;
            _quests = quests;
            _research = research;
            _city = city;
            _sites = sites;
            _provinceSites = provinceSites;
            _settings = settings;
        }

        public bool IsOpen(DoorId door) => _tutorial.Veteran || _tutorial.Seen.Contains(Key(door)) || Opens(door);

        // Doors that have opened and not been marked seen yet: each is announced once.
        public IReadOnlyList<DoorId> FreshlyOpen()
            => _tutorial.Veteran
                ? Array.Empty<DoorId>()
                : Enum.GetValues(typeof(DoorId)).Cast<DoorId>().Where(d => !_tutorial.Seen.Contains(Key(d)) && Opens(d)).ToList();

        public void MarkSeen(DoorId door) => _tutorial.Seen.Add(Key(door));

        // The First Morning: until the tax-day quest is claimed. The Townhall's own Gold shows no bubble then.
        public bool FirstMorningOn => !_tutorial.Veteran && !QuestClaimed("TaxDay");

        public static string Key(DoorId door) => "door:" + door.ToString().ToLowerInvariant();

        private bool Opens(DoorId door) => door switch
        {
            DoorId.Research or DoorId.Knowledge => QuestReached("Woodcraft") || _research.Completed.Count > 0,
            DoorId.Build => QuestReached("GrowingTown") || _city.Districts.Any(d => d.DefinitionId != TOWNHALL && !WasAbandoned(d)),
            DoorId.Heroes or DoorId.Banner => _city.Districts.Any(d => d.DefinitionId == TAVERN && d.Built),
            DoorId.Store or DoorId.Survey => CityQueries.TownhallLevel(_city, _settings) >= 2,
            DoorId.World => _city.Districts.Any(d => d.DefinitionId == WATCHTOWER && d.Built),
            _ => false,
        };

        private bool QuestReached(string id)
        {
            var at = IndexOf(id);
            return at >= 0 && _chain.Index >= at;
        }

        private bool QuestClaimed(string id)
        {
            var at = IndexOf(id);
            return at >= 0 && _chain.Index > at;
        }

        private int IndexOf(string id)
        {
            for (var i = 0; i < _quests.Items.Count; i++)
            {
                if (_quests.Items[i].Id == id) return i;
            }

            return -1;
        }

        // Standing where a repaired ruin stood: the fog's, not the player's own.
        private bool WasAbandoned(DistrictState district)
            => _provinceSites.Abandoned.Any(a => _sites.Repaired.Contains(a.Id) && a.Anchor == district.Anchor && a.District == district.DefinitionId);
    }
}
