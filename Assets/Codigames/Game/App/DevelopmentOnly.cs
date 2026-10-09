using UnityEngine;

namespace Codigames.Game.App
{
    // A tool for whoever builds the game, never for players: gone in a release build, kept in the editor and in
    // development builds (the Graphy overlay that measures frames and memory as we work).
    public class DevelopmentOnly : MonoBehaviour
    {
        private void Awake()
        {
            if (!Debug.isDebugBuild) Destroy(gameObject);
        }
    }
}
