using Codigames.Modules.Core;

namespace Codigames.Modules.Feedback
{
    public interface IWorldFeedbackService
    {
        // A feedback of this kind placed at a world position; the caller configures it and plays it.
        T Spawn<T>(Vector3 worldPosition) where T : class, IWorldFeedback;
    }
}
