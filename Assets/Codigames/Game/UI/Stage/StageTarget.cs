using Codigames.Kingdom.Tutorial;

namespace Codigames.Game.UI.Stage
{
    // What a line points at: a control on screen, by its key, or a plot of the map.
    public readonly struct StageTarget
    {
        private StageTarget(string uiKey, MapTarget? map)
        {
            UiKey = uiKey;
            Map = map;
        }

        public string UiKey { get; }
        public MapTarget? Map { get; }
        public bool IsUi => UiKey != null;

        public static StageTarget Ui(string key) => new(key, null);

        public static StageTarget Plot(MapTarget map) => new(null, map);
    }
}
