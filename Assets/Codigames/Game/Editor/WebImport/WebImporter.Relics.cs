using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data;
using Codigames.Game.Data.Relics;
using Codigames.Kingdom.Relics;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.WebImport
{
    public static partial class WebImporter
    {
        private sealed class RelicContent
        {
            public sealed class StatContent
            {
                public string stat;
                public string op;
            }

            public sealed class SpellContent
            {
                public string name;
                public string text;
            }

            public string name;
            public string sprite;
            public string text;
            public string story;
            public List<StatContent> stats;
            public SpellContent spell;
            public string pending;
        }

        private const string RELIC_ART = "Assets/Art/UI/Relics/";

        // The relics from artifacts.json, their names, art, sentences and the numbers they move from the web's code
        // (relic-content.json, taken from definitions.ts ARTIFACTS, in its ARTIFACT_ORDER), and their rules from
        // relics.json.
        private static void ImportRelics()
        {
            var web = Read<Dictionary<string, ArtifactData>>("Game/artifacts.json");
            var content = Read<Dictionary<string, RelicContent>>("relic-content.json");
            var assets = new List<DefinitionAsset>();
            foreach (var id in content.Keys.Where(web.ContainsKey))
            {
                var row = web[id];
                var c = content[id];
                var asset = LoadOrCreate<RelicAsset>("Relics", id);
                var so = new SerializedObject(asset);
                var city = row.Kind == "city";
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_name").stringValue = c.name;
                so.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{RELIC_ART}{c.sprite}.png");
                var fragments = so.FindProperty("_fragments");
                fragments.arraySize = Kingdom.Relics.Relics.SLOTS;
                for (var i = 0; i < Kingdom.Relics.Relics.SLOTS; i++)
                    fragments.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{RELIC_ART}{c.sprite}_frag{i}.png");
                so.FindProperty("_text").stringValue = c.text;
                so.FindProperty("_story").stringValue = c.story ?? "";
                so.FindProperty("_pending").stringValue = c.pending ?? "";
                so.FindProperty("_kind").enumValueIndex = (int)(city ? RelicKind.City : RelicKind.World);
                so.FindProperty("_door").stringValue = row.Door;
                var stats = so.FindProperty("_stats");
                stats.arraySize = c.stats.Count;
                for (var i = 0; i < c.stats.Count; i++)
                {
                    var entry = stats.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("Stat").stringValue = c.stats[i].stat;
                    entry.FindPropertyRelative("Add").boolValue = c.stats[i].op == "add";
                }

                so.FindProperty("_passiveBase").doubleValue = row.PassiveBase;
                so.FindProperty("_passivePerLevel").doubleValue = row.PassivePerLevel;
                so.FindProperty("_activationMana").intValue = city ? (int)row.ActiveManaCost : 0;
                so.FindProperty("_activationRadius").intValue = city ? (int)row.ActiveRadius : 0;
                so.FindProperty("_spellName").stringValue = c.spell?.name ?? "";
                so.FindProperty("_spellText").stringValue = c.spell?.text ?? "";
                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<RelicCollection>(null, "Relics"), assets);

            var rules = Read<RelicRulesData>("Game/relics.json");
            var settingsAsset = LoadOrCreate<RelicSettingsAsset>("Settings", "Relics");
            var settings = new SerializedObject(settingsAsset);
            var cycle = settings.FindProperty("_cycle");
            cycle.arraySize = rules.CityLevels.Cycle.Count;
            for (var i = 0; i < rules.CityLevels.Cycle.Count; i++)
                cycle.GetArrayElementAtIndex(i).enumValueIndex = (int)System.Enum.Parse<RelicAxis>(rules.CityLevels.Cycle[i], true);
            SetInts(settings.FindProperty("_windowMinutes"), rules.CityLevels.WindowMinutes);
            settings.FindProperty("_keystoneOneIn").intValue = (int)rules.Fragments.KeystoneOneIn;
            settings.FindProperty("_levelStardustBase").doubleValue = rules.Fragments.LevelStardustBase;
            settings.FindProperty("_levelStardustGrowth").doubleValue = rules.Fragments.LevelStardustGrowth;
            settings.FindProperty("_fragmentPackGems").intValue = (int)rules.Fragments.FragmentPackGems;
            settings.FindProperty("_fragmentPackSize").intValue = (int)rules.Fragments.FragmentPackSize;
            settings.FindProperty("_treasureEvery").intValue = (int)rules.Fragments.TreasureEvery;
            SetInts(settings.FindProperty("_perLairTier"), rules.Fragments.PerLairTier);
            settings.FindProperty("_shrineMaterialBuilds").intValue = (int)rules.Shrines.MaterialBuilds;
            SetInts(settings.FindProperty("_shrinePremiumGems"), rules.Shrines.PremiumGems);
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settingsAsset);
            AssetDatabase.SaveAssetIfDirty(settingsAsset);
        }
    }
}
