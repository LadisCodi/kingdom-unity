using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Sites;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Sites
{
    // What the province holds besides its ground: its abandoned buildings, its landmarks and its lairs, by cell, and how each
    // kind of landmark is shown.
    [CreateAssetMenu(fileName = "ProvinceSites", menuName = "Kingdom/Data/Province Sites")]
    public class ProvinceSitesAsset : DataSettings, IProvinceSites
    {
        [ListDrawerSettings(ShowFoldout = false)]
        [SerializeField] private List<AbandonedSiteData> _abandoned = new();

        [ListDrawerSettings(ShowFoldout = false)]
        [SerializeField] private List<LandmarkSiteData> _landmarks = new();

        [ListDrawerSettings(ShowFoldout = false)]
        [SerializeField] private List<LairSiteData> _lairs = new();

        [BoxGroup("Presentation"), ListDrawerSettings(ShowFoldout = false)]
        [SerializeField] private List<LandmarkKindData> _landmarkKinds = new();

        [System.Serializable]
        public class CreatureFace
        {
            public string Unit;
            [PreviewField(48)] public Sprite Face;
        }

        [BoxGroup("Presentation"), ListDrawerSettings(ShowFoldout = false), Tooltip("The creature an enemy squad of each unit is, wherever a lair fields it.")]
        [SerializeField] private List<CreatureFace> _creatureFaces = new();

        private List<IAbandonedSite> _sites;
        private List<ILandmarkSite> _landmarkSites;
        private List<Kingdom.Lairs.ILairSite> _lairSites;

        public IReadOnlyList<IAbandonedSite> Abandoned => _sites ??= _abandoned.ToList<IAbandonedSite>();

        public IReadOnlyList<ILandmarkSite> Landmarks => _landmarkSites ??= _landmarks.ToList<ILandmarkSite>();

        public IReadOnlyList<Kingdom.Lairs.ILairSite> Lairs => _lairSites ??= _lairs.ToList<Kingdom.Lairs.ILairSite>();

        public LairSiteData LairOf(string id) => _lairs.FirstOrDefault(l => l.Id == id);

        public Sprite CreatureOf(string unit) => _creatureFaces.FirstOrDefault(c => c.Unit == unit)?.Face;

        public LandmarkKindData KindOf(string kind) => _landmarkKinds.FirstOrDefault(k => k.Kind == kind);

        public override IEnumerable<string> Problems()
        {
            foreach (var group in _abandoned.GroupBy(s => s.Id).Where(g => g.Count() > 1)) yield return $"Two abandoned buildings share the id {group.Key}.";
        }

        private void OnValidate()
        {
            _sites = null;
            _landmarkSites = null;
            _lairSites = null;
        }
    }
}
