using System;
using Codigames.Kingdom.Sites;
using Sirenix.OdinInspector;
using UnityEngine;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Data.Sites
{
    // A building standing in ruin where the fog took it, as authored on the map.
    [Serializable]
    public class AbandonedSiteData : IAbandonedSite
    {
        [SerializeField, Required] private string _id;
        [SerializeField, Required, Tooltip("The building it is.")] private string _district;
        [SerializeField, Tooltip("Its top-left cell.")] private Vector2Int _anchor;
        [SerializeField, MinValue(0), SuffixLabel("cells")] private int _sight = 3;
        [SerializeField] private string _name;

        public string Id => _id;
        public string District => _district;
        public ModuleVector2Int Anchor => new(_anchor.x, _anchor.y);
        public int Sight => _sight;
        public string Name => _name;
    }
}
