using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Lairs;
using Sirenix.OdinInspector;
using UnityEngine;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Data.Sites
{
    // A lair, as authored on the map: where it stands, the ground it holds, its tier and its guard — and how it is
    // shown: its model on the map, its painting on the card, the creature on its bubble.
    [Serializable]
    public class LairSiteData : ILairSite
    {
        [SerializeField, Required] private string _id;
        [SerializeField] private string _name;
        [SerializeField, TextArea] private string _description;
        [SerializeField, TextArea] private string _flavour;
        [SerializeField, Tooltip("Its top-left cell.")] private Vector2Int _anchor;
        [SerializeField, Range(1, 3)] private int _size = 2;
        [SerializeField, Range(1, 5)] private int _tier = 1;
        [SerializeField, MinValue(0), SuffixLabel("cells round it")] private int _radius = 2;
        [SerializeField, MinValue(0), SuffixLabel("cells")] private int _sight = 3;

        [BoxGroup("Guard"), SerializeField, Tooltip("The unit it fields most; Any for a drake.")] private string _threat = "Warrior";
        [BoxGroup("Guard"), SerializeField, MinValue(1)] private int _power = 60;
        [BoxGroup("Guard"), SerializeField, MinValue(0), SuffixLabel("min to the first raid")] private double _warningMinutes = 30;
        [BoxGroup("Guard"), SerializeField, Tooltip("What it fields, as weights by unit; empty = the threat takes the lion's share.")]
        private List<Amount> _mix = new();

        [BoxGroup("Art"), SerializeField, PreviewField(48)] private Sprite _model;
        [BoxGroup("Art"), SerializeField, PreviewField(48)] private Sprite _painting;
        [BoxGroup("Art"), SerializeField, PreviewField(48)] private Sprite _creature;
        [BoxGroup("Art"), SerializeField, PreviewField(48), Tooltip("The creature on its parchment medallion: its warning bubble's face.")]
        private Sprite _medal;

        public string Id => _id;
        public string Name => _name;
        public string Description => _description;
        public string Flavour => _flavour;
        public ModuleVector2Int Anchor => new(_anchor.x, _anchor.y);
        public int Size => _size;
        public int Tier => _tier;
        public int Radius => _radius;
        public int Sight => _sight;
        public string Threat => _threat;
        public int Power => _power;
        public double WarningMinutes => _warningMinutes;
        public IReadOnlyDictionary<string, double> Mix => _mix.ToDictionary(a => a.Id, a => a.Value);
        public Sprite Model => _model;
        public Sprite Painting => _painting;
        public Sprite Creature => _creature;
        public Sprite Medal => _medal;
    }
}
