namespace Codigames.Modules.Saves
{
    // The step for a version that only added something with a default: the document is read as it is, and the
    // reader fills the new part in.
    public sealed class AdditiveMigration<TRaw> : ISaveMigration<TRaw>
    {
        public AdditiveMigration(int fromVersion)
        {
            FromVersion = fromVersion;
        }

        public int FromVersion { get; }

        public TRaw Apply(TRaw raw) => raw;
    }
}
