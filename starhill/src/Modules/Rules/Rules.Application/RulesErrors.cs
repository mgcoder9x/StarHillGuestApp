using Bedrock.Domain.Results;

namespace Rules.Application;

/// <summary>
/// Mã lỗi module Rules (∈ catalog lỗi §14 sản phẩm). Nghiệp vụ riêng của app → khai ở tầng Application (KHÔNG ở
/// base Bedrock.Domain — base chỉ giữ lỗi hạ tầng chung). Concurrency (xmin) KHÔNG khai ở đây: base
/// <c>ConcurrencyConflictException</c> → middleware map 409 (CP15). Trùng số phòng/unique khác dịch từ
/// <c>UniqueConstraintViolationException</c> (QR-AD-010).
/// </summary>
public static class RulesErrors
{
    /// <summary>Section nội quy không tồn tại (Draft).</summary>
    public static Error RuleSectionNotFound =>
        Error.NotFound("not_found", "Không tìm thấy mục nội quy.");

    /// <summary>Bản dịch cho (section, ngôn ngữ) đã tồn tại — vi phạm <c>ux_rule_section_translation_lang</c>
    /// (chỉ khi hai request tạo cùng ngôn ngữ đua nhau; luồng thường là upsert nên hiếm).</summary>
    public static Error TranslationLanguageTaken =>
        Error.Validation("validation_error", "Bản dịch cho ngôn ngữ này đã tồn tại.");

    /// <summary>Draft đang bị thay đổi đồng thời (đua tạo <c>RuleSet</c> đầu tiên/resort — <c>ux_rule_set_resort</c>,
    /// QR-AD-035). Không phá dữ liệu (ràng buộc DB giữ đúng một RuleSet); client thử lại sẽ thành công.</summary>
    public static Error DraftConflict =>
        Error.Conflict("rules_conflict", "Nội quy đang được thay đổi đồng thời, vui lòng thử lại.");

    /// <summary>Không có nội dung Draft để phát hành (chưa tạo section nào cho resort) — Publish bị từ chối
    /// (chống tạo publication rỗng mà khách phải ack vô nghĩa — CP4).</summary>
    public static Error NoPublishableContent =>
        Error.Validation("validation_error", "Chưa có nội dung nội quy để phát hành.");

    /// <summary>Resort chưa publish bản nội quy nào (không có <c>RulePublication</c> IsCurrent) — guest read/ack
    /// không có gì để trả. 404 (NotFound) — khách chưa cần đọc/ack (D-Rules.4).</summary>
    public static Error RulesUnavailable =>
        Error.NotFound("rules_unavailable", "Nội quy chưa sẵn sàng.");

    /// <summary>Cấu hình resort nền thiếu (chưa seed settings/ngôn ngữ mặc định) — fail-closed khi guest đọc nội quy.
    /// Dùng CHUNG mã <c>configuration_unavailable</c> với GuestAccess (ngữ nghĩa nền thiếu, hợp đồng client thống nhất).</summary>
    public static Error ConfigurationUnavailable =>
        Error.Unexpected("configuration_unavailable", "Cấu hình resort chưa sẵn sàng.");
}
