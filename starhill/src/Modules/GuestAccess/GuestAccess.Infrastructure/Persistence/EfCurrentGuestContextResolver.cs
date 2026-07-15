using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Domain.Results;
using GuestAccess.Application;
using GuestAccess.Contracts;
using GuestAccess.Domain;
using Microsoft.EntityFrameworkCore;
using ResortConfig.Contracts.Queries;

namespace GuestAccess.Infrastructure.Persistence;

/// <summary>
/// Impl EF của <see cref="ICurrentGuestContextResolver"/> (C-GA.4 — QR-AD-032). Portal-window CHECK-BEFORE-TOUCH:
/// <see cref="ResolveAsync"/> chỉ ĐỌC (no-tracking) + kiểm window; <see cref="TouchAsync"/> mới ghi (trượt cửa sổ).
/// <para>
/// Portal-window (phút) suy từ <c>GuestVisit.LastSeenAt + PortalWindowMinutes</c> (KHÔNG vật hoá) → phải đọc
/// <see cref="IResortGuestConfigQuery"/> lấy PortalWindowMinutes (fail-closed nếu cấu hình thiếu). Idle-expiry (giờ)
/// đã vật hoá ở <c>ExpiresAt</c>; TouchAsync BẢO TOÀN delta idle (<c>ExpiresAt - LastSeenAt</c>) khi trượt → không
/// cần đọc cấu hình ở đường touch (robust, không nhánh fail-closed thừa). Persist qua keyed <see cref="IUnitOfWork"/>
/// (guest_access) — cùng scope nên cùng GuestAccessDbContext instance đã track visit.
/// </para>
/// </summary>
public sealed class EfCurrentGuestContextResolver(
    GuestAccessDbContext db,
    IResortGuestConfigQuery configQuery,
    IGuestSessionKeyHasher hasher,
    IUnitOfWork unitOfWork,
    IClock clock) : ICurrentGuestContextResolver
{
    public async Task<Result<CurrentGuestContext>> ResolveAsync(
        string? sessionKey, Guid roomId, CancellationToken ct = default)
    {
        // Cookie rỗng → thiết bị không xác định. (Cookie lạ sẽ không khớp phiên nào → cùng guest_context_missing bên dưới.)
        if (string.IsNullOrEmpty(sessionKey))
        {
            return Result.Failure<CurrentGuestContext>(GuestAccessErrors.GuestContextMissing);
        }

        // Dùng CHUNG hasher với đường resolve-token (nguồn hash DUY NHẤT — không lệch thuật toán).
        var hash = hasher.Hash(sessionKey);

        var session = await db.GuestSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionKeyHash == hash, ct)
            .ConfigureAwait(false);
        if (session is null)
        {
            return Result.Failure<CurrentGuestContext>(GuestAccessErrors.GuestContextMissing);
        }

        // Có thiết bị nhưng không còn visit Active cho phòng này (đã đóng/hết hạn/chưa quét phòng) → session_expired.
        var visit = await db.GuestVisits
            .AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.GuestSessionId == session.Id && v.RoomId == roomId && v.Status == GuestVisitStatus.Active,
                ct)
            .ConfigureAwait(false);
        if (visit is null)
        {
            return Result.Failure<CurrentGuestContext>(GuestAccessErrors.SessionExpired);
        }

        // Portal-window từ cấu hình (fail-closed nếu nền thiếu — nhất quán resolve QR-AD-024).
        var config = await configQuery.GetAsync(visit.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<CurrentGuestContext>(GuestAccessErrors.ConfigurationUnavailable);
        }

        // Quá portal-window (phút) → session_expired. (Idle-expiry giờ dài hơn nên đã bị bao bởi kiểm này.)
        if (clock.UtcNow - visit.LastSeenAt > TimeSpan.FromMinutes(config.PortalWindowMinutes))
        {
            return Result.Failure<CurrentGuestContext>(GuestAccessErrors.SessionExpired);
        }

        return Result.Success(new CurrentGuestContext(visit.Id, session.Id, visit.RoomId, visit.ResortId));
    }

    public async Task TouchAsync(Guid guestVisitId, CancellationToken ct = default)
    {
        var visit = await db.GuestVisits
            .FirstOrDefaultAsync(v => v.Id == guestVisitId, ct)
            .ConfigureAwait(false);

        // No-op nếu visit không tồn tại hoặc đã đóng — không hồi sinh phiên đã kết thúc (check-before-touch).
        if (visit is null || visit.Status != GuestVisitStatus.Active)
        {
            return;
        }

        var now = clock.UtcNow;
        var idleDelta = visit.ExpiresAt - visit.LastSeenAt; // bảo toàn độ dài idle đã cấu hình lúc resolve.
        visit.LastSeenAt = now;
        visit.ExpiresAt = now + idleDelta;

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
