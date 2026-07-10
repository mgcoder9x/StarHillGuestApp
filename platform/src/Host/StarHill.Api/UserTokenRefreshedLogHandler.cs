using Bedrock.Application.Messaging;
using Identity.Contracts.Events;

namespace StarHill.Api;

/// <summary>
/// Handler DEMO (sample Host) cho <see cref="UserTokenRefreshedIntegrationEvent"/> — chứng minh chuỗi consume
/// end-to-end (subscriber → dispatch core → handler + inbox). Chỉ LOG (không side-effect nghiệp vụ ở skeleton).
/// Chạy TRONG transaction consume (dispatch core mở) → nếu handler ghi DB thì inbox mark + business nguyên tử (F30).
/// </summary>
public sealed partial class UserTokenRefreshedLogHandler(ILogger<UserTokenRefreshedLogHandler> logger)
    : IIntegrationEventHandler<UserTokenRefreshedIntegrationEvent>
{
    public Task HandleAsync(UserTokenRefreshedIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        Log.Received(logger, integrationEvent.UserId, integrationEvent.Id);
        return Task.CompletedTask;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information,
            Message = "Consumed UserTokenRefreshed (userId={UserId}, eventId={EventId}).")]
        public static partial void Received(ILogger logger, Guid userId, Guid eventId);
    }
}
