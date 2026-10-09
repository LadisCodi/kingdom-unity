namespace Codigames.Game.UI.Presenters
{
    // The district whose card is open, or null: what the header's plaque reads when it is a building's crew.
    public interface IInspectedDistrict
    {
        string Inspected { get; }
    }
}
