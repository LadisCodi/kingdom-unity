using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Relics.State;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Relics
{
    public enum ActivateBlock
    {
        None,
        NotRestored,
        NotACityRelic,
        NotHosted,
        Active,
        NotEnoughMana,
    }

    public enum HostResult
    {
        Hosted,
        NotRestored,
        NotACityRelic,
        NotAShrine,
    }

    // A RELIC'S HOST (the web's hosts.ts): a restored city relic does nothing until a Shrine holds it and the player
    // activates it — Mana paid, a window opens, and for the window its number reaches every cell of the Shrine's aura,
    // the footprint and the relic's reach round it (Chebyshev). Where one relic's auras overlap it counts once; two
    // relics on one number multiply. One relic, one host: hosting it elsewhere closes the window it had. The window is
    // state, closed at its own boundary on the timeline, so an absence replayed ends it where it ended.
    public class Shrines : ITimedSystem, IRelicAura
    {
        private const string ACTIVE_COST = "activeCost";

        private readonly RelicsState _state;
        private readonly Relics _relics;
        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly ManaPool _mana;
        private readonly Func<Modifiers.IModifiers> _modifiers;

        public Shrines(RelicsState state, Relics relics, CityState city, ICatalog<IBuildingDefinition> buildings, ManaPool mana,
            Func<Modifiers.IModifiers> modifiers = null)
        {
            _state = state;
            _relics = relics;
            _city = city;
            _buildings = buildings;
            _mana = mana;
            _modifiers = modifiers;
        }

        // A relic was hosted, unhosted, activated, or its window closed.
        public event Action Changed;

        // A relic's window closed at its own moment, live or in an absence replayed: it is asleep.
        public event Action<string> WindowClosed;

        // Every Shrine standing, built.
        public IEnumerable<DistrictState> All => _city.Districts.Where(d => d.Built && _buildings.Get(d.DefinitionId).HostsRelic);

        public string Hosted(DistrictState shrine) => _state.Hosts.TryGetValue(shrine.Id, out var relic) ? relic : null;

        public DistrictState HostOf(string relic)
        {
            var id = _state.Hosts.FirstOrDefault(h => h.Value == relic).Key;
            return id == null ? null : _city.Districts.FirstOrDefault(d => d.Id == id);
        }

        public bool IsAwake(string relic) => _relics.Get(relic).Kind == RelicKind.City && _state.Windows.ContainsKey(relic);

        public double? WindowEndsAt(string relic) => _state.Windows.TryGetValue(relic, out var at) ? at : null;

        // ---- hosting

        // Free, and undone the same way: the relic leaves where it was, and one the Shrine held goes back to the Bag.
        public HostResult Host(string relic, string shrineId)
        {
            if (!_relics.IsRestored(relic)) return HostResult.NotRestored;
            if (_relics.Get(relic).Kind != RelicKind.City) return HostResult.NotACityRelic;
            var shrine = All.FirstOrDefault(d => d.Id == shrineId);
            if (shrine == null) return HostResult.NotAShrine;
            var was = HostOf(relic);
            if (was != null && was != shrine)
            {
                _state.Windows.Remove(relic);
                _state.Hosts.Remove(was.Id);
            }

            if (Hosted(shrine) is { } held && held != relic) _state.Windows.Remove(held);
            _state.Hosts[shrine.Id] = relic;
            Changed?.Invoke();
            return HostResult.Hosted;
        }

        public bool Unhost(string relic)
        {
            var host = HostOf(relic);
            if (host == null) return false;
            _state.Windows.Remove(relic);
            _state.Hosts.Remove(host.Id);
            Changed?.Invoke();
            return true;
        }

        // ---- activating

        // What waking it costs now: its Mana, bought down by whatever lowers an activation's cost.
        public int ActivationCost(string relic)
        {
            var base_ = _relics.Get(relic).ActivationMana;
            var cost = _modifiers?.Invoke() is { } modifiers ? Modifiers.ModifiersExtensions.Apply(modifiers, ACTIVE_COST, base_) : base_;
            return Math.Max(0, (int)Math.Round(cost, MidpointRounding.AwayFromZero));
        }

        public ActivateBlock Block(string relic)
        {
            if (!_relics.IsRestored(relic)) return ActivateBlock.NotRestored;
            if (_relics.Get(relic).Kind != RelicKind.City || _relics.Get(relic).ActivationMana <= 0) return ActivateBlock.NotACityRelic;
            var host = HostOf(relic);
            if (host == null || !host.Built) return ActivateBlock.NotHosted;
            if (IsAwake(relic)) return ActivateBlock.Active;
            if (_mana.Amount < ActivationCost(relic)) return ActivateBlock.NotEnoughMana;
            return ActivateBlock.None;
        }

        // Pay its Mana and its number reaches the aura for its level's window, priced now: a level-up mid-window does
        // not stretch it. No cooldown.
        public ActivateBlock Activate(string relic, double now)
        {
            var block = Block(relic);
            if (block != ActivateBlock.None) return block;
            if (!_mana.TrySpend(ActivationCost(relic), now)) return ActivateBlock.NotEnoughMana;
            _state.Windows[relic] = now + _relics.WindowMs(relic, _relics.Level(relic));
            Changed?.Invoke();
            return ActivateBlock.None;
        }

        // ---- the aura

        public int Radius(string relic) => _relics.Radius(relic, _relics.Level(relic));

        // Does the relic's aura round this Shrine reach the cell?
        public bool Covers(DistrictState shrine, string relic, Vector2Int cell)
        {
            var building = _buildings.Get(shrine.DefinitionId);
            return GridMath.ChebyshevToRect(cell, shrine.Anchor, building.Width, building.Height) <= Radius(relic);
        }

        public double At(string stat, Vector2Int cell)
        {
            var mul = 1.0;
            var counted = new HashSet<string>();
            foreach (var shrine in All)
            {
                var relic = Hosted(shrine);
                if (relic == null || counted.Contains(relic) || !IsAwake(relic)) continue;
                var definition = _relics.Get(relic);
                var entries = definition.Stats.Where(s => s.Stat == stat).ToList();
                if (entries.Count == 0 || !Covers(shrine, relic, cell)) continue;
                counted.Add(relic);
                var value = _relics.Value(relic);
                // Every city relic's number is a multiplier; one that adds has no aura to act in.
                foreach (var e in entries.Where(e => !e.Add)) mul *= value;
            }

            return mul;
        }

        // A building reaching into an aura is in it: the strongest any of its cells stands in.
        public double Over(string stat, DistrictState district)
        {
            var building = _buildings.Get(district.DefinitionId);
            var best = 1.0;
            for (var dy = 0; dy < building.Height; dy++)
            for (var dx = 0; dx < building.Width; dx++)
                best = Math.Max(best, At(stat, new Vector2Int(district.Anchor.X + dx, district.Anchor.Y + dy)));
            return best;
        }

        // ---- the timeline: a window ends at its own moment

        public double? NextBoundary(double after)
        {
            double? best = null;
            foreach (var end in _state.Windows.Values)
                if (end > after && (best == null || end < best)) best = end;
            return best;
        }

        public void ApplyDue(double time)
        {
            var closed = _state.Windows.Where(w => w.Value <= time).Select(w => w.Key).ToList();
            foreach (var relic in closed) _state.Windows.Remove(relic);
            foreach (var relic in closed) WindowClosed?.Invoke(relic);
            if (closed.Count > 0) Changed?.Invoke();
        }

        public void RunUntil(double time) { }
    }
}
