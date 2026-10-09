using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Kit
{
    // The kit's button (the web's k-btn): a slab or a round knob of one material, its label riding on it. It draws
    // its own states — rest, pressed (the face pushed in, the words dropping with it), off (the face drained, the
    // words dimmed) — so every button in the game looks and behaves the same. A label button is one height and
    // never narrower than its two ends and a word (the prefab's layout); its label carries the Button role.
    public class KitButton : Button
    {
        [SerializeField] private ButtonSkins _skins;
        [SerializeField] private ButtonMaterial _material = ButtonMaterial.Green;
        [SerializeField] private ButtonShape _shape = ButtonShape.Slab;
        [SerializeField] private Image _face;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private HorizontalLayoutGroup _padding;
        [SerializeField] private Vector2Int _restPadding = new(28, 34);
        [SerializeField] private int _press = 8;

        private SelectionState _state;
        private bool _latched;

        public ButtonMaterial Material
        {
            get => _material;
            set
            {
                _material = value;
                Paint(_state);
            }
        }

        // Held pushed in, as a filter that is on (the web's is-pressed).
        public bool Latched
        {
            get => _latched;
            set
            {
                _latched = value;
                Paint(_state);
            }
        }

        public string Label
        {
            get => _label.text;
            set => _label.text = value;
        }

        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _state = state;
            Paint(state);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            transition = Transition.None;
            Paint(interactable ? SelectionState.Normal : SelectionState.Disabled);
        }
#endif

        private void Paint(SelectionState state)
        {
            if (_skins == null || _face == null) return;
            var skin = _skins.Of(_material);
            if (skin == null) return;

            var off = state == SelectionState.Disabled;
            var down = state == SelectionState.Pressed || _latched;
            _face.sprite = off ? skin.Off(_shape) : down ? skin.Down(_shape) : skin.Rest(_shape);

            if (_label != null)
            {
                if (_shape == ButtonShape.Slab)
                {
                    var material = off ? _skins.OffLabel : skin.Label;
                    if (material != null) _label.fontSharedMaterial = material;
                    _label.color = off ? _skins.OffLabelColor : _skins.LabelColor;
                }
                else
                {
                    _label.color = off ? _skins.OffGlyph : _skins.GlyphOn(_material);
                }
            }

            if (_padding != null)
            {
                var drop = down ? _press : 0;
                var padding = _padding.padding;
                if (padding.top == _restPadding.x + drop) return;
                padding.top = _restPadding.x + drop;
                padding.bottom = _restPadding.y - drop;
                _padding.padding = padding;
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)_padding.transform);
            }
        }
    }
}
