using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codigames.Game.Data;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Economy;
using Codigames.Game.Data.Fog;
using Codigames.Game.Data.Harvest;
using Codigames.Game.Data.Magic;
using Codigames.Game.Data.Quests;
using Codigames.Game.Data.Research;
using Codigames.Game.Data.Sites;
using Codigames.Game.Editor.Data;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Codigames.Game.Editor.WebImport
{
    // One-off: brings the web prototype's balance (Tools/WebData) into the data assets. Each collection is
    // imported when its system is rebuilt; once imported, the assets are the source of truth and are edited
    // in the data window. Re-running it overwrites the imported collections with the web's numbers.
    public static partial class WebImporter
    {
        private static string WebData => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "WebData"));

        // Who holds each currency, from the web's code table.
        private static readonly Dictionary<string, CurrencyScope> SCOPES = new()
        {
            ["Gold"] = CurrencyScope.City, ["Food"] = CurrencyScope.City, ["Wood"] = CurrencyScope.City,
            ["Stone"] = CurrencyScope.City, ["Mana"] = CurrencyScope.City, ["Knowledge"] = CurrencyScope.Kingdom,
            ["Stardust"] = CurrencyScope.Kingdom, ["HeroXp"] = CurrencyScope.Kingdom, ["Gems"] = CurrencyScope.Player,
        };

        // Where each reads on the header's plank, from the web's header (game.ts visibleCurrencies).
        private static readonly Dictionary<string, PlankPlace> PLANK = new()
        {
            ["Gold"] = PlankPlace.Coin, ["Food"] = PlankPlace.Coin, ["Wood"] = PlankPlace.Coin,
            ["Stone"] = PlankPlace.CoinWhenHeld, ["Mana"] = PlankPlace.Right, ["Gems"] = PlankPlace.Right,
        };

        // What each harvest source pays, and what each feature is, from the web's code tables (definitions.ts).
        private static readonly Dictionary<string, string> SOURCE_CURRENCY = new()
        {
            ["Forest"] = "Wood", ["Crops"] = "Food", ["Berries"] = "Food", ["Meat"] = "Food", ["Stone"] = "Stone",
            ["Fish"] = "Food", ["MountainIron"] = "Stone", ["MountainGold"] = "Gold",
        };

        private static readonly (string Id, string Source, string RespawnTerrain, int MaxFootprint, string Name)[] FEATURES =
        {
            ("Trees", "Forest", "Grassland", 1, "Forest"), ("Mountain", "Stone", "Grassland", 3, "Mountain"),
            ("MountainIron", "MountainIron", "Grassland", 1, "Iron mountain"), ("MountainGold", "MountainGold", "Grassland", 1, "Gold mountain"),
            ("BerryBush", "Berries", "Grassland", 1, "Berry bush"), ("WildAnimals", "Meat", "Grassland", 1, "Wild animals"),
            ("FishShoal", "Fish", "Water", 1, "Fish shoal"), ("Crops", "Crops", "Grassland", 1, "Crop plot"),
        };

        [MenuItem("Kingdom/Import web prototype data")]
        public static void ImportAll()
        {
            var currencies = ImportCurrencies();
            var buildings = ImportBuildings();
            ImportConstruction(buildings);
            ImportHarvest();
            ImportEconomy();
            ImportFog();
            ImportMagic();
            var technologies = ImportResearch();
            ImportSites();
            ImportTreasure();
            ImportQuests();
            ImportStage();
            ImportItems();
            ImportGoods();
            ImportUnits();
            ImportLairs();
            ImportHeroes();
            ImportMonetization();
            ImportRelics();
            ImportStore();

            AssetDatabase.SaveAssets();
            Debug.Log($"#Data# Imported {currencies} currencies, {buildings.Count} buildings and {technologies} technologies from the web prototype.");
        }

        // What a unit is, beside its numbers: the web keeps it in code (definitions.ts UNIT_CONTENT).
        private static readonly Dictionary<string, (string Name, string Description, string[] Tags)> UNIT_CONTENT = new()
        {
            ["Warrior"] = ("Warrior", "Sturdy front line: the most armour and health per Gold.", new[] { "Melee" }),
            ["Lancer"] = ("Lancer", "Long reach that keeps the line safe.", new[] { "Melee" }),
            ["Archer"] = ("Archer", "Ranged support: the most attack per Gold, and the least of everything else.", new[] { "Distance" }),
            ["Cavalry"] = ("Cavalry", "Fast and hard-hitting.", new[] { "Mounted", "Melee" }),
        };

        private static void ImportUnits()
        {
            var web = Read<Dictionary<string, UnitData>>("Game/units.json");
            var assets = new List<Codigames.Game.Data.Army.UnitAsset>();
            foreach (var (id, row) in web)
            {
                var asset = LoadOrCreate<Codigames.Game.Data.Army.UnitAsset>("Units", id);
                var so = new SerializedObject(asset);
                var content = UNIT_CONTENT.TryGetValue(id, out var c) ? c : (Name: id, Description: "", Tags: new string[0]);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_name").stringValue = content.Name;
                so.FindProperty("_description").stringValue = content.Description;
                SetStrings(so.FindProperty("_tags"), content.Tags.ToList());
                so.FindProperty("_squadSize").intValue = (int)row.SquadSize;
                so.FindProperty("_frontage").intValue = (int)row.Frontage;
                so.FindProperty("_cooldown").intValue = (int)row.Cooldown;
                so.FindProperty("_speed").intValue = (int)row.Speed;
                so.FindProperty("_range").intValue = (int)row.Range;
                var ranks = so.FindProperty("_ranks");
                var evolutions = row.Evolutions ?? new List<UnitDataEvolutionsEntry>();
                ranks.arraySize = 1 + evolutions.Count;
                void Rank(int i, double atk, double dmg, double def, double hp, double power, Dictionary<string, double> cost, double seconds, double hall)
                {
                    var rank = ranks.GetArrayElementAtIndex(i);
                    var file = i == 0 ? $"unit_{id.ToLowerInvariant()}_avatar" : $"unit_{id.ToLowerInvariant()}_e{i + 1}_avatar";
                    rank.FindPropertyRelative("Portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Portraits/{file}.png");
                    rank.FindPropertyRelative("Atk").intValue = (int)atk;
                    rank.FindPropertyRelative("Dmg").intValue = (int)dmg;
                    rank.FindPropertyRelative("Def").intValue = (int)def;
                    rank.FindPropertyRelative("Hp").intValue = (int)hp;
                    rank.FindPropertyRelative("Power").intValue = (int)power;
                    SetAmounts(rank.FindPropertyRelative("Cost"), cost ?? new Dictionary<string, double>());
                    rank.FindPropertyRelative("TrainSeconds").doubleValue = seconds;
                    rank.FindPropertyRelative("MinHallLevel").intValue = System.Math.Max(1, (int)hall);
                }

                Rank(0, row.Atk, row.Dmg, row.Def, row.Hp, row.Power, row.RecruitCost, row.TrainDurationSeconds, 1);
                for (var i = 0; i < evolutions.Count; i++)
                {
                    var e = evolutions[i];
                    Rank(i + 1, e.Atk, e.Dmg, e.Def, e.Hp, e.Power, e.RecruitCost, e.TrainDurationSeconds, e.MinBuildingLevel);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<Codigames.Game.Data.Army.UnitCollection>(null, "Units"), assets);
        }

        private static void ImportGoods()
        {
            var web = Read<Dictionary<string, GoodData>>("Game/goods.json");
            var assets = new List<Codigames.Game.Data.Goods.GoodAsset>();
            foreach (var (id, row) in web)
            {
                var asset = LoadOrCreate<Codigames.Game.Data.Goods.GoodAsset>("Goods", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_name").stringValue = row.Name;
                so.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Icons/{id}.png");
                so.FindProperty("_tier").intValue = (int)row.Tier;
                so.FindProperty("_precious").boolValue = row.Precious;
                SetAmounts(so.FindProperty("_input"), row.Input ?? new Dictionary<string, double>());
                so.FindProperty("_inputMana").doubleValue = row.InputMana;
                so.FindProperty("_inputGood").stringValue = row.InputGood ?? "";
                so.FindProperty("_inputGoodAmount").intValue = (int)row.InputGoodAmount;
                so.FindProperty("_workSeconds").doubleValue = row.WorkSeconds ?? 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<Codigames.Game.Data.Goods.GoodCollection>(null, "Goods"), assets);
        }

        private static int ImportCurrencies()
        {
            var web = Read<Dictionary<string, CurrencyData>>("Game/currencies.json");
            // A city currency starts at what a new city is handed; the others at their own start.
            var cityStart = Read<EconomyData>("Game/economy.json").City.InitialCurrencies ?? new Dictionary<string, double>();
            var assets = new List<CurrencyAsset>();

            foreach (var (id, row) in web)
            {
                var asset = LoadOrCreate<CurrencyAsset>("Currencies", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_scope").enumValueIndex = (int)SCOPES[id];
                so.FindProperty("_start").doubleValue = SCOPES[id] == CurrencyScope.City && cityStart.TryGetValue(id, out var handed) ? handed : row.Start;
                so.FindProperty("_capped").boolValue = row.Cap.HasValue;
                so.FindProperty("_cap").doubleValue = row.Cap ?? 0;
                so.FindProperty("_icon").objectReferenceValue = CurrencyIcon(id);
                so.FindProperty("_place").enumValueIndex = (int)(PLANK.TryGetValue(id, out var place) ? place : PlankPlace.Hidden);
                so.FindProperty("_sold").boolValue = id == "Gems";
                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<CurrencyCollection>(null, "Currencies"), assets);
            return assets.Count;
        }

        private static Dictionary<string, BuildingAsset> ImportBuildings()
        {
            var web = Read<Dictionary<string, BuildingData>>("Game/buildings.json");
            var assets = new Dictionary<string, BuildingAsset>();

            foreach (var (id, row) in web)
            {
                var asset = LoadOrCreate<BuildingAsset>("Buildings", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_maxLevel").intValue = (int)row.MaxLevel;
                so.FindProperty("_width").intValue = (int)row.Size.X;
                so.FindProperty("_height").intValue = (int)row.Size.Y;
                so.FindProperty("_buildable").boolValue = row.Buildable;
                so.FindProperty("_hostsRelic").boolValue = row.HostsRelic;
                so.FindProperty("_displayName").stringValue = row.Name;
                so.FindProperty("_promise").stringValue = row.Promise;
                so.FindProperty("_description").stringValue = row.Description;
                so.FindProperty("_buildTab").stringValue = row.BuildTab;
                SetArt(so.FindProperty("_art"), row.Sprite);

                var perLevel = so.FindProperty("_cost._perLevel");
                perLevel.arraySize = row.CostPerLevel.Count;
                for (var level = 0; level < row.CostPerLevel.Count; level++)
                {
                    var price = perLevel.GetArrayElementAtIndex(level);
                    SetAmounts(price.FindPropertyRelative("_currencies"), row.CostPerLevel[level].Cost);
                    SetAmounts(price.FindPropertyRelative("_goods"), row.CostPerLevel[level].Goods);
                }

                so.FindProperty("_cost._instanceLinearGrowth").doubleValue = row.InstanceLinearGrowth;
                so.FindProperty("_cost._instanceExponentialGrowth").doubleValue = row.InstanceExponentialGrowth;

                so.FindProperty("_duration._buildSeconds").doubleValue = row.BuildDurationSeconds;
                so.FindProperty("_duration._buildCountGrowth").doubleValue = row.BuildDurationDistrictGrowth;
                so.FindProperty("_duration._buildDistanceGrowth").doubleValue = row.BuildDurationDistanceGrowth;
                so.FindProperty("_duration._upgradeSeconds").doubleValue = row.UpgradeDurationSeconds;
                so.FindProperty("_duration._upgradeLevelGrowth").doubleValue = row.UpgradeDurationLevelGrowth;
                so.FindProperty("_duration._lateUpgradeSeconds").doubleValue = row.UpgradeDurationLateSeconds;
                so.FindProperty("_duration._lateUpgradeLevelGrowth").doubleValue = row.UpgradeDurationLateLevelGrowth;

                SetInts(so.FindProperty("_gates._maxCountPerTownhallLevel"), row.MaxCountPerTownhallLevel);
                SetInts(so.FindProperty("_gates._requiredTownhallLevelPerLevel"), row.RequiredTownhallLevelPerLevel);
                SetInts(so.FindProperty("_gates._requiredPopulationPerLevel"), row.RequiredPopulationPerLevel);
                SetDoubles(so.FindProperty("_production._goldPerMinutePerLevel"), row.GoldPerMinutePerLevel);
                SetDoubles(so.FindProperty("_production._storageCapacityPerLevel"), row.StorageCapacityPerLevel);
                SetInts(so.FindProperty("_production._populationCapacityPerLevel"), row.PopulationCapacityPerLevel);
                SetDoubles(so.FindProperty("_production._taxBonusPerLevel"), row.TaxBonusPerLevel);
                SetStrings(so.FindProperty("_production._harvestSources"), row.HarvestSources);
                so.FindProperty("_production._harmonySupply").doubleValue = row.HarmonySupply;
                SetDoubles(so.FindProperty("_production._harmonyCostPerLevel"), row.HarmonyCostPerLevel);
                so.FindProperty("_production._plants").stringValue = row.Plants ?? "";
                so.FindProperty("_production._produces").stringValue = row.Produces ?? "";
                SetStrings(so.FindProperty("_production._trains"), row.Trains);
                SetInts(so.FindProperty("_production._armyCapPerLevel"), row.ArmyCapPerLevel);
                SetInts(so.FindProperty("_production._bedsPerLevel"), row.BedsPerLevel);
                SetDoubles(so.FindProperty("_production._heroXpBonusPerLevel"), row.HeroXpBonusPerLevel);
                SetInts(so.FindProperty("_production._queueLengthPerLevel"), row.QueueLengthPerLevel);
                SetInts(so.FindProperty("_production._maxWorkersPerLevel"), row.MaxWorkersPerLevel);
                SetInts(so.FindProperty("_production._influenceRadiusPerLevel"), row.InfluenceRadiusPerLevel);
                SetDoubles(so.FindProperty("_production._strikeSpeedPerLevel"), row.StrikeSpeedPerLevel);
                SetDoubles(so.FindProperty("_production._extraUnitsPerDeliveryPerLevel"), row.ExtraUnitsPerDeliveryPerLevel);
                SetStrings(so.FindProperty("_crew"), row.Crew);
                so.FindProperty("_fog._revealRadius").intValue = (int)row.FogRevealRadius;
                SetInts(so.FindProperty("_fog._revealRadiusPerLevel"), row.FogRevealRadiusPerLevel);
                so.FindProperty("_fog._discoverRadius").intValue = (int)row.FogDiscoverRadius;
                so.FindProperty("_repair._seconds").doubleValue = row.RepairDurationSeconds;
                so.FindProperty("_repair._item").stringValue = row.RepairItem ?? "";
                so.FindProperty("_repair._ruinArt").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Buildings/{row.Sprite}_ruin.png");

                so.ApplyModifiedPropertiesWithoutUndo();
                assets[id] = asset;
            }

            SetEntries(LoadOrCreate<BuildingCollection>(null, "Buildings"), assets.Values.ToList());
            return assets;
        }

        private static void ImportConstruction(IReadOnlyDictionary<string, BuildingAsset> buildings)
        {
            var economy = Read<EconomyData>("Game/economy.json");
            var asset = LoadOrCreate<ConstructionSettingsAsset>("Settings", "Construction");
            var so = new SerializedObject(asset);
            so.FindProperty("_townhall").objectReferenceValue = buildings.TryGetValue("Townhall", out var townhall) ? townhall : null;
            so.FindProperty("_lateUpgradeFromLevel").intValue = (int)economy.City.LateUpgradeFromLevel;
            so.FindProperty("_startBuilders").intValue = (int)economy.Kingdom.StartBuilders;
            so.FindProperty("_maxBuilders").intValue = (int)economy.Kingdom.MaxBuilders;
            so.FindProperty("_builderGemCostBase").doubleValue = economy.Kingdom.BuilderGemCostBase;
            so.FindProperty("_builderGemCostGrowth").doubleValue = economy.Kingdom.BuilderGemCostGrowth;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ImportHarvest()
        {
            var sources = new Dictionary<string, HarvestSourceAsset>();
            foreach (var (id, row) in Read<Dictionary<string, HarvestSourceData>>("Game/harvest.json"))
            {
                var asset = LoadOrCreate<HarvestSourceAsset>("HarvestSources", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_currency").stringValue = SOURCE_CURRENCY[id];
                so.FindProperty("_unitsPerStrike").doubleValue = row.UnitsPerStrike;
                so.FindProperty("_secondsPerStrike").doubleValue = row.SecondsPerStrike;
                so.FindProperty("_stock").doubleValue = row.Stock;
                so.FindProperty("_recoverySeconds").doubleValue = row.RecoverySeconds;
                so.FindProperty("_respawnSeconds").doubleValue = row.RespawnSeconds;
                so.FindProperty("_growSeconds").doubleValue = row.GrowSeconds;
                so.ApplyModifiedPropertiesWithoutUndo();
                sources[id] = asset;
            }
            SetEntries(LoadOrCreate<HarvestSourceCollection>(null, "HarvestSources"), sources.Values.ToList<DefinitionAsset>());

            var features = new List<DefinitionAsset>();
            foreach (var (id, source, respawnTerrain, maxFootprint, name) in FEATURES)
            {
                var asset = LoadOrCreate<FeatureAsset>("Features", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_source").objectReferenceValue = sources[source];
                so.FindProperty("_respawnTerrain").stringValue = respawnTerrain;
                so.FindProperty("_tile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/Art/Features/Tiles/{id}.asset");
                so.FindProperty("_maxFootprint").intValue = maxFootprint;
                so.FindProperty("_displayName").stringValue = name;
                var blocks = so.FindProperty("_blockTiles");
                blocks.arraySize = Mathf.Max(0, maxFootprint - 1);
                for (var size = 2; size <= maxFootprint; size++)
                    blocks.GetArrayElementAtIndex(size - 2).objectReferenceValue = MapImporter.BlockTile(id, size);
                so.ApplyModifiedPropertiesWithoutUndo();
                features.Add(asset);
            }
            SetEntries(LoadOrCreate<FeatureCollection>(null, "Features"), features);

            var terrains = new List<DefinitionAsset>();
            foreach (var (id, row) in Read<Dictionary<string, TerrainData>>("Game/terrain.json"))
            {
                var asset = LoadOrCreate<TerrainAsset>("Terrains", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                SetAmounts(so.FindProperty("_yields"), new Dictionary<string, double> { ["Food"] = row.Food, ["Wood"] = row.Wood, ["Stone"] = row.Stone });
                so.ApplyModifiedPropertiesWithoutUndo();
                terrains.Add(asset);
            }
            SetEntries(LoadOrCreate<TerrainCollection>(null, "Terrains"), terrains);

            var tap = Read<EconomyData>("Game/economy.json").Tap;
            var settings = new SerializedObject(LoadOrCreate<TapSettingsAsset>("Settings", "Tap"));
            settings.FindProperty("_workSeconds").doubleValue = tap.WorkSeconds;
            settings.FindProperty("_manaCost").doubleValue = tap.ManaCost;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ImportFog()
        {
            var fog = Read<ExplorationData>("Game/exploration.json").Fog;
            var settings = new SerializedObject(LoadOrCreate<FogSettingsAsset>("Settings", "Fog"));
            SetDoubles(settings.FindProperty("_costPerRing"), fog.Rings.OrderBy(r => r.Distance).Select(r => r.Cost).ToList());
            settings.FindProperty("_fallbackGrowth").doubleValue = fog.FallbackGrowth;
            settings.FindProperty("_minCost").doubleValue = fog.MinCost;
            settings.FindProperty("_countStep").intValue = (int)fog.CountStep;
            settings.FindProperty("_countGrowth").doubleValue = fog.CountGrowth;
            settings.FindProperty("_tapsToReveal").intValue = (int)fog.TapsToReveal;
            SetInts(settings.FindProperty("_reachPerTownhallLevel"), fog.ReachPerTownhallLevel);
            settings.FindProperty("_claimDiscoverRadius").intValue = (int)fog.ClaimDiscoverRadius;
            SetInts(settings.FindProperty("_sightMountainBySize"), fog.Sight.MountainBySize);
            settings.FindProperty("_sightLandmark").intValue = (int)fog.Sight.Landmark;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ImportEconomy()
        {
            var economy = Read<EconomyData>("Game/economy.json");
            var settings = new SerializedObject(LoadOrCreate<EconomySettingsAsset>("Settings", "Economy"));
            settings.FindProperty("_goldPerPopulationPerMinute").doubleValue = economy.Taxes.GoldPerPopulationPerMinute;
            settings.FindProperty("_collectSeconds").doubleValue = economy.Storage.CollectSeconds;
            settings.FindProperty("_moveSpeedTilesPerSecond").doubleValue = economy.Worker.MoveSpeedTilesPerSecond;
            settings.FindProperty("_chestFloorPerHour").doubleValue = economy.Bag.ChestFloorPerHour;
            settings.FindProperty("_secondsPerGem").doubleValue = economy.Rush.SecondsPerGem;
            settings.FindProperty("_noticesShown").intValue = (int)economy.Notices.Shown;
            settings.FindProperty("_noticesKept").intValue = (int)economy.Notices.Kept;
            var tiers = settings.FindProperty("_harmonyTiers");
            tiers.arraySize = economy.Harmony.SurplusTiers.Count;
            for (var i = 0; i < tiers.arraySize; i++)
            {
                tiers.GetArrayElementAtIndex(i).FindPropertyRelative("At").doubleValue = economy.Harmony.SurplusTiers[i].At;
                tiers.GetArrayElementAtIndex(i).FindPropertyRelative("Bonus").doubleValue = economy.Harmony.SurplusTiers[i].Bonus;
            }

            var combat = Read<CombatData>("Game/combat.json");
            settings.FindProperty("_woundedShare").doubleValue = combat.Army.WoundedShare;
            settings.FindProperty("_healCostShare").doubleValue = combat.Army.HealCostShare;
            settings.FindProperty("_healTimeShare").doubleValue = combat.Army.HealTimeShare;

            var adjacency = Read<List<AdjacencyRuleData>>("Game/adjacency.json");
            var rules = settings.FindProperty("_adjacency");
            rules.arraySize = adjacency.Count;
            for (var i = 0; i < adjacency.Count; i++)
            {
                var rule = rules.GetArrayElementAtIndex(i);
                rule.FindPropertyRelative("District").stringValue = adjacency[i].District;
                rule.FindPropertyRelative("Neighbor").stringValue = adjacency[i].Neighbor;
                rule.FindPropertyRelative("Stat").enumValueIndex = adjacency[i].Stat switch
                {
                    "workTime" => (int)Kingdom.City.AdjacencyStat.WorkTime,
                    "trainTime" => (int)Kingdom.City.AdjacencyStat.TrainTime,
                    _ => (int)Kingdom.City.AdjacencyStat.GoldPerMinute,
                };
                rule.FindPropertyRelative("Magnitude").doubleValue = adjacency[i].Magnitude;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();

            var training = new SerializedObject(LoadOrCreate<TrainingSettingsAsset>("Settings", "Training"));
            SetDoubles(training.FindProperty("_costFirst"), economy.City.PopulationCostFirst);
            training.FindProperty("_costGrowth").doubleValue = economy.City.PopulationCostGrowth;
            training.FindProperty("_seconds").doubleValue = economy.Training.Seconds;
            training.FindProperty("_secondsGrowth").doubleValue = economy.Training.VillagerSecondsGrowth;
            training.ApplyModifiedPropertiesWithoutUndo();
        }

        // The pool's base, and a new kingdom's Mana: a full pool.
        private static void ImportMagic()
        {
            var mana = Read<EconomyData>("Game/economy.json").Mana;
            var settings = new SerializedObject(LoadOrCreate<ManaSettingsAsset>("Settings", "Mana"));
            settings.FindProperty("_baseCap").doubleValue = mana.BaseCap;
            settings.FindProperty("_basePerHour").doubleValue = mana.BasePerHour;
            settings.FindProperty("_landmarkCap").doubleValue = mana.LandmarkCap;
            var refills = settings.FindProperty("_gemRefillCosts");
            refills.arraySize = mana.GemRefillCosts?.Count ?? 0;
            for (var i = 0; i < refills.arraySize; i++) refills.GetArrayElementAtIndex(i).doubleValue = mana.GemRefillCosts[i];
            settings.ApplyModifiedPropertiesWithoutUndo();

            var currency = new SerializedObject(LoadOrCreate<CurrencyAsset>("Currencies", "Mana"));
            currency.FindProperty("_start").doubleValue = mana.BaseCap;
            currency.ApplyModifiedPropertiesWithoutUndo();
        }

        // The books' bookmarks: an emblem and a ribbon tint each (the web's research menu).
        private static readonly Dictionary<string, (string Emblem, string Tint)> BOOKS = new()
        {
            ["Kingdom"] = ("research", "#efe2bd"), ["Sagas"] = ("helmet", "#d9912f"), ["Atlas"] = ("compass", "#4f9a6a"),
        };

        private static int ImportResearch()
        {
            var tree = Read<TechTreeDoc>("tech-tree.json");
            var assets = new List<DefinitionAsset>();

            foreach (var (id, row) in tree.Technologies)
            {
                if (id.StartsWith("_")) continue;

                var asset = LoadOrCreate<TechnologyAsset>("Technologies", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_tome").stringValue = row.Tome ?? "Kingdom";
                so.FindProperty("_placed").boolValue = row.IsPlaced;
                so.FindProperty("_era").intValue = (int)(row.Era ?? 1);
                so.FindProperty("_row").intValue = (int)(row.Row ?? 0);
                so.FindProperty("_column").intValue = (int)(row.Col ?? 0);
                SetStrings(so.FindProperty("_requires"), row.Requires);
                so.FindProperty("_knowledge").doubleValue = row.Knowledge ?? 0;

                var price = new Dictionary<string, double>();
                if (row.Gold > 0) price["Gold"] = row.Gold;
                foreach (var (currency, amount) in row.Materials ?? new Dictionary<string, double>()) price[currency] = amount;
                SetAmounts(so.FindProperty("_price"), price);
                SetAmounts(so.FindProperty("_goods"), row.Goods);
                so.FindProperty("_anyPrecious").intValue = (int)(row.AnyPrecious ?? 0);

                so.FindProperty("_kind").enumValueIndex = (int)(row.Kind switch
                {
                    "bonus" => TechKind.Bonus,
                    "mechanic" => TechKind.Mechanic,
                    _ => TechKind.Unlock,
                });
                var unlocks = (row.Unlocks ?? new List<TechUnlock>()).Select(ToUnlock).ToList();
                var list = so.FindProperty("_unlocks");
                list.arraySize = unlocks.Count;
                for (var i = 0; i < unlocks.Count; i++)
                {
                    var line = list.GetArrayElementAtIndex(i);
                    line.FindPropertyRelative("_kind").enumValueIndex = (int)unlocks[i].Kind;
                    line.FindPropertyRelative("_id").stringValue = unlocks[i].Id;
                    line.FindPropertyRelative("_level").intValue = unlocks[i].Level;
                }

                var effects = row.Effects ?? new List<TechEffect>();
                list = so.FindProperty("_effects");
                list.arraySize = effects.Count;
                for (var i = 0; i < effects.Count; i++)
                {
                    var line = list.GetArrayElementAtIndex(i);
                    var (target, targetId) = ToTarget(effects[i].Target);
                    line.FindPropertyRelative("_stat").stringValue = effects[i].Stat;
                    line.FindPropertyRelative("_op").enumValueIndex = (int)(effects[i].Op == "flat" ? EffectOp.Flat : EffectOp.Percent);
                    line.FindPropertyRelative("_value").doubleValue = effects[i].Value;
                    line.FindPropertyRelative("_target").enumValueIndex = (int)target;
                    line.FindPropertyRelative("_targetId").stringValue = targetId;
                }

                so.FindProperty("_planned").boolValue = row.Planned ?? false;
                so.FindProperty("_displayName").stringValue = row.Name;
                so.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Icons/{row.Icon}.png");
                so.FindProperty("_description").stringValue = row.Kind == "mechanic" ? row.Description : "";
                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<TechnologyCollection>(null, "Technologies"), assets);

            var shelf = new SerializedObject(LoadOrCreate<TechTreeAsset>("Settings", "TechTree"));
            var books = shelf.FindProperty("_books");
            books.arraySize = tree.Eras.Count;
            var b = 0;
            foreach (var (tome, cells) in tree.Eras)
            {
                var book = books.GetArrayElementAtIndex(b++);
                book.FindPropertyRelative("_id").stringValue = tome;
                var (emblem, tint) = BOOKS.TryGetValue(tome, out var mark) ? mark : ("research", "#efe2bd");
                book.FindPropertyRelative("_emblem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Icons/{emblem}.png");
                book.FindPropertyRelative("_tint").colorValue = ColorUtility.TryParseHtmlString(tint, out var color) ? color : Color.white;

                var rewards = tree.EraRewards != null && tree.EraRewards.TryGetValue(tome, out var paid) ? paid : new List<double?>();
                var eras = book.FindPropertyRelative("_eras");
                eras.arraySize = cells.Count;
                for (var e = 0; e < cells.Count; e++)
                {
                    var era = eras.GetArrayElementAtIndex(e);
                    var reward = e < rewards.Count ? rewards[e] : null;
                    era.FindPropertyRelative("_cellsToOpen").intValue = (int)cells[e];
                    era.FindPropertyRelative("_rewarded").boolValue = reward.HasValue;
                    era.FindPropertyRelative("_reward").intValue = (int)(reward ?? 0);
                }
            }
            shelf.ApplyModifiedPropertiesWithoutUndo();

            var knowledge = Read<ExplorationData>("Game/exploration.json").Knowledge;
            var settings = new SerializedObject(LoadOrCreate<KnowledgeSettingsAsset>("Settings", "Knowledge"));
            settings.FindProperty("_perHour").doubleValue = knowledge.BasePerHour;
            settings.FindProperty("_cap").doubleValue = knowledge.Cap;
            settings.FindProperty("_goldPriceBase").doubleValue = knowledge.GoldPriceBase;
            settings.FindProperty("_goldPriceExponent").doubleValue = knowledge.GoldPriceExponent;
            settings.FindProperty("_gemsPerPoint").doubleValue = knowledge.GemsPerPoint;
            settings.FindProperty("_landmarkClaimLump").doubleValue = knowledge.LandmarkClaimLump;
            settings.ApplyModifiedPropertiesWithoutUndo();

            return assets.Count;
        }

        // The chain, in its order. The web's separate reward fields (Gems, Stardust, Mana) join the currencies.
        private static void ImportQuests()
        {
            var assets = new List<DefinitionAsset>();
            foreach (var row in Read<List<QuestData>>("Game/quests.json"))
            {
                var asset = LoadOrCreate<QuestAsset>("Quests", row.Id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = row.Id;
                so.FindProperty("_name").stringValue = row.Name;
                so.FindProperty("_goalType").enumValueIndex = (int)System.Enum.Parse<Kingdom.Quests.GoalType>(row.GoalType);
                so.FindProperty("_goalTarget").stringValue = row.GoalTarget ?? "";
                so.FindProperty("_goalAmount").doubleValue = row.GoalAmount;
                so.FindProperty("_goalLevel").intValue = (int)(row.GoalLevel ?? 0);

                var reward = new Dictionary<string, double>(row.Reward ?? new Dictionary<string, double>());
                if (row.RewardMana > 0) reward["Mana"] = row.RewardMana;
                if (row.RewardGems > 0) reward["Gems"] = row.RewardGems;
                if (row.RewardStardust > 0) reward["Stardust"] = row.RewardStardust;
                SetAmounts(so.FindProperty("_reward"), reward);
                so.FindProperty("_rewardKnowledge").doubleValue = row.RewardKnowledge;
                SetAmounts(so.FindProperty("_rewardItems"), row.RewardItems);
                so.FindProperty("_rewardFragments").intValue = (int)row.RewardFragments;
                so.FindProperty("_autoClaim").boolValue = row.AutoClaim;
                so.FindProperty("_tutorialRentSeconds").doubleValue = row.TutorialRentSeconds ?? 0;
                so.FindProperty("_mark").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Icons/{GoalIcon(row)}.png");
                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<QuestCollection>(null, "Quests"), assets);
        }

        // The web's questIcon.ts: a goal's mark on the tracker.
        private static string GoalIcon(QuestData quest)
        {
            var target = quest.GoalTarget;
            string District() => target switch
            {
                null => "build",
                "AnyDecoration" => "harmony",
                "AnyProducer" => "build",
                "AnyHall" => "army",
                "AnyWorkshop" => "anvil",
                _ => target,
            };
            return quest.GoalType switch
            {
                "CollectResource" or "HoldResource" => target ?? "quest",
                "BuildDistrict" or "RepairDistrict" or "UpgradeDistrict" or "WorkInReach" => District(),
                "CompleteTech" or "CompleteTechs" => "research",
                "ReachPopulation" => "population",
                "AssignWorkers" => "workers",
                "TrainArmy" => "army",
                "CollectTaps" => "showme",
                "DiscoverCells" => "tile",
                "DiscoverFeature" => target switch
                {
                    "Trees" => "tree", "BerryBush" => "Berries", "WildAnimals" => "Meat", "FishShoal" => "Fish", "Mountain" => "Stone",
                    "MountainIron" => "Iron", "MountainGold" => "Gold", "Crops" => "FarmLands", _ => "compass",
                },
                "FindLairs" => "dungeon",
                "ClearLairs" => "power",
                "ClaimLandmarks" => target switch { "Leyspring" => "Mana", "Watchtower" => "Watchtower", _ => "landmark" },
                "OwnArtifacts" => "relics",
                "OwnHeroes" => "helmet",
                _ => "quest",
            };
        }

        private static void ImportTreasure()
        {
            var treasure = Read<ExplorationData>("Game/exploration.json").Treasure;
            var settings = new SerializedObject(LoadOrCreate<TreasureSettingsAsset>("Settings", "Treasure"));
            settings.FindProperty("_everyReveals").intValue = (int)treasure.EveryReveals;
            settings.FindProperty("_workSeconds").doubleValue = treasure.WorkSeconds;
            SetAmounts(settings.FindProperty("_floor"), treasure.Floor);
            SetAmounts(settings.FindProperty("_weights"), treasure.Weights);
            settings.FindProperty("_knowledge").doubleValue = treasure.Knowledge;
            settings.FindProperty("_firstCoin").stringValue = treasure.FirstCoin;
            settings.FindProperty("_firstAmount").doubleValue = treasure.FirstAmount;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // The abandoned buildings, as region-map.json places them.
        private static void ImportSites()
        {
            var map = Read<RegionMapDoc>("region-map.json");
            var asset = new SerializedObject(LoadOrCreate<ProvinceSitesAsset>("Settings", "ProvinceSites"));
            var list = asset.FindProperty("_abandoned");
            var abandoned = map.Abandoned ?? new List<AbandonedDoc>();
            list.arraySize = abandoned.Count;
            for (var i = 0; i < abandoned.Count; i++)
            {
                var site = list.GetArrayElementAtIndex(i);
                site.FindPropertyRelative("_id").stringValue = abandoned[i].Id;
                site.FindPropertyRelative("_district").stringValue = abandoned[i].District;
                site.FindPropertyRelative("_anchor").vector2IntValue = new Vector2Int(abandoned[i].X, abandoned[i].Y);
                site.FindPropertyRelative("_sight").intValue = (int)abandoned[i].Sight;
                site.FindPropertyRelative("_name").stringValue = abandoned[i].Name;
            }

            var landmarks = map.Landmarks ?? new List<LandmarkDoc>();
            list = asset.FindProperty("_landmarks");
            list.arraySize = landmarks.Count;
            for (var i = 0; i < landmarks.Count; i++)
            {
                var site = list.GetArrayElementAtIndex(i);
                site.FindPropertyRelative("_id").stringValue = landmarks[i].Id;
                site.FindPropertyRelative("_kind").stringValue = landmarks[i].Kind;
                site.FindPropertyRelative("_anchor").vector2IntValue = new Vector2Int(landmarks[i].X, landmarks[i].Y);
                site.FindPropertyRelative("_size").intValue = landmarks[i].Size ?? 1;
                site.FindPropertyRelative("_claimCost").doubleValue = landmarks[i].ClaimCost;
            }

            // The web's LANDMARK_ART: each kind's name and drawing.
            var kinds = new[] { ("StandingStones", "Standing stones", "landmark_stones"), ("Leyspring", "Leyspring", "landmark_leyspring") };
            list = asset.FindProperty("_landmarkKinds");
            list.arraySize = kinds.Length;
            for (var i = 0; i < kinds.Length; i++)
            {
                var kind = list.GetArrayElementAtIndex(i);
                kind.FindPropertyRelative("_kind").stringValue = kinds[i].Item1;
                kind.FindPropertyRelative("_name").stringValue = kinds[i].Item2;
                kind.FindPropertyRelative("_art").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Features/{kinds[i].Item3}.png");
            }

            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Kingdom.Research.TechUnlock ToUnlock(TechUnlock doc)
        {
            if (doc.District != null) return new(UnlockKind.District, doc.District);
            if (doc.DistrictLevel != null) return new(UnlockKind.DistrictLevel, doc.DistrictLevel.Id, (int)doc.DistrictLevel.Level);
            if (doc.DistrictCount != null) return new(UnlockKind.DistrictCount, doc.DistrictCount);
            if (doc.Unit != null) return new(UnlockKind.Unit, doc.Unit);
            if (doc.Evolution != null) return new(UnlockKind.Evolution, doc.Evolution.Unit, (int)doc.Evolution.Rank);
            if (doc.Harvest != null) return new(UnlockKind.Harvest, doc.Harvest);
            if (doc.Terrain != null) return new(UnlockKind.Terrain, doc.Terrain);
            return new(UnlockKind.WorldUpgrade, doc.WorldUpgrade);
        }

        private static (TargetKind, string) ToTarget(TechTarget doc)
        {
            if (doc == null) return (TargetKind.Global, null);
            if (doc.District != null) return (TargetKind.District, doc.District);
            if (doc.Unit != null) return (TargetKind.Unit, doc.Unit);
            if (doc.UnitTag != null) return (TargetKind.UnitTag, doc.UnitTag);
            if (doc.Harvest != null) return (TargetKind.Harvest, doc.Harvest);
            if (doc.Tome != null) return (TargetKind.Tome, doc.Tome);
            return doc.WorldDistrict != null ? (TargetKind.WorldDistrict, doc.WorldDistrict) : (TargetKind.Global, null);
        }

        private static T Read<T>(string file) => JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(WebData, file)));

        // The asset at Assets/Data/<folder>/<name>.asset, created when missing.
        private static T LoadOrCreate<T>(string folder, string name) where T : ScriptableObject
        {
            var directory = folder == null ? DataAssets.ROOT : $"{DataAssets.ROOT}/{folder}";
            Directory.CreateDirectory(directory);
            var path = $"{directory}/{name}.asset";

            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetEntries(DataCollection collection, IReadOnlyList<DefinitionAsset> entries)
        {
            var so = new SerializedObject(collection);
            var list = so.FindProperty("_entries");
            list.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAmounts(SerializedProperty list, Dictionary<string, double> amounts)
        {
            amounts ??= new Dictionary<string, double>();
            list.arraySize = amounts.Count;
            var i = 0;
            foreach (var (id, value) in amounts)
            {
                var line = list.GetArrayElementAtIndex(i++);
                line.FindPropertyRelative("_id").stringValue = id;
                line.FindPropertyRelative("_value").doubleValue = value;
            }
        }

        // The header's small cut of an icon when it has one, else the icon.
        private static Sprite CurrencyIcon(string id)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Icons/{id}-sm.png")
               ?? AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Icons/{id}.png");

        // A building's tiers, from its sprite stem: <stem>_l<n>.png from level n, or <stem>.png alone; a plantable,
        // which stands no building, shows what it plants (the web's cardArt), its feature's <stem>.png.
        private static void SetArt(SerializedProperty list, string stem)
        {
            var tiers = new List<(int Level, Sprite Sprite)>();
            for (var level = 1; level <= 20; level++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Buildings/{stem}_l{level}.png");
                if (sprite != null) tiers.Add((level, sprite));
            }

            var single = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Buildings/{stem}.png")
                         ?? AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Features/{stem}.png");
            if (tiers.Count == 0 && single != null) tiers.Add((1, single));

            list.arraySize = tiers.Count;
            for (var i = 0; i < tiers.Count; i++)
            {
                var tier = list.GetArrayElementAtIndex(i);
                tier.FindPropertyRelative("_fromLevel").intValue = tiers[i].Level;
                tier.FindPropertyRelative("_sprite").objectReferenceValue = tiers[i].Sprite;
            }
        }

        private static void SetStrings(SerializedProperty list, List<string> values)
        {
            values ??= new List<string>();
            list.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) list.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static void SetDoubles(SerializedProperty list, List<double> values)
        {
            values ??= new List<double>();
            list.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) list.GetArrayElementAtIndex(i).doubleValue = values[i];
        }

        private static void SetInts(SerializedProperty list, List<double> values)
        {
            values ??= new List<double>();
            list.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) list.GetArrayElementAtIndex(i).intValue = (int)values[i];
        }
    }
}
