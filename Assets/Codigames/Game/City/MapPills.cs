using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Codigames.Game.City
{
    // Labels on the map (the web's yield pills): a dark capsule with a figure and its icon, under a cell's centre,
    // white unless the figure is good (green) or bad (red). Drawn over everything on the map, the ghost included:
    // nothing may paint over a number the player is reading.
    public class MapPills : MonoBehaviour
    {
        public enum Tone
        {
            Plain,
            Good,
            Bad,
        }

        [SerializeField] private Sprite _capsule;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Color _fill = new(18 / 255f, 16 / 255f, 14 / 255f, 0.88f);
        [SerializeField] private Color _plain = Color.white;
        [SerializeField] private Color _good = new Color32(0x9d, 0xff, 0x9d, 0xff);
        [SerializeField] private Color _bad = new Color32(0xff, 0x9a, 0x86, 0xff);
        [SerializeField, Tooltip("Sorting order over the map and the ghost.")] private int _order = 5000;

        private readonly List<(SpriteRenderer Pill, TextMeshPro Text)> _pills = new();

        // Each label at its spot (the bottom-centre of its pill), its height in world units.
        public void Show(IReadOnlyList<(Vector3 At, string Text, Tone Tone)> labels, float height)
        {
            for (var i = 0; i < labels.Count; i++)
            {
                if (i == _pills.Count) _pills.Add(Make());
                var (pill, text) = _pills[i];
                pill.gameObject.SetActive(true);
                text.text = labels[i].Text;
                text.fontSize = height / 1.45f * 10;
                text.color = labels[i].Tone == Tone.Good ? _good : labels[i].Tone == Tone.Bad ? _bad : _plain;
                text.ForceMeshUpdate();
                var width = text.preferredWidth + height * 0.7f;
                pill.size = new Vector2(width, height);
                pill.transform.position = labels[i].At + new Vector3(0, height / 2, 0);
            }

            for (var i = labels.Count; i < _pills.Count; i++) _pills[i].Pill.gameObject.SetActive(false);
        }

        public void Hide()
        {
            foreach (var (pill, _) in _pills) pill.gameObject.SetActive(false);
        }

        private (SpriteRenderer, TextMeshPro) Make()
        {
            var pill = new GameObject("Pill", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            pill.transform.SetParent(transform, false);
            pill.sprite = _capsule;
            pill.drawMode = SpriteDrawMode.Sliced;
            pill.color = _fill;
            pill.sortingOrder = _order;
            var text = new GameObject("Text", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            text.transform.SetParent(pill.transform, false);
            text.font = _font;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontStyle = FontStyles.Bold;
            text.rectTransform.sizeDelta = new Vector2(20, 2);
            text.sortingOrder = _order + 1;
            return (pill, text);
        }
    }
}
