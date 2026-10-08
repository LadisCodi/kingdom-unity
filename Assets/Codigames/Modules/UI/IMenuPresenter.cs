using System;
using Cysharp.Threading.Tasks;

namespace Codigames.Modules.UI
{
    public interface IMenuPresenter
    {
        Type MenuType { get; }

        bool IsShown { get; }
        bool HasFocus { get; }

        UniTask Show();
        UniTask Hide();

        void OnFocusGained();
        void OnFocusLost();
    }

    public interface IMenuPresenter<in TData> : IMenuPresenter
    {
        UniTask Show(TData data);
    }
}
