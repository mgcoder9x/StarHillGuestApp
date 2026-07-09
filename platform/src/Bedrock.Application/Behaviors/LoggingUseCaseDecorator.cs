using System.Diagnostics;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Microsoft.Extensions.Logging;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Behavior NGOÀI CÙNG (§8): log kết quả + thời lượng của use case value-returning. Đo bằng
/// <see cref="Stopwatch.GetElapsedTime(long)"/> (monotonic — KHÔNG dùng <c>IClock</c> vốn là wall-clock, có thể
/// nhảy do NTP). Không nuốt exception (không try/catch): lỗi kỹ thuật truyền lên middleware Api.
/// </summary>
public sealed class LoggingUseCaseDecorator<TInput, TOutput> : IUseCase<TInput, TOutput>
{
    private static readonly string UseCaseName = typeof(TInput).Name;
    private readonly IUseCase<TInput, TOutput> _inner;
    private readonly ILogger<LoggingUseCaseDecorator<TInput, TOutput>> _logger;

    public LoggingUseCaseDecorator(IUseCase<TInput, TOutput> inner, ILogger<LoggingUseCaseDecorator<TInput, TOutput>> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);
        _inner = inner;
        _logger = logger;
    }

    public async Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken ct = default)
    {
        var start = Stopwatch.GetTimestamp();
        var result = await _inner.ExecuteAsync(input, ct).ConfigureAwait(false);
        var elapsedMs = (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds;

        if (result.IsSuccess)
        {
            PipelineLog.Succeeded(_logger, UseCaseName, elapsedMs);
        }
        else
        {
            PipelineLog.Failed(_logger, UseCaseName, result.Error.Code, elapsedMs);
        }

        return result;
    }
}
