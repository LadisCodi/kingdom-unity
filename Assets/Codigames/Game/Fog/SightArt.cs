using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Fog
{
    // How what the fog shows is drawn: a thing sighted past it in one flat, cold, faint colour over its own shape; the
    // reach's border in vertex colours.
    [CreateAssetMenu(fileName = "SightArt", menuName = "Kingdom/Art/Sight Art")]
    public class SightArt : ScriptableObject
    {
        [SerializeField, Required] private Material _silhouette;

        [SerializeField, Required] private Material _border;

        public Material Silhouette => _silhouette;
        public Material Border => _border;
    }
}
