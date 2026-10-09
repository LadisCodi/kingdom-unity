using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Notices
{
    // A notice's picture in its frame: a building's art carries sky over its roof, so it is drawn wider than the frame
    // and sat on its foot; anything else fits inside.
    public static class NoticeArt
    {
        public static void Fit(Image image, Sprite art, bool building)
        {
            image.gameObject.SetActive(art != null);
            image.sprite = art;
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = building ? new Vector2(0, 0.06f) : Vector2.zero;
            rect.anchorMax = building ? new Vector2(1, 1.06f) : Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
