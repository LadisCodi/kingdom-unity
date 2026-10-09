namespace Codigames.Kingdom.Research
{
    public interface IKnowledgeSettings
    {
        // The drip, an hour, while the bar is below its cap.
        double PerHour { get; }

        // Where the drip stops; nothing else does.
        double Cap { get; }

        // The nth point ever bought with Gold costs base × n^exponent.
        double GoldPriceBase { get; }
        double GoldPriceExponent { get; }

        // A point bought with Gems; it never rises.
        double GemsPerPoint { get; }
    }
}
