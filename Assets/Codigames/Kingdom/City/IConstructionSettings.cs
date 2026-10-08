namespace Codigames.Kingdom.City
{
    public interface IConstructionSettings
    {
        // The building every city starts with, whose level is the era.
        IBuildingDefinition Townhall { get; }

        // From this level, upgrades wait on their late curve.
        int LateUpgradeFromLevel { get; }

        int StartBuilders { get; }
        int MaxBuilders { get; }

        // The next builder costs base × growth^bought Gems.
        double BuilderGemCostBase { get; }
        double BuilderGemCostGrowth { get; }
    }
}
