using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // Turns its object round, a whole turn every so many seconds (negative: the other way) — rays behind a prize.
    public class Spin : MonoBehaviour
    {
        [SerializeField] private float _turnSeconds = 40;

        private void Update()
        {
            if (_turnSeconds != 0) transform.Rotate(0, 0, -360f / _turnSeconds * Time.unscaledDeltaTime);
        }
    }
}
