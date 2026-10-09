using System.Collections.Generic;

namespace Codigames.Kingdom.Modifiers
{
    public enum ModifierOp
    {
        Add,
        Mul,
    }

    // One term in the kingdom's modifier stack: what moves a number at its base stage beside the tree — a Legendary's
    // boon, a relic, an event. On while its source says so, until it expires.
    public sealed class Modifier
    {
        public Modifier(string id, string stat, ModifierOp op, double value, string scope = null, double? expiresAt = null)
        {
            Id = id;
            Stat = stat;
            Op = op;
            Value = value;
            Scope = scope;
            ExpiresAt = expiresAt;
        }

        public string Id { get; }
        public string Stat { get; }
        public ModifierOp Op { get; }
        public double Value { get; }
        // What it applies to (a currency, a source, a building); null for everything.
        public string Scope { get; }
        // Epoch milliseconds; null while its source stands.
        public double? ExpiresAt { get; }
    }

    // Port: something that holds modifiers — the heroes owned, the relics, the events.
    public interface IModifierSource
    {
        IEnumerable<Modifier> Modifiers { get; }
    }

    // Port: a number at its base stage after every modifier on it.
    public interface IModifiers
    {
        double Resolve(string stat, double value, string scope = null);
    }

    public static class ModifiersExtensions
    {
        // Null-safe: with no stack, the number as it was.
        public static double Apply(this IModifiers modifiers, string stat, double value, string scope = null)
            => modifiers?.Resolve(stat, value, scope) ?? value;
    }
}
