using System;

namespace DragonGameEngine.Core
{
    public static class EngineRandom
    {
        public static readonly Random Random = new Random(Environment.TickCount);
    }
}
