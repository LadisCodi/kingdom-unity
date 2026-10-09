using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Stage
{
    // THE POINTER (Docs/features/24-dialogue.md §4): a gloved hand bobbing over what a line is about — down at it
    // from above, or up from below when there is no room above — a soft blue halo breathing round a control, and
    // magic motes drifting slowly off it. Everything in the stage's own space, from a rect on the screen.
    public class StagePointer : MonoBehaviour
    {
        private const float HAND_DOWN_WIDTH = 120f;
        private const float HAND_UP_WIDTH = 144f;
        // Where the fingertip is across the glove: down, and up.
        private const float TIP_DOWN = 0.575f;
        private const float TIP_UP = 0.396f;
        private const float GAP = 24f;
        private const float BOB = 24f;
        private const float BOB_SECONDS = 0.7f;
        // Room the hand needs above its target before it points from below instead.
        private const float ROOM_ABOVE = 210f;
        private const float HALO_PAD = 18f;
        // How far the halo's art reaches past its rounded edge.
        private const float HALO_GLOW = 48f;
        private const float HALO_SECONDS = 1.4f;
        private const int MOTES = 14;

        [SerializeField] private RectTransform _hand;
        [SerializeField] private Image _handImage;
        [SerializeField] private Sprite _handDown;
        [SerializeField] private Sprite _handUp;
        [SerializeField] private RectTransform _halo;
        [SerializeField] private Image _haloImage;
        [SerializeField] private RectTransform _motes;
        [SerializeField] private Image _motePrefab;

        private readonly List<Mote> _moteList = new();

        private RectTransform Space => (RectTransform)transform;

        private void Awake()
        {
            for (var i = 0; i < MOTES; i++) _moteList.Add(new Mote(Instantiate(_motePrefab, _motes), i));
            _motePrefab.gameObject.SetActive(false);
            Hide();
        }

        public void Hide()
        {
            _hand.gameObject.SetActive(false);
            _halo.gameObject.SetActive(false);
            _motes.gameObject.SetActive(false);
        }

        // Over `screen` (a rect in screen pixels): the halo round it when it is a control, the hand and the motes when
        // it is the player's turn.
        public void Show(Rect screen, bool halo, bool pointing)
        {
            var rect = ToLocal(screen);
            _halo.gameObject.SetActive(halo);
            if (halo)
            {
                var pad = HALO_PAD + HALO_GLOW;
                _halo.anchoredPosition = rect.center;
                _halo.sizeDelta = rect.size + Vector2.one * pad * 2f;
                var pulse = 0.35f + 0.65f * (0.5f - 0.5f * Mathf.Cos(Time.unscaledTime / HALO_SECONDS * Mathf.PI * 2f));
                _haloImage.color = new Color(1, 1, 1, pulse);
            }

            _hand.gameObject.SetActive(pointing);
            _motes.gameObject.SetActive(pointing);
            if (!pointing) return;

            var above = Space.rect.yMax - rect.yMax > ROOM_ABOVE;
            var bob = BOB * (0.5f - 0.5f * Mathf.Cos(Time.unscaledTime / BOB_SECONDS * Mathf.PI));
            _handImage.sprite = above ? _handDown : _handUp;
            var width = above ? HAND_DOWN_WIDTH : HAND_UP_WIDTH;
            var sprite = _handImage.sprite;
            var height = sprite != null ? width * sprite.rect.height / sprite.rect.width : width;
            _hand.sizeDelta = new Vector2(width, height);
            _hand.pivot = new Vector2(above ? TIP_DOWN : TIP_UP, above ? 0f : 1f);
            _hand.anchoredPosition = above
                ? new Vector2(rect.center.x, rect.yMax + GAP + bob)
                : new Vector2(rect.center.x, rect.yMin - GAP - bob);

            _motes.anchoredPosition = rect.center;
            _motes.sizeDelta = rect.size;
            foreach (var mote in _moteList) mote.Tick(rect.size, Time.unscaledTime);
        }

        private Rect ToLocal(Rect screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Space, screen.min, null, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Space, screen.max, null, out var max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // A mote: it starts somewhere on the target's outline and drifts outward, and always a little upward — light
        // rises — fading in and out, each on its own clock so the drift never reads as a loop.
        private sealed class Mote
        {
            private const float SIZE = 36f;
            private readonly RectTransform _rect;
            private readonly Image _image;
            private readonly int _side;
            private readonly float _along;
            private readonly Vector2 _drift;
            private readonly float _scale;
            private readonly float _seconds;
            private readonly float _delay;

            public Mote(Image image, int index)
            {
                _image = image;
                _rect = image.rectTransform;
                _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
                _side = index % 4;
                _along = Random.value;
                var outward = 14f + Random.value * 22f;
                _drift = _side switch
                {
                    0 => new Vector2((Random.value - 0.5f) * 16f, outward),
                    1 => new Vector2(outward, 6f + Random.value * 14f),
                    2 => new Vector2((Random.value - 0.5f) * 16f, -(outward * 0.4f - 12f)),
                    _ => new Vector2(-outward, 6f + Random.value * 14f),
                } * 3f;
                _scale = 0.6f + Random.value * 0.7f;
                _seconds = 2.4f + Random.value * 2f;
                _delay = Random.value * 4f;
            }

            public void Tick(Vector2 size, float now)
            {
                var start = _side switch
                {
                    0 => new Vector2(_along - 0.5f, 0.5f),
                    1 => new Vector2(0.5f, 0.5f - _along),
                    2 => new Vector2(_along - 0.5f, -0.5f),
                    _ => new Vector2(-0.5f, 0.5f - _along),
                };
                var t = (now + _delay) % _seconds / _seconds;
                var eased = 1f - (1f - t) * (1f - t);
                _rect.anchoredPosition = Vector2.Scale(start, size) + _drift * eased;
                var scale = Mathf.Lerp(0.4f, 1f, eased) * _scale;
                _rect.sizeDelta = Vector2.one * SIZE * 2.4f;
                _rect.localScale = Vector3.one * scale;
                _rect.localRotation = Quaternion.Euler(0, 0, -45f * eased);
                var alpha = t < 0.18f ? t / 0.18f : t < 0.7f ? Mathf.Lerp(1f, 0.8f, (t - 0.18f) / 0.52f) : Mathf.Lerp(0.8f, 0f, (t - 0.7f) / 0.3f);
                _image.color = new Color(1, 1, 1, alpha);
            }
        }
    }
}
