using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codigames.Game.Data;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Economy;
using Codigames.Game.Editor.Data;
using Codigames.Kingdom.Economy;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

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

        [MenuItem("Kingdom/Import web prototype data")]
        public static void ImportAll()
        {
            var currencies = ImportCurrencies();
            var buildings = ImportBuildings();
            ImportConstruction(buildings);

            AssetDatabase.SaveAssets();
            Debug.Log($"#Data# Imported {currencies} currencies and {buildings.Count} buildings from the web prototype.");
        }

        private static int ImportCurrencies()
        {
            var web = Read<Dictionary<string, CurrencyData>>("Game/currencies.json");
            var assets = new List<CurrencyAsset>();

            foreach (var (id, row) in web)
            {
                var asset = LoadOrCreate<CurrencyAsset>("Currencies", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_scope").enumValueIndex = (int)SCOPES[id];
                so.FindProperty("_start").doubleValue = row.Start;
                so.FindProperty("_capped").boolValue = row.Cap.HasValue;
                so.FindProperty("_cap").doubleValue = row.Cap ?? 0;
                so.FindProperty("_primary").boolValue = row.Primary;
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

        private static void SetInts(SerializedProperty list, List<double> values)
        {
            values ??= new List<double>();
            list.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) list.GetArrayElementAtIndex(i).intValue = (int)values[i];
        }
    }
}
