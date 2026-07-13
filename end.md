# HANDOFF — Bàn giao phiên làm việc (chuyển máy) — cập nhật 2026-07-13

> Mục đích: máy/phiên khác đọc file này là HIỂU toàn bộ bối cảnh + việc đã làm + việc cần làm để tiếp tục.
> Ngôn ngữ làm việc: **Tiếng Việt**. Nguyên tắc bất di bất dịch của user ghi ở §9.

---

## 1. Bố cục repo (RẤT QUAN TRỌNG)

Repo root: `c:\Users\k.nguyen.manh.toan\Desktop\TOANM\WORK\StarHillGuestApp\` (nhánh git `develop`, remote GitHub `mgcoder9x/StarHillGuestApp`).

Hai cây solution .NET 10 song song:
- **`platform/`** — base modular-monolith domain-agnostic, prefix `Bedrock.*` + `Adapters` + Host mẫu + module Identity mẫu. Solution `platform/Platform.slnx`. Đây là **base tái dùng cho NHIỀU dự án** (design `.kiro/specs/platform-base/design.md:16`).
- **`starhill/`** — SẢN PHẨM QR (StarHill Guest App). Solution `starhill/Platform.slnx`. Chứa **CHỈ phần nghiệp vụ QR**: `src/Modules/{Identity,ResortConfig,Rooms}`, `src/Host/StarHill.Api`, và test nghiệp vụ.

⚠️ **BẪY — nested stale copy:** tồn tại thư mục cũ `StarHillGuestApp/StarHillGuestApp/` ở repo root. **TUYỆT ĐỐI KHÔNG đụng/không commit nó.** Nó đang untracked trong git — khi `git add` phải dùng path tường minh, KHÔNG `git add -A`/`git add .`.

### 1.1 QUAN HỆ MỚI giữa 2 cây (sau D1-a phiên này — thay đổi nền tảng)
- Trước đây `starhill/` là **bản COPY đầy đủ** của base (`Bedrock.*`+Adapters+test base) → đã gây drift 50 file.
- **Nay (D1-a):** `starhill/` KHÔNG còn bản-copy base. Module/Host/test nghiệp vụ trong `starhill/` **ProjectReference thẳng vào `platform/src/*`** qua property `$(PlatformSrc)` (định nghĩa ở `starhill/Directory.Build.props` = `$(MSBuildThisFileDirectory)..\platform\src`).
- ⇒ **MỘT nguồn base duy nhất** (ở `platform/`). Sửa base = sửa `platform/src/*`, sản phẩm nhận ngay lúc compile. **Drift base bất khả thi.**
- `Directory.Build.props`/`Directory.Packages.props` phân giải theo cây từng .csproj → project base dùng props của `platform/`, project nghiệp vụ dùng props của `starhill/` → KHÔNG lẫn version.

---

## 2. Hệ thống JOURNAL (anti-drift — chống trôi dạt; user yêu cầu duy trì XUYÊN SUỐT)

Có HAI journal độc lập, mỗi cái 5 file + test build-gate:

### 2.1 Journal BASE — `.kiro/specs/platform-base/journal/`
- `01-decisions.md` (AD-001…**AD-099**), `02-deviations.md` (DV…16), `03-tradeoffs.md` (TO…11), `04-notes.md` (N-001…**N-079**), `05-anti-drift.md` (guard map + KEYSTONE).
- Guard: `platform/tests/Bedrock.ArchitectureTests/JournalConsistencyTests.cs` (INV-1..5). Chạy trong `platform/`.
- Lịch sử hardening: `.kiro/specs/platform-base/journal/hardening-2026-07-12-ledger.md` (#1…#26).

### 2.2 Journal SẢN PHẨM QR — `.kiro/specs/starhill-qr/journal/`
- Cùng 5 file, tiền tố ID **`QR-`** (QR-AD-001…**QR-AD-015**, QR-DV-001..004, QR-TO-001..004, QR-N-001..**016**).
- Guard: `starhill/tests/StarHill.ArchitectureTests/StarHillJournalConsistencyTests.cs` (INV-1..5, tiền tố QR, CP range 1..15).
- **INV-1** ID duy nhất+liên tục; **INV-2 (KEYSTONE)** mọi QR-AD phải xuất hiện trong `05-anti-drift.md`; **INV-3** không dangling ref; **INV-4** QR-AD/QR-DV có `Status:`+`Provenance/Evidence:`; **INV-5** CP## trong 1..15.

**QUY TẮC mỗi thay đổi:** build 0-warning (`TreatWarningsAsErrors`) + test xanh + ghi journal (AD/DV/TO/N với Provenance THẬT) + cập nhật `05` (INV-2) + chạy JournalConsistency inline.

---

## 3. ĐÃ LÀM phiên này (2 khối) — TẤT CẢ đã verify xanh, CHƯA COMMIT trước file này

### 3.1 Khối A — Hardening BASE `platform/` (Task 1–4; journal base AD-098/AD-099/N-078/N-079)
1. **AD-098** — đóng gap `ICommandUseCase<in TInput>` (void): nay `: IUseCase, ITransactionalUseCase` → **compile-enforce `PersistenceKey`** cho command void. 5 command decorator delegate `PersistenceKey => _inner.PersistenceKey`. Thêm `KeyedCommandPipelineTests`.
2. **AD-099** — mở rộng `DiscoveredProjectBoundaryTests`: cấm adapter kéo EF/Npgsql/AspNetCore PackageReference/FrameworkReference; mọi module/adapter phải có trong `Platform.slnx`.
3. **N-078** — fix test bug `RabbitMqConsumeEndToEndTests` (pre-declare queue thiếu DLX arg → 406). Full suite Docker: 401 pass.
4. **N-079** — thêm `RabbitMqResilienceTests` (DLQ isolation + graceful-shutdown drain). Full suite Docker: **403 pass / 0 fail**.
- File chính: `platform/src/Bedrock.Application/UseCases/IUseCase.cs`, `platform/src/Bedrock.Application/Behaviors/*CommandUseCaseDecorator.cs`, `platform/tests/Bedrock.ArchitectureTests/DiscoveredProjectBoundaryTests.cs`, `platform/tests/Messaging.IntegrationTests/RabbitMq*Tests.cs`, `platform/tests/Bedrock.Infrastructure.Tests/KeyedCommandPipelineTests.cs`.

### 3.2 Khối B — Khắc phục 3 P0 của `starhill/` bằng D1-a (journal QR: QR-AD-012..015, QR-TO-004, QR-N-016; QR-AD-001 Superseded)
**D1-a (QR-AD-012, user duyệt):** xóa 12 project base-copy khỏi `starhill/` (6 `Bedrock.*`/`Adapters` src + 6 test-base) + repoint mọi ref sang `platform/src` qua `$(PlatformSrc)` + viết lại `starhill/Platform.slnx` (chỉ project nghiệp vụ). Lý do: 1 base vật lý ⇒ drift bất khả thi (fix tận gốc).

- **P0-1 (QR-AD-013)** persistence unkeyed → last-registration-wins (catastrophic): chuyển 3 module + Host sang **keyed persistence**.
  - Hằng key: `IdentityModule/ResortConfigModule/RoomsModule.PersistenceKey` (= `"identity"/"resort_config"/"rooms"`) trong từng `*.Contracts`.
  - DI: `AddBedrockPersistence<TContext>(PersistenceKey, cfg)`; Identity thêm `AddBedrockOutbox/Inbox/RefreshTokens<TContext>(key)`; Rooms thêm `AddBedrockRepository<RoomsDbContext, Room/RoomQrToken>(key)`. Use case resolve port qua **factory** `GetRequiredKeyedService<T>(PersistenceKey)`.
  - 3 void command Rooms (Update/ChangeStatus/Delete) khai `public string PersistenceKey => RoomsModule.PersistenceKey`.
  - Host messaging keyed: `AddOutboxDispatcher<IdentityDbContext>(key)`, `AddIntegrationEventConsumer<IdentityDbContext>(key)`, `AddKeyedScoped<IIntegrationEventHandler<...>>(key)`, `o.DispatcherServiceKey=key`.
  - Test Identity.IntegrationTests thích ứng: resolve keyed `GetRequiredKeyedService<IRefreshTokenStore/IUnitOfWork/IOutboxWriter>(IdentityInfrastructureExtensions.PersistenceKey)`.
- **P0-2 (QR-AD-014)** 3 DB vật lý → **một DB `starhill`, schema-per-module** (`appsettings.json` gộp cả 3 connection về `Database=starhill`). Regenerate migration Identity (`20260713075245_InitialCreate`) khớp schema base mới (base đổi outbox/inbox/refresh → `PendingModelChangesWarning`).
- **P0-3 (QR-AD-015)** compose không boot: `docker-compose.yml` postgres `POSTGRES_DB=starhill` + override CẢ 3 `ConnectionStrings__{Identity,ResortConfig,Rooms}`; Docker build **context = repo root** (compose `context: ..`, `dockerfile: starhill/src/Host/StarHill.Api/Dockerfile`, COPY `platform/`+`starhill/`) + tạo `.dockerignore` ở repo root; RabbitMQ healthcheck `check_port_connectivity` (+`start_period: 30s`) chống race Connection-refused.
- **Governance theo D1-a:** `starhill/tools/verify.ps1` Step-Journal chỉ gác journal QR (base do platform/ gác); `.github/workflows/starhill-ci.yml` job docker-image build context repo-root + đính chính comment.

---

## 4. TRẠNG THÁI XANH đã kiểm chứng (2026-07-13, máy hiện tại CÓ Docker)

- `cd starhill ; dotnet build Platform.slnx -c Debug` → **0 warning / 0 error**.
- `cd starhill ; dotnet test Platform.slnx` → **66 test, 0 fail, 0 skip** (8 project: ContractTests 2, Identity.Unit 7, ResortConfig.Unit 10, Identity.Integration 2, StarHill.Architecture 13 [gồm journal QR INV-1..5], ResortConfig.Integration 5, Rooms.Integration 24, StarHill.Api.Tests 3 [Host boot ValidateOnBuild]).
- `cd starhill ; docker compose up -d --build` → 3 container Up; log Host `CREATE SCHEMA identity/resort_config/rooms` trong CÙNG DB `starhill`; `http://localhost:18080/health/ready`=**200**, `/health/live`=**200**. (Đã `docker compose down` sau khi verify.)
- Journal QR INV-1..5: **5/5 pass**.
- `platform/` (khối A): summary phiên trước = **403 test / 0 fail** với Docker.

---

## 5. LỆNH VERIFY (Windows, shell cmd/pwsh)

```
REM starhill (sản phẩm)
cd starhill & dotnet build Platform.slnx -c Debug
cd starhill & dotnet test Platform.slnx
cd starhill & scripts\vp.cmd            REM all: build 0-warning + validate-ci + test
cd starhill & scripts\vp.cmd journal    REM StarHillJournalConsistencyTests (journal QR)
cd starhill & docker compose up -d --build   REM boot end-to-end; health tại :18080/health/ready

REM platform (base)
cd platform & scripts\vp.cmd            REM build + validate-ci + test base
```
- Migration Identity (khi model đổi): `cd starhill & dotnet ef migrations add <Name> -p src\Modules\Identity\Identity.Infrastructure -s src\Modules\Identity\Identity.Infrastructure -o Persistence\Migrations` (dotnet-ef 10.0.9 qua `dotnet tool restore`).
- ⚠️ Testcontainers cần Docker Desktop chạy. Máy KHÔNG Docker → integration test SKIP mềm (không fail); CI runner có Docker chạy thật.

---

## 6. CẦN LÀM TIẾP (defer có chủ đích + P1 + slice kế)

1. **Resilience broker-restart** (consumer `RabbitMqConsumer` StopHost khi RabbitMQ restart) — thuộc BASE `platform/` (mirror platform N-079). Ngoài phạm vi P0 starhill. Cân nhắc: retry-connect có backoff + `BackgroundServiceExceptionBehavior.Ignore` hoặc health-degrade thay vì StopHost.
2. **Migration bundle Rooms** trong `.github/workflows/starhill-ci.yml` (hiện chỉ Identity+ResortConfig; Rooms ĐÃ có migration → thêm job bundle Rooms cho deploy pipeline out-of-band).
3. **P1 cũ (từ review):** ~~(a) ResortId invariant của Rooms qua query port~~ **XONG 2026-07-13 (QR-AD-016, máy toann không-Docker)** — `IResortExistenceQuery` + kiểm trong `CreateRoom` + project mới `Rooms.UnitTests` (xem QR-N-017); ~~(b) default-language nguyên tử ở ResortConfig aggregate~~ **XONG 2026-07-13 (QR-AD-017)** — `ResortLanguagePolicy` (bất biến đúng-một+enabled + set-default nguyên tử in-memory) + seeder fail-fast + 10 unit test; **CÒN defer (QR-N-018, cần Docker):** use case SetDefault thật phải xử lý thứ tự UPDATE Npgsql vs `ux_lang_default`; (c) Dashboard shape = Application+Api thay vì nhét Host (QR-TO-003 để ngỏ khi phức tạp lên).
4. **Slice B-Rooms.3:** `Rooms.Api` (admin CRUD Admin-only/read Staff + `qr.png` endpoint + rotate-token) — cần Identity auth + Role→policy (QR-AD-005). Caller phân giải resortId single-resort (QR-DV-004).
5. **Các module QR còn lại (design-first trước khi code):** GuestAccess (nền cho Rules/Concierge/Housekeeping), Rules, Concierge (chat guest↔staff, QR-AD-004), Housekeeping, Faq, Dashboard-ở-Host. Xem `docs/resort-qr-portal/` + `.kiro/specs/starhill-qr/design.md` + `design-modules/`.

---

## 7. PATTERN CHUẨN (bám theo khi thêm module/use case mới trong starhill)

- **Keyed persistence:** module có hằng `PersistenceKey` ở `*.Contracts`; Infrastructure DI `AddBedrockPersistence<TContext>(key, cfg)` + capability opt-in khớp DbContext (`AddBedrockOutbox/Inbox/RefreshTokens` chỉ khi DbContext map bảng đó) + `AddBedrockRepository<TContext,TEntity>(key)`; use case đăng ký qua **factory** resolve `GetRequiredKeyedService<T>(key)`. Mẫu tham chiếu: `platform/src/Modules/Identity/Identity.Infrastructure/DependencyInjection/IdentityInfrastructureExtensions.cs`.
- **Command void** (`ICommandUseCase<TInput>`) BẮT BUỘC khai `PersistenceKey` (compile-enforce AD-098) → `TransactionCommandUseCaseDecorator` resolve đúng keyed UoW.
- **1 DbContext + 1 schema/module** (`HasDefaultSchema`), migration per-module (history table riêng trong schema), **KHÔNG FK chéo schema** — cross-module dùng Id trần (Guid) + đọc qua `<M>.Contracts` query port.
- **KHÔNG auto-scan use case** — đăng ký THỦ CÔNG trong `Add<M>Infrastructure`.
- Sản phẩm test nghiệp vụ đặt trong `starhill/tests/*` (Modules/*, StarHill.ArchitectureTests, Bedrock.ContractTests). Test BASE sống ở `platform/tests/*` (KHÔNG copy sang starhill).

---

## 8. FILE QUAN TRỌNG (điểm vào nhanh)

- Kế hoạch P0 gốc (đã verify): `.kiro/specs/starhill-qr/remediation-plan-2026-07-12.md`
- Journal QR: `.kiro/specs/starhill-qr/journal/{01..05}.md` (đọc QR-AD-012..015 + QR-N-016 để nắm P0 remediation)
- Journal base: `.kiro/specs/platform-base/journal/{01..05}.md` + `hardening-2026-07-12-ledger.md`
- Base keyed API (nguồn chuẩn): `platform/src/Bedrock.Infrastructure/DependencyInjection/BedrockPersistenceExtensions.cs`
- Host sản phẩm: `starhill/src/Host/StarHill.Api/Program.cs` + `StarHill.Api.csproj` + `Dockerfile` + `appsettings.json`
- Compose: `starhill/docker-compose.yml`; Docker ignore root: `.dockerignore`
- Cross-tree ref: `starhill/Directory.Build.props` (`$(PlatformSrc)`), `starhill/Platform.slnx`
- Design sản phẩm: `.kiro/specs/starhill-qr/design.md` + `design-modules/*.md`; spec nguồn: `docs/resort-qr-portal/*`

---

## 9. NGUYÊN TẮC USER (áp cho MỌI việc — bất di bất dịch)

- **Design-first, verify-then-implement:** chuẩn bị thiết kế rõ → đọc lại/valid nhiều lần → chắc chắn kiểm chứng được → MỚI code. **TUYỆT ĐỐI không bịa, không suy đoán** — mọi khẳng định phải đọc file thật/chạy lệnh thật.
- **Fix tận gốc, không fix ngọn** (nhìn bản chất vấn đề).
- **Khi khuyến nghị:** phải hiểu + nói được LÝ DO CHÍNH XÁC mới được chọn.
- **Không tiết kiệm token** để nhanh xong task; hướng **sản phẩm thương mại, lâu dài**.
- Mọi quyết định AI tự ra ngoài spec → ghi AD; đổi so với yêu cầu → DV; trade-off → TO; điều cần biết → N. Duy trì 4 file journal + `05` guard map + chạy JournalConsistency mỗi turn.
- Ngôn ngữ trả lời: **Tiếng Việt**.
- Dùng "**production-oriented**" (không "production-grade"); outbox = **at-least-once** (không exactly-once).
- Docker sẵn có → chạy Testcontainers thật, không skip câm trên CI.

---

## 10. GIT (trạng thái để push)

- Nhánh: `develop` (upstream `origin/develop`). Remote GitHub (token nhúng sẵn trong URL remote — KHÔNG in ra/không log).
- Phiên này commit CẢ `platform/` (khối A) + `starhill/` (khối B) + `.kiro/` (journal) + `.github/` + `.dockerignore` + `end.md`.
- ⚠️ KHÔNG add nested `StarHillGuestApp/` (stale). Dùng `git add` path tường minh, KHÔNG `git add -A`.
