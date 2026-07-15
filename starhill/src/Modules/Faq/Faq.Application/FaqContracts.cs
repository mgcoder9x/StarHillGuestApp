namespace Faq.Application;

// ---- Category CRUD (E-Faq.2) ----
// Key BẤT BIẾN sau tạo (khóa ổn định — mirror RuleSection.Key) → Update KHÔNG đổi Key. ResortId do caller (Api)
// phân giải server-side (single-resort — mirror Rules/Rooms).

/// <summary>Tạo danh mục FAQ cho <paramref name="ResortId"/>. Unique <c>(ResortId, Key)</c> (race → faq_conflict).</summary>
public sealed record CreateFaqCategoryInput(Guid ResortId, string Key, int SortOrder, bool IsActive);

public sealed record CreateFaqCategoryResult(Guid CategoryId);

/// <summary>Sửa danh mục (SortOrder/IsActive; KHÔNG đổi Key/ResortId). Concurrency xmin (CP15).</summary>
public sealed record UpdateFaqCategoryInput(Guid CategoryId, int SortOrder, bool IsActive);

/// <summary>Xóa danh mục (chặn nếu còn item — faq_category_not_empty). Record riêng (không Guid trần) để service type duy nhất.</summary>
public sealed record DeleteFaqCategoryInput(Guid CategoryId);

// ---- Category translation (sanitize-on-save — CP12) ----

/// <summary>Tạo/cập nhật tên danh mục theo ngôn ngữ. <paramref name="Name"/> sanitize trước lưu. Upsert unique (category, lang).</summary>
public sealed record UpsertFaqCategoryTranslationInput(Guid CategoryId, string LanguageCode, string? Name);

public sealed record UpsertFaqCategoryTranslationResult(Guid TranslationId);

// ---- Item CRUD (E-Faq.2) ----
// ResortId của item DERIVE từ category (nguồn sự thật — tránh mismatch) → input KHÔNG mang ResortId. ParentId self
// (cùng category, không self, không cycle — bất biến cây, validate ở use case).

/// <summary>Tạo mục FAQ dưới <paramref name="CategoryId"/> (ResortId derive từ category). <paramref name="ParentId"/> tùy chọn (flow cha-con).</summary>
public sealed record CreateFaqItemInput(Guid CategoryId, Guid? ParentId, int SortOrder, bool IsActive);

public sealed record CreateFaqItemResult(Guid ItemId);

/// <summary>Sửa mục FAQ (ParentId/SortOrder/IsActive; KHÔNG đổi CategoryId). Re-validate cây. Concurrency xmin (CP15).</summary>
public sealed record UpdateFaqItemInput(Guid ItemId, Guid? ParentId, int SortOrder, bool IsActive);

/// <summary>Xóa mục FAQ (chặn nếu còn con — faq_item_has_children). Record riêng để service type duy nhất.</summary>
public sealed record DeleteFaqItemInput(Guid ItemId);

// ---- Item translation (sanitize-on-save — CP12) ----

/// <summary>
/// Tạo/cập nhật bản dịch mục FAQ. <paramref name="Question"/>/<paramref name="AnswerHtml"/> THÔ → sanitize qua
/// <c>IHtmlSanitizer</c> TRƯỚC lưu (cột <c>AnswerHtmlSanitized</c>; Question cũng sanitize — chống XSS mọi bề mặt).
/// Upsert unique (item, lang).
/// </summary>
public sealed record UpsertFaqItemTranslationInput(Guid ItemId, string LanguageCode, string? Question, string? AnswerHtml);

public sealed record UpsertFaqItemTranslationResult(Guid TranslationId);

// ---- Reorder (E-Faq.3, Req 4.5 drag-drop) ----
// Nhận TOÀN BỘ thứ tự mới (cặp Id→SortOrder) rồi cập nhật hàng loạt trong MỘT transaction (nguyên tử — không swap
// từng cặp, không trạng thái trung gian). Chỉ cập nhật các Id được gửi (partial cho phép); mỗi Id phải thuộc scope
// (category thuộc ResortId; item thuộc CategoryId) — sai scope → not_found. Void command → ICommandUseCase.

/// <summary>Một mục thứ tự mới: <paramref name="Id"/> (category hoặc item) → <paramref name="SortOrder"/>.</summary>
public sealed record FaqReorderEntry(Guid Id, int SortOrder);

/// <summary>Sắp lại thứ tự các danh mục FAQ của <paramref name="ResortId"/> (drag-drop cấp danh mục).</summary>
public sealed record ReorderFaqCategoriesInput(Guid ResortId, IReadOnlyList<FaqReorderEntry> Entries);

/// <summary>Sắp lại thứ tự các mục FAQ trong <paramref name="CategoryId"/> (drag-drop cấp item, cùng danh mục).</summary>
public sealed record ReorderFaqItemsInput(Guid CategoryId, IReadOnlyList<FaqReorderEntry> Entries);

// ---- Guest read cây FAQ (E-Faq.4, Req 4.2/4.3/4.4 + CP3 rule-gate + CP5 i18n) ----

/// <summary>
/// Khách đọc cây FAQ active theo ngôn ngữ. <paramref name="GuestVisitId"/> do server phân giải (từ
/// <c>ICurrentGuestContextResolver</c>) — dùng cho rule-gate (CP3). KHÔNG tin client.
/// </summary>
public sealed record GetGuestFaqTreeInput(Guid ResortId, Guid GuestVisitId, string? RequestedLanguage);

/// <summary>Cây FAQ đã render một ngôn ngữ — category (theo SortOrder) mỗi cái chứa item gốc + con đệ quy.</summary>
public sealed record GetGuestFaqTreeResult(string Language, IReadOnlyList<RenderedFaqCategory> Categories);

/// <summary>Category đã render: tên + trạng thái fallback/thiếu bản dịch (CP5) + item gốc.</summary>
public sealed record RenderedFaqCategory(
    Guid Id,
    string Key,
    int SortOrder,
    string? Name,
    string ResolvedLanguage,
    bool IsFallback,
    bool IsMissing,
    IReadOnlyList<RenderedFaqItem> Items);

/// <summary>Item đã render: câu hỏi/trả lời + fallback/thiếu + các mục con (flow cha-con, đệ quy).</summary>
public sealed record RenderedFaqItem(
    Guid Id,
    int SortOrder,
    string? Question,
    string? AnswerHtmlSanitized,
    string ResolvedLanguage,
    bool IsFallback,
    bool IsMissing,
    IReadOnlyList<RenderedFaqItem> Children);
