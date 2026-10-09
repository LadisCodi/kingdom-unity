using Codigames.Kingdom.Tutorial;
using UnityEngine;

namespace Codigames.Game.UI.Stage
{
    // The quest's "show me where", pointed at with the stage's own hand between scenes — one sign for "here" — until
    // it is tapped or its time runs out.
    public class StageHint
    {
        private float _until;

        public MapTarget? Target { get; private set; }

        public void Point(MapTarget target, double seconds)
        {
            Target = target;
            _until = Time.unscaledTime + (float)seconds;
        }

        public void Clear() => Target = null;

        // The hint still standing now, or null.
        public MapTarget? Current()
        {
            if (Target.HasValue && Time.unscaledTime >= _until) Target = null;
            return Target;
        }
    }
}
