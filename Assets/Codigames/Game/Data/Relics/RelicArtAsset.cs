using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Relics
{
    // The relics' own pictures that no single relic owns: the Shrine's chapel, empty and holding, and what an awake
    // aura is drawn with on the ground — its glow, its sigils and its motes.
    [CreateAssetMenu(fileName = "RelicArt", menuName = "Kingdom/Data/Relic Art")]
    public class RelicArtAsset : ScriptableObject
    {
        [SerializeField, PreviewField(64)] private Sprite _shrineInterior;
        [SerializeField, PreviewField(64)] private Sprite _shrineInteriorEmpty;
        [SerializeField, PreviewField(64)] private Sprite _spellGlow;
        [SerializeField, PreviewField(64)] private Sprite _spellSigil;
        [SerializeField, PreviewField(64)] private Sprite _spellMote;
        [SerializeField, PreviewField(64), Tooltip("The soft light round an awake relic over its Shrine.")] private Sprite _halo;
        [SerializeField, PreviewField(64), Tooltip("A sleeping Shrine's bubble, on the lair bubble's shape.")] private Sprite _bubble;
        [SerializeField, PreviewField(64)] private Sprite _bubbleTail;

        public Sprite ShrineInterior => _shrineInterior;
        public Sprite ShrineInteriorEmpty => _shrineInteriorEmpty;
        public Sprite SpellGlow => _spellGlow;
        public Sprite SpellSigil => _spellSigil;
        public Sprite SpellMote => _spellMote;
        public Sprite Halo => _halo;
        public Sprite Bubble => _bubble;
        public Sprite BubbleTail => _bubbleTail;
    }
}
