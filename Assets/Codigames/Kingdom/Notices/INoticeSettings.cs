namespace Codigames.Kingdom.Notices
{
    public interface INoticeSettings
    {
        // News bubbles shown before the rest fold under a +N.
        int Shown { get; }

        // News kept in the inbox; past it the oldest goes.
        int Kept { get; }
    }
}
