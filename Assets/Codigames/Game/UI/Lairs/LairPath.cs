using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Lairs
{
    // A lair's path (Docs/features/18-garrisons-and-raids.md §5): one stone a fight, joined by a dotted trail. A fight
    // won carries the green wax seal, the next is lit and glows, the ones ahead are dim, and the last is the boss's
    // horned stone.
    public class LairPath : MonoBehaviour
    {
        // Ahead: the web's brightness(0.8) saturate(0.6), as a tint.
        private static readonly Color AHEAD = new(0.74f, 0.72f, 0.7f, 1f);
        private static readonly Color TRAIL_AHEAD = new(1f, 1f, 1f, 0.45f);

        [SerializeField] private RectTransform _row;
        [SerializeField, Tooltip("A stone: its Glow, its Face and its Seal.")] private RectTransform _stone;
        [SerializeField] private Image _trail;
        [SerializeField] private Sprite _plain;
        [SerializeField] private Sprite _lit;
        [SerializeField] private Sprite _boss;
        [SerializeField, Tooltip("A stone's width on a short path; the boss's is wider.")] private float _stoneWidth = 168;
        [SerializeField] private float _bossWidth = 213;

        private readonly List<GameObject> _drawn = new();
        private readonly List<(LayoutElement Layout, bool Boss)> _stones = new();
        private int _fights;

        public void Show(int fights, int won)
        {
            foreach (var drawn in _drawn) Destroy(drawn);
            _drawn.Clear();
            _stones.Clear();
            _fights = fights;
            _stone.gameObject.SetActive(false);
            _trail.gameObject.SetActive(false);

            for (var i = 0; i < fights; i++)
            {
                if (i > 0)
                {
                    var trail = Instantiate(_trail, _row);
                    trail.gameObject.SetActive(true);
                    trail.color = new Color(_trail.color.r, _trail.color.g, _trail.color.b, i <= won ? 1f : TRAIL_AHEAD.a);
                    _drawn.Add(trail.gameObject);
                }

                var boss = i == fights - 1;
                var stone = Instantiate(_stone, _row);
                stone.gameObject.SetActive(true);
                var face = stone.Find("Face").GetComponent<Image>();
                face.sprite = boss ? _boss : i == won ? _lit : _plain;
                face.color = i > won ? AHEAD : Color.white;
                _stones.Add((stone.GetComponent<LayoutElement>(), boss));
                stone.Find("Glow").gameObject.SetActive(i == won);
                var seal = (RectTransform)stone.Find("Seal");
                seal.gameObject.SetActive(i < won);
                // The boss's seal sits in its bowl, clear of its horns.
                seal.anchorMin = boss ? new Vector2(0.24f, 0.30f) : new Vector2(0.16f, 0.16f);
                seal.anchorMax = boss ? new Vector2(0.76f, 0.82f) : new Vector2(0.84f, 0.84f);
                _drawn.Add(stone.gameObject);
            }

            Fit();
        }

        // A stone is as big as the path lets it be, so seven fights still fit: sized again whenever the path's width
        // settles, since a card is laid out after it is filled.
        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit()
        {
            if (_row == null || _fights == 0) return;
            var width = _row.rect.width;
            var room = width > 0 ? width / _fights : float.MaxValue;
            foreach (var (layout, boss) in _stones)
            {
                var size = Mathf.Min(boss ? _bossWidth : _stoneWidth, room * (boss ? 0.95f : 0.72f));
                layout.preferredWidth = size;
                layout.preferredHeight = boss ? size : size * 229f / 256f;
            }
        }
    }
}
