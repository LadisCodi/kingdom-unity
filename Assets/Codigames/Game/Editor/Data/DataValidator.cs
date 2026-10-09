using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Research;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Research;

namespace Codigames.Game.Editor.Data
{
    // Everything wrong with the balance, by the rules Kingdom states: each entry, each settings group, and the
    // rules across collections. The data window shows it; a test and the build refuse it.
    public static class DataValidator
    {
        public static IEnumerable<string> Problems()
        {
            foreach (var collection in DataAssets.FindAll<DataCollection>())
            {
                foreach (var problem in Problems(collection)) yield return problem;
            }

            foreach (var settings in DataAssets.FindAll<DataSettings>())
            {
                foreach (var problem in settings.Problems()) yield return $"{settings.name}: {problem}";
            }

            var buildings = DataAssets.FindFirst<BuildingCollection>();
            var construction = DataAssets.FindFirst<ConstructionSettingsAsset>();
            if (buildings != null && construction != null)
            {
                foreach (var problem in BuildingRules.Problems(buildings.Entries.OfType<IBuildingDefinition>(), construction))
                    yield return $"{buildings.Title}: {problem}";
            }

            var technologies = DataAssets.FindFirst<TechnologyCollection>();
            var tree = DataAssets.FindFirst<TechTreeAsset>();
            if (technologies != null && tree != null)
            {
                foreach (var problem in TechTreeRules.Problems(technologies, tree)) yield return $"{technologies.Title}: {problem}";
            }
        }

        public static IEnumerable<string> Problems(DataCollection collection)
        {
            var seen = new HashSet<string>();

            foreach (var entry in collection.Entries)
            {
                if (entry == null)
                {
                    yield return $"{collection.Title}: an entry is missing.";
                    continue;
                }

                if (!seen.Add(entry.Id)) yield return $"{collection.Title}: two entries share the id \"{entry.Id}\".";

                foreach (var problem in entry.Problems()) yield return $"{collection.Title}/{entry.Id}: {problem}";
            }
        }
    }
}
