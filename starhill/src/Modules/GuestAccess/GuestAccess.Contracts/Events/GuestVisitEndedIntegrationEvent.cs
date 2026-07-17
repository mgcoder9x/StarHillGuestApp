using Bedrock.Messaging.Contracts;

namespace GuestAccess.Contracts.Events;

/// <summary>
/// Integration event PUBLIC của module GuestAccess (C-GA.5, đóng CP9/Req 10.8): một lượt lưu trú (<see cref="GuestVisitId"/>)
/// vừa KẾT THÚC (idle-expiry — lễ tân đóng/sweeper là nguồn tương lai). Phát qua <c>IOutboxWriter</c> trong CÙNG
/// transaction với chuyển trạng thái visit → Expired (all-or-nothing, CP6/F5) → worker phát lên bus khi Host bật messaging.
/// <para>
/// Consumer (cascade C-GA.5b) khử trùng bằng inbox rồi gọi các use case IDEMPOTENT: <c>CloseConversationForVisitUseCase</c>
/// (Concierge — đóng hội thoại Open của visit) + <c>CancelOpenTicketsForVisitUseCase</c> (Housekeeping — huỷ ticket mở).
/// AT-LEAST-ONCE + handler idempotent = hiệu ứng đúng-một-lần về nghiệp vụ (QR-AD-027). Payload = <see cref="GuestVisitId"/>
/// trần (hai consumer đều tra theo id này); KHÔNG lộ entity GuestAccess. <see cref="EventType"/> hằng ổn định (hợp đồng máy-đọc).
/// </para>
/// </summary>
public sealed record GuestVisitEndedIntegrationEvent(Guid Id, DateTimeOffset OccurredAt, Guid GuestVisitId)
    : IntegrationEvent(Id, OccurredAt)
{
    public override string EventType => "guest_access.guest_visit_ended";
}
