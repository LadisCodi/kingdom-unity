using UnityEngine;

namespace Codigames.Modules.UI
{
    // The canvas every menu is placed under. Registered in the scene's scope, so nothing searches for it.
    [RequireComponent(typeof(RectTransform))]
    public class UIRoot : MonoBehaviour
    {
        public RectTransform Container => (RectTransform)transform;
    }
}
