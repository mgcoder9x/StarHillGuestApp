using ResortQr.Application.Abstractions;
using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Common;
using ResortQr.Domain.GuestAccess;
using ResortQr.Domain.Resorts;
using ResortQr.Domain.Rooms;
using ResortQr.SharedKernel.DependencyInjection;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.GuestAccess;

/// <summary>
/// Phân giải token QR → phòng → GuestSession (thiết bị) → GuestVisit (lazy idle-expiry + nối lại Active
/// + tạo mới có race DB-arbitrated + sliding window). Bám 13 §3. KHÔNG lộ phòng khác khi token lỗi (Req 1.4).
/// Hoãn (wave sau): rule_ack, cascade EndVisit, danh sách ngôn ngữ đầy đủ.
/// </summary>
public sealed class ResolveTokenUseCase : IUseCase<ResolveTokenInput, ResolveTokenResult>, IScopedService
{
    private const int DefaultIdleHours = 24;
    private const int DefaultPortalMinutes = 30;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IGuestSessionKeyHasher _sessionKeyHasher;

    public ResolveTokenUseCase(
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ITokenGenerator tokenGenerator,
        IGuestSessionKeyHasher sessionKeyHasher)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
        _tokenGenerator = tokenGenerator;
        _sessionKeyHasher = sessionKeyHasher;
    }

    public async Task<Result<ResolveTokenResult>> ExecuteAsync(ResolveTokenInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var now = _clock.UtcNow;

        // (1) token → phòng (không lộ phòng khác nếu lỗi).
        var token = await _unitOfWork.Repository<RoomQrToken>()
            .FirstOrDefaultAsync(t => t.Token == input.RawToken, cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return Result.Fail<ResolveTokenResult>(GuestAccessErrors.QrInvalid);
        }

        if (token.Status == RoomQrTokenStatus.Revoked)
        {
            return Result.Fail<ResolveTokenResult>(GuestAccessErrors.QrRevoked);
        }

        // FirstOrDefault áp global query filter → soft-deleted room trả null (đã verify bằng test persistence).
        var room = await _unitOfWork.Repository<Room>()
            .FirstOrDefaultAsync(r => r.Id == token.RoomId, cancellationToken).ConfigureAwait(false);
        if (room is null || room.Status != RoomStatus.Active)
        {
            return Result.Fail<ResolveTokenResult>(GuestAccessErrors.RoomInactive);
        }

        var resort = await _unitOfWork.Repository<Resort>()
            .FindByIdAsync(room.ResortId, cancellationToken).ConfigureAwait(false);
        if (resort is null)
        {
            return Result.Fail<ResolveTokenResult>(GuestAccessErrors.RoomInactive);
        }

        var settings = await _unitOfWork.Repository<ResortSettings>()
            .FirstOrDefaultAsync(s => s.ResortId == resort.Id, cancellationToken).ConfigureAwait(false);
        var idleHours = settings?.VisitIdleExpiryHours ?? DefaultIdleHours;
        var portalMinutes = settings?.PortalWindowMinutes ?? DefaultPortalMinutes;

        // (2) GuestSession (định danh thiết bị) — lưu TRƯỚC (tách khỏi visit → không entangle race visit).
        var (session, issuedKey) = await ResolveSessionAsync(input.CurrentSessionKey, now, cancellationToken).ConfigureAwait(false);

        // (3) GuestVisit — lazy idle-expiry + nối lại + tạo mới (race).
        var visit = await ResolveVisitAsync(session.Id, resort.Id, room.Id, now, idleHours, cancellationToken).ConfigureAwait(false);

        var defaultLang = await _unitOfWork.Repository<ResortLanguage>()
            .FirstOrDefaultAsync(l => l.ResortId == resort.Id && l.IsDefault, cancellationToken).ConfigureAwait(false);

        return Result.Ok(new ResolveTokenResult(
            room.Id,
            room.RoomNumber,
            resort.Id,
            resort.Name,
            visit.Id,
            visit.LastSeenAt.AddMinutes(portalMinutes),
            issuedKey,
            settings?.FaqEnabled ?? true,
            settings?.ChatEnabled ?? true,
            settings?.HousekeepingEnabled ?? true,
            defaultLang?.Code));
    }

    private async Task<(GuestSession Session, string? IssuedKey)> ResolveSessionAsync(
        string? currentSessionKey, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var sessions = _unitOfWork.Repository<GuestSession>();

        GuestSession? session = null;
        if (!string.IsNullOrEmpty(currentSessionKey))
        {
            var hash = _sessionKeyHasher.Hash(currentSessionKey);
            session = await sessions.FirstOrDefaultAsync(s => s.SessionKeyHash == hash, cancellationToken).ConfigureAwait(false);
        }

        if (session is null)
        {
            var rawKey = _tokenGenerator.NewToken();
            session = new GuestSession
            {
                SessionKeyHash = _sessionKeyHasher.Hash(rawKey),
                FirstSeenAt = now,
                LastSeenAt = now,
            };
            sessions.Add(session);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return (session, rawKey);
        }

        session.LastSeenAt = now;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (session, null);
    }

    private async Task<GuestVisit> ResolveVisitAsync(
        Guid sessionId, Guid resortId, Guid roomId, DateTimeOffset now, int idleHours, CancellationToken cancellationToken)
    {
        var visits = _unitOfWork.Repository<GuestVisit>();

        // ux_visit_active → ≤1 Active/(session,room).
        var visit = await visits
            .FirstOrDefaultAsync(v => v.GuestSessionId == sessionId && v.RoomId == roomId && v.Status == GuestVisitStatus.Active, cancellationToken)
            .ConfigureAwait(false);

        // Lazy idle-expiry: KHÔNG nối lại visit đã quá hạn (Req 10.5) — độc lập sweeper (13 §2).
        if (visit is not null && now > visit.ExpiresAt)
        {
            visit.Status = GuestVisitStatus.Expired;
            visit.ClosedAt = now;
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            visit = null;
        }

        if (visit is null)
        {
            var fresh = new GuestVisit
            {
                ResortId = resortId,
                RoomId = roomId,
                GuestSessionId = sessionId,
                Status = GuestVisitStatus.Active,
                StartedAt = now,
                LastSeenAt = now,
                ExpiresAt = now.AddHours(idleHours),
            };
            visits.Add(fresh);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return fresh;
            }
            catch (UniqueConstraintViolationException)
            {
                // Race: request song song (cùng session) vừa tạo visit Active → dùng lại (không lỗi ra khách).
                var existing = await visits
                    .FirstOrDefaultAsync(v => v.GuestSessionId == sessionId && v.RoomId == roomId && v.Status == GuestVisitStatus.Active, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is null)
                {
                    throw; // không phải race visit → lỗi thật, không nuốt.
                }

                return existing;
            }
        }

        // Nối lại visit còn hạn → sliding window (Req 10.6).
        visit.LastSeenAt = now;
        visit.ExpiresAt = now.AddHours(idleHours);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return visit;
    }
}
