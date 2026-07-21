using System.Text.RegularExpressions;
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
/// <item>VALIDATE + chuẩn hoá <see cref="RequestHousekeepingInput.Details"/> (INV-HK1/HK2) — TRƯỚC idempotency để
/// malformed luôn bị từ chối nhất quán → <c>housekeeping_invalid_request</c>.</item>
/// <item>IDEMPOTENT (Req 6.2): đã có ticket MỞ của phòng → trả ticket đó (nhắc), KHÔNG tạo trùng, KHÔNG áp Details mới
/// (yêu cầu đầu thắng — INV-HK3). Race tạo đồng thời → bắt <see cref="UniqueConstraintViolationException"/>
/// (partial-unique <c>ux_hk_open_ticket_room</c>) → re-query trả existing.</item>
/// </list>
/// Tạo ticket <c>Requested</c> + <see cref="HousekeepingEvent"/>(Guest) trong CÙNG transaction (Req 6.8 log).
/// </summary>
public sealed partial class RequestHousekeepingUseCase : IUseCase<RequestHousekeepingInput, RequestHousekeepingResult>
{
    private const int MaxNoteLength = 500;
    private const int MinAmenity = 0;
    private const int MaxAmenity = 5;

    // HH:mm 24 giờ (00:00–23:59). Source-generated regex (miễn phí runtime compile; provider-agnostic).
    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex SpecificTimeRegex();

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

        // VALIDATE + chuẩn hoá chi tiết TRƯỚC idempotency (malformed luôn bị từ chối nhất quán — INV-HK1/HK2).
        var validation = ValidateAndNormalize(input.Details, out var details);
        if (validation is not null)
        {
            return Result.Failure<RequestHousekeepingResult>(validation);
        }

        // Idempotent (Req 6.2): đã có ticket MỞ của phòng → trả ticket đó (nhắc). KHÔNG áp Details mới (yêu cầu đầu thắng — INV-HK3).
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
            ServiceType = details.ServiceType,
            PreferredTime = details.PreferredTime,
            PreferredTimeText = details.PreferredTimeText,
            AmenityToothbrush = details.AmenityToothbrush,
            AmenityTowel = details.AmenityTowel,
            AmenityWater = details.AmenityWater,
            AmenitySoap = details.AmenitySoap,
            Note = details.Note,
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

    /// <summary>
    /// Validate + chuẩn hoá chi tiết yêu cầu (INV-HK1/HK2). Trả <c>null</c> nếu hợp lệ (kèm <paramref name="normalized"/>
    /// đã chuẩn hoá: PreferredTimeText ép null khi ≠SpecificTime; Note trim, rỗng→null), ngược lại trả Error.
    /// </summary>
    private static Error? ValidateAndNormalize(HousekeepingRequestDetails input, out HousekeepingRequestDetails normalized)
    {
        normalized = input;

        if (!Enum.IsDefined(input.ServiceType) || !Enum.IsDefined(input.PreferredTime))
        {
            return HousekeepingErrors.InvalidRequest;
        }

        if (input.AmenityToothbrush is < MinAmenity or > MaxAmenity
            || input.AmenityTowel is < MinAmenity or > MaxAmenity
            || input.AmenityWater is < MinAmenity or > MaxAmenity
            || input.AmenitySoap is < MinAmenity or > MaxAmenity)
        {
            return HousekeepingErrors.InvalidRequest;
        }

        // PreferredTimeText: bắt buộc + đúng HH:mm khi SpecificTime; ép null khi loại khác.
        string? timeText = null;
        if (input.PreferredTime == HousekeepingPreferredTime.SpecificTime)
        {
            var candidate = input.PreferredTimeText?.Trim();
            if (string.IsNullOrEmpty(candidate) || !SpecificTimeRegex().IsMatch(candidate))
            {
                return HousekeepingErrors.InvalidRequest;
            }

            timeText = candidate;
        }

        // Note: trim, rỗng→null, ≤ MaxNoteLength.
        var note = input.Note?.Trim();
        if (string.IsNullOrEmpty(note))
        {
            note = null;
        }
        else if (note.Length > MaxNoteLength)
        {
            return HousekeepingErrors.InvalidRequest;
        }

        normalized = input with { PreferredTimeText = timeText, Note = note };
        return null;
    }
}
