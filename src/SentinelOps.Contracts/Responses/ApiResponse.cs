namespace SentinelOps.Contracts.Responses;

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ApiResponse<T> Ok(T data, string message = "")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Errors = []
        };
    }

    public static ApiResponse<T> SuccessResponse(T data, string message = "") => Ok(data, message);

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = errors?.ToList() ?? []
        };
    }

    public static ApiResponse<T> FailureResponse(string message, IEnumerable<string>? errors = null) => Fail(message, errors);
}
