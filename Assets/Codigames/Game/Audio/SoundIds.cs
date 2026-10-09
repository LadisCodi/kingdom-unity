namespace Codigames.Game.Audio
{
    // Ids of the game's sounds, the web prototype's names; each is a sound in the SoundCatalog.
    public static class SoundIds
    {
        public const string BUTTON_PRESS = "click";
        public const string ERROR = "error";
        public const string POP = "pop";
        public const string TAP_EMPTY = "tapEmpty";
        public const string TAP_HOUSE = "tapHouse";
        public const string BUILD_PLACED = "buildPlaced";
        public const string CONSTRUCTION_COMPLETE = "constructionComplete";
        public const string UPGRADE_BOUGHT = "upgradeBought";
        public const string VILLAGER_TRAINED = "villagerTrained";
        public const string REVEAL_PAID = "revealPaid";
        public const string REVEAL_DONE = "revealDone";
        public const string GHOST_LIFT = "ghostLift";
        public const string GHOST_STEP = "ghostStep";
        public const string GHOST_PLANT = "ghostPlant";
        public const string RESEARCH = "research";
        public const string RESEARCH_COMPLETE = "researchComplete";
        public const string GEM_SPEND = "gemSpend";
        public const string QUEST = "quest";
        public const string QUEST_COMPLETE = "questComplete";
        public const string CHAIN_FINISHED = "chainFinished";
        public const string SCROLL_OPEN = "scrollOpen";
        public const string SCROLL_CLOSE = "scrollClose";
        public const string UNLOCK = "unlock";
        public const string RAID_ALARM = "raidAlarm";
        public const string BATTLE_START = "battleStart";
        public const string SWORD_HIT = "swordHit";
        public const string ARROW_HIT = "arrowHit";
        public const string LANCE_HIT = "lanceHit";
        public const string CAVALRY_HIT = "cavalryHit";
        public const string CAVALRY_CHARGE = "cavalryCharge";
        public const string ARROW_LOOSE = "arrowLoose";
        public const string BOLT_CAST = "boltCast";
        public const string SKILL_CHARGE = "skillCharge";
        public const string HEAL = "heal";
        public const string DAZE = "daze";
        public const string SQUAD_DOWN = "squadDown";
        public const string SKULL_STAMP = "skullStamp";
        public const string FINAL_BLOW = "finalBlow";
        public const string VICTORY = "victory";
        public const string DEFEAT = "defeat";

        // What a tap on each kind of ground sounds like (and a crew's strike, quieter).
        public static string TapOn(string harvestSource) => harvestSource switch
        {
            "Forest" => "tapTree",
            "Berries" => "tapBerries",
            "Crops" => "tapBerries",
            "Meat" => "tapAnimals",
            "Stone" => "tapStone",
            "MountainIron" => "tapIron",
            "MountainGold" => "tapIron",
            "Fish" => "tapFish",
            _ => POP,
        };
    

        // The chest reveal.
        public const string BAR_FILL = "barFill";
        public const string CARD_DRAW = "cardDraw";
        public const string CARD_FLIP = "cardFlip";
        public const string CARD_IMPACT = "cardImpact";
        public const string CARD_REVEAL_COMMON = "cardRevealCommon";
        public const string CARD_REVEAL_LEGEND = "cardRevealLegend";
        public const string CARD_REVEAL_RARE = "cardRevealRare";
        public const string CARD_SETTLE = "cardSettle";
        public const string CARD_SPARKLE = "cardSparkle";
        public const string CARD_WHOOSH = "cardWhoosh";
        public const string CHEST_LAND = "chestLand";
        public const string CHEST_OPEN = "chestOpen";
        public const string CHEST_SUMMARY = "chestSummary";
        public const string CHEST_UNLOCK = "chestUnlock";
        public const string HERO_APPLAUSE = "heroApplause";
        public const string HERO_FANFARE = "heroFanfare";
        public const string HERO_FANFARE_LEGEND = "heroFanfareLegend";
        public const string HERO_LEGEND = "heroLegend";
        public const string HERO_NEW = "heroNew";
        public const string RELIC_WAKE = "relicWake";
        public const string HERO_POP = "heroPop";
        public const string HERO_RISER = "heroRiser";
        public const string HERO_RISER_SHORT = "heroRiserShort";
    

        // A fight's skills.
        public const string RIBBON = "ribbon";
        public const string VOLLEY = "volley";
        public const string AMBUSH = "ambush";
        public const string SHARPSHOT = "sharpshot";
        public const string CLEAVE = "cleave";
        public const string CRUSH = "crush";
        public const string WAR_CRY = "warCry";
        public const string BULWARK = "bulwark";
        public const string VIGOUR = "vigour";
        public const string SHIELD_SOAK = "shieldSoak";
        public const string SHIELD_BREAK = "shieldBreak";
        public const string SHIELD_UP = "shieldUp";
    }
}
