
namespace Kingdom.Game.UI.Widgets
{
    using System.Threading;
    using Cysharp.Threading.Tasks;

    public interface IStateViewAnimation
    {
        UniTask PlayShow(CancellationToken ct);
        UniTask PlayHide(CancellationToken ct);
    }
}