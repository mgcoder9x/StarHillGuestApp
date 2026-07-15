namespace Rules.Application;

// ---- Draft section CRUD (D-Rules.2b) ----
// RuleSet là container Draft/resort (một bản nháp/resort — ux_rule_set_resort, QR-AD-035). CreateRuleSection
// tự ensure RuleSet cho ResortId (lazy find-or-create) rồi thêm section. Key BẤT BIẾN sau tạo (khóa ổn định giữ
// tiến độ đọc khi đổi ngôn ngữ — design §3) → Update KHÔNG đổi Key.

/// <summary>Tạo section nội quy Draft dưới RuleSet của <paramref name="ResortId"/> (ensure RuleSet nếu chưa có).</summary>
public sealed record CreateRuleSectionInput(
    Guid ResortId,
    string Key,
    int SortOrder,
    bool IsRequired,
    bool RequireScrollEnd,
    int MinReadSeconds);

public sealed record CreateRuleSectionResult(Guid SectionId);

/// <summary>Sửa cấu hình đọc của section (KHÔNG đổi Key/ResortId). Concurrency xmin (CP15).</summary>
public sealed record UpdateRuleSectionInput(
    Guid SectionId,
    int SortOrder,
    bool IsRequired,
    bool RequireScrollEnd,
    int MinReadSeconds);

// ---- Draft translation upsert (sanitize-on-save — CP12) ----

/// <summary>
/// Tạo/cập nhật bản dịch của một section. <paramref name="Title"/>/<paramref name="BodyHtml"/> là nội dung THÔ do
/// admin nhập; use case sanitize qua <c>IHtmlSanitizer</c> TRƯỚC khi lưu vào cột <c>BodyHtmlSanitized</c> (Title
/// cũng sanitize — design §7, chống XSS mọi bề mặt). Upsert theo unique (section, lang).
/// </summary>
public sealed record UpsertRuleSectionTranslationInput(
    Guid SectionId,
    string LanguageCode,
    string? Title,
    string? BodyHtml);

public sealed record UpsertRuleSectionTranslationResult(Guid TranslationId);

// ---- Draft section delete ----
// Dùng record RIÊNG (không Guid trần) để service type ICommandUseCase<DeleteRuleSectionInput> DUY NHẤT toàn Host —
// tránh đụng ICommandUseCase<Guid> của module khác (vd DeleteRoom) khi cùng wire (last-registration-wins).
public sealed record DeleteRuleSectionInput(Guid SectionId);

// ---- Publish snapshot (D-Rules.3, CP4) ----

/// <summary>
/// Phát hành bản Draft hiện tại của <paramref name="ResortId"/> thành một <c>RulePublication</c> snapshot bất biến
/// mới (Version++). <paramref name="PublishedByUserId"/> là actor (Guid trần, có thể null nếu hệ thống). ChangeNote
/// tùy chọn (ghi chú thay đổi — Req 8).
/// </summary>
public sealed record PublishRulesInput(Guid ResortId, Guid? PublishedByUserId, string? ChangeNote);

public sealed record PublishRulesResult(Guid PublicationId, int Version);

// ---- Guest read (D-Rules.4a, CP5) ----

/// <summary>Khách đọc bản nội quy hiện hành của <paramref name="ResortId"/> theo ngôn ngữ mong muốn (fallback default).</summary>
public sealed record GetCurrentRulesInput(Guid ResortId, string? RequestedLanguage);

/// <summary>Bản nội quy đã render theo ngôn ngữ đã chọn — khách hiển thị + biết version để ack.</summary>
public sealed record GetCurrentRulesResult(
    Guid PublicationId,
    int Version,
    string Language,
    IReadOnlyList<RenderedRuleSection> Sections);

/// <summary>Section đã render một ngôn ngữ: nội dung + cờ đọc + trạng thái fallback/thiếu bản dịch (CP5).</summary>
public sealed record RenderedRuleSection(
    string Key,
    int SortOrder,
    bool IsRequired,
    bool RequireScrollEnd,
    int MinReadSeconds,
    string? Title,
    string? BodyHtmlSanitized,
    string ResolvedLanguage,
    bool IsFallback,
    bool IsMissing);

// ---- Guest acknowledge (D-Rules.4b, CP13 — server-authoritative) ----

/// <summary>
/// Khách xác nhận đã đọc bản nội quy HIỆN HÀNH. Input mang ngữ cảnh khách ĐÃ được server phân giải (từ
/// <c>GuestAccess.Contracts.ICurrentGuestContextResolver</c>), KHÔNG mang version/publicationId do client gửi — server
/// TỰ đọc <c>RulePublication IsCurrent</c> để quyết định ack cho bản nào (CP13). <paramref name="RequestedLanguage"/>
/// chỉ để GHI NHẬN ngôn ngữ khách đọc (chuẩn hóa về supported), không phải nguồn quyết định.
/// </summary>
public sealed record AcknowledgeRulesInput(
    Guid ResortId,
    Guid RoomId,
    Guid GuestSessionId,
    Guid GuestVisitId,
    string? RequestedLanguage);

/// <summary>
/// Kết quả ack: bản publication (server-chọn) + version + <see cref="AlreadyAcknowledged"/> (true nếu visit đã ack
/// đúng bản IsCurrent này từ trước — idempotent, không tạo trùng).
/// </summary>
public sealed record AcknowledgeRulesResult(Guid RulePublicationId, int Version, bool AlreadyAcknowledged);

// ---- Admin preview + publication history (D-Rules.3b, Req 8.4) ----

/// <summary>Xem trước bản Draft "NHƯ KHÁCH" (render theo ngôn ngữ + fallback, KHÔNG publish) — chỉ Staff/Admin.</summary>
public sealed record GetDraftPreviewInput(Guid ResortId, string? RequestedLanguage);

/// <summary>Draft đã render một ngôn ngữ (tái dùng <see cref="RenderedRuleSection"/>) — không có PublicationId (chưa publish).</summary>
public sealed record GetDraftPreviewResult(string Language, IReadOnlyList<RenderedRuleSection> Sections);

/// <summary>Lịch sử publication của resort (metadata) — chỉ Staff/Admin đối soát.</summary>
public sealed record GetPublicationHistoryInput(Guid ResortId);

public sealed record GetPublicationHistoryResult(IReadOnlyList<RulePublicationHistoryItem> Publications);
