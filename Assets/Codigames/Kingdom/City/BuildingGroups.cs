using Codigames.Kingdom.Quests;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.City
{
    // A target that names one kind of building, or a group by its token: membership read off what a building is,
    // never authored — a decoration supplies Harmony, a producer has harvest sources.
    public class BuildingGroups : IBuildingGroups
    {
        public const string ANY_DECORATION = "AnyDecoration";
        public const string ANY_PRODUCER = "AnyProducer";
        public const string ANY_HALL = "AnyHall";
        public const string ANY_WORKSHOP = "AnyWorkshop";

        private readonly ICatalog<IBuildingDefinition> _buildings;

        public BuildingGroups(ICatalog<IBuildingDefinition> buildings) => _buildings = buildings;

        public bool Names(string target, string definitionId)
        {
            if (target == null || !_buildings.TryGet(definitionId, out var building)) return false;

            return target switch
            {
                ANY_DECORATION => building.Production.HarmonySupply > 0,
                ANY_PRODUCER => building.Production.HarvestSources.Count > 0,
                ANY_HALL => building.Production.ArmyCapPerLevel.Count > 0,
                ANY_WORKSHOP => !string.IsNullOrEmpty(building.Production.Produces),
                _ => definitionId == target,
            };
        }
    }
}
