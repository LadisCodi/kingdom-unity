namespace Kingdom.Game.Feedback
{
    // Shows short, pooled lines of feedback over the game (a toast).
    public interface IQuickInfoMessageService
    {
        void Show(QuickInfoMessageData data);
    }
}
