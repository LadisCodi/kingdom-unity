using System;
using UnityEngine;

namespace Kingdom.Game.Feedback
{
    // One prefab per concrete WorldFeedbackView type.
    [CreateAssetMenu(fileName = "WorldFeedbackCatalog", menuName = "Kingdom/Feedback/World Feedback Catalog")]
    public class WorldFeedbackCatalog : ScriptableObject
    {
        [SerializeField] private WorldFeedbackView[] _prefabs = Array.Empty<WorldFeedbackView>();

        public WorldFeedbackView GetPrefab(Type type)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab != null && prefab.GetType() == type) return prefab;
            }

            throw new InvalidOperationException($"No world feedback prefab of type {type.Name} in {name}.");
        }
    }
}
