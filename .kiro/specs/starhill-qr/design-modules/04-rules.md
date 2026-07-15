# Module design — Rules (Wave D)

> **Design-first, CHƯA triển khai code.** Chi tiết hóa `../design.md` §4.5. WHAT:
> `docs/resort-qr-portal/requirements.md` Req 3 (force-read/acknowledge), Req 8 (Draft→Publish/version/concurrency),
> Req 11.4 (sanitize), Req 14.1/14.2 (rule-gate flags trong ResortSettings). Data model nguồn:
> `docs/resort-qr-portal/design.md` §Data Models (Rules) + §Correctness Properties CP3/CP4/CP5/CP12/CP13/CP15.
> Mọi kết luận dưới đây dựa trên file/code đã đọc (không suy đoán). Rules là module **DỰNG MỚI** — legacy
> `resort-qr/` CHƯA có Rules nên không port; chỉ tái dùng port Bedrock + Contracts module đã có.

## 0. Đối soát nguồn (đã verify trên đĩa)

- **Legacy KHÔNG có Rules**: `resort-qr/` mới tới Identity/Rooms/GuestAccess (design.md §0). ⇒ thiết kế từ requirements, không port.
- **Contracts đã có** (đọc code): `ResortConfig.Contracts.IResortSettingsQuery` (`ResortSettingsSnapshot` mang đủ 3 cờ
  `RequireRuleAckForFaq/Chat/Housekeeping`, `PortalWindowMinutes`), `ITranslationResolver`/`ITranslation`/`Translated<T>`
  (i18n fallback + `MissingLanguages`), `Rooms.Contracts.IRoomTokenResolver`.
- **Contracts CÒN THIẾU (Rules là consumer đầu → phải tạo)**:
  1. `GuestAccess.Contracts` hiện CHỈ có `GuestAccessModule` (PersistenceKey) — **chưa có port current-guest-context**.
     `GuestAccess.Contracts.csproj` ghi rõ "thêm khi consumer đầu tiên (Rules) tồn tại (QR-AD-024)". ⇒ **C-GA.4 co-design ở đây** (§6).
  2. `IHtmlSanitizer` (base `platform/src/Bedrock.Application/Ports/Html`) là **port bắt buộc, KHÔNG default**, nhưng
     hiện **chưa có adapter, chưa pin package, chưa RequirePort** (Host boot được vì chưa ai consume). Rules là consumer
     đầu → phải đóng mắt xích (§7).
- **xmin**: `PlatformDbContext.OnModelCreating` map `RowVersion`→`xmin` (xid, ValueGeneratedOnAddOrUpdate,
  IsConcurrencyToken) CHỈ khi Npgsql + entity implement `IHasConcurrencyToken`. ⇒ Draft entity khai
  `IHasConcurrencyToken` để đạt CP15 (409).

## 1. Mục tiêu và bất biến

1. **Snapshot bất biến (CP4)**: khách CHỈ đọc từ `RulePublication` có `IsCurrent=true`; sửa Draft (`RuleSection`/
   translation) KHÔNG bao giờ lọt ra khách tới khi Publish tạo publication mới.
2. **Đúng một publication hiện hành/resort (CP4)**: partial unique `RulePublication(ResortId) WHERE IsCurrent=true`.
3. **Acknowledge do SERVER xác thực (CP13)**: server tự xác định publication `IsCurrent`; KHÔNG tin `version` client gửi.
4. **Ack gắn GuestVisit (Req 3.7/3.8)**: unique `(GuestVisitId, RulePublicationId)`; đã ack version hiện hành trong
   visit hiện tại → không bắt đọc lại (kể cả quét lại); publish version mới HOẶC visit mới → phải ack lại.
5. **Rule-gate enforce ở BACKEND (CP3)**: Faq/Concierge/Housekeeping gọi gate; chưa ack (khi cấu hình yêu cầu) →
   `403 rule_ack_required`. Không chỉ chặn ở frontend.
6. **Sanitize nội dung (CP12/Req 8.6)**: HTML nội quy do nội bộ nhập được sanitize; khách không bao giờ nhận HTML chứa script.
7. **i18n fallback (CP5)**: thiếu bản dịch → trả default + `IsFallback`, không rỗng.
8. **Optimistic concurrency (CP15)**: hai người sửa cùng nội dung Draft → người sau nhận 409, không ghi đè âm thầm.
9. **Boundary**: cross-module chỉ qua `<M>.Contracts` + Id trần (Guid); không FK chéo schema; keyed persistence `rules`.

## 2. Cấu trúc module và dependency

```text
starhill/src/Modules/Rules/
  Rules.Domain         -> Bedrock.Domain
  Rules.Contracts      -> Bedrock.Messaging.Contracts   (lộ IRuleGate + DTO gate)
  Rules.Application    -> Domain + Contracts + ResortConfig.Contracts + GuestAccess.Contracts + Bedrock.Application (+FluentValidation)
  Rules.Infrastructure -> Application + Bedrock.Infrastructure + EF Core/Npgsql (+FluentValidation)
  Rules.Api            -> Application + ResortConfig.Contracts + GuestAccess.Contracts + StarHill.Authorization + Bedrock.Api
```

- `RulesModule.PersistenceKey = "rules"` (hằng ở `Rules.Contracts`, nguồn duy nhất — mirror các module khác).
- Mirror chính xác khuôn Rooms (đã đọc 5 csproj): Domain chỉ ref Bedrock.Domain; Api KHÔNG ref Infrastructure (I7);
  cross-module chỉ Contracts; dùng `$(PlatformSrc)`.
- **Rules.Contracts phụ thuộc gì?** Chỉ `Bedrock.Messaging.Contracts` (thuần). `IRuleGate` nhận Id trần + enum feature,
  KHÔNG ref GuestAccess/ResortConfig (tránh coupling Contracts↔Contracts). Consumer (Faq/Concierge/Housekeeping) tự
  resolve GuestVisit rồi truyền `guestVisitId` vào gate.

## 3. Domain model

> Field/enum/quan hệ là **nguồn product** `docs/resort-qr-portal/design.md` §Data Models (Rules) — TÁI DÙNG nguyên,
> không viết lại. Phần dưới chốt **nơi đặt** (schema `rules`) + **ràng buộc DB** + marker concurrency.

- **Draft (mutable, chỉ admin)**: `RuleSet(Id, ResortId, UpdatedAt, RowVersion)`; `RuleSection(Id, RuleSetId, Key,
  SortOrder, IsRequired, RequireScrollEnd, MinReadSeconds, RowVersion)`; `RuleSectionTranslation(Id, RuleSectionId,
  LanguageCode, Title, BodyHtmlSanitized, RowVersion)` — unique `(RuleSectionId, LanguageCode)`, implement
  `ITranslation` (`HasContent` = Title/Body sau trim khác rỗng).
- **Publication (snapshot bất biến — khách đọc)**: `RulePublication(Id, ResortId, Version, PublishedAt,
  PublishedByUserId, ChangeNote?, IsCurrent)` — partial unique `(ResortId) WHERE IsCurrent`; `RulePublicationSection`
  (bản sao đông cứng section); `RulePublicationSectionTranslation` — unique `(RulePublicationSectionId, LanguageCode)`.
  **KHÔNG concurrency token** (bất biến sau publish, không sửa).
- **`RuleAcknowledgement(Id, ResortId, RoomId, GuestSessionId, GuestVisitId, RulePublicationId, Version, LanguageCode,
  AcceptedAt)`** — unique `(GuestVisitId, RulePublicationId)`; các Id là **Guid trần** (không FK chéo schema
  guest_access/rooms). Bản ghi vận hành (Req 11.7 — KHÔNG chứng cứ pháp lý; không IpHash phức tạp).
- Concurrency: `RuleSet`/`RuleSection`/`RuleSectionTranslation` implement `IHasConcurrencyToken` → xmin (CP15).
- FK NỘI-module (cùng schema `rules`, hợp lệ): Section→RuleSet, Translation→Section, PublicationSection→Publication,
  PublicationSectionTranslation→PublicationSection. FK cross-schema: KHÔNG.

## 4. Draft → Publish (snapshot nguyên tử) — CP4

`PublishRulesUseCase` (transactional, keyed `rules`):

1. Đọc Draft `RuleSet` + toàn bộ `RuleSection(+Translation)` theo `SortOrder`.
2. Trong MỘT transaction:
   a. `UPDATE RulePublication SET IsCurrent=false WHERE ResortId=@r AND IsCurrent` **và SaveChanges TRƯỚC**;
   b. insert `RulePublication` mới (`Version = maxVersion+1`, `IsCurrent=true`, `PublishedByUserId`, `ChangeNote`);
   c. copy đông cứng mỗi section→`RulePublicationSection`, mỗi translation→`RulePublicationSectionTranslation`
      (BodyHtmlSanitized đã sanitize ở Draft — §7).
3. Commit. Khách lần resolve/GetCurrentRules kế tiếp thấy version mới và phải ack lại (§5).

**Thứ tự flip-before-insert (bản chất, không phải ngọn):** PostgreSQL kiểm partial unique `WHERE IsCurrent` tại mỗi
row-write (không deferrable mặc định). Nếu insert bản mới `IsCurrent=true` TRƯỚC khi hạ bản cũ → hai hàng cùng thỏa
partial unique tại thời điểm insert → 23505. Vì vậy hạ cũ + SaveChanges trước, rồi mới insert mới. Đây là cùng lớp
bài học Npgsql-ordering đã ghi ở QR-N-018 (ResortLanguage default) — áp lại có chủ đích, guard bằng test Postgres thật.

**Preview (Req 8.4):** `GetDraftPreviewUseCase` render từ Draft (KHÔNG tạo publication) — chỉ admin/staff, không ảnh
hưởng khách. Lịch sử: `GetPublicationHistory` đọc các publication cũ.

## 5. Guest đọc nội quy + Acknowledge (server-authoritative) — CP13

- `GetCurrentRulesUseCase(resortId, requestedLanguage)`: đọc `RulePublication IsCurrent` + sections (theo SortOrder) +
  translation resolve qua `ITranslationResolver` (fallback default + `IsFallback` — CP5). KHÔNG đọc Draft.
- `AcknowledgeRulesUseCase(currentGuestContext, requestedLanguage)`:
  1. Server đọc `RulePublication IsCurrent` của `resortId` (từ context, KHÔNG tin version client — CP13).
  2. Ghi `RuleAcknowledgement(GuestVisitId, RulePublicationId=current.Id, Version=current.Version, RoomId, ...)`.
  3. Unique `(GuestVisitId, RulePublicationId)` → ack lặp idempotent (đã ack cùng publication trong visit → trả OK,
     không tạo trùng; bắt `UniqueConstraintViolationException` base QR-AD-010 → coi là đã ack).
  4. Publish version mới ⇒ `IsCurrent` mới ⇒ ack cũ (publication khác) không thỏa → khách phải ack lại (Req 3.8/3.9).
- **Không** tin `version`/`publicationId` do client gửi để quyết định — chỉ dùng `IsCurrent` server đọc. Client gửi
  version chỉ để đối chiếu hiển thị (tùy chọn), không phải nguồn quyết định.

## 6. Rule-gate + current-guest-context (co-design C-GA.4) — CP3

### 6.1 `Rules.Contracts.IRuleGate` (Rules lộ ra)

```csharp
public enum GuestFeature { Faq, Chat, Housekeeping }

public interface IRuleGate
{
    // Trả Result.Success nếu KHÔNG cần ack (cấu hình tắt) hoặc đã ack version IsCurrent trong visit này;
    // ngược lại Result.Failure(rule_ack_required). resortId+guestVisitId là Guid trần (không ref module khác).
    Task<Result> EnsureAcknowledgedAsync(Guid resortId, Guid guestVisitId, GuestFeature feature, CancellationToken ct = default);
}
```

- Impl `RuleGate` (Rules.Application): đọc cờ `RequireRuleAckFor<Feature>` qua `IResortSettingsQuery`; nếu tắt → Success.
  Nếu bật → kiểm tồn tại `RuleAcknowledgement(GuestVisitId, RulePublicationId=current.Id)`; thiếu → Failure
  `rule_ack_required` (map HTTP 403).
- Faq/Concierge/Housekeeping ref `Rules.Contracts` + gọi gate SAU khi đã resolve GuestVisit (§6.2), TRƯỚC nghiệp vụ.

### 6.2 `GuestAccess.Contracts` current-guest-context (C-GA.4 — Rules kích hoạt)

Rules (và Faq/Concierge/Housekeeping) cần GuestVisit hiện hành từ cookie thiết bị + enforce **portal-window
check-before-touch** (design.md §resolve bước 5: kiểm `now - LastSeenAt > PortalWindow` TRƯỚC; quá hạn → `session_expired`
KHÔNG touch; còn hạn → xử lý xong rồi mới touch). GuestAccess sở hữu GuestVisit nên port đặt ở `GuestAccess.Contracts`:

```csharp
public sealed record CurrentGuestContext(Guid GuestVisitId, Guid GuestSessionId, Guid RoomId, Guid ResortId);

public interface ICurrentGuestContextResolver
{
    // Từ raw cookie key + roomId (khách đang ở trang phòng nào). Trả:
    //  - Success(context) nếu có GuestVisit Active hợp lệ và CÒN trong portal-window (KHÔNG touch ở bước đọc).
    //  - Failure(session_expired) nếu quá portal-window hoặc visit không Active.
    //  - Failure(qr_invalid/guest_context_missing) nếu cookie/visit không phân giải được.
    Task<Result<CurrentGuestContext>> ResolveAsync(string? sessionKey, Guid roomId, CancellationToken ct = default);

    // Touch LastSeenAt=now + đẩy ExpiresAt SAU khi nghiệp vụ thành công (tách khỏi Resolve để giữ đúng
    // check-before-touch: chỉ gia hạn khi thao tác hợp lệ đã hoàn tất — không tự gia hạn vô hạn).
    Task TouchAsync(Guid guestVisitId, CancellationToken ct = default);
}
```

- **Vì sao tách Resolve/Touch:** design.md §resolve bước 5 cảnh báo tận gốc — nếu touch trước khi kiểm thì cửa sổ tự
  gia hạn vô hạn, không bao giờ hết hạn. Endpoint guest: `Resolve` (đọc, kiểm window) → chạy nghiệp vụ → nếu OK `Touch`.
- **Vì sao đặt ở GuestAccess.Contracts, không phải Rules:** GuestVisit thuộc schema `guest_access`; QR-AD-024 cấm module
  khác đọc GuestAccessDbContext. Đây chính là port C-GA.4 mà `GuestAccess.Contracts.csproj` đã hẹn thêm khi có consumer.
- Impl `EfCurrentGuestContextResolver` (GuestAccess.Infrastructure) đọc GuestSession(hash cookie)→GuestVisit Active của
  (session, room). Hasher/`FOR UPDATE` chỉ cần cho ghi; đọc-kiểm-window là read thường + `Touch` là update hẹp.

## 7. Sanitize HTML + đóng mắt xích IHtmlSanitizer (Req 8.6/11.4/CP12)

- **Sanitize-on-save là bất biến chuẩn** (cột `BodyHtmlSanitized`): mọi ghi `RuleSectionTranslation.Title/Body` đi qua
  `IHtmlSanitizer.Sanitize` TRƯỚC khi lưu (trong use case Application). Publish copy nội dung ĐÃ sanitize → guest read
  an toàn by-construction (không cần double-sanitize khi trả — xem QR-TO-011).
- **Đóng mắt xích port bắt buộc**: cần (a) adapter impl `IHtmlSanitizer` (thư viện allowlist `Ganss.Xss`/HtmlSanitizer —
  legacy resort-qr từng dùng), (b) pin package ở `starhill/Directory.Packages.props`, (c) đăng ký DI + **RequirePort**
  để boot fail-fast nếu thiếu (đúng ý "port bảo mật không default").
- **Nơi đặt adapter (quyết định — QR-AD-031):** shared project cấp starhill (mirror precedent `StarHill.Authorization`)
  thay vì nhét base hoặc Rules.Infrastructure. Lý do: adapter Ganss là generic, Faq cũng cần → đặt ở Rules.Infrastructure
  sẽ buộc Faq ref Rules (phá boundary); nhét base `platform/` đúng về "domain-agnostic" nhưng là nhu cầu do sản phẩm kéo
  vào và làm bẩn base — shared starhill project giữ base sạch (D1-a) + tái dùng Rules/Faq. RequirePort đặt ở Host wiring
  (hoặc extension shared) — chốt cụ thể ở slice code, không khóa sớm.

## 8. i18n (CP5)

Tái dùng `ITranslationResolver` (đã có): `MatchSupported` chọn ngôn ngữ hiển thị; `Resolve<T>` fallback default +
`IsFallback`; `MissingLanguages` cho admin editor (Req 8.7). `RuleSectionTranslation`/`RulePublicationSectionTranslation`
implement `ITranslation`. Enabled languages + default đọc qua `IResortGuestConfigQuery`/`IResortSettingsQuery`.

## 9. HTTP contract

- **Admin/Staff** (StarHill.Authorization — Req 8/11.3, RequireStaff cho soạn nội quy, Publish có thể RequireStaff):
  - `GET/POST/PUT/DELETE /v1/rules/sections` (+ `/translations`) — CRUD Draft (sanitize-on-save).
  - `POST /v1/rules/publish` — snapshot + Version++ (§4).
  - `GET /v1/rules/preview` — render Draft như khách.
  - `GET /v1/rules/publications` — lịch sử publication.
- **Guest** (AllowAnonymous — cookie thiết bị, mirror GuestAccess):
  - `GET /v1/guest/rules?lang=` — đọc publication `IsCurrent` (resolve visit qua §6.2 để biết resortId/room + window).
  - `POST /v1/guest/rules/acknowledge` — server-authoritative ack (§5); `no-store`; không log secret.
- Mọi lỗi qua `ProblemDetailsBuilder`; mã ổn định mới: `rule_ack_required` (403), `rules_unavailable` (chưa publish),
  `session_expired` (portal-window) — thêm vào catalog + cập nhật `ErrorCodeSnapshotTests` (QR-AD-018) khi code.

## 10. Persistence & Host wiring

- `RulesDbContext : PlatformDbContext`, schema `rules`, keyed `RulesModule.PersistenceKey`; migration + history table
  trong schema `rules` (QR-AD-028: `MigrationsHistoryTable("__EFMigrationsHistory","rules")` ở Host + factory + integration).
- Repos/UoW keyed; use case đăng ký factory thủ công (mirror Rooms/GuestAccess). KHÔNG map Outbox/Inbox (Rules chưa phát event).
- Thêm 5 project + test vào `starhill/Platform.slnx`; thêm CI migration bundle `rules` (mirror 4 module).
- Host: connection string `Rules` (cùng DB `starhill`, schema `rules`) + `AddRulesInfrastructure`/`AddRulesApi` + migrate gated.

## 11. Correctness properties & guard test (mỗi CP một guard — keystone)

| CP | Guard test (khi code) | Docker? |
|---|---|---|
| CP4 snapshot: sửa Draft không đổi cái khách đọc; publish flip IsCurrent atomic | `PublishRulesUseCaseTests` (SQLite) + Postgres partial-unique `RulePublication IsCurrent` (Testcontainers) | Postgres cho unique |
| CP13 ack server-authoritative | `AcknowledgeRulesUseCaseTests`: bỏ qua version client; luôn dùng IsCurrent; ack lặp idempotent | Không |
| CP3 rule-gate backend | `RuleGateTests` (cờ tắt→Success; bật+chưa ack→Failure; bật+đã ack→Success) + endpoint 403 | Không |
| CP5 i18n fallback | `RuleTranslationTests` qua ITranslationResolver (thiếu dịch→default+IsFallback) | Không |
| CP12 sanitize | `RuleSanitizeTests`: Body chứa `<script>` → lưu ra không còn script | Không |
| CP15 concurrency | `RuleConcurrencyTests` (xmin, Testcontainers): hai update Draft → người sau 409 | Postgres |
| ack unique (visit,publication) | Postgres constraint test | Postgres |
| boundary | `RulesBoundaryTests` (Contracts thuần; Application⊥Infra/Api; cross-module chỉ Contracts) | Không |
| C-GA.4 window check-before-touch | `CurrentGuestContextResolverTests` (Postgres): quá window→session_expired KHÔNG touch; còn hạn→touch sau thành công | Postgres |

Docker daemon phải có Server cho các test Postgres; không "skip mềm" làm bằng chứng cuối (QR-AD, mirror GuestAccess).

## 12. Build slices và cổng dừng

1. **D-Rules.0 — design/reconciliation (file này):** journal + diagnostics; chưa code.
2. **D-Rules.1 — Domain/Contracts/Persistence:** ✅ XONG (QR-N-031): 7 entity + `RulesDbContext` schema `rules` keyed +
   migration `InitialCreate` (partial unique `ux_rule_publication_current` filter `is_current`, unique ack `(visit,pub)`,
   unique translation `(section,lang)`, xmin cho Draft entity, FK nội-schema Cascade/Restrict, KHÔNG FK chéo schema) +
   `RulesBoundaryTests` (3) + `RulesPostgresConstraintTests` (3, Docker thật). Build 0-warning; full suite 0-fail/0-skip Docker.
3. **D-Rules.2 — Draft CRUD + sanitize:**
   - **D-Rules.2a** ✅ XONG (QR-N-033): adapter `IHtmlSanitizer` (project shared `StarHill.Html`, Ganss.Xss 9.0.892,
     per-call construct chống concurrency) + pin package + `AddStarHillHtml` + 6 unit test (script/event/js-uri/img/format/empty).
   - **D-Rules.2b** ✅ XONG (QR-N-035): `Rules.Application` CRUD Draft section/translation (sanitize-on-save qua
     `IHtmlSanitizer`) + Rules.Infrastructure ref Application + keyed repo/use-case factory + đúng-một RuleSet/resort
     `ux_rule_set_resort` (QR-AD-035, lazy find-or-create + DraftConflict). Test: CP12 `RuleSanitizeTests` (SQLite +
     Ganss thật, 3), CP15 `RuleConcurrencyTests` (Postgres xmin, 1), AD-035 `RulesPostgresConstraintTests.Only_one_rule_set_per_resort`,
     I7 `RulesBoundaryTests.Application_should_not_depend...`. Rules.IntegrationTests 8/8 Docker thật; boundary 4/4.
     QR-AD-031 vẫn Proposed (còn RequirePort ở D-Rules.4 mới đủ Implemented).
4. **D-Rules.3 — Publish snapshot + preview/history:**
   - **D-Rules.3a** ✅ XONG (QR-N-036/QR-AD-036): `PublishRulesUseCase` (IUseCase tự-quản transaction, flip-before-insert,
     version=current+1) + read-model `IRuleDraftReader`/`EfRuleDraftReader` (F9-compliant, không IQueryable) + 3 repo
     publication keyed + `NoPublishableContent`. Test `PublishRulesUseCaseTests` (Postgres migration thật, 4): version-1
     frozen ordered; demote+đúng-một-current (partial-unique atomic); CP4 immutability (sửa Draft không đổi snapshot);
     Draft rỗng→fail. Rules.IntegrationTests 12/12 Docker, 0 skip.
   - **D-Rules.3b** ⏳: `GetDraftPreviewUseCase` (render Draft như khách, không publish — admin/staff) + `GetPublicationHistory`
     (đọc publication cũ). Read đơn giản — có thể gộp cùng đường guest-read D-Rules.4.
5. **C-GA.4 — GuestAccess current-guest-context port** (§6.2): ✅ XONG (QR-N-037/QR-AD-032): `ICurrentGuestContextResolver`
   + `CurrentGuestContext` (Contracts, Result<T>) + `EfCurrentGuestContextResolver` (Resolve đọc-kiểm-window KHÔNG touch;
   Touch trượt+giữ idle-delta, no-op nếu không Active) + 2 mã lỗi `session_expired`/`guest_context_missing`. Test
   `CurrentGuestContextResolverTests` (Postgres, 8). GuestAccess.IntegrationTests 33/33 Docker. Đóng gap registry Rules
   (`rules_conflict` vào ErrorCodeSnapshotTests). Là dependency của guest rules read/ack + rule-gate (D-Rules.4).
6. **D-Rules.4 — Guest read + acknowledge + IRuleGate:**
   - **D-Rules.4a** ✅ XONG (QR-N-038): `GetCurrentRulesUseCase` (publication IsCurrent + i18n fallback CP5) + read-model
     `IRulePublicationReader`/`EfRulePublicationReader` + `RulesUnavailable`/`ConfigurationUnavailable`. `ITranslation`
     trên read-DTO (QR-DV-007). Test `GetCurrentRulesTests` (SQLite + resolver thật, 7). Rules.IntegrationTests 19/19.
   - **D-Rules.4b** ⏳: `AcknowledgeRulesUseCase` (server-authoritative CP13, unique (visit,publication) idempotent,
     dùng `ICurrentGuestContextResolver` + touch sau thành công). Postgres.
   - **D-Rules.4c** ⏳: `IRuleGate` (CP3, cờ ResortSettings) + endpoints (admin publish/preview + guest read/ack) +
     Host wiring + CI bundle + RequirePort(IHtmlSanitizer) → QR-AD-030 (+031) chuyển Implemented + Guard-Tests.

Mỗi slice dừng nếu: build warning/error; JournalConsistency INV-1..6 fail; migration model drift; Docker unique/
concurrency/window test fail; raw secret/cookie lọt log; **AD chuyển Implemented mà thiếu `Guard-Tests` (INV-6)**.

## 13. Quyết định/trade-off liên quan (ghi journal khi chốt)

- **QR-AD-030**: Rules snapshot-on-publish + acknowledge server-authoritative + rule-gate backend (`IRuleGate`).
- **QR-AD-031**: IHtmlSanitizer adapter (Ganss) đặt ở shared starhill project + RequirePort + sanitize-on-save.
- **QR-AD-032**: `GuestAccess.Contracts.ICurrentGuestContextResolver` (C-GA.4) — port current-guest-context +
  portal-window check-before-touch (Resolve/Touch tách đôi).
- **QR-TO-010**: rule-gate truyền `guestVisitId` (Rules.Contracts không ref GuestAccess/ResortConfig) vs gate tự resolve visit.
- **QR-TO-011**: sanitize-on-save authoritative (không double-sanitize on-read) vs sanitize cả hai chiều.

## 14. Self-validation trước code

- [x] Data model lấy từ product design (không bịa field); nơi đặt schema/constraint chốt rõ.
- [x] Contracts đã có (`IResortSettingsQuery`/`ITranslationResolver`/`IRoomTokenResolver`) đọc thật; phần thiếu
      (current-guest-context, IHtmlSanitizer wiring) nêu tường minh + phương án.
- [x] Snapshot flip-before-insert lý giải bằng cơ chế partial-unique Postgres (mirror QR-N-018), có guard Postgres.
- [x] Acknowledge server-authoritative + idempotent (unique visit,publication) — không tin client version (CP13).
- [x] Rule-gate backend + cờ ResortSettings; Rules.Contracts không coupling Contracts module khác.
- [x] Portal-window check-before-touch giữ đúng semantics design §resolve (tách Resolve/Touch).
- [x] Concurrency xmin qua IHasConcurrencyToken (cơ chế PlatformDbContext đã đọc).
- [x] Mỗi CP có guard test + nơi chạy; Postgres test là gate cuối cho unique/concurrency/window.
- [ ] User review design trước D-Rules.1 implementation.
