using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingdom.Sim.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using static Kingdom.Tests.Sim.Parity.JsonParity;

namespace Kingdom.Tests.Sim.Parity
{
    // GameData derives what the web prototype's definitions.ts derives, field for field.
    public class DefinitionsParityTests
    {
        private static GameData _data;
        private static JToken _golden;

        private static GameData Data => _data ??= GameData.Load(file => File.ReadAllText(Path.Combine(TestPaths.Data, file)));
        private static JToken Golden => _golden ??= JsonParity.Golden("definitions.json");

        private static readonly Dictionary<string, System.Func<GameData, JToken>> PROJECTIONS = new()
        {
            ["techOrder"] = d => ToJson(d.TechOrder),
            ["technologies"] = d => Map(d.Technologies, t => ToJson(t)),
            ["districts"] = d => Map(d.Districts, x => Merge(x.Data, new { id = x.Id, requiredTech = x.RequiredTech, requiredTechPerLevel = x.RequiredTechPerLevel, extraCountTech = x.ExtraCountTech })),
            ["buildableDistricts"] = d => ToJson(d.BuildableDistricts),
            ["workshops"] = d => ToJson(d.Workshops),
            ["decorations"] = d => ToJson(d.Decorations),
            ["harvest"] = d => Map(d.Harvest, h => Merge(h.Data, new { id = h.Id, currencyId = h.CurrencyId, requiredTech = h.RequiredTech })),
            ["currencies"] = d => Map(d.Currencies, c => ToJson(c)),
            ["goods"] = d => ToJson(d.Collections.Goods.ToDictionary(g => g.Key, g => Merge(g.Value, new { id = g.Key }))),
            ["goodOrder"] = d => ToJson(d.GoodOrder),
            ["features"] = d => Map(d.Features, f => ToJson(f)),
            ["units"] = d => Map(d.Units, u => Merge(u.Data, new { id = u.Id, tags = u.Tags, requiredTech = u.RequiredTech })),
            ["unitOrder"] = d => ToJson(d.UnitOrder),
            ["troops"] = d => Map(d.Troops, t => ToJson(t)),
            ["troopOrder"] = d => ToJson(d.TroopOrder),
            ["tomeOrder"] = d => ToJson(d.TomeOrder),
            ["eraCount"] = d => ToJson(d.EraCount),
            ["eraUnlockCells"] = d => ToJson(d.EraUnlockCells),
            ["eraRewards"] = d => ToJson(d.EraRewards),
            ["techsInTome"] = d => ToJson(d.TomeOrder.ToDictionary(t => t, d.TechsInTome)),
            ["terrainGates"] = d => ToJson(new[] { "Grassland", "Plains", "Desert", "Snow", "Tundra", "Water" }.ToDictionary(t => t, d.TerrainGate)),
            ["worldUpgradeGates"] = d => ToJson(new[] { "Fortress", "Chapel" }.ToDictionary(t => t, d.WorldUpgradeGate)),
            ["landmarks"] = d => ToJson(d.Landmarks),
            ["abandoned"] = d => ToJson(d.Abandoned),
            ["lairOrder"] = d => ToJson(d.LairOrder),
            ["lairs"] = d => Map(d.Lairs, l => ToJson(l)),
            ["artifactOrder"] = d => ToJson(d.ArtifactOrder),
            ["artifacts"] = d => Map(d.Artifacts, a => ToJson(a)),
            ["relicKinds"] = d => ToJson(d.Artifacts.ToDictionary(a => a.Key, a => a.Value.Kind)),
            ["relicDoors"] = d => ToJson(d.Artifacts.ToDictionary(a => a.Key, a => a.Value.Door)),
            ["heroOrder"] = d => ToJson(d.HeroOrder),
            ["heroes"] = d => Map(d.Heroes, h => Merge(h.Data, new { id = h.Id, skill = h.Skill })),
            ["villainOrder"] = d => ToJson(d.VillainOrder),
            ["villains"] = d => Map(d.Villains, v => Merge(v.Data, new { id = v.Id, skill = v.Skill })),
            ["bannerOrder"] = d => ToJson(d.BannerOrder),
            ["banners"] = d => ToJson(d.BannerOrder.ToDictionary(b => b, b => Merge(d.Collections.Banners[b], new { id = b }))),
            ["garrisonsByTier"] = d => ToJson(Enumerable.Range(1, 6).ToDictionary(t => t.ToString(System.Globalization.CultureInfo.InvariantCulture), t => d.GarrisonForTier(t))),
            ["storeOrder"] = d => ToJson(d.StoreOrder),
            ["gemPackOrder"] = d => ToJson(d.StoreShelf("gems")),
            ["itemBundleOrder"] = d => ToJson(d.StoreShelf("bag")),
            ["offerOrder"] = d => ToJson(d.StoreShelf("offer")),
            ["dailyPool"] = d => ToJson(d.StoreShelf("daily")),
            ["itemOrder"] = d => ToJson(d.ItemOrder),
        };

        // Fields the web carries that nothing reads: a troop spreads its unit's row, evolutions and all.
        private static readonly HashSet<string> IGNORED = new() { "evolutions" };

        private static IEnumerable<string> Sections() => PROJECTIONS.Keys;

        [TestCaseSource(nameof(Sections))]
        public void Definitions_ShouldMatchTheWebPrototype(string section)
        {
            var difference = Diff(Golden[section], PROJECTIONS[section](Data), section, IGNORED);
            Assert.That(difference, Is.Null, difference);
        }

        [Test]
        public void Golden_ShouldHaveNoSectionWithoutAProjection()
        {
            var unchecked_ = ((JObject)Golden).Properties().Select(p => p.Name).Where(n => !PROJECTIONS.ContainsKey(n)).ToList();
            Assert.That(unchecked_, Is.Empty);
        }
    }
}
