using System;

namespace Codigames.Modules.Clock
{
    public class SystemClock : IClock
    {
        public long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
