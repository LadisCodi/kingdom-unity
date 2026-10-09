using System.Collections.Generic;

namespace Codigames.Kingdom.City.State
{
    // The city: its buildings, the jobs its builders are on, and how many builders it has.
    public class CityState
    {
        public List<DistrictState> Districts { get; set; } = new();
        public List<ConstructionJob> Jobs { get; set; } = new();
        public int Builders { get; set; }

        // Villagers living in the city, housed in build order.
        public int Population { get; set; }

        // Villagers in training at the Townhall, in order.
        public List<Trainee> Trainees { get; set; } = new();

        // Villagers working for a building's crew.
        public List<Codigames.Kingdom.Crews.State.WorkerState> Workers { get; set; } = new();

        // Ids are handed out in order and never reused.
        public int NextId { get; set; } = 1;

        public string NewId(string prefix) => prefix + "-" + NextId++;
    }
}
