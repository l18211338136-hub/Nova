namespace Nova.Framework.Web.Responses;

/// <summary>
/// 统一 API 响应格式 (泛型)
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// 状态码 (200为成功)
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应业务数据
    /// </summary>
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

/// <summary>
/// 统一 API 响应格式 (非泛型)
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// 状态码 (200为成功)
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应业务数据 (通常为空)
    /// </summary>
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
