using System;
using Foundation.Application.Abstractions;

namespace Foundation.UnitTests.TestDoubles;

/// <summary>Clock giả lập cho test — điều khiển thời gian tất định.</summary>
public sealed class FakeDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = now;
}
