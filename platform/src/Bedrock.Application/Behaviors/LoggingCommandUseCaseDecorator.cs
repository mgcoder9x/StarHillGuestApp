using System.Diagnostics;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Microsoft.Extensions.Logging;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Như <see cref="LoggingUseCaseDecorator{TInput,TOutput}"/> nhưng cho <see cref="ICommandUseCase{TInput}"/>
/// (không trả giá trị). Log Ok/Fail + thời lượng, đo monotonic bằng <see cref="Stopwatch"/>.
/// </summary>
public sealed class LoggingCommandUseCaseDecorator<TInput> : ICommandUseCase<TInput>
{
    private static readonly string UseCaseName = typeof(TInput).Name;
    private readonly ICommandUseCase<TInput> _inner;
    private readonly ILogger<LoggingCommandUseCaseDecorator<TInput>> _logger;

    public LoggingCommandUseCaseDecorator(ICommandUseCase<TInput> inner, ILogger<LoggingCommandUseCaseDecorator<TInput>> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);
        _inner = inner;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(TInput input, CancellationToken ct = default)
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

    public string? PersistenceKey => _inner.PersistenceKey;
}
