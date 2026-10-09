using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Kit
{
    // A card docked over the map (a building's, a ruin's, a landmark's): where its top edge stands, so the camera can
    // move what it is about into the map it leaves visible.
    [RequireComponent(typeof(RectTransform))]
    public class MapCard : MonoBehaviour
    {
        private readonly Vector3[] _corners = new Vector3[4];

        // The window's top edge as a share of the screen's height, from the bottom, its layout settled first.
        public float ViewportTop()
        {
            var rect = (RectTransform)transform;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            rect.GetWorldCorners(_corners);
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(camera, _corners[1]).y / Screen.height;
        }
    }
}
