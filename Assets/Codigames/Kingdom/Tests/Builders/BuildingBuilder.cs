using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;

namespace Codigames.Kingdom.Tests.Builders
{
    // A small building for a test, legal unless told otherwise: "a farm of 3 levels at 10 Gold a level".
    public class BuildingBuilder
    {
        private string _id = "Farm";
        private int _maxLevel = 3;
        private int _width = 1;
        private int _height = 1;
        private List<ILevelCost> _perLevel;
        private double _linear = 2;
        private double _exponential = 1.2;
        private double _buildSeconds = 10;
        private readonly List<int> _maxCount = new();
        private readonly List<int> _townhallGates = new();

        public BuildingBuilder WithId(string id) { _id = id; return this; }
        public BuildingBuilder WithMaxLevel(int maxLevel) { _maxLevel = maxLevel; return this; }
        public BuildingBuilder WithSize(int width, int height) { _width = width; _height = height; return this; }
        public BuildingBuilder WithLevelPrices(params ILevelCost[] prices) { _perLevel = prices.ToList(); return this; }
        public BuildingBuilder WithInstanceGrowth(double linear, double exponential) { _linear = linear; _exponential = exponential; return this; }
        public BuildingBuilder WithBuildSeconds(double seconds) { _buildSeconds = seconds; return this; }
        public BuildingBuilder WithMaxCountPerTownhallLevel(params int[] caps) { _maxCount.AddRange(caps); return this; }
        public BuildingBuilder WithTownhallGates(params int[] levels) { _townhallGates.AddRange(levels); return this; }

        public static ILevelCost Price(string currency, double amount)
            => new LevelCost(new Dictionary<string, double> { [currency] = amount }, new Dictionary<string, double>());

        public IBuildingDefinition Build() => new Building
        {
            Id = _id,
            MaxLevel = _maxLevel,
            Width = _width,
            Height = _height,
            Buildable = true,
            Cost = new Cost(_perLevel ?? Enumerable.Range(0, _maxLevel).Select(_ => Price("Gold", 10)).ToList(), _linear, _exponential),
            Duration = new Duration { BuildSeconds = _buildSeconds },
            Gates = new Gates(_maxCount, _townhallGates, new List<int>()),
        };

        private sealed class Building : IBuildingDefinition
        {
            public string Id { get; set; }
            public int MaxLevel { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool Buildable { get; set; }
            public IBuildingCost Cost { get; set; }
            public IBuildingDuration Duration { get; set; }
            public IBuildingGates Gates { get; set; }
        }

        private sealed class LevelCost : ILevelCost
        {
            public LevelCost(IReadOnlyDictionary<string, double> currencies, IReadOnlyDictionary<string, double> goods)
            {
                Currencies = currencies;
                Goods = goods;
            }

            public IReadOnlyDictionary<string, double> Currencies { get; }
            public IReadOnlyDictionary<string, double> Goods { get; }
        }

        private sealed class Cost : IBuildingCost
        {
            public Cost(IReadOnlyList<ILevelCost> perLevel, double linear, double exponential)
            {
                PerLevel = perLevel;
                InstanceLinearGrowth = linear;
                InstanceExponentialGrowth = exponential;
            }

            public IReadOnlyList<ILevelCost> PerLevel { get; }
            public double InstanceLinearGrowth { get; }
            public double InstanceExponentialGrowth { get; }
        }

        private sealed class Duration : IBuildingDuration
        {
            public double BuildSeconds { get; set; }
            public double BuildCountGrowth { get; set; } = 1;
            public double BuildDistanceGrowth { get; set; } = 1;
            public double UpgradeSeconds { get; set; } = 10;
            public double UpgradeLevelGrowth { get; set; } = 1.5;
            public double LateUpgradeSeconds { get; set; }
            public double LateUpgradeLevelGrowth { get; set; } = 1;
        }

        private sealed class Gates : IBuildingGates
        {
            public Gates(IReadOnlyList<int> maxCount, IReadOnlyList<int> townhall, IReadOnlyList<int> population)
            {
                MaxCountPerTownhallLevel = maxCount;
                RequiredTownhallLevelPerLevel = townhall;
                RequiredPopulationPerLevel = population;
            }

            public IReadOnlyList<int> MaxCountPerTownhallLevel { get; }
            public IReadOnlyList<int> RequiredTownhallLevelPerLevel { get; }
            public IReadOnlyList<int> RequiredPopulationPerLevel { get; }
        }
    }
}
