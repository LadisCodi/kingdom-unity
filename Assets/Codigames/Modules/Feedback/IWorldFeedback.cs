using System.Threading.Tasks;

namespace Codigames.Modules.Feedback
{
    // A fire-and-forget effect in the world (a coin that floats up and fades). It completes when it is done.
    public interface IWorldFeedback
    {
        Task Play();
    }
}
