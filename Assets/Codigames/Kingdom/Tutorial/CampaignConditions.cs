using System;
using System.Linq;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Bag.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Tutorial
{
    // The conditions answered by what lies past the city's walls and in its pockets: the lairs found, beaten and
    // cleared, a raid's hoard, the wounded and the soldiers, the heroes, the Bag's items, and the relics in their Shrines.
    public class CampaignConditions : IConditionReader
    {
        private readonly KingdomState _state;
        private readonly ICatalog<IItemDefinition> _items;

        public CampaignConditions(KingdomState state, ICatalog<IItemDefinition> items)
        {
            _state = state;
            _items = items;
        }

        public bool TryHolds(Condition c, int tapsAtStart, out bool holds)
        {
            var lairs = _state.Lairs.Lairs;
            bool? answer = c.Kind switch
            {
                ConditionKind.LairFound => c.Target == string.Empty ? lairs.Count > 0 : lairs.ContainsKey(c.Target),
                ConditionKind.LairDefeated => lairs.Any(l => (c.Target == string.Empty || l.Key == c.Target) && (l.Value.Defeated || l.Value.Cleared)),
                ConditionKind.LairCleared => lairs.Any(l => (c.Target == string.Empty || l.Key == c.Target) && l.Value.Cleared),
                ConditionKind.Raided => lairs.Values.Any(l => l.Hoard.Values.Any(n => n > 0)),
                ConditionKind.Wounded => _state.Army.Wounded.Values.Sum() > 0,
                ConditionKind.Heroes => _state.Heroes.Owned.Count >= c.AtLeast(),
                // Soldiers: that many standing, or one in training.
                ConditionKind.Troops => _state.Army.Troops.Values.Sum() >= c.AtLeast()
                                        || _state.Army.Lines.Any(i => i.Kind == Army.State.HallItemKind.Recruit),
                // An item, or an item kind, held; and none left of it once used.
                ConditionKind.HoldsItem => Held(c.Target) >= c.AtLeast(),
                ConditionKind.ItemUsed => Held(c.Target) == 0,
                ConditionKind.RelicHosted => _state.Relics.Hosts.Values.Any(r => c.Target == string.Empty || r == c.Target),
                _ => null,
            };
            holds = answer ?? false;
            return answer.HasValue;
        }

        private int Held(string target)
            => _state.Bag.Held.Where(h => h.Key == target
                    || (_items.TryGet(h.Key, out var item) && string.Equals(item.Kind.ToString(), target, StringComparison.OrdinalIgnoreCase)))
                .Sum(h => h.Value);
    }
}
