using System.Collections.Generic;
using Codigames.Kingdom.Research;

namespace Codigames.Kingdom.Tests.Builders
{
    // A technology for a test: placed in the Kingdom's first band, 2 Knowledge and 20 Gold, unless told otherwise.
    public class TechBuilder
    {
        private readonly FakeTechnology _tech = new();

        public TechBuilder(string id)
        {
            _tech.Id = id;
        }

        public TechBuilder In(string tome, int era, int row = 0, int column = 0)
        {
            _tech.Tome = tome;
            _tech.Era = era;
            _tech.Row = row;
            _tech.Column = column;
            return this;
        }

        public TechBuilder Unplaced()
        {
            _tech.IsPlaced = false;
            return this;
        }

        public TechBuilder Requires(params string[] ids)
        {
            _tech.Requires = ids;
            return this;
        }

        public TechBuilder Costs(double knowledge, double gold)
        {
            _tech.Knowledge = knowledge;
            _tech.Price = new Dictionary<string, double> { ["Gold"] = gold };
            return this;
        }

        public TechBuilder Opens(UnlockKind kind, string id, int level = 0)
        {
            _tech.Kind = TechKind.Unlock;
            _tech.UnlockList.Add(new TechUnlock(kind, id, level));
            return this;
        }

        public TechBuilder Moves(string stat, EffectOp op, double value, TargetKind target = TargetKind.Global, string targetId = null)
        {
            _tech.Kind = TechKind.Bonus;
            _tech.EffectList.Add(new TechEffect(stat, op, value, target, targetId));
            return this;
        }

        public ITechnology Build() => _tech;

        private sealed class FakeTechnology : ITechnology
        {
            public string Id { get; set; }
            public string Tome { get; set; } = "Kingdom";
            public int Era { get; set; } = 1;
            public int Row { get; set; }
            public int Column { get; set; }
            public bool IsPlaced { get; set; } = true;
            public IReadOnlyList<string> Requires { get; set; } = new string[0];
            public double Knowledge { get; set; } = 2;
            public IReadOnlyDictionary<string, double> Price { get; set; } = new Dictionary<string, double> { ["Gold"] = 20 };
            public TechKind Kind { get; set; } = TechKind.Mechanic;
            public List<TechUnlock> UnlockList { get; } = new();
            public List<TechEffect> EffectList { get; } = new();
            public IReadOnlyList<TechUnlock> Unlocks => UnlockList;
            public IReadOnlyList<TechEffect> Effects => EffectList;
            public bool Planned => false;
        }
    }
}
