namespace Codigames.Kingdom.Tutorial
{
    // A lesson never asks for what the player cannot pay: whether the purse covers what a scene leads to, and the
    // lines that make it up for a building.
    public interface IScenePurse
    {
        bool CanPayFor(ISceneDefinition scene);

        // A line that stocks a building has nothing to say while the purse can already pay for one.
        bool Needless(ISceneLine line);

        // Makes up what the purse lacks for one more of the building; what was added, by currency.
        System.Collections.Generic.IReadOnlyDictionary<string, double> Stock(string building);
    }
}
