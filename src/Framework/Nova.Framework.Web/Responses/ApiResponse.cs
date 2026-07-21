namespace Nova.Framework.Web.Responses;

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Success", int code = 200)
    {
        return new ApiResponse<T>
        {
            Code = code,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Error(string message, int code = 500)
    {
        return new ApiResponse<T>
        {
            Code = code,
            Message = message,
            Data = default
        };
    }
}

public class ApiResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }

    public static ApiResponse Success(string message = "Success", int code = 200)
    {
        return new ApiResponse
        {
            Code = code,
            Message = message,
            Data = null
        };
    }

    public static ApiResponse Error(string message, int code = 500)
    {
        return new ApiResponse
        {
            Code = code,
            Message = message,
            Data = null
        };
    }
}
