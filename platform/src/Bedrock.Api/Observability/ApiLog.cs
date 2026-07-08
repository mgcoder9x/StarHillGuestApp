using Microsoft.Extensions.Logging;

namespace Bedrock.Api.Observability;

/// <summary>
/// Log messages qua source generator <c>[LoggerMessage]</c> (high-perf, tránh CA1848). Path LUÔN là bản đã
/// mask (CP13/F15) — caller truyền path đã qua <see cref="PathMasker"/>, KHÔNG truyền raw.
/// </summary>
internal static partial class ApiLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "HTTP {Method} {MaskedPath} responded {StatusCode} in {ElapsedMs}ms [correlation={CorrelationId}]")]
    public static partial void Request(
        ILogger logger, string method, string maskedPath, int statusCode, long elapsedMs, string correlationId);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Unhandled exception for {Method} {MaskedPath} [correlation={CorrelationId}]")]
    public static partial void UnhandledException(
        ILogger logger, Exception exception, string method, string maskedPath, string correlationId);
}
