using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Army;
using Codigames.Modules.Randomness;

namespace Codigames.Kingdom.Battles
{
    // What a room fields from its power budget (Docs/features/combat.md §11), seeded by the room's own address: the same
    // room is the same fight every time it is looked at, so the preview and the attempt are one query. The budget is
    // spent, not decorated — every squad and villain costs its power.
    public class EnemyGenerator
    {
        private readonly Combat _combat;
        private readonly ICombatSettings _settings;

        public EnemyGenerator(Combat combat)
        {
            _combat = combat;
            _settings = combat.Settings;
        }

        // `parts` identify the room, never the moment it was asked. `affinity` is a unit or "Any"; an authored `mix`
        // overrides its shares; `pool` the villains it may field, `boss` the one it always does.
        public EnemyPlan Generate(uint seed, object[] parts, double budgetAsked, string affinity, IReadOnlyDictionary<string, double> mix = null,
            IReadOnlyList<FighterSpec> pool = null, FighterSpec boss = null)
        {
            pool ??= Array.Empty<FighterSpec>();
            var budget = (double)Math.Max(1, Combat.JsRound(budgetAsked));
            var fighters = new List<FighterSpec>();

            // The villains first, because what is left is what the squads may cost.
            if (boss != null)
            {
                fighters.Add(boss.Clone());
                budget = Math.Max(1, budget - boss.Power);
            }

            if (pool.Count > 0 && budget >= _settings.GenVillainThreshold)
            {
                double purse = Combat.JsRound(budget * _settings.GenVillainShare);
                for (var i = fighters.Count; i < _settings.GenVillainSlots; i++)
                {
                    var picked = pool[Rand.Int(seed, pool.Count, With(parts, "villain", i))];
                    if (picked.Power > purse) break;
                    fighters.Add(picked.Clone());
                    purse -= picked.Power;
                    budget = Math.Max(1, budget - picked.Power);
                }
            }

            // How many squads: the roll gives the variety, the budget the floor.
            var span = _settings.GenSlotsMax - _settings.GenSlotsMin;
            var roll = Rand.Int(seed, span + 1, With(parts, "slots"));
            var warrior = _combat.Unit("Warrior");
            var needed = (int)Math.Ceiling(budget / (warrior.SquadSize * warrior.Ranks[0].Power));
            var wanted = Math.Min(_settings.GenSlotsMax, Math.Max(_settings.GenSlotsMin, Math.Max(roll + _settings.GenSlotsMin, needed)));

            // The lair's own type takes the lion's share and is spent first; the rest is split evenly. An authored mix
            // fields its types alone, by weight, the heaviest first.
            var types = Combat.CHART.Select(c => c.Unit).ToList();
            var mixed = mix == null ? new List<string>() : types.Where(t => (mix.TryGetValue(t, out var w) ? w : 0) > 0)
                .OrderByDescending(t => mix[t]).ThenBy(t => types.IndexOf(t)).ToList();
            var order = mixed.Count > 0 ? mixed : affinity == "Any" ? types : new[] { affinity }.Concat(types.Where(t => t != affinity)).ToList();
            var share = new Dictionary<string, double>();
            if (mixed.Count > 0)
            {
                var total = mixed.Sum(t => mix[t]);
                foreach (var t in mixed) share[t] = budget * mix[t] / total;
            }
            else if (affinity == "Any")
            {
                foreach (var t in types) share[t] = budget / types.Count;
            }
            else
            {
                double lion = Combat.JsRound(budget * 0.6);
                share[affinity] = lion;
                foreach (var t in order.Skip(1)) share[t] = (budget - lion) / (order.Count - 1);
            }

            var filler = mixed.Count > 0 ? mixed[0] : affinity == "Any" ? order[0] : affinity;

            // The squads at a rank: all I while the board holds the budget, else the board in R−1 and R, the first squads
            // — the affinity's, spent first — promoted.
            (List<SquadSpec> Squads, double Left) Plan(Func<int, int> rankAt)
            {
                var squads = new List<SquadSpec>();
                var left = budget;
                void Add(string unit, double purse)
                {
                    var want = purse;
                    while (want > 0 && squads.Count < wanted)
                    {
                        var troop = Troops.Of(unit, rankAt(squads.Count));
                        var power = _combat.RankOf(troop).Power;
                        var count = (int)Math.Min(_combat.Unit(unit).SquadSize, Math.Floor(want / power));
                        if (count <= 0) break;
                        squads.Add(new SquadSpec(troop, count));
                        want -= count * power;
                        left -= count * power;
                    }
                }

                foreach (var t in order) Add(t, Math.Min(share.TryGetValue(t, out var s) ? s : 0, Math.Max(0, left)));
                // What the shares left on the table goes to the affinity, while there is a slot for it.
                while (squads.Count < wanted && left >= _combat.RankOf(Troops.Of(filler, rankAt(squads.Count))).Power) Add(filler, left);
                return (squads, left);
            }

            // The board holds the budget when what is left is rounding, not an army it had no room for.
            bool Holds((List<SquadSpec> Squads, double Left) p, int low)
                => p.Left <= Math.Max(budget * 0.1, _combat.RankOf(Troops.Of(filler, low)).Power - 1);

            var best = Plan(_ => 1);
            // Only a full board evolves.
            if (!Holds(best, 1) && wanted >= _settings.GenSlotsMax)
            {
                var found = false;
                for (var r = 2; r <= 5 && !found; r++)
                {
                    for (var k = 1; k <= wanted; k++)
                    {
                        var top = r;
                        var low = r - 1;
                        var promoted = k;
                        best = Plan(i => i < promoted ? top : low);
                        if (!Holds(best, low)) continue;
                        found = true;
                        break;
                    }
                }
            }

            var result = best.Squads;
            // A room always fields something, even at a budget of one.
            if (result.Count == 0) result.Add(new SquadSpec(filler, 1));

            // Scaled villains (§9.4): what a board of rank-V squads cannot hold goes to the villains, spread evenly.
            if (best.Left > 0 && !Holds(best, 5) && pool.Count > 0)
            {
                for (var i = fighters.Count; i < _settings.GenVillainSlots; i++)
                    fighters.Add(pool[Rand.Int(seed, pool.Count, With(parts, "scaled", i))].Clone());
                var each = best.Left / fighters.Count;
                for (var i = 0; i < fighters.Count; i++) fighters[i] = Scale(fighters[i], each);
            }

            return new EnemyPlan(result, fighters);
        }

        // A villain grown by `extra` budget: k = (power + extra) / power; hp, dmg and power × k; atk and def +2 for every
        // ×1.6 in k, as a troop's rank climbs. Its skill, passive and cooldown stay its own.
        public static FighterSpec Scale(FighterSpec f, double extra)
        {
            if (extra <= 0) return f;
            var k = (f.Power + extra) / f.Power;
            var steps = (int)Math.Floor(Math.Log(k) / Math.Log(1.6) + 1e-9);
            var scaled = f.Clone();
            scaled.Hp = Combat.JsRound(f.Hp * k);
            scaled.Dmg = Combat.JsRound(f.Dmg * k);
            scaled.Power = Combat.JsRound(f.Power * k);
            scaled.Atk = f.Atk + 2 * steps;
            scaled.Def = f.Def + 2 * steps;
            return scaled;
        }

        private static object[] With(object[] parts, params object[] more) => parts.Concat(more).ToArray();
    }
}
