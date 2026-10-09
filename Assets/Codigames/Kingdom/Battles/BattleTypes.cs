using System;
using System.Collections.Generic;

namespace Codigames.Kingdom.Battles
{
    public enum Side
    {
        Ours,
        Theirs,
    }

    public enum Row
    {
        Front,
        Back,
    }

    // How a slot chooses what to hit (§8): derived from the unit's tags — a Distance unit shoots, a Mounted one flanks.
    public enum Targeting
    {
        Melee,
        Ranged,
        Flanker,
    }

    // A hero's or a villain's skill resolved to its rank (§9.3): a share as per-mille, a time as ticks; how often it
    // fires, 0 for a rally or a spoil.
    public sealed class SlotSkill
    {
        public SlotSkill(string id, int amount, int every)
        {
            Id = id;
            Amount = amount;
            Every = every;
        }

        public string Id { get; }
        public int Amount { get; }
        public int Every { get; }
    }

    // One slot on the board, resolved: the passives already applied, so nothing in the tick loop asks who else stands
    // on its side. Its id is stable within its side, and what every event names.
    public sealed class BoardSlot
    {
        public int Id;
        public bool IsHero;
        public Row Row;
        // What it fights as on the type chart: a unit.
        public string Type;
        // The troop it is (unit and rank), for the screen; null for a hero.
        public string Troop;
        public string FighterId;
        public string Name;
        public int Count;
        public int Frontage;
        public int Atk;
        // A hero's damage and health are its level's, unrounded, as the web's are.
        public double Dmg;
        // A rating the research may leave fractional; the Attack/Defence rule rounds it.
        public double Def;
        public double HpUnit;
        public double HpPool;
        public int Cooldown;
        public int Power;
        public SlotSkill Skill;

        public int Alive => (int)Math.Ceiling(HpPool / HpUnit);

        public BoardSlot Clone() => (BoardSlot)MemberwiseClone();
    }

    public sealed class Board
    {
        public Board(List<BoardSlot> slots)
        {
            Slots = slots;
        }

        public List<BoardSlot> Slots { get; }
    }

    public readonly struct SlotRef : IEquatable<SlotRef>
    {
        public SlotRef(Side side, int id)
        {
            Side = side;
            Id = id;
        }

        public Side Side { get; }
        public int Id { get; }

        public bool Equals(SlotRef other) => Side == other.Side && Id == other.Id;
        public override bool Equals(object obj) => obj is SlotRef other && Equals(other);
        public override int GetHashCode() => (int)Side * 397 ^ Id;
        public override string ToString() => (Side == Side.Ours ? "ours" : "theirs") + ":" + Id;
    }

    // A point on the field, in field units (§3): x across, y from the middle line — the attacker's rows below it
    // (positive), the defender's above.
    public readonly struct FieldPoint
    {
        public FieldPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }

    public enum BattleEventKind
    {
        Start,
        Move,
        Attack,
        Skill,
        Healed,
        Shielded,
        Dazed,
        TroopsLost,
        SlotWiped,
        End,
    }

    public enum EndReason
    {
        Wiped,
        Timeout,
    }

    // One entry of the stream the screen replays (§13); the fields a kind does not use stay at their defaults.
    public sealed class BattleEvent
    {
        public BattleEventKind Kind;
        public int Tick;
        // Start: both boards as the fight opens, and where each slot stands.
        public IReadOnlyList<(BoardSlot Slot, FieldPoint At)> Ours;
        public IReadOnlyList<(BoardSlot Slot, FieldPoint At)> Theirs;
        // Attack and Skill: who; Attack: on whom. Every other kind names its slot in At.
        public SlotRef From;
        public SlotRef At;
        // Move: where it now stands.
        public int X;
        public int Y;
        // Attack: troops that reached the enemy, what reached its health after any shield, what a shield soaked, and
        // where the type chart stood (+1 an advantage, -1 a disadvantage).
        public int Hits;
        public int Dealt;
        public int Absorbed;
        public int Edge;
        // Attack (a skill's strike) and Skill.
        public string Skill;
        // Healed: how much (whole for the screen; Healed exact); Shielded: the shield now; Dazed: the delay in ticks.
        public int Amount;
        public double Healed;
        // Healed and TroopsLost: what is left.
        public int Alive;
        public double HpPool;
        // End.
        public Side Winner;
        public EndReason Reason;
    }

    public sealed class BattleLog
    {
        public BattleLog(List<BattleEvent> events, Side winner, EndReason reason, int ticks)
        {
            Events = events;
            Winner = winner;
            Reason = reason;
            Ticks = ticks;
        }

        public IReadOnlyList<BattleEvent> Events { get; }
        public Side Winner { get; }
        public EndReason Reason { get; }
        // How long it ran: at the tick's length, also exactly how long its replay lasts.
        public int Ticks { get; }
    }

    // What a hero or a villain brings — one shape for both, since a villain is an enemy hero (§9).
    public sealed class FighterSpec
    {
        public string Id;
        public string Name;
        public string Type;
        public double Atk;
        public double Dmg;
        public double Def;
        public double Hp;
        // The health it starts with when less than Hp: a hero carries its wounds. Null = full.
        public double? HpNow;
        public int Cooldown;
        public int Power;
        public double TroopDmgMult = 1;
        public double TroopHpMult = 1;
        public int TroopDefBonus;
        public SlotSkill Skill;

        public FighterSpec Clone() => (FighterSpec)MemberwiseClone();
    }

    public sealed class SquadSpec
    {
        public SquadSpec(string troop, int count)
        {
            Troop = troop;
            Count = count;
        }

        public string Troop { get; }
        public int Count { get; }
    }

    // Flat bonuses the kingdom's research hands its own troops, resolved upstream; HpMult a multiplier, 1 the identity.
    public sealed class TroopBonus
    {
        public static readonly TroopBonus None = new(_ => 0, _ => 0, _ => 1);

        public TroopBonus(Func<string, double> dmg, Func<string, double> def, Func<string, double> hpMult)
        {
            Dmg = dmg;
            Def = def;
            HpMult = hpMult;
        }

        public Func<string, double> Dmg { get; }
        public Func<string, double> Def { get; }
        public Func<string, double> HpMult { get; }
    }

    // A generated enemy, ready to build a board from.
    public sealed class EnemyPlan
    {
        public EnemyPlan(List<SquadSpec> squads, List<FighterSpec> fighters)
        {
            Squads = squads;
            Fighters = fighters;
        }

        public List<SquadSpec> Squads { get; }
        public List<FighterSpec> Fighters { get; }
    }
}
