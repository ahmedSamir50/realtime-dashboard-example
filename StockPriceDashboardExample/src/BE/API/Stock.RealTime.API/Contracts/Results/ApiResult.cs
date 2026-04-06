namespace Stock.RealTime.API.Contracts.Results;

/// <param name="Data"> Gets or sets the data. </param>
/// <param name="IsSucceeded"> Gets or sets a value indicating whether is succeeded. </param>
/// <param name="Messages"> Gets or sets the messages. </param>
/// <param name="Code"> Gets or sets the code. </param>
/// <param name="DataCount"> Gets or sets the data count. </param>
public record ApiResult<T>(T Data, bool IsSucceeded, List<string> Messages, ResponseStatusCode Code, long DataCount)
{
    /// <summary>
    /// Gets a value indicating whether is failed.
    /// </summary>
    public bool IsFailed => !IsSucceeded;

    /// <summary>
    /// Successes the.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns>A Result.</returns>
    public static ApiResult<T> Success(T data)
    {
        return new ApiResult<T>(data, true, default, ResponseStatusCode.Success, default);
    }

    /// <summary>
    /// Successes the.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="totalCount">The total count.</param>
    /// <returns>A Result.</returns>
    public static ApiResult<T> Success(T data, int totalCount)
    {
        return new ApiResult<T>(data, true, default, ResponseStatusCode.Success, totalCount);
    }

    /// <summary>
    /// Successes the.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="totalCount">The total count.</param>
    /// <returns>A Result.</returns>
    public static ApiResult<T> Success(T data, long totalCount)
    {
        return new ApiResult<T>(data, true, default, ResponseStatusCode.Success, default)
        {
            DataCount = totalCount
        };
    }

    /// <summary>
    /// Successes the.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="messages">The messages.</param>
    /// <returns>A Result.</returns>
    public static ApiResult<T> Success(T data, List<string> messages)
    {
        return new ApiResult<T>(data, true, messages, ResponseStatusCode.Success, default);
    }

    /// <summary>
    /// Successes the.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="messages">The messages.</param>
    /// <returns>A Result.</returns>
    public static ApiResult<T> Success(T data, params string[] messages)
    {
        return new ApiResult<T>(data, true, messages.Any() ? messages.ToList() : null, ResponseStatusCode.Success, default);
    }

    /// <summary>
    /// Faileds the.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="messages">The messages.</param>
    /// <returns>A Result.</returns>
    public static ApiResult<T> Failed(ResponseStatusCode code,
                                   params string[] messages)
    {
        return new ApiResult<T>(default, false, messages.Any() ? messages.ToList() : null, code, default);
    }

    /// <summary>
    /// Faileds the.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>A Result.</returns>
    public static ApiResult<T> Failed(ResponseStatusCode code, Exception exception)
    {
        var messages = new List<string> { exception.Message };
        if (exception.InnerException is not null)
        {
            messages.AddRange(exception.InnerException.Message.Split(Environment.NewLine));
        }

        return new ApiResult<T>(default, false, messages, code, default);
    }

    /// <summary>
    /// Faileds the.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="messages">The messages.</param>
    /// <returns>A Result.</returns>
    public static ApiResult<T> Failed(ResponseStatusCode code, List<string> messages)
    {
        return new ApiResult<T>(default, false, messages, code, default);
    }

    /// <summary>
    /// Faileds the.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="data">The data.</param>
    /// <param name="messages">The messages.</param>
    /// <returns>A Result.</returns>
    public static ApiResult<T> Failed(ResponseStatusCode code,
                                   T data,
                                   List<string> messages)
    {
        return new ApiResult<T>(data, false, messages, code, default);
    }
}