using System;

namespace Codigames.Kingdom.Tutorial
{
    // A condition: a kind, what it names (a quest, a building, a screen) and how many (0 reads as one).
    public readonly struct Condition
    {
        public Condition(ConditionKind kind, string target = "", double amount = 0)
        {
            Kind = kind;
            Target = target ?? "";
            Amount = amount;
        }

        public ConditionKind Kind { get; }
        public string Target { get; }
        public double Amount { get; }

        // At least one: an amount of 0 asks for one.
        public double AtLeast(double floor = 1) => Math.Max(floor, Amount);

        public bool IsSet => Kind != ConditionKind.Unknown;

        // The web's camelCase name ("questReached"); an unknown or empty name is Unknown.
        public static ConditionKind Parse(string name)
            => !string.IsNullOrEmpty(name) && Enum.TryParse<ConditionKind>(name, true, out var kind) ? kind : ConditionKind.Unknown;
    }
}
