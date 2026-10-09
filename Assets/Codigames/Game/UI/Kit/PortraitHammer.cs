using Codigames.Game.City;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Kit
{
    // The builder's hammer over a portrait while its building is being built (the web's dc-work): the map hammer's
    // loop (HammerMotion), drawn in the portrait's own frame — its top-left corner this object's origin.
    public class PortraitHammer : MonoBehaviour
    {
        [SerializeField] private RectTransform _frame;
        [SerializeField, Tooltip("Turns about the handle's end.")] private RectTransform _pivot;
        [SerializeField] private RectTransform _hammer;
        [SerializeField] private Image[] _specks = new Image[4];

        private float _phase;

        // Works for the building `id` (its phase, so two never hammer in step).
        public void Work(string id)
        {
            _phase = HammerMotion.Phase(id);
            gameObject.SetActive(true);
        }

        public void Rest() => gameObject.SetActive(false);

        private void Update()
        {
            var unit = _frame.rect.width / HammerMotion.FRAME_WIDTH;
            var size = HammerMotion.HAMMER_SIZE * unit;
            var k = HammerMotion.At(Time.time, _phase);
            var shift = HammerMotion.Shift(k);

            _hammer.sizeDelta = new Vector2(size, size);
            // The handle's end (15% across, 90% down the art) on the pivot.
            _hammer.anchoredPosition = new Vector2((0.5f - 0.15f) * size, (0.9f - 0.5f) * size);
            _pivot.anchoredPosition = new Vector2((35 + shift.x) * unit + size * 0.15f, -((-16 + shift.y) * unit + size * 0.9f));
            _pivot.localRotation = Quaternion.Euler(0, 0, -HammerMotion.Rotation(k));

            var shown = 0;
            foreach (var burst in HammerMotion.BURSTS)
            {
                var p = (k - burst.At) / burst.Life;
                if (p < 0 || p > 1) continue;
                foreach (var direction in HammerMotion.SPECKS)
                {
                    if (shown >= _specks.Length) break;
                    var speck = _specks[shown++];
                    speck.enabled = true;
                    var rect = speck.rectTransform;
                    rect.anchoredPosition = new Vector2((burst.X + burst.Reach * direction.x * p) * unit,
                        -(burst.Y + burst.Reach * direction.y * p) * unit);
                    rect.sizeDelta = Vector2.one * (6f * unit * (1 - 0.6f * p));
                    var colour = speck.color;
                    speck.color = new Color(colour.r, colour.g, colour.b, 1 - p);
                }
            }

            for (var i = shown; i < _specks.Length; i++) _specks[i].enabled = false;
        }
    }
}
