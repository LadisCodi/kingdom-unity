using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Army;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Battles
{
    // The tick resolver (Docs/features/combat.md §3, §7–§10, §13): two boards in, an ordered list of events out. It reads
    // no state, no clock and no random number; everything is integer, positions included, so the same boards give the
    // same list on any engine — the web prototype's, event for event.
    public class Combat
    {
        // Who beats whom on the type chart (§7), in the web's order: what the generator walks.
        public static readonly IReadOnlyList<(string Unit, string Beats)> CHART = new[]
        {
            ("Lancer", "Cavalry"), ("Cavalry", "Archer"), ("Archer", "Warrior"), ("Warrior", "Lancer"),
        };

        private readonly ICatalog<IUnitDefinition> _units;
        private readonly ICombatSettings _settings;

        public Combat(ICatalog<IUnitDefinition> units, ICombatSettings settings)
        {
            _units = units;
            _settings = settings;
        }

        public ICombatSettings Settings => _settings;

        public IUnitDefinition Unit(string unit) => _units.Get(unit);

        // A troop's numbers: its unit's, at its rank.
        public UnitRank RankOf(string troop)
        {
            var unit = _units.Get(Troops.UnitOf(troop));
            return unit.Ranks[Math.Min(Troops.RankOf(troop), unit.Ranks.Count) - 1];
        }

        public static string Beats(string unit) => CHART.FirstOrDefault(c => c.Unit == unit).Beats;

        public Targeting TargetingOf(string unit)
        {
            var tags = _units.Get(unit).Tags;
            return tags.Contains("Distance") ? Targeting.Ranged : tags.Contains("Mounted") ? Targeting.Flanker : Targeting.Melee;
        }

        // The ones that have to reach the enemy stand in front, the ones that shoot behind.
        public Row RowOf(string unit) => TargetingOf(unit) == Targeting.Ranged ? Row.Back : Row.Front;

        // One side, its passives baked in once (§9.2): every squad whose type matches a fighter on its side takes that
        // fighter's multipliers, added on the excess; the flats sum; a rally reaches every squad. Squads first, the
        // heroes after, so a troop's slot id never depends on who leads it.
        public Board BuildBoard(IReadOnlyList<SquadSpec> squads, IReadOnlyList<FighterSpec> fighters, TroopBonus bonus = null)
        {
            bonus ??= TroopBonus.None;
            var dmgMult = new Dictionary<string, double>();
            var hpMult = new Dictionary<string, double>();
            var defFlat = new Dictionary<string, double>();
            double rallyDmg = 0, rallyHp = 0;
            var rallyDef = 0.0;
            foreach (var f in fighters)
            {
                dmgMult[f.Type] = (dmgMult.TryGetValue(f.Type, out var d) ? d : 1) + (f.TroopDmgMult - 1);
                hpMult[f.Type] = (hpMult.TryGetValue(f.Type, out var h) ? h : 1) + (f.TroopHpMult - 1);
                defFlat[f.Type] = (defFlat.TryGetValue(f.Type, out var df) ? df : 0) + f.TroopDefBonus;
                if (f.Skill?.Id == "WarCry") rallyDmg += f.Skill.Amount / 1000.0;
                if (f.Skill?.Id == "Vigour") rallyHp += f.Skill.Amount / 1000.0;
                if (f.Skill?.Id == "Bulwark") rallyDef += f.Skill.Amount;
            }

            var slots = new List<BoardSlot>();
            foreach (var squad in squads)
            {
                if (squad.Count <= 0) continue;
                var type = Troops.UnitOf(squad.Troop);
                var unit = _units.Get(type);
                var rank = RankOf(squad.Troop);
                var hpUnit = Math.Max(1, JsRound(rank.Hp * ((hpMult.TryGetValue(type, out var hm) ? hm : 1) + rallyHp) * bonus.HpMult(squad.Troop)));
                slots.Add(new BoardSlot
                {
                    Id = slots.Count,
                    Row = RowOf(type),
                    Type = type,
                    Troop = squad.Troop,
                    Name = Troops.RankOf(squad.Troop) <= 1 ? unit.Name : unit.Name + " " + Troops.Roman(Troops.RankOf(squad.Troop)),
                    Count = squad.Count,
                    Frontage = unit.Frontage,
                    Atk = rank.Atk,
                    Dmg = Math.Max(1, JsRound((rank.Dmg + bonus.Dmg(squad.Troop)) * ((dmgMult.TryGetValue(type, out var dm) ? dm : 1) + rallyDmg))),
                    Def = rank.Def + bonus.Def(squad.Troop) + (defFlat.TryGetValue(type, out var dfl) ? dfl : 0) + rallyDef,
                    HpUnit = hpUnit,
                    HpPool = squad.Count * hpUnit,
                    Cooldown = unit.Cooldown,
                    Power = rank.Power,
                });
            }

            foreach (var f in fighters)
            {
                slots.Add(new BoardSlot
                {
                    Id = slots.Count,
                    IsHero = true,
                    // A hero stands where its type would, and fights on the same chart.
                    Row = RowOf(f.Type),
                    Type = f.Type,
                    FighterId = f.Id,
                    Name = f.Name,
                    Count = 1,
                    Frontage = 1,
                    Atk = JsRound(f.Atk),
                    Dmg = f.Dmg,
                    Def = f.Def,
                    HpUnit = f.Hp,
                    HpPool = Math.Min(f.Hp, f.HpNow ?? f.Hp),
                    Cooldown = f.Cooldown,
                    Power = f.Power,
                    Skill = f.Skill,
                });
            }

            return new Board(slots);
        }

        // Where a side stands as the fight opens (§3): three lines behind the gap — front row, back row, heroes — each
        // centred, its slots in id order a column apart.
        public FieldPoint[] Place(IReadOnlyList<BoardSlot> slots, Side side)
        {
            var sign = side == Side.Ours ? 1 : -1;
            int LineOf(BoardSlot s) => s.IsHero ? 2 : s.Row == Row.Front ? 0 : 1;
            var output = new FieldPoint[slots.Count];
            for (var line = 0; line < 3; line++)
            {
                var inLine = slots.Where(s => LineOf(s) == line).ToList();
                for (var i = 0; i < inLine.Count; i++)
                {
                    output[inLine[i].Id] = new FieldPoint(
                        (int)Math.Truncate((2.0 * i - (inLine.Count - 1)) * _settings.FieldColPitch / 2),
                        sign * (_settings.FieldGap / 2 + line * _settings.FieldRowPitch));
                }
            }

            return output;
        }

        // The Attack and Defence rule (§7), Heroes III's: per mille, inside its caps.
        public int AttackMultiplier(double atk, double def)
        {
            var lead = JsRound(atk) - JsRound(def);
            return lead >= 0
                ? 1000 + Math.Min(_settings.AttackCapPerMille, _settings.AttackStepPerMille * lead)
                : 1000 - Math.Min(_settings.DefenceCapPerMille, _settings.DefenceStepPerMille * -lead);
        }

        // One fight, start to finish (§10). `ours` is the attacker: it swings first within a tick, and a fight out of
        // clock is the defender's. There are no draws.
        public BattleLog Resolve(Board ours, Board theirs)
        {
            var sides = new[] { Copy(ours), Copy(theirs) };
            var where = new[] { Place(sides[0].Slots, Side.Ours), Place(sides[1].Slots, Side.Theirs) };
            var events = new List<BattleEvent>
            {
                new()
                {
                    Kind = BattleEventKind.Start,
                    Ours = sides[0].Slots.Select(s => (s.Clone(), where[0][s.Id])).ToList(),
                    Theirs = sides[1].Slots.Select(s => (s.Clone(), where[1][s.Id])).ToList(),
                },
            };
            var ready = new[] { sides[0].Slots.Select(s => s.Cooldown).ToArray(), sides[1].Slots.Select(s => s.Cooldown).ToArray() };
            var skillReady = new[] { sides[0].Slots.Select(s => s.Skill?.Every ?? 0).ToArray(), sides[1].Slots.Select(s => s.Skill?.Every ?? 0).ToArray() };
            // What a heal may bring a slot back to: what it walked in with.
            var maxPool = new[] { sides[0].Slots.Select(MaxPool).ToArray(), sides[1].Slots.Select(MaxPool).ToArray() };
            var shield = new[] { new int[sides[0].Slots.Count], new int[sides[1].Slots.Count] };

            BattleLog Finish(int tick, Side winner, EndReason reason)
            {
                events.Add(new BattleEvent { Kind = BattleEventKind.End, Tick = tick, Winner = winner, Reason = reason });
                return new BattleLog(events, winner, reason, tick);
            }

            // A side with nothing standing has lost before a blow is struck.
            if (!Living(sides[0]).Any()) return Finish(0, Side.Theirs, EndReason.Wiped);
            if (!Living(sides[1]).Any()) return Finish(0, Side.Ours, EndReason.Wiped);

            // The rallies have been in the board since it was built; they are named as the fight opens.
            for (var s = 0; s < 2; s++)
                foreach (var slot in sides[s].Slots)
                    if (slot.Skill != null && Skills.KindOf(slot.Skill.Id) == SkillKind.Rally)
                        events.Add(new BattleEvent { Kind = BattleEventKind.Skill, Tick = 0, From = new SlotRef((Side)s, slot.Id), Skill = slot.Skill.Id });

            // One blow landing: the shield first, then the health. True when it ended the fight.
            bool Land(int tick, int side, BoardSlot from, BoardSlot target, int hits, int @base, string skill)
            {
                var foe = 1 - side;
                var raw = Math.Max(1, (int)Math.Floor((long)hits * @base * AttackMultiplier(from.Atk, target.Def) / 1000.0));
                var (num, den) = Fraction(from.Type, target.Type);
                var dealt = (int)Math.Floor((long)raw * num / (double)den);
                var absorbed = Math.Min(shield[foe][target.Id], dealt);
                shield[foe][target.Id] -= absorbed;
                dealt -= absorbed;
                var before = target.Alive;
                target.HpPool = Math.Max(0, target.HpPool - dealt);
                var after = target.Alive;
                var at = new SlotRef((Side)foe, target.Id);
                events.Add(new BattleEvent
                {
                    Kind = BattleEventKind.Attack, Tick = tick, From = new SlotRef((Side)side, from.Id), At = at, Hits = hits, Dealt = dealt,
                    Skill = skill, Absorbed = absorbed, Edge = num > den ? 1 : num < den ? -1 : 0,
                });
                if (after != before) events.Add(new BattleEvent { Kind = BattleEventKind.TroopsLost, Tick = tick, At = at, Alive = after, HpPool = target.HpPool });
                if (target.HpPool > 0) return false;
                events.Add(new BattleEvent { Kind = BattleEventKind.SlotWiped, Tick = tick, At = at });
                return !Living(sides[foe]).Any();
            }

            // A skill on its clock fires (§9.3). True when it ended the fight.
            bool Fire(int tick, int side, BoardSlot slot, SlotSkill skill)
            {
                var foe = 1 - side;
                var enemies = Living(sides[foe]).ToList();
                var allies = Living(sides[side]).ToList();
                BoardSlot Least(List<BoardSlot> list) => list.Count == 0 ? null
                    : list.Aggregate((b, s) => s.HpPool < b.HpPool || (s.HpPool == b.HpPool && s.Id < b.Id) ? s : b);
                BoardSlot Most(List<BoardSlot> list) => list.Count == 0 ? null
                    : list.Aggregate((b, s) => s.HpPool > b.HpPool || (s.HpPool == b.HpPool && s.Id < b.Id) ? s : b);
                var kind = Skills.KindOf(skill.Id);
                var targets = new List<BoardSlot>();
                if (kind == SkillKind.Strike)
                {
                    var front = enemies.Where(s => s.Row == Row.Front).ToList();
                    var back = enemies.Where(s => s.Row == Row.Back).ToList();
                    if (skill.Id == "Volley") targets = enemies;
                    else if (skill.Id == "Cleave") targets = front.Count > 0 ? front : enemies;
                    else
                    {
                        var one = skill.Id == "Crush" ? Most(enemies) : skill.Id == "Ambush" ? Least(back.Count > 0 ? back : enemies) : Least(enemies);
                        if (one != null) targets.Add(one);
                    }
                }
                else if (kind == SkillKind.Heal)
                {
                    var max = maxPool[side];
                    var hurt = allies.Where(s => s.HpPool < max[s.Id]).ToList();
                    if (skill.Id == "Wave") targets = hurt;
                    else if (hurt.Count > 0)
                    {
                        // The most wounded: the lowest share of what it walked in with, compared without a division.
                        targets.Add(hurt.Aggregate((b, s) =>
                        {
                            var sb = (long)s.HpPool * max[b.Id];
                            var bs = (long)b.HpPool * max[s.Id];
                            return sb < bs || (sb == bs && s.Id < b.Id) ? s : b;
                        }));
                    }
                }
                else if (kind == SkillKind.Shield)
                {
                    var front = allies.Where(s => s.Row == Row.Front).ToList();
                    var t = Least(front.Count > 0 ? front : allies);
                    if (t != null) targets.Add(t);
                }
                else if (kind == SkillKind.Daze)
                {
                    long Threat(BoardSlot s) => (long)s.Dmg * Math.Min(s.Alive, s.Frontage);
                    if (enemies.Count > 0)
                        targets.Add(enemies.Aggregate((b, s) => Threat(s) > Threat(b) || (Threat(s) == Threat(b) && s.Id < b.Id) ? s : b));
                }

                if (targets.Count == 0) return false;
                events.Add(new BattleEvent { Kind = BattleEventKind.Skill, Tick = tick, From = new SlotRef((Side)side, slot.Id), Skill = skill.Id });
                foreach (var target in targets)
                {
                    switch (kind)
                    {
                        case SkillKind.Strike:
                            if (target.HpPool <= 0) continue;
                            if (Land(tick, side, slot, target, 1, (int)Math.Floor((long)slot.Dmg * skill.Amount / 1000.0), skill.Id)) return true;
                            break;
                        case SkillKind.Heal:
                        {
                            var max = maxPool[side][target.Id];
                            var amount = Math.Max(1, (int)Math.Floor((long)max * skill.Amount / 1000.0));
                            var pool = Math.Min(max, target.HpPool + amount);
                            var healed = pool - target.HpPool;
                            target.HpPool = pool;
                            events.Add(new BattleEvent
                            {
                                Kind = BattleEventKind.Healed, Tick = tick, At = new SlotRef((Side)side, target.Id), Amount = healed, Alive = target.Alive, HpPool = pool,
                            });
                            break;
                        }
                        case SkillKind.Shield:
                        {
                            var amount = (int)Math.Floor((long)slot.HpUnit * skill.Amount / 1000.0);
                            shield[side][target.Id] = Math.Max(shield[side][target.Id], amount);
                            events.Add(new BattleEvent { Kind = BattleEventKind.Shielded, Tick = tick, At = new SlotRef((Side)side, target.Id), Amount = shield[side][target.Id] });
                            break;
                        }
                        case SkillKind.Daze:
                            ready[foe][target.Id] += skill.Amount;
                            events.Add(new BattleEvent { Kind = BattleEventKind.Dazed, Tick = tick, At = new SlotRef((Side)foe, target.Id), Amount = skill.Amount });
                            break;
                    }
                }

                return false;
            }

            for (var tick = 1; tick <= _settings.TimeoutTicks; tick++)
            {
                // Everyone walks at once, from where everyone stood as the tick began.
                var before = new[] { (FieldPoint[])where[0].Clone(), (FieldPoint[])where[1].Clone() };
                for (var side = 0; side < 2; side++)
                {
                    var foe = 1 - side;
                    foreach (var slot in sides[side].Slots)
                    {
                        if (slot.HpPool <= 0) continue;
                        var here = before[side][slot.Id];
                        var target = PickTarget(slot, here, sides[foe], before[foe]);
                        if (target == null) break;
                        var unit = _units.Get(slot.Type);
                        var there = before[foe][target.Id];
                        var d2 = Dist2(here, there);
                        if (d2 <= (long)unit.Range * unit.Range || unit.Speed <= 0) continue;
                        var dist = Isqrt(d2);
                        long dx = there.X - here.X;
                        long dy = there.Y - here.Y;
                        // The last step lands just inside range — over `dist + 1`, truncated toward the target.
                        var last = dist - unit.Range <= unit.Speed;
                        var x = last ? there.X - (int)Math.Truncate(dx * unit.Range / (double)(dist + 1)) : here.X + JsRound(dx * unit.Speed / (double)dist);
                        var y = last ? there.Y - (int)Math.Truncate(dy * unit.Range / (double)(dist + 1)) : here.Y + JsRound(dy * unit.Speed / (double)dist);
                        if (x == here.X && y == here.Y) continue;
                        where[side][slot.Id] = new FieldPoint(x, y);
                        events.Add(new BattleEvent { Kind = BattleEventKind.Move, Tick = tick, At = new SlotRef((Side)side, slot.Id), X = x, Y = y });
                    }
                }

                // Then the blows, attacker first, each at whatever is its target now and within reach.
                for (var side = 0; side < 2; side++)
                {
                    var foe = 1 - side;
                    foreach (var slot in sides[side].Slots)
                    {
                        if (slot.HpPool <= 0) continue;
                        var target = PickTarget(slot, where[side][slot.Id], sides[foe], where[foe]);
                        if (target == null) break;
                        var range = _units.Get(slot.Type).Range;
                        // Its countdown runs while it walks; a slot that is ready holds its blow until it arrives.
                        if (ready[side][slot.Id] > 0) ready[side][slot.Id] -= 1;
                        if (ready[side][slot.Id] <= 0 && Dist2(where[side][slot.Id], where[foe][target.Id]) <= (long)range * range)
                        {
                            ready[side][slot.Id] = slot.Cooldown;
                            var hits = Math.Min(slot.Alive, slot.Frontage);
                            if (Land(tick, side, slot, target, hits, slot.Dmg, null)) return Finish(tick, (Side)side, EndReason.Wiped);
                        }

                        var skill = slot.Skill;
                        if (skill == null || skill.Every <= 0) continue;
                        skillReady[side][slot.Id] -= 1;
                        if (skillReady[side][slot.Id] > 0) continue;
                        skillReady[side][slot.Id] = skill.Every;
                        if (Fire(tick, side, slot, skill)) return Finish(tick, (Side)side, EndReason.Wiped);
                    }
                }
            }

            // Out of clock: the defender holds the ground it stood on.
            return Finish(_settings.TimeoutTicks, Side.Theirs, EndReason.Timeout);
        }

        // What is left of each slot of a side when the dust settles: the losses the caller charges are count − alive.
        public static Dictionary<int, int> Survivors(BattleLog log, Side side)
        {
            var output = new Dictionary<int, int>();
            var start = log.Events[0];
            foreach (var (slot, _) in side == Side.Ours ? start.Ours : start.Theirs) output[slot.Id] = slot.Count;
            foreach (var e in log.Events)
            {
                if (e.At.Side != side) continue;
                if (e.Kind is BattleEventKind.TroopsLost or BattleEventKind.Healed) output[e.At.Id] = e.Alive;
                if (e.Kind == BattleEventKind.SlotWiped) output[e.At.Id] = 0;
            }

            return output;
        }

        // Each slot's health when the dust settles: a hero carries it into its next fight.
        public static Dictionary<int, int> PoolsAfter(BattleLog log, Side side)
        {
            var output = new Dictionary<int, int>();
            var start = log.Events[0];
            foreach (var (slot, _) in side == Side.Ours ? start.Ours : start.Theirs) output[slot.Id] = slot.HpPool;
            foreach (var e in log.Events)
            {
                if (e.Kind == BattleEventKind.Attack && e.At.Side == side) output[e.At.Id] = Math.Max(0, output[e.At.Id] - e.Dealt);
                if (e.Kind == BattleEventKind.Healed && e.At.Side == side) output[e.At.Id] = e.HpPool;
            }

            return output;
        }

        // The bar at the top of the screen as it stood before the first blow.
        public static int BoardPower(Board board) => board.Slots.Sum(s => s.Power * s.Count);

        public int FormationPower(IEnumerable<SquadSpec> squads) => squads.Sum(s => RankOf(s.Troop).Power * s.Count);

        // JavaScript's Math.round: halves go up, toward +∞.
        public static int JsRound(double x) => (int)Math.Floor(x + 0.5);

        private static Board Copy(Board board) => new(board.Slots.Select(s => s.Clone()).ToList());

        private static int MaxPool(BoardSlot s) => s.IsHero ? s.HpUnit : s.Count * s.HpUnit;

        private static IEnumerable<BoardSlot> Living(Board board) => board.Slots.Where(s => s.HpPool > 0);

        private static long Dist2(FieldPoint a, FieldPoint b) => (long)(a.X - b.X) * (a.X - b.X) + (long)(a.Y - b.Y) * (a.Y - b.Y);

        // The whole square root, exact: Math.Sqrt is only the first guess.
        private static long Isqrt(long n)
        {
            var r = (long)Math.Floor(Math.Sqrt(n));
            while (r * r > n) r -= 1;
            while ((r + 1) * (r + 1) <= n) r += 1;
            return r;
        }

        private (int Num, int Den) Fraction(string attacker, string target)
        {
            if (Beats(attacker) == target) return (_settings.TypeAdvantageNum, _settings.TypeAdvantageDen);
            if (Beats(target) == attacker) return (_settings.TypeDisadvantageNum, _settings.TypeDisadvantageDen);
            return (1, 1);
        }

        // Who a slot goes for, fresh every tick (§8); ties to the lowest slot id.
        private BoardSlot PickTarget(BoardSlot attacker, FieldPoint here, Board enemy, FieldPoint[] where)
        {
            var candidates = Living(enemy).ToList();
            if (candidates.Count == 0) return null;
            BoardSlot Nearest(List<BoardSlot> list) => list.Aggregate((best, s) =>
            {
                var d = Dist2(here, where[s.Id]);
                var b = Dist2(here, where[best.Id]);
                return d < b || (d == b && s.Id < best.Id) ? s : best;
            });

            switch (TargetingOf(attacker.Type))
            {
                case Targeting.Ranged:
                {
                    // The weakest thing it can reach; nothing in reach, the nearest.
                    var range = (long)_units.Get(attacker.Type).Range;
                    var near = candidates.Where(s => Dist2(here, where[s.Id]) <= range * range).ToList();
                    return near.Count == 0 ? Nearest(candidates)
                        : near.Aggregate((best, s) => s.HpPool < best.HpPool || (s.HpPool == best.HpPool && s.Id < best.Id) ? s : best);
                }
                case Targeting.Flanker:
                {
                    var back = candidates.Where(s => s.Row == Row.Back).ToList();
                    return Nearest(back.Count > 0 ? back : candidates);
                }
                default:
                {
                    var front = candidates.Where(s => s.Row == Row.Front).ToList();
                    return Nearest(front.Count > 0 ? front : candidates);
                }
            }
        }
    }
}
