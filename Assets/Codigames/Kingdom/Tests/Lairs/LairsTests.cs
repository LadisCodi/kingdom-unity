using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Lairs;
using Codigames.Kingdom.Lairs.State;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Lairs
{
    // A lair: a garrison with a clock, and the ground it holds (Docs/features/18-garrisons-and-raids.md).
    public class LairsTests
    {
        private const double MINUTE = 60_000;
        private const double HOUR = 60 * MINUTE;
        private const double DAY = 24 * HOUR;
        // Noon UTC, a day in: the offset is 0.
        private const double T0 = DAY + 12 * HOUR;

        private sealed class Settings : IEconomySettings, IWorkerSettings, ILairSettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
            public double MoveSpeedTilesPerSecond => 1;

            public IReadOnlyList<Garrison> Garrisons { get; } = new[]
            {
                new Garrison(3, 300, 500, new Dictionary<string, int> { ["ConstructionSpeedup15m"] = 1 }),
                new Garrison(4, 600, 1500, new Dictionary<string, int>()),
            };

            public double TakeFractionMax => 0.5;
            public int RaidsPerDay => 3;
            public double WindowStartHour => 9;
            public double WindowEndHour => 23;
            public double FirstFightPower => 0.5;
            public double FirstClearKnowledge => 3;
        }

        private sealed class Site : ILairSite
        {
            public string Id { get; set; }
            public string Name => Id;
            public string Description => "";
            public string Flavour => "";
            public Vector2Int Anchor { get; set; }
            public int Size => 2;
            public int Tier { get; set; } = 1;
            public int Radius { get; set; } = 1;
            public int Sight => 3;
            public string Threat => "Warrior";
            public int Power { get; set; } = 60;
            public double WarningMinutes { get; set; } = 30;
            public IReadOnlyDictionary<string, double> Mix { get; set; } = new Dictionary<string, double>();
            public string RollKey { get; set; }
        }

        private sealed class Sites : IProvinceSites
        {
            public IReadOnlyList<IAbandonedSite> Abandoned { get; } = Array.Empty<IAbandonedSite>();
            public IReadOnlyList<ILandmarkSite> Landmarks { get; } = Array.Empty<ILandmarkSite>();
            public IReadOnlyList<ILairSite> Lairs { get; set; }
        }

        // Two houses of 6 paying rent into stores of 3,600, a Townhall making its own Gold, and two lairs in the fog: the
        // Orcs in the far corner (-4, -4), and a second over the trees at (2, 2).
        private sealed class Fixture : HarvestFixture
        {
            public static readonly Site ORCS = new() { Id = "Orcs", Anchor = new Vector2Int(-4, -4) };
            public static readonly Site HARPIES = new() { Id = "Harpies", Anchor = new Vector2Int(2, 2), Tier = 2, Power = 300, WarningMinutes = 90 };

            public Fixture()
            {
                var settings = new Settings();
                foreach (var cell in Map.Cells.Where(c => Zone(ORCS).Contains(c) || Zone(HARPIES).Contains(c))) Revealed.Fogged.Add(cell);
                Timeline.Advance(T0);

                Stand("Housing", new Vector2Int(3, -1));
                Stand("Housing", new Vector2Int(3, -3));
                City.Population = 12;
                Stores = new Stores(City, Buildings, settings, Treasury, Construction);
                Held = new LairGround(new Sites { Lairs = new ILairSite[] { ORCS, HARPIES } }, State, Revealed);
                Crews = new Workforce(City, Buildings, Revealed, Harvesting, Stores, settings, null, Held);
                Lairs = new Codigames.Kingdom.Lairs.Lairs(State, Held, settings, City, Stores, Crews, 42);
                Lairs.Raided += (id, at, took) => Raids.Add((id, at, new Dictionary<string, double>(took)));
                Stores.WakeAll(T0);
                Timeline.Register(Stores);
                Timeline.Register(Lairs);
                Timeline.Register(Crews);
            }

            public LairsState State { get; } = new();
            public LairGround Held { get; }
            public Stores Stores { get; }
            public Workforce Crews { get; }
            public Codigames.Kingdom.Lairs.Lairs Lairs { get; }
            public List<(string Id, double At, Dictionary<string, double> Took)> Raids { get; } = new();

            public static HashSet<Vector2Int> Zone(ILairSite lair) => GridMath.AroundRect(lair.Anchor, lair.Size, lair.Size, lair.Radius).ToHashSet();

            public void Find(ILairSite lair) => Revealed.Fogged.ExceptWith(new[] { lair.Anchor });

            // Its whole zone revealed: found, though not armed until the next advance.
            public void Uncover(ILairSite lair) => Revealed.Fogged.ExceptWith(Zone(lair));

            public double Stored(string currency, double now) => City.Districts.Where(d => !Stores.MakesItsOwn(d)).Sum(d => Stores.HeldOf(d, currency, now));

            protected override IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable()
                    .WithOwnGold(10, 60).WithStorage(600, 4700).Build();

            protected override IBuildingDefinition[] ExtraBuildings() => new[]
            {
                new BuildingBuilder().WithId("Housing").WithMaxLevel(3).WithHousing(6).WithStorage(3600).Build(),
                new BuildingBuilder().WithId("Sawmill").WithCrew("Forest", 2, 2).WithStorage(30).Build(),
            };
        }

        // Every raid time of a lair from `from`, for `days` days.
        private static List<double> Schedule(Codigames.Kingdom.Lairs.Lairs lairs, string id, double from, int days)
        {
            var times = new List<double>();
            for (var t = lairs.RaidTimeAfter(id, from); t < from + days * DAY; t = lairs.RaidTimeAfter(id, t)) times.Add(t);
            return times;
        }

        private static double LocalHour(LairsState state, double t) => ((t + state.UtcOffsetMinutes * MINUTE) % DAY + DAY) % DAY / HOUR;

        private static Fixture Watched()
        {
            var fixture = new Fixture();
            fixture.Find(Fixture.ORCS);
            fixture.Timeline.Advance(T0);
            return fixture;
        }

        [Test]
        public void Finding_ShouldBeRevealingAnyCellOfItsZone_AndStartTheWholeWarning()
        {
            var fixture = new Fixture();
            var corner = Fixture.Zone(Fixture.ORCS).Where(fixture.Map.Contains).Last();
            fixture.Revealed.Fogged.Remove(corner);

            fixture.Timeline.Advance(T0 + 10 * MINUTE);

            var lair = fixture.Lairs.StateOf("Orcs");
            Assert.That(lair.ArmedAt, Is.EqualTo(T0));
            Assert.That(lair.NextRaidAt, Is.EqualTo(T0 + 30 * MINUTE));
            Assert.That(fixture.Lairs.StateOf("Harpies"), Is.Null);
        }

        [Test]
        public void Schedule_ShouldBeThreeRaidsALocalDay_OneInEachSliceOfTheWindow()
        {
            var fixture = Watched();

            var times = Schedule(fixture.Lairs, "Orcs", T0 + 12 * HOUR, 5);

            Assert.That(times.Count, Is.EqualTo(15));
            var slice = (23 - 9) / 3.0;
            for (var i = 0; i < times.Count; i++)
            {
                var hour = LocalHour(fixture.State, times[i]);
                Assert.That(hour, Is.GreaterThanOrEqualTo(9).And.LessThan(23));
                Assert.That((int)Math.Floor((hour - 9) / slice), Is.EqualTo(i % 3));
            }
        }

        [Test]
        public void Schedule_ShouldFollowThePlayersLocalDay()
        {
            var fixture = Watched();
            fixture.State.UtcOffsetMinutes = -8 * 60;

            foreach (var t in Schedule(fixture.Lairs, "Orcs", T0 + DAY, 3))
                Assert.That(LocalHour(fixture.State, t), Is.GreaterThanOrEqualTo(9).And.LessThan(23));
        }

        [Test]
        public void Schedule_ShouldBeAHashOfTheLairAndTheDay()
        {
            var a = Watched();
            var b = Watched();

            Assert.That(Schedule(a.Lairs, "Orcs", T0, 4), Is.EqualTo(Schedule(b.Lairs, "Orcs", T0, 4)));
            Assert.That(Schedule(a.Lairs, "Harpies", T0, 4), Is.Not.EqualTo(Schedule(a.Lairs, "Orcs", T0, 4)));
        }

        [Test]
        public void Raids_ShouldComeAfterTheFirstWarning_ThenKeepComing()
        {
            var fixture = Watched();

            fixture.Timeline.Advance(T0 + 7 * DAY);

            Assert.That(fixture.Raids.Count, Is.GreaterThan(15));
            Assert.That(fixture.Raids[0].At, Is.EqualTo(T0 + 30 * MINUTE));
            Assert.That(fixture.Lairs.StateOf("Orcs").NextRaidAt, Is.Not.Null);
        }

        [Test]
        public void UtcOffset_ShouldMoveTheScheduleFromTheNextSliceOn_AndSpareTheFirstWarning()
        {
            var fixture = Watched();
            var first = T0 + 30 * MINUTE;

            fixture.Lairs.SetUtcOffset(120, T0 + MINUTE);
            Assert.That(fixture.Lairs.StateOf("Orcs").NextRaidAt, Is.EqualTo(first));

            fixture.Timeline.Advance(first + MINUTE);
            fixture.Lairs.SetUtcOffset(-300, first + 2 * MINUTE);

            var moved = fixture.Lairs.StateOf("Orcs").NextRaidAt.Value;
            Assert.That(moved, Is.EqualTo(fixture.Lairs.RaidTimeAfter("Orcs", first + 2 * MINUTE)));
            Assert.That(LocalHour(fixture.State, moved), Is.GreaterThanOrEqualTo(9));
        }

        [Test]
        public void ASavedLairWithNoClock_ShouldGoBackOnTheSchedule()
        {
            var fixture = Watched();
            fixture.State.Lairs["Orcs"].NextRaidAt = null;

            fixture.Lairs.ApplyDue(T0 + DAY);

            Assert.That(fixture.Lairs.StateOf("Orcs").NextRaidAt, Is.EqualTo(fixture.Lairs.RaidTimeAfter("Orcs", T0 + DAY)));
        }

        [Test]
        public void ARaid_ShouldTakeFromTheStores_NeverFromTheTreasury()
        {
            var fixture = Watched();
            var first = T0 + 30 * MINUTE;
            fixture.Timeline.Advance(first - MINUTE);
            var purse = fixture.Treasury.Get("Gold");
            var before = fixture.Stored("Gold", first - MINUTE);

            fixture.Timeline.Advance(first);

            var took = fixture.Raids.Single().Took["Gold"];
            Assert.That(took, Is.GreaterThan(0));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(purse));
            Assert.That(fixture.Stored("Gold", first), Is.LessThan(before + 360 - took + 1));
            Assert.That(fixture.State.Lairs["Orcs"].Hoard["Gold"], Is.EqualTo(took));
        }

        [Test]
        public void ARaid_ShouldTakeSecondsOfTheCitysMaking_CappedAtHalfOfWhatIsStored()
        {
            var fixture = Watched();
            var now = T0 + 20 * MINUTE;
            fixture.Timeline.Advance(now);

            var took = fixture.Lairs.Take(Fixture.ORCS, now);

            // Twelve villagers at 30 a minute: 6 a second, 1,800 in the Orcs' 300 s; the houses hold 20 minutes of it.
            Assert.That(fixture.Lairs.RatePerSecond("Gold", now), Is.EqualTo(6));
            Assert.That(took["Gold"], Is.EqualTo(Math.Min(1800, Math.Floor(fixture.Stored("Gold", now) * 0.5))));
            Assert.That(took.ContainsKey("Wood"), Is.False);
        }

        [Test]
        public void ARaid_ShouldNeverTouchTheTownhallsOwnGold()
        {
            var fixture = Watched();
            var hall = fixture.City.Districts[0];

            fixture.Timeline.Advance(T0 + 3 * DAY);

            Assert.That(fixture.Raids.Count, Is.GreaterThan(0));
            Assert.That(fixture.Stores.Held(hall, T0 + 3 * DAY), Is.EqualTo(fixture.Stores.Capacity(hall)));
            Assert.That(fixture.Raids.SelectMany(r => r.Took.Keys).Distinct(), Is.EquivalentTo(new[] { "Gold" }));
        }

        [Test]
        public void TheHoard_ShouldCarryAtMostADayOfRaids_AndWhatItCannotCarryIsLost()
        {
            var fixture = Watched();
            fixture.Timeline.Advance(T0 + 10 * DAY);
            var lair = fixture.State.Lairs["Orcs"];
            var cap = fixture.Lairs.HoardCap(Fixture.ORCS, "Gold", T0 + 10 * DAY);

            Assert.That(cap, Is.EqualTo(3 * 6 * 300));
            Assert.That(lair.Hoard["Gold"], Is.EqualTo(cap));

            var at = lair.NextRaidAt.Value;
            fixture.Timeline.Advance(at);
            Assert.That(fixture.Raids.Last().At, Is.EqualTo(at));
            Assert.That(fixture.Raids.Last().Took["Gold"], Is.GreaterThan(0));
            Assert.That(lair.Hoard["Gold"], Is.EqualTo(cap));
        }

        [Test]
        public void ARaidThatFindsNothing_ShouldStillMoveItsClockOn()
        {
            var fixture = Watched();
            fixture.City.Population = 0;
            fixture.Stores.SettleAll(T0);
            foreach (var district in fixture.City.Districts) district.Store.Held.Clear();

            fixture.Timeline.Advance(T0 + 2 * DAY);

            Assert.That(fixture.Raids, Is.Empty);
            Assert.That(fixture.State.Lairs["Orcs"].NextRaidAt, Is.GreaterThan(T0 + 2 * DAY));
        }

        [Test]
        public void OneAdvance_ShouldEqualSteppedTicking_AcrossTwoDaysOfRaids()
        {
            var end = T0 + 2 * DAY;
            var once = Watched();
            once.Timeline.Advance(end);
            var stepped = Watched();
            for (var t = T0 + MINUTE; t <= end; t += MINUTE) stepped.Timeline.Advance(t);

            Assert.That(once.Raids.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(stepped.Raids.Select(r => (r.Id, r.At, r.Took["Gold"])), Is.EqualTo(once.Raids.Select(r => (r.Id, r.At, r.Took["Gold"]))));
            Assert.That(stepped.State.Lairs["Orcs"].Hoard, Is.EqualTo(once.State.Lairs["Orcs"].Hoard));
            Assert.That(stepped.State.Lairs["Orcs"].NextRaidAt, Is.EqualTo(once.State.Lairs["Orcs"].NextRaidAt));
            for (var i = 0; i < once.City.Districts.Count; i++)
                Assert.That(stepped.City.Districts[i].Store.Held, Is.EqualTo(once.City.Districts[i].Store.Held));
        }

        [Test]
        public void TheZone_ShouldHoldNothingBeforeTheLairIsFound_AndNothingAfterItIsCleared()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Held.HoldingAt(HarvestFixture.TREE), Is.Null);

            fixture.Find(Fixture.HARPIES);
            fixture.Timeline.Advance(T0);
            Assert.That(fixture.Held.HoldingAt(HarvestFixture.TREE), Is.EqualTo("Harpies"));
            Assert.That(fixture.Held.HoldingAt(new Vector2Int(-3, -3)), Is.Null);

            fixture.State.Lairs["Harpies"].Cleared = true;
            Assert.That(fixture.Held.HoldingAt(HarvestFixture.TREE), Is.Null);
        }

        [Test]
        public void TheZone_ShouldRefuseATap_BeforeAnyMana()
        {
            var fixture = new Fixture();
            fixture.Uncover(Fixture.HARPIES);
            fixture.Timeline.Advance(T0);
            var harvesting = new Harvesting(fixture.HarvestState, fixture.Ground, fixture.City, fixture.Map,
                fixture.Buildings, fixture.Features, fixture.Sources, fixture.Yields, new Tap(), fixture.Treasury, fixture.Mana, 42,
                fixture.Revealed, fixture.Gates, fixture.Bonuses, null, fixture.Held);
            var mana = fixture.Treasury.Get("Mana");

            var result = harvesting.Tap(HarvestFixture.TREE, T0);

            Assert.That(result.Refusal, Is.EqualTo(TapRefusal.LairHeld));
            Assert.That(fixture.Treasury.Get("Mana"), Is.EqualTo(mana));
        }

        [Test]
        public void TheZone_ShouldRefuseABuilding_AndTheLairsOwnCellsUntilItFalls()
        {
            var fixture = new Fixture();
            var placement = new Placement(fixture.Map, fixture.Buildings, fixture.Settings, fixture.City, fixture.Ground, fixture.Revealed,
                null, fixture.Held);
            fixture.Uncover(Fixture.HARPIES);
            Assert.That(placement.Check("Housing", new Vector2Int(1, 4)), Is.EqualTo(PlacementProblem.None));
            Assert.That(placement.Check("Housing", new Vector2Int(2, 2)), Is.EqualTo(PlacementProblem.Occupied));

            fixture.Timeline.Advance(T0);
            Assert.That(placement.Check("Housing", new Vector2Int(1, 4)), Is.EqualTo(PlacementProblem.LairZone));

            fixture.State.Lairs["Harpies"].Cleared = true;
            Assert.That(placement.Check("Housing", new Vector2Int(1, 4)), Is.EqualTo(PlacementProblem.None));
            Assert.That(placement.Check("Housing", new Vector2Int(2, 2)), Is.EqualTo(PlacementProblem.None));
        }

        [Test]
        public void TheZone_ShouldNeverBeWorkedByACrew()
        {
            var fixture = new Fixture();
            var mill = fixture.Stand("Sawmill", new Vector2Int(4, 1));
            fixture.Uncover(Fixture.HARPIES);
            Assert.That(fixture.Crews.Workable(mill), Has.Member(HarvestFixture.TREE));

            fixture.Timeline.Advance(T0);

            Assert.That(fixture.Crews.Workable(mill), Is.Empty);
        }

        [Test]
        public void ThePath_ShouldRampUpToTheLairsOwnPowerAtTheLastFight()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Lairs.Fights(Fixture.ORCS), Is.EqualTo(3));
            Assert.That(Enumerable.Range(0, 3).Select(i => fixture.Lairs.FightPower(Fixture.ORCS, i)), Is.EqualTo(new[] { 30, 45, 60 }));
            Assert.That(fixture.Lairs.Fights(Fixture.HARPIES), Is.EqualTo(4));
            Assert.That(fixture.Lairs.FightPower(Fixture.HARPIES, 3), Is.EqualTo(300));
        }

        [Test]
        public void ThePath_ShouldMoveOnAStepAWin_AndKeepRaidingUntilTheLast()
        {
            var fixture = Watched();

            Assert.That(fixture.Lairs.WinFight("Orcs"), Is.False);
            Assert.That(fixture.Lairs.WinFight("Orcs"), Is.False);
            Assert.That(fixture.Lairs.FightIndex(Fixture.ORCS), Is.EqualTo(2));
            Assert.That(fixture.State.Lairs["Orcs"].NextRaidAt, Is.Not.Null);

            Assert.That(fixture.Lairs.WinFight("Orcs"), Is.True);
            Assert.That(fixture.Lairs.AwaitsClaim("Orcs"), Is.True);
            Assert.That(fixture.State.Lairs["Orcs"].NextRaidAt, Is.Null);
            Assert.That(fixture.Lairs.Coming, Is.Empty);
        }

        [Test]
        public void TheClaim_ShouldHandBackTheHoard_AndFreeItsGround()
        {
            var fixture = Watched();
            fixture.Timeline.Advance(T0 + DAY);
            var hoard = fixture.State.Lairs["Orcs"].Hoard["Gold"];
            var gold = fixture.Treasury.Get("Gold");
            for (var i = 0; i < 3; i++) fixture.Lairs.WinFight("Orcs");

            var back = fixture.Lairs.Claim("Orcs", fixture.Treasury);

            Assert.That(back["Gold"], Is.EqualTo(hoard));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold + hoard));
            Assert.That(fixture.Held.HoldingAt(Fixture.ORCS.Anchor), Is.Null);
            Assert.That(fixture.Lairs.ClearedCount, Is.EqualTo(1));
            Assert.That(fixture.Lairs.Claim("Orcs", fixture.Treasury), Is.Empty);
        }

        [Test]
        public void TheClaim_ShouldRefuseALairStillStanding()
        {
            var fixture = Watched();

            Assert.That(fixture.Lairs.Claim("Orcs", fixture.Treasury), Is.Empty);
            Assert.That(fixture.State.Lairs["Orcs"].Cleared, Is.False);
        }

        [Test]
        public void TheReward_ShouldBeAFightsHeroXp_AndTheFirstClearKnowledge()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Lairs.ClearReward(Fixture.ORCS), Is.EqualTo((167.0, 3.0)));
        }

        private sealed class Tap : ITapSettings
        {
            public double WorkSeconds => 10;
            public double ManaCost => 1;
        }
    }
}
