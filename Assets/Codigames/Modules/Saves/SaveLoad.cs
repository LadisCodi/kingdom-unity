namespace Codigames.Modules.Saves
{
    // What reading a slot found.
    public enum SaveLoadStatus
    {
        // Nothing saved yet.
        Missing,
        Loaded,
        // Written by a newer build: never downgraded, and never written over.
        TooNew,
        // Not a save that can be read; it was set aside.
        Unreadable,
    }

    public readonly struct SaveLoad<TState>
    {
        public SaveLoad(SaveLoadStatus status, TState state, int version)
        {
            Status = status;
            State = state;
            Version = version;
        }

        public SaveLoadStatus Status { get; }
        public TState State { get; }

        // The version it was written at.
        public int Version { get; }
    }
}
