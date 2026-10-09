using System.Collections.Generic;
using Codigames.Kingdom.Research;

namespace Codigames.Game.UI.Research
{
    // What a player is told about each number the tree can move, one sentence per op (the web's TECH_STATS
    // `says`). Filled in by TechProse: {v} the signed amount, {pct} a fraction read as a percentage, {target}
    // what it aims at, {resource} the coin a harvest target pays; a [ … ] segment is dropped when the bonus
    // is unaimed. English source text: the localizer translates it.
    public static class TechStatSentences
    {
        private static readonly Dictionary<(string, EffectOp), string> SAYS = new()
        {
            [("tapWorkSeconds", EffectOp.Percent)] = "{v} out of every tap",
            [("harvestYield", EffectOp.Percent)] = "{v}[ {resource}] per tap and delivery[ from {target}]",
            [("regrowthSpeed", EffectOp.Percent)] = "{v} regrowth speed[ for {target}]",
            [("crewYield", EffectOp.Percent)] = "{v} on every worker delivery",
            [("workerSpeed", EffectOp.Percent)] = "{v} worker walking speed",
            [("cellStock", EffectOp.Percent)] = "{v}[ {resource}] held in every cell[ of {target}]",
            [("respawnSpeed", EffectOp.Percent)] = "{v} speed coming back[ for {target}]",
            [("crewStrikeSpeed", EffectOp.Percent)] = "{v} work speed for the crew[ of the {target}]",
            [("crewSlots", EffectOp.Flat)] = "{v} worker slots[ at the {target}]",
            [("influenceRadius", EffectOp.Flat)] = "{v} reach for the crew[ of the {target}]",
            [("buildSpeed", EffectOp.Percent)] = "{v} build speed",
            [("storageCapacity", EffectOp.Percent)] = "{v} storage[ in {target}]",
            [("taxRate", EffectOp.Percent)] = "{v} tax income[ from {target}]",
            [("populationCapacity", EffectOp.Flat)] = "{v} bed space[ at {target}]",
            [("villagerTrainingSpeed", EffectOp.Percent)] = "{v} villager training speed",
            [("workshopSpeed", EffectOp.Percent)] = "{v} workshop speed[ at {target}]",
            [("workshopQueueSlots", EffectOp.Flat)] = "{v} order slots in the queue[ of the {target}]",
            [("ownGold", EffectOp.Percent)] = "{v} Gold the Townhall makes by itself",
            [("decorationHarmony", EffectOp.Flat)] = "{v} Harmony from every[ {target}] decoration",
            [("decorationHarmony", EffectOp.Percent)] = "{v} Harmony from every[ {target}] decoration",
            [("manaCap", EffectOp.Percent)] = "{v} to the Mana the kingdom holds",
            [("manaRegen", EffectOp.Percent)] = "{v} Mana regeneration",
            [("knowledgeYield", EffectOp.Percent)] = "{v} on every lump of Knowledge",
            [("landmarkKnowledge", EffectOp.Percent)] = "{v} Knowledge from every landmark claimed",
            [("lairKnowledge", EffectOp.Percent)] = "{v} Knowledge from every lair cleared",
            [("discoverRadius", EffectOp.Flat)] = "{v} sight into the fog for every[ {target}] building",
            [("treasureYield", EffectOp.Percent)] = "{v} from every treasure found in the fog",
            [("explorerSlots", EffectOp.Flat)] = "{v} to the explorers out at once",
            [("worldRevealRadius", EffectOp.Flat)] = "{v} to how far an explorer sees round the hex it explores",
            [("explorerSpeed", EffectOp.Percent)] = "{v} explorer speed on the world board",
            [("exploreSpeed", EffectOp.Percent)] = "{v} exploring speed on the world board",
            [("armyMarchSpeed", EffectOp.Percent)] = "{v} army marching speed on the world board",
            [("improvementYield", EffectOp.Percent)] = "{v} from every[ {target}] world district",
            [("improvementStore", EffectOp.Percent)] = "{v} storage in every[ {target}] world district",
            [("worldBuildSpeed", EffectOp.Percent)] = "{v} building speed on the world board",
            [("worldRepairSpeed", EffectOp.Percent)] = "{v} repair speed for burnt districts",
            [("fortressSlots", EffectOp.Flat)] = "{v} to the Fortresses you may build",
            [("chapelSlots", EffectOp.Flat)] = "{v} to the Chapels you may build",
            [("campLoot", EffectOp.Percent)] = "{v} loot from monster camps",
            [("dungeonLoot", EffectOp.Percent)] = "{v} loot from dungeon rooms",
            [("portalLoot", EffectOp.Percent)] = "{v} loot from the Dark Portal’s floors",
            [("worldHeroXp", EffectOp.Percent)] = "{v} Hero XP from fights on the world board",
            [("scoutReward", EffectOp.Percent)] = "{v} from every hex your explorers reveal",
            [("armyCap", EffectOp.Percent)] = "{v} soldiers the halls can hold",
            [("recruitSpeed", EffectOp.Percent)] = "{v} training speed[ for the {target}]",
            [("unitAtk", EffectOp.Percent)] = "{v} damage for every[ {target}] unit",
            [("unitDef", EffectOp.Percent)] = "{v} defence for every[ {target}] unit",
            [("unitHp", EffectOp.Percent)] = "{v} health for every unit",
            [("infirmaryBeds", EffectOp.Percent)] = "{v} beds in the Infirmary",
            [("healSpeed", EffectOp.Percent)] = "{v} healing speed in the Infirmary",
            [("heroXp", EffectOp.Percent)] = "{v} Hero XP",
            [("summonStardust", EffectOp.Percent)] = "{v} Stardust from calls",
        };

        // Null when the stat has no sentence for this op.
        public static string For(string stat, EffectOp op) => SAYS.TryGetValue((stat, op), out var says) ? says : null;

        public static IEnumerable<string> All => SAYS.Values;
    }
}
