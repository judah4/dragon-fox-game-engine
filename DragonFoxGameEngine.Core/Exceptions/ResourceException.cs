using System;

namespace DragonGameEngine.Core.Exceptions
{
    public class ResourceException : Exception
    {
        public ResourceException(string name, string message)
            : base($"Resource {name}: {message}")
        {
        }
    }
}
