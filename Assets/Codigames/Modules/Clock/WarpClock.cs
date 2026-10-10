using System;

namespace Codigames.Modules.Clock
{
    // The system's clock, pushed forward on demand: a development aid that shows what an absence does without waiting
    // for it (the web's dev time-warp). Never backward — the game's time only moves on.
    public class WarpClock : IClock
    {
        private long _offset;

        public WarpClock(long offset = 0) => _offset = System.Math.Max(0, offset);

        public long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _offset;

        // How far ahead of the real clock it runs.
        public long OffsetMs => _offset;

        public void Skip(long ms)
        {
            if (ms > 0) _offset += ms;
        }
    }
}
