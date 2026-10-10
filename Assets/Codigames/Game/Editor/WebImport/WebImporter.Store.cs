using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data;
using Codigames.Game.Data.Store;
using Codigames.Kingdom.Store;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.WebImport
{
    public static partial class WebImporter
    {
        private const string STORE_ART = "Assets/Art/UI/Store/";

        // store.json's products, whole, in its order; monetization.json's payer budgets and offers' draw.
        private static void ImportStore()
        {
            var rows = Read<Dictionary<string, ProductData>>("Game/store.json");
            var assets = new List<DefinitionAsset>();
            foreach (var (id, row) in rows)
            {
                var asset = LoadOrCreate<ProductAsset>("Store", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_name").stringValue = row.Name ?? "";
                so.FindProperty("_description").stringValue = row.Description ?? "";
                so.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{STORE_ART}{row.Sprite}.png");
                so.FindProperty("_art").objectReferenceValue = string.IsNullOrEmpty(row.Art) ? null : AssetDatabase.LoadAssetAtPath<Sprite>($"{STORE_ART}{row.Art}.png");
                so.FindProperty("_shelf").enumValueIndex = (int)Enum.Parse<ProductShelf>(row.Shelf, true);
                so.FindProperty("_priceUsd").doubleValue = row.PriceUsd;
                so.FindProperty("_gems").intValue = (int)row.Gems;
                SetAmounts(so.FindProperty("_items"), row.Items);
                so.FindProperty("_hero").stringValue = row.Hero ?? "";
                so.FindProperty("_builders").intValue = (int)row.Builders;
                so.FindProperty("_explorers").intValue = (int)row.Explorers;
                so.FindProperty("_heroSlots").intValue = (int)row.HeroSlots;
                so.FindProperty("_nextDayGems").intValue = (int)row.NextDayGems;
                so.FindProperty("_nextDayHeroXp").intValue = (int)row.NextDayHeroXp;
                so.FindProperty("_nextDayFragments").intValue = (int)row.NextDayFragments;
                SetAmounts(so.FindProperty("_nextDayItems"), row.NextDayItems);
                so.FindProperty("_opensOn").stringValue = row.OpensOn ?? "always";
                so.FindProperty("_door").stringValue = row.Door ?? "";
                so.FindProperty("_after").stringValue = row.After ?? "";
                so.FindProperty("_townhall").intValue = (int)row.Townhall;
                so.FindProperty("_hours").doubleValue = row.Hours;
                so.FindProperty("_limit").intValue = (int)row.Limit;
                so.FindProperty("_cooldownHours").doubleValue = row.CooldownHours;
                so.FindProperty("_splash").boolValue = row.Splash;
                so.FindProperty("_widget").boolValue = row.Widget;
                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<ProductCollection>(null, "Products"), assets);

            var payer = Read<MonetizationData>("Game/monetization.json");
            var settingsAsset = LoadOrCreate<PayerSettingsAsset>("Settings", "Payer");
            var settings = new SerializedObject(settingsAsset);
            settings.FindProperty("_f2p").doubleValue = payer.Payer.F2pMonthlyUsd;
            settings.FindProperty("_minnow").doubleValue = payer.Payer.MinnowMonthlyUsd;
            settings.FindProperty("_dolphin").doubleValue = payer.Payer.DolphinMonthlyUsd;
            settings.FindProperty("_whale").doubleValue = payer.Payer.WhaleMonthlyUsd;
            settings.FindProperty("_superWhale").doubleValue = payer.Payer.SuperWhaleMonthlyUsd;
            settings.FindProperty("_dailyOffers").intValue = (int)payer.Offers.DailyCount;
            settings.FindProperty("_offerSpacingHours").doubleValue = payer.Offers.SpacingHours;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settingsAsset);
            AssetDatabase.SaveAssetIfDirty(settingsAsset);
        }
    }
}
