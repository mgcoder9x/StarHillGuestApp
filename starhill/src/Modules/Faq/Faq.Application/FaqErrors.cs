using Bedrock.Domain.Results;

namespace Faq.Application;

/// <summary>
/// Mã lỗi module Faq (∈ catalog lỗi §14 sản phẩm — cập nhật <c>ErrorCodeSnapshotTests</c> khi thêm). Nghiệp vụ riêng
/// của app → khai ở tầng Application (KHÔNG ở base). Concurrency (xmin) KHÔNG khai ở đây: base
/// <c>ConcurrencyConflictException</c> → middleware map 409 (CP15). Unique-violation dịch từ
/// <c>UniqueConstraintViolationException</c> (QR-AD-010) → <see cref="FaqConflict"/>.
/// </summary>
public static class FaqErrors
{
    /// <summary>Danh mục FAQ không tồn tại.</summary>
    public static Error CategoryNotFound =>
        Error.NotFound("faq_category_not_found", "Không tìm thấy danh mục FAQ.");

    /// <summary>Mục FAQ không tồn tại.</summary>
    public static Error ItemNotFound =>
        Error.NotFound("faq_item_not_found", "Không tìm thấy mục FAQ.");

    /// <summary>Vi phạm unique (Key danh mục/bản dịch theo ngôn ngữ) khi hai request đua tạo — client thử lại.</summary>
    public static Error Conflict =>
        Error.Conflict("faq_conflict", "FAQ đang được thay đổi đồng thời hoặc đã tồn tại, vui lòng thử lại.");

    /// <summary>
    /// <c>ParentId</c> không hợp lệ: tự trỏ mình, khác danh mục, hoặc tạo CHU TRÌNH cha-con (bất biến cây — chống
    /// render loop guest). Là quyết định AI tự ra (spec chỉ nói "cha-con" — QR-AD Faq §11).
    /// </summary>
    public static Error InvalidParent =>
        Error.Validation("faq_invalid_parent", "Mục cha không hợp lệ (khác danh mục, tự trỏ, hoặc tạo vòng lặp).");

    /// <summary>Không thể xóa danh mục khi còn mục FAQ tham chiếu (giữ toàn vẹn cây — phải xóa/chuyển item trước).</summary>
    public static Error CategoryNotEmpty =>
        Error.Conflict("faq_category_not_empty", "Không thể xóa danh mục khi vẫn còn mục FAQ bên trong.");

    /// <summary>Không thể xóa mục FAQ khi còn mục con tham chiếu (giữ toàn vẹn cây — phải xóa/chuyển con trước).</summary>
    public static Error ItemHasChildren =>
        Error.Conflict("faq_item_has_children", "Không thể xóa mục FAQ khi vẫn còn mục con.");

    /// <summary>Tính năng FAQ bị TẮT cho resort (<c>FaqEnabled=false</c> — Req 14) — backend enforce, không dựa chỉ vào
    /// việc frontend ẩn nút. <c>Forbidden</c> → HTTP 403.</summary>
    public static Error Disabled =>
        Error.Forbidden("faq_disabled", "Tính năng FAQ hiện đang tắt.");

    /// <summary>Cấu hình resort nền thiếu (chưa seed settings/ngôn ngữ mặc định) — fail-closed khi guest đọc FAQ.
    /// Dùng CHUNG mã <c>configuration_unavailable</c> với GuestAccess/Rules (ngữ nghĩa nền thiếu, hợp đồng client thống nhất).</summary>
    public static Error ConfigurationUnavailable =>
        Error.Unexpected("configuration_unavailable", "Cấu hình resort chưa sẵn sàng.");
}
