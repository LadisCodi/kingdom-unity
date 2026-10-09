using System;
using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // One material's art: its slab and its knob, each at rest, pressed and off, and the label material whose
    // outline is a darker tone of that slab.
    [Serializable]
    public class ButtonSkin
    {
        [SerializeField] private ButtonMaterial _material;
        [SerializeField] private Sprite _slab;
        [SerializeField] private Sprite _slabDown;
        [SerializeField] private Sprite _slabOff;
        [SerializeField] private Sprite _knob;
        [SerializeField] private Sprite _knobDown;
        [SerializeField] private Sprite _knobOff;
        [SerializeField] private Material _label;

        public ButtonSkin()
        {
        }

        public ButtonSkin(ButtonMaterial material, Sprite slab, Sprite slabDown, Sprite slabOff, Sprite knob, Sprite knobDown,
            Sprite knobOff, Material label)
        {
            _material = material;
            _slab = slab;
            _slabDown = slabDown;
            _slabOff = slabOff;
            _knob = knob;
            _knobDown = knobDown;
            _knobOff = knobOff;
            _label = label;
        }

        public ButtonMaterial Material => _material;
        public Material Label => _label;

        public Sprite Rest(ButtonShape shape) => shape == ButtonShape.Slab ? _slab : _knob;
        public Sprite Down(ButtonShape shape) => shape == ButtonShape.Slab ? _slabDown : _knobDown;
        public Sprite Off(ButtonShape shape) => shape == ButtonShape.Slab ? _slabOff : _knobOff;
    }
}
