namespace Codigames.Kingdom.Harvest
{
    public interface ITapSettings
    {
        // Seconds of work one tap is worth: ten seconds of a woodcutter's swing.
        double WorkSeconds { get; }

        // Mana one tap on the ground costs.
        double ManaCost { get; }
    }
}
