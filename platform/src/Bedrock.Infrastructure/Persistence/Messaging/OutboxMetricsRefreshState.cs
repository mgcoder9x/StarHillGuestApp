namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>Per-context throttle for async backlog metric refreshes.</summary>
internal static class OutboxMetricsRefreshState<TContext>
    where TContext : PlatformDbContext
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, DateTimeOffset> NextRefreshByModule = new(StringComparer.Ordinal);

    public static bool TryAcquire(string module, DateTimeOffset now, TimeSpan interval)
    {
        lock (Gate)
        {
            if (NextRefreshByModule.TryGetValue(module, out var nextRefreshAt) && now < nextRefreshAt)
            {
                return false;
            }

            NextRefreshByModule[module] = now + interval;
            return true;
        }
    }
}
