using Cysharp.Threading.Tasks;

namespace Kingdom.Game.UI
{
    public interface IMenu
    {
        void Initialize();
        void Dispose();
        UniTask Show();
        UniTask Hide();
    }
}
