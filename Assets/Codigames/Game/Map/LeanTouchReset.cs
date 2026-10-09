using Lean.Touch;
using UnityEngine;

namespace Codigames.Game.Map
{
    // With domain reload off, LeanTouch's static finger lists outlive a play session: a finger still down when play
    // stopped would come back as a ghost press. They are emptied as play starts.
    public static class LeanTouchReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetFingers()
        {
            LeanTouch.Fingers.Clear();
            LeanTouch.InactiveFingers.Clear();
        }
    }
}
