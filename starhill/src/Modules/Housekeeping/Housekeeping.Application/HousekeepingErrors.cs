using Bedrock.Domain.Results;

namespace Housekeeping.Application;

/// <summary>
/// Mã lỗi module Housekeeping (∈ catalog §14 — cập nhật <c>ErrorCodeSnapshotTests</c> khi thêm). Nghiệp vụ riêng →
/// khai ở Application. Concurrency (xmin) → base <c>ConcurrencyConflictException</c> map 409 (CP15). <see cref="QrInvalid"/>
/// và <see cref="ConfigurationUnavailable"/> TÁI DÙNG mã ổn định đã có (GuestAccess) — cùng string nên snapshot dedup, KHÔNG mã mới.
/// </summary>
public static class HousekeepingErrors
{
    /// <summary>Tính năng dọn phòng bị TẮT (<c>HousekeepingEnabled=false</c> — Req 14) — backend enforce. 403.</summary>
    public static Error Disabled =>
        Error.Forbidden("housekeeping_disabled", "Tính năng yêu cầu dọn phòng hiện đang tắt.");

    /// <summary>Cấu hình resort nền thiếu — fail-closed khi guest tạo/xem ticket (dùng chung mã với GuestAccess/Rules/Faq).</summary>
    public static Error ConfigurationUnavailable =>
        Error.Unexpected("configuration_unavailable", "Cấu hình resort chưa sẵn sàng.");

    /// <summary>Phòng KHÔNG có ticket dọn phòng đang mở để hoàn tất (complete-by-room/token). 404.</summary>
    public static Error NoOpenTicket =>
        Error.NotFound("housekeeping_no_open_ticket", "Phòng không có yêu cầu dọn phòng đang mở.");

    /// <summary>Ticket không tồn tại (set-status theo id). 404.</summary>
    public static Error TicketNotFound =>
        Error.NotFound("housekeeping_ticket_not_found", "Không tìm thấy ticket dọn phòng.");

    /// <summary>Chuyển trạng thái không hợp lệ (vd Done→InProgress; terminal Done/Cancelled không đổi tiếp). Validation.</summary>
    public static Error InvalidTransition =>
        Error.Validation("housekeeping_invalid_transition", "Chuyển trạng thái ticket không hợp lệ.");

    /// <summary>Chi tiết yêu cầu dọn phòng không hợp lệ (thiếu/không hợp lệ loại dịch vụ, thời gian, giờ cụ thể sai định dạng,
    /// số vật dụng ngoài [0,5], hoặc ghi chú quá dài — Req 6.1, INV-HK1/HK2). 400.</summary>
    public static Error InvalidRequest =>
        Error.Validation("housekeeping_invalid_request", "Chi tiết yêu cầu dọn phòng không hợp lệ.");

    /// <summary>QR quét (complete-by-token) không phân giải được phòng (token lạ/đã thu hồi). TÁI DÙNG mã ổn định
    /// <c>qr_invalid</c> (GuestAccess) — cùng hợp đồng client, snapshot dedup.</summary>
    public static Error QrInvalid =>
        Error.Validation("qr_invalid", "Mã QR không hợp lệ hoặc đã bị thu hồi.");
}
