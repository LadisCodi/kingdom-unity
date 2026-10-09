using UnityEngine;

namespace Codigames.Game.UI
{
    // Draws a menu above or below the others, whatever its place among them: the plank and the nav bar stay over
    // every menu. A canvas at a prefab's root cannot keep its override, so it is set once the menu sits under the UI.
    [RequireComponent(typeof(Canvas))]
    public class CanvasOrder : MonoBehaviour
    {
        [SerializeField] private int _order;

        private void OnEnable()
        {
            var canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = _order;
        }
    }
}
