using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // The crew stepper: how many work here against the most it holds, and whether − and + can be pressed.
    public sealed class CrewPanelData
    {
        public CrewPanelData(Sprite bust, Vector2 bustShift, float bustScale, string count, string limit, bool canRemove, bool canAdd)
        {
            Bust = bust;
            BustShift = bustShift;
            BustScale = bustScale;
            Count = count;
            Limit = limit;
            CanRemove = canRemove;
            CanAdd = canAdd;
        }

        public Sprite Bust { get; }
        public Vector2 BustShift { get; }
        public float BustScale { get; }
        public string Count { get; }
        public string Limit { get; }
        public bool CanRemove { get; }
        public bool CanAdd { get; }
    }
}
