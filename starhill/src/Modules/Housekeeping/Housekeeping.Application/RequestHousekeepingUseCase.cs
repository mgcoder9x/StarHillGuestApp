using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Housekeeping.Domain;
using ResortConfig.Contracts.Queries;
using Rules.Contracts;

namespace Housekeeping.Application;

/// <summary>
/// Khách yêu cầu dọn phòng (Req 6.1/6.2). Read-write value-returning <see cref="IUseCase{TInput,TOutput}"/> tự quản
/// một SaveChanges (mirror CreateRoom/CreateFaqCategory). Thứ tự (fail-closed + gate TRƯỚC tạo — CP3):
/// <list type="number">
/// <item>Config qua <see cref="IResortGuestConfigQuery"/> — thiếu → <c>configuration_unavailable</c>.</item>
/// <item><c>HousekeepingEnabled=false</c> → <c>housekeeping_disabled</c> (backend enforce feature-flag — Req 14).</item>
/// <item>RULE-GATE <see cref="IRuleGate.EnsureAcknowledgedAsync"/> với <see cref="GuestFeature.Housekeeping"/> (CP3,
/// defense-in-depth trong use case).</item>
/// <item>IDEMPOTENT (Req 6.2): đã có ticket MỞ của phòng → trả ticket đó (nhắc), KHÔNG tạo trùng. Race tạo đồng thời
/// → bắt <see cref="UniqueConstraintViolationException"/> (partial-unique <c>ux_hk_open_ticket_room</c>) → re-query trả existing.</item>
/// </list>
/// Tạo ticket <c>Requested</c> + <see cref="HousekeepingEvent"/>(Guest) trong CÙNG transaction (Req 6.8 log).
/// </summary>
public sealed class RequestHousekeepingUseCase : IUseCase<RequestHousekeepingInput, RequestHousekeepingResult>
{
    private readonly IResortGuestConfigQuery _configQuery;
    private readonly IRuleGate _ruleGate;
    private readonly IRepository<HousekeepingTicket> _tickets;
    private readonly IRepository<HousekeepingEvent> _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RequestHousekeepingUseCase(
        IResortGuestConfigQuery configQuery,
        IRuleGate ruleGate,
        IRepository<HousekeepingTicket> tickets,
        IRepository<HousekeepingEvent> events,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _configQuery = configQuery;
        _ruleGate = ruleGate;
        _tickets = tickets;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<RequestHousekeepingResult>> ExecuteAsync(
        RequestHousekeepingInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var config = await _configQuery.GetAsync(input.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<RequestHousekeepingResult>(HousekeepingErrors.ConfigurationUnavailable);
        }

        if (!config.HousekeepingEnabled)
        {
            return Result.Failure<RequestHousekeepingResult>(HousekeepingErrors.Disabled);
        }

        var gate = await _ruleGate
            .EnsureAcknowledgedAsync(input.ResortId, input.GuestVisitId, GuestFeature.Housekeeping, ct)
            .ConfigureAwait(false);
        if (gate.IsFailure)
        {
            return Result.Failure<RequestHousekeepingResult>(gate.Error);
        }

        // Idempotent (Req 6.2): đã có ticket MỞ của phòng → trả ticket đó (nhắc).
        var existing = await FindOpenTicketAsync(input.RoomId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result.Success(new RequestHousekeepingResult(existing.Id, existing.Status, AlreadyOpen: true));
        }

        var now = _clock.UtcNow;
        var ticket = new HousekeepingTicket
        {
            ResortId = input.ResortId,
            RoomId = input.RoomId,
            RequestedByGuestSessionId = input.GuestSessionId,
            GuestVisitId = input.GuestVisitId,
            Status = HousekeepingStatus.Requested,
            CreatedAt = now,
        };
        _tickets.Add(ticket);
        _events.Add(new HousekeepingEvent
        {
            HousekeepingTicketId = ticket.Id,
            NewStatus = HousekeepingStatus.Requested,
            ActorType = HousekeepingActorType.Guest,
            CreatedAt = now,
        });

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // Race: request khác vừa tạo ticket mở cho phòng này → trả ticket mở hiện có (idempotent, không lỗi).
            var raced = await FindOpenTicketAsync(input.RoomId, ct).ConfigureAwait(false);
            return raced is not null
                ? Result.Success(new RequestHousekeepingResult(raced.Id, raced.Status, AlreadyOpen: true))
                : Result.Failure<RequestHousekeepingResult>(HousekeepingErrors.NoOpenTicket);
        }

        return Result.Success(new RequestHousekeepingResult(ticket.Id, ticket.Status, AlreadyOpen: false));
    }

    private Task<HousekeepingTicket?> FindOpenTicketAsync(Guid roomId, CancellationToken ct) =>
        _tickets.FirstOrDefaultAsync(
            t => t.RoomId == roomId
                 && (t.Status == HousekeepingStatus.Requested || t.Status == HousekeepingStatus.InProgress),
            ct);
}
