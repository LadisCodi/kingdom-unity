namespace Codigames.Modules.Saves
{
    // One step of a save's history: reshapes a raw document of one version into the next.
    public interface ISaveMigration<TRaw>
    {
        // The version it reads; it writes FromVersion + 1.
        int FromVersion { get; }

        TRaw Apply(TRaw raw);
    }
}
