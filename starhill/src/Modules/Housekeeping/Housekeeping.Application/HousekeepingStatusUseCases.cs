using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using Housekeeping.Domain;
using Rooms.Contracts;

namespace Housekeeping.Application;

/// <summary>
/// Nhân viên đổi trạng thái ticket theo id (Req 6.4 — InProgress/Done). Không tồn tại → <see cref="HousekeepingErrors.TicketNotFound"/>;
/// chuyển không hợp lệ → <see cref="HousekeepingErrors.InvalidTransition"/> (máy trạng thái). Ghi event(Staff) cùng transaction
/// (Req 6.8). Value-returning một SaveChanges. Concurrency xmin (CP15). Chỉ cho phép tiến tới InProgress/Done (Cancelled qua cascade).
/// </summary>
public sealed class SetHousekeepingStatusUseCase : IUseCase<SetHousekeepingStatusInput, HousekeepingTicketResult>
{
    private readonly IRepository<HousekeepingTicket> _tickets;
    private readonly IRepository<HousekeepingEvent> _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetHousekeepingStatusUseCase(
        IRepository<HousekeepingTicket> tickets, IRepository<HousekeepingEvent> events, IUnitOfWork unitOfWork, IClock clock)
    {
        _tickets = tickets;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<HousekeepingTicketResult>> ExecuteAsync(
        SetHousekeepingStatusInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Endpoint set-status chỉ tiến tới InProgress/Done (Cancelled do cascade — không cho set tuỳ ý ở đây).
        if (input.NewStatus is not (HousekeepingStatus.InProgress or HousekeepingStatus.Done))
        {
            return Result.Failure<HousekeepingTicketResult>(HousekeepingErrors.InvalidTransition);
        }

        var ticket = await _tickets.FindByIdAsync(input.TicketId, ct).ConfigureAwait(false);
        if (ticket is null)
        {
            return Result.Failure<HousekeepingTicketResult>(HousekeepingErrors.TicketNotFound);
        }

        var method = input.NewStatus == HousekeepingStatus.Done
            ? input.Method ?? HousekeepingCompletionMethod.App
            : (HousekeepingCompletionMethod?)null;

        var error = HousekeepingStateMachine.TryApply(
            ticket, input.NewStatus, HousekeepingActorType.Staff, input.ActorUserId, method, _clock.UtcNow, out var @event);
        if (error is not null)
        {
            return Result.Failure<HousekeepingTicketResult>(error);
        }

        _events.Add(@event!);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(new HousekeepingTicketResult(ticket.Id, ticket.Status));
    }
}

/// <summary>Validate set-status (TicketId bắt buộc).</summary>
public sealed class SetHousekeepingStatusValidator : AbstractValidator<SetHousekeepingStatusInput>
{
    public SetHousekeepingStatusValidator() => RuleFor(x => x.TicketId).NotEmpty();
}

/// <summary>
/// Hoàn tất ticket MỞ của một phòng (Req 6.4 — chọn phòng trong app, method App). Không có ticket mở →
/// <see cref="HousekeepingErrors.NoOpenTicket"/>. Ghi event(Staff, App). Value-returning một SaveChanges.
/// </summary>
public sealed class CompleteHousekeepingByRoomUseCase : IUseCase<CompleteHousekeepingByRoomInput, HousekeepingTicketResult>
{
    private readonly IRepository<HousekeepingTicket> _tickets;
    private readonly IRepository<HousekeepingEvent> _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CompleteHousekeepingByRoomUseCase(
        IRepository<HousekeepingTicket> tickets, IRepository<HousekeepingEvent> events, IUnitOfWork unitOfWork, IClock clock)
    {
        _tickets = tickets;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<Result<HousekeepingTicketResult>> ExecuteAsync(
        CompleteHousekeepingByRoomInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        return HousekeepingCompletion.CompleteOpenTicketAsync(
            _tickets, _events, _unitOfWork, _clock, input.RoomId, input.ActorUserId, HousekeepingCompletionMethod.App, ct);
    }
}

/// <summary>
/// Hoàn tất ticket MỞ qua QUÉT QR phòng (Req 6.5 — lối tắt, method StaffScan). Token phân giải qua
/// <see cref="IRoomTokenResolver"/> (QR CHỈ định phòng, KHÔNG thay đăng nhập — staff vẫn RequireStaff ở Api). Token
/// lạ/thu hồi → <see cref="HousekeepingErrors.QrInvalid"/>; phòng không có ticket mở → <see cref="HousekeepingErrors.NoOpenTicket"/>.
/// </summary>
public sealed class CompleteHousekeepingByTokenUseCase : IUseCase<CompleteHousekeepingByTokenInput, HousekeepingTicketResult>
{
    private readonly IRoomTokenResolver _tokenResolver;
    private readonly IRepository<HousekeepingTicket> _tickets;
    private readonly IRepository<HousekeepingEvent> _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CompleteHousekeepingByTokenUseCase(
        IRoomTokenResolver tokenResolver,
        IRepository<HousekeepingTicket> tickets,
        IRepository<HousekeepingEvent> events,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _tokenResolver = tokenResolver;
        _tickets = tickets;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<HousekeepingTicketResult>> ExecuteAsync(
        CompleteHousekeepingByTokenInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var resolution = await _tokenResolver.ResolveActiveTokenAsync(input.Token, ct).ConfigureAwait(false);
        if (resolution is null)
        {
            return Result.Failure<HousekeepingTicketResult>(HousekeepingErrors.QrInvalid);
        }

        return await HousekeepingCompletion.CompleteOpenTicketAsync(
            _tickets, _events, _unitOfWork, _clock, resolution.RoomId, input.ActorUserId,
            HousekeepingCompletionMethod.StaffScan, ct).ConfigureAwait(false);
    }
}

/// <summary>Validate complete-by-token (Token bắt buộc).</summary>
public sealed class CompleteHousekeepingByTokenValidator : AbstractValidator<CompleteHousekeepingByTokenInput>
{
    public CompleteHousekeepingByTokenValidator() => RuleFor(x => x.Token).NotEmpty();
}

/// <summary>
/// Nhân viên chủ động tạo ticket cho phòng (Req 6.9). Idempotent 1-mở/phòng (như guest nhưng KHÔNG gate/flag — vận
/// hành nội bộ). Ghi event(Staff). Race → bắt unique → trả existing.
/// </summary>
public sealed class CreateHousekeepingByStaffUseCase : IUseCase<CreateHousekeepingByStaffInput, RequestHousekeepingResult>
{
    private readonly IRepository<HousekeepingTicket> _tickets;
    private readonly IRepository<HousekeepingEvent> _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateHousekeepingByStaffUseCase(
        IRepository<HousekeepingTicket> tickets, IRepository<HousekeepingEvent> events, IUnitOfWork unitOfWork, IClock clock)
    {
        _tickets = tickets;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<RequestHousekeepingResult>> ExecuteAsync(
        CreateHousekeepingByStaffInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var existing = await FindOpenAsync(input.RoomId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result.Success(new RequestHousekeepingResult(existing.Id, existing.Status, AlreadyOpen: true));
        }

        var now = _clock.UtcNow;
        var ticket = new HousekeepingTicket
        {
            ResortId = input.ResortId,
            RoomId = input.RoomId,
            Status = HousekeepingStatus.Requested,
            CreatedAt = now,
        };
        _tickets.Add(ticket);
        _events.Add(new HousekeepingEvent
        {
            HousekeepingTicketId = ticket.Id,
            NewStatus = HousekeepingStatus.Requested,
            ActorType = HousekeepingActorType.Staff,
            ActorUserId = input.ActorUserId,
            CreatedAt = now,
        });

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            var raced = await FindOpenAsync(input.RoomId, ct).ConfigureAwait(false);
            return raced is not null
                ? Result.Success(new RequestHousekeepingResult(raced.Id, raced.Status, AlreadyOpen: true))
                : Result.Failure<RequestHousekeepingResult>(HousekeepingErrors.NoOpenTicket);
        }

        return Result.Success(new RequestHousekeepingResult(ticket.Id, ticket.Status, AlreadyOpen: false));
    }

    private Task<HousekeepingTicket?> FindOpenAsync(Guid roomId, CancellationToken ct) =>
        _tickets.FirstOrDefaultAsync(
            t => t.RoomId == roomId
                 && (t.Status == HousekeepingStatus.Requested || t.Status == HousekeepingStatus.InProgress),
            ct);
}

/// <summary>
/// Huỷ mọi ticket MỞ do một lượt lưu trú tạo (cascade khi visit kết thúc — CP9/Req 10.8). Đọc id ticket mở qua
/// read-model <see cref="IHousekeepingReader"/> (F9), load từng cái + chuyển Cancelled + event(System) trong CÙNG
/// transaction. WIRING event-driven (consumer GuestVisitEnded) ở C-GA.5 — use case này là capability (test độc lập).
/// </summary>
public sealed class CancelOpenTicketsForVisitUseCase : IUseCase<CancelOpenTicketsForVisitInput, CancelOpenTicketsForVisitResult>
{
    private readonly IHousekeepingReader _reader;
    private readonly IRepository<HousekeepingTicket> _tickets;
    private readonly IRepository<HousekeepingEvent> _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelOpenTicketsForVisitUseCase(
        IHousekeepingReader reader,
        IRepository<HousekeepingTicket> tickets,
        IRepository<HousekeepingEvent> events,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _reader = reader;
        _tickets = tickets;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CancelOpenTicketsForVisitResult>> ExecuteAsync(
        CancelOpenTicketsForVisitInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var ids = await _reader.ListOpenTicketIdsByVisitAsync(input.GuestVisitId, ct).ConfigureAwait(false);
        var now = _clock.UtcNow;
        var cancelled = 0;

        foreach (var id in ids)
        {
            var ticket = await _tickets.FindByIdAsync(id, ct).ConfigureAwait(false);
            if (ticket is null)
            {
                continue; // đã bị đổi trạng thái trong lúc chờ — bỏ qua (idempotent).
            }

            var error = HousekeepingStateMachine.TryApply(
                ticket, HousekeepingStatus.Cancelled, HousekeepingActorType.System, null, null, now, out var @event);
            if (error is null)
            {
                _events.Add(@event!);
                cancelled++;
            }
        }

        if (cancelled > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return Result.Success(new CancelOpenTicketsForVisitResult(cancelled));
    }
}

/// <summary>Logic dùng chung hoàn tất ticket MỞ của một phòng (complete-by-room/token) — tránh lặp.</summary>
internal static class HousekeepingCompletion
{
    public static async Task<Result<HousekeepingTicketResult>> CompleteOpenTicketAsync(
        IRepository<HousekeepingTicket> tickets,
        IRepository<HousekeepingEvent> events,
        IUnitOfWork unitOfWork,
        IClock clock,
        Guid roomId,
        Guid? actorUserId,
        HousekeepingCompletionMethod method,
        CancellationToken ct)
    {
        var ticket = await tickets.FirstOrDefaultAsync(
            t => t.RoomId == roomId
                 && (t.Status == HousekeepingStatus.Requested || t.Status == HousekeepingStatus.InProgress),
            ct).ConfigureAwait(false);
        if (ticket is null)
        {
            return Result.Failure<HousekeepingTicketResult>(HousekeepingErrors.NoOpenTicket);
        }

        var error = HousekeepingStateMachine.TryApply(
            ticket, HousekeepingStatus.Done, HousekeepingActorType.Staff, actorUserId, method, clock.UtcNow, out var @event);
        if (error is not null)
        {
            return Result.Failure<HousekeepingTicketResult>(error);
        }

        events.Add(@event!);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(new HousekeepingTicketResult(ticket.Id, ticket.Status));
    }
}
