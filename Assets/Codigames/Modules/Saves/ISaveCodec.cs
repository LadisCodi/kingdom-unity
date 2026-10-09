namespace Codigames.Modules.Saves
{
    // Port: how a state is written as text and read back. A save is read in two steps — the text into a raw
    // document (whatever the codec's format is: a JSON tree), which carries its version and which migrations
    // reshape, then the document into the state.
    public interface ISaveCodec<TState, TRaw>
    {
        string Encode(TState state, int version);

        // Throws when the text is not a save at all.
        TRaw Parse(string text);

        int VersionOf(TRaw raw);

        TState Decode(TRaw raw);
    }
}
