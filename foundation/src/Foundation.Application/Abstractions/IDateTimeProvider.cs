using Foundation.SharedKernel.DependencyInjection;

namespace Foundation.Application.Abstractions;

/// <summary>
/// Nguồn thời gian trừu tượng (thay <c>DateTimeOffset.UtcNow</c> rải rác) → use case tất định, test được.
/// </summary>
public interface IDateTimeProvider : ISingletonService
{
    DateTimeOffset UtcNow { get; }
}
