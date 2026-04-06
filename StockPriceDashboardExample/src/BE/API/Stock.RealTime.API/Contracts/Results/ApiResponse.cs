namespace Stock.RealTime.API.Contracts.Results;


/// <param name="Code"> Gets or sets the code. </param>
/// <param name="Messages"> Gets or sets the messages. </param>
/// <param name="Data"> Gets or sets the data. </param>
/// <param name="DataCount"> Gets or sets the data count. </param>
public sealed record ApiResponse<T>(ResponseStatusCode Code, IList<string> Messages, T Data, long DataCount)
{
    public ApiResponse() : this(default, default, default, default)
    {

    }

    /// <summary>
    /// Oks the.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>A ApiResponse.</returns>
    public static ApiResponse<T> Ok(ApiResult<T> result)
    {
        return new ApiResponse<T>()
        {
            Code = result.Code,
            Data = result.Data,
            Messages = result.Messages,
            DataCount = result.DataCount
        };
    }

    /// <summary>
    /// Oks the.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="messages">The messages.</param>
    /// <returns>A ApiResponse.</returns>
    public static ApiResponse<T> Ok(T data, params string[] messages)
    {
        return new ApiResponse<T>()
        {
            Code = ResponseStatusCode.Success,
            Data = data,
            Messages = messages,
            DataCount = default
        };
    }
    /// <summary>
    /// Oks the.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="messages">The messages.</param>
    /// <returns>A ApiResponse.</returns>
    public static ApiResponse<T> Ok(T data, IList<string> messages)
    {
        return new ApiResponse<T>()
        {
            Code = ResponseStatusCode.Success,
            Data = data,
            Messages = messages,
            DataCount = default
        };
    }

    /// <summary>
    /// Bads the request.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>A ApiResponse.</returns>
    public static ApiResponse<T> BadRequest(ApiResult<T> result)
    {
        return new ApiResponse<T>()
        {
            Code = result.Code,
            Messages = result.Messages,
            DataCount = default
        };
    }

    /// <summary>
    /// Bads the request.
    /// </summary>
    /// <param name="errors">The errors.</param>
    /// <returns>A ApiResponse.</returns>
    public static ApiResponse<T> BadRequest(IList<string> errors)
    {
        return new ApiResponse<T>()
        {
            Code = ResponseStatusCode.Error,
            Messages = errors,
            DataCount = default
        };
    }

    /// <summary>
    /// Bads the request.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="errors">The errors.</param>
    /// <returns>A ApiResponse.</returns>
    public static ApiResponse<T> BadRequest(T data, IList<string> errors)
    {
        return new ApiResponse<T>()
        {
            Code = ResponseStatusCode.Error,
            Data = data,
            Messages = errors,
            DataCount = default
        };
    }

    /// <summary>
    /// Uns the authorized.
    /// </summary>
    /// <param name="errors">The errors.</param>
    /// <returns>A ApiResponse.</returns>
    public static ApiResponse<T> UnAuthorized(IList<string> errors = null)
    {
        return new ApiResponse<T>()
        {
            Code = ResponseStatusCode.Error,
            Messages = errors,
            DataCount = default
        };
    }
}