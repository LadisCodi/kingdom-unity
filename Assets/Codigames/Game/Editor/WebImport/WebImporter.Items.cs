using System.Collections.Generic;
using Codigames.Game.Data;
using Codigames.Game.Data.Bag;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Economy;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.WebImport
{
    // The Bag's items, in their order, each with its picture: its own drawing when the atlas has one, else the chest of
    // its coin, the boost of its kind, the flask, the tome, the winged hourglass.
    public static partial class WebImporter
    {
        private const string ICONS = "Assets/Art/UI/Icons/";

        private static void ImportItems()
        {
            var assets = new List<DefinitionAsset>();
            foreach (var (id, row) in Read<Dictionary<string, ItemData>>("Game/items.json"))
            {
                var asset = LoadOrCreate<ItemAsset>("Items", id);
                var so = new SerializedObject(asset);
                var kind = System.Enum.Parse<ItemKind>(row.Kind, true);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_name").stringValue = row.Name;
                so.FindProperty("_kind").enumValueIndex = (int)kind;
                so.FindProperty("_tier").intValue = (int)row.Tier;
                so.FindProperty("_coin").stringValue = row.Coin ?? "";
                so.FindProperty("_seconds").doubleValue = row.Seconds;
                if (row.Speeds != null) so.FindProperty("_speeds").enumValueIndex = (int)System.Enum.Parse<SpeedupKind>(row.Speeds);
                if (row.Boost != null) so.FindProperty("_boost").enumValueIndex = (int)System.Enum.Parse<BoostKind>(row.Boost);
                so.FindProperty("_value").doubleValue = row.Value;
                so.FindProperty("_icon").objectReferenceValue = ItemIcon(id, kind, row);
                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<ItemCollection>(null, "Items"), assets);
        }

        private static Sprite ItemIcon(string id, ItemKind kind, ItemData row)
        {
            var own = AssetDatabase.LoadAssetAtPath<Sprite>($"{ICONS}{id}.png");
            if (own != null) return own;
            var stem = kind switch
            {
                ItemKind.Chest => "chest" + row.Coin,
                ItemKind.Choice => "choiceChest",
                ItemKind.Boost => "boost" + row.Boost,
                ItemKind.Flask => "flask",
                ItemKind.Tome => "tome",
                ItemKind.Part => "shard",
                _ => "speedup",
            };
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ICONS}{stem}.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ICONS}chest.png");
        }
    }
}
