using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data;
using Codigames.Game.Data.Heroes;
using Codigames.Kingdom.Heroes;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.WebImport
{
    public static partial class WebImporter
    {
        private sealed class HeroContent
        {
            public string name;
            public string title;
            public string sprite;
        }

        // The heroes from heroes.json, their names and art from the web's code (hero-content.json, taken from
        // definitions.ts heroContent, in its HERO_ORDER — the roster's order and the bag's), the ladder from
        // heroLadder.json and combat.json's party, and the banners.
        private static void ImportHeroes()
        {
            var web = Read<Dictionary<string, HeroData>>("Game/heroes.json");
            var content = Read<Dictionary<string, HeroContent>>("hero-content.json");
            var assets = new List<DefinitionAsset>();
            foreach (var id in content.Keys.Where(web.ContainsKey).Concat(web.Keys.Where(k => !content.ContainsKey(k))))
            {
                var row = web[id];
                var asset = LoadOrCreate<HeroAsset>("Heroes", id);
                var so = new SerializedObject(asset);
                var c = content.TryGetValue(id, out var found) ? found : new HeroContent { name = id, title = "", sprite = "hero_" + id.ToLowerInvariant() };
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_name").stringValue = c.name;
                so.FindProperty("_title").stringValue = c.title;
                so.FindProperty("_art").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Heroes/{c.sprite}.png");
                so.FindProperty("_portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Heroes/{c.sprite}_avatar.png");
                so.FindProperty("_fragment").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Heroes/{c.sprite}_fragment.png");
                so.FindProperty("_rarity").enumValueIndex = (int)System.Enum.Parse<HeroRarity>(row.Rarity);
                so.FindProperty("_bagRank").intValue = (int)(row.BagRank ?? 0);
                so.FindProperty("_unitType").stringValue = row.UnitType;
                so.FindProperty("_skill").stringValue = row.Skill;
                so.FindProperty("_skillValue").doubleValue = row.SkillValue;
                so.FindProperty("_skillEvery").doubleValue = row.SkillEvery;
                so.FindProperty("_atk").doubleValue = row.Atk;
                so.FindProperty("_dmg").doubleValue = row.Dmg;
                so.FindProperty("_def").doubleValue = row.Def;
                so.FindProperty("_hp").doubleValue = row.Hp;
                so.FindProperty("_cooldown").intValue = (int)row.Cooldown;
                so.FindProperty("_atkPerLevel").doubleValue = row.AtkPerLevel;
                so.FindProperty("_dmgPerLevel").doubleValue = row.DmgPerLevel;
                so.FindProperty("_defPerLevel").doubleValue = row.DefPerLevel;
                so.FindProperty("_hpPerLevel").doubleValue = row.HpPerLevel;
                so.FindProperty("_troopDmgMult").doubleValue = row.TroopDmgMult;
                so.FindProperty("_troopHpMult").doubleValue = row.TroopHpMult;
                so.FindProperty("_troopDefBonus").intValue = (int)row.TroopDefBonus;
                so.FindProperty("_boonStat").stringValue = row.Boon?.Stat ?? "";
                so.FindProperty("_boonValue").doubleValue = row.Boon?.Value ?? 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<HeroCollection>(null, "Heroes"), assets);

            var ladder = Read<HeroLadderSettingsData>("Game/heroLadder.json").HeroLadder;
            var party = Read<CombatData>("Game/combat.json").Party;
            var settings = new SerializedObject(LoadOrCreate<HeroLadderSettingsAsset>("Settings", "HeroLadder"));
            void Int(string field, double v) => settings.FindProperty(field).intValue = (int)v;
            void Dbl(string field, double v) => settings.FindProperty(field).doubleValue = v;
            Int("_ascensionStars", ladder.AscensionStars);
            Int("_ascensionStepsPerStar", ladder.AscensionStepsPerStar);
            Dbl("_fragmentsPerStepBase", ladder.FragmentsPerStepBase);
            Dbl("_fragmentsPerStepGrowth", ladder.FragmentsPerStepGrowth);
            Dbl("_ascensionStardustBase", ladder.AscensionStardustBase);
            Dbl("_ascensionStardustGrowth", ladder.AscensionStardustGrowth);
            Dbl("_statsPerAscension", ladder.StatsPerAscension);
            Int("_recruitCommon", ladder.RecruitFragments.Common);
            Int("_recruitRare", ladder.RecruitFragments.Rare);
            Int("_recruitLegendary", ladder.RecruitFragments.Legendary);
            Dbl("_xpLevelCostBase", ladder.XpLevelCostBase);
            Dbl("_xpLevelCostGrowth", ladder.XpLevelCostGrowth);
            Int("_heroLevelsPerStar", ladder.HeroLevelsPerStar);
            Int("_heroLevelsPerAscension", ladder.HeroLevelsPerAscension);
            Int("_heroMaxLevel", ladder.HeroMaxLevel);
            SetInts(settings.FindProperty("_skillRankLevels"), ladder.SkillRankLevels);
            SetDoubles(settings.FindProperty("_skillRankStardust"), ladder.SkillRankStardust);
            SetDoubles(settings.FindProperty("_skillRankMaterial"), ladder.SkillRankMaterial);
            Dbl("_skillRankStep", ladder.SkillRankStep);
            Int("_heroSlots", party.HeroSlots);
            Dbl("_heroSlotGemCostBase", party.HeroSlotGemCostBase);
            Dbl("_heroSlotGemCostGrowth", party.HeroSlotGemCostGrowth);
            Dbl("_heroRecoverHours", party.HeroRecoverHours);
            Int("_bagOpenCommon", ladder.BagOpen.Common);
            Int("_bagOpenRare", ladder.BagOpen.Rare);
            Int("_bagOpenLegendary", ladder.BagOpen.Legendary);
            Int("_firstCallsNewHero", ladder.FirstCallsNewHero);
            settings.ApplyModifiedPropertiesWithoutUndo();

            ImportBanners();
        }

        // banners.json, in its order: the standard banner first.
        private static void ImportBanners()
        {
            var web = Read<Dictionary<string, BannerData>>("Game/banners.json");
            var assets = new List<DefinitionAsset>();
            foreach (var (id, row) in web)
            {
                var asset = LoadOrCreate<BannerAsset>("Banners", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_key").stringValue = row.Key;
                so.FindProperty("_keyGemCost").doubleValue = row.KeyGemCost;
                so.FindProperty("_freePerDay").intValue = (int)row.FreePerDay;
                so.FindProperty("_freeCooldownSeconds").doubleValue = row.FreeCooldownSeconds;
                SetDoubles(so.FindProperty("_heroChanceByOwned"), row.HeroChanceByOwned);
                so.FindProperty("_softPityAt").intValue = (int)row.SoftPityAt;
                so.FindProperty("_hardPityAt").intValue = (int)row.HardPityAt;
                so.FindProperty("_legendaryPityAt").intValue = (int)row.LegendaryPityAt;
                so.FindProperty("_weightCommon").doubleValue = row.Weights.Common;
                so.FindProperty("_weightRare").doubleValue = row.Weights.Rare;
                so.FindProperty("_weightLegendary").doubleValue = row.Weights.Legendary;
                so.FindProperty("_duplicateFragments").intValue = (int)row.DuplicateFragments;
                so.FindProperty("_extraHeroSlotChance").doubleValue = row.ExtraHeroSlotChance;
                so.FindProperty("_showsHero").boolValue = row.ShowsHero;
                so.FindProperty("_featuredHero").stringValue = row.FeaturedHero ?? "";
                var loot = so.FindProperty("_loot");
                loot.arraySize = row.Loot.Count;
                for (var i = 0; i < row.Loot.Count; i++)
                {
                    var entry = loot.GetArrayElementAtIndex(i);
                    var cell = row.Loot[i];
                    entry.FindPropertyRelative("Reward").enumValueIndex = (int)System.Enum.Parse<LootReward>(cell.Reward);
                    entry.FindPropertyRelative("Rarity").enumValueIndex = string.IsNullOrEmpty(cell.Rarity)
                        ? 0 : (int)System.Enum.Parse<BannerAsset.LootRarity>(cell.Rarity);
                    entry.FindPropertyRelative("Item").stringValue = cell.Item ?? "";
                    entry.FindPropertyRelative("Amount").intValue = (int)cell.Amount;
                    entry.FindPropertyRelative("Weight").doubleValue = cell.Weight;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<BannerCollection>(null, "Banners"), assets);
        }
    }
}
