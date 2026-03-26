namespace ZansiHustle.Shared.Results;

/// <summary>
/// Represents a generic operation result with data.
/// </summary>
public sealed class Result<T> : Result
{
    public T? Data { get; private set; }

    public static Result<T> Success(T data, string message = "Success")
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }

    public new static Result<T> Failure(string code, string message)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Code = code,
            Message = message,
            Data = default
        };
    }
}