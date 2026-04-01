namespace ZansiHustle.Shared.Results;

/// <summary>
/// Represents a non-generic operation result.
/// </summary>
public class Result
{
    public bool IsSuccess { get; protected set; }
    public string Code { get; protected set; } = string.Empty;
    public string Message { get; protected set; } = string.Empty;

    public static Result Success(string message = "Success")
    {
        return new Result
        {
            IsSuccess = true,
            Message = message
        };
    }

    public static Result Failure(string code, string message)
    {
        return new Result
        {
            IsSuccess = false,
            Code = code,
            Message = message
        };
    }

    public static Result Failure(string message)
    {
        return new Result
        {
            IsSuccess = false,
            Code = "500",
            Message = message
        };
    }
}