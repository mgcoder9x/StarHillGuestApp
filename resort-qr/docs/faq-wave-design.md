# Wave FAQ — thiết kế (CMS đơn giản: category → item, đa ngôn ngữ) — design-first

> Bám master `design.md` (§data model FAQ), `04` §2/§5/§7, `18` (resolve fallback). Khác Rules: **KHÔNG draft/publish/version** — nội dung sửa trực tiếp, hiển thị bản `IsActive` (CMS đơn giản).

## 0. Mô hình
- `FaqCategory` → `FaqItem` (cascade). Bản dịch: `FaqCategoryTranslation` (Name), `FaqItemTranslation` (Question + AnswerHtmlSanitized). Tất cả `AuditableEntity` (audit + xmin).
- Resort suy qua category (FaqItem KHÔNG mang ResortId — tránh drift denormalize).
- Ẩn/hiện bằng `IsActive` (category + item). Không snapshot.

## 1. Index / ràng buộc (`04` §5/§7)
- `ux_tr_faq_cat` (faq_category_id, language_code) unique; `ux_tr_faq` (faq_item_id, language_code) unique.
- `ix_faq_category_resort`, `ix_faq_item_category` (tra cứu).
- Cascade: category → item → translation (+ category → category_translation). FK category → resort **Restrict**.

## 2. Lộ trình sub-slice
- **A (✅ DONE):** domain (4 entity) + EF config + **migration `Faq_Init`** (per-wave — trước bị THIẾU gây model drift, đã fix DEC-067) + DbSet + test ràng buộc SQLite (ux_tr_faq_cat/ux_tr_faq/cascade/FK). Nền schema.
- **C — guest read (✅ DONE — DEC-068):** `GET /api/guest/faq?visitId=&lang=` — đọc cây category/item `IsActive` resolve theo ngôn ngữ (fallback `18` §3). Bảo vệ: `IPortalWindowGuard` (DEV-026) + `IRuleGate(Faq)` (DEC-064). Tắt tính năng (`FaqEnabled=false`) → trả rỗng. Refresh sliding-window sau khi đọc thành công (B8). 6 test SQLite.
- **B — admin CRUD (✅ DONE — DEC-069):** `GET /api/admin/faq` (cây + MissingLanguages) + CRUD category/item + **MERGE bản dịch** theo language_code (upsert — tránh delete-insert cùng key vs unique, DEV-025) + sanitize answer HTML. `RequireStaff`. Xóa category → cascade. 9 test SQLite. **→ WAVE FAQ HOÀN TẤT A+B+C.**

## 3. Sub-slice C — chi tiết (DEC-068)
- Contracts (Application/Faq): `GetGuestFaqInput(VisitId, RawSessionKey?, Lang?)`; `GuestFaqResponse(ResolvedLanguage, Categories[])`; `GuestFaqCategoryDto(Id, Name, SortOrder, Items[])`; `GuestFaqItemDto(Id, Question, AnswerHtmlSanitized, SortOrder, IsFallback, LanguageCode)`.
- `GetGuestFaqUseCase : IUseCase<...>, IScopedService` (mirror `GetGuestRulesUseCase`): guard portal-window → nếu `!FaqEnabled` trả rỗng → `IRuleGate.CheckAsync(Faq)` (fail → rule_ack_required) → nạp category/item `IsActive` (OrderBy SortOrder) + translations → resolve từng cái (`ITranslationResolver.Resolve` fallback) → refresh window.
- Validator: VisitId NotEmpty, Lang ≤16.
- Endpoint: `GET /api/guest/faq` (AllowAnonymous), đọc cookie→rawSessionKey, visitId qua query.
- Test SQLite: đọc cây theo lang; fallback khi thiếu bản dịch; session sai → session_expired; FaqEnabled=false → rỗng; RequireRuleAckForFaq + chưa ack → rule_ack_required; refresh sliding-window.

## 4. Truy vết
- master design §FAQ, `04` §2/§5/§7, `18`; DEC-046 (resolver), DEC-063 (guest rules pattern), DEC-064 (rule-gate), DEV-026 (portal-window guard), DEC-067 (fix drift + migration).
