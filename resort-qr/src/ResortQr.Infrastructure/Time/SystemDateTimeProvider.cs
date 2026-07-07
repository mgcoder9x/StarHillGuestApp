using ResortQr.Application.Abstractions;

namespace ResortQr.Infrastructure.Time;

/// <summary>Nguồn thời gian thật (UTC). Singleton — đăng ký qua marker <c>ISingletonService</c> của port.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
