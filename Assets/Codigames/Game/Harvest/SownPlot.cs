using Codigames.Game.City;
using UnityEngine;

namespace Codigames.Game.Harvest
{
    // A plot being sown: the construction's bar and the builder's hammer, over its cell.
    public class SownPlot : MonoBehaviour
    {
        [SerializeField] private ProgressBarView _bar;
        [SerializeField] private WorkingHammer _hammer;

        public ProgressBarView Bar => _bar;
        public WorkingHammer Hammer => _hammer;
    }
}
