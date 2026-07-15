# HANDOFF — Bàn giao phiên làm việc (chuyển máy) — cập nhật 2026-07-14 (phiên GuestAccess + anti-drift INV-6)

> Mục đích: máy/phiên khác đọc file này là HIỂU toàn bộ bối cảnh + việc đã làm + việc cần làm để tiếp tục.
> Ngôn ngữ làm việc: **Tiếng Việt**. Nguyên tắc bất di bất dịch của user ghi ở §9.

---

## 0. CẬP NHẬT MỚI NHẤT (đọc TRƯỚC) — phiên GuestAccess + INV-6, 2026-07-14

**Máy phiên này có Docker Server 29.5.2 → mọi Testcontainers/Compose chạy THẬT (không skip).**

**Trạng thái git khi bàn giao:** commit `790a9de` (68 file: toàn bộ GuestAccess + C-GA.3a + INV-6 + journal + end.md) ĐÃ push `origin/develop`, sync 0/0. Chi tiết §10.

### 0.1 Đã hoàn thành phiên này

Module **GuestAccess** (Wave C) — resolve token QR cho khách — đã được port lên Bedrock theo lát cắt, cùng đợt hardening C-GA.3a và nâng cấp anti-drift INV-6.

- **C-GA.1 — Domain/Contracts/Persistence:** module 5-project `GuestAccess.{Domain,Contracts,Application,Infrastructure,Api}`; `GuestSession`/`GuestVisit`/`GuestVisitStatus`; `GuestAccessModule.PersistenceKey="guest_access"`; `GuestAccessDbContext` schema `guest_access` keyed; migration `20260714031018_InitialCreate` (unique session-hash; partial unique 1 visit Active/(session,room); partial expiry sweep index; 2 check vòng đời; FK nội-module Restrict; KHÔNG FK chéo schema). Guard: `GuestAccessPostgresConstraintTests` (Testcontainers, 4) + `GuestAccessBoundaryTests` (3).
- **C-GA.2a — cross-module read contract:** `ResortConfig.Contracts.IResortGuestConfigQuery.GetAsync(Guid resortId)` + `EfResortGuestConfigQuery` **fail-closed** (thiếu resort/settings/default-language → null → `configuration_unavailable`, không đoán default). Guard: `ResortGuestConfigQueryTests` (3, SQLite).
- **C-GA.2b — resolve core:** `ResolveTokenUseCase` là **`IUseCase<,>` thường** (KHÔNG `ITransactionalUseCase`) — đọc Rooms/Config NGOÀI transaction rồi tự mở transaction hẹp `guest_access` cho critical-section session/visit. `EfGuestSessionStore` dùng model-driven `SELECT ... LIMIT 1 FOR UPDATE` (Npgsql; SQLite fallback không lock). `Sha256GuestSessionKeyHasher` (hex 64). Errors `qr_invalid`/`room_inactive`/`configuration_unavailable` (ErrorCodeSnapshot cập nhật). Guard: `GuestAccessResolveRaceTests` (Postgres, 8 request cùng cookie+room → 1 Active/cùng VisitId) + `ResolveTokenUseCaseTests`.
- **C-GA.3 — public API/Host/deploy:** `POST /v1/guest/resolve` `AllowAnonymous`, token trong BODY (QR-DV-006), cookie `__Host-starhill_guest` (HttpOnly/Secure/SameSite=Lax/Path=/, chỉ set khi phát session mới), `Cache-Control: no-store`, không echo raw key. `GuestAccessOptions` ValidateOnStart (`__Host-`, days [30,90]). Host wiring + connection string `guest_access` + migrate gated + compose override + CI migration bundle.
- **C-GA.3a — reconciliation/hardening (fix drift design↔code phát hiện khi rà soát):**
  1. **Reconcile design:** `03-guestaccess.md` §6 sửa từ `ICommandUseCase`/decorator → `IUseCase` + transaction hẹp (đúng code); QR-AD-024/025/026 chuyển `Proposed`→`Accepted/Implemented`.
  2. **Canonical input guard** `GuestCredentialFormat` (43 ký tự base64url = output `ITokenGenerator.NewToken()`): token không canonical → `qr_invalid` TRƯỚC resolver/DB; cookie không canonical (whitespace/quá dài/ký tự lạ) → coi như thiết bị mới, KHÔNG hash/lookup.
  3. **HTTP contract NESTED** (`room/resort/languages/defaultLanguage/visit/features`) đúng design §7 + snapshot test.
  4. **Body limit 1 KiB** (`RequestSizeLimitAttribute`) — Kestrel enforce THẬT (Compose: body 4108B → **413**).
  5. **Log-redaction guard THẬT** `GuestAccessResolveLogRedactionTests` (pipeline `UseBedrockApi`+`RequestLoggingMiddleware`+`LoggingUseCaseDecorator`): raw token/cookie/issued-key KHÔNG vào log (thay claim "by design").
  6. **Migration history per-schema (QR-AD-028):** phát hiện DB thật chỉ có `public.__EFMigrationsHistory` chứa cả 5 migration của 4 module → cấu hình `MigrationsHistoryTable("__EFMigrationsHistory", <schema>)` ở **Host + 4 design-time factory + 5 integration setup**; thêm transition SQL idempotent `starhill/deploy/migrations/20260714-split-ef-history.sql` (copy đúng MigrationId sang từng schema, GIỮ ledger public cho rollback). Guard: `ModuleMigrationHistorySchemaTests` (Docker-free, boot Host + đọc `RelationalOptionsExtension.MigrationsHistoryTableSchema` 4 context).

### 0.2 Anti-drift NÂNG CẤP — INV-6 traceability gate (QR-AD-029) — CƠ CHẾ CHỐNG DRIFT MẠNH NHẤT phiên này

- **Vấn đề gốc:** INV-1..5 (cả base) chỉ kiểm NỘI BỘ journal → KHÔNG bắt được drift "design ghi X, code làm Y" (đúng loại lọt ở C-GA.3a). INV-2 keystone chỉ khớp CHUỖI "AD có mặt".
- **INV-6 (mới, trong `StarHillJournalConsistencyTests`):** mọi `QR-AD` có `Status` chứa **"Implemented"** PHẢI khai dòng `- Guard-Tests:` liệt kê ≥1 test class; và MỌI test class liệt kê PHẢI tồn tại thật (`class <Name>`) trong `starhill/tests/**/*.cs` (quét source, ancestor-walk như RequireJournalDir). Xóa/đổi tên/tuyên-bố-rỗng-guard = **FAIL BUILD**.
- Cơ chế = field cấu trúc `Guard-Tests:` + quét source (KHÔNG reflection cross-assembly — lý do ở QR-TO-009). **Đã kiểm chứng guard KHÔNG vacuous:** tạm đổi 1 Guard-Test thành tên rác → INV-6 ĐỎ đúng thông điệp → revert → xanh.
- Journal 4-file đã cập nhật: `01` QR-AD-028/029 + Guard-Tests cho 024/025/026/028/029; `03` QR-TO-007/008/009; `04` QR-N-028/029; `05` mô tả INV-6 + guard row 028/029 + range AD-001..029/TO-001..009/N-001..029.

### 0.3 Kiểm chứng phiên này (Docker THẬT — không bịa)

- `dotnet build starhill/Platform.slnx -c Release` → **0 warning / 0 error**.
- Full test Docker: **0 fail / 0 skip** — GuestAccess.IntegrationTests **25**, StarHill.Api.Tests **31** (gồm nested-contract + body-limit + 2 log-redaction + ModuleMigrationHistorySchema), StarHill.ArchitectureTests **17** (INV-1..6 = 6/6), ResortConfig 15, Rooms 27, Identity 2.
- Compose end-to-end: 4 schema (`identity/resort_config/rooms/guest_access`); per-schema ledger đúng (2/1/1/1=5); transition SQL idempotent; **upgrade path** binary mới boot "No migrations were applied. Already up to date." 0 re-apply; `/health/ready`+`/health/live`=200; resolve invalid → 404 + `no-store` + KHÔNG cookie + `qr_invalid`; oversize → 413.
- Diagnostics spec = 0; JournalConsistency INV-1..6 xanh; `git diff --check` sạch.

### 0.4 BƯỚC KẾ TIẾP (design-first, chưa code)

- **Module Rules** (consumer đầu tiên của GuestAccess current-context). Khi Rules tồn tại mới mở khóa:
  - **C-GA.4** — current-guest-context DTO/port (GuestAccess.Contracts) + portal check-before-touch + idle sweeper `FOR UPDATE SKIP LOCKED`.
  - **C-GA.5** — staff close + `GuestVisitEndedIntegrationEvent` outbox/inbox **at-least-once** (QR-AD-027) khi Concierge/Housekeeping tồn tại. KHÔNG map outbox ở resolve slice.
- Bám design-first: viết `design-modules/04-rules.md` + journal trước khi code; mọi AD chuyển Implemented phải kèm `Guard-Tests` (INV-6 sẽ chặn nếu quên).

---

## 0-bis. (phiên trước) CẬP NHẬT — phiên API slices 2026-07-13

**HEAD `develop` = `d1490f9` (lúc đó), sau này 16df850 (handoff) → 790a9de (phiên GuestAccess).**

3 increment (mỗi cái: `vp all` build 0-warning + full suite 0-fail + `vp journal` INV-1..5 xanh; đã commit + push riêng):
- **`d721182` — B-Rooms.3 (Rooms.Api):** endpoint admin `/v1/rooms` — POST create / PUT update / PATCH `/{id}/status` / DELETE / POST `/{id}/rotate-token` (đều **RequireAdmin**, Req 7.6) + GET `/{id}/qr.png` (**RequireStaff**). Tạo project dùng chung **`starhill/src/StarHill.Authorization`** (policy `RequireAdmin`/`RequireStaff`, Admin⊇Staff superset — QR-AD-020) + bật **`JsonStringEnumConverter` toàn cục** ở Host (QR-AD-021). Response KHÔNG trả token thô (chỉ preview — CP1). resortId phân giải ở Api qua `IResortSettingsQuery` (QR-DV-004). Journal **QR-AD-019/020/021**.
- **`302826e` — B-Rooms.4 (Rooms query):** GET `/v1/rooms` (phân trang `PagedRequest/PagedResult` + lọc `status`) + GET `/v1/rooms/{id}` (RequireStaff, 404 nếu vắng). Read-model `RoomListItem` + port `IRoomQueries` (Rooms.Application, nội-module) + `EfRoomQueries` (Infra, 2-query map in-memory, loại soft-delete). Journal **QR-AD-022**.
- **`d1490f9` — B-Config.3 (ResortConfig settings Api):** **write-path ĐẦU TIÊN của ResortConfig.** GET/PUT `/v1/resort/settings` (**RequireAdmin**, Req 9.3). `UpdateResortSettingsUseCase` + validator (Application, keyed `IRepository<ResortSettings>`+`IUnitOfWork` mirror Rooms; GuestWebBaseUrl null|https; ranges) + `AddBedrockRepository<ResortConfigDbContext,ResortSettings>(key)` + project **`ResortConfig.Api`**. Tái dùng `CommonErrors.NotFoundGeneric` (KHÔNG tạo error catalog mới → không đụng `ErrorCodeSnapshotTests`). **Đóng mắt xích:** admin nay cấu hình được `GuestWebBaseUrl` mà `qr.png` cần. Journal **QR-AD-023**.

**Design docs:** `.kiro/specs/starhill-qr/design-B-Rooms.3-rooms-api.md`, `design-B-Rooms.4-rooms-queries.md`, `design-B-Config.3-settings-api.md`.

---

## 1. Bố cục repo (RẤT QUAN TRỌNG)

Repo root: **TÙY MÁY** (nhánh git `develop`, remote GitHub `mgcoder9x/StarHillGuestApp`). Máy phiên này: `c:\Users\k.nguyen.manh.toan\Desktop\TOANM\WORK\StarHillGuestApp\`. (Đường dẫn khác nhau giữa các máy — không hardcode.)

Hai cây solution .NET 10 song song:
- **`platform/`** — base modular-monolith domain-agnostic, prefix `Bedrock.*` + `Adapters` + Host mẫu + module Identity mẫu. Solution `platform/Platform.slnx`. Đây là **base tái dùng cho NHIỀU dự án**.
- **`starhill/`** — SẢN PHẨM QR (StarHill Guest App). Solution `starhill/Platform.slnx`. Chứa **CHỈ phần nghiệp vụ QR**: `src/Modules/{Identity,ResortConfig,Rooms,GuestAccess}`, `src/StarHill.Authorization`, `src/Host/StarHill.Api`, và test nghiệp vụ.

⚠️ **BẪY — nested stale copy:** tồn tại thư mục cũ `StarHillGuestApp/StarHillGuestApp/` ở repo root. **TUYỆT ĐỐI KHÔNG đụng/không commit nó.** Nó đang untracked trong git — khi `git add` phải dùng path tường minh, KHÔNG `git add -A`/`git add .`.

### 1.1 QUAN HỆ giữa 2 cây (D1-a — nền tảng)
- `starhill/` KHÔNG chứa bản-copy base. Module/Host/test nghiệp vụ **ProjectReference thẳng vào `platform/src/*`** qua property `$(PlatformSrc)` (định nghĩa ở `starhill/Directory.Build.props` = `$(MSBuildThisFileDirectory)..\platform\src`).
- ⇒ **MỘT nguồn base duy nhất** (ở `platform/`). Sửa base = sửa `platform/src/*`, sản phẩm nhận ngay lúc compile. **Drift base bất khả thi.**
- `Directory.Build.props`/`Directory.Packages.props` phân giải theo cây từng .csproj → project base dùng props của `platform/`, project nghiệp vụ dùng props của `starhill/` → KHÔNG lẫn version.

---

## 2. Hệ thống JOURNAL (anti-drift — chống trôi dạt; user yêu cầu duy trì XUYÊN SUỐT)

Có HAI journal độc lập, mỗi cái 5 file + test build-gate:

### 2.1 Journal BASE — `.kiro/specs/platform-base/journal/`
- `01-decisions.md` (AD-001…**AD-102**), `02-deviations.md` (DV), `03-tradeoffs.md` (TO), `04-notes.md` (N-001…**N-082**), `05-anti-drift.md` (guard map + KEYSTONE).
- Guard: `platform/tests/Bedrock.ArchitectureTests/JournalConsistencyTests.cs` (INV-1..5). Chạy trong `platform/`.

### 2.2 Journal SẢN PHẨM QR — `.kiro/specs/starhill-qr/journal/`
- Cùng 5 file, tiền tố ID **`QR-`** (QR-AD-001…**QR-AD-029**, QR-DV-001..006, QR-TO-001..**009**, QR-N-001..**029**). Mới nhất: **QR-AD-024..027** (GuestAccess boundary/config/resolve/cascade), **QR-AD-028** (migration history per-schema), **QR-AD-029** (traceability gate INV-6).
- Guard: `starhill/tests/StarHill.ArchitectureTests/StarHillJournalConsistencyTests.cs` (**INV-1..6**, tiền tố QR, CP range 1..15).
- **INV-1** ID duy nhất+liên tục; **INV-2 (KEYSTONE)** mọi QR-AD phải xuất hiện trong `05-anti-drift.md`; **INV-3** không dangling ref; **INV-4** QR-AD/QR-DV có `Status:`+`Provenance/Evidence:`; **INV-5** CP## trong 1..15; **INV-6 (traceability, QR-AD-029)** mọi QR-AD `Status` chứa "Implemented" phải khai `- Guard-Tests:` và mọi test class liệt kê phải tồn tại thật (`class <Name>`) trong `starhill/tests/**/*.cs` — nối journal↔code, chống drift design↔code.
- **KỶ LUẬT MỚI (bắt buộc):** khi chuyển một QR-AD sang `Status: ...Implemented`, PHẢI thêm dòng `- Guard-Tests: TestClassA, TestClassB` trỏ test có thật, nếu không INV-6 fail build.

**QUY TẮC mỗi thay đổi:** build 0-warning (`TreatWarningsAsErrors`) + test xanh + ghi journal (AD/DV/TO/N với Provenance THẬT) + cập nhật `05` (INV-2) + chạy JournalConsistency inline.

---

## 3. ĐÃ LÀM — phiên P0 remediation (2026-07-13) — đã commit+push

### 3.1 Khối A — Hardening BASE `platform/` (journal base AD-098/AD-099/N-078/N-079)
1. **AD-098** — `ICommandUseCase<in TInput>` (void) nay `: IUseCase, ITransactionalUseCase` → **compile-enforce `PersistenceKey`**.
2. **AD-099** — `DiscoveredProjectBoundaryTests`: cấm adapter kéo EF/Npgsql/AspNetCore; mọi project phải trong `Platform.slnx`.
3. **N-078/N-079** — fix + thêm `RabbitMqResilienceTests` (DLQ isolation + graceful-shutdown drain). Full suite Docker: 403 pass.

### 3.2 Khối B — Khắc phục 3 P0 của `starhill/` bằng D1-a (QR-AD-012..015, QR-TO-004, QR-N-016)
- **D1-a (QR-AD-012):** xóa 12 project base-copy khỏi `starhill/` + repoint ref sang `platform/src` qua `$(PlatformSrc)`.
- **P0-1 (QR-AD-013):** keyed persistence (hằng `PersistenceKey` ở `*.Contracts`; `AddBedrockPersistence<TContext>(key,..)`; use case resolve qua `GetRequiredKeyedService<T>(key)`).
- **P0-2 (QR-AD-014):** một DB `starhill`, schema-per-module.
- **P0-3 (QR-AD-015):** compose boot — Docker build context = repo root; RabbitMQ healthcheck `check_port_connectivity`.

---

## 4. TRẠNG THÁI XANH đã kiểm chứng (phiên này, Docker thật)

- `cd starhill ; dotnet build Platform.slnx -c Release` → **0 warning / 0 error**.
- `cd starhill ; dotnet test Platform.slnx -c Release` → **0 fail / 0 skip** (GuestAccess.IntegrationTests 25; StarHill.Api.Tests 31; StarHill.ArchitectureTests 17 gồm INV-1..6; ResortConfig 15; Rooms 27; Identity 2; + unit/contract).
- Compose end-to-end: 4 schema; per-schema `__EFMigrationsHistory`; health ready/live 200; resolve invalid 404+no-store+no-cookie; oversize 413.
- Journal QR INV-1..6: pass. Diagnostics spec 0.

---

## 5. LỆNH VERIFY (Windows, shell cmd/pwsh)

```
REM starhill (sản phẩm)
cd starhill & dotnet build Platform.slnx -c Release
cd starhill & dotnet test Platform.slnx -c Release
cd starhill & scripts\vp.cmd            REM all: build 0-warning + validate-ci + test
cd starhill & scripts\vp.cmd journal    REM StarHillJournalConsistencyTests (journal QR, INV-1..6)
cd starhill & docker compose up -d --build   REM boot end-to-end; health tại :18080/health/ready

REM platform (base)
cd platform & scripts\vp.cmd
```
- Migration module (khi model đổi): `dotnet ef migrations add <Name> -p src\Modules\<M>\<M>.Infrastructure -s src\Modules\<M>\<M>.Infrastructure -o Persistence\Migrations` (dotnet-ef 10.0.9 qua `dotnet tool restore`).
- **Migration history per-schema (QR-AD-028):** mọi call site Npgsql (Host + factory + integration) PHẢI `MigrationsHistoryTable("__EFMigrationsHistory", "<schema>")`. Upgrade DB cũ (ledger public chung) chạy `starhill/deploy/migrations/20260714-split-ef-history.sql` TRƯỚC binary mới.
- ⚠️ Testcontainers cần Docker. Máy KHÔNG Docker → integration SKIP mềm; CI runner có Docker chạy thật.

---

## 6. CẦN LÀM TIẾP

1. **Module Rules (design-first)** — consumer đầu của GuestAccess current-context. Viết `design-modules/04-rules.md` + journal trước khi code. Rule-gate (`IRuleGate`) là contract dùng chung cho Faq/Concierge/Housekeeping.
2. **GuestAccess C-GA.4** (khi Rules cần): current-guest-context DTO/port + portal check-before-touch + idle sweeper `FOR UPDATE SKIP LOCKED`.
3. **GuestAccess C-GA.5** (khi Concierge/Housekeeping tồn tại): staff close + `GuestVisitEndedIntegrationEvent` outbox/inbox at-least-once (QR-AD-027).
4. **Các module QR còn lại (design-first):** Rules → Faq ‖ Concierge ‖ Housekeeping → Dashboard-ở-Host. Xem `docs/resort-qr-portal/` + `.kiro/specs/starhill-qr/design.md` §10 (build waves) + `design-modules/`.
5. **Defer base:** resilience broker-restart consumer (mirror platform N-079) — thuộc `platform/`.

---

## 7. PATTERN CHUẨN (bám theo khi thêm module/use case mới trong starhill)

- **Keyed persistence:** module có hằng `PersistenceKey` ở `*.Contracts`; Infrastructure DI `AddBedrockPersistence<TContext>(key, cfg)` + capability opt-in khớp DbContext (`AddBedrockOutbox/Inbox/RefreshTokens` chỉ khi DbContext map bảng đó) + `AddBedrockRepository<TContext,TEntity>(key)`; use case đăng ký qua **factory** resolve `GetRequiredKeyedService<T>(key)`. Mẫu: `platform/src/Modules/Identity/.../IdentityInfrastructureExtensions.cs` và `starhill/.../GuestAccessInfrastructureExtensions.cs`.
- **Command void** (`ICommandUseCase<TInput>`) BẮT BUỘC khai `PersistenceKey` (compile-enforce AD-098).
- **1 DbContext + 1 schema/module** (`HasDefaultSchema`), migration per-module + **`__EFMigrationsHistory` trong schema module** (QR-AD-028), **KHÔNG FK chéo schema** — cross-module dùng Id trần (Guid) + đọc qua `<M>.Contracts` query port.
- **Use case đọc cross-module NGOÀI transaction; nếu cần lock/ghi thì tự mở transaction hẹp** (mẫu `ResolveTokenUseCase`) hoặc dùng `ICommandUseCase` khi transaction bao trọn là đúng (mẫu Rooms).
- **KHÔNG auto-scan use case** — đăng ký THỦ CÔNG trong `Add<M>Infrastructure`.
- Test nghiệp vụ ở `starhill/tests/*`. Test BASE ở `platform/tests/*` (KHÔNG copy sang starhill).
- **Endpoint public (guest):** AllowAnonymous + canonical input guard trước resolver/DB + body size limit + no-store + KHÔNG log secret + ProblemDetails. Mẫu `GuestAccessEndpointModule` + `GuestAccessResolveLogRedactionTests`.

---

## 8. FILE QUAN TRỌNG (điểm vào nhanh)

- Design GuestAccess (có hiệu lực): `.kiro/specs/starhill-qr/design-modules/03-guestaccess.md`
- Journal QR: `.kiro/specs/starhill-qr/journal/{01..05}.md` (đọc QR-AD-024..029 cho GuestAccess + anti-drift INV-6)
- Journal base: `.kiro/specs/platform-base/journal/{01..05}.md`
- Base keyed API: `platform/src/Bedrock.Infrastructure/DependencyInjection/BedrockPersistenceExtensions.cs`
- Host sản phẩm: `starhill/src/Host/StarHill.Api/Program.cs` + `appsettings.json` + `Dockerfile`
- Compose: `starhill/docker-compose.yml`; migration transition: `starhill/deploy/migrations/20260714-split-ef-history.sql`
- Cross-tree ref: `starhill/Directory.Build.props` (`$(PlatformSrc)`), `starhill/Platform.slnx`
- Design tổng: `.kiro/specs/starhill-qr/design.md` + README.md; spec nguồn: `docs/resort-qr-portal/*`

---

## 9. NGUYÊN TẮC USER (áp cho MỌI việc — bất di bất dịch)

- **Design-first, verify-then-implement:** chuẩn bị thiết kế rõ → đọc lại/valid nhiều lần → chắc chắn kiểm chứng được → MỚI code. **TUYỆT ĐỐI không bịa, không suy đoán** — mọi khẳng định phải đọc file thật/chạy lệnh thật.
- **Fix tận gốc, không fix ngọn** (nhìn bản chất vấn đề).
- **Khi khuyến nghị:** phải hiểu + nói được LÝ DO CHÍNH XÁC mới được chọn.
- **Không tiết kiệm token** để nhanh xong task; hướng **sản phẩm thương mại, lâu dài**.
- Mọi quyết định AI tự ra ngoài spec → ghi AD; đổi so với yêu cầu → DV; trade-off → TO; điều cần biết → N. Duy trì 4 file journal + `05` guard map + chạy JournalConsistency mỗi turn.
- Ngôn ngữ trả lời: **Tiếng Việt**.
- Dùng "**production-oriented**" (không "production-grade"); outbox = **at-least-once** (không exactly-once).
- Docker sẵn có → chạy Testcontainers thật, không skip câm.
- Mọi AD chuyển `Implemented` phải kèm `- Guard-Tests:` (INV-6 chặn nếu quên).

---

## 10. GIT (trạng thái để push)

- Nhánh: `develop` (upstream `origin/develop`). Remote GitHub (token nhúng sẵn trong URL remote — KHÔNG in ra/không log).
- **HEAD = `790a9de`** (commit "update", 68 file: toàn bộ GuestAccess C-GA.1..3 + C-GA.3a + INV-6 + journal QR + end.md) — ĐÃ push `origin/develop`. Trước đó: `16df850` (handoff API slices) → `d1490f9`/`302826e`/`d721182`.
- Kiểm sync: `git rev-list --left-right --count origin/develop...HEAD` = `0 0` → in-sync.
- ⚠️ Commit `790a9de` đã verify KHÔNG chứa `/bin/`, `/obj/`, `StarHillGuestApp/StarHillGuestApp` hay file tạm (`git diff --name-only 16df850 790a9de | Select-String` rỗng cho các pattern đó).
- ⚠️ KHÔNG add nested `StarHillGuestApp/` (stale, untracked). Dùng `git add` **path tường minh** (`.github .kiro starhill end.md`), KHÔNG `git add -A`/`.`. `.gitignore` đã loại `bin/`/`obj/`.
- Quirk: `git push` đôi khi hiển thị `^C`/exit bất thường do pipeline PowerShell nhưng push THẬT thành công — xác nhận bằng so `git rev-parse HEAD` == `git rev-parse origin/develop`.
- **SỰ CỐ đã xử lý phiên này:** end.md từng bị ghi đè bằng log terminal và lọt vào commit `790a9de`; đã khôi phục nội dung handoff đúng và commit/push lại (xem commit sau `790a9de`). Bài học: sau khi sửa end.md phải `Get-Content end.md -TotalCount 5` kiểm nội dung trước khi commit.
