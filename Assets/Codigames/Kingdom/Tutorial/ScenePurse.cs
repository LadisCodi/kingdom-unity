using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Quests;

namespace Codigames.Kingdom.Tutorial
{
    // Can the purse pay for what a scene leads to? Every placing/placed line's next building, every upgraded line's
    // next level on at least one building it names. What is already done asks nothing, and a building a line stocks
    // asks nothing either.
    public class ScenePurse : IScenePurse
    {
        private readonly Construction _construction;
        private readonly CityState _city;
        private readonly ITreasury _treasury;
        private readonly IConditions _conditions;
        private readonly IBuildingGroups _groups;

        public ScenePurse(Construction construction, CityState city, ITreasury treasury, IConditions conditions, IBuildingGroups groups)
        {
            _construction = construction;
            _city = city;
            _treasury = treasury;
            _conditions = conditions;
            _groups = groups;
        }

        public bool CanPayFor(ISceneDefinition scene)
        {
            var stocked = new HashSet<string>(scene.Lines.Where(l => !string.IsNullOrEmpty(l.Stocks)).Select(l => l.Stocks));
            return scene.Lines.All(line =>
            {
                var until = line.Until;
                if (until.Kind == ConditionKind.Placing || until.Kind == ConditionKind.Placed)
                {
                    var id = until.Target;
                    // A placed line already met: the building is up, nothing to pay.
                    if (scene.Lines.Any(m => m.Until.Kind == ConditionKind.Placed && m.Until.Target == id && _conditions.Holds(m.Until))) return true;
                    return stocked.Contains(id) || _treasury.CanAfford(_construction.Offer(id).Price);
                }

                if (until.Kind == ConditionKind.Upgraded && !_conditions.Holds(until))
                {
                    var level = (int)until.AtLeast(2);
                    return _city.Districts.Any(d => _groups.Names(until.Target, d.DefinitionId) && d.Level < level
                        && _treasury.CanAfford(_construction.UpgradeOffer(d.Id).Price));
                }

                return true;
            });
        }

        public bool Needless(ISceneLine line) => !string.IsNullOrEmpty(line.Stocks) && _treasury.CanAfford(_construction.Offer(line.Stocks).Price);

        public IReadOnlyDictionary<string, double> Stock(string building)
        {
            var added = new Dictionary<string, double>();
            foreach (var cost in _construction.Offer(building).Price)
            {
                var lacking = cost.Value - _treasury.Get(cost.Key);
                if (lacking <= 0) continue;
                var got = _treasury.Add(cost.Key, Math.Ceiling(lacking));
                if (got > 0) added[cost.Key] = got;
            }

            return added;
        }
    }
}
