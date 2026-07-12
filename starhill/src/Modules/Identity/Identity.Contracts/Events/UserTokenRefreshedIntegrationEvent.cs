using Bedrock.Messaging.Contracts;

namespace Identity.Contracts.Events;

/// <summary>
/// Integration event PUBLIC của module Identity: một access/refresh token vừa được cấp lại (rotation) cho user.
/// module/telemetry khác có thể consume qua <c>IIntegrationEventHandler&lt;UserTokenRefreshedIntegrationEvent&gt;</c>.
/// ĐÃ EMIT (AD-057, đóng N-040): <c>RefreshAccessTokenUseCase</c> enqueue event này qua <c>IOutboxWriter</c> trong
/// CÙNG transaction rotation (all-or-nothing, CP6) → worker (AD-056) phát lên bus khi Host bật messaging.
/// EventType là chuỗi ỔN ĐỊNH (hợp đồng máy-đọc, versioning F32).
/// </summary>
public sealed record UserTokenRefreshedIntegrationEvent(Guid Id, DateTimeOffset OccurredAt, Guid UserId)
    : IntegrationEvent(Id, OccurredAt)
{
    public override string EventType => "identity.user_token_refreshed";
}
