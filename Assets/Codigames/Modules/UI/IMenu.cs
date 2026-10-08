using Cysharp.Threading.Tasks;

namespace Codigames.Modules.UI
{
    public interface IMenu
    {
        void Initialize();
        void Dispose();
        UniTask Show();
        UniTask Hide();
    }
}
