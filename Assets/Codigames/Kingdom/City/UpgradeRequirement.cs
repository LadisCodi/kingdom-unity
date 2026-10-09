namespace Codigames.Kingdom.City
{
    // One thing that must be true before a level can be bought, met or not: a Townhall level or a population
    // (`Amount`), or a technology (`Tech`).
    public sealed class UpgradeRequirement
    {
        public UpgradeRequirement(RequirementKind kind, bool met, int amount = 0, string tech = null)
        {
            Kind = kind;
            Met = met;
            Amount = amount;
            Tech = tech;
        }

        public RequirementKind Kind { get; }
        public bool Met { get; }
        public int Amount { get; }
        public string Tech { get; }
    }
}
