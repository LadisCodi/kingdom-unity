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
    }
}
