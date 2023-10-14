using System;

namespace DragonGameEngine.Core.Exceptions
{
    public class EngineException : Exception
    {
        public EngineException(string message)
            : base(message)
        {
        }
    }
}
