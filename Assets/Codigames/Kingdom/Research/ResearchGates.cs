using System.Collections.Generic;
using Codigames.Kingdom.Research.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Research
{
    // Every gate, derived from what the technologies say they open: nothing else names a technology. A
    // technology left off its page opens nothing.
    public class ResearchGates : IResearchGates
    {
        private readonly ResearchState _state;
        private readonly Dictionary<(UnlockKind, string, int), string> _gates = new();

        public ResearchGates(ResearchState state, ICatalog<ITechnology> technologies)
        {
            _state = state;
            foreach (var tech in technologies.Items)
            {
                if (!tech.IsPlaced) continue;

                foreach (var unlock in tech.Unlocks) _gates[(unlock.Kind, unlock.Id, unlock.Level)] = tech.Id;
            }
        }

        public string DistrictTech(string district) => Gate(UnlockKind.District, district);

        public string LevelTech(string district, int level) => Gate(UnlockKind.DistrictLevel, district, level);

        public string ExtraCountTech(string district) => Gate(UnlockKind.DistrictCount, district);

        public string UnitTech(string unit) => Gate(UnlockKind.Unit, unit);

        public string HarvestTech(string source) => Gate(UnlockKind.Harvest, source);

        public string TerrainTech(string terrain) => Gate(UnlockKind.Terrain, terrain);

        public bool IsOpen(string tech) => tech == null || _state.Completed.Contains(tech);

        private string Gate(UnlockKind kind, string id, int level = 0)
            => id != null && _gates.TryGetValue((kind, id, level), out var tech) ? tech : null;
    }
}
