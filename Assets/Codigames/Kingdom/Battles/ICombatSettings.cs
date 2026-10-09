namespace Codigames.Kingdom.Battles
{
    // The fight's dials (Docs/features/combat.md §17).
    public interface ICombatSettings
    {
        // One tick of the fight, in milliseconds: what a replay spends on it.
        int TickMs { get; }
        // The Attack/Defence step and its caps, per mille (§7).
        int AttackStepPerMille { get; }
        int AttackCapPerMille { get; }
        int DefenceStepPerMille { get; }
        int DefenceCapPerMille { get; }
        // A fight that runs this long is the defender's (§10).
        int TimeoutTicks { get; }
        // The field (§3): the front rows' gap, the pitch between lines and between slots.
        int FieldGap { get; }
        int FieldRowPitch { get; }
        int FieldColPitch { get; }
        // The type chart's fractions, as integer pairs (§7).
        int TypeAdvantageNum { get; }
        int TypeAdvantageDen { get; }
        int TypeDisadvantageNum { get; }
        int TypeDisadvantageDen { get; }
        // The generator (§11): how many squads, and when and how villains are fielded.
        int GenSlotsMin { get; }
        int GenSlotsMax { get; }
        int GenVillainThreshold { get; }
        double GenVillainShare { get; }
        int GenVillainSlots { get; }
        // What a hero is worth in the power estimate, per point of damage (§12).
        double HeroPowerPerDmg { get; }
        // What an attack charges on the way in.
        double FightMana { get; }
    }
}
