using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Army;
using Codigames.Kingdom.Battles;
using Codigames.Kingdom.Lairs;
using Codigames.Modules.Core;

namespace Codigames.Game.Lairs
{
    // The party on the attack sheet (Docs/proposals/lairs.md §6): squads in the six troop slots, drawn from the army at
    // home, and the heroes leading it. The player never picks a slot: a tap on a troop sends one squad — as big as the unit's squad, or all of it
    // left — into the next free slot; a tap on a slot sends it home. A screen's state, not the kingdom's.
    public class AttackParty
    {
        private readonly Army _army;
        private readonly Combat _combat;
        private readonly LairAttack _attack;
        private readonly ICatalog<IUnitDefinition> _units;
        private readonly List<SquadSpec> _slots = new();
        private readonly List<string> _heroes = new();

        public AttackParty(Army army, Combat combat, LairAttack attack, ICatalog<IUnitDefinition> units)
        {
            _army = army;
            _combat = combat;
            _attack = attack;
            _units = units;
        }

        public IReadOnlyList<SquadSpec> Slots => _slots;

        // The heroes leading the party, in their slots' order.
        public IReadOnlyList<string> Heroes => _heroes;

        public void SetHeroes(IEnumerable<string> heroes)
        {
            _heroes.Clear();
            _heroes.AddRange(heroes);
        }

        public int SlotCount => _attack.TroopSlots;

        // Every unit's rank I, always — a type with none left still shows, saying so — and a higher rank once the
        // kingdom has any of it.
        public IReadOnlyList<string> Roster => _units.Items
            .SelectMany(u => Enumerable.Range(1, u.Ranks.Count).Select(r => Troops.Of(u.Id, r)))
            .Where(t => Troops.RankOf(t) == 1 || _army.Count(t) > 0).ToList();

        public int LeftAtHome(string troop) => Math.Max(0, _army.Count(troop) - _slots.Where(s => s.Troop == troop).Sum(s => s.Count));

        public int AvailableFor(string troop) => Math.Max(0, Math.Min(_units.Get(Troops.UnitOf(troop)).SquadSize, LeftAtHome(troop)));

        public bool IsFull => _slots.Count >= SlotCount;

        // One tap on a troop: one more squad of it. False when there are none left or every slot is full.
        public bool Assign(string troop)
        {
            if (AvailableFor(troop) <= 0 || IsFull) return false;
            _slots.Add(new SquadSpec(troop, AvailableFor(troop)));
            return true;
        }

        public void Clear(int index)
        {
            if (index >= 0 && index < _slots.Count) _slots.RemoveAt(index);
        }

        // The strongest legal party, answering the lair's creature first: squad after squad of the best-answering type
        // that still has soldiers at home, until the slots or the army run out.
        public void QuickDeploy(string threat)
        {
            _slots.Clear();
            var order = _units.Items.SelectMany(u => Enumerable.Range(1, u.Ranks.Count).Select(r => Troops.Of(u.Id, r)))
                .OrderByDescending(t => Score(t, threat)).ToList();
            while (true)
            {
                var next = order.FirstOrDefault(t => AvailableFor(t) > 0 && !IsFull);
                if (next == null) return;
                _slots.Add(new SquadSpec(next, AvailableFor(next)));
            }
        }

        // A fight kills soldiers: the squads in the slots can outrun what is left at home. The army changed, not the
        // player's mind, so the slots are clamped rather than refused.
        public void Reconcile()
        {
            var seen = new Dictionary<string, int>();
            for (var i = 0; i < _slots.Count; i++)
            {
                var troop = _slots[i].Troop;
                seen.TryGetValue(troop, out var used);
                var count = Math.Min(_slots[i].Count, Math.Max(0, _army.Count(troop) - used));
                seen[troop] = used + count;
                _slots[i] = new SquadSpec(troop, count);
            }

            _slots.RemoveAll(s => s.Count <= 0);
        }

        // Soldiers the fight would cost the party as it stands: the same fight the attack would run, on the same boards.
        public int Fallen(ILairSite lair)
        {
            var committed = _slots.Where(s => s.Count > 0).ToList();
            if (committed.Count == 0) return 0;
            var ours = _attack.PartyBoard(committed, _heroes);
            var log = _combat.Resolve(ours, _attack.Board(lair));
            var left = Combat.Survivors(log, Side.Ours);
            return ours.Slots.Where(s => !s.IsHero).Sum(s => s.Count - (left.TryGetValue(s.Id, out var alive) ? alive : s.Count));
        }

        // How well a troop answers a lair's threat — only to pre-fill a sensible party, never to decide anything.
        private double Score(string troop, string threat)
        {
            var unit = Troops.UnitOf(troop);
            var settings = _combat.Settings;
            var type = threat == "Any" ? 1
                : Combat.Beats(unit) == threat ? settings.TypeAdvantageNum / (double)settings.TypeAdvantageDen
                : Combat.Beats(threat) == unit ? settings.TypeDisadvantageNum / (double)settings.TypeDisadvantageDen : 1;
            return type * _combat.RankOf(troop).Dmg;
        }
    }
}
