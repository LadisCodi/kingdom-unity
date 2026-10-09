using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Research;
using UnityEngine;

namespace Codigames.Game.Data.Research
{
    // Every technology, in the tree's order — which is also the order bonuses are summed in.
    [CreateAssetMenu(fileName = "Technologies", menuName = "Kingdom/Data/Technology Collection")]
    public class TechnologyCollection : DefinitionCollection<ITechnology, TechnologyAsset>, ITechnologyCards
    {
        public override string Title => "Technologies";

        public ITechnologyCard Card(string id) => TryGet(id, out var tech) ? (ITechnologyCard)tech : null;

        public IReadOnlyList<ITechnologyCard> Cards => Entries.OfType<TechnologyAsset>().ToList<ITechnologyCard>();
    }
}
