using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Core;
using Newtonsoft.Json.Linq;

namespace Codigames.Kingdom.Data
{
    // Everything the sim knows that is not state: the collections as authored, the tech tree and the map,
    // and what is derived from them — above all the GATES, which every technology states once and every
    // building, unit, rank, source and terrain reads. Built once from the data files and never changed.
    public sealed class GameData
    {
        private static readonly string[] TOME_ORDER = { "Kingdom", "Sagas", "Atlas" };
        private static readonly string[] UNIT_ORDER = { "Warrior", "Lancer", "Archer", "Cavalry" };
        private static readonly string[] LAIR_ORDER = { "Orcs", "Harpies", "Goblins", "WolfRiders", "Drake" };
        private static readonly string[] BANNER_ORDER = { "basic", "advanced" };

        private static readonly string[] ARTIFACT_ORDER =
        {
            "DowsingRod", "VerdantSeal", "ForemansSigil", "GildedLedger", "WanderersCompass",
            "DelversLantern", "MusterHorn", "BailiffsTally",
        };

        private static readonly string[] HERO_ORDER =
        {
            "Warden", "Quartermaster", "Scholar", "RelicHunter", "Scout", "Adventurer", "Bard",
            "BeastkinHunter", "Cleric", "Cook", "Gardener", "Joker", "Merchant", "Priest", "Rogue",
            "ThreeMice", "Sellsword", "DarkKnight", "Paladin", "Wizard", "Witch", "Druid",
            "IceLancer", "HolyWarrior", "SavageWarrior", "Spymaster", "ElectricArcher",
            "GoldenDragon", "VampireLord", "Necromancer", "Pharao", "ElvenPrincess",
        };

        private readonly Gates _gates;

        public GameData(Collections collections, TechTreeDoc techTree, RegionMapDoc map)
        {
            Collections = collections;
            TechTree = techTree;
            Map = map;

            TechOrder = techTree.Technologies.Keys.Where(id => !id.StartsWith("_", StringComparison.Ordinal)).ToList();
            Technologies = TechOrder.ToDictionary(id => id, id => ToTechnology(id, techTree.Technologies[id]));
            _gates = new Gates(TechOrder, Technologies);

            Districts = collections.Buildings.ToDictionary(e => e.Key, e => ToDistrict(e.Key, e.Value));
            DistrictOrder = collections.Buildings.Keys.ToList();
            BuildableDistricts = DistrictOrder.Where(id => collections.Buildings[id].Buildable).ToList();
            Workshops = DistrictOrder.Where(id => collections.Buildings[id].Produces != null).ToList();
            Decorations = DistrictOrder
                .Where(id => collections.Buildings[id].HarmonySupply > 0 && !collections.Buildings[id].HostsRelic).ToList();

            Harvest = HARVEST_CURRENCIES.ToDictionary(e => e.Key, e => new HarvestSpec
            {
                Id = e.Key,
                CurrencyId = e.Value,
                Data = collections.Harvest[e.Key],
                RequiredTech = _gates.Of(_gates.Harvest, e.Key),
            });

            Currencies = CURRENCY_SCOPES.ToDictionary(e => e.Key, e => ToCurrency(e.Key, e.Value, collections.Currencies[e.Key]));
            Features = FEATURES.ToDictionary(f => f.Id);

            Units = UNIT_ORDER.ToDictionary(id => id, id => new UnitDef
            {
                Id = id,
                Tags = UNIT_TAGS[id],
                Data = collections.Units[id],
                RequiredTech = _gates.Of(_gates.Unit, id),
            });
            Troops = BuildTroops();
            TroopOrder = UNIT_ORDER.SelectMany(u => Enumerable.Range(1, 5).Select(r => TroopId(u, r))).Where(Troops.ContainsKey).ToList();

            EraCount = TOME_ORDER.ToDictionary(t => t, t => techTree.Eras != null && techTree.Eras.TryGetValue(t, out var e) ? e.Count : 0);
            EraUnlockCells = TOME_ORDER.ToDictionary(t => t, t => (IReadOnlyList<double>)new[] { 0d }
                .Concat(Enumerable.Range(1, EraCount[t]).Select(era => techTree.Eras[t][era - 1])).ToList());
            EraRewards = TOME_ORDER.ToDictionary(t => t, t => (IReadOnlyList<double?>)new double?[] { null }
                .Concat(Enumerable.Range(0, EraCount[t]).Select(i =>
                    techTree.EraRewards != null && techTree.EraRewards.TryGetValue(t, out var list) && i < list.Count ? list[i] : null))
                .ToList());

            Landmarks = (map.Landmarks ?? new List<LandmarkDoc>()).Select(l => new LandmarkDef
            {
                Id = l.Id, Kind = l.Kind, Location = new Coord(l.X, l.Y), ClaimCost = l.ClaimCost, Size = l.Size ?? 1,
            }).ToList();
            Abandoned = (map.Abandoned ?? new List<AbandonedDoc>()).Select(a => new AbandonedDef
            {
                Id = a.Id, DistrictId = a.District, Location = new Coord(a.X, a.Y), Sight = a.Sight, Name = a.Name,
            }).ToList();
            Lairs = LAIR_ORDER.ToDictionary(id => id, id => ToLair(id, map));
            Artifacts = ARTIFACT_ORDER.ToDictionary(id => id, id => ToArtifact(id, collections.Artifacts[id]));
            Heroes = HERO_ORDER.ToDictionary(id => id, id => new HeroDef
            {
                Id = id,
                Data = collections.Heroes[id],
                Skill = new SkillDef { Id = collections.Heroes[id].Skill, Value = collections.Heroes[id].SkillValue, Every = collections.Heroes[id].SkillEvery },
            });
            VillainOrder = collections.Villains.Keys.ToList();
            Villains = collections.Villains.ToDictionary(e => e.Key, e => new VillainDef
            {
                Id = e.Key,
                Data = e.Value,
                Skill = new SkillDef { Id = e.Value.Skill, Value = e.Value.SkillValue, Every = e.Value.SkillEvery },
            });
            foreach (var id in BANNER_ORDER)
            {
                if (!collections.Banners.ContainsKey(id)) throw new InvalidOperationException($"banners.json is missing the banner \"{id}\".");
            }

            StoreOrder = collections.Store.Keys.ToList();
            ItemOrder = collections.Items.Keys.ToList();
            GoodOrder = collections.Goods.Keys.ToList();
        }

        public Collections Collections { get; }
        public TechTreeDoc TechTree { get; }
        public RegionMapDoc Map { get; }

        public IReadOnlyList<string> TechOrder { get; }
        public IReadOnlyDictionary<string, TechnologyDef> Technologies { get; }
        public IReadOnlyList<string> TomeOrder => TOME_ORDER;
        public IReadOnlyDictionary<string, int> EraCount { get; }
        public int MaxEra => TOME_ORDER.Max(t => EraCount[t]);

        // Indexed by era: [0] is unused, [1] is the first band (always 0 cells).
        public IReadOnlyDictionary<string, IReadOnlyList<double>> EraUnlockCells { get; }

        // Indexed by era like EraUnlockCells: relic fragments finishing a band pays; null = nothing.
        public IReadOnlyDictionary<string, IReadOnlyList<double?>> EraRewards { get; }

        public IReadOnlyDictionary<string, DistrictDef> Districts { get; }
        public IReadOnlyList<string> DistrictOrder { get; }
        public IReadOnlyList<string> BuildableDistricts { get; }
        public IReadOnlyList<string> Workshops { get; }
        public IReadOnlyList<string> Decorations { get; }

        public IReadOnlyDictionary<string, HarvestSpec> Harvest { get; }
        public IReadOnlyDictionary<string, CurrencyDef> Currencies { get; }
        public IReadOnlyDictionary<string, FeatureDef> Features { get; }
        public IReadOnlyList<string> GoodOrder { get; }

        public IReadOnlyDictionary<string, UnitDef> Units { get; }
        public IReadOnlyList<string> UnitOrder => UNIT_ORDER;
        public IReadOnlyDictionary<string, TroopDef> Troops { get; }
        public IReadOnlyList<string> TroopOrder { get; }

        public IReadOnlyList<LandmarkDef> Landmarks { get; }
        public IReadOnlyList<AbandonedDef> Abandoned { get; }
        public IReadOnlyList<string> LairOrder => LAIR_ORDER;
        public IReadOnlyDictionary<string, LairDef> Lairs { get; }
        public IReadOnlyList<string> ArtifactOrder => ARTIFACT_ORDER;
        public IReadOnlyDictionary<string, ArtifactDef> Artifacts { get; }
        public IReadOnlyList<string> HeroOrder => HERO_ORDER;
        public IReadOnlyDictionary<string, HeroDef> Heroes { get; }
        public IReadOnlyList<string> VillainOrder { get; }
        public IReadOnlyDictionary<string, VillainDef> Villains { get; }
        public IReadOnlyList<string> BannerOrder => BANNER_ORDER;
        public IReadOnlyList<string> StoreOrder { get; }
        public IReadOnlyList<string> ItemOrder { get; }

        // Technologies on one book's page, in file order. One off the page is in no book.
        public IReadOnlyList<string> TechsInTome(string tome)
            => TechOrder.Where(id => Technologies[id].Placed && Technologies[id].Tome == tome).ToList();

        // The technology a terrain waits on, or null.
        public string TerrainGate(string terrain) => _gates.Of(_gates.Terrain, terrain);

        // The technology a world-board building waits on, or null.
        public string WorldUpgradeGate(string upgrade) => _gates.Of(_gates.WorldUpgrade, upgrade);

        // The tier row, or the deepest one authored: a lair never falls off the end of the table.
        public GarrisonData GarrisonForTier(double tier)
            => Collections.Garrisons.FirstOrDefault(g => g.Tier == tier) ?? Collections.Garrisons[Collections.Garrisons.Count - 1];

        public IReadOnlyList<string> StoreShelf(string shelf) => StoreOrder.Where(id => Collections.Store[id].Shelf == shelf).ToList();

        public IReadOnlyList<string> HeroesOfRarity(string rarity) => HERO_ORDER.Where(id => Heroes[id].Data.Rarity == rarity).ToList();

        // 1-based per-level list lookup that clamps to the last entry (the data's convention).
        public static T LevelIndexed<T>(IReadOnlyList<T> list, double level)
            => list[(int)Math.Min(Math.Max(level, 1), list.Count) - 1];

        // The troop a unit is at a rank: "Warrior" at I, "Warrior_e3" at III.
        public static string TroopId(string unit, int rank) => rank == 1 ? unit : unit + "_e" + rank;

        public static string UnitOf(string troop)
        {
            var at = troop.IndexOf("_e", StringComparison.Ordinal);
            return at < 0 ? troop : troop.Substring(0, at);
        }

        public static int RankOf(string troop)
        {
            var at = troop.IndexOf("_e", StringComparison.Ordinal);
            return at < 0 ? 1 : int.Parse(troop.Substring(at + 2), System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---------------------------------------------------------------- code tables

        // What each source pays. Identity and money are different things: bushes, game and shoals all pay Food.
        private static readonly Dictionary<string, string> HARVEST_CURRENCIES = new()
        {
            ["Forest"] = "Wood", ["Crops"] = "Food", ["Berries"] = "Food", ["Meat"] = "Food",
            ["Stone"] = "Stone", ["Fish"] = "Food", ["MountainIron"] = "Stone", ["MountainGold"] = "Gold",
        };

        // Who holds each currency, in the header's order.
        private static readonly Dictionary<string, string> CURRENCY_SCOPES = new()
        {
            ["Gold"] = "city", ["Food"] = "city", ["Wood"] = "city", ["Stone"] = "city", ["Mana"] = "city",
            ["Knowledge"] = "kingdom", ["Stardust"] = "kingdom", ["HeroXp"] = "kingdom", ["Gems"] = "player",
        };

        private static readonly FeatureDef[] FEATURES =
        {
            new() { Id = "Trees", Source = "Forest", RespawnTerrain = "Grassland" },
            new() { Id = "Mountain", Source = "Stone", RespawnTerrain = "Grassland", MaxFootprint = 3 },
            new() { Id = "MountainIron", Source = "MountainIron", RespawnTerrain = "Grassland" },
            new() { Id = "MountainGold", Source = "MountainGold", RespawnTerrain = "Grassland" },
            new() { Id = "BerryBush", Source = "Berries", RespawnTerrain = "Grassland" },
            new() { Id = "WildAnimals", Source = "Meat", RespawnTerrain = "Grassland" },
            new() { Id = "FishShoal", Source = "Fish", RespawnTerrain = "Water" },
            new() { Id = "Crops", Source = "Crops", RespawnTerrain = "Grassland" },
        };

        private static readonly Dictionary<string, IReadOnlyList<string>> UNIT_TAGS = new()
        {
            ["Warrior"] = new[] { "Melee" }, ["Lancer"] = new[] { "Melee" },
            ["Archer"] = new[] { "Distance" }, ["Cavalry"] = new[] { "Mounted", "Melee" },
        };

        // What each relic's passive moves. Its numbers are data; which numbers is code.
        private static readonly Dictionary<string, RelicStat[]> RELIC_STATS = new()
        {
            ["DowsingRod"] = new[] { Mul("harvestStock"), Mul("recoverySpeed") },
            ["VerdantSeal"] = new[] { Mul("harvestUnitsPerStrike") },
            ["ForemansSigil"] = new[] { Mul("workerStrikeSpeed"), Mul("workerSpeed"), Mul("trainingSpeed") },
            ["GildedLedger"] = new[] { Mul("taxRate") },
            ["DelversLantern"] = new[] { Mul("roomHaul") },
            ["MusterHorn"] = new[] { Mul("armyCap") },
            ["BailiffsTally"] = new[] { Mul("worldImprovementYield") },
            ["WanderersCompass"] = new[] { Mul("stardustYield") },
        };

        // The relics with a city activation in a Shrine.
        private static readonly HashSet<string> CITY_ACTIVATED = new() { "DowsingRod", "VerdantSeal", "ForemansSigil", "GildedLedger" };

        private static RelicStat Mul(string stat) => new() { Stat = stat, Scope = null, Op = "mul" };

        // ---------------------------------------------------------------- derivations

        private static TechnologyDef ToTechnology(string id, TechNodeDoc node)
        {
            var placed = node.IsPlaced;
            var cost = new Dictionary<string, double> { ["Gold"] = node.Gold };
            if ((node.Knowledge ?? 0) > 0) cost["Knowledge"] = node.Knowledge.Value;
            if (node.Materials != null)
            {
                foreach (var material in node.Materials) cost[material.Key] = material.Value;
            }

            return new TechnologyDef
            {
                Id = id,
                Kind = node.Kind,
                Unlocks = node.Unlocks ?? new List<TechUnlock>(),
                Tome = placed ? node.Tome : "Kingdom",
                Era = placed ? node.Era.Value : 1,
                Row = placed ? node.Row.Value : 0,
                Col = placed ? node.Col.Value : 0,
                Placed = placed,
                Requires = node.Requires ?? new List<string>(),
                Cost = cost,
                Goods = node.Goods ?? new Dictionary<string, double>(),
                AnyPrecious = node.AnyPrecious ?? 0,
                Effects = node.Effects ?? new List<TechEffect>(),
                Planned = node.Planned == true,
            };
        }

        private DistrictDef ToDistrict(string id, BuildingData data) => new()
        {
            Id = id,
            Data = data,
            RequiredTech = _gates.Of(_gates.District, id),
            RequiredTechPerLevel = Enumerable.Range(0, Math.Max(0, (int)data.MaxLevel - 1))
                .Select(i => _gates.Of(_gates.DistrictLevel, id + ":" + (i + 2))).ToList(),
            ExtraCountTech = _gates.Of(_gates.DistrictCount, id),
        };

        private static CurrencyDef ToCurrency(string id, string scope, CurrencyData data) => new()
        {
            Id = id, Scope = scope, Cap = data.Cap, Start = data.Start, Primary = data.Primary, GoldValue = data.GoldValue,
        };

        private Dictionary<string, TroopDef> BuildTroops()
        {
            var troops = new Dictionary<string, TroopDef>();

            foreach (var unit in UNIT_ORDER)
            {
                var u = Units[unit];
                troops[unit] = Troop(u, unit, 1, 1, u.Data.Power, u.Data.Atk, u.Data.Dmg, u.Data.Def, u.Data.Hp,
                    u.Data.RecruitCost, u.Data.TrainDurationSeconds, u.RequiredTech);

                var rows = u.Data.Evolutions ?? new List<UnitDataEvolutionsEntry>();
                for (var i = 0; i < rows.Count; i++)
                {
                    var rank = i + 2;
                    var row = rows[i];
                    troops[TroopId(unit, rank)] = Troop(u, TroopId(unit, rank), rank, row.MinBuildingLevel, row.Power, row.Atk,
                        row.Dmg, row.Def, row.Hp, row.RecruitCost, row.TrainDurationSeconds,
                        _gates.Of(_gates.Evolution, unit + ":" + rank));
                }
            }

            return troops;
        }

        private static TroopDef Troop(UnitDef unit, string id, int rank, double minLevel, double power, double atk, double dmg,
            double def, double hp, Dictionary<string, double> cost, double trainSeconds, string requiredTech) => new()
        {
            Id = id, Unit = unit.Id, Rank = rank, MinBuildingLevel = minLevel, Power = power, Atk = atk, Dmg = dmg, Def = def,
            Hp = hp, RecruitCost = cost, TrainDurationSeconds = trainSeconds, RequiredTech = requiredTech, Tags = unit.Tags,
            SquadSize = unit.Data.SquadSize, Frontage = unit.Data.Frontage, Cooldown = unit.Data.Cooldown,
            Speed = unit.Data.Speed, Range = unit.Data.Range,
        };

        private static LairDef ToLair(string id, RegionMapDoc map)
        {
            if (map.Lairs == null || !map.Lairs.TryGetValue(id, out var lair))
                throw new InvalidOperationException($"region-map.json is missing the lair \"{id}\".");

            return new LairDef
            {
                Id = id, Location = new Coord(lair.X, lair.Y), Size = lair.Size ?? 1, Tier = lair.Tier, Radius = lair.Radius,
                Sight = lair.Sight, Flavour = lair.Flavour, Guard = lair.Guard,
            };
        }

        private static ArtifactDef ToArtifact(string id, ArtifactData data) => new()
        {
            Id = id,
            Kind = data.Kind,
            Door = data.Door,
            Passive = new RelicPassive { Stats = RELIC_STATS[id], Base = data.PassiveBase, PerLevel = data.PassivePerLevel },
            Active = id switch
            {
                // Counted in rooms, not minutes, and untargeted: a delve is the place.
                "DelversLantern" => new RelicActive
                {
                    Id = "Lamplight", Targeted = false, ManaCost = data.ActiveManaCost, Power = data.ActivePower,
                    PowerPerLevel = data.ActivePowerPerLevel, Charges = data.ActiveCharges, ChargesPerLevel = data.ActiveChargesPerLevel,
                },
                // Radius is its whole growth: for a reveal, more ground is the effect.
                "WanderersCompass" => new RelicActive
                {
                    Id = "Survey", Targeted = true, ManaCost = data.ActiveManaCost, Radius = data.ActiveRadius,
                },
                _ => null,
            },
            Activation = CITY_ACTIVATED.Contains(id) ? new RelicActivation { ManaCost = data.ActiveManaCost, Radius = data.ActiveRadius } : null,
        };

        // ---------------------------------------------------------------- loading

        // Builds the data from a reader that returns the text of a file under the data folder
        // ("Game/buildings.json", "tech-tree.json", "region-map.json").
        public static GameData Load(Func<string, string> readFile)
        {
            var collections = new JObject();
            foreach (var property in typeof(Collections).GetProperties())
            {
                var file = ((Newtonsoft.Json.JsonPropertyAttribute)property.GetCustomAttributes(typeof(Newtonsoft.Json.JsonPropertyAttribute), false)[0]).PropertyName;
                collections[file] = JToken.Parse(readFile("Game/" + file + ".json"));
            }

            return new GameData(
                collections.ToObject<Collections>(),
                JToken.Parse(readFile("tech-tree.json")).ToObject<TechTreeDoc>(),
                JToken.Parse(readFile("region-map.json")).ToObject<RegionMapDoc>());
        }

        // Every gate, from what each placed technology says it opens.
        private sealed class Gates
        {
            public readonly Dictionary<string, string> District = new();
            public readonly Dictionary<string, string> DistrictLevel = new();
            public readonly Dictionary<string, string> DistrictCount = new();
            public readonly Dictionary<string, string> Unit = new();
            public readonly Dictionary<string, string> Evolution = new();
            public readonly Dictionary<string, string> Harvest = new();
            public readonly Dictionary<string, string> Terrain = new();
            public readonly Dictionary<string, string> WorldUpgrade = new();

            public Gates(IEnumerable<string> order, IReadOnlyDictionary<string, TechnologyDef> technologies)
            {
                foreach (var id in order)
                {
                    var tech = technologies[id];
                    if (!tech.Placed) continue;

                    foreach (var unlock in tech.Unlocks)
                    {
                        if (unlock.District != null) District[unlock.District] = id;
                        else if (unlock.DistrictLevel != null) DistrictLevel[unlock.DistrictLevel.Id + ":" + Invariant(unlock.DistrictLevel.Level)] = id;
                        else if (unlock.DistrictCount != null) DistrictCount[unlock.DistrictCount] = id;
                        else if (unlock.Unit != null) Unit[unlock.Unit] = id;
                        else if (unlock.Evolution != null) Evolution[unlock.Evolution.Unit + ":" + Invariant(unlock.Evolution.Rank)] = id;
                        else if (unlock.Harvest != null) Harvest[unlock.Harvest] = id;
                        else if (unlock.Terrain != null) Terrain[unlock.Terrain] = id;
                        else if (unlock.WorldUpgrade != null) WorldUpgrade[unlock.WorldUpgrade] = id;
                    }
                }
            }

            private static string Invariant(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            public string Of(Dictionary<string, string> gates, string key) => gates.TryGetValue(key, out var tech) ? tech : null;
        }
    }
}
