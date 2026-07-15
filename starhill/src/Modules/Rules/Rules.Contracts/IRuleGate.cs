using Bedrock.Domain.Results;

namespace Rules.Contracts;

/// <summary>Tính năng guest chịu rule-gate (Req 14.1/14.2 — cờ <c>RequireRuleAckFor*</c> ở ResortSettings).</summary>
public enum GuestFeature
{
    Faq,
    Chat,
    Housekeeping,
}

/// <summary>
/// Rule-gate BACKEND (CP3/QR-AD-030 — Req 3.11): các module guest-facing (Faq/Concierge/Housekeeping) gọi TRƯỚC
/// nghiệp vụ để enforce "phải đọc + xác nhận nội quy" ở SERVER (không chỉ chặn frontend). Nhận Id TRẦN
/// (<paramref name="resortId"/>/<paramref name="guestVisitId"/>) — Rules.Contracts KHÔNG ref GuestAccess/ResortConfig
/// (tránh coupling Contracts↔Contracts — QR-TO-010); consumer tự phân giải GuestVisit (qua
/// <c>GuestAccess.Contracts.ICurrentGuestContextResolver</c>) rồi truyền vào.
/// </summary>
public interface IRuleGate
{
    /// <summary>
    /// <see cref="Result.Success()"/> nếu KHÔNG yêu cầu ack (cờ tính năng tắt) HOẶC visit đã ack bản nội quy
    /// <c>IsCurrent</c>; ngược lại <see cref="Result.Failure"/> <c>rule_ack_required</c> (map HTTP 403). Cấu hình nền
    /// thiếu → <c>configuration_unavailable</c> (fail-closed, nhất quán guest read/resolve).
    /// </summary>
    Task<Result> EnsureAcknowledgedAsync(
        Guid resortId, Guid guestVisitId, GuestFeature feature, CancellationToken ct = default);
}
