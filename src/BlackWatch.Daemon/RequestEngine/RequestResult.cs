namespace BlackWatch.Daemon.RequestEngine;

/// <summary>
///     execution results for request executed by <see cref="RequestRunner" />
/// </summary>
public enum RequestResult
{
    /// <summary>
    ///     all is fine
    /// </summary>
    Ok,

    /// <summary>
    ///     enqueue the request again
    /// </summary>
    Retry,

    /// <summary>
    ///     enqueue the request again and suspend job execution for a minute
    /// </summary>
    WaitAndRetry,

    /// <summary>
    ///     give up on the request
    /// </summary>
    Fatal
}
