using System;

namespace Kingdom.Game.Clock
{
    public class SystemClock : IClock
    {
        public long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
