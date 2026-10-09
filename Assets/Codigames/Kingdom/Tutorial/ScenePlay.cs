using System;
using System.Collections.Generic;

namespace Codigames.Kingdom.Tutorial
{
    // A scene on the stage and how far it has got. A beat checks its condition when it starts: lines already met are
    // passed at once, so a reload resumes at the first one still owed; a scene starts after the last line whose
    // PROGRESS already holds, rather than replaying the greeting to someone halfway through the morning.
    public class ScenePlay
    {
        private readonly IConditions _conditions;
        private readonly IScenePurse _purse;
        private readonly Func<int> _taps;

        public ScenePlay(ISceneDefinition scene, IConditions conditions, IScenePurse purse, Func<int> taps)
        {
            Scene = scene;
            _conditions = conditions;
            _purse = purse;
            _taps = taps;
        }

        // A line stocked a building: the line, and what was added by currency.
        public event Action<ISceneLine, IReadOnlyDictionary<string, double>> Stocked;

        // A new line is on: the stage shows it.
        public event Action<ISceneLine> LineBegan;

        public ISceneDefinition Scene { get; }

        public int Index { get; private set; }

        // The tap count when this line began.
        public int TapsAtStart { get; private set; }

        public bool Finished => Index >= Scene.Lines.Count;

        public ISceneLine Line => Finished ? null : Scene.Lines[Index];

        public void Start() => Begin(ProgressedTo(0));

        public bool LineHolds(ISceneLine line) => line.Until.Kind != ConditionKind.Tap && _conditions.Holds(line.Until, TapsAtStart);

        // The line after the last one from `from` whose PROGRESS condition already holds; `from` when none does.
        public int ProgressedTo(int from)
        {
            var at = from;
            for (var i = from; i < Scene.Lines.Count; i++)
            {
                var line = Scene.Lines[i];
                if (SceneDirector.IsProgress(line.Until.Kind) && _conditions.Holds(line.Until, TapsAtStart)) at = i + 1;
            }

            return at;
        }

        // The line is done: what it hands over is handed over, and the next begins.
        public void Next()
        {
            if (Finished) return;
            Hand(Line);
            Begin(Index + 1);
        }

        // Begins line `index`, passing every line already met.
        public void Begin(int index)
        {
            while (index < Scene.Lines.Count)
            {
                var line = Scene.Lines[index];
                Index = index;
                TapsAtStart = _taps();
                if (_purse.Needless(line))
                {
                    index++;
                    continue;
                }

                if (line.Until.Kind == ConditionKind.Tap || !LineHolds(line)) break;
                Hand(line);
                index++;
            }

            Index = index;
            if (!Finished) LineBegan?.Invoke(Line);
        }

        private void Hand(ISceneLine line)
        {
            if (string.IsNullOrEmpty(line.Stocks)) return;
            var added = _purse.Stock(line.Stocks);
            if (added.Count > 0) Stocked?.Invoke(line, added);
        }
    }
}
