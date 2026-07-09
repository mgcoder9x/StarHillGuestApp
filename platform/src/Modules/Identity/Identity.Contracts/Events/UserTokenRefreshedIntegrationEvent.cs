using Bedrock.Messaging.Contracts;

namespace Identity.Contracts.Events;

/// <summary>
/// Integration event PUBLIC của module Identity: một access/refresh token vừa được cấp lại (rotation) cho user.
/// Khai contract-first (giống ports task 12–13) — module/telemetry khác có thể consume qua
/// <c>IIntegrationEventHandler&lt;UserTokenRefreshedIntegrationEvent&gt;</c>. Emission (qua <c>IOutboxWriter</c>
/// trong cùng transaction rotation) sẽ được nối khi có consumer thật hoặc ở DoD (task 21) — N-040.
/// EventType là chuỗi ỔN ĐỊNH (hợp đồng máy-đọc, versioning F32).
/// </summary>
public sealed record UserTokenRefreshedIntegrationEvent(Guid Id, DateTimeOffset OccurredAt, Guid UserId)
    : IntegrationEvent(Id, OccurredAt)
{
    public override string EventType => "identity.user_token_refreshed";
}
