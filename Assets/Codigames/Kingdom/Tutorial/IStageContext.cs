namespace Codigames.Kingdom.Tutorial
{
    // What the screen says about whether a scene may start: the parts of the director's rules the kingdom cannot
    // answer.
    public interface IStageContext
    {
        // Something that holds every scene back: a fight, a reveal, a video, an unlock splash waiting.
        bool HeldBack { get; }

        // The world board is the screen in front of the player.
        bool OnWorld { get; }

        // A menu, a card or a placement covers the map.
        bool HasOpenSheet { get; }
    }
}
