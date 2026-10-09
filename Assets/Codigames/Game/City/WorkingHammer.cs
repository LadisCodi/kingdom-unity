using UnityEngine;

namespace Codigames.Game.City
{
    // The builder's hammer at work over a building being built or a plot being sown, as the web draws it: a
    // 3.2-second loop of a wind-up, a blow that sparks, a flurry of taps and two more blows, written in an 84 × 75
    // frame whose top-left corner sits at `Place`'s point. Each site has its own phase, so two never hammer in step.
    public class WorkingHammer : MonoBehaviour
    {
        [SerializeField, Tooltip("Turns about the handle's end.")] private Transform _pivot;
        [SerializeField] private SpriteRenderer _hammer;
        [SerializeField] private SpriteRenderer[] _specks = new SpriteRenderer[4];

        private float _unit = 0.01f;
        private float _phase;

        // The frame `width` world units wide, its top-left corner at `topLeft` (local to the parent); `id` names the
        // site, for its phase.
        public void Place(Vector3 topLeft, float width, string id)
        {
            transform.localPosition = topLeft;
            _unit = width / HammerMotion.FRAME_WIDTH;
            _phase = HammerMotion.Phase(id);
            var size = HammerMotion.HAMMER_SIZE * _unit;
            var sprite = _hammer.sprite;
            if (sprite != null)
            {
                var native = sprite.rect.width / sprite.pixelsPerUnit;
                _hammer.transform.localScale = Vector3.one * (size / native);
            }

            // The handle's end (15% across, 90% down the art) on the pivot.
            _hammer.transform.localPosition = new Vector3((0.5f - 0.15f) * size, (0.9f - 0.5f) * size, 0f);
            foreach (var speck in _specks) speck.transform.localScale = Vector3.one * (6f * _unit / Native(speck));
        }

        private void Update()
        {
            var k = HammerMotion.At(Time.time, _phase);
            var rotation = HammerMotion.Rotation(k);
            var shift = HammerMotion.Shift(k);
            var size = HammerMotion.HAMMER_SIZE * _unit;
            _pivot.localPosition = new Vector3((35 + shift.x) * _unit + size * 0.15f, -((-16 + shift.y) * _unit + size * 0.9f), 0f);
            _pivot.localRotation = Quaternion.Euler(0, 0, -rotation);

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
                    speck.transform.localPosition = new Vector3((burst.X + burst.Reach * direction.x * p) * _unit,
                        -(burst.Y + burst.Reach * direction.y * p) * _unit, 0f);
                    speck.transform.localScale = Vector3.one * (6f * _unit * (1 - 0.6f * p) / Native(speck));
                    var colour = speck.color;
                    speck.color = new Color(colour.r, colour.g, colour.b, 1 - p);
                }
            }

            for (var i = shown; i < _specks.Length; i++) _specks[i].enabled = false;
        }

        private static float Native(SpriteRenderer renderer)
            => renderer.sprite == null ? 1f : renderer.sprite.rect.width / renderer.sprite.pixelsPerUnit;

    }
}
