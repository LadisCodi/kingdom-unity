using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Sites;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Sites
{
    // What the province holds besides its ground: its abandoned buildings, by cell.
    [CreateAssetMenu(fileName = "ProvinceSites", menuName = "Kingdom/Data/Province Sites")]
    public class ProvinceSitesAsset : DataSettings, IProvinceSites
    {
        [ListDrawerSettings(ShowFoldout = false)]
        [SerializeField] private List<AbandonedSiteData> _abandoned = new();

        private List<IAbandonedSite> _sites;

        public IReadOnlyList<IAbandonedSite> Abandoned => _sites ??= _abandoned.ToList<IAbandonedSite>();

        public override IEnumerable<string> Problems()
        {
            foreach (var group in _abandoned.GroupBy(s => s.Id).Where(g => g.Count() > 1)) yield return $"Two abandoned buildings share the id {group.Key}.";
        }

        private void OnValidate() => _sites = null;
    }
}
