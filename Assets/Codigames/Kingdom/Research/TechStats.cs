namespace Codigames.Kingdom.Research
{
    // The numbers the tree can move that something in the game reads, by the name a technology's effect gives
    // them. A stat is read where its number is computed; one nothing reads yet is not here.
    public static class TechStats
    {
        // The thumb and the crew.
        public const string TAP_WORK_SECONDS = "tapWorkSeconds";
        public const string HARVEST_YIELD = "harvestYield";
        public const string REGROWTH_SPEED = "regrowthSpeed";
        public const string CREW_YIELD = "crewYield";
        public const string WORKER_SPEED = "workerSpeed";
        public const string CELL_STOCK = "cellStock";
        public const string RESPAWN_SPEED = "respawnSpeed";
        public const string CREW_STRIKE_SPEED = "crewStrikeSpeed";
        public const string CREW_SLOTS = "crewSlots";
        public const string INFLUENCE_RADIUS = "influenceRadius";

        // The city.
        public const string BUILD_SPEED = "buildSpeed";
        public const string STORAGE_CAPACITY = "storageCapacity";
        public const string TAX_RATE = "taxRate";
        public const string POPULATION_CAPACITY = "populationCapacity";
        public const string VILLAGER_TRAINING_SPEED = "villagerTrainingSpeed";
        public const string OWN_GOLD = "ownGold";

        // Magic and sight.
        public const string MANA_CAP = "manaCap";
        public const string MANA_REGEN = "manaRegen";
        public const string DISCOVER_RADIUS = "discoverRadius";
    }
}
