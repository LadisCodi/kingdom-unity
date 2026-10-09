using System;
using Codigames.Kingdom.Sites;
using Sirenix.OdinInspector;
using UnityEngine;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Data.Sites
{
    // A sanctuary of the province, as authored on the map.
    [Serializable]
    public class LandmarkSiteData : ILandmarkSite
    {
        [SerializeField, Required] private string _id;
        [SerializeField, Required] private string _kind;
        [SerializeField, Tooltip("Its top-left cell.")] private Vector2Int _anchor;
        [SerializeField, Range(1, 3)] private int _size = 1;
        [SerializeField, MinValue(1), SuffixLabel("Gold")] private double _claimCost = 10000;

        public string Id => _id;
        public string Kind => _kind;
        public ModuleVector2Int Anchor => new(_anchor.x, _anchor.y);
        public int Size => _size;
        public double ClaimCost => _claimCost;
    }
}
