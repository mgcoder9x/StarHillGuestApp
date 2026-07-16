using Bedrock.Domain.Results;
using Housekeeping.Domain;

namespace Housekeeping.Application;

/// <summary>
/// Máy trạng thái ticket dọn phòng (Req 6) — nguồn DUY NHẤT của luật chuyển trạng thái + tạo <see cref="HousekeepingEvent"/>
/// (Req 6.8 log mỗi transition). Dùng chung bởi mọi use case đổi trạng thái (set-status/complete/cancel) để không lặp
/// luật + không quên ghi event. Terminal (Done/Cancelled) không đổi tiếp. Áp mốc thời gian tương ứng (StartedAt/CompletedAt).
/// </summary>
internal static class HousekeepingStateMachine
{
    /// <summary>Chuyển hợp lệ: Requested→{InProgress,Done,Cancelled}; InProgress→{Done,Cancelled}; terminal→{}.</summary>
    public static bool IsValidTransition(HousekeepingStatus from, HousekeepingStatus to) => from switch
    {
        HousekeepingStatus.Requested => to is HousekeepingStatus.InProgress or HousekeepingStatus.Done or HousekeepingStatus.Cancelled,
        HousekeepingStatus.InProgress => to is HousekeepingStatus.Done or HousekeepingStatus.Cancelled,
        _ => false, // Done/Cancelled = terminal.
    };

    /// <summary>
    /// Áp chuyển trạng thái + tạo event nhật ký. Trả <c>null</c> nếu OK (kèm <paramref name="event"/> để use case Add);
    /// ngược lại trả <see cref="HousekeepingErrors.InvalidTransition"/>. Set StartedAt (→InProgress) / CompletedAt+
    /// CompletedByUserId+CompletionMethod (→Done). Method chỉ ghi khi →Done.
    /// </summary>
    public static Error? TryApply(
        HousekeepingTicket ticket,
        HousekeepingStatus newStatus,
        HousekeepingActorType actorType,
        Guid? actorUserId,
        HousekeepingCompletionMethod? method,
        DateTimeOffset now,
        out HousekeepingEvent? @event)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        @event = null;

        if (!IsValidTransition(ticket.Status, newStatus))
        {
            return HousekeepingErrors.InvalidTransition;
        }

        ticket.Status = newStatus;
        if (newStatus == HousekeepingStatus.InProgress)
        {
            ticket.StartedAt = now;
        }
        else if (newStatus == HousekeepingStatus.Done)
        {
            ticket.CompletedAt = now;
            ticket.CompletedByUserId = actorUserId;
            ticket.CompletionMethod = method;
        }

        @event = new HousekeepingEvent
        {
            HousekeepingTicketId = ticket.Id,
            NewStatus = newStatus,
            ActorType = actorType,
            ActorUserId = actorUserId,
            Method = newStatus == HousekeepingStatus.Done ? method : null,
            CreatedAt = now,
        };
        return null;
    }
}
