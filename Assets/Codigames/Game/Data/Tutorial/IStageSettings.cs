namespace Codigames.Game.Data.Tutorial
{
    // The stage's pacing, and the help it offers a player who seems stuck.
    public interface IStageSettings
    {
        // Idle this long on an unfinished quest: the quest scroll wiggles.
        double IdleWiggleSeconds { get; }

        // Idle this long: the advisor peeks in to offer a hand.
        double IdleAdvisorSeconds { get; }

        // Between two peeks.
        double AdvisorRestSeconds { get; }

        // How long a peek stays.
        double AdvisorShowSeconds { get; }

        // How long the quest's "show me" hand stays.
        double PointerSeconds { get; }

        // The advisor peeks only until this quest is past.
        string UntilQuest { get; }

        // A lock whose target is missing this long lets go.
        double LockFailsafeSeconds { get; }

        double TypeCharsPerSecond { get; }

        // The breath between two scenes.
        double SceneGapSeconds { get; }

        // A line that appears on its own takes no tap for this long.
        double InputGraceSeconds { get; }
    }
}
