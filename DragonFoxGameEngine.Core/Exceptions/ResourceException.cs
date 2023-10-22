using System;

namespace DragonGameEngine.Core.Exceptions
{
    public class ResourceException : Exception
    {
        public ResourceException(string name, string message)
            : this(name, message, null)
        {
        }

        public ResourceException(string name, string message, Exception? innerException)
            : base($"Resource {name}: {message}", innerException)
        {
        }
    }
}
