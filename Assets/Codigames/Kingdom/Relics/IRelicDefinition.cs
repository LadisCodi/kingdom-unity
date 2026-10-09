using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Relics
{
    // A city relic acts from a Shrine while activated; a world relic from a Chapel (not built yet).
    public enum RelicKind
    {
        City,
        World,
    }

    // What a city relic's level-up raises, round a cycle: its window, its reach, its number.
    public enum RelicAxis
    {
        Window,
        Radius,
        Effect,
    }

    // One number a relic moves, and whether its value adds to it or multiplies it.
    public readonly struct RelicStat
    {
        public RelicStat(string stat, bool add)
        {
            Stat = stat;
            Add = add;
        }

        public string Stat { get; }
        public bool Add { get; }
    }

    // A relic (Docs/proposals/relic-restoration.md): city or world, the door its first fragment is found at, the
    // numbers it moves with one value — base at level 1, and what each effect step adds — and a city relic's
    // activation: its Mana and its aura's reach at level 1. Pending: its system is not in the game yet.
    public interface IRelicDefinition : IIdentifiable
    {
        RelicKind Kind { get; }
        string Door { get; }
        IReadOnlyList<RelicStat> Stats { get; }
        double PassiveBase { get; }
        double PassivePerLevel { get; }
        // A city relic's activation; 0 Mana on a world relic.
        int ActivationMana { get; }
        int ActivationRadius { get; }
        bool Pending { get; }
    }
}
