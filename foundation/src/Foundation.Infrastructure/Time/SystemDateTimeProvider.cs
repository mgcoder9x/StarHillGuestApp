using Foundation.Application.Abstractions;

namespace Foundation.Infrastructure.Time;

/// <summary>Nguồn thời gian thật (UTC). Singleton — đăng ký qua marker <c>ISingletonService</c> của port.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
