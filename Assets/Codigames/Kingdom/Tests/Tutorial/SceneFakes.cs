using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Doors;
using Codigames.Kingdom.Tutorial;

namespace Codigames.Kingdom.Tests.Tutorial
{
    internal sealed class Line : ISceneLine
    {
        public string Speaker { get; set; } = "advisor";
        public StageSide Side { get; set; }
        public string Text { get; set; } = "Hello.";
        public string Expression { get; set; } = "";
        public string Box { get; set; } = "auto";
        public string Point { get; set; } = "";
        public LineLock Lock { get; set; }
        public Condition Until { get; set; } = new(ConditionKind.Tap);
        public bool Exit { get; set; }
        public string Stocks { get; set; } = "";

        public static Line Tap() => new();

        public static Line Waiting(ConditionKind kind, string target = "") => new() { Until = new Condition(kind, target) };
    }

    internal sealed class Scene : ISceneDefinition
    {
        public string Id { get; set; }
        public Condition Trigger { get; set; } = new(ConditionKind.Always);
        public bool Anywhere { get; set; }
        public bool Skippable { get; set; } = true;
        public SceneWhere Where { get; set; }
        public Condition DoneWhen { get; set; }
        public IReadOnlyList<ISceneLine> Lines { get; set; } = new ISceneLine[] { Line.Tap() };
    }

    // Conditions set by hand: a fact is true when its kind and target are in the set.
    internal sealed class FakeConditions : IConditions
    {
        public HashSet<(ConditionKind, string)> True { get; } = new();

        public bool Holds(Condition condition, int tapsAtStart = 0)
            => condition.Kind == ConditionKind.Always || True.Contains((condition.Kind, condition.Target));
    }

    internal sealed class FakePurse : IScenePurse
    {
        public HashSet<string> Unaffordable { get; } = new();
        public HashSet<string> Affordable { get; } = new();
        public List<string> StockedBuildings { get; } = new();

        public bool CanPayFor(ISceneDefinition scene) => !Unaffordable.Contains(scene.Id);

        public bool Needless(ISceneLine line) => !string.IsNullOrEmpty(line.Stocks) && Affordable.Contains(line.Stocks);

        public IReadOnlyDictionary<string, double> Stock(string building)
        {
            StockedBuildings.Add(building);
            return new Dictionary<string, double> { ["Gold"] = 10 };
        }
    }

    internal sealed class FakeMorning : IMorning
    {
        public bool FirstMorningOn { get; set; }
    }

    internal sealed class FakeContext : IStageContext
    {
        public bool HeldBack { get; set; }
        public bool OnWorld { get; set; }
        public bool HasOpenSheet { get; set; }
    }

    internal static class Scenes
    {
        public static IReadOnlyList<ISceneDefinition> Of(params Scene[] scenes) => scenes.Cast<ISceneDefinition>().ToList();
    }
}
