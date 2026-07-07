# Wave Rules — thiết kế (Draft → Publish snapshot → Guest read + Ack) — design-first

> Bám master `docs/.../design.md` (model + API rules), `04` §2/§4/§5/§8, `05` §2 (publish nguyên tử), `18` (translation resolve + fallback), `14`. Test SQLite Docker-free.

## 0. Mô hình (bản chất — draft mutable + snapshot bất biến)
- **Draft** (admin sửa, khách KHÔNG thấy): `RuleSet` → `RuleSection` → `RuleSectionTranslation`.
- **Publish** = đóng băng toàn bộ draft vào **snapshot bất biến** `RulePublication`(Version++) → `RulePublicationSection` → `RulePublicationSectionTranslation`; đặt `IsCurrent=true` bản mới, `false` bản cũ (đúng 1 current/resort).
- **Khách đọc TỪ publication `IsCurrent`** (không bao giờ thấy draft) → sửa draft không lọt ra khách.
- **Ack** = `RuleAcknowledgement` gắn `GuestVisit` + `RulePublication` (server tự xác định current, KHÔNG tin version client).

## 1. Entity + base-class + concurrency (`04` §8, master design §data model)
| Entity | Base | Interface | Concurrency | Field chính |
|---|---|---|---|---|
| `RuleSet` | AuditableEntity | — | RowVersion | ResortId (1 draft/resort → unique ResortId) |
| `RuleSection` | AuditableEntity | — | RowVersion | RuleSetId, Key, SortOrder, IsRequired, RequireScrollEnd, MinReadSeconds |
| `RuleSectionTranslation` | AuditableEntity | ITranslation | RowVersion | RuleSectionId, LanguageCode, Title, BodyHtmlSanitized; **ux_tr_rule** (RuleSectionId,LanguageCode) |
| `RulePublication` | Entity | — | Không (bất biến) | ResortId, Version, PublishedAt, PublishedByUserId?, ChangeNote?, IsCurrent; **ux_pub_current** (ResortId) WHERE IsCurrent |
| `RulePublicationSection` | Entity | — | Không | RulePublicationId, Key, SortOrder, IsRequired, RequireScrollEnd, MinReadSeconds |
| `RulePublicationSectionTranslation` | Entity | ITranslation | Không | RulePublicationSectionId, LanguageCode, Title, BodyHtmlSanitized; **unique** (…,LanguageCode) |
| `RuleAcknowledgement` | Entity | — | Không (append-only) | ResortId, RoomId, GuestSessionId, GuestVisitId, RulePublicationId, Version, LanguageCode, AcceptedAt; **ux_ack** (GuestVisitId, RulePublicationId) |

- `ITranslation.HasContent` = Title (trim) khác rỗng (nội dung chính hiển thị).
- Body rich-text (`BodyHtmlSanitized`) — đã sanitize qua `IHtmlSanitizer` ở tầng ghi (draft edit slice); cột text (không MaxLength). Title ≤200, Key ≤50, LanguageCode ≤16, ChangeNote ≤500.

## 2. Delete behavior (`04` §7.3)
- **Cascade** cho quan hệ SỞ HỮU (aggregate): RuleSet→RuleSection→RuleSectionTranslation (draft); RulePublication→RulePublicationSection→…Translation (snapshot).
- **Restrict** cho tham chiếu chéo lịch sử: mọi FK tới Resort/Room/GuestSession/GuestVisit/RulePublication (từ Acknowledgement). Publication KHÔNG bao giờ xóa (bất biến).

## 3. Index (`04` §5) — provider-aware cho BOOL
- **ux_pub_current** `rule_publication(resort_id) WHERE is_current` → BOOL filter → thêm ở `AppDbContext.OnModelCreating` (ProviderPartialIndex.BoolEquals — như ux_lang_default).
- **ux_ack** `rule_acknowledgement(guest_visit_id, rule_publication_id)` unique; **ux_tr_rule** `rule_section_translation(rule_section_id, language_code)` unique; **unique** `rule_publication_section_translation(rule_publication_section_id, language_code)` — đặt trong config (không filter).
- Unique `rule_set(resort_id)` (1 draft/resort).

## 4. Lộ trình sub-slice (mỗi cái 1 increment, test được)
- **A (✅ DONE):** domain 7 entity + EF config + **migration Rules_Init** (per-wave, Req 8.7) + test ràng buộc SQLite (ux_pub_current/ux_ack/ux_tr_rule/FK/cascade). Nền schema.
- **B (✅ DONE — DEC-061):** Authoring draft — get/**FULL-REPLACE** `RuleSet` draft (xóa cascade + chèn lại sections + translations trong 1 SaveChanges nguyên tử, sanitize HTML tầng ghi), `MissingLanguages` cho editor (`18` §5). Endpoint `GET/PUT /api/admin/rules/draft` (**Staff|Admin**). Concurrency (RowVersion) HOÃN — TRD-010. 6 test SQLite (create/full-replace/sanitize/missing-langs/null/validator).
- **C (✅ DONE — DEC-062):** `PublishRulesUseCase` (`02` §3) — snapshot draft→publication (Version++, IsCurrent, hạ cờ cũ) trong 1 transaction (2 SaveChanges: hạ-cờ-flush-trước rồi insert — DEV-025); race ux_pub_current/ux_pub_version → conflict. Endpoint `POST /api/admin/rules/publish` (**RequireAdmin**). 6 test SQLite.
- **D (✅ DONE — DEC-063):** Guest read + ack — `GET /api/guest/rules?visitId=&lang=` (đọc current publication, resolve translation fallback `18` §3), `POST /api/guest/rules/acknowledge` (ack current, ux_ack idempotent, server tự xác định version). Bảo vệ bằng `IPortalWindowGuard` (DEV-026) + sliding-window refresh sau khi thành công. ResolveResponse rules-state đã tích hợp qua port `IRuleAckStatusProvider`. 9 test SQLite.
- **E (✅ DONE — DEC-064):** Ack-gate — cổng tái dùng `IRuleGate.CheckAsync(resortId, visitId, GuestFeature)` trả `rule_ack_required` khi settings yêu cầu ack mà lượt chưa ack publication current. Fail-open khi chưa publish. Wave sau (FAQ/chat/housekeeping) gọi cổng SAU `IPortalWindowGuard`. 5 test SQLite. **→ WAVE RULES HOÀN TẤT (A→E).**

## 5. Sub-slice A — chi tiết triển khai
- Domain: `ResortQr.Domain/Rules/*` (7 entity). Translation entity implement `ITranslation`.
- EF config: `Configurations/RuleDraftConfigurations.cs` (RuleSet/Section/Translation) + `RulePublicationConfigurations.cs` (Publication/Section/Translation/Acknowledgement). Enum: không có (Rules không enum). ToTable snake_case, MaxLength, FK cascade/restrict, unique index.
- `AppDbContext`: thêm 7 DbSet + ux_pub_current (OnModelCreating provider-aware).
- Migration: `dotnet ef migrations add Rules_Init` (additive, per-wave).
- Test SQLite: ux_pub_current (2 current/resort → DbUpdateException); ux_ack (2 ack/visit+pub → chặn); ux_tr_rule (2 tr/section+lang → chặn); cascade (xóa RuleSection → translation biến mất); FK restrict (ack với visit không tồn tại → chặn).

## 5-B. Sub-slice B — chi tiết triển khai (DEC-061)
- **Contracts (Application/Rules):** `UpdateRuleDraftInput(Sections)` + `DraftSectionInput(Key, SortOrder, IsRequired, RequireScrollEnd, MinReadSeconds, Translations)` + `DraftTranslationInput(LanguageCode, Title, BodyHtml)`. Read: `RuleDraftDto(Sections)` + `RuleDraftSectionDto(Id, Key, …, Translations, MissingLanguages)` + `RuleDraftTranslationDto(LanguageCode, Title, BodyHtmlSanitized, HasContent)`.
- **`UpdateRuleDraftUseCase : ICommandUseCase<UpdateRuleDraftInput>`** (inject `IUnitOfWork`, `IHtmlSanitizer`): lấy resort single (`FirstOrDefault(_=>true)`); get-or-create `RuleSet`; `ListAsync` section cũ + `Remove` từng cái (cascade DB xóa translation); chèn section+translation mới (`Title` trim, `BodyHtmlSanitized = Sanitize(BodyHtml)`); MỘT `SaveChanges` nguyên tử. FULL-REPLACE (an toàn vì không unique trên Key). Concurrency HOÃN (TRD-010).
- **`UpdateRuleDraftValidator`:** Sections không null; mỗi Key required ≤50; MinReadSeconds ≥0; mỗi translation LanguageCode required ≤16, Title ≤200.
- **`IRuleDraftQueries`/`EfRuleDraftQueries`** (namespace `Infrastructure.Persistence`, DEC-049; inject `ResortQrDbContext`+`ITranslationResolver`): nạp RuleSet→sections(OrderBy SortOrder)→translations + enabled `ResortLanguage.Code`; per-section `MissingLanguages` qua resolver. Wire `TryAddScoped` trong `AddResortQrPersistence`.
- **Endpoint `AdminRuleEndpoints`:** `GET /api/admin/rules/draft` (RequireStaff → 200, rỗng nếu null); `PUT /api/admin/rules/draft` (RequireStaff → 204). Đăng ký `app.MapResortQrAdminRuleEndpoints()` trong Program.cs.
- **`IRepository.ListAsync(predicate)`** mở rộng contract base (DEV-024) — materialize tracked list, không leak IQueryable.
- **LanguageCode** trim-nguyên-văn, không lowercase (TK-041).
- Test SQLite (6): create draft; full-replace (section a/b→c, translation cũ cascade); sanitize (strip `<script>`); GetDraft sections+translations+MissingLanguages (en+vi có → thiếu ko); GetDraft null khi chưa có; validator reject Key rỗng.

## 5-C. Sub-slice C — Publish snapshot (DEC-062) — thiết kế
- **Mục tiêu:** `POST /api/admin/rules/publish` — đóng băng draft (`RuleSet`→sections→translations) thành SNAPSHOT bất biến `RulePublication`(Version++)→`RulePublicationSection`→`RulePublicationSectionTranslation`; đặt `IsCurrent=true` bản mới + hạ cờ bản cũ. Khách (slice D) chỉ đọc từ publication `IsCurrent`.
- **Phân quyền: `RequireAdmin`** (KHÁC draft = Staff|Admin). Lý do: publish là hành động "go-live" hệ trọng — đổi nội dung khách thấy TOÀN CỤC + là hành động nhạy cảm cần audit (`19`/`10`). Tách nhiệm vụ: Staff soạn draft, Admin duyệt & publish (separation-of-duties, giống Rooms mutation = Admin). Reversible nếu user muốn Staff publish.
- **Thứ tự ghi NGUYÊN TỬ (bản chất — tránh vi phạm `ux_pub_current` thoáng qua):** trong `ExecuteInTransactionAsync`:
  1. Nạp `current` (IsCurrent). Nếu có → `IsCurrent=false` + **SaveChanges #1** (FLUSH hạ cờ TRƯỚC). Version mới = `current.Version + 1`; không có current → Version=1.
  2. Tạo `RulePublication`(IsCurrent=true) + copy sections (OrderBy SortOrder) + copy translations (Title/BodyHtmlSanitized/LanguageCode nguyên văn — draft đã sanitize ở tầng ghi B, KHÔNG sanitize lại). Add. **SaveChanges #2**.
  - Vì sao 2 SaveChanges (không gộp 1): EF Core KHÔNG đảm bảo thứ tự UPDATE-trước-INSERT trên cùng bảng → gộp 1 lần có thể INSERT dòng is_current=true khi dòng cũ còn true → vi phạm `ux_pub_current`. Hạ-cờ-rồi-flush TRƯỚC loại bỏ trạng thái 2-current thoáng qua. Cả 2 save trong 1 transaction → vẫn all-or-nothing (đúng ý đồ DEV-023).
- **Race (2 admin publish đồng thời):** DB phân xử — `ux_pub_version`(resort,version) chặn 2 bản cùng version; `ux_pub_current`(resort) WHERE is_current chặn 2 current. Lần commit sau ném unique-violation → use case bắt `UniqueConstraintViolationException` → trả `conflict`. (Race thật cần đa-connection → Testcontainers TK-036; SQLite 1-connection chỉ kiểm được luồng tuần tự.)
- **Guard trước transaction:** không có `RuleSet` HOẶC 0 section → `validation_error` ("chưa có nội dung nội quy để publish"). Quyết định AI: yêu cầu ≥1 section (publish rỗng gần như luôn là nhầm; muốn tắt rule-gate thì đổi settings, không publish rỗng) — reversible.
- **Contracts:** `PublishRulesInput(string? ChangeNote)` (≤500), `PublishRulesResult(Guid PublicationId, int Version, int SectionCount)`. Error code tái dùng catalog (`validation_error`/`conflict`) — không thêm code mới (DEC-019).
- **Test SQLite:** publish lần đầu (v1 current, snapshot đủ section+translation); publish lần 2 (v1→non-current, v2 current, đúng 1 current — kiểm cả `ux_pub_current`); snapshot ĐỘC LẬP draft (sửa draft sau publish KHÔNG đổi snapshot cũ); publish khi chưa có draft/0 section → validation_error; ChangeNote lưu đúng; validator ChangeNote >500 reject.

## 5-D. Sub-slice D — Guest read + acknowledge (DEC-063) — thiết kế
- **Endpoint (guest-surface, AllowAnonymous, cookie-based — như resolve):**
  - `GET /api/guest/rules?visitId={guid}&lang={code}` → publication `IsCurrent` resolve theo lang (fallback `18` §3) + rules-state (currentVersion, acknowledged).
  - `POST /api/guest/rules/acknowledge` (body: visitId, lang) → ghi `RuleAcknowledgement` cho publication current, **idempotent** (ux_ack).
- **Xác định visit (bản chất — chống truy cập chéo session):** client gửi `visitId` (nhận từ resolve) + cookie `shq_guest` (raw). Server: hash cookie → `GuestSession`; load visit theo id; **verify `visit.GuestSessionId == session.Id`** (KHÔNG suy visit từ session vì 1 session có thể nhiều visit đa-phòng — `13` E11). Sai/không khớp/không active → `session_expired` (403, không lộ). Server tự xác định publication current + version — KHÔNG tin version client (chống ack version cũ).
- **`IPortalWindowGuard` (Application/GuestAccess — DỜI từ slice E, xem DEV-026):** guard chung cho MỌI guest interactive endpoint. `ValidateAsync(visitId, rawKey)` = hiện thực `13` §4 EnforcePortalWindow (check-before): session ownership + visit Active + **lazy idle-expiry** (now>ExpiresAt → set Expired+save → session_expired; KHÔNG cascade — nhất quán ResolveToken, hoãn tới có Messaging/Housekeeping) + **portal window** (now-LastSeenAt>PortalWindowMinutes → session_expired, KHÔNG cập nhật). Trả `PortalWindowState(Visit, Session, IdleHours)`. Use case gọi guard TRƯỚC, làm nghiệp vụ, rồi **refresh sliding window** (LastSeenAt/ExpiresAt=now+idle) SAU khi thành công (B8 — cập-nhật-sau).
- **Resolve translation:** `MatchSupported(lang, enabledCodes, defaultCode)` → mỗi section `Resolve(translations, requested, default)` → `Translated{Value, ResolvedLanguage, IsFallback, IsMissing}`. Section thiếu cả 2 → Title/Body rỗng (giữ cấu trúc section — admin đã publish có chủ đích). Trả `IsFallback` để guest-web hiện badge (`03` FE).
- **Idempotent ack:** check-then-act (`AnyAsync` ux_ack) + bắt `UniqueConstraintViolationException` (race 2 ack) → đều trả `Ok(AlreadyAcknowledged=true)`. Ack ghi `Version`+`LanguageCode` (mã đã match) tại thời điểm ack (snapshot vận hành).
- **ResolveResponse rules-state (DEV-027):** ResolveToken (GuestAccess) gọi **port** `IRuleAckStatusProvider` (Application/Rules — giữ ranh giới module: GuestAccess không đọc thẳng entity Rules) → `RuleAckStatus(CurrentVersion, Acknowledged)`; thêm `RulesVersion`+`RulesAcknowledged` vào ResolveResponse để guest-web biết có cần hiện rule-gate ngay sau resolve.
- **Error:** `session_expired` (guard), `validation_error` (ack khi chưa có publication — `RulesErrors.NoPublishedRules`). Tái dùng catalog (DEC-019).
- **Test SQLite:** GET trả sections+translation resolved (đúng lang); GET fallback khi thiếu bản dịch requested (IsFallback=true); GET khi chưa publish → CurrentVersion=null, Sections=[]; ack tạo bản ghi (Version/Lang đúng); ack idempotent (gọi 2 lần → 1 bản ghi, AlreadyAcknowledged); ack visit của session KHÁC → session_expired; ack khi chưa publish → validation_error; portal window quá hạn → session_expired + không refresh; visit idle quá hạn → Expired + session_expired; resolve trả rules-state (version + acknowledged sau khi ack).

## 5-D. Sub-slice D — Guest read + acknowledge (DEC-063) — đã hoàn thiện
- **`GetGuestRulesUseCase`** (`GET /api/guest/rules?visitId=&lang=`): guard portal window → match ngôn ngữ (`MatchSupported`) → publication IsCurrent → sections (OrderBy SortOrder) resolve từng cái (`Resolve` fallback default, `IsFallback`/`LanguageCode` per-section) → refresh sliding window. Chưa publish → `CurrentVersion=null, Sections=[]`.
- **`AcknowledgeRulesUseCase`** (`POST /api/guest/rules/acknowledge`): guard → publication IsCurrent (chưa có → `validation_error`) → check-then-act idempotent (đã ack → AlreadyAcknowledged=true) → ghi `RuleAcknowledgement` (server tự set publicationId+version) + refresh window CÙNG lần ghi; bắt `UniqueConstraintViolationException` (ux_ack) khi đua → AlreadyAcknowledged=true.
- **`IPortalWindowGuard`/`PortalWindowGuard`** (DEV-026): một nguồn luật cổng guest interactive (session thuộc cookie + visit Active còn hạn + lazy-expiry). Thất bại → `session_expired`.
- **ResolveResponse rules-state:** qua port `IRuleAckStatusProvider` (ranh giới module) → `RulesVersion`/`RulesAcknowledged`.
- **Trạng thái tìm thấy + fix (TK-042):** tầng Application do phiên trước làm dở (thiếu 2 `using`, endpoint chưa nối, test cũ chưa cập nhật ctor) → phiên này build-verify, fix gốc, hoàn thiện endpoint + 9 test.
- Test SQLite: read theo lang; read rỗng khi chưa publish; fallback default khi thiếu bản dịch; session sai → session_expired; read refresh sliding-window; ack tạo record rồi idempotent (1 row ux_ack); ack chưa publish → validation_error; ack session sai → session_expired; ack rồi read → Acknowledged=true.

## 5-E. Sub-slice E — Ack-gate tái dùng (DEC-064) — đóng wave
- **`IRuleGate`/`RuleGate`** (Application/Rules, phụ thuộc thuần IUnitOfWork → auto-scan): `CheckAsync(resortId, visitId, GuestFeature)` → `Result`. `GuestFeature` {Faq, Chat, Housekeeping} ↔ cờ `ResortSettings.RequireRuleAckFor*`.
- **Luật:** không yêu cầu ack → Ok; yêu cầu ack + chưa publish → **Ok (fail-open, DEC-064)**; yêu cầu ack + đã publish + lượt chưa ack → `rule_ack_required` (Forbidden 403); đã ack → Ok.
- **Tái dùng cho wave sau:** endpoint FAQ/chat/housekeeping gọi lần lượt `IPortalWindowGuard` (DEV-026) → `IRuleGate` → xử lý. Cổng CHỈ kiểm ack (không lặp portal-window — tách trách nhiệm).
- Test SQLite (5): feature không yêu cầu → Ok; yêu cầu nhưng chưa publish → Ok (fail-open); yêu cầu + publish + chưa ack → rule_ack_required; đã ack → Ok; per-feature độc lập (Faq chặn, Chat mở).

## 6. Truy vết
- master design (model rules, API §guest/§admin, quyết định Draft→Publish snapshot); `04` §2/§4/§5/§8, `05` §2, `18`, `14`; Req 8.6/8.7, 9. Base: DEC-053/054/057 (entity/mapping pattern), DEC-046 (translation resolver).
