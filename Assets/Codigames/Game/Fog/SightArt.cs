using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Fog
{
    // How a thing sighted past the fog is drawn: one flat, cold, faint colour over its own shape.
    [CreateAssetMenu(fileName = "SightArt", menuName = "Kingdom/Art/Sight Art")]
    public class SightArt : ScriptableObject
    {
        [SerializeField, Required] private Material _silhouette;

        public Material Silhouette => _silhouette;
    }
}
