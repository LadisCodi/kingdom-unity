namespace Codigames.Modules.UI
{
    // Port: where menu views come from (a prefab, a scene object, a test double).
    public interface IMenuViewFactory
    {
        TView Resolve<TView>() where TView : class, IMenuView;
    }
}
