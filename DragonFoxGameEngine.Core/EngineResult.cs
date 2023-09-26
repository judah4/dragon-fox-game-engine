using System;

namespace DragonFoxGameEngine.Core
{
    public static class EngineResult
    {
        public static EngineResult<T> Fail<T>(string message)
        {
            return new EngineResult<T>(false, message, default);
        }

        public static EngineResult<T> Ok<T>()
        {
            return new EngineResult<T>(true, string.Empty, default);
        }

        public static EngineResult<T> Ok<T>(T value)
        {
            return new EngineResult<T>(true, string.Empty, value);
        }
    }

    public struct EngineResult<T>
    {
        public T? Value { get; }
        public bool Success { get; }
        public string Error { get; }
        public bool IsFailure => !Success;

        internal EngineResult(bool success, string error, T? value)
        {
            if (success && error != string.Empty)
                throw new InvalidOperationException();
            if (!success && error == string.Empty)
                throw new InvalidOperationException();
            Value = value;
            Success = success;
            Error = error;
        }

        public EngineResult()
        {
            Value = default;
            Success = true;
            Error = string.Empty;
        }
    }
}
