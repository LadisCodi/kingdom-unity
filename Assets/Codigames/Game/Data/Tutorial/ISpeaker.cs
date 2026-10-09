using Codigames.Modules.Core;
using UnityEngine;

namespace Codigames.Game.Data.Tutorial
{
    // Someone who speaks on the stage: their name on the ribbon, their figure in each mood.
    public interface ISpeaker : IIdentifiable
    {
        string Name { get; }

        string Title { get; }

        // Their figure in a mood ("happy", "worried"…): the mood's art where it exists, their own otherwise.
        Sprite Picture(string expression);

        // The ribbon their name is written on, in their colour.
        Sprite Ribbon { get; }
    }
}
