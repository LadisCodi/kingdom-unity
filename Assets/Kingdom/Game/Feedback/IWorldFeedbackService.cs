using UnityEngine;

namespace Kingdom.Game.Feedback
{
    public interface IWorldFeedbackService
    {
        // A pooled feedback placed at the position; the caller configures it and calls Play, and it returns
        // to its pool when it finishes.
        T Spawn<T>(Vector3 worldPosition) where T : WorldFeedbackView;
    }
}
