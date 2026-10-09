using System.Collections.Generic;
using Codigames.Game.Data.Lairs;
using Codigames.Game.Data.Sites;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.WebImport
{
    public static partial class WebImporter
    {
        // What a lair is beside its numbers: the web keeps it in code (definitions.ts lairContent), its art by id.
        private static readonly (string Id, string Name, string Description, string Art, string Creature)[] LAIR_CONTENT =
        {
            ("Orcs", "Orc Lair", "Orcs dug in on the hillside, and bored of waiting.", "orcs", "orc"),
            ("Harpies", "Harpy Roost", "Harpies on the high rocks, watching everything that shines.", "harpies", "harpy"),
            ("Goblins", "Goblin Den", "Goblins with sharp sticks and sharper ideas about your stores.", "goblins", "goblin"),
            ("WolfRiders", "Wolf-rider Camp", "Wolf riders who reach your walls before the dust of their riding does.", "wolfriders", "wolfrider"),
            ("Drake", "Drake's Lair", "A drake asleep on a hoard it means to make larger.", "drake", "drake"),
        };

        // The lairs as region-map.json places them, and their garrisons and raids.
        private static void ImportLairs()
        {
            var map = Read<RegionMapDoc>("region-map.json");
            var asset = new SerializedObject(LoadOrCreate<ProvinceSitesAsset>("Settings", "ProvinceSites"));
            var list = asset.FindProperty("_lairs");
            list.arraySize = LAIR_CONTENT.Length;
            for (var i = 0; i < LAIR_CONTENT.Length; i++)
            {
                var content = LAIR_CONTENT[i];
                var doc = map.Lairs[content.Id];
                var lair = list.GetArrayElementAtIndex(i);
                lair.FindPropertyRelative("_id").stringValue = content.Id;
                lair.FindPropertyRelative("_name").stringValue = content.Name;
                lair.FindPropertyRelative("_description").stringValue = content.Description;
                lair.FindPropertyRelative("_flavour").stringValue = doc.Flavour;
                lair.FindPropertyRelative("_anchor").vector2IntValue = new Vector2Int(doc.X, doc.Y);
                lair.FindPropertyRelative("_size").intValue = doc.Size ?? 2;
                lair.FindPropertyRelative("_tier").intValue = (int)doc.Tier;
                lair.FindPropertyRelative("_radius").intValue = (int)doc.Radius;
                lair.FindPropertyRelative("_sight").intValue = (int)doc.Sight;
                lair.FindPropertyRelative("_threat").stringValue = doc.Guard.Threat;
                lair.FindPropertyRelative("_power").intValue = (int)doc.Guard.Power;
                lair.FindPropertyRelative("_warningMinutes").doubleValue = doc.Guard.WarningMinutes;
                SetAmounts(lair.FindPropertyRelative("_mix"), doc.Guard.Mix);
                lair.FindPropertyRelative("_model").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Features/lair_{content.Art}.png");
                lair.FindPropertyRelative("_painting").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Lairs/lair_art_{content.Art}.png");
                lair.FindPropertyRelative("_medal").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/World/creature_{content.Creature}_medal.png");
                lair.FindPropertyRelative("_creature").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Portraits/creature_{content.Creature}_avatar.png");
            }

            asset.ApplyModifiedPropertiesWithoutUndo();

            var garrisons = Read<List<GarrisonData>>("Game/garrisons.json");
            var exploration = Read<ExplorationData>("Game/exploration.json");
            var settings = new SerializedObject(LoadOrCreate<LairSettingsAsset>("Settings", "Lairs"));
            list = settings.FindProperty("_garrisons");
            garrisons.Sort((a, b) => a.Tier.CompareTo(b.Tier));
            list.arraySize = garrisons.Count;
            for (var i = 0; i < garrisons.Count; i++)
            {
                var garrison = list.GetArrayElementAtIndex(i);
                garrison.FindPropertyRelative("Fights").intValue = (int)garrisons[i].Fights;
                garrison.FindPropertyRelative("TakeSeconds").doubleValue = garrisons[i].TakeSeconds;
                garrison.FindPropertyRelative("HeroXp").doubleValue = garrisons[i].HeroXp;
                SetAmounts(garrison.FindPropertyRelative("RewardItems"), garrisons[i].RewardItems);
            }

            settings.FindProperty("_takeFractionMax").doubleValue = exploration.Raid.TakeFractionMax;
            settings.FindProperty("_raidsPerDay").intValue = (int)exploration.Raid.PerDay;
            settings.FindProperty("_windowStartHour").doubleValue = exploration.Raid.WindowStartHour;
            settings.FindProperty("_windowEndHour").doubleValue = exploration.Raid.WindowEndHour;
            settings.FindProperty("_firstFightPower").doubleValue = exploration.Delve.FirstFightPower;
            settings.FindProperty("_firstClearKnowledge").doubleValue = exploration.Delve.FirstClearKnowledge;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
