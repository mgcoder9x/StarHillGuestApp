namespace Rules.Application;

/// <summary>
/// Read-model NỘI-MODULE (CQRS-lite) để Publish đọc trọn bản Draft của một resort: <see cref="RuleSet"/> + toàn bộ
/// <c>RuleSection</c> (theo <c>SortOrder</c>) + bản dịch mỗi section. Tách khỏi <c>IRepository&lt;T&gt;</c> vì port
/// ghi generic CỐ Ý không phơi <c>IQueryable</c> (F9 — không rò provider) nên không list/sort được; đọc-nhiều dùng
/// read-model riêng trả DTO (đúng hướng dẫn <c>IRepository</c>). Trả snapshot BẤT BIẾN (no-tracking) — Publish chỉ
/// COPY nội dung Draft (đã sanitize khi lưu — CP12) sang publication, KHÔNG sửa Draft.
/// </summary>
public interface IRuleDraftReader
{
    /// <summary>Trả snapshot Draft của resort; <c>null</c> nếu resort chưa có bản Draft (chưa tạo section nào).</summary>
    Task<RuleDraftSnapshot?> LoadDraftAsync(Guid resortId, CancellationToken ct = default);
}

/// <summary>Snapshot Draft (chỉ đọc) — RuleSet + các section đã sắp theo SortOrder.</summary>
public sealed record RuleDraftSnapshot(Guid RuleSetId, IReadOnlyList<RuleDraftSectionSnapshot> Sections);

/// <summary>Section Draft + bản dịch của nó (thứ tự SortOrder giữ ở danh sách cha).</summary>
public sealed record RuleDraftSectionSnapshot(
    string Key,
    int SortOrder,
    bool IsRequired,
    bool RequireScrollEnd,
    int MinReadSeconds,
    IReadOnlyList<RuleDraftTranslationSnapshot> Translations);

/// <summary>Bản dịch Draft (Title/Body ĐÃ sanitize khi lưu — sao chép nguyên trạng sang publication).</summary>
public sealed record RuleDraftTranslationSnapshot(string LanguageCode, string? Title, string? BodyHtmlSanitized);
