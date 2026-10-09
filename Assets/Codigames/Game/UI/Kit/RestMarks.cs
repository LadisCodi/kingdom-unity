using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // The resting Zs (the web's k-zzz): three white Zs rising from the corner one after another, each growing, tilting
    // and fading as it drifts up and right, round a 2.4 s loop. Off screen, they cost nothing.
    public class RestMarks : MonoBehaviour
    {
        private const float LOOP = 2.4f;

        [SerializeField] private List<TMP_Text> _zs = new();

        private void Update()
        {
            var rect = ((RectTransform)transform).rect;
            for (var i = 0; i < _zs.Count; i++)
            {
                var t = Mathf.Repeat(Time.unscaledTime / LOOP - i / 3f, 1);
                var eased = 1 - (1 - t) * (1 - t);
                var z = _zs[i];
                z.alpha = t < 0.2f ? t / 0.2f : t > 0.75f ? (1 - t) / 0.25f : 1;
                var zr = z.rectTransform;
                zr.anchoredPosition = new Vector2(eased * rect.width * 0.55f, eased * rect.height * 0.62f);
                zr.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.1f, eased);
                zr.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(8, -8, eased));
            }
        }
    }
}
