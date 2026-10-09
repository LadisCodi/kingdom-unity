using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Doors;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Sites;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Tutorial
{
    // The conditions answered by the kingdom as it stands: the quest chain, research, buildings, the fog and what it
    // hides. Pure reads; a full store is judged at the last advance, never at a clock.
    public class KingdomConditions : IConditionReader
    {
        private readonly KingdomState _state;
        private readonly QuestChain _chain;
        private readonly ICatalog<IQuestDefinition> _quests;
        private readonly Researching _research;
        private readonly IBookshelf _shelf;
        private readonly Doors.Doors _doors;
        private readonly IBuildingGroups _groups;
        private readonly FogOfWar _fog;
        private readonly Sighting _sighting;
        private readonly IProvinceSites _sites;
        private readonly Ruins _ruins;
        private readonly ManaPool _mana;
        private readonly KnowledgeBar _knowledge;
        private readonly Stores _stores;
        private readonly Workforce _crews;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IPlanting _planting;

        public KingdomConditions(KingdomState state, QuestChain chain, ICatalog<IQuestDefinition> quests, Researching research,
            IBookshelf shelf, Doors.Doors doors, IBuildingGroups groups, FogOfWar fog, Sighting sighting, IProvinceSites sites, Ruins ruins,
            ManaPool mana, KnowledgeBar knowledge, Stores stores, Workforce crews, ICatalog<IBuildingDefinition> buildings, IPlanting planting)
        {
            _buildings = buildings;
            _planting = planting;
            _state = state;
            _chain = chain;
            _quests = quests;
            _research = research;
            _shelf = shelf;
            _doors = doors;
            _groups = groups;
            _fog = fog;
            _sighting = sighting;
            _sites = sites;
            _ruins = ruins;
            _mana = mana;
            _knowledge = knowledge;
            _stores = stores;
            _crews = crews;
        }

        private CityState City => _state.City;

        public bool TryHolds(Condition c, int tapsAtStart, out bool holds)
        {
            bool? answer = c.Kind switch
            {
                ConditionKind.QuestReached => QuestAt(c.Target) is var at && at >= 0 && _chain.Index >= at,
                ConditionKind.QuestClaimed => QuestAt(c.Target) is var at && at >= 0 && _chain.Index > at,
                ConditionKind.QuestComplete => QuestDone(c.Target, q => _chain.IsComplete(q)),
                ConditionKind.QuestProgress => QuestDone(c.Target, q => _chain.Value(q) >= c.AtLeast()),
                ConditionKind.TechDone => _research.IsComplete(c.Target),
                ConditionKind.TechFilled => _research.IsComplete(c.Target) || IsFilled(c.Target),
                ConditionKind.Placed => City.Districts.Count(d => d.DefinitionId == c.Target) + Planted(c.Target) >= c.AtLeast(),
                ConditionKind.Built => City.Districts.Count(d => d.Built && d.DefinitionId == c.Target) + Planted(c.Target) >= c.AtLeast(),
                ConditionKind.Upgraded => Upgraded(c),
                ConditionKind.Population => City.Population >= c.AtLeast(),
                // Villagers called: home or on their way.
                ConditionKind.Training => City.Population + City.Trainees.Count >= c.AtLeast(),
                ConditionKind.BuildersBusy => CityQueries.BusyBuilders(City) >= City.Builders,
                ConditionKind.ManaEmpty => _mana.Amount < 1,
                ConditionKind.KnowledgeFull => _knowledge.Amount >= _knowledge.Cap,
                ConditionKind.StoreFull => City.Districts.Any(d => d.Built && (c.Target == "" || d.DefinitionId == c.Target)
                    && _stores.IsFull(d, _state.LastAdvance)),
                ConditionKind.IdleCrew => City.Districts.Any(d => d.Built && _crews.Assigned(d.Id) > 0
                    && _crews.Assigned(d.Id) > _crews.Workable(d).Count),
                ConditionKind.BookOpen => _shelf.IsOpen(c.Target),
                ConditionKind.DoorOpen => Enum.TryParse<DoorId>(c.Target, true, out var door) && _doors.IsOpen(door),
                ConditionKind.Revealed => _fog.RevealedCount >= c.AtLeast(),
                ConditionKind.FeatureSeen => _state.Ground.Features.Any(f => f.Value == c.Target
                    && _fog.VisibilityAt(f.Key) != Visibility.Undiscovered),
                ConditionKind.Sighted => _sighting.Things.Any(t => c.Target == "" || t.Id == c.Target
                    || string.Equals(t.Kind.ToString(), c.Target, StringComparison.OrdinalIgnoreCase)
                    || (t.Kind == SightedKind.Landmark && _sites.Landmarks.Any(l => l.Id == t.Id && l.Kind == c.Target))),
                ConditionKind.TreasureRevealed => TreasuresPicked > 0 || _state.Fog.Treasures.Keys.Any(_fog.IsRevealed),
                ConditionKind.TreasurePicked => TreasuresPicked >= c.AtLeast(),
                ConditionKind.AbandonedRevealed => Abandoned(c.Target) is { } site
                    && (_state.Sites.Repaired.Contains(site.Id) || _fog.VisibilityAt(site.Anchor) == Visibility.Revealed),
                ConditionKind.Repairing => _state.Sites.Repaired.Contains(c.Target),
                ConditionKind.CanRepair => _ruins.Get(c.Target) != null && _ruins.Refusal(c.Target) == RepairRefusal.None,
                ConditionKind.LandmarkClaimed => _sites.Landmarks.Any(l => _state.Sites.Claimed.Contains(l.Id)
                    && (c.Target == "" || l.Id == c.Target || l.Kind == c.Target)),
                ConditionKind.LandmarkSeen => _sites.Landmarks.FirstOrDefault(l => l.Id == c.Target) is { } landmark
                    && _fog.VisibilityAt(landmark.Anchor) != Visibility.Undiscovered,
                _ => null,
            };

            holds = answer ?? false;
            return answer.HasValue;
        }

        // A plantable stands as its feature on the ground, never as a district.
        private int Planted(string target)
            => _buildings.TryGet(target, out var building) && building.Production.Plants != null ? _planting.Count(building.Production.Plants) : 0;

        private int TreasuresPicked => _state.Fog.TreasuresPlaced - _state.Fog.Treasures.Count;

        private IAbandonedSite Abandoned(string id) => _sites.Abandoned.FirstOrDefault(a => a.Id == id);

        private bool IsFilled(string tech)
        {
            try { return _research.IsFilled(tech); }
            catch (KeyNotFoundException) { return false; }
        }

        private int QuestAt(string id)
        {
            var items = _quests.Items;
            for (var i = 0; i < items.Count; i++)
                if (items[i].Id == id) return i;
            return -1;
        }

        private bool QuestDone(string id, Func<IQuestDefinition, bool> done)
        {
            var at = QuestAt(id);
            if (at < 0) return false;
            if (_chain.Index > at) return true;
            return _chain.Index == at && done(_quests.Items[at]);
        }

        // One of a kind (or of a group) at the level, or with its upgrade to it under way: the Upgrade pressed is
        // the lesson.
        private bool Upgraded(Condition c)
        {
            var level = c.AtLeast(2);
            return City.Districts.Any(d => _groups.Names(c.Target, d.DefinitionId)
                && (d.Level >= level || City.Jobs.Any(j => j.DistrictId == d.Id && j.TargetLevel >= level)));
        }
    }
}
