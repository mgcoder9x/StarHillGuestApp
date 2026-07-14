using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using GuestAccess.Domain;
using ResortConfig.Contracts.Queries;
using Rooms.Contracts;

namespace GuestAccess.Application;

/// <summary>
/// Phân giải token QR → phòng → GuestSession (thiết bị) → GuestVisit (nối lại Active + lazy idle-expiry + tạo mới),
/// đặt cửa sổ thao tác. Cross-module CHỈ qua Contracts: <see cref="IRoomTokenResolver"/> (Rooms) +
/// <see cref="IResortGuestConfigQuery"/> (ResortConfig). NON-DISCLOSURE khi token/phòng lỗi (Req 1.4 / CP1).
/// <para>
/// <b>Transaction &amp; race (QR-AD-026):</b> đọc cross-module TRƯỚC (ngoài transaction — không giữ khoá lâu),
/// rồi bọc CRITICAL SECTION (khóa session + đọc/ghi visit) trong <see cref="IUnitOfWork.ExecuteInTransactionAsync"/>
/// tường minh. <see cref="IGuestSessionStore.FindByKeyHashForUpdateAsync"/> khóa hàng session (<c>FOR UPDATE</c>)
/// → SERIALIZE resolve cùng thiết bị → hai request đua cùng cookie+phòng HỘI TỤ về CÙNG một visit Active. KHÔNG
/// dùng "insert → catch 23505 → query lại" của bản cũ (transaction abort sau unique-violation → query trong catch
/// không an toàn). Là <see cref="IUseCase{TInput,TOutput}"/> thường (không <c>ITransactionalUseCase</c>) → decorator
/// pipeline pass-through, use case tự quản transaction ở phạm vi hẹp nhất (mirror precedent <c>CreateRoom</c>).
/// </para>
/// </summary>
public sealed class ResolveTokenUseCase : IUseCase<ResolveTokenInput, ResolveTokenResult>
{
    private readonly IGuestSessionStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRoomTokenResolver _roomTokenResolver;
    private readonly IResortGuestConfigQuery _configQuery;
    private readonly IGuestSessionKeyHasher _hasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IClock _clock;

    public ResolveTokenUseCase(
        IGuestSessionStore store,
        IUnitOfWork unitOfWork,
        IRoomTokenResolver roomTokenResolver,
        IResortGuestConfigQuery configQuery,
        IGuestSessionKeyHasher hasher,
        ITokenGenerator tokenGenerator,
        IClock clock)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _roomTokenResolver = roomTokenResolver;
        _configQuery = configQuery;
        _hasher = hasher;
        _tokenGenerator = tokenGenerator;
        _clock = clock;
    }

    public async Task<Result<ResolveTokenResult>> ExecuteAsync(ResolveTokenInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Capability do Rooms phát hành = 32-byte base64url không padding (43 ASCII). Chặn malformed trước
        // resolver/DB: cùng public error qr_invalid, không lộ token tồn tại hay lý do sai (QR-AD-025/CP1).
        if (!GuestCredentialFormat.IsCanonical(input.RawToken))
        {
            return Result.Failure<ResolveTokenResult>(GuestAccessErrors.QrInvalid);
        }

        // Cookie không canonical được coi như thiết bị mới. Chuẩn hóa TRƯỚC transaction/hash để whitespace/chuỗi
        // quá dài/ký tự lạ không ném từ hasher và không tiêu tốn DB lookup.
        var currentSessionKey = GuestCredentialFormat.IsCanonical(input.CurrentSessionKey)
            ? input.CurrentSessionKey
            : null;

        // (1) token → phòng (cross-module, NGOÀI transaction). null = không tồn tại/thu hồi/xóa mềm → qr_invalid (CP1).
        var room = await _roomTokenResolver.ResolveActiveTokenAsync(input.RawToken, ct).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Failure<ResolveTokenResult>(GuestAccessErrors.QrInvalid);
        }

        if (!room.IsRoomActive)
        {
            return Result.Failure<ResolveTokenResult>(GuestAccessErrors.RoomInactive);
        }

        // (2) cấu hình nền (fail-closed): thiếu resort/settings/default-language → configuration_unavailable.
        var config = await _configQuery.GetAsync(room.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<ResolveTokenResult>(GuestAccessErrors.ConfigurationUnavailable);
        }

        var now = _clock.UtcNow;

        // (3) CRITICAL SECTION trong transaction hẹp: khóa session → nối/expire/tạo visit → persist.
        var outcome = await _unitOfWork.ExecuteInTransactionAsync(
            async token => await ResolveSessionAndVisitAsync(currentSessionKey, room.RoomId, room.ResortId, config, now, token).ConfigureAwait(false),
            ct).ConfigureAwait(false);

        return Result.Success(new ResolveTokenResult(
            room.RoomId,
            room.RoomNumber,
            room.Building,
            room.Floor,
            room.ResortId,
            config.ResortName,
            config.LogoUrl,
            outcome.VisitId,
            now.AddMinutes(config.PortalWindowMinutes),
            config.EnabledLanguageCodes,
            config.DefaultLanguageCode,
            config.FaqEnabled,
            config.ChatEnabled,
            config.HousekeepingEnabled,
            config.RequireRuleAckForFaq,
            config.RequireRuleAckForChat,
            config.RequireRuleAckForHousekeeping,
            outcome.IssuedSessionKey));
    }

    private async Task<(Guid VisitId, string? IssuedSessionKey)> ResolveSessionAndVisitAsync(
        string? currentSessionKey,
        Guid roomId,
        Guid resortId,
        ResortGuestConfig config,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var (session, issuedKey) = await ResolveSessionAsync(currentSessionKey, now, ct).ConfigureAwait(false);

        // Visit chạy DƯỚI khóa session (nếu session đã tồn tại) → không cần khóa visit riêng.
        var visit = await _store.FindActiveVisitAsync(session.Id, roomId, ct).ConfigureAwait(false);

        // Lazy idle-expiry: visit quá hạn KHÔNG nối lại (Req 10.5). Flush Expired TRƯỚC khi insert mới để không
        // đụng partial unique ux_guest_visit_active (không phụ thuộc thứ tự lệnh EF).
        if (visit is not null && now > visit.ExpiresAt)
        {
            visit.Status = GuestVisitStatus.Expired;
            visit.ClosedAt = now;
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            visit = null;
        }

        if (visit is null)
        {
            visit = new GuestVisit
            {
                ResortId = resortId,
                RoomId = roomId,
                GuestSessionId = session.Id,
                Status = GuestVisitStatus.Active,
                StartedAt = now,
                LastSeenAt = now,
                ExpiresAt = now.AddHours(config.VisitIdleExpiryHours),
            };
            _store.AddVisit(visit);
        }
        else
        {
            // Nối lại visit còn hạn → sliding window (Req 10.6).
            visit.LastSeenAt = now;
            visit.ExpiresAt = now.AddHours(config.VisitIdleExpiryHours);
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return (visit.Id, issuedKey);
    }

    private async Task<(GuestSession Session, string? IssuedKey)> ResolveSessionAsync(
        string? currentSessionKey, DateTimeOffset now, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(currentSessionKey))
        {
            var hash = _hasher.Hash(currentSessionKey);
            var existing = await _store.FindByKeyHashForUpdateAsync(hash, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                existing.LastSeenAt = now;
                return (existing, null);
            }
        }

        // Chưa có session (hoặc cookie không khớp) → cấp mới. Raw key CHỈ trả để set cookie; DB lưu hash.
        var rawKey = _tokenGenerator.NewToken();
        var session = new GuestSession
        {
            SessionKeyHash = _hasher.Hash(rawKey),
            FirstSeenAt = now,
            LastSeenAt = now,
        };
        _store.AddSession(session);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false); // persist để có Id cho visit.
        return (session, rawKey);
    }
}
