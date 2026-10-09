using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codigames.Game.Data;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Economy;
using Codigames.Game.Data.Harvest;
using Codigames.Game.Data.Magic;
using Codigames.Game.Editor.Data;
using Codigames.Kingdom.Economy;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Codigames.Game.Editor.WebImport
{
    // One-off: brings the web prototype's balance (Tools/WebData) into the data assets. Each collection is
    // imported when its system is rebuilt; once imported, the assets are the source of truth and are edited
    // in the data window. Re-running it overwrites the imported collections with the web's numbers.
    public static class WebImporter
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

        private static readonly (string Id, string Source, string RespawnTerrain)[] FEATURES =
        {
            ("Trees", "Forest", "Grassland"), ("Mountain", "Stone", "Grassland"), ("MountainIron", "MountainIron", "Grassland"),
            ("MountainGold", "MountainGold", "Grassland"), ("BerryBush", "Berries", "Grassland"), ("WildAnimals", "Meat", "Grassland"),
            ("FishShoal", "Fish", "Water"), ("Crops", "Crops", "Grassland"),
        };

        [MenuItem("Kingdom/Import web prototype data")]
        public static void ImportAll()
        {
            var currencies = ImportCurrencies();
            var buildings = ImportBuildings();
            ImportConstruction(buildings);
            ImportHarvest();
            ImportEconomy();
            ImportMagic();

            AssetDatabase.SaveAssets();
            Debug.Log($"#Data# Imported {currencies} currencies and {buildings.Count} buildings from the web prototype.");
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
                so.ApplyModifiedPropertiesWithoutUndo();
                sources[id] = asset;
            }
            SetEntries(LoadOrCreate<HarvestSourceCollection>(null, "HarvestSources"), sources.Values.ToList<DefinitionAsset>());

            var features = new List<DefinitionAsset>();
            foreach (var (id, source, respawnTerrain) in FEATURES)
            {
                var asset = LoadOrCreate<FeatureAsset>("Features", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_source").objectReferenceValue = sources[source];
                so.FindProperty("_respawnTerrain").stringValue = respawnTerrain;
                so.FindProperty("_tile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/Art/Features/Tiles/{id}.asset");
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

        private static void ImportEconomy()
        {
            var economy = Read<EconomyData>("Game/economy.json");
            var settings = new SerializedObject(LoadOrCreate<EconomySettingsAsset>("Settings", "Economy"));
            settings.FindProperty("_goldPerPopulationPerMinute").doubleValue = economy.Taxes.GoldPerPopulationPerMinute;
            settings.FindProperty("_collectSeconds").doubleValue = economy.Storage.CollectSeconds;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // The pool's base, and a new kingdom's Mana: a full pool.
        private static void ImportMagic()
        {
            var mana = Read<EconomyData>("Game/economy.json").Mana;
            var settings = new SerializedObject(LoadOrCreate<ManaSettingsAsset>("Settings", "Mana"));
            settings.FindProperty("_baseCap").doubleValue = mana.BaseCap;
            settings.FindProperty("_basePerHour").doubleValue = mana.BasePerHour;
            settings.ApplyModifiedPropertiesWithoutUndo();

            var currency = new SerializedObject(LoadOrCreate<CurrencyAsset>("Currencies", "Mana"));
            currency.FindProperty("_start").doubleValue = mana.BaseCap;
            currency.ApplyModifiedPropertiesWithoutUndo();
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

        // A building's tiers, from its sprite stem: <stem>_l<n>.png from level n, or <stem>.png alone.
        private static void SetArt(SerializedProperty list, string stem)
        {
            var tiers = new List<(int Level, Sprite Sprite)>();
            for (var level = 1; level <= 20; level++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Buildings/{stem}_l{level}.png");
                if (sprite != null) tiers.Add((level, sprite));
            }

            var single = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Buildings/{stem}.png");
            if (tiers.Count == 0 && single != null) tiers.Add((1, single));

            list.arraySize = tiers.Count;
            for (var i = 0; i < tiers.Count; i++)
            {
                var tier = list.GetArrayElementAtIndex(i);
                tier.FindPropertyRelative("_fromLevel").intValue = tiers[i].Level;
                tier.FindPropertyRelative("_sprite").objectReferenceValue = tiers[i].Sprite;
            }
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
