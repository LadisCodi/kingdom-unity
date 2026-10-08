using System.Threading.Tasks;

namespace Codigames.Modules.UI
{
    // A menu shown with data.
    public interface IDataMenuPresenter<in TData> : IMenuPresenter
    {
        Task Show(TData data);
    }
}
