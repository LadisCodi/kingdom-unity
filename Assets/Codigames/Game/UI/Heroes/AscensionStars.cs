using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Heroes
{
    // A hero's ascension as the stars the player counts (the web's ascensionStars): each star the empty socket with the
    // gold star over it, uncovered one petal per point, clockwise from the top. The stars before the current one are
    // full, the ones after it empty.
    public class AscensionStars : MonoBehaviour
    {
        [SerializeField] private List<Image> _lit = new();

        public void Show(int ascension, int stepsPerStar)
        {
            for (var i = 0; i < _lit.Count; i++)
            {
                var points = Mathf.Clamp(ascension - i * stepsPerStar, 0, stepsPerStar);
                _lit[i].fillAmount = stepsPerStar > 0 ? (float)points / stepsPerStar : 0;
            }
        }
    }
}
