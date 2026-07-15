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
