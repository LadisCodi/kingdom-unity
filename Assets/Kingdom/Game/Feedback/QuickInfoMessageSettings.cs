
using UnityEngine;

namespace Kingdom.Game.Feedback
{
    [CreateAssetMenu(fileName = "Quick Info Message Settings", menuName = "Kingdom/Feedback/Quick Info Message Settings")]
    public class QuickInfoMessageSettings : ScriptableObject
    {
        [SerializeField, Tooltip("Pooled prefab for quick info messages; needs a QuickInfoMessageView with a TMP label and an MMF_Player.")]
        private QuickInfoMessageView _prefab;

        public QuickInfoMessageView Prefab => _prefab;
    }
}
