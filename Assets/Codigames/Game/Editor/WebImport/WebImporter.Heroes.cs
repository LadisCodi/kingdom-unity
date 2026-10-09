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
        // definitions.ts heroContent), and the ladder from heroLadder.json and combat.json's party.
        private static void ImportHeroes()
        {
            var web = Read<Dictionary<string, HeroData>>("Game/heroes.json");
            var content = Read<Dictionary<string, HeroContent>>("hero-content.json");
            var assets = new List<DefinitionAsset>();
            foreach (var (id, row) in web)
            {
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
                so.FindProperty("_bagRank").intValue = (int)(row.BagRank ?? 1);
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
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
