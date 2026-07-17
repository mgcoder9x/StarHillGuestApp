# 04 — Notes / Things To Know (pha QR)

> Journal RIÊNG cho pha `starhill-qr`. ID tiền tố `QR-N-###`. Mọi điều cần biết trước khi động vào pha QR.

---

### QR-N-001 — Điểm khởi đầu pha QR (trạng thái đã verify, 2026-07-10)
- **Base `platform-base` HOÀN TẤT + verify:** `platform\scripts\vp.cmd` → build 0-warning + test 247 (230 pass / 17 skip-Docker / 0 fail) + validate-ci OK; `vp journal` → JournalConsistency 5/5. Journal base tới AD-068/DV-016/TO-011/N-073. Cổng "base tốt" (user yêu cầu trước khi làm QR) ĐẠT.
- **Đã copy:** `starhill/` = bản vendored của `platform/` (loại bin/obj), build 0-warning độc lập. `platform/` giữ nguyên sạch (chưa/không có nghiệp vụ QR — CP1 no-business-in-core vẫn giữ ở base).
- **Bản CŨ `resort-qr/`:** standalone trên `ResortQr.SharedKernel` (không phải Bedrock). Đã build nhiều slice (resolve-token, rooms-crud/qr, qr-render, rules, faq, messaging, housekeeping, openapi) + admin-web (Vue). Domain: Rooms/GuestAccess/Rules/FAQ/Messaging/Housekeeping/Identity/Resorts. Là NGUỒN LOGIC để port + tham chiếu design (`resort-qr/docs/*-design.md`).
- **Spec QR nguồn:** `docs/resort-qr-portal/` (requirements 14 nhóm, design, tasks 19 nhóm, deep-solution-design + Decision Log, test-plan, 15 CP).

### QR-N-002 — Ranh giới base sạch (KHÔNG vi phạm khi làm QR)
- `platform/` (base gốc) = ĐÓNG BĂNG. Mọi thay đổi cho QR làm ở `starhill/`. Nếu phát hiện base gốc thiếu năng lực nền (không phải nghiệp vụ QR) → cân nhắc bổ sung ở `platform/` (base) rồi copy sang, KHÔNG nhồi nghiệp vụ QR vào base.
- Bedrock lõi (`Bedrock.*` trong `starhill/`) vẫn phải giữ no-business-in-core (CP1) — nghiệp vụ QR nằm ở MODULE (`starhill/src/Modules/<QRDomain>`), KHÔNG lọt vào Bedrock.*.

### QR-N-003 — Loose-end kỹ thuật của bản copy (xử lý khi dựng CI/CD sản phẩm)
- `starhill/scripts/vp.cmd` + `tools/verify.ps1` copy theo → chạy được cho `starhill/` (path tương đối trong starhill/). NHƯNG `starhill/tests/validate_ci.py` (`parents[2]`) trỏ về repo-root `.github/workflows/ci.yml` = CI của BASE (nhắm `platform/`), KHÔNG phải CI sản phẩm. → Khi dựng CI cho sản phẩm StarHill: tạo CI riêng nhắm `starhill/` + sửa `validate_ci.py` của bản copy cho khớp. Ghi để không quên.

### QR-N-004 — design.md pha QR đã tạo + verify (2026-07-11); trạng thái nguồn port đã kiểm
- **design.md tạo xong** tại `.kiro/specs/starhill-qr/design.md`, getDiagnostics = **0** (đủ section chuẩn Kiro Spec Format: Overview/Architecture/Components/Data Models/Correctness Properties[15 Property + Validates]/Error Handling/Testing Strategy). Là artifact design-first để user duyệt TRƯỚC khi build.
- **Nguồn port đã verify trên đĩa** (không suy đoán): `resort-qr/` đã làm tới **wave 4** — có Domain `{Identity, Resorts, Rooms, GuestAccess}`, Application `{Identity(login/refresh/logout), Rooms(CRUD+token+QR PNG), GuestAccess(resolve), Localization}`, Infra `{AppDbContext đơn, QrCoderQrService, Argon2/JWT/HtmlSanitizer/Sha256, ResortSeeder, migration InitialCreate}`. **CHƯA có** Rules/Faq/Messaging/Housekeeping/Dashboard (task 6–19 còn `[ ]`) → 4 module đó **DỰNG MỚI** trên Bedrock, phần còn lại **PORT** (bỏ hạ tầng tự cuộn, nối port Bedrock).
- **Ports Bedrock xác nhận có** (đọc `Bedrock.Application/Ports`): Time(`IClock`), Users(`ICurrentUser`), Persistence(`IRepository`/`IUnitOfWork`), Security(`IJwtTokenService`/`IPasswordHasher`/`IRefreshTokenStore`/`ITokenGenerator`), Html(`IHtmlSanitizer`), Caching(`IIdempotencyStore`/`IAppCache`), Email/Search/Storage/ExternalAuth. → QR KHÔNG tự cuộn hạ tầng trùng.
- **Host đã tên `StarHill.Api`** trong bản copy; ghép module qua `Add<M>Infrastructure(UseNpgsql(cs))` + `Add<M>Api()`; mỗi module connection string riêng (cùng 1 PostgreSQL, khác schema).
- **NEXT**: chờ user duyệt design (§2/§3/§5/§9). Nếu đồng ý/không phản hồi → **Wave A: ResortConfig** + thêm test project sản phẩm + JournalConsistencyTests cho journal QR (QR-N-003) + CI nhắm `starhill/`.

### QR-N-005 — Wave A phần 1 XONG: cổng anti-drift journal QR (QR-AD-006); còn lại của Wave A
- **Đã xong (verify)**: test project sản phẩm `starhill/tests/StarHill.ArchitectureTests` + `StarHillJournalConsistencyTests` (INV-1..5, tiền tố `QR-`) → đăng ký `Platform.slnx` → `verify.ps1` `Step-Journal` chạy cả 2 journal. `vp build` 0-warning; `vp journal` = 5/5 (QR) + 5/5 (base). Đây là "cách cực mạnh tránh drift" áp cho pha QR: lệch journal QR = FAIL BUILD (không còn review thủ công).
- **QR-N-003 cập nhật**: phần "JournalConsistencyTests QR" ĐÃ xong (QR-AD-006). CÒN LẠI: (a) `starhill/tests/validate_ci.py` vẫn trỏ `parents[2]` về CI base (`.github/workflows/ci.yml`) — cần CI riêng cho sản phẩm StarHill nhắm `starhill/` + sửa validate_ci.py bản copy; (b) `vp ci` của starhill hiện validate CI base, chưa phải CI sản phẩm.
- **NEXT Wave A**: (1) dựng CI sản phẩm nhắm `starhill/` + sửa `starhill/tests/validate_ci.py` (self-contained); (2) module **ResortConfig** (Resort/ResortSettings/ResortLanguage + i18n) — nền feature-flags/ngôn ngữ cho mọi module. Vẫn DESIGN-FIRST cho phần entity/luồng của ResortConfig trước khi code.
- **Governance note**: `verify.ps1` là script cố định của base tooling; thêm logic (chạy 2 project journal) đặt THẲNG vào file này theo đúng quy tắc "logic mới → bỏ vào script cố định", KHÔNG tạo lệnh mới, KHÔNG one-liner ad-hoc. Tên project mới: `StarHill.ArchitectureTests` (chạy qua `vp journal`/`vp test` sẵn có).

### QR-N-006 — Wave A HOÀN TẤT (hạ tầng governance sản phẩm); QR-N-003 đã giải quyết trọn vẹn
- **Wave A xong** (hạ tầng trước khi build module nghiệp vụ):
  - QR-AD-006: cổng anti-drift journal QR (`StarHillJournalConsistencyTests`, INV-1..5) — `vp journal` 5/5 QR + 5/5 base.
  - QR-AD-007: CI sản phẩm `starhill-ci.yml` (nhắm starhill/) + `validate_ci.py` retarget + bất biến "nhắm starhill".
  - Gate tổng: `vp all` = build 0-warning + validate-ci OK + test **252 (235 pass / 17 skip Docker / 0 fail)**.
- **QR-N-003 GIẢI QUYẾT**: (a) JournalConsistencyTests QR ✅ (QR-AD-006); (b) CI sản phẩm + `validate_ci.py` nhắm starhill ✅ (QR-AD-007). Loose-end kỹ thuật của bản copy đã đóng.
- **NEXT — Wave B (module đầu tiên, DESIGN-FIRST)**: module **ResortConfig** (`resort_config` schema): Resort, ResortSettings (1-1, concurrency token), ResortLanguage (unique default), + i18n `ITranslationResolver`/`Translated<T>`. Nguồn port: `resort-qr/src/ResortQr.Domain/Resorts/*` + `ResortQr.Application/Localization/*` (bỏ SharedKernel → Bedrock.Domain; DbContext riêng kế thừa PlatformDbContext khuôn IdentityDbContext). Trước khi code: viết design chi tiết entity/luồng/guard-test (CP5 fallback, CP14 một-default) rồi valid lại.
- **Docker note**: 17 test SKIP (Testcontainers Postgres/RabbitMQ) do máy này không có Docker — KHÔNG phải fail; trên runner CI (ubuntu-latest có Docker) sẽ chạy thật. Khớp pattern base N-067.

### QR-N-007 — Design module ResortConfig xong (Wave B, design-first); chưa code
- **design-modules/01-resortconfig.md** tạo xong, getDiagnostics = 0. Bản đồ đầy đủ: 5-project ref-graph (mirror Identity, đã đọc csproj), 3 entity port SharedKernel→Bedrock (ResortSettings thêm `IHasConcurrencyToken` vì `AuditableEntity` Bedrock KHÔNG mang concurrency — đã đọc Abstractions/AuditableEntity), i18n placement (QR-DV-002), `ResortConfigDbContext`(+Factory) mirror IdentityDbContext(Factory), DB constraints (1-1 settings, partial unique IsDefault=CP14, unique (ResortId,Code)), seeder runtime (QR-AD-008), Contracts (IResortSettingsQuery/IResortLanguageQuery/i18n), CP5/CP14/CP15 guard + nơi test.
- **Slice build (khi triển khai)**: B.1 skeleton (Domain+Contracts+Application+Infrastructure + migration + resolver + CP5 unit) → B.2 query impl + CP14/CP15 integration (Testcontainers) → B.3 Api settings/languages (cần Identity auth + Role→policy QR-AD-005).
- **Verify cần khi code**: tạo test project `ResortConfig.UnitTests`+`ResortConfig.IntegrationTests` (mirror Identity) + đăng ký `Platform.slnx`; thêm bundle migration ResortConfig vào `starhill-ci.yml`; thêm ModuleBoundary arch test cho ResortConfig. Mỗi slice: `vp build` 0-warning + `vp all`/`vp journal` xanh → journal.
- **DESIGN-FIRST tôn trọng**: chưa viết code C# — chờ increment build sau. Thiết kế đã tự-valid (§10) dựa mã đã đọc, không suy đoán.

### QR-N-008 — Slice B.1 ResortConfig XONG (skeleton nền + migration + resolver + CP5)
- **Đã build + verify** (`vp all` xanh: build 0-warning + validate-ci OK + test **262 (245 pass / 17 skip Docker / 0 fail)**):
  - 4 project: `ResortConfig.{Domain, Contracts, Application, Infrastructure}` (Api hoãn tới B.3). Ref-graph khớp khuôn Identity.
  - Domain: `Resort`, `ResortLanguage`, `ResortSettings` (: AuditableEntity, IHasConcurrencyToken → xmin).
  - Contracts: i18n `ITranslation`/`Translated<T>`/`ITranslationResolver` (không marker — QR-DV-002).
  - Application: `TranslationResolver` (port logic resort-qr).
  - Infrastructure: `ResortConfigDbContext`(schema `resort_config`, không outbox — QR-AD-008) + Factory + 3 EF config + `AddResortConfigInfrastructure` (AddBedrockPersistence + AddSingleton resolver).
  - Migration `InitialCreate` (verify: `ux_lang_default` filter `is_default` = CP14; `ux_resort_settings_resort` unique = 1-1; `xmin` xid rowVersion = CP15; check constraints; FK Restrict).
  - Test: `ResortConfig.UnitTests/TranslationResolverTests` = **CP5** (10 case) — thuần, pass mọi máy.
  - `Platform.slnx` +5 project; `starhill-ci.yml` +bundle ResortConfig (deploy artifact).
  - Fix gốc CA1861 (migration generated): `.editorconfig generated_code` (QR-AD-009).
- **CHƯA làm (B.2/B.3)**: Host wiring (connection string `ResortConfig` + AddResortConfigInfrastructure + migrate/seed gated) — CHƯA đụng Host nên smoke test không đổi; `ResortConfigSeeder` (en default) + query impl `EfResortSettingsQuery`/`EfResortLanguageQuery` + CP14/CP15 integration test (Testcontainers); `ResortConfig.Api` settings/languages (cần Identity auth + Role→policy QR-AD-005). ModuleBoundary arch test cho ResortConfig.
- **NEXT increment**: slice B.2 — Host wiring + seeder + query ports + CP14/CP15 integration (design-first cho phần seeder/query nếu cần, rồi code + `vp all`).

### QR-N-009 — Slice B.2 ResortConfig XONG (seeder + Host wiring + CP14/CP15 integration + ModuleBoundary)
- **Đã build + verify** (`vp all` xanh: build 0-warning + validate-ci OK + test **269 (249 pass / 20 skip Docker / 0 fail)**):
  - **Seeder** `ResortConfigSeeder` (idempotent, en default) — seed CHỈ resort/settings/languages, KHÔNG admin (data ownership: admin là việc module Identity). Đăng ký scoped trong `AddResortConfigInfrastructure`.
  - **Host wiring**: `ConnectionStrings:ResortConfig` (appsettings placeholder + smoke factory full) + `AddResortConfigInfrastructure` + migrate/seed gated `Bedrock:ApplyMigrationsOnStartup` (mirror Identity). Host ref `ResortConfig.Infrastructure` (composition root). Smoke test vẫn pass (fail-fast HS256 test dùng placeholder ResortConfig từ appsettings nên vẫn dừng ở HS256 — không lệ thuộc thứ tự).
  - **Integration (Testcontainers, skip nếu thiếu Docker)** `ResortConfigPersistenceTests`: **CP14** (2 default → DbUpdateException do partial unique `ux_lang_default`); **CP15** (2 writer đồng thời ResortSettings → `DbUpdateConcurrencyException` do xmin); **seeder idempotent** (chạy 2 lần → 1 resort/1 settings/4 lang/đúng 1 default=en, Req 12.4).
  - **ModuleBoundary** `ResortConfigBoundaryTests` (StarHill.ArchitectureTests): Contracts thuần; Domain/Application ⊥ Infrastructure; + negative control (CrossModuleInternalLeak giữ ResortConfigDbContext → engine bắt). StarHill.ArchitectureTests +NetArchTest +ref 4 project ResortConfig.
  - `Platform.slnx` +ResortConfig.IntegrationTests.
- **Đổi nhỏ so với base control**: ResortConfig.Contracts KHÔNG có type nào DÙNG `Bedrock.Messaging.Contracts` (i18n thuần) → không dùng được negative-control kiểu Identity ("Contracts phụ thuộc Messaging.Contracts phải bị bắt"); thay bằng control `CrossModuleInternalLeak` (mirror cơ chế base, chứng minh engine không false-pass).
- **HOÃN có chủ đích (I10)**: query ports `IResortSettingsQuery`/`IResortLanguageQuery` → làm khi module tiêu thụ đầu tiên (GuestAccess) định hình DTO thật (tránh API suy đoán). `ResortConfig.Api` (settings/languages endpoints) → slice B.3 (cần Identity auth + Role→policy QR-AD-005).
- **NEXT — Wave B xong; sang Wave C (GuestAccess)** hoặc hoàn tất B.3 (Api settings). Đề xuất: theo dependency graph, GuestAccess (resolve token + visit + portal-window) là nền cho Rules/Concierge/Housekeeping → ưu tiên. DESIGN-FIRST module GuestAccess trước khi code.

### QR-N-010 — Design module Rooms xong (design-first); phát hiện gap nền + sửa thứ tự module
- **design-modules/02-rooms.md** tạo xong, getDiagnostics = 0. Bản đồ: 5-project, entity Room(+IHasConcurrencyToken/ISoftDeletable)/RoomQrToken, use case Create/Update/ChangeStatus/Delete/RotateToken/RenderQrPng (port), QR service QRCoder, EF config (ux_room_number partial, ux_qr_active partial, ux_qrtoken_token), Contracts `IRoomTokenResolver`(cho GuestAccess/Housekeeping)+`IRoomStats`(Dashboard), CP1/CP2 guard, build slices B-Rooms.0..3.
- **SỬA drift thứ tự (QR-AD-011)**: module kế tiếp là **Rooms** (KHÔNG phải GuestAccess) — GuestAccess.resolve phụ thuộc Rooms (dependency graph). README đã sửa.
- **Phát hiện GAP NỀN (QR-AD-010)**: base thiếu dịch unique-violation (grep 0 match). Sẽ bổ sung `UniqueConstraintViolationException` + EfUnitOfWork dịch ở **platform/** (base, QR-N-002) rồi copy sang starhill/ — đây là increment đụng base ĐẦU TIÊN của pha QR, làm cẩn thận (verify base trước/sau + copy).
- **Deviations (QR-DV-003)**: Rooms bỏ FK chéo-schema Room→Resort (ResortId Guid trần) + đọc GuestWebBaseUrl qua `ResortConfig.Contracts.IResortSettingsQuery` (kích hoạt query port đã hoãn — consumer thật).
- **VERIFY khi impl (chưa chắc, phải đọc lúc code — KHÔNG bịa)**: (1) Bedrock.Infrastructure có ref Npgsql cho `PostgresException`? (PlatformDbContext dùng Database.IsNpgsql() → gần như chắc, nhưng verify csproj); (2) tên method `ITokenGenerator` của Bedrock; (3) cách đăng ký use case (Identity đăng ký THỦ CÔNG → Rooms cũng thủ công trong AddRoomsInfrastructure, không dựa marker auto-scan); (4) QRCoder version từ resort-qr/Directory.Packages.props.
- **NEXT increment**: slice **B-Rooms.0** — thêm năng lực nền unique-violation ở platform/ (design-first phần này đã đủ; code + guard test + verify `vp` base) → copy sang starhill → verify `vp all`. Rồi B-Rooms.1 (query port ResortConfig) → B-Rooms.2 (Rooms module).

### QR-N-011 — Slice B-Rooms.0 XONG: năng lực nền unique-violation (đụng base + copy) — increment đụng base ĐẦU TIÊN
- **Bổ sung ở base `platform/`** (QR-AD-010 → base AD-069): `Bedrock.Domain/Results/UniqueConstraintViolationException.cs` (mang ConstraintName) + `EfUnitOfWork.SaveChangesAsync` dịch `DbUpdateException` inner Postgres 23505 → exception trung lập (catch `DbUpdateConcurrencyException` con TRƯỚC; filter Postgres-only nên SQLite giữ DbUpdateException) + `ExceptionHandlingMiddleware` map 409 (`CommonErrors.Conflict()` — không thêm mã, không đụng CP12 snapshot). Cập nhật test Postgres `RefreshTokenRotationRaceTests` (DbUpdateException→UniqueConstraintViolationException). Base journal +AD-069 (01 + 05 guard map).
- **Verify base TRƯỚC/SAU**: `platform\scripts\vp.cmd all` → build 0-warning + validate-ci + **247 test (230 pass / 17 skip / 0 fail)** + JournalConsistency (AD-069 INV-1/INV-2 pass). Base vẫn sạch/0-fail.
- **Copy sang `starhill/`** (4 file y hệt: exception mới + EfUnitOfWork + ExceptionHandlingMiddleware + RefreshTokenRotationRaceTests) → `starhill vp all` → build 0-warning + **269 test (249 pass / 20 skip / 0 fail)**. Hai bản base đồng bộ.
- **Bản chất tuân thủ**: đây là increment ĐỤNG BASE đầu tiên của pha QR — làm đúng QR-N-002 (năng lực nền, không nghiệp vụ QR) + verify base trước/sau + đồng bộ 2 bản. `Bedrock.Infrastructure` đã ref Npgsql (xác minh csproj: `Npgsql.EntityFrameworkCore.PostgreSQL`) → detect PostgresException hợp lệ.
- **Đã VERIFY (mục design §10 checklist)**: (1) Npgsql ref ✅; (2) `PostgresErrorCodes.UniqueViolation` + `PostgresException.ConstraintName`/`SqlState` biên dịch OK (build 0-warning) ✅. Còn lại verify khi B-Rooms.2: tên method `ITokenGenerator`, cách đăng ký use case, version QRCoder.
- **NEXT — slice B-Rooms.1**: ResortConfig `IResortSettingsQuery` (Contracts) + `EfResortSettingsQuery` (Infra) + đăng ký + test đọc settings (consumer là Rooms.RenderQrPng). Rồi B-Rooms.2 (Rooms module).

### QR-N-012 — Slice B-Rooms.1 XONG: `IResortSettingsQuery` (ResortConfig) — read-surface cho module hạ nguồn
- **Đã build + verify** (`vp all`: build 0-warning + validate-ci + **271 test (251 pass / 20 skip / 0 fail)**):
  - `ResortConfig.Contracts/Queries/IResortSettingsQuery.cs` + record `ResortSettingsSnapshot` (DTO thuần, KHÔNG marker DI — như i18n QR-DV-002). Trả **trọn snapshot** settings (không chỉ GuestWebBaseUrl): đọc cohesive của aggregate; consumer đầu Rooms.RenderQrPng dùng GuestWebBaseUrl, GuestAccess/Rules/Faq/Housekeeping (sau) dùng portal-window/idle/flags/rate-limit → không phải speculative, là read tự nhiên của settings.
  - `EfResortSettingsQuery` (Infra, AsNoTracking, single-resort, map entity→snapshot) + đăng ký scoped trong `AddResortConfigInfrastructure`.
  - Test `ResortSettingsQueryTests` (**SQLite in-memory → CHẠY CỤC BỘ**, không Docker): GetAsync null-khi-chưa-seed + snapshot đúng default sau seed (flags/30'/24h/base-url-null). +2 test pass local (271 total).
- **Chọn SQLite cho test query** (không Testcontainers): query chỉ đọc (không partial-index/xmin) → SQLite phản ánh đúng hành vi map + cho **proof cục bộ mạnh** (khác CP14/CP15 buộc Postgres). Thêm `Microsoft.EntityFrameworkCore.Sqlite` vào ResortConfig.IntegrationTests.
- **NEXT — slice B-Rooms.2**: module Rooms (Domain/Contracts/Application/Infrastructure) — port use case + QRCoder + migration schema `rooms` (ux_room_number/ux_qr_active/ux_qrtoken_token) + CP1(resolver)/CP2(1-active-token) integration + ModuleBoundary. VERIFY lúc code: tên method `ITokenGenerator`, cách đăng ký use case (thủ công), version QRCoder (từ resort-qr).

### QR-N-013 — Slice B-Rooms.2a XONG: Rooms persistence + resolver (Domain/Contracts/Infrastructure)
- **Đã build + verify** (`vp all`: build 0-warning + validate-ci + **282 test (261 pass / 21 skip / 0 fail)**):
  - **Rooms.Domain**: Room (AuditableEntity+ISoftDeletable+IHasConcurrencyToken; ResortId Guid trần — QR-DV-003), RoomQrToken, RoomStatus, RoomQrTokenStatus.
  - **Rooms.Contracts**: `IRoomTokenResolver` + `RoomResolution` (bool IsRoomActive — không lộ enum Domain). Surface cho GuestAccess/Housekeeping.
  - **Rooms.Infrastructure**: `RoomsDbContext` (schema `rooms`, không outbox) + Factory + EF config (ux_qrtoken_token, ux_qr_active provider-agnostic; ux_room_number partial Npgsql-only ở DbContext) + `EfRoomTokenResolver` + `AddRoomsInfrastructure` + migration InitialCreate (verify: 3 index đúng filter, FK nội-module room_qr_token→room, KHÔNG FK chéo-schema).
  - Host wiring (conn `Rooms` + AddRoomsInfrastructure + migrate) + smoke factory. `Platform.slnx` +4 project.
  - **Tests**: `RoomsPersistenceTests` (SQLite **local**): **CP2** (1 token Active/phòng — ux_qr_active; revoke giải phóng slot; token unique toàn cục) + **CP1** resolver (token Active→phòng; token lạ/revoked/phòng-xóa-mềm→null; phòng Inactive→IsRoomActive=false) — 7 test pass local. `RoomsPostgresConstraintTests` (Testcontainers, skip local): ux_room_number partial (unique khi sống + tái dùng sau soft-delete). `RoomsBoundaryTests` (StarHill.ArchitectureTests): Contracts thuần + Domain⊥Infra + negative control.
- **Fix gốc 1 test fail**: `Resolver_returns_null_when_room_soft_deleted` ban đầu Remove(room) trong CÙNG scope đang track token con → EF cascade-delete child (FK required) trước khi interceptor soft-delete. Bản chất: TEST tự track con sai pattern (DeleteRoomUseCase thật load room một mình). Fix: soft-delete trong scope riêng chỉ load room. KHÔNG phải lỗi design (soft-delete + FK Restrict đúng).
- **NEXT — slice B-Rooms.2b**: Rooms.Application (use case Create/Update/ChangeStatus/Delete/RotateToken/RenderQrPng) — VERIFY lúc code: tên method `ITokenGenerator`, Result API Bedrock (Success/Failure — AD-002), bắt `UniqueConstraintViolationException` (AD-069) map RoomsErrors, `IResortSettingsQuery` cho GuestWebBaseUrl, QRCoder version (từ resort-qr). Rồi B-Rooms.3 (Rooms.Api, cần Identity auth).

### QR-N-014 — Slice B-Rooms.2b-i XONG: Rooms.Application use case CRUD + RotateToken (port SharedKernel→Bedrock)
- **Đã build + verify** (`vp all`: build 0-warning + validate-ci + **292 test (270 pass / 22 skip Docker / 0 fail)**):
  - **Rooms.Application** (+`Platform.slnx`): `RoomsErrors` (RoomNumberTaken=validation_error, QrGenerationFailed=Conflict, RoomNotFound=NotFound — hoãn InvalidConfiguration sang 2b-ii, I10); `RoomContracts` (CreateRoomInput[+ResortId — QR-DV-004]/CreateRoomResult/RotateRoomTokenInput/Result/UpdateRoomInput/ChangeRoomStatusInput; hoãn RoomListItem+RenderQrPng DTO); `RoomTokenFactory` (internal: 5-retry `ITokenGenerator.NewToken()` + Mask 6 ký tự+"…"); 5 use case + 2 validator.
  - **Use case** (port `Result.Ok/Fail`→`Result.Success/Failure` — AD-002; inject `IRepository<T>` TRỰC TIẾP — DV-002; `IDateTimeProvider`→`IClock`): `CreateRoomUseCase`(IUseCase, bắt `UniqueConstraintViolationException` phân biệt `ConstraintName=="ux_room_number"`→RoomNumberTaken else QrGenerationFailed), `RotateRoomTokenUseCase`(IUseCase, revoke+insert nguyên tử, Version++, race ux_qr_active→QrGenerationFailed), `UpdateRoomUseCase`/`ChangeRoomStatusUseCase`/`DeleteRoomUseCase`(ICommandUseCase; Delete soft qua Remove).
  - **Đăng ký THỦ CÔNG** trong `AddRoomsInfrastructure` (mirror Identity — Bedrock KHÔNG auto-scan use case module): 5 `AddScoped<IUseCase<..>/ICommandUseCase<..>>` + 2 `AddTransient<IValidator<..>>`. `Rooms.Infrastructure.csproj` đổi ref Domain+Contracts→**Rooms.Application** (kế thừa; matrix Infra→Application) +FluentValidation.
  - **Tests**: `RoomsUseCaseTests` (SQLite **local**, 10 test): Create→1 token Active/Version=1/actor-audit; Rotate→revoke cũ+Active mới+Version++/giữ lịch sử; Rotate not-found; Rotate phòng-Inactive→qr_generation_failed; Update+not-found; ChangeStatus+not-found; Delete-soft giữ token+not-found. `RoomsPostgresConstraintTests` +1 (Testcontainers, skip local): CreateRoom trùng số phòng → chuỗi dịch 23505→UniqueConstraintViolationException(ux_room_number)→RoomNumberTaken đầu-cuối. `RoomsBoundaryTests` +1: **Rooms.Application ⊥ Infrastructure/EF/ASP.NET** (I7 — use case bắt exception trung lập, không DbUpdateException).
  - **VERIFY đã chốt (design §4/§10 checklist)**: `ITokenGenerator.NewToken(int=32)` base64url ✅; Result API `Success/Failure` + implicit operators ✅; `IUseCase`/`ICommandUseCase` trả `Task<Result<T>>`/`Task<Result>` ✅; đăng ký thủ công như Identity ✅; `UniqueConstraintViolationException.ConstraintName` phân biệt ràng buộc ✅. CÒN cho 2b-ii: QRCoder version (1.8.0 từ resort-qr — thêm CPM khi build RenderQrPng), `IResortSettingsQuery` cho GuestWebBaseUrl.
  - **Lý do test SQLite-local cho use case** (không Testcontainers): logic use case (retry token, revoke+insert, soft-delete, not-found, Version++) provider-agnostic → SQLite cho proof CỤC BỘ mạnh mọi máy. CHỈ mapping trùng-số-phòng→RoomNumberTaken cần Npgsql (UniqueConstraintViolationException dịch từ SqlState 23505 — SQLite cho DbUpdateException trần) → đặt Testcontainers (skip w/o Docker, chạy CI).
- **NEXT — slice B-Rooms.2b-ii**: `RenderRoomQrPngUseCase` + `IQrService`/`QrCoderQrService` (QRCoder 1.8.0 CPM) + đọc GuestWebBaseUrl qua `IResortSettingsQuery` (kích hoạt Rooms.Application→ResortConfig.Contracts) + validate https + URL `{base}/r/{token}`. Rồi B-Rooms.3 (Rooms.Api, cần Identity auth + Role→policy QR-AD-005).

### QR-N-015 — Slice B-Rooms.2b-ii XONG: RenderRoomQrPng + IQrService/QrCoderQrService (hoàn tất Rooms use case)
- **Đã build + verify** (`vp all`: build 0-warning + validate-ci + **299 test (277 pass / 22 skip Docker / 0 fail)**):
  - **QRCoder 1.8.0** thêm vào `starhill/Directory.Packages.props` (verify từ `resort-qr/Directory.Packages.props` — KHÔNG bịa) + `PackageReference` ở `Rooms.Infrastructure.csproj`.
  - **`IQrService`** (Rooms.Application, port `byte[] RenderPng(string url)` — BỎ marker `ISingletonService` của resort-qr, đăng ký thủ công) + **`QrCoderQrService`** (Rooms.Infrastructure, QRCoder `PngByteQRCode` ECC Q, 20px — PNG THUẦN MANAGED, không System.Drawing/SkiaSharp → chạy cục bộ + CI Linux). Đăng ký `AddSingleton<IQrService, QrCoderQrService>()`.
  - **`RenderRoomQrPngUseCase`** (`IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>`, scoped): thứ tự kiểm phòng tồn tại→Active→cấu hình https hợp lệ→có token Active→render. URL = `{GuestWebBaseUrl}/r/{token}` (CHỈ URL, KHÔNG số phòng trần — CP1/Req 1.6). **QR-DV-003**: đọc `GuestWebBaseUrl` qua **`IResortSettingsQuery.GetAsync()`** (Contracts, cross-module — KHÔNG repo `ResortSettings` chéo-schema như resort-qr) → kích hoạt `Rooms.Application`→`ResortConfig.Contracts` (consumer thật của query port hoãn từ B-Rooms.1). Inject `IRepository<T>` trực tiếp (DV-002). `RoomsErrors.InvalidConfiguration` (hoãn từ 2b-i) nay dùng.
  - **Đăng ký** `RenderRoomQrPngUseCase` scoped trong `AddRoomsInfrastructure`. Phụ thuộc `IResortSettingsQuery` (đăng ký ở `AddResortConfigInfrastructure`) — Host ráp cả 2 module nên có sẵn ở composition root; DI order-independent (verify: Host `vp all` smoke pass — StartupValidator vẫn dừng đúng ở HS256, không lỗi DI thiếu đăng ký).
  - **Tests**: `RenderRoomQrPngUseCaseTests` (SQLite **local** + QRCoder thật, 7 test): render PNG hợp lệ (chữ ký `89 50 4E 47`) cho phòng Active+https; invalid baseurl (null/http/not-a-url → invalid_configuration, Theory 3 case); phòng Inactive → qr_generation_failed; phòng lạ → not_found; phòng Active không còn token Active → qr_generation_failed. **`IResortSettingsQuery` STUB** (cô lập module — impl EF thật đã test ở ResortConfig QR-N-012; use case chỉ phụ thuộc Contracts nên stub hợp lệ, tránh phải wire ResortConfigDbContext vào test Rooms). `RoomsBoundaryTests.Application_should_not_depend...` +assertion: `Rooms.Application` ⊥ `ResortConfig.{Domain,Application,Infrastructure}` (CHỈ `.Contracts` — QR-DV-003/QR-AD-002).
  - **Lý do render test SQLite-local** (không Testcontainers): logic render (URL build, https-validate, thứ tự nhánh lỗi) + QRCoder PNG đều provider-agnostic/thuần managed → proof CỤC BỘ mạnh mọi máy. Không có nhánh nào cần Postgres-specific.
- **Rooms use case HOÀN TẤT** (2b-i CRUD/rotate + 2b-ii render). Design §10 checklist: tất cả VERIFY item đã ✅ (Npgsql ref, ITokenGenerator, đăng ký thủ công, QRCoder 1.8.0).
- **NEXT — slice B-Rooms.3**: `Rooms.Api` (admin CRUD Admin-only/read Staff + `qr.png` endpoint trả PNG + rotate-token) — cần Identity auth + Role→policy (QR-AD-005). Caller Api phân giải resortId single-resort (QR-DV-004) qua `IResortSettingsQuery`/claim trước khi gọi CreateRoom. Gộp với slice Identity-auth. Hoặc chuyển sang module kế (GuestAccess) tùy ưu tiên dependency graph.

### QR-N-016 — P0 REMEDIATION XONG: D1-a + keyed persistence + 1-DB + compose boot (verify Docker end-to-end)
- **Bối cảnh**: review phát hiện 3 P0 catastrophic ở `starhill/` (bản vendored cũ). Verify lại bằng đọc file thật (không tin báo cáo mù). Fix theo design-first + verify-then-implement, user duyệt D1-a.
- **P0-1 (persistence unkeyed → last-registration-wins)**: FIX bằng D1-a (QR-AD-012, xóa 12 project base-copy, ref `platform/src` có keyed API) + keyed conversion 3 module (QR-AD-013). Compile-enforce (base AD-098 `ICommandUseCase:ITransactionalUseCase`) bắt 3 void command Rooms thiếu `PersistenceKey` ngay lúc build → xác nhận gap thật. Sau fix: Host boot `WebApplicationFactory` (ValidateOnBuild=true) PASS.
- **P0-2 (3 DB vật lý lệch design)**: FIX 1-DB-nhiều-schema (QR-AD-014) — appsettings gộp về `Database=starhill`; regenerate migration Identity khớp base mới (base đổi schema outbox/inbox/refresh → `PendingModelChangesWarning`).
- **P0-3 (compose không boot)**: FIX (QR-AD-015) — compose 1-DB + override 3 connection; Docker context repo-root (hệ quả D1-a) + `.dockerignore` root; RabbitMQ healthcheck `check_port_connectivity` (hết race Connection-refused → StopHost).
- **VERIFY (Docker THẬT, máy này có Docker)**: (1) `dotnet build Platform.slnx` 0-warning/0-error. (2) `dotnet test Platform.slnx` = **66 test, 0 fail, 0 skip** (8 project nghiệp vụ: ContractTests 2, Identity.Unit 7, ResortConfig.Unit 10, Identity.Integration 2, StarHill.Architecture 13 [gồm journal QR], ResortConfig.Integration 5, Rooms.Integration 24, StarHill.Api.Tests 3 [Host boot]). (3) `docker compose up` → 3 container Up, log `CREATE SCHEMA identity/resort_config/rooms` trong CÙNG DB `starhill`, `/health/ready`=200 + `/health/live`=200.
- **Governance cập nhật theo D1-a**: `verify.ps1` Step-Journal chỉ gác journal QR (base do platform/ gác — Bedrock.ArchitectureTests không còn ở starhill); `starhill-ci.yml` docker-image build context repo-root; comment build-test đính chính (base test chạy ở CI base).
- **Test Identity.IntegrationTests thích ứng keyed**: resolve `IRefreshTokenStore`/`IUnitOfWork`/`IOutboxWriter` bằng `GetRequiredKeyedService<T>(IdentityInfrastructureExtensions.PersistenceKey)` — cascade hợp lệ của keyed base.
- **CÒN LẠI (defer có chủ đích)**: (a) resilience broker-restart (consumer StopHost khi RabbitMQ restart) — thuộc base platform/ (mirror platform N-079), ngoài phạm vi P0 starhill; (b) migration bundle Rooms trong `starhill-ci.yml` (hiện chỉ Identity+ResortConfig — Rooms có migration, nên thêm khi hoàn tất deploy pipeline); (c) P1 cũ (ResortId invariant qua query port, default-language nguyên tử, Dashboard shape) — sau P0.
- **NEXT**: P1 hoặc slice B-Rooms.3 (Rooms.Api + Identity auth) tùy ưu tiên. Nền đã vững: keyed base một-nguồn (drift bất khả thi), compose boot 3 module xác minh.


### QR-N-017 — P1(a) XONG: CreateRoom thẩm định Resort + tạo project `Rooms.UnitTests`
- Ngày 2026-07-13 (máy toann — KHÔNG Docker; verify bằng unit test thuần).
- **P1(a)** (QR-AD-016): `CreateRoomUseCase` giờ kiểm `IResortExistenceQuery.ExistsAsync(input.ResortId)` TRƯỚC khi tạo → resort ma → `RoomsErrors.ResortNotFound` (code `resort_not_found`). Port `IResortExistenceQuery` + impl `EfResortExistenceQuery` (AnyAsync) + đăng ký scoped ở `AddResortConfigInfrastructure`. Factory DI Rooms truyền thêm `IResortExistenceQuery` (Host ráp cả 2 module nên có sẵn).
- **Cấu trúc MỚI cần biết:** tạo project test `starhill/tests/Modules/Rooms.UnitTests` (trước đây Rooms KHÔNG có unit test — chỉ IntegrationTests SQLite/Docker). Đã thêm vào `starhill/Platform.slnx`. Chứa `CreateRoomUseCaseTests` (2 test fake-port, chạy mọi máy). KHI thêm use case Rooms mới → ưu tiên unit test ở đây (không cần Docker) trước integration.
- **Fallout đã xử lý:** đổi ctor `CreateRoomUseCase` (+1 dep) làm 3 harness `Rooms.IntegrationTests` (RoomsUseCaseTests/RenderRoomQrPngUseCaseTests/RoomsPostgresConstraintTests) thiếu `IResortExistenceQuery` → thêm stub dùng chung `TestResortExistenceQuery` (trả true). `RoomsPersistenceTests` không đụng (dùng DbContext trực tiếp, không resolve CreateRoom).
- Verify: `starhill scripts\vp.cmd all` = build 0-warning + validate-ci + full suite 0-fail (Rooms.UnitTests 2/2, Rooms.IntegrationTests 22 pass/2 skip-Docker); `vp journal` QR INV-1..5 = 5/5.
- CÒN LẠI P1 (review): (b) default-language nguyên tử ở ResortConfig aggregate; (c) Dashboard shape (QR-TO-003). Xem end.md §6.


### QR-N-018 — DEFER (cần Docker): set-default-language nguyên tử trên Npgsql khi viết use case
- Ngày 2026-07-13 (máy toann — không Docker).
- P1(b) đã đặt bất biến + primitive `ResortLanguagePolicy.TrySetDefault` (in-memory nguyên tử) + guard (QR-AD-017). NHƯNG khi viết use case `SetDefaultLanguage` thật (kèm admin API slice B.3), phải xử lý sắc thái Npgsql:
  - `TrySetDefault` set target.IsDefault=true + gỡ default cũ, rồi use case `SaveChangesAsync` MỘT lần. Trên Npgsql, EF phát nhiều câu UPDATE THỨ TỰ KHÔNG đảm bảo → có thể set target=true TRƯỚC khi unset default cũ → trạng thái 2-default TẠM THỜI → vi phạm `ux_lang_default` (partial-unique index, NON-deferrable, kiểm ngay từng-hàng, KHÔNG hoãn tới commit).
  - Hướng xử lý (chọn khi có Docker để KIỂM CHỨNG, không đoán): (a) một câu SQL `UPDATE ... SET is_default = (code = @target)` cho cả tập (nguyên tử, không trạng thái trung gian); hoặc (b) unset-tất-cả rồi set-target bằng 2 ExecuteUpdate tuần tự trong 1 transaction; hoặc (c) đổi index thành CONSTRAINT DEFERRABLE INITIALLY DEFERRED (lưu ý: partial-unique bắt buộc là INDEX, không defer được → loại (c) trừ khi bỏ partial).
  - KHÔNG merge use case set-default nếu chưa có e2e Postgres (Testcontainers) chứng minh không vi phạm index giữa-transaction.
- Máy hiện tại KHÔNG Docker → chỉ làm được phần thuần (đã làm). Ghi để KHÔNG quên + KHÔNG bịa "đã fix Npgsql".


### QR-N-019 — Thêm Rooms vào migration-bundle CI (đóng gap deploy pipeline §6.2)
- Ngày 2026-07-13 (máy toann — verify EMPIRICAL không cần Docker).
- **Gap:** `starhill-ci.yml` job `migration-bundle` có Identity + ResortConfig bundle nhưng THIẾU Rooms (comment còn ghi "Rooms → thêm bundle tương ứng"). Rooms có migrations (`20260711133837_InitialCreate`: ux_room_number/ux_qr_active/ux_qrtoken_token) + `RoomsDbContextFactory` (IDesignTimeDbContextFactory parameterless) → schema `rooms` KHÔNG được deploy qua pipeline bundle out-of-band (AD-050/055: áp migration TRƯỚC rollout, không auto-migrate trong app) → deploy thiếu schema.
- **Fix:** thêm 2 bước vào job `migration-bundle`: `dotnet ef migrations bundle --project/--startup-project src/Modules/Rooms/Rooms.Infrastructure --self-contained -r linux-x64 -o efbundle-rooms` + upload artifact `starhill-efbundle-rooms` (mirror Identity/ResortConfig).
- **Verify EMPIRICAL (không cần Docker — bundle chỉ compile+publish, connection dummy):** chạy chính lệnh C:
  `dotnet ef migrations bundle --project ... Rooms.Infrastructure --self-contained -r linux-x64 -o efbundle-rooms-verify --force` → "Build succeeded. Building bundle... Done." (exit 0). Artifact tạo được (đã xoá; `efbundle*` gitignored). `vp ci` validate-ci vẫn OK sau sửa yaml.
- KHÔNG phải quyết định thiết kế mới (chỉ hoàn tất pattern đã dự liệu) → ghi N, không AD. Deploy runbook: chạy 3 bundle (identity/resortconfig/rooms) TRƯỚC rollout Host.

### QR-N-020 — B-Rooms.3 XONG: Rooms.Api slice + StarHill.Authorization + string-enum JSON
- Date: 2026-07-13
- Slice B-Rooms.3 hoàn tất (design-first: `design-B-Rooms.3-rooms-api.md` → user duyệt 4 Open Question → code). Tạo `Rooms.Api` (6 endpoint `/v1/rooms`, role Req 7.6) + `StarHill.Authorization` (policy Admin/Staff superset) + bật `JsonStringEnumConverter` toàn cục ở Host. Guard KHÔNG Docker: `StarHillAuthorizationPolicyTests` (6, CP8 superset) + `RoomsEndpointAuthTests` (6, route+policy+qr.png content-type qua fake use case). `vp all` = build 0-warning + validate-ci OK + StarHill.Api.Tests 15/15, toàn suite 0-fail (skip = Docker-only). Journal: QR-AD-019/020/021 + guard map cập nhật (QR-AD-005 Role→policy CP8 và QR-DV-001 route `/v1/<group>` chuyển ⏳→✅).
- CÒN LẠI cho Rooms: GET list/detail phòng (cần read-model `RoomListItem` — slice query sau, I10); bulk QR (cần list trước).


### QR-N-021 — B-Rooms.4 XONG: Rooms query slice (GET list + detail)
- Date: 2026-07-13
- Hoàn tất Req 7.6 "Staff xem danh sách/thông tin phòng". `IRoomQueries`+`RoomListItem` (Rooms.Application) + `EfRoomQueries` (Infra, 2-query map in-memory) + GET `/v1/rooms` (list, phân trang PagedRequest/PagedResult + lọc status) & GET `/v1/rooms/{id}` (detail 404) — RequireStaff (Admin superset). Guard KHÔNG Docker: `RoomQueriesTests` (3, SQLite: preview token Active/loại soft-delete/filter status/phân trang/GetById null) + `RoomsEndpointAuthTests` +4 (Staff+Admin xem list/detail=200, unknown=404, no-token=401). `vp all` xanh: StarHill.Api.Tests 19/19, Rooms.IntegrationTests 25 pass/2 skip(Docker). Journal QR-AD-022.
- CÒN LẠI Rooms: token history endpoint (nếu cần), bulk QR in hàng loạt. Module kế: GuestAccess (dùng IRoomTokenResolver).


### QR-N-022 — B-Config.3 XONG: ResortConfig settings Api (admin xem/sửa cấu hình)
- Date: 2026-07-13
- Thiết lập write-path ĐẦU TIÊN cho ResortConfig. `UpdateResortSettingsUseCase`+validator (Application, mirror Rooms keyed repo+pipeline) + `Ef/AddBedrockRepository<ResortSettings>` + `ResortConfig.Api` (GET+PUT `/v1/resort/settings`, RequireAdmin). Đóng mắt xích: admin nay cấu hình được `GuestWebBaseUrl` mà qr.png cần. Guard KHÔNG Docker: `ResortConfigEndpointAuthTests` (3: Admin 200/204, Staff 403, no-token 401) + `ResortConfigSettingsUseCaseTests` (7: update đổi field/not-found + validator https/range). `vp all` xanh: StarHill.Api.Tests 22/22, ResortConfig.IntegrationTests 9 pass/3 skip(Docker). Journal QR-AD-023.
- Module admin còn lại: Faq/Rules/Concierge/Housekeeping (mirror khuôn write-path). Module guest-flow: GuestAccess (dùng IRoomTokenResolver).


### QR-N-023 — C-GA.0 đối soát + design GuestAccess hoàn tất, chưa code
- Date: 2026-07-14
- Source active đã kiểm: `starhill/src/Modules` chỉ Identity/ResortConfig/Rooms; GuestAccess chưa tồn tại. Source port hợp lệ là root `resort-qr/` commit `c7622a5`; nested stale copy tuyệt đối không dùng.
- Task 5 `[x]/[~]` ở `docs/resort-qr-portal/tasks.md` khớp legacy ResolveTokenUseCase/tests, không phải trạng thái active Bedrock port (QR-DV-005).
- Đã đọc contract thật: `IRoomTokenResolver` gộp unknown/revoked thành null nhưng giữ inactive bool; `IResortSettingsQuery` thiếu presentation/language. Đã đọc `TransactionUseCaseDecorator`, `EfUnitOfWork`, `IUnitOfWork`: PostgreSQL 23505 trong explicit transaction không cho phép legacy catch-query cùng context.
- Design mới: `design-modules/03-guestaccess.md`; chốt module/schema/key, guest config query, SHA-256 cookie hash, `__Host-` cookie, POST-body resolve, session row lock, portal check-before-touch, PostgreSQL race guards, sweeper và outbox cascade deferred.
- Không sửa C#. Validation C-GA.0: diagnostics các spec/journal = 0; `StarHillJournalConsistencyTests` INV-1..5 = **5/5 pass, 0 fail/skip** (targeted `dotnet test`, build thành công). Docker engine chưa được dùng; C-GA.1/2 phải có Docker Server trước bằng chứng PostgreSQL cuối.
- NEXT sau review: C-GA.1 Domain/Contracts/Persistence; không dispatch từ legacy tasks.md vì `.kiro/specs/starhill-qr/tasks.md` không tồn tại.


### QR-N-024 — C-GA.1 XONG: GuestAccess persistence nền (Domain/Contracts/Infrastructure + migration + Postgres constraint)
- Date: 2026-07-14
- Đã build + verify (Docker THẬT, máy này Docker Server 29.5.2):
  - 3 project mới: `GuestAccess.{Domain,Contracts,Infrastructure}` (mirror khuôn Rooms/ResortConfig). Domain: `GuestSession`/`GuestVisit`/`GuestVisitStatus` (Entity base; ResortId/RoomId Guid trần — không FK chéo-schema). Contracts: `GuestAccessModule.PersistenceKey = "guest_access"`.
  - Infrastructure: `GuestAccessDbContext` (schema `guest_access`, không outbox) + Factory design-time + `GuestAccessConfigurations` (ux_guest_session_key_hash; ux_guest_visit_active partial `status='Active'`; ix_guest_visit_active_expiry partial; 2 check `ck_guest_visit_expiry_after_seen`/`ck_guest_visit_closed_at`; FK guest_session_id→guest_session Restrict) + `AddGuestAccessInfrastructure` (CHỈ keyed persistence — resolve/hasher/repo ở C-GA.2).
  - Migration `20260714031018_InitialCreate` (verify Up: EnsureSchema + 2 bảng + đúng index/filter/check/FK). `.editorconfig generated_code` che analyzer cho composite index (QR-AD-009).
  - Test `GuestAccessPostgresConstraintTests` (Testcontainers, 4 test): unique hash; 1 Active/(session,room) + tái dùng sau close; check expiry<last_seen bị chặn; check Active+closed_at bị chặn. `GuestAccessBoundaryTests` (StarHill.ArchitectureTests, 3): Contracts thuần; Domain⊥Infra; negative control.
  - `Platform.slnx` +3 project src +1 test; StarHill.ArchitectureTests +3 ref GuestAccess.
- Gate: `dotnet build Platform.slnx` = **0 warning/0 error**; `dotnet test Platform.slnx` = **114 test, 0 fail, 0 skip** (Docker) — trong đó GuestAccess.IntegrationTests 4/4 chạy THẬT (không skip), StarHill.ArchitectureTests 16/16 (gồm journal INV-1..5 + 3 boundary GuestAccess).
- Tuân thủ slice boundary design (C-GA.1 chỉ persistence): CHƯA Host wiring/connection string/compose (C-GA.3), CHƯA resolve/hasher/Application (C-GA.2). Đúng fidelity design↔code = chống drift.
- NEXT — C-GA.2: `GuestAccess.Application` (resolve use case transactional keyed) + `IResortGuestConfigQuery` (ResortConfig.Contracts + Infra) + `IGuestSessionKeyHasher`/SHA-256 + session-row-lock store (FOR UPDATE) + unit + Postgres race test (nhiều request cùng cookie+room → một Active/cùng VisitId).


### QR-N-025 — C-GA.2a XONG: `IResortGuestConfigQuery` (ResortConfig) — read-surface guest-facing cho GuestAccess.resolve
- Date: 2026-07-14
- Đã build + verify (build 0-warning; ResortConfig.IntegrationTests **15 test, 0 fail, 0 skip** — +3 test mới, SQLite local không Docker):
  - `ResortConfig.Contracts/Queries/IResortGuestConfigQuery.cs` + record `ResortGuestConfig` (ResortId/Name/LogoUrl + EnabledLanguageCodes[SortOrder] + DefaultLanguageCode + 6 flags + PortalWindowMinutes/VisitIdleExpiryHours). DTO thuần, KHÔNG marker DI (mirror IResortSettingsQuery/QR-DV-002).
  - `EfResortGuestConfigQuery` (Infra, AsNoTracking): gộp Resort+ResortSettings+ResortLanguage(enabled). **FAIL-CLOSED (QR-AD-024)**: thiếu resort / thiếu settings / không có default-language hợp lệ → `null` (consumer map `configuration_unavailable`, KHÔNG đoán default). Đăng ký scoped ở `AddResortConfigInfrastructure`.
  - Test `ResortGuestConfigQueryTests` (SQLite local): null khi chưa seed; config đúng sau seed (en default, en/vi/ko/zh enabled, flags, 30/24); null khi resortId mismatch (không "khớp ngầm" resort đơn — mở đường multi-resort + cho resolve kiểm ResortId khớp phòng).
- Lý do tách query riêng (không mở rộng `IResortSettingsQuery`): settings query đang phục vụ Rooms.qr (chỉ settings vận hành); guest config cần thêm tên/logo/ngôn ngữ — gộp vào một DTO đa-trách-nhiệm sẽ bẩn surface. Query theo resortId (không single-resort ngầm) để đúng lâu dài + cho phép kiểm khớp `room.ResortId`.
- Bằng chứng cho C-GA.2b (đã đọc mã, không suy đoán): row-lock dùng `FromSqlRaw("... FOR UPDATE", ...)` — base có precedent `EfOutboxDispatcher` (FOR UPDATE SKIP LOCKED) + `EfInboxStore` (ExecuteSqlRawAsync). Resolve = `ICommandUseCase<In,Out>` (transactional keyed) → decorator mở một transaction; lock trong transaction đó.
- NEXT — C-GA.2b: GuestAccess.Application (hasher SHA-256 + GuestAccessErrors {qr_invalid, room_inactive, configuration_unavailable} + ResolveTokenUseCase + IGuestSessionStore) + Infra (store FOR UPDATE, hasher, factory) + unit SQLite + Postgres race. LƯU Ý: thêm GuestAccessErrors → phải cập nhật ErrorCodeSnapshot (đọc ErrorCodeSnapshotTests trước khi code, tránh vỡ snapshot QR-AD-018).


### QR-N-026 — C-GA.2b XONG: resolve core (row-lock FOR UPDATE, fail-closed, race verify Docker thật)
- Date: 2026-07-14
- Đã build + verify (Docker THẬT): build 0-warning; `dotnet test Platform.slnx` = **124 test, 0 fail, 0 skip** (GuestAccess.IntegrationTests 11 = 4 constraint + 6 logic resolve + 1 race; Bedrock.ContractTests 2 gồm ErrorCodeSnapshot khớp 3 code mới; StarHill.ArchitectureTests 16).
  - `GuestAccess.Application`: `GuestAccessErrors` {qr_invalid=NotFound(404), room_inactive=Conflict(409), configuration_unavailable=Failure(500)}; `IGuestSessionKeyHasher`; `IGuestSessionStore` (row-lock port); `ResolveTokenContracts` (ResolveTokenInput/Result); `ResolveTokenUseCase` (`IUseCase<,>` thường — tự quản transaction hẹp).
  - `GuestAccess.Infrastructure`: `EfGuestSessionStore` (Npgsql `SELECT ... LIMIT 1 FOR UPDATE` qua FromSqlRaw+ToListAsync, tên bảng lấy từ model — mirror EfOutboxDispatcher; SQLite fallback FirstOrDefault không lock) + `Sha256GuestSessionKeyHasher` (hex 64) + factory DI resolve keyed IUnitOfWork + cross-module Contracts. csproj đổi ref Domain+Contracts→Application (mirror Rooms).
  - **Transaction/race (QR-AD-026 verify):** resolve đọc token/config cross-module NGOÀI transaction, rồi bọc critical-section (lock session + visit) trong `IUnitOfWork.ExecuteInTransactionAsync` tường minh. Race test 8 request đồng thời cùng cookie đua TẠO visit cho phòng-2 (chưa có visit) → FOR UPDATE serialize → 8/8 cùng VisitId, đúng 1 Active row, 0 lỗi. Chứng minh row-lock hội tụ, KHÔNG cần "catch 23505 requery".
  - **Fail-closed:** config null → configuration_unavailable, KHÔNG ghi session/visit (verify test). qr_invalid/room_inactive không lộ metadata.
  - **ErrorCodeSnapshot (QR-AD-018):** Bedrock.ContractTests +ref GuestAccess.Application + 3 code vào ExpectedCodes + assembly vào collector → catalog GuestAccess được gác (không vỡ snapshot).
- Chọn `IUseCase<,>` thường + transaction tường minh (không ICommandUseCase decorator): (a) lock FOR UPDATE chỉ giữ trong critical-section (tối thiểu contention endpoint public); (b) test được KHÔNG cần AddBedrockCore; (c) đúng precedent CreateRoom. Đã verify AddBedrockCore decorate `IUseCase<,>` bằng TransactionUseCaseDecorator (chỉ mở tx khi ITransactionalUseCase) — resolve không phải ITransactionalUseCase nên decorator pass-through, không mở tx thừa.
- NEXT — C-GA.3: `GuestAccess.Api` (`POST /v1/guest/resolve` AllowAnonymous + cookie `__Host-` + no-store + rate-limit + no-secret-log) + Host wiring (connection string guest_access + migrate gated) + compose/CI bundle + HTTP TestServer tests + log-capture guard. Cần đọc endpoint module mẫu (RoomsEndpointModule) + GuestOptions + rate-limit policy trước khi code.


### QR-N-027 — C-GA.3 XONG: public API resolve + Host wiring + deploy (verify Docker thật)
- Date: 2026-07-14
- Đã build + verify (Docker THẬT): build 0-warning; `dotnet test Platform.slnx` = **128 test, 0 fail, 0 skip** (StarHill.Api.Tests 26 = +4 GuestAccessResolveEndpointTests; smoke Host boot ValidateOnBuild wiring GuestAccess PASS; fail-fast HS256 test vẫn đạt nhờ appsettings có placeholder GuestAccess).
  - `GuestAccess.Api`: `GuestAccessEndpointModule` `POST /v1/guest/resolve` (AllowAnonymous — Req 11.2), token trong BODY (QR-DV-006), set cookie `__Host-starhill_guest` (HttpOnly/Secure/SameSite=Lax/Path=/, Expires theo `IClock`) CHỈ khi phát session mới, `Cache-Control: no-store`, Result→ProblemDetails. `GuestAccessOptions` (CookieName/SessionCookieDays) + `AddGuestAccessApi` bind + ValidateOnStart (name `__Host-`, days [30,90]).
  - Host: StarHill.Api.csproj +ref GuestAccess.Infrastructure/Api; Program.cs +AddGuestAccessInfrastructure/Api + migrate guest_access; appsettings + smoke factory + docker-compose +connection string GuestAccess; starhill-ci.yml +migration bundle GuestAccess. Platform.slnx +GuestAccess.Api.
  - Test HTTP (TestServer, KHÔNG Docker): anonymous POST 200; Set-Cookie `__Host-` đủ thuộc tính; no-store; **NON-DISCLOSURE** raw session key CHỈ ở Set-Cookie KHÔNG trong body JSON; reconnect (IssuedSessionKey null) → KHÔNG Set-Cookie; failure qr_invalid → 404 + KHÔNG cookie + ProblemDetails; GET → 405/404.
- **Rate-limit (Req 11.5):** KHÔNG dựng mới — Bedrock.Api đã có global rate limiter (F16, `UseBedrockApi` #9, partition theo IP thật resolve sau ForwardedHeaders) áp cho MỌI endpoint gồm resolve anonymous → baseline DoS protection đạt sẵn. Named policy chặt hơn cho resolve = tùy chọn hardening sau (chưa cần).
- **Log-safety (Req 11.6) đạt BẰNG THIẾT KẾ:** token ở BODY (không path/query) → request-path logging không bao giờ thấy token; endpoint KHÔNG log token/cookie; response không echo key. (Log-capture assertion tầng pipeline để dành nếu cần hardening thêm.)
- **Bảo mật endpoint công khai (flag):** endpoint AllowAnonymous CÓ CHỦ ĐÍCH (guest không đăng nhập — Req 11.2); bù lại: non-disclosure lỗi (CP1), rate-limit biên (F16), cookie `__Host-` HttpOnly/Secure, no-store, token không vào URL/log. Cách ly mạng nội bộ là trách nhiệm hạ tầng (design gốc), app không tự enforce.
- CÒN của module GuestAccess (defer có chủ đích): C-GA.4 (current-guest-context DTO/port + sweeper idle→Expired) khi consumer đầu (Rules) cần; C-GA.5 (staff close + outbox cascade GuestVisitEnded) khi Concierge/Housekeeping tồn tại. Resolve slice đã đủ dùng độc lập.
- Compose end-to-end (guest_access migrate + /health/ready) mirror y hệt 3 module đã boot ở QR-N-016 (P0) — khuyến nghị chạy `docker compose up` khi kiểm tra deploy; wiring đã verify qua smoke ValidateOnBuild + migration thật (constraint tests).


### QR-N-028 — C-GA.3a XONG: reconciliation design↔code + input/log/ledger hardening (verify Docker + Compose thật)
- Date: 2026-07-14
- Bối cảnh: rà soát sau C-GA.3 phát hiện DRIFT (không phải bug runtime) giữa design và code + một coupling deploy ngầm. Xử lý tận gốc theo design-first: cập nhật design/journal (QR-AD-024..026 Proposed→Accepted, thêm QR-AD-028, QR-TO-007/008) TRƯỚC, rồi mới sửa code + guard.
- Nội dung đã sửa (bản chất, không phải ngọn):
  1. **Transaction reconcile:** design §6 từng ghi resolve là `ICommandUseCase`/`ITransactionalUseCase` + transaction decorator; code thực (C-GA.2b) là `IUseCase<,>` tự mở transaction hẹp SAU read cross-module. Đã sửa design về đúng implementation (giữ transaction hẹp — ít giữ lock hơn, không giả-atomic xuyên DbContext). Không đổi code (code đã đúng).
  2. **Canonical input guard (QR-AD-025):** thêm `GuestCredentialFormat.IsCanonical` (43 ký tự base64url = đúng output `ITokenGenerator.NewToken()` mặc định). Token không canonical → `qr_invalid` TRƯỚC resolver/DB; cookie không canonical (whitespace/quá dài/ký tự lạ) → coi như thiết bị mới, KHÔNG hash/lookup (trước đây cookie whitespace ném từ hasher, cookie quá dài vẫn bị hash — nay chặn tận gốc trước SHA-256/DB).
  3. **HTTP contract (QR-AD-025):** `ResolveResponse` đổi từ phẳng sang NESTED (room/resort/languages/defaultLanguage/visit/features) đúng design §7; thêm snapshot test khoá shape (chống drift âm thầm vì trước đó không test shape).
  4. **Request body limit:** khai `RequestSizeLimitAttribute(1024)` cho endpoint resolve; **Kestrel enforce THẬT** — Compose smoke body 4108 byte → HTTP 413. Guard test khoá giá trị 1 KiB (metadata).
  5. **Log-redaction (Req 11.6) từ "by design" → GUARD:** thêm `GuestAccessResolveLogRedactionTests` chạy PIPELINE THẬT (`UseBedrockApi` + `RequestLoggingMiddleware` + `LoggingUseCaseDecorator`) — khẳng định raw token/raw cookie/raw issued key KHÔNG vào bất kỳ dòng log nào (cả success lẫn failure), issued key CHỈ ra qua Set-Cookie.
  6. **Migration history per-schema (QR-AD-028):** phát hiện DB thật chỉ có `public.__EFMigrationsHistory` chứa cả 5 migration của 4 module (coupling ngầm, trái schema-ownership). Cấu hình `MigrationsHistoryTable("__EFMigrationsHistory", <schema>)` ở CẢ Host runtime + 4 design-time factory + 5 integration setup (đồng nhất → bundle không lệch Host). Thêm transition SQL idempotent `deploy/migrations/20260714-split-ef-history.sql`.
- Verify (Docker Server thật + Compose):
  - `dotnet build Platform.slnx -c Release` = **0 warning/0 error**.
  - `dotnet test Platform.slnx -c Release` = **PASS toàn bộ, 0 fail, 0 skip** (GuestAccess.IntegrationTests 25 gồm 14 guard input mới + race Postgres; StarHill.Api.Tests 30 gồm nested-contract + body-limit + 2 log-redaction; ResortConfig 15; Rooms 27; Identity 2; ArchitectureTests 16 gồm JournalConsistency 5/5).
  - **Transition SQL trên DB thật (mô phỏng upgrade):** chạy trên DB đang mang ledger public → tạo 4 ledger per-schema đúng (identity 2, resort_config 1, rooms 1, guest_access 1 = 5, khớp public); guard "unmapped migration" hoạt động (bắt lỗi precedence EXCEPT/UNION của chính script → đã sửa); **idempotent** (chạy lần 2 không đổi).
  - **Upgrade path đầu-cuối:** rebuild image binary mới (history per-schema) trên volume ĐÃ transition → log `SELECT ... FROM <schema>."__EFMigrationsHistory"` + "No migrations were applied. The database is already up to date." cho cả 4 module → "Application started" (KHÔNG re-apply, KHÔNG xung đột).
  - **Compose smoke:** `/health/ready`=200, `/health/live`=200; `POST /v1/guest/resolve` token invalid → 404 + `Cache-Control: no-store` + KHÔNG Set-Cookie + ProblemDetails `code:qr_invalid`; body 4108 byte → 413.
  - Diagnostics spec = 0; JournalConsistency INV-1..5 = 5/5.
- Defer có chủ đích (ghi trade-off, không giả completion): named resolve limiter (QR-TO-007 — chờ SLO/shared-NAT budget); drop ledger public (QR-TO-008 — giữ cho rollback, rollout riêng); C-GA.4/C-GA.5 (chờ consumer Rules/Concierge/Housekeeping).
- NEXT: chờ user duyệt commit; kế tiếp design-first module Rules (consumer đầu của GuestAccess current-context).


### QR-N-029 — Anti-drift tầng traceability (INV-6) + guard per-schema history (biến claim thành test)
- Date: 2026-07-14
- Bối cảnh: phiên C-GA.3a lộ ra lớp drift design↔code mà cổng journal INV-1..5 KHÔNG bắt (chỉ kiểm nội bộ journal). Fix tận gốc = thêm ràng buộc code-enforced nối "quyết định đã xong" với "guard test có thật".
- Đã làm:
  1. **INV-6 (QR-AD-029)** trong `StarHillJournalConsistencyTests`: (a) mọi QR-AD `Status` chứa "Implemented" phải có dòng `- Guard-Tests:` ≥1 test class; (b) mọi test class được liệt kê phải tồn tại `class <Name>` trong `starhill/tests/**/*.cs` (quét source, ancestor-walk như RequireJournalDir; FAIL nếu không tìm thấy thư mục tests — không tự tắt). Đã thêm dòng `Guard-Tests` cho QR-AD-024/025/026/028/029.
  2. **Guard per-schema history (QR-AD-028)**: thêm `ModuleMigrationHistorySchemaTests` (StarHill.Api.Tests, Docker-free) — boot Host thật qua `SecretInjectingHostFactory`, resolve 4 DbContext, đọc `RelationalOptionsExtension.MigrationsHistoryTableSchema` và khẳng định đúng schema module (identity/resort_config/rooms/guest_access). Biến QR-AD-028 từ "chỉ Compose-verify" thành có guard tự động → đủ điều kiện Status Implemented dưới INV-6.
- Lý do (bản chất): INV-1..5 đảm bảo journal tự-nhất-quán nhưng không nối journal↔code; một AD có thể ghi "xong" trong khi test tương ứng bị xóa/đổi tên/chưa từng có. INV-6 khóa vòng đó bằng build. Chọn structured field + source-scan thay reflection để robust và cùng triết lý parse-file của cổng (QR-TO-009).
- Verify: build 0-warning; `StarHillJournalConsistencyTests` (giờ 6 INV) + `ModuleMigrationHistorySchemaTests` xanh; diagnostics spec 0. (Chi tiết lệnh trong lần chạy phiên này.)
- NEXT: giữ kỷ luật — mọi AD tương lai chuyển Implemented phải kèm Guard-Tests hợp lệ; cân nhắc nâng cấp attribute `[Traces]` nếu cần kiểm NỘI DUNG test (không chỉ tồn tại). Module kế tiếp: design-first Rules.


### QR-N-030 — D-Rules.0 design-first XONG: thiết kế module Rules + kích hoạt C-GA.4 (chưa code)
- Date: 2026-07-14
- Đã tạo `design-modules/04-rules.md` (design-first, chưa code) — grounded vào file thật, không suy đoán:
  - Data model lấy từ product `docs/resort-qr-portal/design.md` §Data Models (Rules): RuleSet/RuleSection(+Translation)/RulePublication(+Section+Translation)/RuleAcknowledgement; unique `RulePublication(ResortId) WHERE IsCurrent`, unique `RuleAcknowledgement(GuestVisitId, RulePublicationId)`, unique `(section, lang)`.
  - Quyết định: QR-AD-030 (snapshot publish + ack server-authoritative + rule-gate backend), QR-AD-031 (IHtmlSanitizer adapter Ganss ở shared starhill project + RequirePort + sanitize-on-save), QR-AD-032 (GuestAccess.Contracts.ICurrentGuestContextResolver = C-GA.4 + portal-window check-before-touch, tách Resolve/Touch). Trade-off QR-TO-010/011.
  - Đối soát Contracts THẬT: `IResortSettingsQuery.ResortSettingsSnapshot` có đủ 3 cờ RequireRuleAckFor*; `ITranslationResolver`/`Translated<T>` sẵn cho CP5; `GuestAccess.Contracts` mới chỉ có `GuestAccessModule` (đúng như .csproj hẹn C-GA.4); `IHtmlSanitizer` là port bắt buộc CHƯA có adapter/package/RequirePort → Rules đóng mắt xích; xmin auto-map khi entity `IHasConcurrencyToken` (PlatformDbContext đã đọc).
  - Build slices D-Rules.0..4 + chèn C-GA.4 làm dependency của guest rules/gate; mỗi CP (CP3/4/5/12/13/15 + ack-unique + window) map guard test + nơi chạy; unique/concurrency/window là gate Postgres thật.
- INV-6: QR-AD-030/031/032 Status=Proposed (chưa Implemented) → KHÔNG cần Guard-Tests; sẽ thêm khi từng slice code xong và chuyển Implemented.
- Verify phiên này (design-only): diagnostics 5 file spec/journal = 0; JournalConsistency INV-1..6 xanh (AD-030/031/032 đã map anti-drift, ID liên tục, không dangling). KHÔNG build/test code (chưa có code Rules).
- NEXT: chờ user review `04-rules.md`; khi duyệt → D-Rules.1 (Domain/Contracts/Persistence + migration + Postgres constraint) trước, rồi D-Rules.2 (sanitize) → D-Rules.3 (publish) → C-GA.4 → D-Rules.4 (guest read/ack/gate).


### QR-N-031 — D-Rules.1 XONG: Rules persistence nền (Domain/Contracts/Infrastructure + migration + Postgres constraint)
- Date: 2026-07-15
- Đã build + verify (Docker Server 29.5.2 thật — phải KHỞI ĐỘNG Docker Desktop trong phiên vì daemon ban đầu chưa chạy):
  - 3 project src mới: `Rules.{Domain,Contracts,Infrastructure}` (mirror khuôn GuestAccess/Rooms). Domain: 7 entity —
    Draft (`RuleSet`/`RuleSection`/`RuleSectionTranslation` implement `IHasConcurrencyToken` → xmin CP15) + snapshot bất
    biến (`RulePublication`/`RulePublicationSection`/`RulePublicationSectionTranslation`, KHÔNG concurrency) +
    `RuleAcknowledgement` (Guid trần RoomId/GuestSessionId/GuestVisitId — không FK chéo schema). Contracts:
    `RulesModule.PersistenceKey="rules"` (thuần, KHÔNG coupling GuestAccess/ResortConfig Contracts — QR-TO-010).
  - Infrastructure: `RulesDbContext` (schema `rules`, keyed, KHÔNG outbox) + `RulesConfigurations` (7 config) + Factory
    design-time (MigrationsHistoryTable `rules` — QR-AD-028) + `AddRulesInfrastructure` (CHỈ keyed persistence — use case
    ở D-Rules.2..4). Migration `20260715014025_InitialCreate` verify: EnsureSchema `rules`; partial unique
    `ux_rule_publication_current` filter `is_current`; unique `ux_rule_ack_visit_publication`,
    `ux_rule_section_translation_lang`, `ux_rule_pub_section_translation_lang`; FK toàn bộ `principalSchema:"rules"`
    (KHÔNG chéo schema). `.editorconfig generated_code` che analyzer (QR-AD-009) → build 0-warning.
  - Test: `RulesBoundaryTests` (StarHill.ArchitectureTests, 3: Contracts thuần + không coupling GuestAccess/ResortConfig
    Contracts; Domain⊥Infra; negative control) + `RulesPostgresConstraintTests` (Testcontainers, 3: một IsCurrent/resort
    + tái dùng sau demote = flip-before-insert QR-AD-030; ack unique/visit,pub; translation unique/section,lang).
  - `Platform.slnx` +3 project src +1 test (`Rules.IntegrationTests`); `StarHill.ArchitectureTests.csproj` +3 ref Rules.
- Gate: `dotnet build Platform.slnx -c Release` = **0 warning/0 error**; `dotnet test Platform.slnx -c Release` = **0 fail,
  0 skip** với Docker (Rules.IntegrationTests 3/3 chạy THẬT; StarHill.ArchitectureTests 20 gồm INV-1..6 + RulesBoundary 3;
  GuestAccess 25; Rooms 27; ResortConfig 15; Identity 2; Api 31; unit/contract). Diagnostics spec 0.
- INV-6: chưa AD nào của Rules chuyển "Implemented" (AD-030/031/032 vẫn Proposed — publish/sanitize/gate ở slice sau) →
  D-Rules.1 KHÔNG kích hoạt ràng buộc Guard-Tests. Persistence này DE-RISK phần snapshot-unique của AD-030 (đã chứng minh
  partial-unique + flip-before-insert bằng Postgres thật) nhưng AD-030 chỉ Implemented khi có PublishRulesUseCase.
- NEXT — D-Rules.2: `Rules.Application` (CRUD Draft section/translation) + `IHtmlSanitizer` adapter Ganss (shared starhill)
  + pin package + RequirePort (QR-AD-031) + CP12 sanitize test + CP15 concurrency test. Đổi Rules.Infrastructure ref
  Domain+Contracts → Rules.Application (mirror Rooms/GuestAccess). Khi D-Rules.2 xong + AD-031 Implemented → thêm Guard-Tests.


### QR-N-032 — Web-testability Phase 1 XONG: OpenAPI dev-only (prod tắt) + overlay compose dev
- Date: 2026-07-15
- Bối cảnh: user hỏi "đã test trên web được chưa". Probe compose thật: `live=200 ready=200 openapi=404 swagger=404 rooms(noauth)=401 resolve=404 guestrules=404` → API chạy nhưng chưa browser-testable (không OpenAPI, không frontend, chưa seed admin/room, Rules chưa wiring). Phase 1 mở khám phá API qua trình duyệt (dev), giữ prod an toàn.
- Đã làm (QR-AD-033): Host gọi `AddBedrockOpenApi()` CHỈ khi `builder.Environment.IsDevelopment()`; `docker-compose.dev.yml` overlay EXPLICIT đặt `ASPNETCORE_ENVIRONMENT=Development` (compose mặc định vẫn Production → OpenAPI tắt). KHÔNG thêm UI (giữ DV-015 — QR-TO-012).
- Guard `OpenApiExposureTests` (StarHill.Api.Tests, WebApplicationFactory, KHÔNG Docker): Development → `/openapi/v1.json` = 200 (thân tài liệu chứa "openapi"); Production → 404. Env-gating verify thật.
- Verify: build 0-warning; StarHill.Api.Tests + OpenApiExposureTests xanh; JournalConsistency INV-1..6 (AD-033 map anti-drift + Guard-Tests hợp lệ); diagnostics 0. (Smoke compose dev: `docker compose -f docker-compose.yml -f docker-compose.dev.yml up` → `/openapi/v1.json`=200.)
- CÒN để browser test ĐẦU-CUỐI luồng guest (đã nêu, chưa làm — chờ user chọn): (Phase 2) dev-only bootstrap admin + phòng/token demo để login + resolve ra dữ liệu thật; (Phase 3) D-Rules.2..4 đưa `/v1/guest/rules` lên Host; (Phase 4/Wave F) frontend SPA. UI OpenAPI click-through = QR-TO-012 (hoãn).
- NEXT (mặc định nếu user không chỉ khác): tiếp D-Rules.2 (Application CRUD Draft + IHtmlSanitizer adapter QR-AD-031) HOẶC Phase 2 dev bootstrap tùy user.


### QR-N-033 — D-Rules.2a XONG: adapter IHtmlSanitizer (Ganss, shared StarHill.Html) — đóng mắt xích port bắt buộc
- Date: 2026-07-15
- Đã build + verify (KHÔNG cần Docker):
  - Pin `HtmlSanitizer` (Ganss.Xss) **9.0.892** ở `starhill/Directory.Packages.props` — tái dùng version legacy resort-qr (verify build restore sạch, KHÔNG lỗi NuGetAudit/CVE trên .NET 10).
  - Project shared mới `starhill/src/StarHill.Html` (mirror precedent `StarHill.Authorization` — QR-AD-031): `GanssHtmlSanitizerAdapter : IHtmlSanitizer` (allowlist mặc định Ganss + bỏ `img`; null/rỗng→"") + `AddStarHillHtml` DI. Reference Bedrock.Application (port) + package Ganss; KHÔNG ref module QR (Faq/Rules tái dùng không coupling chéo).
  - **Thread-safety (bản chất, không fix ngọn):** legacy dùng sanitizer singleton, nhưng `Ganss.Xss.HtmlSanitizer` không đảm bảo thread-safe cho `Sanitize` đồng thời qua mọi version (không kiểm chứng được offline) → adapter đăng ký singleton nhưng KHỞI TẠO sanitizer MỚI mỗi call. Đường admin ghi nội dung tần suất thấp → chi phí không đáng kể; loại nghi ngờ concurrency tận gốc.
  - Test `StarHill.Html.Tests/GanssHtmlSanitizerAdapterTests` (6, KHÔNG Docker): loại script/event-handler/javascript-uri/img; giữ `<b>`/`<em>`; rỗng→"". Verify tích hợp Ganss 9.0.892 chạy đúng .NET 10 (khử rủi ro package/version).
  - `Platform.slnx` +1 project src (StarHill.Html) +1 test (StarHill.Html.Tests). Sửa CA1859 (field test dùng kiểu concrete).
- Gate: `dotnet build Platform.slnx -c Release` = **0 warning/0 error**; `StarHill.Html.Tests` 6/6 pass.
- QR-AD-031 VẪN Proposed (chưa Implemented): 2a mới làm adapter+package+placement; CÒN sanitize-on-save trong use case (D-Rules.2b) + RequirePort(IHtmlSanitizer) ở Host (D-Rules.4). Khi đủ 3 phần → chuyển Implemented + Guard-Tests (INV-6). Vì vậy chưa kích hoạt ràng buộc INV-6.
- NEXT — D-Rules.2b: `Rules.Application` use case CRUD Draft section/translation (Title/Body đi qua `IHtmlSanitizer.Sanitize` TRƯỚC khi lưu → cột BodyHtmlSanitized) + validator + đổi Rules.Infrastructure ref sang Application + `AddBedrockRepository<RulesDbContext, RuleSet/RuleSection/RuleSectionTranslation>(key)` + use case factory keyed. Test: CP12 (use case lưu ra không còn script — inject adapter thật) + CP15 (hai update cùng section → 409 xmin, Postgres).


### QR-N-034 — StarHill compose tách tối giản (không RabbitMQ) + xác nhận cơ chế bật/tắt tính năng (base + product)
- Date: 2026-07-15
- Bối cảnh: user hỏi RabbitMQ có bật/tắt được không (giữ StarHill sạch–đơn giản, 60 phòng) và base có config bật/tắt tính năng không.
- XÁC MINH (code thật, không suy đoán):
  - Messaging/RabbitMQ ĐÃ opt-in: `Program.cs` chỉ cắm khi `Bedrock:Messaging:Enabled=true`; mặc định TẮT → `ThrowingEventBusPublisher`. `HostSmokeTests` boot Host OFF-mode và pass ⇒ chạy không cần RabbitMQ đã kiểm chứng.
  - Base feature-toggle ĐÃ CÓ: hạ tầng (`Bedrock:Messaging:Enabled`, `Bedrock:ApplyMigrationsOnStartup`, OpenAPI IsDevelopment QR-AD-033, port posture AddXxxCore+override); tính năng SẢN PHẨM per-resort trong `ResortSettings` (FaqEnabled/ChatEnabled/HousekeepingEnabled/RequireRuleAckFor*) — admin bật/tắt, KHÔNG cần dựng thêm.
  - P1-15: chỉ Identity produce outbox (`AddBedrockOutbox`); off-mode cần `AllowOutboxWithoutDispatcher=true` (guard chống event tích lũy im lặng).
- ĐÃ LÀM (QR-AD-034/QR-TO-013): `docker-compose.yml` → tối giản (postgres+host, messaging off, AllowOutboxWithoutDispatcher true); thêm `docker-compose.messaging.yml` overlay explicit (rabbitmq + Messaging__Enabled). Default `docker compose up` = 2 service.
- Verify (THỰC ĐO phiên 2026-07-15, Docker Desktop 29.5.2): (1) `docker compose config --services` default = `postgres`, `host` (KHÔNG rabbitmq); (2) `docker compose -f docker-compose.yml -f docker-compose.messaging.yml config --services` = `postgres`, `rabbitmq`, `host` (overlay bật đúng); (3) `docker compose down --remove-orphans` dọn rabbitmq phiên trước → `docker compose up -d --build` default → `docker compose ps` chỉ `starhill-host-1` + `starhill-postgres-1` (KHÔNG rabbitmq); (4) `/health/ready`=200 Healthy + `/health/live`=200 Healthy; (5) log host: `CREATE SCHEMA identity` + áp 5 migration (Identity InitialCreate+AddOutboxTraceContext, ResortConfig, Rooms, GuestAccess InitialCreate) + `Now listening on http://[::]:8080`, và grep `rabbit|amqp|BrokerUnreachable|SocketException` = 0 dòng (boot off-mode sạch, không thử kết nối broker). INV-1..6 (`StarHillJournalConsistencyTests`) = 6/6 pass sau khi thêm AD-034/TO-013.
- ĐỀ XUẤT (chưa làm, cần user duyệt vì đụng base/QR-AD-027): (a) làm integration-event emission opt-in ở base Identity → off-mode KHÔNG ghi outbox thừa (sạch tuyệt đối); (b) khi cascade GuestVisitEnded cần: cân nhắc in-process dispatch cho single-instance 60 phòng thay vì bắt buộc broker (đánh giá lại QR-AD-027 ở quy mô nhỏ).
- NEXT: theo mạch build vẫn là D-Rules.2b; hoặc xử lý đề xuất (a)/(b) nếu user muốn tối giản triệt để messaging.

### QR-N-035 — Slice D-Rules.2b XONG: Rules.Application Draft CRUD + sanitize-on-save (CP12) + concurrency (CP15) + đúng-một-RuleSet (QR-AD-035)
- Date: 2026-07-15
- Bối cảnh: tiếp mạch build Rules sau D-Rules.1 (persistence nền) + D-Rules.2a (adapter IHtmlSanitizer). D-Rules.2b = tầng Application ghi Draft + đóng bất biến lưu-trữ.
- ĐÃ CÓ SẴN trên đĩa (verify lúc vào phiên — KHÔNG tin summary): `Rules.Application` (CreateRuleSectionUseCase find-or-create + UpsertRuleSectionTranslationUseCase sanitize-on-save + Update/DeleteRuleSectionUseCase + 3 validator + RuleContracts + RulesErrors), `Rules.Infrastructure` đã đổi ref → Application + keyed repo (RuleSet/RuleSection/RuleSectionTranslation) + factory use case + validator (mirror Rooms), EF `RuleSetConfiguration` unique `ux_rule_set_resort`, migration `20260715045457_AddRuleSetResortUnique`, test csproj đã ref `StarHill.Html`+Sqlite. Build 0-warning/0-error.
- **DRIFT phát hiện + fix tận gốc**: code (RuleSetConfiguration/RuleContracts/CreateRuleSectionUseCase/migration) tham chiếu "QR-AD-035" NHƯNG journal CHƯA có entry → đúng loại drift design↔journal. Fix: bổ sung QR-AD-035 (01-decisions) đầy đủ Provenance/Rationale/Alternatives + Guard-Tests; thêm row anti-drift map (05).
- **ĐÃ BỔ SUNG phiên này** (phần còn thiếu của 2b):
  1. **CP12 sanitize (no Docker)** `RuleSanitizeTests` (SQLite + adapter Ganss THẬT qua `AddStarHillHtml`, 3 test): HTML độc (`<script>`/`onerror`/`javascript:`) KHÔNG lọt vào `BodyHtmlSanitized` đã lưu, markup an toàn (`<p>`/`<strong>`) được GIỮ, Title cũng sanitize; blank→null chuẩn hóa; upsert idempotent (một bản ghi/section+lang).
  2. **CP15 concurrency (Postgres Testcontainers)** `RuleConcurrencyTests`: hai scope sửa cùng `RuleSection` → người sau `DbUpdateConcurrencyException` (xmin active trên RuleSection — mirror ResortConfig CP15).
  3. **QR-AD-035 guard (Postgres)** thêm `Only_one_rule_set_per_resort` vào `RulesPostgresConstraintTests`: hai RuleSet cùng ResortId → vi phạm `ux_rule_set_resort`.
  4. **I7 boundary** thêm `RulesBoundaryTests.Application_should_not_depend_on_infrastructure_or_api` (Application ⊥ Bedrock.Infrastructure/Api/Rules.Infrastructure/EFCore/AspNetCore) + ref Rules.Application.
- Gate (THỰC ĐO): `dotnet build Platform.slnx -c Release` = **0 warning/0 error**; `Rules.IntegrationTests` = **8/8 pass, 0 skip** (Docker thật: 4 constraint + 1 concurrency + 3 sanitize); `RulesBoundaryTests` = **4/4 pass** (gồm I7 mới).
- QR-AD-031 (sanitize adapter + RequirePort + sanitize-on-save) VẪN **Proposed**: sanitize-on-save nay ĐÃ code+test (RuleSanitizeTests) nhưng còn **RequirePort(IHtmlSanitizer)** ở Host (D-Rules.4) → chưa đủ 3 phần để chuyển Implemented. Vì Proposed nên INV-6 chưa ràng buộc Guard-Tests (đúng). QR-AD-030 (snapshot-publish/ack/gate) vẫn Proposed (Publish=D-Rules.3, gate/ack=D-Rules.4).
- NEXT — **D-Rules.3**: `PublishRulesUseCase` (flip-before-insert: hạ IsCurrent cũ + SaveChanges TRƯỚC → insert publication mới Version++ → copy đông cứng section/translation đã sanitize) + preview/history + CP4 test (Postgres partial-unique `ux_rule_publication_current`).

### QR-N-036 — Slice D-Rules.3a XONG: PublishRulesUseCase (snapshot flip-before-insert, CP4)
- Date: 2026-07-15
- Bối cảnh: tiếp mạch Rules sau D-Rules.2b. D-Rules.3a = phần lõi + rủi ro cao nhất của Publish (snapshot bất biến + partial-unique atomic). Preview/history tách D-Rules.3b (read đơn giản, gộp cùng đường guest-read D-Rules.4).
- Đã đọc/valid TRƯỚC code (không suy đoán): `IRepository` cấm IQueryable (F9) → cần read-model; `IUnitOfWork.ExecuteInTransactionAsync` reentrancy-safe; `Entity` sinh UUIDv7 trong ctor (Id sẵn cho FK copy trước SaveChanges); precedent `ResolveTokenUseCase` (QR-AD-026, flush-trước-insert cho partial-unique); `RulePublicationConfiguration` partial-unique `ux_rule_publication_current` filter `is_current`.
- Đã làm (QR-AD-036):
  1. `Rules.Application/IRuleDraftReader` (+ DTO RuleDraftSnapshot/Section/Translation) — read-model nội-module đọc Draft ordered.
  2. `Rules.Infrastructure/Persistence/EfRuleDraftReader` (no-tracking, 3 truy vấn set→sections ordered→translations, ghép bộ nhớ).
  3. `Rules.Application/PublishRulesUseCase` (IUseCase tự-quản transaction; flip-before-insert; version=current+1; copy đông cứng section/translation đã sanitize) + `PublishRulesValidator` + `PublishRulesInput/Result` (RuleContracts) + `RulesErrors.NoPublishableContent`.
  4. Wiring `RulesInfrastructureExtensions`: +3 repo publication keyed (`RulePublication`/`RulePublicationSection`/`RulePublicationSectionTranslation`) + `IRuleDraftReader`→`EfRuleDraftReader` (scoped) + factory `PublishRulesUseCase` + validator.
  5. Test `PublishRulesUseCaseTests` (Postgres Testcontainers, migration thật, 4): version-1 frozen + section sắp theo SortOrder + translation copy; publish lần 2 demote bản 1 + đúng-một-current (flip-before-insert giữ partial-unique); sửa Draft sau publish KHÔNG đổi snapshot (CP4 immutability); Draft rỗng → NoPublishableContent.
- Gate (THỰC ĐO): `dotnet build Platform.slnx -c Release` = **0 warning/0 error**; `Rules.IntegrationTests` = **12/12 pass, 0 skip** (Docker thật: 4 constraint + 1 concurrency + 3 sanitize + 4 publish).
- QR-AD-030 (snapshot-publish + ack + gate) VẪN Proposed: snapshot-publish PART nay đã code+test (PublishRulesUseCaseTests) nhưng ack server-authoritative + rule-gate = D-Rules.4 → chưa đủ để Implemented. Vì Proposed nên INV-6 chưa ràng buộc Guard-Tests (đúng).
- NEXT — chọn một:
  * **D-Rules.3b**: preview Draft (render như khách, không publish) + GetPublicationHistory (đọc publication cũ) — read đơn giản.
  * **C-GA.4** (`ICurrentGuestContextResolver` + portal-window check-before-touch) — dependency của guest-read/ack/gate.
  * **D-Rules.4**: guest GetCurrentRules + AcknowledgeRules (server-authoritative) + IRuleGate + Host wiring + RequirePort(IHtmlSanitizer) → khi đó QR-AD-030/031 chuyển Implemented.

### QR-N-037 — Slice C-GA.4 XONG: ICurrentGuestContextResolver (portal-window check-before-touch) + đóng gap registry Rules
- Date: 2026-07-15
- Bối cảnh: theo dependency graph, C-GA.4 là port cross-module GuestAccess mà guest-read/ack/rule-gate (D-Rules.4) phụ thuộc. Ưu tiên C-GA.4 trước D-Rules.3b (preview/history là leaf không mở khóa gì; shape read có thể đổi khi làm guest-read → tránh gold-plate).
- Đã đọc/valid TRƯỚC code (không suy đoán): `GuestVisit.LastSeenAt`/`ExpiresAt`/`Status`; `IResortGuestConfigQuery` mang `PortalWindowMinutes`+`VisitIdleExpiryHours`; `Error` factory (Unauthorized→401); boundary test GuestAccess.Contracts CHỈ cấm Application/Infra/Api (KHÔNG cấm Bedrock.Domain → Result<T> hợp lệ); precedent `EfRoomTokenResolver` (Contracts-resolver impl ở Infra); `Sha256GuestSessionKeyHasher` (hex thường 64); check constraint `ck_guest_visit_closed_at` (visit không-Active phải có ClosedAt — phát hiện qua test đỏ, sửa seed).
- Đã làm (QR-AD-032 → Implemented):
  1. `GuestAccess.Contracts/ICurrentGuestContextResolver` + `CurrentGuestContext` (+ref Bedrock.Domain cho Result<T>).
  2. `GuestAccessErrors` +`SessionExpired`/`GuestContextMissing` (Unauthorized/401).
  3. `EfCurrentGuestContextResolver` (Infra): ResolveAsync đọc-kiểm-window KHÔNG touch (cookie rỗng/lạ→guest_context_missing; không-Active/quá-window→session_expired; config null→configuration_unavailable; còn hạn→context); reuse `IGuestSessionKeyHasher` (nguồn hash duy nhất); TouchAsync bảo toàn idle-delta, no-op nếu không Active. Đăng ký factory keyed IUnitOfWork ở `AddGuestAccessInfrastructure`.
  4. Test `CurrentGuestContextResolverTests` (Postgres Testcontainers, 8): within-window→context+không-touch; quá-window→session_expired+không-touch; visit-Closed→session_expired; cookie lạ/null→guest_context_missing; config null→configuration_unavailable; Touch→trượt LastSeenAt+giữ delta idle; Touch no-op trên visit đã đóng.
- **DRIFT/GAP đóng tận gốc**: rà `ErrorCodeSnapshotTests` (QR-AD-018 "phủ MỌI catalog module") phát hiện **thiếu `Rules.Application`** → mã `rules_conflict` (RulesErrors.DraftConflict, D-Rules.2b) KHÔNG được gác. Fix: thêm `Rules.Application` vào catalogAssemblies + ProjectReference Bedrock.ContractTests + `rules_conflict`/`guest_context_missing`/`session_expired` vào snapshot. Nay registry phủ MỌI catalog (Identity/Rooms/GuestAccess/Rules).
- Gate (THỰC ĐO): `dotnet build Platform.slnx -c Release` = **0 warning/0 error**; `GuestAccess.IntegrationTests` = **33/33 pass, 0 skip** (25 cũ + 8 C-GA.4); `Bedrock.ContractTests` = 2/2 (snapshot khớp sau khi phủ Rules).
- NEXT — **D-Rules.4** (mở khóa QR-AD-030/031 → Implemented): guest `GetCurrentRulesUseCase` (đọc publication IsCurrent + i18n fallback CP5) + `AcknowledgeRulesUseCase` (server-authoritative, dùng ICurrentGuestContextResolver + IsCurrent, idempotent CP13) + `IRuleGate` (CP3, cờ ResortSettings) + endpoints (admin publish/preview + guest read/ack) + Host wiring + RequirePort(IHtmlSanitizer). D-Rules.3b (preview/history) có thể gộp vào đây.

### QR-N-038 — Slice D-Rules.4a XONG: guest GetCurrentRules (đọc publication IsCurrent + i18n fallback CP5)
- Date: 2026-07-15
- Bối cảnh: D-Rules.4 là slice lớn mở khóa QR-AD-030/031→Implemented (guest read + ack + rule-gate + endpoints + Host wiring + RequirePort). Chia sub-slice verify từng bước: 4a = guest READ (cohesive, testable CP5, nền cho ack/gate). 4b = Acknowledge (server-authoritative, dùng ICurrentGuestContextResolver C-GA.4). 4c = IRuleGate + endpoints + Host wiring + RequirePort.
- Đã đọc/valid TRƯỚC code: `ITranslationResolver.MatchSupported`/`Resolve<T>` + `ITranslation.HasContent` + `Translated<T>` (IsFallback/IsMissing); `IResortGuestConfigQuery` (EnabledLanguageCodes/DefaultLanguageCode); `IRepository` cấm IQueryable (F9 → read-model); precedent `EfRuleDraftReader` (QR-AD-036).
- Đã làm:
  1. `Rules.Application.csproj` +ref `ResortConfig.Contracts` (theo design §2 — cross-module Contracts cho config + i18n).
  2. `IRulePublicationReader` + DTO (`CurrentRulesSnapshot`/`PublishedSectionSnapshot`/`PublishedTranslationSnapshot`). **QR-DV-007**: `ITranslation` đặt trên DTO `PublishedTranslationSnapshot` (Application), KHÔNG trên entity Domain → giữ `Rules.Domain` thuần (i18n là concern rendering/Application).
  3. `GetCurrentRulesUseCase` (IUseCase read-only, không transaction): config fail-closed→configuration_unavailable; chưa publish→rules_unavailable; MatchSupported chọn ngôn ngữ hiển thị; Resolve mỗi section (requested→default fallback→missing).
  4. `EfRulePublicationReader` (no-tracking, 3 truy vấn publication IsCurrent→sections ordered→translations).
  5. `RulesErrors` +`RulesUnavailable` (rules_unavailable, 404) +`ConfigurationUnavailable` (dùng chung mã `configuration_unavailable`). Wiring reader + use case (factory resolve IResortGuestConfigQuery + ITranslationResolver).
  6. `ErrorCodeSnapshotTests` +`rules_unavailable`.
  7. Test `GetCurrentRulesTests` (SQLite + TranslationResolver THẬT, 7): exact-match không fallback; thiếu bản dịch→fallback default (IsFallback); ngôn ngữ lạ→default; section không có bản dịch→IsMissing; giữ SortOrder; chưa publish→rules_unavailable; config null→configuration_unavailable.
- Gate (THỰC ĐO): `dotnet build Platform.slnx -c Release` = **0 warning/0 error**; `Rules.IntegrationTests` = **19/19 pass, 0 skip** (12 + 7 CP5); `Bedrock.ContractTests` = 2/2 (snapshot +rules_unavailable).
- QR-AD-030 VẪN Proposed: snapshot-publish (D-Rules.3a) + guest-read (4a) đã có; CÒN ack server-authoritative (4b) + rule-gate (4c) → mới đủ Implemented.
- NEXT — **D-Rules.4b**: `AcknowledgeRulesUseCase` (server-authoritative CP13 — server đọc publication IsCurrent, KHÔNG tin version client; ghi RuleAcknowledgement unique (visit,publication) idempotent; dùng `ICurrentGuestContextResolver` để lấy GuestVisit + touch sau thành công). Postgres test.


### QR-N-039 — Slice D-Rules.4b XONG: AcknowledgeRulesUseCase (server-authoritative CP13, idempotent)
- Date: 2026-07-15
- Bối cảnh: sub-slice 4b của D-Rules.4 (guest ack). Verify code thật trước: KHÔNG có drift (design §12 khớp — 4a xong, 4b/4c chưa; tree CLEAN, HEAD 9f75b0a). `AcknowledgeRulesUseCase`/`IRuleGate`/`Rules.Api` chưa tồn tại → đúng ranh giới.
- Đã đọc/valid TRƯỚC code: `RuleAcknowledgement` (unique `ux_rule_ack_visit_publication`, Id trần), `IRulePublicationReader.LoadCurrentAsync` (PublicationId+Version), `ResortGuestConfig` (EnabledLanguageCodes/DefaultLanguageCode), `ITranslationResolver.MatchSupported`, `UniqueConstraintViolationException` (Bedrock.Domain.Results, QR-AD-010), precedent `CreateRoomUseCase` (value-returning write MỘT SaveChanges + catch-unique, KHÔNG ITransactionalUseCase).
- Đã làm:
  1. `RuleContracts.cs` +`AcknowledgeRulesInput`(ResortId/RoomId/GuestSessionId/GuestVisitId/RequestedLanguage — KHÔNG version/publicationId client) + `AcknowledgeRulesResult`(RulePublicationId/Version/AlreadyAcknowledged).
  2. `AcknowledgeRulesUseCase` (`IUseCase<,>`): server đọc `RulePublication IsCurrent` (CP13 — không tin client); idempotency HAI LỚP: (1) pre-check `AnyAsync(visit,pub)` (chạy mọi provider, phủ re-ack thường) + (2) catch `UniqueConstraintViolationException` (backstop race Postgres). Ghi MỘT insert (nguyên tử, mirror CreateRoom). Fail-closed config→configuration_unavailable; chưa publish→rules_unavailable.
  3. Wiring: +`AddBedrockRepository<RulesDbContext,RuleAcknowledgement>(key)` + factory use case (reader + config + i18n + repo ack + UoW + clock).
  4. Test CP13 `AcknowledgeRulesTests` (SQLite, 6, KHÔNG Docker): ack đầu ghi đúng pub IsCurrent+version+language; ack lặp cùng visit idempotent (pre-check, không tạo trùng); publish version mới→ack lại (bản mới, giữ lịch sử); chưa publish→rules_unavailable; config null→configuration_unavailable; ngôn ngữ lạ→chuẩn hóa default. Unique-constraint (visit,publication) race = `RulesPostgresConstraintTests.Acknowledgement_is_unique_per_visit_and_publication` (đã có sẵn từ D-Rules.1, Postgres/CI).
- Quyết định (không AD mới — nằm trong QR-AD-030 acknowledge part): use case NHẬN `CurrentGuestContext` đã-phân-giải qua input (KHÔNG tự gọi `ICurrentGuestContextResolver`) → giữ use case thuần/testable-không-Docker; việc resolve context + `Touch` sau thành công là trách nhiệm ENDPOINT (D-Rules.4c, đúng tách Resolve/Touch của QR-AD-032). Idempotency pre-check + unique-backstop = cùng triết lý QR-AD-010/026 (không TOCTOU, DB là nguồn sự thật race).
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail; Rules.IntegrationTests 16 pass/9 skip(Postgres không-Docker) gồm 6 AcknowledgeRulesTests. `vp journal` INV-1..6 xanh.
- QR-AD-030 VẪN Proposed (còn 4c: IRuleGate + endpoints guest/admin + Host wiring + RequirePort(IHtmlSanitizer) mới đủ Implemented + Guard-Tests — INV-6). CÒN LẠI: D-Rules.4c, D-Rules.3b (preview/history).


### QR-N-040 — Slice D-Rules.4c(gate) XONG: IRuleGate backend (CP3)
- Date: 2026-07-15
- Bối cảnh: phần rule-gate của D-Rules.4c (tách nhỏ: gate trước, endpoints+Host+RequirePort sau). Verify code thật: tree CLEAN sau 4b (956307d); `IRuleGate`/`RuleGate` chưa tồn tại → đúng ranh giới. Đọc/valid trước: `ResortGuestConfig` (RequireRuleAckForFaq/Chat/Housekeeping — tên field xác nhận từ Contracts), `IRulePublicationReader.LoadCurrentAsync`, `IRepository.AnyAsync`, `ErrorCodeSnapshotTests` (Ordinal + scan Rules.Application), precedent GuestAccess.Contracts (trả `Result`).
- Đã làm:
  1. `Rules.Contracts`: `IRuleGate.EnsureAcknowledgedAsync(resortId, guestVisitId, GuestFeature, ct)` + enum `GuestFeature{Faq,Chat,Housekeeping}`. +ref `Bedrock.Domain` (trả `Result` — mirror GuestAccess.Contracts; KHÔNG ref Contracts module khác — QR-TO-010 giữ nguyên).
  2. `RulesErrors.RuleAckRequired` = `Error.Forbidden("rule_ack_required", ...)` → HTTP 403. +vào `ErrorCodeSnapshotTests.ExpectedCodes` (Ordinal giữa room_inactive/rules_conflict) → snapshot 2/2 pass.
  3. `RuleGate` (Rules.Application): cờ tắt→Success; cờ bật→kiểm `RuleAcknowledgement(visit, IsCurrent.Id)` (server-authoritative, cùng nguồn CP13); thiếu→rule_ack_required; chưa publish→rule_ack_required (fail-closed, không cho qua); config null→configuration_unavailable. Read-only, đăng ký scoped dưới `IRuleGate` (keyed repo ack).
  4. Test CP3 `RuleGateTests` (SQLite, 6, KHÔNG Docker): cờ tắt cho qua dù chưa ack; cờ bật chưa ack→403; cờ bật đã ack→qua; cờ bật chưa publish→403; cờ độc lập theo tính năng; config null→configuration_unavailable.
- Bằng chứng: `vp all` build 0-warning + full suite 0-fail; Rules.IntegrationTests 22 pass/9 skip(Postgres không-Docker) gồm 6 RuleGateTests; StarHill.ArchitectureTests 21/21 (boundary OK sau khi Rules.Contracts +Bedrock.Domain); ErrorCodeSnapshot 2/2. `vp journal` INV-1..6 xanh.
- QR-AD-030 VẪN Proposed: còn endpoints guest/admin (Rules.Api) + Host wiring (AddRulesApi + RequirePort(IHtmlSanitizer) → QR-AD-031) + CI bundle mới đủ Implemented + Guard-Tests (INV-6). CÒN LẠI: D-Rules.4c(api) + D-Rules.3b (preview/history).


### QR-N-041 — Slice D-Rules.4c(api-1) XONG: Host wiring Rules + RequirePort(IHtmlSanitizer) + admin endpoints
- Date: 2026-07-15
- Bối cảnh: phần đầu D-Rules.4c(api) — wire Rules vào Host LẦN ĐẦU + endpoints admin. Verify code thật: Host Program.cs CHƯA có Rules (không AddRulesInfrastructure/Api, không conn string, không migrate); `Rules.Api` chưa tồn tại; preview/history use case CHƯA có (D-Rules.3b) → endpoint admin giới hạn ở use case ĐANG CÓ.
- Đã đọc/valid TRƯỚC: pattern guest/admin endpoint (GuestAccessEndpointModule/RoomsEndpointModule), `AddStarHillHtml` (StarHill.Html), API `services.AddRequiredPort<TPort>()` (BedrockRegistrationExtensions — Bedrock.Application.DependencyInjection), `IResortSettingsQuery.GetAsync()` (single-resort ResortId), `ICurrentUser.UserId` (actor publish), CI bundle pattern (4 module).
- Đã làm:
  1. `Rules.Api` project (ref Rules.Application + ResortConfig.Contracts + StarHill.Authorization + Bedrock.Api) + `RulesAdminEndpointModule` (`/v1/rules`: POST sections, PUT sections/{id}, DELETE sections/{id}, PUT sections/{id}/translations/{lang}, POST publish — TẤT CẢ RequireStaff Req 8/11.3; ResortId phân giải server qua IResortSettingsQuery; actor publish = ICurrentUser.UserId) + `AddRulesApi`.
  2. Host: +conn string `Rules` + `AddRulesInfrastructure(MigrationsHistoryTable rules — QR-AD-028)` + `AddRulesApi` + migrate rules. **`AddStarHillHtml()` + `AddRequiredPort<IHtmlSanitizer>()`** — port bảo mật KHÔNG default (thiếu = XSS lọt) → boot FAIL-FAST tường minh. csproj Host +Rules.Infrastructure/Rules.Api/StarHill.Html; Platform.slnx +Rules.Api.
  3. appsettings + docker-compose +`ConnectionStrings__Rules`; starhill-ci.yml +bundle `rules` (mirror 4 module).
  4. Test `RulesAdminEndpointAuthTests` (TestServer + fake use cases, KHÔNG DB, 2): Staff soạn+publish→2xx (201/204/200); no-token→401. Fakes Rules admin thêm vào file fakes chung.
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail; StarHill.Api.Tests 35/35 (**Host boot WebApplicationFactory với Rules wired + RequirePort(IHtmlSanitizer)+AddStarHillHtml — không phá boot**); Rules.IntegrationTests 22 pass/9 skip(Postgres). `vp journal` INV-1..6 xanh.
- QR-AD-031 (adapter + sanitize-on-save + RequirePort) NAY đủ 3 phần code; QR-AD-030 còn guest endpoints. GIỮ cả hai Proposed → flip Implemented + Guard-Tests atomic ở D-Rules.4c(api-2, guest endpoints). CÒN LẠI: 4c-api-2 (GET/POST guest rules + resolver+touch), D-Rules.3b (preview/history).


### QR-N-042 — Slice D-Rules.4c(api-2) XONG: guest endpoints → QR-AD-030/031 Implemented
- Date: 2026-07-15
- Bối cảnh: mắt xích cuối D-Rules.4 — guest read/ack endpoints. Verify code thật: `RulesGuestEndpointModule` chưa có; tree CLEAN sau 4c-api-1 (ffc820e). Quyết định thiết kế: nguồn-sự-thật tên cookie cross-module.
- **QR-AD (cookie name canonical — nằm trong QR-AD-025, không AD mới):** guest rules endpoint (Rules.Api) cần đọc cookie session, nhưng `GuestAccessOptions.CookieName` ở GuestAccess.Api mà Rules.Api KHÔNG được ref (coupling Api↔Api). FIX TẬN GỐC: thêm hằng canonical `GuestAccessModule.SessionCookieName` ở **GuestAccess.Contracts** (khớp pattern PersistenceKey); `GuestAccessOptions.CookieName` mặc định = hằng đó (một nguồn sự thật, behavior-preserving — value không đổi nên GuestAccessResolveEndpointTests vẫn assert `__Host-starhill_guest`). Consumer khác (Faq/Concierge/Housekeeping) sau này dùng chung hằng. Trade-off (QR-TO ngầm): nếu deployment override `GuestAccess:CookieName` qua config thì cross-module đọc hằng → lệch; NHƯNG appsettings KHÔNG có section GuestAccess (override không tồn tại) + tên `__Host-` là hợp đồng browser-security cố định → chấp nhận, tài liệu hoá.
- Đã làm:
  1. `GuestAccessModule.SessionCookieName` (Contracts) + `GuestAccessOptions.CookieName` default = hằng.
  2. `Rules.Api` +ref GuestAccess.Contracts. `RulesGuestEndpointModule` (AllowAnonymous): `GET /v1/guest/rules?roomId&lang` (resolve context qua `ICurrentGuestContextResolver` từ cookie → `GetCurrentRulesUseCase`; **KHÔNG touch** — đọc phụ trợ) + `POST /v1/guest/rules/acknowledge {roomId,lang}` (resolve → `AcknowledgeRulesUseCase` → **`TouchAsync` SAU khi ack thành công** — check-before-touch QR-AD-032). `no-store`; roomId client gửi (cookie định danh THIẾT BỊ, không phòng). `AddRulesApi` +guest module.
  3. Test `RulesGuestEndpointTests` (TestServer + fake resolver/use case, 4, KHÔNG DB): context OK→200+no-store+sections + cookie đọc đúng qua hằng; resolver Failure→ProblemDetails KHÔNG touch; ack OK→TouchCount=1; ack resolver-fail→TouchCount=0.
- **FLIP Implemented (INV-6 cưỡng chế Guard-Tests tồn tại):** QR-AD-030 (snapshot+ack+gate — Guard: PublishRulesUseCaseTests/AcknowledgeRulesTests/RuleGateTests/RulesAdminEndpointAuthTests/RulesGuestEndpointTests/RulesPostgresConstraintTests) + QR-AD-031 (adapter+sanitize+RequirePort — Guard: GanssHtmlSanitizerAdapterTests/RuleSanitizeTests).
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail; StarHill.Api.Tests 39/39 (+4 RulesGuestEndpointTests); `vp journal` INV-1..6 xanh (INV-6 xác nhận mọi Guard-Tests class tồn tại thật). CÒN LẠI Rules: D-Rules.3b (preview/history endpoint — use case chưa có), C-GA.4 sweeper (defer), C-GA.5 cascade (khi có Concierge/Housekeeping).


### QR-N-043 — Host endpoint WIRING smoke (browser-substitute, KHÔNG DB/Docker) — anti-drift tầng HTTP
- Date: 2026-07-15
- Bối cảnh: user báo "mở web bằng browser phát hiện cực nhiều lỗi" + nghĩ có MCP browser. Verify THẬT: `kiro_powers list` = **KHÔNG có power/MCP** (không lái browser được); `web_fetch` chỉ HTTPS công khai (không localhost); máy KHÔNG Docker → không chạy full-stack+browser. Không bịa một phiên browser.
- Vấn đề bản chất (fix tận gốc): test module lẻ (Rooms/ResortConfig/Rules/GuestAccessEndpointAuthTests) tự map endpoint trong TestServer riêng → KHÔNG chứng minh `AddRoomsApi/AddResortConfigApi/AddRulesApi/AddGuestAccessApi` THẬT SỰ được Host wire. Đây đúng lớp lỗi browser hay lộ (endpoint 404 do quên wire; auth sai). Cần verify ở HOST THẬT đã compose mà KHÔNG cần DB/browser.
- Cơ chế: `HostEndpointWiringSmokeTests` (WebApplicationFactory<Program> — Host THẬT compose, KHÔNG DB): (1) 9 endpoint admin protected KHÔNG token → **401** (401 chứng minh ĐÃ wire + ĐÃ bảo vệ; 404 = quên wire; 200 = quên bảo vệ — bắt cả 3 lớp lỗi; auth middleware chạy TRƯỚC handler nên không chạm DB); (2) guest `/v1/guest/rules` KHÔNG cookie → `EfCurrentGuestContextResolver` short-circuit `guest_context_missing` TRƯỚC DB (đọc code xác nhận) → `application/problem+json` (chứng minh mapped + qua pipeline); (3) route lạ → 404.
- Bằng chứng: `HostEndpointWiringSmokeTests` 11/11 pass → MỌI endpoint 5 module wire đúng vào Host + bảo vệ đúng (KHÔNG lỗi wiring tầng HTTP). `vp all` build 0-warning + full suite 0-fail (StarHill.Api.Tests 50). Lỗi browser của user (nếu còn) thuộc tầng full-stack+DB runtime (cần Docker tái hiện — chạy CI/máy có Docker) hoặc frontend Vue (CHƯA dựng — N-076).
- Đề xuất nếu cần test browser/frontend thật: cài Playwright MCP qua powers UI (khi có frontend), hoặc chạy `docker compose up` trên máy có Docker rồi mở `:18080` (health/openapi) — ngoài khả năng máy hiện tại.

### QR-N-044 — Slice D-Rules.3b XONG: admin preview Draft "như khách" + lịch sử publication (Req 8.4)
- Date: 2026-07-15
- Bối cảnh: mắt xích cuối mặt ADMIN nội quy còn thiếu sau D-Rules.4c (QR-N-041/042 đã ghi "CÒN LẠI: D-Rules.3b"). Req 8.4 cần admin (1) xem TRƯỚC bản Draft render đúng như khách sẽ thấy (theo ngôn ngữ + fallback CP5) mà KHÔNG publish, và (2) xem lịch sử các bản đã phát hành để đối soát/rollback thủ công. Verify code thật TRƯỚC: `GetDraftPreviewUseCase`/`GetPublicationHistoryUseCase` chưa tồn tại; `IRulePublicationReader` chỉ có `LoadCurrentAsync`; tree clean sau 5fc37e5.
- Đã làm (fix tận gốc, mirror pattern read đã kiểm — không cuộn mới):
  1. `IRuleDraftReader.RuleDraftTranslationSnapshot` NAY implement `ITranslation` (+`HasContent` = Title|Body sau trim khác rỗng) → dùng chung `ITranslationResolver` fallback (CP5) như guest read. Đặt `ITranslation` trên DTO read-model tầng Application (đã ref ResortConfig.Contracts) THAY VÌ entity Domain — giữ `Rules.Domain` thuần, i18n là concern render/Application (QR-DV-007, nhất quán `PublishedTranslationSnapshot`).
  2. `IRulePublicationReader` +`ListHistoryAsync(resortId)` + record `RulePublicationHistoryItem(PublicationId, Version, PublishedAt, PublishedByUserId, ChangeNote, IsCurrent)`. `EfRulePublicationReader.ListHistoryAsync`: chỉ metadata (không kéo section/translation — nhẹ), `OrderByDescending(Version)` (mới nhất trước), no-tracking.
  3. `GetDraftPreviewUseCase` (`IUseCase<GetDraftPreviewInput,GetDraftPreviewResult>`, read-only, không transaction — mirror `GetCurrentRulesUseCase`): config null→`configuration_unavailable` (fail-closed nhất quán guest read); Draft null→Success RỖNG (admin đang soạn, KHÔNG phải lỗi — khác guest `rules_unavailable`); còn lại render Draft theo lang qua resolver, tái dùng `RenderedRuleSection` (không PublicationId). `GetPublicationHistoryUseCase`: đọc `reader.ListHistoryAsync`.
  4. `RuleContracts.cs` +`GetDraftPreviewInput/Result` + `GetPublicationHistoryInput/Result`.
  5. `RulesInfrastructureExtensions` +đăng ký 2 use case (scoped, factory resolve reader/config/resolver keyed như GetCurrentRules).
  6. `RulesAdminEndpointModule` +`GET /v1/rules/preview?lang=` + `GET /v1/rules/publications` (RequireStaff Req 8/11.3; resortId server qua `IResortSettingsQuery`; map Result→HTTP qua ProblemDetailsBuilder). Handler `PreviewAsync`/`PublicationsAsync`.
  7. Test `RulesAdminReadTests` (Rules.IntegrationTests, SQLite in-memory — read + i18n provider-agnostic, KHÔNG Docker, 5): preview i18n-fallback (yêu-cầu vi, chỉ có en → fallback en + IsFallback=true); preview no-draft→empty; preview config-null→configuration_unavailable; history newest-first (Version giảm dần + IsCurrent đúng); history empty khi chưa publish.
  8. Guard wiring: `RulesAdminEndpointAuthTests` +2 fake (`FakeGetDraftPreview`/`FakeGetPublicationHistory`) +đăng ký +assertion (Staff GET preview/publications→200; no-token→401). **Root-cause fix:** endpoint GET map trong TestServer THẬT → nếu thiếu đăng ký use case, RDF suy luận param service thành BODY → GET cấm body → InvalidOperationException lúc build endpoint (đã gặp: 2 test fail trước khi thêm fake). Bổ sung fake là đúng bản chất — guard endpoint-auth phải phản ánh mọi endpoint module map thật.
- Bằng chứng: `starhill\scripts\vp.cmd build` 0-warning; `vp all` build 0-warning + validate-ci OK + full suite 0-fail — StarHill.Api.Tests 52/52 (+2 GET auth), Rules.IntegrationTests 27 pass/9 skip(Postgres không-Docker) gồm 5 RulesAdminReadTests. `vp journal` INV-1..6 xanh.
- Mặt admin nội quy (soạn Draft + upsert i18n sanitize + publish + preview + history) HOÀN TẤT. CÒN LẠI Rules: C-GA.5 cascade (khi có Concierge/Housekeeping), C-GA.4 sweeper (defer). Kế tiếp module: FAQ/Concierge/Housekeeping theo dependency graph (đều dùng `IRuleGate` đã có).

### QR-N-045 — Design module Faq XONG (Wave E, design-first); CHƯA code
- Date: 2026-07-16
- Bối cảnh: BE mới xong 5/8 module (Identity/ResortConfig/Rooms/GuestAccess/Rules). Module kế theo dependency graph +
  độ đơn giản = **Faq** (tái dùng ngay `IRuleGate` + i18n resolver + `IHtmlSanitizer` đã có; không phụ thuộc SignalR như
  Concierge). Giữ nguyên tắc DESIGN-FIRST: viết `design-modules/05-faq.md` → diagnostics 0 → đọc lại valid → chờ user
  duyệt trước khi code.
- Đã VERIFY chữ ký thật TRƯỚC khi thiết kế (không suy đoán): `Rules.Contracts.IRuleGate.EnsureAcknowledgedAsync(resortId,
  guestVisitId, GuestFeature, ct)` + `enum GuestFeature{Faq,Chat,Housekeeping}`; `GuestAccess.Contracts.ICurrentGuestContextResolver.
  ResolveAsync(sessionKey, roomId)`→`Result<CurrentGuestContext(GuestVisitId,GuestSessionId,RoomId,ResortId)>`+`TouchAsync`;
  `IResortGuestConfigQuery.GetAsync(resortId)`→`ResortGuestConfig(FaqEnabled,EnabledLanguageCodes,DefaultLanguageCode,
  RequireRuleAckForFaq,...)`; `RulesGuestEndpointModule`/`RulesAdminEndpointModule` (pattern resolve→gate→touch/no-store/
  ProblemDetailsBuilder/RequireStaff). Data model Faq lấy từ `docs/resort-qr-portal/design.md` §Data Models (FaqCategory/
  FaqCategoryTranslation/FaqItem[ParentId self]/FaqItemTranslation) + §Constraints + §Concurrency.
- **Faq là CONSUMER ĐẦU TIÊN của `IRuleGate`** → slice E-Faq.4 kiểm chứng luôn CP3 rule-gate ở tầng dùng thật (không chỉ
  RuleGateTests nội bộ Rules).
- Quyết định thiết kế nổi bật (sẽ cấp số QR-AD/DV/TO thật khi code — INV-3 cấm forward-ref số chưa tồn tại):
  1. Faq **KHÔNG Draft→Publish/version** (khác Rules) — requirements không yêu cầu ack/version cho FAQ; `IsActive` đủ; tránh gold-plate.
  2. Rule-gate đặt trong **use case guest-read** (`GetGuestFaqTreeUseCase`) → `Faq.Application` ref `Rules.Contracts` (defense-in-depth CP3, mọi caller bị gate; endpoint-only dễ quên).
  3. Guest `/faq` GET **touch cửa sổ sau thành công** (KHÁC Rules-GET-no-touch) — product design §resolve liệt kê /faq là API tương tác; duyệt FAQ là hoạt động chính (rules-viewer là đọc-lại phụ trợ); tránh session_expired khi đang đọc.
  4. Bất biến cây: `ParentId` phải cùng category + không self + không cycle → `faq_invalid_parent` (spec chỉ nói "cha-con"; AI tự ra chống đồ thị vòng gây render loop). Cycle-check in-memory (dữ liệu nhỏ, không recursive CTE).
  5. Mở rộng xmin cho Category/CategoryTranslation (product chỉ liệt kê Item/ItemTranslation) — nhất quán chống ghi đè âm thầm.
  6. Hard-delete chặn-khi-còn-tham-chiếu (category còn item / item còn con) thay cascade âm thầm.
  7. DEFER `FaqEvent` (product ghi "tùy chọn") + CTA chat-prefill (Req 4.6 — cần Concierge tồn tại) — I10 tránh phân mảnh/coupling sớm.
- Bằng chứng: `design-modules/05-faq.md` tạo xong, **getDiagnostics = 0**. Chưa viết C# (design-first tôn trọng). Mỗi CP/bất biến
  đã map guard test + Docker? + slice (§9/§10). Mã lỗi mới `FaqErrors` (faq_category_not_found/faq_item_not_found/faq_conflict/
  faq_invalid_parent/faq_category_not_empty/faq_item_has_children/faq_disabled) sẽ vào `ErrorCodeSnapshotTests` (QR-AD-018) khi code.
- **NEXT**: chờ user duyệt design §1/§3/§5/§11. Nếu đồng ý/không phản hồi → slice **E-Faq.1** (Domain/Contracts/Persistence +
  migration + boundary + Postgres unique) rồi lần lượt E-Faq.2..4. Mỗi slice: `vp all` 0-warning/0-fail + `vp journal` INV-1..6 + journal.

### QR-N-046 — Slice E-Faq.1 XONG: Faq persistence nền (Domain/Contracts/Infrastructure + migration + boundary + Postgres unique)
- Date: 2026-07-16
- Bối cảnh: bắt đầu hiện thực module Faq theo design QR-N-045 (đã duyệt ngầm — user "tiếp tục"). Mirror staging Rules
  D-Rules.1 (persistence nền TRƯỚC, KHÔNG wire Host — Host wiring để slice có Api = E-Faq.4).
- Đã làm:
  1. `Faq.Domain` (4 entity `: Entity, IHasConcurrencyToken`): `FaqCategory(ResortId,Key,SortOrder,IsActive)`,
     `FaqCategoryTranslation(FaqCategoryId,LanguageCode,Name)`, `FaqItem(ResortId,CategoryId,ParentId?,SortOrder,IsActive)`,
     `FaqItemTranslation(FaqItemId,LanguageCode,Question,AnswerHtmlSanitized)`. ResortId Guid trần.
  2. `Faq.Contracts` (`FaqModule.PersistenceKey="faq"`, chỉ ref Bedrock.Messaging.Contracts).
  3. `Faq.Infrastructure`: `FaqDbContext:PlatformDbContext` schema `faq` keyed + `FaqDbContextFactory` (design-time,
     MigrationsHistoryTable faq + snake_case) + `FaqConfigurations` (unique ux_faq_category_key/ux_faq_category_translation_lang/
     ux_faq_item_translation_lang; FK Restrict item→category & item→parent, Cascade translation→cha; index resort/category/parent)
     + `AddFaqInfrastructure` (AddBedrockPersistence + 4 keyed repository; use case ở E-Faq.2). Ref Domain+Contracts
     (đổi sang Application ở E-Faq.2 — mirror Rules).
  4. Migration `InitialCreate` sinh bằng `dotnet ef` (local tool 10.0.9, không cần DB). VERIFY grep: 4 CreateTable; `xid`
     rowVersion cả 4 (CP15); 3 unique; 2 Cascade + 2 Restrict đúng thiết kế. `.editorconfig` glob `**/Persistence/Migrations/*.cs`
     đã miễn analyzer (QR-AD-009) → build 0-warning.
  5. Test: `FaqBoundaryTests` (StarHill.ArchitectureTests, 3: Contracts thuần / Domain⊥Infra / negative control) +
     `FaqPostgresConstraintTests` (Faq.IntegrationTests, 4 SkippableFact Postgres: unique category-key / category-translation /
     item-translation + FK-Restrict chặn xóa category còn item). `Platform.slnx` +3 project Faq +Faq.IntegrationTests;
     arch-test csproj +3 ref Faq.
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail — StarHill.ArchitectureTests 24 (+3 Faq),
  Faq.IntegrationTests 4 skip(Postgres không-Docker; chạy CI), không regression (StarHill.Api.Tests 52, Rules 27 pass/9 skip).
  QR-AD-037 (Faq foundation) Status Implemented + Guard-Tests `FaqBoundaryTests`/`FaqPostgresConstraintTests` (tồn tại thật — INV-6).
- **NEXT — slice E-Faq.2**: `Faq.Application` (CRUD category/item + translation sanitize-on-save CP12 + cycle-check bất biến cây)
  + đổi Faq.Infrastructure ref→Application + validator + `FaqSanitizeTests`/`FaqItemParentValidationTests`/`FaqConcurrencyTests`.
  Rồi E-Faq.3 (reorder), E-Faq.4 (guest tree + rule-gate + Api + Host wiring + CI bundle faq).

### QR-N-047 — Slice E-Faq.2 XONG: Faq.Application (CRUD category/item + translation sanitize-on-save + cycle-check)
- Date: 2026-07-16
- Bối cảnh: tiếp E-Faq.1, hiện thực use case admin theo design §4/§10. Mirror Rules.Application (Result API, IRepository
  trực tiếp, IUseCase value-returning / ICommandUseCase void khai PersistenceKey, bắt UniqueConstraintViolationException).
- Đã làm:
  1. `Faq.Application` (ref Faq.Domain+Contracts+Bedrock.Application+FluentValidation): `FaqErrors` (6 mã: faq_category_not_found/
     faq_item_not_found/faq_conflict/faq_invalid_parent/faq_category_not_empty/faq_item_has_children) + `FaqContracts` (input/result)
     + 8 use case: CreateFaqCategory/UpdateFaqCategory/DeleteFaqCategory/UpsertFaqCategoryTranslation + CreateFaqItem/UpdateFaqItem/
     DeleteFaqItem/UpsertFaqItemTranslation + validator mỗi cái.
  2. **Sanitize-on-save (CP12)**: UpsertFaq*Translation sanitize Question/Answer/Name qua `IHtmlSanitizer` TRƯỚC lưu (adapter
     Ganss do Host/test cấp qua AddStarHillHtml).
  3. **Bất biến cây** (`FaqItemParentValidator` — quyết định AI tự ra): ParentId phải cùng category + không self + không cycle;
     walk tổ tiên bằng FindById lặp (F9, không IQueryable), giới hạn độ sâu. Create: parent-exists + same-category (cycle bất
     khả thi vì item chưa tồn tại). Update: +cycle-check (selfId).
  4. **Delete an toàn tham chiếu**: DeleteFaqCategory chặn khi còn item (faq_category_not_empty); DeleteFaqItem chặn khi còn
     con (faq_item_has_children) — enforce ở use case (AnyAsync) + backstop FK Restrict DB.
  5. **ResortId item DERIVE từ category** (không trust input) — tránh mismatch. Đổi `Faq.Infrastructure` ref Domain+Contracts→
     `Faq.Application`; đăng ký 8 use case (factory keyed) + 6 validator trong `AddFaqInfrastructure`. `Platform.slnx` +Faq.Application.
  6. `ErrorCodeSnapshotTests` (Bedrock.ContractTests) +assembly Faq.Application + 6 mã (Ordinal giữa conflict/forbidden) →
     đóng gap QR-AD-018 (registry phủ mọi catalog module). `FaqBoundaryTests` +Application⊥Infra/EF/ASP.NET (I7).
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail — Faq.IntegrationTests 13 pass/5 skip(Postgres):
  FaqSanitizeTests(3, Ganss thật) + FaqItemParentValidationTests(5) + FaqAdminCrudTests(5); StarHill.ArchitectureTests 25
  (+1 Faq Application boundary); Bedrock.ContractTests 2 (snapshot có mã Faq). `vp journal` INV-1..6 xanh. QR-AD-037 Guard-Tests
  mở rộng +4 class (INV-6 xác nhận tồn tại thật). Fix biên dịch: FaqItemParentValidationTests thiếu `using Bedrock.Domain.Results`
  cho `Result<>` (helper) → thêm; build lại 0-warning.
- **NEXT — slice E-Faq.3** (reorder): `ReorderFaqUseCase` (nhận toàn bộ thứ tự mới, nguyên tử) + `ReorderFaqUseCaseTests`.
  Rồi E-Faq.4 (guest tree read + rule-gate consumer đầu tiên IRuleGate + Api + Host wiring + CI bundle faq).

### QR-N-048 — Slice E-Faq.3 XONG: reorder danh mục/mục FAQ (drag-drop, batch nguyên tử)
- Date: 2026-07-16
- Bối cảnh: Req 4.5 (sắp xếp drag-drop category + item). Design §4: nhận TOÀN BỘ thứ tự mới (không swap từng cặp) →
  batch một transaction.
- Đã làm:
  1. `FaqContracts` +`FaqReorderEntry(Id, SortOrder)` + `ReorderFaqCategoriesInput(ResortId, Entries)` + `ReorderFaqItemsInput(CategoryId, Entries)`.
  2. `ReorderFaqUseCases`: **2 use case tách** (category vs item — khác entity/repo/validation): `ReorderFaqCategoriesUseCase`
     (mỗi entry phải là category THUỘC ResortId — sai scope → faq_category_not_found) + `ReorderFaqItemsUseCase` (mỗi entry
     phải là item THUỘC CategoryId — sai → faq_item_not_found). ICommandUseCase (void) khai PersistenceKey → batch SortOrder
     một transaction (nguyên tử). Chỉ cập nhật Id được gửi (partial). KHÔNG đổi cha-con (chỉ thứ tự; move dùng UpdateFaqItem).
     Validator: Entries not-empty + Id phân biệt + SortOrder ≥ 0.
  3. Đăng ký 2 use case + 2 validator (keyed) trong `AddFaqInfrastructure`.
  4. Test `ReorderFaqUseCaseTests` (SQLite, 4: reorder category cập nhật SortOrder; chặn category resort khác;
     reorder item; chặn item ngoài category) + `ReorderValidatorTests` (4: Id trùng/rỗng/SortOrder âm/hợp lệ — validator độc lập).
- Quyết định (∈ QR-AD-037): reorder KHÔNG dùng unique-SortOrder (SortOrder là gợi ý sắp xếp, ties vỡ ở read bằng thứ
  tự phụ — E-Faq.4) → không cần "trạng thái trung gian không trùng"; batch vẫn tốt hơn swap (một transaction, ít round-trip).
  Tách 2 use case (không dùng type-discriminator runtime) → type-safe + validation rõ theo scope.
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail — Faq.IntegrationTests 21 pass/5 skip(Postgres)
  (+8 reorder). `vp journal` INV-1..6 xanh. QR-AD-037 Guard-Tests +ReorderFaqUseCaseTests/ReorderValidatorTests (INV-6 tồn tại thật).
- **NEXT — slice E-Faq.4** (mắt xích cuối module Faq): read-model `IFaqReader`/`EfFaqReader` + `GetGuestFaqTreeUseCase`
  (config→FaqEnabled→**rule-gate IRuleGate GuestFeature.Faq** [Faq là consumer đầu tiên]→i18n fallback CP5→dựng cây in-memory)
  + `FaqGuestEndpointModule` (resolve context→gate→tree→touch) + `FaqAdminEndpointModule` (RequireStaff) + `AddFaqApi` +
  Host wiring (conn Faq + migrate + AddFaqApi) + CI bundle `faq` + `FaqEndpointAuthTests` + `HostEndpointWiringSmokeTests` +InlineData.
  Faq.Application sẽ +ref ResortConfig.Contracts + Rules.Contracts (rule-gate trong use case — defense-in-depth). Flip guard đủ.

### QR-N-049 — Slice E-Faq.4a XONG: guest tree read + rule-gate (consumer đầu tiên IRuleGate) + Api + Host wiring
- Date: 2026-07-16
- Bối cảnh: mắt xích cuối mặt guest+admin module Faq. FAQ là **CONSUMER ĐẦU TIÊN của `IRuleGate`** → kiểm chứng CP3 ở
  tầng dùng thật (không chỉ RuleGateTests nội bộ Rules).
- Đã làm:
  1. `Faq.Application` +ref ResortConfig.Contracts + Rules.Contracts. `FaqErrors` +`Disabled` (faq_disabled 403) +
     `ConfigurationUnavailable`. Read-model `IFaqReader` + snapshot DTO (FaqCategory/Item/Translation implement ITranslation
     — QR-DV-007) + `GetGuestFaqTreeUseCase`: config→FaqEnabled(false→faq_disabled)→**rule-gate EnsureAcknowledgedAsync(Faq)**
     (gate TRONG use case = defense-in-depth CP3)→MatchSupported→dựng cây (ToLookup theo ParentId [key null OK, KHÔNG
     ToDictionary], order SortOrder/Id, đệ quy). `EfFaqReader` (Infra, no-tracking, CHỈ active — item parent-inactive tự
     ẩn cả nhánh). Đăng ký reader + guest use case.
  2. `Faq.Api`: `FaqAdminEndpointModule` (CRUD category/item + upsert translation + reorder categories/items — TẤT CẢ
     RequireStaff; resortId server-side qua IResortSettingsQuery) + `FaqGuestEndpointModule` (GET `/v1/guest/faq?roomId&lang`
     AllowAnonymous, resolve context→use case→**touch SAU thành công** [KHÁC Rules-GET, QR-N-045]; no-store) + `AddFaqApi`.
  3. Host: +conn `Faq` + `AddFaqInfrastructure(MigrationsHistoryTable faq)` + `AddFaqApi` + migrate faq. csproj Host
     +Faq.Infrastructure/Faq.Api; Platform.slnx +Faq.Api. appsettings + docker-compose +`ConnectionStrings__Faq`;
     starhill-ci.yml +bundle `faq`. IHtmlSanitizer RequirePort đã có từ Rules (không thêm trùng).
  4. Test: `GuestFaqTreeUseCaseTests` (SQLite + resolver thật + fake IRuleGate, 5: i18n fallback + cha-con; gate-fail→
     rule_ack_required; faq_disabled; config-null→configuration_unavailable; inactive loại) + `FaqEndpointAuthTests`
     (TestServer + fake, 4: Staff CRUD/reorder 2xx; no-token admin→401; guest tree 200 no-token; guest resolver-fail→
     problem không-401) + `HostEndpointWiringSmokeTests` +4 InlineData (3 admin faq→401 + guest faq→problem+json). +11 fake
     Faq vào file fakes chung. `ErrorCodeSnapshotTests` +faq_disabled.
- Fix biên dịch (tận gốc): (a) `ToDictionary<Guid?,...>` vi phạm notnull → chuyển `ToLookup` (cho phép key null) + order
  nguồn trước; (b) test dùng `Rules.Application.RulesErrors` mà Faq.IntegrationTests không ref Rules.Application → dùng
  `Error.Forbidden("rule_ack_required",...)` trực tiếp (không kéo ref thừa).
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail — StarHill.Api.Tests 60 (+8), Faq.IntegrationTests
  26 pass/5 skip(Postgres) (+5). `vp journal` INV-1..6 xanh. QR-AD-037 Guard-Tests +GuestFaqTreeUseCaseTests/FaqEndpointAuthTests.
- **Module Faq HOÀN TẤT mặt guest+admin write** (BE 6/8 module: Identity/ResortConfig/Rooms/GuestAccess/Rules/**Faq**).
  CÒN LẠI Faq: **E-Faq.4b** (admin READ-tree editor: full tree incl inactive + raw translations + MissingLanguages —
  hoãn pairs admin FE, I10). Kế tiếp module: **Concierge** (chat SignalR) hoặc **Housekeeping** (ticket) theo dependency graph.

### QR-N-050 — FIX bug thật lộ khi chạy Docker: `FaqPostgresConstraintTests.Cannot_delete_category_while_items_reference_it` ném EF client-side severing thay vì DB RESTRICT
- Date: 2026-07-16
- Bối cảnh: máy mới (`k.nguyen.manh.toan`) pull HEAD `a41d29f` (sync 0/0). Chạy FULL suite với Docker THẬT → 1 FAIL. Ở các phiên trước test này bị **SKIP** (máy khi đó không có Docker) nên CHƯA TỪNG chạy thật → lỗi ẩn (đúng cảnh báo user "mở web/chạy thật phát hiện nhiều lỗi").
- Triệu chứng: `System.InvalidOperationException: The association between 'FaqCategory' and 'FaqItem' has been severed, but the relationship is ... required` — KHÔNG phải `DbUpdateException` (23503) mà test kỳ vọng.
- Gốc rễ (không phải ngọn): test seed category + item trong CÙNG scope/DbContext (cả hai bị ChangeTracker theo dõi), rồi `Remove(category)` + SaveChanges. EF Core thấy item con TRACKED tham chiếu category qua FK required (DeleteBehavior.Restrict = client-side) → "sever" quan hệ và ném InvalidOperationException NGAY ở change-tracker, TRƯỚC khi lệnh DELETE chạm DB → ràng buộc DB `fk_faq_item_faq_category_category_id ON DELETE RESTRICT` không bao giờ được kiểm. Cùng lớp bài học QR-N-013 (Rooms soft-delete: test track con sai pattern).
- Verify production KHÔNG dính: `DeleteFaqCategoryUseCase` load CHỈ category (`FindByIdAsync`) + pre-check `_items.AnyAsync(CategoryId==)` → trả `CategoryNotEmpty`; KHÔNG track item con → không sever client-side. FK RESTRICT chỉ là backstop cho race (item thêm giữa pre-check và delete). ⇒ lỗi THUẦN ở test setup, không phải lỗi thiết kế/production.
- Fix: tách seed (scope 1) khỏi delete (scope 2). Scope 2 chỉ `SingleAsync(category)` (không load item) → DELETE chạm DB → FK RESTRICT (23503) → `DbUpdateException` đúng như kỳ vọng. Mirror production + kiểm đúng backstop DB. KHÔNG nới lỏng ràng buộc.
- Gate (THỰC ĐO Docker): test đơn 1/1 pass; trọn `Faq.IntegrationTests` **31/31 pass, 0 skip**.
- INSIGHT chống drift: "skip mềm khi thiếu Docker" có thể CHE lỗi thật (test xanh giả). Bằng chứng cuối PHẢI chạy Docker thật (đã là quy tắc — nay có ca thực chứng). Cân nhắc: thêm cổng CI/local bắt buộc Docker cho các `*PostgresConstraintTests`/`*ConcurrencyTests` trước khi coi slice "xong" (hiện CI ubuntu có Docker nên sẽ bắt; local phải nhớ bật Docker).

### QR-N-051 — Runtime HTTP probe (browser-substitute) lộ + FIX class lỗi 500 tại base (AD-103) + ghi nhận gap login
- Date: 2026-07-16
- Bối cảnh: user báo "mở web bằng browser phát hiện cực nhiều lỗi" + nghĩ có MCP browser. THỰC TẾ verify: KHÔNG có MCP/power browser nào cấu hình (`.kiro/settings/mcp.json` + user-level đều NONE; powers list rỗng). Docker chưa chạy trên máy mới → tôi khởi động Docker Desktop (29.5.2), rồi dùng HTTP probe (Invoke-WebRequest) qua docker-compose dev (OpenAPI bật) làm "browser thay thế".
- Baseline máy mới: HEAD `a41d29f` sync 0/0, working tree sạch (trừ nested stale `StarHillGuestApp/` — không đụng). Build 0-warning. Full suite Docker thật: **1 FAIL** = `FaqPostgresConstraintTests.Cannot_delete_category...` (đã fix QR-N-050 — test track child sai, EF sever client-side); phần còn lại xanh.
- **Lỗi runtime tìm qua probe** (đúng cảnh báo user): `GET /v1/guest/faq` và `GET /v1/guest/rules` KHÔNG kèm `?roomId=` → **HTTP 500** (log: `BadHttpRequestException: Required parameter "Guid roomId" was not provided`). Gốc rễ: `ExceptionHandlingMiddleware` (BASE) không xử `BadHttpRequestException` → rơi catch chung → 500.
- **FIX TẬN GỐC ở base (AD-103, không vá ngọn)**: thêm catch `BadHttpRequestException`→400 (`CommonErrors.Validation`, log Warning `ApiLog.BadRequest` EventId 1002). Đóng cả LỚP "malformed request→500" cho MỌI endpoint, không vá từng param. Verify sau fix (rebuild Host image, probe lại): guest/faq & guest/rules thiếu param → **400**; có `?roomId=<guid>` thiếu cookie → **401 guest_context_missing** (đúng); `POST /v1/guest/resolve` body 'not-json' → **400** (trước cũng 500). Sweep admin endpoints (rules/faq/rooms/resort-settings) → **401** (không 500); sai method → 405. `Bedrock.Api.Tests` (base) 61/61 (+`Bad_http_request_becomes_400_not_500`).
- **GAP GHI NHẬN (chưa fix — cần user duyệt vì là tính năng + bảo mật)**: Identity CHỈ map `POST /v1/identity/token/refresh`; KHÔNG có endpoint LOGIN (username/password → token). ⇒ không lấy được token khởi đầu ⇒ toàn bộ luồng admin (401) không dùng được end-to-end. Đây nhiều khả năng là phần lớn "lỗi khi mở web" (không đăng nhập được → không thao tác được), KHÔNG phải regression mà là slice CHƯA dựng. Đề xuất slice kế: Identity login use case (verify password qua `IPasswordHasher` base, phát access+refresh qua `IJwtTokenService`/`IRefreshTokenStore`, lockout/rate-limit) + endpoint + seed admin dev. DESIGN-FIRST trước khi code (đụng bảo mật).
- INSIGHT: unit/integration test xanh KHÔNG đủ — phải CHẠY Host thật + probe HTTP mới lộ lỗi tầng binding/middleware/wiring. "Browser-substitute" = docker dev + Invoke-WebRequest probe toàn endpoint từ OpenAPI. Nên biến thành cổng thường quy sau mỗi slice đụng Api/Host.
- Git: các thay đổi (fix QR-N-050 test + AD-103 base middleware + journal) CHƯA commit — chờ user duyệt.

### QR-N-052 — Design module Identity Login XONG (Wave F, design-first); CHƯA code — chờ user duyệt 2 quyết định
- Date: 2026-07-16
- Bối cảnh: gap login (QR-N-051) chặn admin end-to-end. Trước khi code AUTH (nhạy cảm bảo mật), làm design-first + valid mọi hợp đồng.
- Đã VALID (đọc code thật): Argon2idPasswordHasher + IJwtTokenService + ITokenGenerator SẴN ở base (AddBedrockSecurity + RequirePort); claim `role`=admin|staff (StarHillPolicies + JwtTestTokens); IRefreshTokenStore.AddAsync + RefreshTokenSnapshot; IRepository.FirstOrDefaultAsync đủ lookup username (F9); Identity chưa có User entity.
- Design `design-modules/06-identity-login.md` (diagnostics 0): IdentityUser tối giản (QR-AD-038) + LoginUseCase (generic-error + timing-defense, QR-AD-039) + endpoint POST /v1/identity/token/login + seed admin dev-only + refresh-role follow-up (F.2). Build slices F.1a/b/c + F.2.
- **CHỜ USER DUYỆT trước khi code**: (a) cơ chế seed admin prod (đề xuất: seeder dev-only, prod out-of-band) — QR-AD-039; (b) lockout: v1 dựa rate-limiter base đủ chưa hay cần account-lockout đếm-lần-sai.
- **FINDING drift (flag, chưa fix)**: có HAI host `StarHill.Api` — `platform/src/Host/StarHill.Api` (trong base Foundation.slnx, host tối giản của base) + `starhill/src/Host/StarHill.Api` (sản phẩm, chạy thật). Base domain-agnostic KHÔNG nên chứa host tên sản phẩm "StarHill.Api" → đề xuất đổi tên host base thành generic (vd `Bedrock.Sample.Api`) ở slice dọn base riêng (pre-existing, không vỡ chức năng; host chạy = starhill).
- NEXT: user duyệt (a)/(b) → code F.1a (Domain+persistence) → F.1b (login+endpoint) → F.1c (seeder+Host+verify runtime probe login→token→admin 2xx) → F.2 (refresh-role). Sau Identity login: còn Concierge + Housekeeping (2/8 module) để "BE done".

### QR-N-053 — Slice F.1a XONG: IdentityUser persistence + DỌN migration trùng (2 session tạo migration chưa-commit)
- Date: 2026-07-16
- Đã làm (F.1a): `IdentityUser : AuditableEntity` (Username lower-unique, PasswordHash Argon2, Role enum→string, IsActive, DisplayName, audit tự set) + `UserRole` (Admin/Staff) + `IdentityUserConfiguration` (bảng `app_user`, unique `ux_identity_user_username`) + DbSet + `ApplyConfigurationsFromAssembly` + repo keyed. Migration `AddIdentityUser` (app_user + audit columns + unique; KHÔNG drop bảng khác).
- **SỰ CỐ + FIX TẬN GỐC (drift chống được)**: phát hiện TRÊN ĐĨA có SẴN migration `20260716064738_AddIdentityUsers` (bảng `users`, Entity+CreatedAt) + `IdentityPostgresConstraintTests` do một phần session TRƯỚC (ngoài context sau khi bị nén) tạo — **CHƯA commit** (untracked). Khi tôi làm lại F.1a (bảng `app_user`, AuditableEntity) → `dotnet ef migrations add` sinh migration thứ hai có `DropTable users`+`CreateTable app_user` (cảnh báo "may result in loss of data"). Gốc rễ: HAI migration cho cùng entity + snapshot chỉ khớp cái cuối → áp cả hai = tạo rồi drop rồi tạo lại (bẩn/nguy hiểm). Fix: xóa CẢ 2 migration untracked + `git checkout` ModelSnapshot về committed (pre-IdentityUser) + regenerate MỘT migration sạch từ base rỗng. Reconcile 1 thiết kế: `app_user` (singular đúng convention `room`/`faq_item`; tránh reserved word `user`) + AuditableEntity (audit trail tài khoản admin — giá trị thương mại). Test cũ giữ nguyên (dùng LINQ, không ref tên bảng; CreatedAt settable ở AuditableEntity).
- **BÀI HỌC chống drift (quan trọng)**: work chưa-commit vắt qua nhiều session → sinh MIGRATION TRÙNG (mỗi session `ef migrations add` một cái). Quy tắc rút ra: **commit ngay slice có migration** (đừng để untracked qua session) + luôn `git status` migration trước khi `ef migrations add`. Đây là lý do commit F.1a NGAY.
- Gate (THỰC ĐO): build Platform.slnx 0-warning/0-error; `Identity.IntegrationTests` **3/3 Docker** (PendingModelChanges guard model==snapshot ✓ + round-trip + unique username). QR-AD-038 vẫn Proposed (role→claim + login = F.1b).
- NEXT — F.1b: `LoginUseCase` (lookup username → Verify Argon2 [timing-safe] → phát access[sub+role]+refresh family) + `AuthErrors.InvalidCredentials` (+snapshot) + endpoint `POST /v1/identity/token/login` + `LoginUseCaseTests` (Argon2 thật) + endpoint test.

### QR-N-054 — Slice F.1b XONG: LoginUseCase + endpoint /token/login (Argon2 + JWT role-claim, generic-error + timing-defense)
- Date: 2026-07-16
- Đã làm: `LoginUseCase` (IUseCase tự-transaction): lookup username (lower-normalize, IRepository.FirstOrDefault) → Verify Argon2 → phát access-token (`sub`+`role`) + refresh-token gia đình MỚI. `AuthErrors.InvalidCredentials` (mã mới, +snapshot). `LoginCommand/Result` + validator. Endpoint `POST /v1/identity/token/login` (AllowAnonymous, no-store, rate-limiter base slot #9). Đăng ký factory keyed.
- Chống enumeration (QR-AD-039 phần login): (1) MỌI fail (user lạ/sai pass/inactive) → CÙNG `identity.invalid_credentials`; (2) user lạ vẫn Verify dummy-hash (tạo bằng CHÍNH Argon2 production, cache process-wide) → cân bằng timing.
- Fix-at-root chống drift: (a) hash refresh gom về `RefreshTokenHashing.Sha256Hex` DÙNG CHUNG login+refresh (nguồn duy nhất — login-lưu-hash phải khớp refresh-tra-hash); refactor RefreshAccessTokenUseCase dùng chung (test refresh re-verify). (b) role→claim: DERIVE `UserRole.ToString().ToLowerInvariant()` (KHÔNG ref StarHill.Authorization vào Application vì nó FrameworkReference ASP.NET → phá I7); GUARD hợp đồng bằng `RoleClaimContractTests` (enum-name==StarHillPolicies.RoleAdmin/RoleStaff → fail build nếu lệch).
- Gate (THỰC ĐO): build 0-warning; `Identity.IntegrationTests` **8/8 Docker** (5 login: đúng-cred→token+role=admin+refresh-persist; sai-pass/user-lạ/inactive→invalid_credentials; case-insensitive) + 3 cũ; `StarHill.Api.Tests` **62/62** (+2 RoleClaimContract); `Bedrock.ContractTests` 2/2 (snapshot +identity.invalid_credentials).
- QR-AD-038 → **Implemented** (entity + role-claim done + guarded). QR-AD-039 VẪN Proposed: generic-error+timing-defense DONE (LoginUseCaseTests), CÒN seed admin (F.1c) → mới đủ Implemented.
- NEXT — F.1c: `IdentityUserSeeder` (dev-gated, config credential, idempotent) + Host wiring (migrate+seed gated) + compose/appsettings + seeder test + **verify runtime probe THẬT: login→access-token→gọi admin endpoint (Rooms POST) trả 2xx** (đóng vòng end-to-end admin — mục tiêu FE dùng được). Rồi F.2 (refresh-role).

### QR-N-055 — Slice F.1c XONG: seed admin dev + Host wiring + VERIFY runtime end-to-end (login→token→admin 2xx)
- Date: 2026-07-16
- Đã làm: `IdentityUserSeeder` (idempotent, băm Argon2, chỉ tạo khi chưa có) + đăng ký scoped + Host wiring (Program.cs: seed CHỈ khi có CẢ Identity:SeedAdmin:Username/Password → prod không cấu hình = không seed, F35) + `docker-compose.dev.yml` đặt SeedAdmin dev (admin/DevAdmin!2026, placeholder). Test `IdentityUserSeederTests` (Postgres, idempotent: chạy 2 lần → 1 admin, password đã băm, role Admin).
- **VERIFY RUNTIME (browser-substitute, Docker THẬT — mấu chốt user cần)**: rebuild Host (dev+seed) → probe HTTP: (1) `POST /v1/identity/token/login {admin/DevAdmin!2026}` → **200** + accessToken(303)+refreshToken(43); (2) `GET /v1/resort/settings` KÈM Bearer admin-token → **200** (role-claim `admin` khớp policy RequireAdmin → authorize OK — chứng minh CHUỖI login→JWT-role→policy→admin-endpoint hoạt động end-to-end); (3) không token → 401; (4) sai password → 401 invalid_credentials. ⇒ **Vòng admin end-to-end ĐÓNG** — mở khóa toàn bộ mặt admin + FE (đây chính là gốc "mở web không dùng được" trước đó: thiếu login).
- Gate: build 0-warning; `Identity.IntegrationTests` **9/9 Docker** (5 login + seeder + 3 cũ); `StarHill.Api.Tests` 62/62. QR-AD-039 → **Implemented** (seeder + generic-error/timing đủ).
- **LOGIN (F.1) HOÀN TẤT** (F.1a persistence + F.1b use case/endpoint + F.1c seeder/wiring/e2e). Admin đăng nhập được thật.
- NEXT: **F.2** (refresh-role: RefreshAccessTokenUseCase load user theo UserId → thêm claim role + kiểm IsActive → auth đúng SAU refresh; hiện refresh mất role). Rồi module **Concierge** + **Housekeeping** + **Dashboard** để "BE done" → chọn FE template.

### QR-N-056 — Slice F.2 XONG: refresh giữ role + chặn user vô hiệu; helper claim dùng chung (auth đúng end-to-end)
- Date: 2026-07-16
- Đã làm (QR-AD-040): `RefreshAccessTokenUseCase` +`IRepository<IdentityUser>`; sau reuse-detection/trước consume nạp user → `null||!IsActive`→InvalidRefreshToken; phát access-token qua helper CHUNG `IdentityClaims.Build(userId, role)` (login + refresh dùng chung → nguồn dựng claim duy nhất). Bỏ `BuildIdentity` riêng ở cả 2 use case + xóa using thừa.
- Verify: build 0-warning; `Identity.UnitTests` **9/9** (+2: missing-user/inactive-user→fail-không-consume); `Identity.IntegrationTests` **9/9 Docker** (RefreshRotationEmitsEventTests cập nhật seed user active). **Runtime e2e (Docker)**: login→refresh(rotation)→access-token mới KÈM Bearer gọi `GET /v1/resort/settings` → **200** (ROLE GIỮ qua refresh); reuse refresh cũ → **401** (reuse-detection). ⇒ vòng đời auth đầy-đủ đúng.
- **AUTH (F.1+F.2) HOÀN TẤT**: login (role) → dùng admin → refresh (role bền) → dùng admin; user vô hiệu → refresh chết; reuse token → 401. Admin surface + FE sẵn sàng dùng thật.
- Ghi chú (chưa làm, không chặn): revoke-family TỨC THÌ khi vô hiệu hoá user (hiện session chết ở lần refresh kế trong ≤ access-TTL) — làm khi có màn quản trị user (admin-user-mgmt slice).
- NEXT: module **Concierge** (chat khách↔lễ tân + ChatHub) hoặc **Housekeeping** (yêu cầu dịch vụ theo phòng) — 2/8 module cuối; cả hai là consumer của `IRuleGate` (rule-ack) + `ICurrentGuestContextResolver`. Rồi Dashboard. Sau đó "BE done" → chọn FE template.

### QR-N-057 — Design module Housekeeping XONG (Wave G, design-first); CHƯA code
- Date: 2026-07-16
- Bối cảnh: BE 6/8 module (+ Auth Wave F). Module kế theo dependency graph + độ contained = **Housekeeping** (không SignalR
  như Concierge; tái dùng ngay `IRoomTokenResolver`/`IRuleGate`/`ICurrentGuestContextResolver`/config; mở đường C-GA.5 cascade).
  DESIGN-FIRST: `design-modules/06-housekeeping.md` → diagnostics 0 → đọc lại valid → chờ user duyệt trước code.
- Đã VERIFY chữ ký thật TRƯỚC khi thiết kế (không suy đoán): `Rooms.Contracts.IRoomTokenResolver.ResolveActiveTokenAsync`
  → `RoomResolution(RoomId,ResortId,RoomNumber,Building?,Floor?,IsRoomActive)` (complete-by-token); `IRuleGate` enum có
  `Housekeeping`; `ICurrentGuestContextResolver`; `IResortGuestConfigQuery` (HousekeepingEnabled+RequireRuleAckForHousekeeping);
  `IResortSettingsQuery` (HousekeepingRateLimitPerHour). Data model Housekeeping lấy từ `docs/resort-qr-portal/design.md`
  §Data Models (HousekeepingTicket[Status Requested/InProgress/Done/Cancelled + CompletionMethod App/StaffScan] +
  HousekeepingEvent[log mỗi transition]) + §Constraints (1 ticket mở/phòng unique) + §API. Req 6 đầy đủ.
- Quyết định thiết kế nổi bật (cấp số QR-AD/DV/TO thật khi code — INV-3 tránh forward-ref):
  1. **1-ticket-mở/phòng** bằng **partial unique** `ux_hk_open_ticket_room WHERE Status IN (Requested,InProgress)` (race-safe,
     cùng lớp CP2/ux_qr_active) + create **idempotent** (bắt UniqueConstraintViolationException → trả ticket mở existing, không lỗi).
  2. Rule-gate (Housekeeping) enforce TRONG use case (defense-in-depth CP3, mirror Faq); Application ref Rules.Contracts + Rooms.Contracts.
  3. Enum lưu **string** (HasConversion) để partial-filter đọc được + ổn định (verify precedent Rooms lúc code).
  4. **Log mỗi transition** (HousekeepingEvent) CÙNG transaction với đổi Status (Req 6.8 đối soát).
  5. complete-by-token dùng `IRoomTokenResolver` (QR chỉ định phòng, KHÔNG thay đăng nhập — Req 6.5); complete-by-room/set-status; ghi CompletedByUserId+CompletionMethod.
  6. **Cascade huỷ khi visit-end (CP9/Req 10.8) TÁCH sang C-GA.5** (event-driven outbox/inbox) — module cấp use case
     `CancelOpenTicketsForVisitUseCase` (capability + test) nay, WIRING consumer sau (defer — cần outbox GuestAccess). Không premature-couple.
  7. GET guest status KHÔNG touch (đọc phụ trợ như Rules-GET); POST create touch-sau-thành-công. Rate-limit dựa 1-mở/phòng + Bedrock global IP limiter (không bảng riêng).
- Bằng chứng: `design-modules/06-housekeeping.md` tạo xong, **getDiagnostics = 0**. Chưa viết C#. Mỗi CP/bất biến map guard
  test + Docker? + slice (§7/§8). Mã lỗi mới `HousekeepingErrors` (housekeeping_disabled/no_open_ticket/ticket_not_found/
  invalid_transition) sẽ vào ErrorCodeSnapshotTests khi code.
- **NEXT**: chờ user duyệt design §1/§3/§4/§9. Nếu đồng ý/không phản hồi → slice **H-Hk.1** (Domain/Contracts/Persistence +
  migration partial-unique/xmin + boundary + Postgres constraint) rồi H-Hk.2 (Application) → H-Hk.3 (Api + Host). C-GA.5 cascade sau.

### QR-N-058 — Slice H-Hk.1 XONG: Housekeeping persistence nền (Domain/Contracts/Infrastructure + migration + boundary + Postgres constraint)
- Date: 2026-07-16
- Bối cảnh: hiện thực module Housekeeping theo design QR-N-057. Mirror staging Rules/Faq (persistence nền TRƯỚC, KHÔNG
  wire Host — Host wiring để slice có Api = H-Hk.3).
- Đã làm:
  1. `Housekeeping.Domain` (2 entity + 3 enum): `HousekeepingTicket : Entity, IHasConcurrencyToken` (ResortId/RoomId/
     GuestSessionId?/GuestVisitId?/CompletedByUserId? Guid trần; Status + CompletionMethod?; timestamps) + `HousekeepingEvent
     : Entity` (append-only nhật ký, không concurrency) + enum `HousekeepingStatus{Requested,InProgress,Done,Cancelled}`/
     `HousekeepingCompletionMethod{App,StaffScan}`/`HousekeepingActorType{Guest,Staff,System}`.
  2. `Housekeeping.Contracts` (`HousekeepingModule.PersistenceKey="housekeeping"`).
  3. `Housekeeping.Infrastructure`: `HousekeepingDbContext:PlatformDbContext` schema `housekeeping` keyed + Factory +
     `HousekeepingConfigurations` (**partial unique `ux_hk_open_ticket_room` filter `status IN ('Requested','InProgress')`**
     — 1 ticket mở/phòng Req 6.2; enum lưu string HasConversion mirror Rooms; FK Cascade event→ticket; index resort/status
     + event/ticket) + `AddHousekeepingInfrastructure` (persistence + 2 keyed repo).
  4. Migration `InitialCreate` (dotnet ef, không cần DB). VERIFY grep: 2 CreateTable; xid rowVersion ticket; ux_hk_open_ticket_room
     unique filter đúng; FK Cascade; 2 index.
  5. Test: `HousekeepingBoundaryTests` (StarHill.ArchitectureTests, 3) + `HousekeepingPostgresConstraintTests` (2 SkippableFact
     Postgres: 2-ticket-mở-cùng-phòng→vi phạm + Done giải phóng slot; Cancelled giải phóng slot). `Platform.slnx` +3 project
     +Housekeeping.IntegrationTests; arch-test csproj +3 ref.
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail — StarHill.ArchitectureTests 28 (+3 Housekeeping),
  Housekeeping.IntegrationTests 2 skip(Postgres không-Docker), không regression. `vp journal` INV-1..6 xanh. QR-AD-041
  (Housekeeping foundation) Status Implemented + Guard-Tests `HousekeepingBoundaryTests`/`HousekeepingPostgresConstraintTests` (INV-6 tồn tại thật).
- **NEXT — slice H-Hk.2**: `Housekeeping.Application` (RequestHousekeeping guest idempotent + rule-gate + flag; máy trạng thái
  complete-by-room/token/set-status + event-log; guest status read; CancelOpenTicketsForVisit capability; read-model IHousekeepingReader)
  + đổi Infra ref→Application + validator + tests (SQLite + Postgres concurrency) + ErrorCodeSnapshotTests +mã Housekeeping. Rồi H-Hk.3 (Api + Host).

### QR-N-059 — Slice H-Hk.2 XONG: Housekeeping.Application (create idempotent + rule-gate + máy trạng thái + event-log + complete app/token + cancel-for-visit)
- Date: 2026-07-16
- Bối cảnh: tiếp H-Hk.1, hiện thực use case theo design §4. Mirror Faq/Rules.Application (Result API, IRepository trực tiếp,
  IUseCase value-returning, bắt UniqueConstraintViolationException, cross-module qua Contracts).
- Đã làm:
  1. `Housekeeping.Application` (ref Domain+Contracts+Rooms.Contracts+Rules.Contracts+ResortConfig.Contracts+Bedrock.Application+FluentValidation):
     `HousekeepingErrors` (housekeeping_disabled/no_open_ticket/ticket_not_found/invalid_transition + **tái dùng qr_invalid/
     configuration_unavailable**) + `HousekeepingContracts` + `HousekeepingStateMachine` (luật chuyển + tạo event, nguồn DUY NHẤT)
     + `IHousekeepingReader`/`EfHousekeepingReader`.
  2. Use case: `RequestHousekeepingUseCase` (guest: config→HousekeepingEnabled→**rule-gate Housekeeping**→idempotent [ticket mở→trả existing;
     race→bắt unique→existing]); `SetHousekeepingStatusUseCase` (InProgress/Done + timestamps + event, xmin CP15); `CompleteHousekeepingByRoomUseCase`
     (method App); `CompleteHousekeepingByTokenUseCase` (`IRoomTokenResolver`→RoomId, method StaffScan; token lạ→qr_invalid);
     `CreateHousekeepingByStaffUseCase` (Req 6.9, idempotent, không gate/flag); `CancelOpenTicketsForVisitUseCase` (cascade capability CP9, System);
     `GetRoomHousekeepingStatusUseCase` (read-model). Helper `HousekeepingCompletion` dùng chung complete-by-room/token. Đổi Infra ref→Application + đăng ký keyed.
  3. `ErrorCodeSnapshotTests` +assembly Housekeeping + 4 mã (Ordinal giữa guest_context_missing/identity.*). `HousekeepingBoundaryTests` +Application⊥Infra/EF/ASP.NET.
  4. Test `HousekeepingUseCaseTests` (SQLite, 13: config-null/disabled/gate-fail/create+idempotent; máy trạng thái Requested→InProgress→Done +
     3 event + Done→InProgress invalid; not-found; complete-by-room App + no-open; complete-by-token StaffScan + qr_invalid; cancel-for-visit; guest status; create-by-staff idempotent)
     + `HousekeepingConcurrencyTests` (Postgres xmin).
- Fix biên dịch (tận gốc): (a) `Error` là CLASS → `Error?` nullable-reference, dùng `error` không `error.Value`; (b) ternary `? enum : null` cast `(HousekeepingCompletionMethod?)null`.
- **Fix bug thật (provider-portability, fix tận gốc):** `Get_room_status` fail — **SQLite KHÔNG hỗ trợ ORDER BY DateTimeOffset** (chỉ Postgres).
  Read-model đổi order `CreatedAt DESC` → **`Id DESC` (UUIDv7 time-ordered by design — Entity sinh Guid.CreateVersion7)** = "mới nhất" TƯƠNG ĐƯƠNG
  nhưng PROVIDER-AGNOSTIC. **Trade-off (ghi nhận):** "latest" nay phụ thuộc bất biến UUIDv7-monotonic của Id thay vì CreatedAt tường minh —
  chấp nhận vì (1) Id LUÔN là v7 (Entity ctor), (2) tránh order client-side unbounded, (3) không để giới hạn provider-test chi phối. Nếu đổi cách sinh Id → phải xem lại.
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail — Housekeeping.IntegrationTests 13 pass/3 skip(Postgres),
  StarHill.ArchitectureTests 29 (+1 Application boundary), Bedrock.ContractTests 2 (snapshot có 4 mã Housekeeping). `vp journal` INV-1..6 xanh.
  QR-AD-041 Guard-Tests +HousekeepingUseCaseTests/HousekeepingConcurrencyTests.
- **NEXT — slice H-Hk.3** (Api + Host): guest endpoints (POST /v1/guest/housekeeping create + GET status, rule-gate, touch) + admin endpoints
  (GET board phân trang + complete-by-room/token + set-status + create-by-staff, RequireStaff) + `AddHousekeepingApi` + Host wire (conn Housekeeping +
  migrate) + CI bundle + `HousekeepingEndpointAuthTests` + `HostEndpointWiringSmokeTests` +InlineData + board read-model (paged). Rồi C-GA.5 cascade wiring.

### QR-N-060 — Slice H-Hk.3 XONG: Housekeeping Api + Host wiring (guest tạo/xem + admin board/complete/status); module Housekeeping hoàn tất guest+admin — BE 7/8
- Date: 2026-07-16
- Bối cảnh: mắt xích cuối mặt guest+admin module Housekeeping. Mirror Faq/Rules Api.
- Đã làm:
  1. Read-model: `IHousekeepingReader` +`ListBoardAsync(resortId,status?,paging)` → `PagedResult<HousekeepingBoardItem>`
     (order Id-v7 ASC = FIFO cũ-nhất-trước, provider-agnostic). `EfHousekeepingReader` impl.
  2. `Housekeeping.Api`: `HousekeepingAdminEndpointModule` (GET board phân trang + POST tạo-chủ-động + POST {id}/status +
     complete-by-room + complete-by-token — TẤT CẢ RequireStaff; actor=ICurrentUser.UserId; resortId server qua IResortSettingsQuery)
     + `HousekeepingGuestEndpointModule` (POST /v1/guest/housekeeping tạo [rule-gate trong use case] + GET status; resolve context;
     **POST touch-sau-thành-công / GET không-touch** [QR-N-045]; no-store) + `AddHousekeepingApi`.
  3. Host: +conn `Housekeeping` + AddHousekeepingInfrastructure/Api + migrate. csproj Host + Platform.slnx +Housekeeping.Api.
     appsettings + docker-compose +`ConnectionStrings__Housekeeping`; starhill-ci.yml +bundle `housekeeping`.
  4. Test: `HousekeepingEndpointAuthTests` (TestServer + fake, 4: Staff board/create/status/complete 2xx; no-token admin→401;
     guest tạo/xem 200 no-token; guest resolver-fail→problem không-401) + `HostEndpointWiringSmokeTests` +4 InlineData (3 admin→401 +
     guest→problem+json). +7 fake Housekeeping (6 use case + reader) vào file fakes chung.
- Fix tận gốc phát sinh: `Staff_can_operate_housekeeping` fail — TestServer thiếu `JsonStringEnumConverter` → body {status:"InProgress"}
  bind HousekeepingStatus fail 400. Thêm `ConfigureHttpJsonOptions(JsonStringEnumConverter)` khớp Host (QR-AD-021) — không vá ngọn.
- Bằng chứng: `vp all` build 0-warning + validate-ci OK + full suite 0-fail — StarHill.Api.Tests 70 (+8), Housekeeping.IntegrationTests
  13 pass/3 skip(Postgres). `vp journal` INV-1..6 xanh. QR-AD-041 Guard-Tests +HousekeepingEndpointAuthTests. **Module Housekeeping HOÀN TẤT
  mặt guest+admin — BE 7/8 module** (Identity/ResortConfig/Rooms/GuestAccess/Rules/Faq/**Housekeeping**).
- **NEXT**: (a) **C-GA.5 cascade** (GuestVisitEnded outbox GuestAccess → consumer gọi `CancelOpenTicketsForVisitUseCase` [đã có capability] +
  đóng hội thoại Concierge khi có) — cần outbox GuestAccess + consumer; (b) **Concierge** (chat SignalR — module cuối, phức tạp nhất);
  (c) **Dashboard** (thống kê ghép Host); (d) E-Faq.4b admin read; (e) Frontend Vue. Đề xuất: Concierge (để đủ 8/8 + kích hoạt trọn C-GA.5), rồi Dashboard, rồi FE.

### QR-N-061 — Design-first module Concierge (Wave K, module nghiệp vụ CUỐI 8/8); diagnostics 0
- **design-modules/07-concierge.md** tạo xong, `getDiagnostics` = **0**. Bản đồ đầy đủ, tự-valid (§10) dựa file/code đã ĐỌC (không suy đoán). Chi tiết hóa `design.md` §2 module #7 Concierge (QR-AD-004 — Messaging+Notes gộp) + Req 5 (nhắn tin gom theo phòng, 1 hội thoại/visit, reopen, realtime) + Req 9.4 (ghi chú nội bộ) + Req 10.8/CP9 (cascade đóng hội thoại khi visit-end) + Req 3.11 (rule-gate `/messages`) + Req 14 (ChatEnabled/RequireRuleAckForChat/MaxMessageLength/MessageRateLimitPerMinute).
- **Đối soát nguồn đã verify (§0)** — chữ ký Contracts THẬT đã đọc: `Rules.Contracts.IRuleGate.EnsureAcknowledgedAsync(resortId, guestVisitId, GuestFeature.Chat, ct)` (enum có `Chat`); `GuestAccess.Contracts.ICurrentGuestContextResolver.ResolveAsync(sessionKey, roomId)`→`Result<CurrentGuestContext>`+`TouchAsync`; `ResortConfig.Contracts.IResortGuestConfigQuery.GetAsync`→`ResortGuestConfig(ChatEnabled, RequireRuleAckForChat)` + `IResortSettingsQuery.GetAsync`→`ResortSettingsSnapshot(MaxMessageLength, MessageRateLimitPerMinute)`; `ICurrentUser` (actor staff). **SignalR CHƯA có trong codebase** (grep 0) → realtime là năng lực MỚI, thêm ở Host (K-Con.4).
- **8 quyết định thiết kế chốt (dùng placeholder `QR-AD-0xx`/`QR-TO-0xx` trong doc — AN TOÀN INV-3, số thật cấp lúc code)**:
  1. Tên "Concierge" schema `concierge` keyed (QR-AD-004) — tránh đụng `Bedrock.Messaging.Contracts` (bus hạ tầng base).
  2. Unique `Conversation(GuestVisitId)` = **1 hội thoại/visit + reopen** (guest gửi lại sau close mà visit còn Active → mở lại đúng hội thoại đó, KHÔNG tạo mới; race tạo đầu → bắt `UniqueConstraintViolationException`→load existing).
  3. **Body PLAIN TEXT KHÔNG sanitize** (KHÁC Rules/FAQ) — chat là text; sanitize sẽ mangle `<`/`>` hợp lệ; client render bằng `textContent` (auto-escape), KHÔNG `innerHTML`. Chỉ trim + giới hạn `MaxMessageLength`. Trade-off ghi rõ (dựa client render đúng vs sanitize backend).
  4. Rule-gate `GuestFeature.Chat` (CP3, Req 3.11) + flag `ChatEnabled` (Req 14) — enforce BÊN TRONG use case (defense-in-depth), tắt→`chat_disabled`, chưa ack→`rule_ack_required`.
  5. Realtime qua **port `IConciergeRealtimeNotifier`** (Application THUẦN, no-op default ở Infrastructure, `SignalRConciergeNotifier` override ở Host/Api) → testable + SignalR isolated. **Polling `GET /conversation` LUÔN là fallback** (realtime chỉ tăng tốc, không phải nguồn sự thật).
  6. `UnreadForStaff` denormalize trên Conversation (guest gửi→++; staff read→0) — badge dashboard (Req 5.3).
  7. Enum lưu **string** (HasConversion, nhất quán Rooms/Housekeeping) + order read-model theo **Id-v7** (KHÔNG OrderBy DateTimeOffset — bài học SQLite QR-N-059).
  8. Cascade `CloseConversationForVisitUseCase` = capability nay, **wiring event-driven = C-GA.5** (cùng lúc Housekeeping `CancelOpenTicketsForVisitUseCase`, đóng CP9/QR-AD-027 trọn vẹn).
- **Slice build**: K-Con.0 (design, file này) → K-Con.1 (Domain/Contracts/Persistence + migration unique-visit + FK + xmin + boundary + Postgres constraint) → K-Con.2 (Application core: send create/reopen + gate + flag + unread + reply/read/close + notes CRUD + read-model + notifier no-op + tests + ErrorCodeSnapshot) → K-Con.3 (Api REST guest+admin + Host wiring + CI bundle + endpoint auth + smoke) → K-Con.4 (SignalR ChatHub + notifier impl + auth-on-join) → C-GA.5 (cascade cross-module cuối).
- **VERIFY khi code (chưa chắc, phải đọc lúc impl — KHÔNG bịa)**: (1) chữ ký chính xác `IResortGuestConfigQuery`/`ResortGuestConfig` field `ChatEnabled`/`RequireRuleAckForChat` (đọc lại lúc K-Con.2); (2) `MessageRateLimitPerMinute` thật có trong `ResortSettingsSnapshot` không (nếu chưa → dùng Bedrock global IP limiter + MaxMessageLength, QR-TO chốt lúc code); (3) cách wire SignalR trên `Bedrock.Api`/Host (AddSignalR + MapHub) + auth-on-join resolve context.
- **DESIGN-FIRST tôn trọng**: CHƯA viết code C# — chờ K-Con.1. Thiết kế tự-valid (§10 checklist) — chỉ mục cuối "user review" để ngỏ (standing continue).
- **NEXT increment**: slice **K-Con.1** — mirror khuôn Housekeeping H-Hk.1: 3 entity (Conversation `IHasConcurrencyToken`, Message append-only, InternalNote `IHasConcurrencyToken`) + 3 enum string + `ConciergeDbContext` schema `concierge` keyed + Factory + migration (`ux_conversation_visit` unique + index + FK Cascade Message→Conversation + FK Restrict InternalNote→Conversation + xmin) + `AddConciergeInfrastructure` + `ConciergeBoundaryTests` + `ConciergePostgresConstraintTests`. Verify `vp all` + `vp journal` xanh → cấp QR-AD thật (module foundation) → journal → commit path tường minh → push verify `0 0`.

### QR-N-062 — Slice K-Con.1 XONG: Concierge persistence nền (Domain/Contracts/Infrastructure + migration + boundary + Postgres constraint)
- **Đã build + verify** (`vp all` xanh: build 0-warning + validate-ci OK + full suite 0-fail; `vp journal` INV-1..6 xanh):
  - **Concierge.Domain** (ref CHỈ Bedrock.Domain): `Conversation` (`: Entity, IHasConcurrencyToken` — xmin CP15; unique GuestVisitId; UnreadForStaff denormalize; LastMessageAt/LastGuest/LastStaff; ClosedAt?/ClosedByUserId?), `Message` (`: Entity` append-only KHÔNG concurrency; ConversationId FK; SenderType; Body plain text; ReadByStaffAt?/ReadByGuestAt?), `InternalNote` (`: Entity, IHasConcurrencyToken` — sửa được; RoomId?/ConversationId?; AuthorUserId; Body) + enum `ConversationStatus{Open,Closed}`/`MessageSenderType{Guest,Staff,System}`. Id trần mọi cross-schema.
  - **Concierge.Contracts** (ref CHỈ Bedrock.Messaging.Contracts): `ConciergeModule.PersistenceKey="concierge"` (nguồn duy nhất, mirror HousekeepingModule). Port cascade (C-GA.5) thêm sau.
  - **Concierge.Infrastructure** (ref Domain+Contracts+Bedrock.Infrastructure — K-Con.1 nền, K-Con.2 đổi sang Application): `ConciergeDbContext:PlatformDbContext` schema `concierge` + Factory (MigrationsHistoryTable "concierge" QR-AD-028 + UseSnakeCaseNamingConvention) + `ConciergeConfigurations` (3 IEntityTypeConfiguration) + `AddConciergeInfrastructure` (AddBedrockPersistence keyed + AddBedrockRepository ×3 entity; CHƯA use case).
  - **Migration** `20260716162819_InitialCreate` (verify đọc file thật): 3 CreateTable schema `concierge`; `ux_conversation_visit` unique(guest_visit_id) = 1 hội thoại/visit; FK **Cascade** message→conversation + FK **Restrict** internal_note→conversation; xmin (`xid` rowVersion) conversation+internal_note [message KHÔNG xmin — append-only]; ix_conversation_resort_room_last / ix_message_conversation / ix_note_resort_room; status+sender_type varchar(16); body text.
  - **Tests**: `ConciergeBoundaryTests` (StarHill.ArchitectureTests — Contracts thuần + Domain⊥Infra + negative control CrossModuleInternalLeak; Application assertion thêm K-Con.2) → ArchitectureTests 32 (+3). `ConciergePostgresConstraintTests` (Concierge.IntegrationTests, Testcontainers — 3 test: only-one-conversation-per-visit vi phạm unique; delete-conversation-cascade-messages; cannot-delete-conversation-referenced-by-note FK Restrict) → **3 skip máy này (không Docker), chạy thật CI**. Dùng `conversation.Id` (auto UUIDv7 đọc được) — KHÔNG reflection.
  - `starhill/Platform.slnx` +3 project src (Concierge.Contracts/Domain/Infrastructure) +1 test (Concierge.IntegrationTests). StarHill.ArchitectureTests.csproj +3 ProjectReference Concierge.
- **VERIFY đã chốt (design §10)**: `Entity` tự sinh `Id=Guid.CreateVersion7()` (đọc `platform/src/Bedrock.Domain/Entities/Entity.cs` — `Id` protected init, không set từ ngoài) → entity dùng object-initializer `required` + `{get;set;}`; `IHasConcurrencyToken.RowVersion` uint (xmin do PlatformDbContext tự map trên Npgsql); enum-string + FK khớp precedent Housekeeping. **QR-AD-042 Implemented** (Guard-Tests: ConciergeBoundaryTests + ConciergePostgresConstraintTests — INV-6 pass).
- **CHƯA có DB bundle CI**: `starhill-ci.yml` migration-bundle-job cần +bundle Concierge khi K-Con.3 (Host wiring) — hiện K-Con.1 chỉ persistence, chưa đụng Host. Ghi để không quên (mirror gap Housekeeping đã đóng ở H-Hk.3).
- **NEXT — slice K-Con.2**: Concierge.Application core. Đổi Infra ref Domain+Contracts→Concierge.Application (matrix Infra→Application). Use case: `SendGuestMessageUseCase` (config→ChatEnabled→rule-gate Chat→find-or-create/reopen Conversation unique-GuestVisitId + Message(Guest) + UnreadForStaff++ + LastMessageAt/LastGuestMessageAt; bắt UniqueConstraintViolationException→reopen existing; validate body ≤ MaxMessageLength; notifier.NotifyMessageReceived); `GetGuestConversationUseCase` (read-model, mark ReadByGuest); `ReplyConversationUseCase`/`MarkConversationReadUseCase`/`CloseConversationUseCase`/`CloseConversationForVisitUseCase` (staff/system); notes CRUD; read-model `IConciergeReader` (order Id-v7 — bài học QR-N-059, KHÔNG OrderBy DateTimeOffset); port `IConciergeRealtimeNotifier` (no-op default) + ErrorCodeSnapshot +mã Concierge. VERIFY lúc code: chữ ký thật `IResortGuestConfigQuery`/`ResortGuestConfig.ChatEnabled`/`RequireRuleAckForChat` + `ResortSettingsSnapshot.MaxMessageLength`/`MessageRateLimitPerMinute` (đọc lại — nếu thiếu MessageRateLimit thì dùng Bedrock global IP limiter, QR-TO chốt).

### QR-N-063 — Slice K-Con.2a XONG: Concierge Application guest core (send create/reopen/gate/flag/unread/plain-text + get scope + mark-read + notifier port)
- **Đã build + verify** (`vp all` xanh: build 0-warning + validate-ci OK + full suite 0-fail; `vp journal` INV-1..6 xanh; Concierge.IntegrationTests **13 pass** SQLite + 3 skip Postgres no-Docker):
  - **Concierge.Application** (ref Domain+Contracts+Rules.Contracts+ResortConfig.Contracts+Bedrock.Application+FluentValidation): `ConciergeErrors` (chat_disabled/concierge_message_empty/concierge_message_too_long/concierge_conversation_not_found + configuration_unavailable tái dùng); `ConciergeContracts` (guest records: SendGuestMessageInput/Result + GetGuestConversationInput/Result + GuestConversationView/GuestMessageView); `IConciergeRealtimeNotifier` (port 3 method Id-trần); `IConciergeReader` (GetGuestConversationByVisit + ListMessageIdsUnreadByGuest); `SendGuestMessageUseCase` + `GetGuestConversationUseCase`.
  - **Concierge.Infrastructure** đổi ref Domain+Contracts→**Concierge.Application** (matrix Infra→Application). `EfConciergeReader` (no-tracking, order Id-v7 — bài học QR-N-059) + `NoOpConciergeRealtimeNotifier` (default; Host override K-Con.4). `AddConciergeInfrastructure` +reader +notifier no-op +2 use case (factory keyed).
  - **QR-AD-043 Implemented** (Guard-Tests: ConciergeUseCaseTests + ConciergeBoundaryTests) — chốt: body PLAIN-TEXT không sanitize; realtime qua port Application-thuần; rule-gate+flag trong use case; two-config-source (IResortGuestConfigQuery ChatEnabled + IResortSettingsQuery MaxMessageLength); create-race recovery không mất tin (Remove-untrack+refind+append); GetGuestConversation mark-ReadByGuest có điều kiện.
  - **Tests**: `ConciergeUseCaseTests` (SQLite local, 13): config-null/chat-disabled/gate-fail/empty/too-long/create-unread-1/append-1-hội-thoại/reopen-cùng-hội-thoại/plain-text-`<script>`-lưu-nguyên/notifier-1-lần-sau-persist/get-null/get-scope-theo-visit/get-mark-staff-read. `ConciergeBoundaryTests` +assertion Application ⊥ Infra/EF/ASP.NET/**SignalR** (I7 — realtime qua port, không chạm IHubContext) → StarHill.ArchitectureTests 33. `ErrorCodeSnapshotTests` +assembly Concierge +4 mã.
  - **VERIFY đã chốt**: chữ ký `IRuleGate`/`IResortGuestConfigQuery`(ChatEnabled)/`IResortSettingsQuery`(MaxMessageLength=default 2000) đọc THẬT; `Result.Success/Failure`/`IsSuccess`/`IsFailure`/`Error.Code`; `CommonErrors.Conflict()`→"conflict"; `IRepository` không ListAsync → multi-update qua reader→ids→FindByIdAsync. `MessageRateLimitPerMinute` CÓ trong snapshot (chưa dùng — rate-limit chốt ở K-Con.3 Api: Bedrock global IP limiter vs limiter chuyên biệt, QR-TO).
- **NEXT — slice K-Con.2b**: staff use case (`ReplyConversationUseCase` [Message(Staff)+LastStaffMessageAt+Status=Open reopen-nếu-Closed+notifier ConversationUpdated], `MarkConversationReadUseCase` [UnreadForStaff=0 + ReadByStaffAt các tin guest + notifier MessageRead], `CloseConversationUseCase` [Status=Closed+ClosedAt/ClosedByUserId+notifier], `CloseConversationForVisitUseCase` [System, cascade C-GA.5 — đóng hội thoại Open của visit]) + notes CRUD (`CreateInternalNote`/`UpdateInternalNote`/`DeleteInternalNote`) + board read-model (`ListConversationsAsync` paged, order Id-v7 + `GetConversationAsync` chi tiết) + validators. Dùng `ConversationNotFound` (đã khai). Rồi K-Con.3 (Api guest+admin + Host wiring + CI bundle) → K-Con.4 (SignalR).

### QR-N-064 — Slice K-Con.2b XONG: Concierge staff use case + notes CRUD + board read-model (module Concierge Application HOÀN TẤT)
- Date: 2026-07-17
- Bối cảnh: tiếp K-Con.2a (guest core), hiện thực mặt NHÂN VIÊN + ghi chú nội bộ + board dashboard theo design §4/§9. Mirror pattern Rules/Housekeeping (Result API, IRepository trực tiếp, split IUseCase value-returning vs ICommandUseCase void, cross-module qua Contracts).
- Đã làm (verify bằng build + test THẬT):
  1. `ConciergeErrors` +`NoteNotFound` (`concierge_note_not_found`, 404 — mã MỚI cho update/delete note theo id).
  2. `IConciergeReader` +3 method: `ListMessageIdsUnreadByStaffAsync` (tin Guest chưa ReadByStaff — cho mark-read); `ListConversationsAsync(resortId, status?, page, pageSize)→PagedConversations` (board, order **LastMessageAt DESC** + ThenByDescending Id tie-break, clamp page/pageSize); `GetConversationAsync(id)→ConversationDetailView?` (chi tiết staff-facing, tin order Id-v7, LỘ SenderUserId). `EfConciergeReader` impl (no-tracking).
  3. `StaffConversationUseCases.cs`: `ReplyConversationUseCase` (IUseCase value-returning: load null→ConversationNotFound; settings null→ConfigurationUnavailable; validate body empty/MaxMessageLength PLAIN-TEXT; append Message(Staff,SenderUserId); reopen nếu Closed; LastStaffMessageAt=now; KHÔNG tăng UnreadForStaff; notifier.NotifyConversationUpdated) + `ReplyConversationValidator`; `MarkConversationReadUseCase` (ICommandUseCase void: UnreadForStaff=0 + ReadByStaffAt cho tin guest chưa đọc qua reader→ids→FindByIdAsync; notifier.NotifyMessageRead); `CloseConversationUseCase` (void: Status=Closed+ClosedAt+ClosedByUserId=StaffUserId; idempotent đã-Closed→no-op; notifier); `CloseConversationForVisitUseCase` (void, System cascade C-GA.5: tìm theo GuestVisitId; null/đã-Closed→no-op; ClosedByUserId=null; notifier).
  4. `InternalNoteUseCases.cs`: `CreateInternalNoteUseCase` (IUseCase value-returning: body empty→MessageEmpty; nếu ConversationId có→AnyAsync verify→không có→ConversationNotFound [tránh FK Restrict thô]; trả NoteId) + `CreateInternalNoteValidator` (resort/author/body + ÍT NHẤT phòng-hoặc-hội-thoại); `UpdateInternalNoteUseCase` (void: not-found→NoteNotFound; set Body+UpdatedAt; xmin CP15) + validator; `DeleteInternalNoteUseCase` (void: not-found→NoteNotFound; hard-delete Remove).
  5. `ConciergeInfrastructureExtensions` +7 use case (factory keyed repo/UoW mirror SendGuestMessage) + 3 validator (AddTransient IValidator).
  6. `ErrorCodeSnapshotTests` +`concierge_note_not_found` (ordinal giữa concierge_message_too_long/concurrency_conflict).
  7. Tests: `ConciergeStaffUseCaseTests` (SQLite, 12: reply not-found/config-null/empty/too-long/happy-no-unread-increment+notify/reopen-Closed; mark-read not-found/reset+mark+notify; close not-found/happy+idempotent+notify-1-lần; close-for-visit no-conv-noop/đã-Closed-noop/System-null-actor+notify) + `ConciergeNoteUseCaseTests` (SQLite, 9: create empty/conv-not-found/room-only/conv-exists; update not-found/empty/set-body+UpdatedAt; delete not-found/hard-delete) + `ConciergeBoardTests` (**Postgres Testcontainers, 5**: order-LastMessageAt-desc [convA tạo trước nhưng last mới nhất→đầu], filter-status, phân-trang, get-null, get-detail-messages+SenderUserId).
- **Quyết định mấu chốt (fix bản chất, không fix ngọn):** board `OrderByDescending(LastMessageAt)` — KHÔNG Id-v7. Lý do CHÍNH XÁC: board (Req 5.3) là "recency-of-ACTIVITY"; LastMessageAt biến thiên độc lập thời-điểm-tạo → Id-v7 (creation-order) hiển thị SAI thứ tự khi hội thoại cũ có tin mới. Bài học QR-N-059 (Id-v7≈CreatedAt) CHỈ đúng khi "latest"=thời-điểm-tạo-entity; KHÔNG áp cho board. Vì SQLite không ORDER BY DateTimeOffset → board đo trên Postgres (`ConciergeBoardTests`), nhất quán chiến lược provider-specific→Postgres. QR-AD-044 ghi đầy đủ.
- **Docker rules tuân thủ (bài học QR-N-050):** Docker Desktop KHÔNG chạy đầu phiên → 8 test Testcontainers (3 constraint + 5 board) skip → KHỞI ĐỘNG lại Docker Desktop (server 29.5.2) → chạy lại `Concierge.IntegrationTests` = **43 pass / 0 skip / 0 fail**. KHÔNG lấy skip mềm làm bằng chứng cuối.
- Bằng chứng: `dotnet build Platform.slnx -c Release` 0-warning/0-error; Concierge.IntegrationTests 43/43 (0 skip); Bedrock.ContractTests 2/2 (snapshot có `concierge_note_not_found`); StarHill.ArchitectureTests 33/33 (INV-1..6 xanh). QR-AD-044 Implemented + Guard-Tests (ConciergeStaffUseCaseTests/ConciergeNoteUseCaseTests/ConciergeBoardTests/ConciergeBoundaryTests — INV-6 tồn tại thật). **Module Concierge HOÀN TẤT mặt Application** (guest K-Con.2a + staff/notes/board K-Con.2b).
- **NEXT — slice K-Con.3** (Api REST + Host wiring): guest endpoints (GET /v1/guest/conversation + POST /v1/guest/messages, resolve context→gate-in-usecase→touch-sau-POST/no-touch-GET, no-store) + admin endpoints (GET board phân trang + GET {id} detail + POST {id}/reply + POST {id}/read + POST {id}/close + notes CRUD, RequireStaff, actor=ICurrentUser.UserId, resortId server qua IResortSettingsQuery) + `AddConciergeApi` + Host wire (conn Concierge + migrate) + CI bundle `concierge` (mirror gap Housekeeping H-Hk.3) + `ConciergeEndpointAuthTests` + `HostEndpointWiringSmokeTests` +InlineData + fakes. Rồi K-Con.4 (SignalR ChatHub + SignalRConciergeNotifier override + auth-on-join) → C-GA.5 (cascade cross-module cuối: GuestVisitEnded outbox GuestAccess → consumer gọi CloseConversationForVisit [Concierge] + CancelOpenTicketsForVisit [Housekeeping], đóng CP9/QR-AD-027).
