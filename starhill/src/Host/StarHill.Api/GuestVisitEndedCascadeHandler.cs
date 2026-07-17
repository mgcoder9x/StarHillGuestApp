using Bedrock.Application.Messaging;
using Bedrock.Application.UseCases;
using Concierge.Application;
using GuestAccess.Contracts.Events;
using Housekeeping.Application;

namespace StarHill.Api;

/// <summary>
/// Handler CASCADE cross-module cho <see cref="GuestVisitEndedIntegrationEvent"/> (C-GA.5b — đóng CP9/Req 10.8): khi
/// một lượt lưu trú kết thúc (idle-expiry), ĐÓNG hội thoại Concierge (<see cref="CloseConversationForVisitUseCase"/>)
/// + HUỶ mọi ticket Housekeeping đang mở (<see cref="CancelOpenTicketsForVisitUseCase"/>) của visit đó. Ở Host
/// (composition root) vì điều phối HAI module qua Contracts/use case — không module nào "sở hữu" cascade (QR-AD-002).
/// <para>
/// Chạy trong scope consume (inbox <c>guest_access</c> khử trùng — QR-AD-027). Cả hai use case IDEMPOTENT (đóng hội
/// thoại đã đóng / huỷ khi không còn ticket mở = no-op), nên AT-LEAST-ONCE + idempotent = hiệu ứng ĐÚNG-MỘT-LẦN về
/// nghiệp vụ. Mỗi use case tự quản transaction module mình (Concierge/Housekeeping keyed). Use case thất bại (vd
/// concurrency) → NÉM để consume KHÔNG mark inbox → message redeliver (retry an toàn nhờ idempotency).
/// </para>
/// </summary>
public sealed partial class GuestVisitEndedCascadeHandler(
    ICommandUseCase<CloseConversationForVisitInput> closeConversation,
    IUseCase<CancelOpenTicketsForVisitInput, CancelOpenTicketsForVisitResult> cancelTickets,
    ILogger<GuestVisitEndedCascadeHandler> logger)
    : IIntegrationEventHandler<GuestVisitEndedIntegrationEvent>
{
    public async Task HandleAsync(GuestVisitEndedIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var visitId = integrationEvent.GuestVisitId;

        // (1) Đóng hội thoại Concierge của visit (idempotent — đã đóng/không có → no-op).
        var close = await closeConversation
            .ExecuteAsync(new CloseConversationForVisitInput(visitId), ct)
            .ConfigureAwait(false);
        if (close.IsFailure)
        {
            // KHÔNG nuốt lỗi: ném → consume không mark inbox → redeliver (at-least-once + idempotent an toàn).
            throw new InvalidOperationException(
                $"Cascade close-conversation thất bại cho visit {visitId}: {close.Error.Code}.");
        }

        // (2) Huỷ mọi ticket Housekeeping đang mở của visit (idempotent — không có ticket mở → count=0).
        var cancel = await cancelTickets
            .ExecuteAsync(new CancelOpenTicketsForVisitInput(visitId), ct)
            .ConfigureAwait(false);
        if (cancel.IsFailure)
        {
            throw new InvalidOperationException(
                $"Cascade cancel-tickets thất bại cho visit {visitId}: {cancel.Error.Code}.");
        }

        Log.Cascaded(logger, visitId, cancel.Value.CancelledCount, integrationEvent.Id);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information,
            Message = "Cascaded GuestVisitEnded (visitId={VisitId}, cancelledTickets={CancelledTickets}, eventId={EventId}).")]
        public static partial void Cascaded(ILogger logger, Guid visitId, int cancelledTickets, Guid eventId);
    }
}
