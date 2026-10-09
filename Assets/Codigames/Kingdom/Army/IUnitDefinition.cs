using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Army
{
    // A unit (Docs/features/combat.md §5–§6): what a squad of it is in a fight, and its ranks — I its own numbers,
    // II to V its evolutions, each dearer, slower to train and asking a higher hall.
    public interface IUnitDefinition : IIdentifiable
    {
        string Name { get; }
        string Description { get; }
        // Melee, Distance, Mounted.
        IReadOnlyList<string> Tags { get; }
        int SquadSize { get; }
        int Frontage { get; }
        int Cooldown { get; }
        int Speed { get; }
        int Range { get; }
        // Rank I first.
        IReadOnlyList<UnitRank> Ranks { get; }
    }

    public sealed class UnitRank
    {
        public UnitRank(int atk, int dmg, int def, int hp, int power, IReadOnlyDictionary<string, double> cost, double trainSeconds,
            int minHallLevel)
        {
            Atk = atk;
            Dmg = dmg;
            Def = def;
            Hp = hp;
            Power = power;
            Cost = cost;
            TrainSeconds = trainSeconds;
            MinHallLevel = minHallLevel;
        }

        public int Atk { get; }
        public int Dmg { get; }
        public int Def { get; }
        public int Hp { get; }
        public int Power { get; }
        public IReadOnlyDictionary<string, double> Cost { get; }
        public double TrainSeconds { get; }
        public int MinHallLevel { get; }
    }
}
