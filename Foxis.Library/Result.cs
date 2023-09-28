using System;

namespace Foxis.Library
{
    /// <summary>
    /// Result for status, error, and value.
    /// </summary>
    public static class Result
    {
        public static Result<T> Fail<T>(string message)
        {
            return new Result<T>(false, message, default);
        }

        public static Result<T> Ok<T>()
        {
            return new Result<T>(true, string.Empty, default);
        }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(true, string.Empty, value);
        }
    }

    public struct Result<T>
    {
        public T? Value { get; }
        public bool Success { get; }
        public string Error { get; }
        public bool IsFailure => !Success;

        internal Result(bool success, string error, T? value)
        {
            if (success && error != string.Empty)
                throw new InvalidOperationException();
            if (!success && error == string.Empty)
                throw new InvalidOperationException();
            Value = value;
            Success = success;
            Error = error;
        }

        public Result()
        {
            Value = default;
            Success = true;
            Error = string.Empty;
        }
    }
}
