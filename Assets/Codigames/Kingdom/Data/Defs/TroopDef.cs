using System.Collections.Generic;

namespace Codigames.Kingdom.Data
{
    // A unit at one rank: rank I is the unit's own row, ranks II–V its evolutions.
    public sealed class TroopDef
    {
        public string Id { get; set; }
        public string Unit { get; set; }
        public int Rank { get; set; }
        public double MinBuildingLevel { get; set; }
        public double Power { get; set; }
        public double Atk { get; set; }
        public double Dmg { get; set; }
        public double Def { get; set; }
        public double Hp { get; set; }
        public IReadOnlyDictionary<string, double> RecruitCost { get; set; }
        public double TrainDurationSeconds { get; set; }
        public string RequiredTech { get; set; }
        public IReadOnlyList<string> Tags { get; set; }
        public double SquadSize { get; set; }
        public double Frontage { get; set; }
        public double Cooldown { get; set; }
        public double Speed { get; set; }
        public double Range { get; set; }
    }
}
