using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Sites;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Quests
{
    // The absolute goals, read off the kingdom. A build counts the moment it starts (it cannot be cancelled), and a
    // repaired ruin counts as its building. What is not in the game yet (an army, lairs, relics, heroes) reads 0.
    public class QuestGoals : IQuestGoals, IBuildingGroups
    {

        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly City.BuildingGroups _groups;
        private readonly ITreasury _treasury;
        private readonly Researching _research;
        private readonly Workforce _crews;
        private readonly Landmarks _landmarks;
        private readonly FogOfWar _fog;
        private readonly IPlanting _planting;

        public QuestGoals(CityState city, ICatalog<IBuildingDefinition> buildings, ITreasury treasury, Researching research, Workforce crews,
            Landmarks landmarks, FogOfWar fog, IPlanting planting)
        {
            _planting = planting;
            _city = city;
            _buildings = buildings;
            _groups = new City.BuildingGroups(buildings);
            _treasury = treasury;
            _research = research;
            _crews = crews;
            _landmarks = landmarks;
            _fog = fog;
        }

        public double Value(IQuestDefinition quest)
        {
            var target = quest.GoalTarget;
            switch (quest.GoalType)
            {
                case GoalType.BuildDistrict:
                case GoalType.RepairDistrict:
                    return _city.Districts.Count(d => Names(target, d.DefinitionId)) + Planted(target);
                case GoalType.UpgradeDistrict:
                    return _city.Districts.Count(d => Names(target, d.DefinitionId) && d.Built && d.Level >= System.Math.Max(1, quest.GoalLevel));
                case GoalType.HoldResource:
                    return target == null ? 0 : _treasury.Get(target);
                case GoalType.ReachPopulation:
                    return _city.Population;
                case GoalType.CompleteTech:
                    return target != null && _research.IsComplete(target) ? 1 : 0;
                case GoalType.CompleteTechs:
                    return _research.Completed.Count;
                case GoalType.AssignWorkers:
                    return _city.Workers.Count;
                case GoalType.WorkInReach:
                    // The most its crew could work, of every one of its kind standing where it stands.
                    return _city.Districts.Where(d => d.DefinitionId == target).Select(d => _crews.Workable(d).Count).DefaultIfEmpty(0).Max();
                case GoalType.ClaimLandmarks:
                    return _landmarks.All.Count(l => _landmarks.IsClaimed(l.Id) && (target == null || l.Kind == target));
                case GoalType.DiscoverCells:
                    return _fog.RevealedCount;
                default:
                    return 0;
            }
        }

        // A plantable stands as its feature on the ground, never as a district.
        private int Planted(string target)
        {
            if (target == null || !_buildings.TryGet(target, out var building) || building.Production.Plants == null) return 0;
            return _planting.Count(building.Production.Plants);
        }

        // A goal names one kind of building, or a group of them by its token.
        public bool Names(string target, string definitionId) => _groups.Names(target, definitionId);
    }
}
