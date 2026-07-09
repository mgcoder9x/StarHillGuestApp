using Microsoft.Extensions.Logging;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Log có cấu trúc cho pipeline, sinh bằng source-generator <see cref="LoggerMessageAttribute"/> (tránh CA1848 —
/// delegate cache, zero-alloc khi level tắt). Chỉ log KẾT QUẢ (Ok/Fail) + thời lượng; KHÔNG try/catch (exception
/// truyền lên middleware Api xử lý — tránh nuốt lỗi/CA1031). Tracing spans đầy đủ hoãn tới task 18 (§9.3).
/// </summary>
internal static partial class PipelineLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "UseCase {UseCase} succeeded in {ElapsedMs} ms")]
    public static partial void Succeeded(ILogger logger, string useCase, long elapsedMs);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "UseCase {UseCase} failed [{ErrorCode}] in {ElapsedMs} ms")]
    public static partial void Failed(ILogger logger, string useCase, string errorCode, long elapsedMs);
}
