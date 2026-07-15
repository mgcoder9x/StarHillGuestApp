using Bedrock.Domain.Results;

namespace GuestAccess.Contracts;

/// <summary>
/// Ngữ cảnh khách HIỆN HÀNH cho một phòng — GuestVisit Active còn trong portal-window, phân giải từ cookie thiết bị.
/// Mọi Id là Guid TRẦN (không lộ entity GuestAccess). Consumer (Rules/Faq/Concierge/Housekeeping) dùng
/// <see cref="GuestVisitId"/>/<see cref="ResortId"/> để gọi rule-gate + ghi ack, KHÔNG chạm schema guest_access.
/// </summary>
public sealed record CurrentGuestContext(Guid GuestVisitId, Guid GuestSessionId, Guid RoomId, Guid ResortId);

/// <summary>
/// Port cross-module (C-GA.4 — QR-AD-032) để các module guest-facing lấy GuestVisit hiện hành từ cookie thiết bị +
/// enforce PORTAL-WINDOW theo mô hình <b>check-before-touch</b> (design §resolve bước 5). GuestVisit thuộc schema
/// <c>guest_access</c> nên port đặt ở <c>GuestAccess.Contracts</c> (QR-AD-024 cấm module khác đọc GuestAccessDbContext).
/// Trả <see cref="Result{T}"/> (Bedrock.Domain — nguyên thủy shared kernel, KHÔNG phải Application/Infra) để consumer
/// map thẳng Error → HTTP.
/// <para>
/// <b>Vì sao tách Resolve/Touch:</b> nếu gia hạn (touch) TRƯỚC khi kiểm window thì cửa sổ tự gia hạn vô hạn, không
/// bao giờ hết hạn (design cảnh báo tận gốc). Do đó: <see cref="ResolveAsync"/> CHỈ đọc + kiểm window (KHÔNG touch);
/// endpoint chạy nghiệp vụ; nếu thành công mới gọi <see cref="TouchAsync"/> để trượt cửa sổ.
/// </para>
/// </summary>
public interface ICurrentGuestContextResolver
{
    /// <summary>
    /// Phân giải ngữ cảnh khách cho (<paramref name="sessionKey"/> cookie, <paramref name="roomId"/>) mà KHÔNG touch:
    /// <list type="bullet">
    /// <item>Success — có GuestVisit Active còn trong portal-window.</item>
    /// <item><c>session_expired</c> — có thiết bị nhưng không còn phiên hợp lệ cho phòng này (visit không Active,
    /// hoặc quá portal-window).</item>
    /// <item><c>guest_context_missing</c> — cookie rỗng/không phân giải được thiết bị.</item>
    /// <item><c>configuration_unavailable</c> — cấu hình resort nền thiếu (fail-closed).</item>
    /// </list>
    /// </summary>
    Task<Result<CurrentGuestContext>> ResolveAsync(string? sessionKey, Guid roomId, CancellationToken ct = default);

    /// <summary>
    /// Trượt cửa sổ của visit SAU khi nghiệp vụ thành công: đặt <c>LastSeenAt=now</c> và đẩy <c>ExpiresAt</c> giữ
    /// nguyên độ dài idle đã cấu hình (bảo toàn delta — không phụ thuộc query cấu hình ở đường touch). No-op nếu
    /// visit không còn Active (không hồi sinh phiên đã đóng).
    /// </summary>
    Task TouchAsync(Guid guestVisitId, CancellationToken ct = default);
}
