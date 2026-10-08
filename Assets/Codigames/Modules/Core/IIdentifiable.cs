namespace Codigames.Modules.Core
{
    // Something with a stable identity: what a catalog or a registry keys it by, and what a save stores.
    public interface IIdentifiable
    {
        string Id { get; }
    }
}
