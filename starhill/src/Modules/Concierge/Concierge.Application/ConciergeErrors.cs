using Bedrock.Domain.Results;

namespace Concierge.Application;

/// <summary>
/// Mã lỗi module Concierge (∈ catalog §14 — cập nhật <c>ErrorCodeSnapshotTests</c> khi thêm). Nghiệp vụ riêng →
/// khai ở Application. Concurrency (xmin) → base <c>ConcurrencyConflictException</c> map 409 (CP15). <see cref="ConfigurationUnavailable"/>
/// TÁI DÙNG mã ổn định đã có (GuestAccess/Housekeeping/Rules/Faq) — cùng string nên snapshot dedup, KHÔNG mã mới.
/// </summary>
public static class ConciergeErrors
{
    /// <summary>Tính năng nhắn tin bị TẮT (<c>ChatEnabled=false</c> — Req 14) — backend enforce. 403.</summary>
    public static Error ChatDisabled =>
        Error.Forbidden("chat_disabled", "Tính năng nhắn tin hiện đang tắt.");

    /// <summary>Cấu hình resort nền thiếu — fail-closed khi guest gửi/đọc tin (dùng chung mã với GuestAccess/Rules/Faq/Housekeeping).</summary>
    public static Error ConfigurationUnavailable =>
        Error.Unexpected("configuration_unavailable", "Cấu hình resort chưa sẵn sàng.");

    /// <summary>Nội dung tin nhắn rỗng (sau khi trim). Validation.</summary>
    public static Error MessageEmpty =>
        Error.Validation("concierge_message_empty", "Nội dung tin nhắn không được để trống.");

    /// <summary>Nội dung tin nhắn vượt giới hạn <c>MaxMessageLength</c> (cấu hình per-resort). Validation.</summary>
    public static Error MessageTooLong =>
        Error.Validation("concierge_message_too_long", "Nội dung tin nhắn vượt quá độ dài cho phép.");

    /// <summary>Hội thoại không tồn tại (staff reply/read/close theo id). 404.</summary>
    public static Error ConversationNotFound =>
        Error.NotFound("concierge_conversation_not_found", "Không tìm thấy hội thoại.");

    /// <summary>Ghi chú nội bộ không tồn tại (staff update/delete theo id). 404.</summary>
    public static Error NoteNotFound =>
        Error.NotFound("concierge_note_not_found", "Không tìm thấy ghi chú.");
}
