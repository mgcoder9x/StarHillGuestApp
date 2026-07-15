# Module design — Faq (Wave E)

> **Design-first, CHƯA triển khai code.** Chi tiết hóa `../design.md` §2 (module #6 Faq). WHAT:
> `docs/resort-qr-portal/requirements.md` Req 4 (FAQ cha-con do lễ tân soạn), Req 8.5/8.6 (admin CRUD FAQ + sanitize),
> Req 3.11/2.11 (rule-gate backend cho `/faq`), Req 2 (i18n fallback). Data model nguồn:
> `docs/resort-qr-portal/design.md` §Data Models (Faq*) + §Constraints (`unique (FaqItemId,LanguageCode)`/`(FaqCategoryId,LanguageCode)`)
> + §Optimistic concurrency (`FaqItem`/`FaqItemTranslation` có concurrency token) + §API (guest `GET /faq?lang=`;
> admin `/faq/categories`,`/faq/items`,`/faq/reorder`). Mọi kết luận dựa trên file/code đã đọc (KHÔNG suy đoán).
> Faq là module **DỰNG MỚI** (legacy `resort-qr/` chưa có Faq) — thiết kế từ requirements + tái dùng Contracts đã có.

## 0. Đối soát nguồn (đã verify trên đĩa)

- **Legacy KHÔNG có Faq**: `resort-qr/` mới tới Identity/Rooms/GuestAccess (design.md §0). ⇒ thiết kế từ requirements, không port code.
- **Contracts đã có + đã đọc chữ ký thật (không suy đoán)** — Faq là consumer, KHÔNG tạo lại:
  1. `Rules.Contracts.IRuleGate.EnsureAcknowledgedAsync(Guid resortId, Guid guestVisitId, GuestFeature feature, ct)`
     + `enum GuestFeature { Faq, Chat, Housekeeping }` (đọc `Rules.Contracts/IRuleGate.cs`). **Faq là CONSUMER ĐẦU TIÊN**
     của rule-gate → dùng `GuestFeature.Faq`. Gate trả `Result.Success` khi cờ tắt / đã ack; `rule_ack_required` (403)
     khi bật+chưa ack/chưa publish; `configuration_unavailable` khi config nền thiếu (fail-closed).
  2. `GuestAccess.Contracts.ICurrentGuestContextResolver.ResolveAsync(string? sessionKey, Guid roomId, ct)` →
     `Result<CurrentGuestContext(GuestVisitId, GuestSessionId, RoomId, ResortId)>` + `TouchAsync(guestVisitId)`
     (đọc `ICurrentGuestContextResolver.cs`). Check-before-touch (QR-AD-032). Cookie canonical
     `GuestAccessModule.SessionCookieName`.
  3. `ResortConfig.Contracts.Queries.IResortGuestConfigQuery.GetAsync(Guid resortId)` → `ResortGuestConfig` (mang
     `FaqEnabled`, `EnabledLanguageCodes`, `DefaultLanguageCode`, `RequireRuleAckForFaq`; trả `null` fail-closed).
  4. `ResortConfig.Contracts.Localization.ITranslationResolver` (`MatchSupported` + `Resolve<T>` fallback + `IsFallback`)
     + `ITranslation` (`HasContent`). i18n render tree theo ngôn ngữ.
  5. `ResortConfig.Contracts.Queries.IResortSettingsQuery.GetAsync()` (single-resort, trả `ResortSettingsSnapshot.ResortId`)
     — admin endpoint phân giải `resortId` server-side (mirror `RulesAdminEndpointModule`).
  6. `IHtmlSanitizer` (base `Bedrock.Application.Ports.Html`) + adapter `StarHill.Html` (`AddStarHillHtml`, QR-AD-031,
     `RequirePort<IHtmlSanitizer>` đã đặt ở Host từ D-Rules.4c) — sanitize `AnswerHtmlSanitized` khi lưu (CP12).
- **Pattern guest/admin endpoint đã đọc thật**: `RulesGuestEndpointModule` (resolve context → nghiệp vụ → check-before-touch;
  `Cache-Control: no-store`; map `Result`→HTTP qua `ProblemDetailsBuilder`; KHÔNG log cookie) + `RulesAdminEndpointModule`
  (RequireStaff; resortId server-side; ProblemDetailsBuilder). Faq mirror y hệt.
- **xmin**: `PlatformDbContext.OnModelCreating` map `RowVersion`→`xmin` CHỈ khi Npgsql + entity implement
  `IHasConcurrencyToken`. ⇒ `FaqItem`/`FaqItemTranslation`/`FaqCategory`/`FaqCategoryTranslation` khai `IHasConcurrencyToken`
  để đạt CP15 (409) — nhất quán Rules Draft.

## 1. Mục tiêu và bất biến

1. **Cây FAQ 2 tầng cấu trúc + cha-con trong item (Req 4.1)**: `FaqCategory` → nhiều `FaqItem`; `FaqItem` có
   `ParentId?` (self-reference trong CÙNG category) để tạo flow câu hỏi con. Guest duyệt theo category hoặc theo flow.
2. **Chỉ hiển thị active cho khách (Req 4.4)**: guest read CHỈ trả `FaqCategory.IsActive` + `FaqItem.IsActive`;
   admin read trả tất cả (kèm cờ để lọc "show inactive").
3. **i18n fallback (CP5, Req 4.2)**: thiếu bản dịch ngôn ngữ đang chọn → trả bản mặc định + `IsFallback`, KHÔNG rỗng.
   Áp cho cả `FaqCategoryTranslation.Name` và `FaqItemTranslation.Question/AnswerHtmlSanitized`.
4. **Sanitize-on-save (CP12, Req 8.6)**: `AnswerHtmlSanitized` sanitize qua `IHtmlSanitizer` TRƯỚC khi lưu; guest read
   an toàn by-construction (không double-sanitize on-read — QR-TO-011). `Question` cũng sanitize (chống XSS mọi bề mặt).
5. **Rule-gate BACKEND cho guest FAQ (CP3, Req 3.11)**: `GET /faq` gọi `IRuleGate.EnsureAcknowledgedAsync(..., Faq)` →
   `403 rule_ack_required` khi cấu hình yêu cầu mà chưa ack. Không chỉ chặn frontend.
6. **Feature-flag `FaqEnabled` (Req 14)**: `FaqEnabled=false` → guest FAQ trả `faq_disabled` (backend enforce, không
   dựa chỉ vào việc frontend ẩn nút).
7. **Optimistic concurrency (CP15)**: hai lễ tân sửa cùng item/translation → người sau nhận 409, không ghi đè âm thầm.
8. **Bất biến cây (mới — spec ngầm định "cha-con" nhưng KHÔNG nói ràng buộc)**: `ParentId` phải (a) cùng `CategoryId`,
   (b) không tự trỏ mình, (c) không tạo CHU TRÌNH. Vi phạm → `faq_invalid_parent` (validation). Đây là quyết định
   AI tự ra để bảo toàn tính "cây" (§11 QR-AD-0xx) — nếu không chặn, cây FAQ có thể thành đồ thị vòng → guest render loop.
9. **Boundary (QR-AD-002)**: cross-module chỉ qua `<M>.Contracts` + Id trần (Guid); không FK chéo schema; keyed
   persistence `faq`. Faq KHÔNG map Outbox/Inbox (chưa phát event).

## 2. Cấu trúc module và dependency

```text
starhill/src/Modules/Faq/
  Faq.Domain         -> Bedrock.Domain
  Faq.Contracts      -> (chỉ FaqModule.PersistenceKey — Faq chưa lộ port cross-module nào; Dashboard đọc count sau)
  Faq.Application    -> Domain + Contracts + ResortConfig.Contracts + Rules.Contracts + Bedrock.Application (+FluentValidation)
  Faq.Infrastructure -> Application + Bedrock.Infrastructure + EF Core/Npgsql (+FluentValidation)
  Faq.Api            -> Application + ResortConfig.Contracts + GuestAccess.Contracts + StarHill.Authorization + Bedrock.Api
```

- `FaqModule.PersistenceKey = "faq"` (hằng ở `Faq.Contracts`, nguồn duy nhất — mirror mọi module).
- Mirror khuôn Rules (5 csproj đã đọc): Domain chỉ Bedrock.Domain; Api KHÔNG ref Infrastructure (I7); cross-module chỉ
  Contracts; dùng `$(PlatformSrc)`.
- **Vì sao `Faq.Application` ref `Rules.Contracts`** (quyết định — §11): rule-gate enforce đặt trong USE CASE guest-read
  (`GetGuestFaqTreeUseCase` gọi `IRuleGate` TRƯỚC khi build tree) thay vì chỉ ở endpoint. Lý do tận gốc: CP3 là bất biến
  backend "không truy cập feature khi chưa ack" — đặt trong use case thì MỌI caller (endpoint hiện tại + caller nội bộ
  tương lai) đều bị gate, không thể quên khi thêm endpoint mới. Endpoint-only enforcement dễ drift. Đánh đổi: coupling
  `Faq.Application`→`Rules.Contracts` (chấp nhận — đúng QR-AD-002 cross-module qua Contracts; `IRuleGate` nhận Id trần).
- **Vì sao `Faq.Api` ref `GuestAccess.Contracts`** (không phải Application): resolve `CurrentGuestContext` từ cookie là
  concern tầng HTTP (đọc cookie, roomId từ query) — mirror `RulesGuestEndpointModule`. Application nhận `resortId`+
  `guestVisitId` Guid trần.
- `Faq.Api` KHÔNG cần ref `StarHill.Html`/`IHtmlSanitizer` — sanitize nằm trong use case (Application), adapter do Host cấp.

## 3. Domain model

> Field/quan hệ là **nguồn product** `docs/resort-qr-portal/design.md` §Data Models (Faq*) — TÁI DÙNG nguyên. Phần dưới
> chốt **nơi đặt** (schema `faq`) + **ràng buộc DB** + marker concurrency.

- **`FaqCategory(Id, ResortId, Key, SortOrder, IsActive, RowVersion)`** — `Key` bất biến sau tạo (khóa ổn định, mirror
  RuleSection.Key). `ResortId` Guid trần (không FK chéo schema resort_config).
- **`FaqCategoryTranslation(Id, FaqCategoryId, LanguageCode, Name, RowVersion)`** — unique `(FaqCategoryId, LanguageCode)`;
  implement `ITranslation` (`HasContent` = `Name` sau trim khác rỗng; Name-only nên Body coi như rỗng).
- **`FaqItem(Id, ResortId, CategoryId, ParentId?, SortOrder, IsActive, RowVersion)`** — `ParentId?` self-reference
  (FK nội-module `FaqItem`→`FaqItem`, `OnDelete Restrict` để không xóa cha khi còn con); `CategoryId` FK→`FaqCategory`
  (Restrict). `ResortId` Guid trần (denormalize để guard "cùng resort" + query theo resort không join category).
- **`FaqItemTranslation(Id, FaqItemId, LanguageCode, Question, AnswerHtmlSanitized, RowVersion)`** — unique
  `(FaqItemId, LanguageCode)`; implement `ITranslation` (`HasContent` = Question|Answer sau trim khác rỗng).
- **`FaqEvent`** (Req 4 "tùy chọn") — **DEFER** (không thuộc MVP module; thêm khi có nhu cầu analytics thật, tránh
  gold-plate I10). Ghi rõ để không quên.
- Concurrency: cả 4 entity implement `IHasConcurrencyToken` → xmin (CP15). (Product design chỉ liệt kê FaqItem/
  FaqItemTranslation; MỞ RỘNG sang Category/CategoryTranslation cho nhất quán — quyết định §11, tránh ghi đè âm thầm Name.)
- FK NỘI-module (cùng schema `faq`, hợp lệ): CategoryTranslation→Category, Item→Category, Item→Item(parent),
  ItemTranslation→Item. FK cross-schema: KHÔNG (ResortId Guid trần).

### Ràng buộc DB (migration `faq`)

| Ràng buộc | Cột | Mục đích |
|---|---|---|
| unique `ux_faq_category_key` | `FaqCategory(ResortId, Key)` | Key định danh ổn định/resort |
| unique `ux_faq_category_tr` | `FaqCategoryTranslation(FaqCategoryId, LanguageCode)` | 1 bản dịch/ngôn ngữ (CP5 nguồn) |
| unique `ux_faq_item_tr` | `FaqItemTranslation(FaqItemId, LanguageCode)` | 1 bản dịch/ngôn ngữ |
| FK Restrict | Item→Category, Item→Item(parent), *Translation→cha | không FK chéo schema; không xóa cha còn con |
| xmin | 4 entity (IHasConcurrencyToken) | CP15 409 |

> KHÔNG có partial-unique phức tạp như Rules (không có "IsCurrent"). FAQ mutable trực tiếp (không Draft→Publish) —
> quyết định §11 (khác Rules): FAQ không cần snapshot/version vì (a) requirements KHÔNG yêu cầu version/ack cho FAQ
> (chỉ Rules cần acknowledge); (b) `IsActive` đủ để ẩn/hiện; (c) sửa FAQ hiển thị ngay là hành vi mong muốn của lễ tân
> (không như nội quy cần "publish có kiểm soát"). Tránh gold-plate.

## 4. Admin CRUD + reorder + sanitize (Req 8.5/8.6)

Use case (Application, keyed `faq`) — mirror Rooms/Rules write pattern (value-returning `IUseCase<,>` một SaveChanges;
void command `ICommandUseCase<TInput>` khai `PersistenceKey`):

- **Category**: `CreateFaqCategoryUseCase` (ensure Key unique/resort → `UniqueConstraintViolationException`→`faq_conflict`),
  `UpdateFaqCategoryUseCase` (SortOrder/IsActive; KHÔNG đổi Key), `DeleteFaqCategoryUseCase` (chặn nếu còn item →
  `faq_category_not_empty`, hoặc soft via IsActive — quyết định §11: **hard-delete + chặn khi còn item** để giữ toàn
  vẹn tham chiếu; lễ tân phải xóa/chuyển item trước).
- **CategoryTranslation**: `UpsertFaqCategoryTranslationUseCase` (sanitize `Name` — text thuần nhưng vẫn qua sanitizer
  để nhất quán; upsert theo unique `(category, lang)`).
- **Item**: `CreateFaqItemUseCase` (validate `ParentId` cùng category + không cycle — bất biến §1.8), `UpdateFaqItemUseCase`
  (SortOrder/IsActive/ParentId — re-validate cycle), `DeleteFaqItemUseCase` (chặn nếu còn con → `faq_item_has_children`).
- **ItemTranslation**: `UpsertFaqItemTranslationUseCase` (**sanitize `Question` + `AnswerHtmlSanitized` qua
  `IHtmlSanitizer` TRƯỚC lưu** — CP12; upsert theo unique `(item, lang)`).
- **Reorder** (`POST /faq/reorder`, Req 4.5 drag-drop): `ReorderFaqUseCase` — nhận danh sách `(id, sortOrder)` cho
  category HOẶC item trong một category; cập nhật `SortOrder` hàng loạt trong MỘT transaction. Quyết định §11: reorder
  nhận **toàn bộ thứ tự mới** (không swap từng cặp) → nguyên tử, không trạng thái trung gian trùng SortOrder.

**Cycle-check (bản chất, không ngọn):** khi set `ParentId=p` cho item `c`, đi ngược chuỗi parent từ `p` lên gốc; nếu
gặp `c` → cycle → `faq_invalid_parent`. Dữ liệu nhỏ (chục item/resort) → duyệt in-memory sau khi load các item cùng
category (read-model), KHÔNG cần recursive CTE (tránh provider lock-in, test được SQLite). Đồng thời chặn `p` khác
category của `c` và `p==c`.

## 5. Guest read (cây active + i18n) + rule-gate (CP3 — consumer đầu tiên của IRuleGate)

`GetGuestFaqTreeUseCase` (`IUseCase<GetGuestFaqTreeInput, GetGuestFaqTreeResult>`, read-only, không transaction):

1. Đọc config qua `IResortGuestConfigQuery.GetAsync(resortId)`; null → `configuration_unavailable` (fail-closed).
2. `FaqEnabled=false` → `faq_disabled` (backend enforce feature-flag — §1.6).
3. **Rule-gate**: `IRuleGate.EnsureAcknowledgedAsync(resortId, guestVisitId, GuestFeature.Faq)`; Failure → trả thẳng
   Error (endpoint map `rule_ack_required`→403). Đây là CP3 tận gốc trong use case (§2 lý do).
4. `MatchSupported(requestedLanguage, config.EnabledLanguageCodes, config.DefaultLanguageCode)` → ngôn ngữ hiển thị.
5. Đọc read-model `IFaqReader.LoadActiveTreeAsync(resortId)` — CHỈ category+item `IsActive` + translations (no-tracking).
6. Render mỗi node qua `ITranslationResolver.Resolve(...)` (fallback default + `IsFallback`/`IsMissing` — CP5); dựng
   cây: category (theo SortOrder) → item gốc (`ParentId==null`, theo SortOrder) → item con đệ quy (in-memory).
7. Trả `GetGuestFaqTreeResult(Language, Categories[])`. KHÔNG chứa Id nội bộ nhạy cảm ngoài Guid item/category (cần cho
   navigation cha-con + CTA chat prefill sau).

**Endpoint** `GET /v1/guest/faq?roomId&lang` (mirror `RulesGuestEndpointModule`, AllowAnonymous):
- Resolve `CurrentGuestContext` qua `ICurrentGuestContextResolver` (cookie `GuestAccessModule.SessionCookieName` + roomId).
- Gọi `GetGuestFaqTreeUseCase(context.ResortId, context.GuestVisitId, lang)`.
- **Touch SAU khi đọc thành công** (`TouchAsync(context.GuestVisitId)`) — quyết định §11 (KHÁC Rules-GET-no-touch):
  product design §resolve bước 5 liệt kê `/faq` là API tương tác trượt cửa sổ; duyệt FAQ là hoạt động CHÍNH của khách
  (khác rules-viewer là đọc-lại phụ trợ). Không touch → khách đọc FAQ 31' bị `session_expired` (user-hostile). Touch
  sau-thành-công giữ đúng check-before-touch (QR-AD-032).
- `Cache-Control: no-store`; map Result→HTTP qua ProblemDetailsBuilder; KHÔNG log cookie.

## 6. i18n (CP5)

Tái dùng `ITranslationResolver`: `MatchSupported` chọn ngôn ngữ; `Resolve<ITranslation>` fallback default + `IsFallback`;
`MissingLanguages` cho admin editor (Req 8.7 — "chỉ báo ngôn ngữ thiếu"). Category dịch `Name`; Item dịch
`Question`/`AnswerHtmlSanitized`. Enabled languages + default đọc qua `IResortGuestConfigQuery` (guest) /
`IResortLanguageQuery` hoặc config (admin editor — chốt ở slice nếu cần liệt kê missing).

## 7. HTTP contract

- **Admin/Staff** (`StarHill.Authorization` — Req 8.5 RequireStaff; resortId server-side qua `IResortSettingsQuery`):
  - `POST/PUT/DELETE /v1/faq/categories` (+ `PUT /v1/faq/categories/{id}/translations/{lang}`).
  - `POST/PUT/DELETE /v1/faq/items` (+ `PUT /v1/faq/items/{id}/translations/{lang}`).
  - `POST /v1/faq/reorder` — cập nhật SortOrder hàng loạt (category hoặc item).
  - `GET /v1/faq/admin?lang=` — cây đầy đủ (kể cả inactive) + cờ missing-translation cho editor.
- **Guest** (AllowAnonymous — cookie thiết bị):
  - `GET /v1/guest/faq?roomId&lang=` — cây active đã dịch; `403 rule_ack_required` nếu chưa ack (khi cấu hình);
    `faq_disabled` nếu tắt; `no-store`.
- Mọi lỗi qua `ProblemDetailsBuilder`; mã ổn định mới (thêm `FaqErrors` + cập nhật `ErrorCodeSnapshotTests` QR-AD-018):
  `faq_category_not_found`, `faq_item_not_found`, `faq_conflict` (unique/concurrency), `faq_invalid_parent`,
  `faq_category_not_empty`, `faq_item_has_children`, `faq_disabled`, `configuration_unavailable` (mã chung, hằng riêng module).

## 8. Persistence & Host wiring

- `FaqDbContext : PlatformDbContext`, schema `faq`, keyed `FaqModule.PersistenceKey`; migration + history table trong
  schema `faq` (QR-AD-028: `MigrationsHistoryTable("__EFMigrationsHistory","faq")` ở Host + factory + integration).
- Repos/UoW keyed; read-model `IFaqReader`/`EfFaqReader` (F9 — không IQueryable, trả DTO snapshot). Use case đăng ký
  factory thủ công (mirror Rules/Rooms). KHÔNG map Outbox/Inbox.
- Thêm 5 project + test vào `starhill/Platform.slnx`; thêm CI migration bundle `faq` vào `starhill-ci.yml` (mirror 5 module).
- Host: connection string `Faq` (cùng DB `starhill`, schema `faq`) + `AddFaqInfrastructure`/`AddFaqApi` + migrate gated.
  `IHtmlSanitizer` đã có `RequirePort` từ D-Rules.4c (Faq tái dùng — không thêm RequirePort trùng).

## 9. Correctness properties & guard test (mỗi CP một guard — keystone)

| CP / bất biến | Guard test (khi code) | Docker? |
|---|---|---|
| CP3 rule-gate backend `/faq` | `GuestFaqTreeUseCaseTests` (fake `IRuleGate`: cờ tắt→cây; bật+chưa ack→`rule_ack_required`; bật+đã ack→cây) + endpoint 403 | Không |
| CP5 i18n fallback cây | `GuestFaqTreeUseCaseTests` (SQLite + resolver thật): thiếu dịch item/category→default+IsFallback | Không |
| CP12 sanitize | `FaqSanitizeTests` (SQLite + Ganss thật): Answer chứa `<script>`→lưu ra sạch; Question sanitize | Không |
| CP15 concurrency | `FaqConcurrencyTests` (Postgres xmin): hai update item/translation→người sau 409 | Postgres |
| bất biến cây (ParentId cùng category, không self, không cycle) | `FaqItemParentValidationTests` (SQLite): p khác category→invalid; p==c→invalid; chuỗi tạo cycle→invalid | Không |
| unique `(item,lang)`/`(category,lang)`/`(resort,key)` | `FaqPostgresConstraintTests` (Postgres): vi phạm→UniqueConstraintViolationException | Postgres |
| feature-flag `FaqEnabled=false`→`faq_disabled` | `GuestFaqTreeUseCaseTests` (config stub FaqEnabled=false) | Không |
| reorder nguyên tử | `ReorderFaqUseCaseTests` (SQLite): SortOrder mới áp trọn, không trạng thái trùng | Không |
| role guard `/v1/faq/*` = RequireStaff; guest AllowAnonymous | `FaqEndpointAuthTests` (TestServer + fake use case): Staff→2xx admin; no-token→401 admin; guest no-cookie→problem | Không |
| boundary | `FaqBoundaryTests` (Contracts thuần; Application⊥Infra/Api; cross-module chỉ Contracts; Application ref Rules.Contracts hợp lệ) | Không |
| Host wiring | `HostEndpointWiringSmokeTests` +InlineData `/v1/faq/*` (admin no-token→401; guest no-cookie→problem+json) | Không |

Docker daemon phải có cho test Postgres; không "skip mềm" làm bằng chứng cuối (mirror Rules/GuestAccess).

## 10. Build slices và cổng dừng

1. **E-Faq.0 — design/reconciliation (file này):** journal + diagnostics 0; chưa code.
2. **E-Faq.1 — Domain/Contracts/Persistence:** ✅ XONG (QR-N-046/QR-AD-037): 4 entity `: Entity, IHasConcurrencyToken`
   + `FaqModule.PersistenceKey="faq"` + `FaqDbContext` schema `faq` keyed + `FaqDbContextFactory` + `FaqConfigurations`
   + `AddFaqInfrastructure` (persistence + 4 keyed repo) + migration `InitialCreate` (verify: 3 unique
   `ux_faq_category_key`/`ux_faq_category_translation_lang`/`ux_faq_item_translation_lang`, FK Restrict item→category &
   item→parent + Cascade translation→cha, xmin ×4) + `FaqBoundaryTests` (3) + `FaqPostgresConstraintTests` (4, Postgres/CI).
   Host wiring + CI bundle `faq` DEFER sang E-Faq.4 (mirror Rules D-Rules.1 chưa wire Host). `vp all` 0-warning/0-fail;
   `vp journal` INV-1..6 xanh.
3. **E-Faq.2 — Admin CRUD category/item + translation (sanitize-on-save) + cycle-check:** ✅ XONG (QR-N-047): `FaqErrors`
   (6 mã) + `FaqContracts` + 8 use case (Create/Update/Delete category+item, UpsertTranslation category+item) + validator +
   `FaqItemParentValidator` (cùng-category/không-self/không-cycle, walk FindById F9) + delete-guard (category-not-empty/item-has-children).
   ResortId item derive từ category. `Faq.Infrastructure` ref→Application + đăng ký use case/validator keyed. `ErrorCodeSnapshotTests`
   +6 mã Faq. Test: `FaqSanitizeTests` (CP12, 3) + `FaqItemParentValidationTests` (5) + `FaqAdminCrudTests` (5) + `FaqConcurrencyTests`
   (CP15 Postgres). `vp all` Faq.IntegrationTests 13 pass/5 skip; `vp journal` INV-1..6 xanh.
4. **E-Faq.3 — Reorder:** `ReorderFaqUseCase` + `ReorderFaqUseCaseTests`.
5. **E-Faq.4 — Guest read tree + rule-gate + Api:** `IFaqReader`/`EfFaqReader` + `GetGuestFaqTreeUseCase` (gate+i18n+tree)
   + `FaqGuestEndpointModule` (resolve→gate→tree→touch) + `FaqAdminEndpointModule` (RequireStaff) + `AddFaqApi` + Host
   wire + `FaqEndpointAuthTests` + `HostEndpointWiringSmokeTests` +InlineData + `ErrorCodeSnapshotTests` +mã Faq. Flip
   QR-AD-0xx (Faq) → Implemented + Guard-Tests (INV-6).

Mỗi slice dừng nếu: build warning/error; JournalConsistency INV-1..6 fail; migration model drift; Docker unique/
concurrency test fail; raw cookie/secret lọt log; **AD chuyển Implemented mà thiếu `Guard-Tests` (INV-6)**.

## 11. Quyết định/trade-off liên quan (ghi journal khi chốt code)

- **QR-AD-0xx (Faq module)**: Faq DỰNG MỚI, keyed schema `faq`, 4 entity + i18n; **KHÔNG Draft→Publish/version**
  (khác Rules) vì requirements không yêu cầu ack/version cho FAQ — `IsActive` đủ; tránh gold-plate.
- **QR-AD-0xx (rule-gate trong use case)**: `Faq.Application` ref `Rules.Contracts`; gate đặt trong `GetGuestFaqTreeUseCase`
  (defense-in-depth CP3) thay vì chỉ endpoint. Trade-off: coupling Application→Rules.Contracts vs enforce-in-endpoint
  dễ quên.
- **QR-AD-0xx (FAQ GET touch cửa sổ)**: guest `/faq` GET **touch sau thành công** (product design liệt kê /faq là API
  tương tác), KHÁC quyết định Rules-GET-no-touch (rules-viewer là đọc-lại phụ trợ). Trade-off: nhất quán nội bộ 2 GET
  endpoint vs trung thành product-spec + tránh expire khi đang duyệt FAQ. Chọn trung thành product-spec.
- **QR-AD-0xx (bất biến cây)**: chặn ParentId khác-category/self/cycle (`faq_invalid_parent`) — spec chỉ nói "cha-con"
  không nói ràng buộc; AI tự ra để tránh đồ thị vòng gây render loop guest. Cycle-check in-memory (dữ liệu nhỏ).
- **QR-AD-0xx (concurrency token mở rộng)**: thêm xmin cho Category/CategoryTranslation (product chỉ liệt kê Item/
  ItemTranslation) — nhất quán chống ghi đè âm thầm Name.
- **QR-AD-0xx (delete an toàn tham chiếu)**: hard-delete category chặn khi còn item; hard-delete item chặn khi còn con
  (`faq_category_not_empty`/`faq_item_has_children`) — giữ toàn vẹn cây thay vì cascade âm thầm.
- **QR-DV-0xx (FaqEvent defer)**: bỏ `FaqEvent` (product ghi "tùy chọn") khỏi MVP — I10 tránh phân mảnh; thêm khi có analytics.
- **QR-TO-0xx**: reorder nhận-toàn-bộ-thứ-tự-mới (nguyên tử) vs swap từng cặp; in-memory cycle-check vs recursive CTE.
- **QR-TO-0xx (CTA chat prefill Req 4.6)**: DEFER sang Concierge (cần Conversation tồn tại) — guest FAQ trả Guid item
  để frontend dựng CTA; không nhồi coupling Faq→Concierge sớm.

## 12. Self-validation trước code

- [x] Data model lấy từ product design (không bịa field); nơi đặt schema/constraint chốt rõ (§3).
- [x] Contracts tiêu thụ (`IRuleGate`/`ICurrentGuestContextResolver`/`IResortGuestConfigQuery`/`ITranslationResolver`/
      `IResortSettingsQuery`/`IHtmlSanitizer`) đã ĐỌC CHỮ KÝ THẬT (§0) — không suy đoán.
- [x] Faq là consumer đầu tiên của `IRuleGate` → thiết kế kiểm chứng luôn rule-gate (CP3) qua use-case placement.
- [x] Guest endpoint mirror `RulesGuestEndpointModule` (resolve→gate→tree→touch; no-store; ProblemDetails; cookie canonical).
- [x] Admin endpoint mirror `RulesAdminEndpointModule` (RequireStaff; resortId server-side).
- [x] Sanitize-on-save qua `IHtmlSanitizer` (adapter + RequirePort đã có từ D-Rules.4c — không dựng lại).
- [x] Bất biến cây (ParentId) + concurrency (xmin) + i18n fallback + feature-flag đều có guard test + nơi chạy (§9).
- [x] Quyết định tự-ra + trade-off + deviation liệt kê §11 để ghi journal khi code (4-việc).
- [ ] User review design trước E-Faq.1 implementation.
