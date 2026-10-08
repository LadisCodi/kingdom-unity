using Codigames.Modules.Core;

namespace Codigames.Kingdom.Economy
{
    public interface ICurrencyDefinition : IIdentifiable
    {
        CurrencyScope Scope { get; }

        // What a new kingdom starts with.
        double Start { get; }

        // The most it may hold; null = no cap.
        double? Cap { get; }
    }
}
