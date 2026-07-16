# 01 — Autonomous Decisions (pha QR) — quyết định AI tự ra mà spec không nói

> Journal RIÊNG cho pha `starhill-qr` (tách khỏi journal `platform-base`). ID tiền tố `QR-AD-###`. Mỗi bản ghi có Provenance/Evidence thật.

---

### QR-AD-001 — Sản phẩm QR = bản VENDORED-COPY của base tại `starhill/`; `platform/` giữ SẠCH/tái dùng
- Status: Superseded by QR-AD-012 (2026-07-13) — mô hình vendored-copy đã sinh drift 50-file thực đo (P0-1/2/3 catastrophic); user duyệt lại chuyển sang D1-a cross-tree ProjectReference. Provenance/Evidence bên dưới giữ nguyên cho lịch sử.
- Date: 2026-07-10
- Decider: user (chốt) — "nếu làm qr trên base cần copy ra rồi làm để giữ sạch base; trước khi làm phải đảm bảo base tốt".
- Provenance/Evidence: cổng base-tốt verify qua `platform\scripts\vp.cmd` (build 0-warning + 247 test 0-fail + validate-ci OK) + `vp journal` (JournalConsistency 5/5). Copy `robocopy platform starhill /E /XD bin obj` (exit=1 = thành công). Verify copy: `starhill\scripts\vp.cmd build` → 0-warning độc lập (mọi project build từ `starhill\src\...`).
- Context: `platform-base` là base domain-agnostic tái dùng (có thể tách repo riêng). Nếu nhồi module QR thẳng vào `platform/` sẽ trộn "base tái dùng" với "sản phẩm cụ thể" → phá tính tái dùng/cô lập.
- Decision/Change: (1) COPY `platform/` → `starhill/` (nguồn, loại bin/obj) làm điểm khởi đầu sản phẩm StarHill; (2) `platform/` ĐÓNG BĂNG sạch (không thêm nghiệp vụ QR); (3) module QR + tuỳ biến sản phẩm chỉ làm trong `starhill/`; (4) verify base tốt TRƯỚC copy.
- Rationale (verifiable): giữ base sạch = tái dùng cho dự án khác + tách repo dễ (đúng ý user nhiều lần). Vendored-copy = sản phẩm tự chủ hoàn toàn (đổi base cho nhu cầu sản phẩm không ảnh hưởng base gốc). Verify-trước-copy = không nhân bản lỗi.
- Alternatives: (a) module QR trong `platform/` (loại: trộn base+sản phẩm, phá cô lập — user bác); (b) product solution tham chiếu Bedrock qua project-ref/NuGet (khả thi hơn về "một nguồn base" nhưng user chọn copy để base sạch tuyệt đối + sản phẩm tự chủ).
- Consequences: `starhill/` có bản copy Bedrock.* riêng → cải tiến base về sau KHÔNG tự chảy sang sản phẩm (và ngược lại) — xem QR-TO-001. Sản phẩm tự do sửa base-copy cho nhu cầu QR.
- Reversibility: Medium (đã copy; đổi mô hình = re-structure).
- Traceability: user directive 2026-07-10; base `platform-base` (nguồn copy); QR-TO-001 (tradeoff vendored).

---

### QR-AD-002 — Phân rã 8 module 5-project + Dashboard-ở-Host; cross-module Id-trần (không FK chéo schema) + cascade đồng bộ
- Status: Partially superseded by QR-AD-027 (2026-07-14) — phân rã/cross-module Id-trần còn hiệu lực; chỉ lựa chọn cascade đồng bộ bị thay bằng outbox/inbox at-least-once vì DI scope không tạo transaction chung cho nhiều DbContext.
- Date: 2026-07-11
- Decider: AI (spec QR không nói cách map sang module Bedrock).
- Provenance/Evidence: đọc mã `starhill/`: `PlatformDbContext` (per-module DbContext, schema riêng, xmin chỉ Npgsql, dispatch domain-event cùng transaction), `IdentityDbContext` (`HasDefaultSchema("identity")` + `AddOutboxInbox` + `AddRefreshTokens` per-schema), `Program.cs` Host (`AddIdentityInfrastructure(UseNpgsql(cs))` + `AddIdentityApi`, mỗi module một connection string). Đọc `docs/resort-qr-portal/design.md` §Data Models + §DB constraints (partial unique index, cross-entity refs). Đọc `resort-qr/src` (bản cũ monolith 1 DbContext — nguồn port).
- Context: design QR gốc liệt kê 11 nhóm (Identity/Resorts/Rooms/QrTokens/GuestAccess/Rules/Faq/Messaging/Housekeeping/Notes/Dashboard) trên MỘT DbContext. Bedrock bắt buộc per-module DbContext + schema riêng + cấm FK chéo schema.
- Decision/Change: gom thành **8 module** (Identity, ResortConfig[=Resorts+i18n], Rooms[+QrTokens], GuestAccess, Rules, Faq, Concierge[=Messaging+Notes], Housekeeping) — mỗi module 5-project + schema riêng; **Dashboard KHÔNG là module** mà ghép ở Host (đọc query-port mỗi module). Cross-module tham chiếu bằng **Id trần (Guid), KHÔNG FK chéo schema**; nhất quán bằng kiểm tầng Application qua `<M>.Contracts` trong cùng Host. Cascade khi kết thúc GuestVisit (đóng hội thoại + huỷ ticket) làm **đồng bộ trong Host (A)**, không event-driven (B).
- Rationale (verifiable): (1) gom theo cohesion (QrTokens 1-1 Room; i18n phụ thuộc ResortLanguage; Notes cùng ngữ cảnh Inbox) tránh phân mảnh thừa (I10). (2) per-module schema là bất biến base (F31/I6) — không thể 1 DbContext. (3) partial unique index vẫn đặt được trong schema module sở hữu (một PostgreSQL vật lý). (4) cascade đồng bộ hợp modular monolith một-process, nhất quán tức thời > nới ghép (event-driven để dành khi tách tải — QR-TO-002).
- Alternatives: (a) 11 module tách hết (loại: phân mảnh, ranh giới vô giá trị); (b) 1 DbContext như bản cũ (loại: phá F31 base); (c) cascade event-driven qua outbox (để dành — cần RabbitMQ + eventual consistency).
- Consequences: Host `StarHill.Api` ghép 8 module + connection string/schema mỗi module (cùng 1 DB). Dashboard endpoints ở Host. Guard ModuleBoundaryTests phải chắc module chỉ ref `<M>.Contracts` module khác.
- Reversibility: Medium (đổi phân rã = re-structure project).
- Traceability: design.md §2/§3/§4/§6; `docs/resort-qr-portal/design.md`; QR-TO-002; QR-TO-003.

### QR-AD-003 — Mặc định deploy: cùng-origin `portal.starhill.local` + internal CA (DNS nội bộ)
- Status: Proposed (mặc định — user cho phép chốt mặc định nếu không nêu)
- Date: 2026-07-11
- Decider: AI (2 quyết định còn mở trong README pha QR).
- Provenance/Evidence: `docs/resort-qr-portal/design.md` §Deployment ("MVP nên cùng origin `https://portal.starhill.local`"; "bắt buộc HTTPS cert hợp lệ cho camera StaffScan/getUserMedia; tránh self-signed; internal CA hoặc cert công khai qua DNS nội bộ"). `requirements.md` Req 12.5.
- Decision/Change: (1) **cùng-origin** (`/`→guest-web, `/admin`→admin-web, `/v1`→API, `/hubs`→SignalR sau reverse proxy); (2) **internal CA** (root cài thiết bị) trên DNS nội bộ thật; cert công khai qua DNS nội bộ là phương án thay thế.
- Rationale (verifiable): cùng-origin đơn giản cookie/CORS (guest cookie SameSite=Lax đủ); secure-context bắt buộc cho camera; internal CA kiểm soát được, tránh self-signed bị điện thoại chặn. Đúng khuyến nghị design gốc.
- Alternatives: tách subdomain (guest./admin./api.) — để dành khi cần tách bundle/bảo mật; self-signed (loại — điện thoại cảnh báo/chặn camera).
- Reversibility: Easy (cấu hình reverse proxy/cert, chưa động code).
- Traceability: design.md §9; README pha QR "Quyết định còn mở".

### QR-AD-004 — Module messaging đặt tên "Concierge" (tránh đụng Bedrock.Messaging)
- Status: Proposed
- Date: 2026-07-11
- Decider: AI.
- Provenance/Evidence: base có `Bedrock.Messaging.Contracts` (integration-event/outbox — hạ tầng). Nghiệp vụ QR "nhắn tin khách↔lễ tân" trùng khái niệm "messaging".
- Decision/Change: module chat guest↔staff (Conversation/Message/InternalNote + ChatHub) tên **Concierge** (schema `concierge`), KHÔNG tên "Messaging".
- Rationale (verifiable): tránh nhầm lẫn đọc mã lâu dài giữa "Messaging" hạ tầng (bus sự kiện) vs nghiệp vụ chat. Tên "Concierge" mô tả đúng vai trò lễ tân.
- Reversibility: Easy (đổi tên trước khi tạo project).
- Traceability: design.md §2.

### QR-AD-005 — Role Admin/Staff → policy RequireAdmin/RequireStaff (Admin superset)
- Status: Proposed
- Date: 2026-07-11
- Decider: AI.
- Provenance/Evidence: `resort-qr` domain `UserRole` (Admin/Staff). Base dùng permission-based `RequirePermissionAttribute` + policy (đọc `Bedrock.Application/Authorization`).
- Decision/Change: map Role→policy: Admin→`RequireAdmin`, Staff→`RequireStaff`; Admin là superset của Staff (Admin qua được endpoint Staff). Áp ở module Identity + các admin endpoint.
- Rationale (verifiable): giữ mô hình role đơn giản của sản phẩm (2 vai) mà vẫn dùng cơ chế authorization của base; superset khớp yêu cầu Req 11.3 (Admin có thêm quyền của Staff).
- Reversibility: Medium.
- Traceability: design.md §4.1; requirements Req 11.1/11.3.

### QR-AD-006 — Tạo test project sản phẩm `StarHill.ArchitectureTests` + `StarHillJournalConsistencyTests` (anti-drift journal QR tự động)
- Status: Done
- Date: 2026-07-11
- Decider: AI (hiện thực hoá QR-N-003/N-004 + yêu cầu user "cách cực mạnh tránh drift").
- Provenance/Evidence: đọc `platform/tests/Bedrock.ArchitectureTests/JournalConsistencyTests.cs` (cơ chế L4/AD-030, INV-1..5) + bản copy `starhill/tests/Bedrock.ArchitectureTests/JournalConsistencyTests.cs` — bản copy `RequireJournalDir()` đi lên tìm `.kiro/specs/platform-base/journal` → **chỉ validate journal BASE, KHÔNG bảo vệ journal QR** (đi lên từ starhill/tests vẫn gặp `.kiro` repo-root chứa platform-base). `starhill/tools/verify.ps1` `Step-Journal` chỉ chạy `Bedrock.ArchitectureTests`. → journal QR trước đó không có cổng tự động. Sau khi làm: `starhill\scripts\vp.cmd build` = **0-warning** (project mới build OK); `vp journal` = **StarHill.ArchitectureTests 5/5** (INV-1..5 tiền tố QR) + **Bedrock.ArchitectureTests 5/5** (base).
- Context: pha QR cần cổng anti-drift tương đương base cho journal `starhill-qr`.
- Decision/Change: (1) tạo `starhill/tests/StarHill.ArchitectureTests/` (test project SẢN PHẨM, tách khỏi test base vendored — nơi ở tương lai của guard test QR CP1..15 + ModuleBoundary); (2) `StarHillJournalConsistencyTests` mirror INV-1..5 với regex tiền tố `QR-` + journal path `.kiro/specs/starhill-qr/journal` + CP range 1..15; (3) đăng ký vào `Platform.slnx`; (4) cập nhật `verify.ps1` `Step-Journal` chạy CẢ hai project (logic mới đặt vào script cố định — đúng governance, KHÔNG đổi tên lệnh `vp`).
- Rationale (verifiable): project riêng = tách governance sản phẩm khỏi base vendored (giữ base copy sạch hơn, đúng long-term); mirror đúng cơ chế đã chứng minh (fix tận gốc, không chế lại lệch); tiền tố `QR-` để không đụng token base (`AD-###` trần trong journal QR bị regex QR bỏ qua). Test là chính guard cho mọi QR-AD (kể cả QR-AD-006 này — keystone tự-thực thi).
- Alternatives: (a) nhét test journal QR vào `Bedrock.ArchitectureTests` (loại: làm bẩn base copy, không phải nơi ở lâu dài của guard QR); (b) đổi `RequireJournalDir` bản copy trỏ QR (loại: mất luôn cổng cho journal base).
- Consequences: `vp journal` giờ = 10 test (5 base + 5 QR). Guard QR mọi tài liệu journal QR = build gate.
- Reversibility: Easy. Traceability: QR-N-003, QR-N-004; design.md §10 (Wave A); base AD-030.

### QR-AD-007 — CI sản phẩm riêng `.github/workflows/starhill-ci.yml` (nhắm starhill/) + retarget `validate_ci.py`
- Status: Done
- Date: 2026-07-11
- Decider: AI (hoàn tất phần CI của QR-N-003; spec không nói cách dựng CI cho bản vendored).
- Provenance/Evidence: đọc `.github/workflows/ci.yml` (CI base — mọi bước `working-directory: platform`, 3 job build-test/docker-image/migration-bundle) + `starhill/tests/validate_ci.py` bản copy (`parents[2]`=repo-root, `CI_PATH=ci.yml` → đang validate CI BASE, không phải CI sản phẩm). GitHub chỉ đọc workflow ở `<repo-root>/.github/workflows/`. Sau khi làm: `starhill\scripts\vp.cmd ci` → `VALIDATE CI: OK (starhill-ci.yml ... nhắm starhill/)`; `vp all` → build 0-warning + validate-ci OK + **test 252 (235 pass / 17 skip Docker / 0 fail)**.
- Context: mono-repo chứa CẢ base (`platform/`) và sản phẩm (`starhill/`). Cần CI riêng cho sản phẩm mà không đụng CI base.
- Decision/Change: (1) tạo workflow root `starhill-ci.yml` mirror cấu trúc base (push main/master/develop + PR; concurrency cancel-in-progress; permissions contents:read; job build-test/docker-image/migration-bundle + timeout-minutes) NHƯNG mọi bước `working-directory: starhill` + `global-json-file: starhill/global.json`; build-test chạy full suite (gồm CẢ JournalConsistencyTests base VÀ StarHillJournalConsistencyTests QR). (2) retarget `starhill/tests/validate_ci.py`: `CI_PATH=starhill-ci.yml` + thêm bất biến raw `REQUIRED_TARGET_TOKENS` (`working-directory: starhill`, `global-json-file: starhill/global.json`) chống drift trỏ nhầm base.
- Rationale (verifiable): hai workflow tách biệt cùng repo-root, concurrency group theo `github.workflow` (tên khác) → không hủy lẫn nhau; validator sản phẩm phải verify CI thực nhắm starhill (nếu ai đó sửa trỏ về platform → FAIL). migration-bundle CHỈ Identity vì đó là module duy nhất có migration hiện tại — thêm bundle khi có module QR mới (không gold-plate, I10).
- Alternatives: (a) một CI matrix cho cả platform+starhill (loại: ghép base+product, khó tiến hóa độc lập); (b) không CI sản phẩm (loại: sản phẩm thương mại bắt buộc có CI).
- Consequences: commit+push `starhill/` → GitHub chạy `starhill-ci.yml`. Thêm module QR có migration → thêm job bundle tương ứng.
- Reversibility: Easy. Traceability: QR-N-003; QR-AD-006; base AD-061/AD-050/AD-055.

### QR-AD-008 — ResortConfig persistence tối giản (no outbox/inbox/refresh-token) + seed bằng seeder runtime idempotent
- Status: Proposed (design Wave B; áp khi build slice B.1)
- Date: 2026-07-11
- Decider: AI (spec không nói chi tiết persistence per-module trên Bedrock).
- Provenance/Evidence: đọc `starhill/src/Modules/Identity/.../IdentityDbContext.cs` — `AddOutboxInbox`/`AddRefreshTokens` là opt-in GỌI TƯỜNG MINH trong `OnModelCreating` của module (không mặc định). Đọc `PlatformDbContext.OnModelCreating` — RowVersion→xmin `ValueGeneratedOnAddOrUpdate` (store-generated). Đọc `resort-qr/.../Persistence/ResortSeeder.cs` (precedent seeder runtime). Cascade đóng-visit chọn ĐỒNG BỘ (QR-AD-002) → ResortConfig chưa cần phát integration-event.
- Decision/Change: (1) `ResortConfigDbContext` chỉ `HasDefaultSchema("resort_config")` + DbSet; KHÔNG `AddOutboxInbox`/`AddRefreshTokens` (module không phát event, không auth-token). (2) Seed (Resort + ResortSettings + ResortLanguage en/vi/ko/zh, `en` IsDefault) bằng `ResortConfigSeeder` runtime idempotent gọi từ Host sau migrate (gated cờ dev/compose), KHÔNG dùng `HasData`.
- Rationale (verifiable): tối giản đúng nhu cầu (I10) — thêm outbox khi thực sự phát event; HasData xung đột cột xmin store-generated + audit-field (CreatedAt/actor) → seeder idempotent an toàn + khớp precedent resort-qr.
- Alternatives: (a) luôn AddOutboxInbox mọi module (loại: thừa bảng/khớp nối không dùng); (b) HasData seed (loại: xung đột xmin/audit, khó bảo trì).
- Consequences: nếu sau này ResortConfig cần phát "settings changed" → thêm AddOutboxInbox + outbox dispatcher lúc đó.
- Reversibility: Easy (chưa code). Traceability: design-modules/01-resortconfig.md §4; QR-AD-002.

### QR-AD-009 — Migration EF = generated_code (miễn analyzer) qua `.editorconfig` scoped Migrations
- Status: Done
- Date: 2026-07-11
- Decider: AI (build gate 0-warning bắt lỗi trong code sinh tự động).
- Provenance/Evidence: `vp build` FAIL đúng 1 lỗi **CA1861** tại `ResortConfig.Infrastructure/Persistence/Migrations/20260711112306_InitialCreate.cs(102)` — EF sinh `columns: new[] { "resort_id", "code" }` cho unique index composite `ux_lang_code`. Base Identity KHÔNG dính vì migration Identity không có index composite `new[]` (chỉ single-column + filter). Sau fix: `vp build` 0-warning; `vp all` xanh (test 262, 0 fail).
- Context: `TreatWarningsAsErrors=true` + `EnableNETAnalyzers` (Directory.Build.props) áp cho MỌI .cs, gồm code EF sinh. Sửa tay file migration = fragile (regenerate mất) → sửa ngọn.
- Decision/Change: thêm vào `starhill/.editorconfig` section `[**/Persistence/Migrations/*.cs]` với `generated_code = true` → analyzer coi migration là code sinh và bỏ qua (CA1861 và các rule tương tự). Chính sách áp cho MỌI module (Identity/ResortConfig/Rooms/... về sau).
- Rationale (verifiable): migration là artifact SINH TỰ ĐỘNG bởi `dotnet ef` — không phải surface code người viết → đúng bản chất là generated_code; đánh dấu tận gốc chặn cả lớp lỗi analyzer cho mọi migration tương lai (không phải fix từng file). Chuẩn công nghiệp.
- Alternatives: (a) sửa tay migration dùng `static readonly` array (loại: regenerate mất — sửa ngọn); (b) `dotnet_diagnostic.CA1861.severity=none` scoped (hẹp hơn nhưng chỉ chặn 1 rule — migration còn có thể dính rule khác); (c) NoWarn ở csproj (kém tường minh hơn editorconfig-by-path).
- Consequences: `.editorconfig` starhill khác base platform/ (base chưa cần — migration base chưa có composite). Base platform/ ĐÓNG BĂNG, không sửa (QR-N-002); nếu base sau này gặp cùng lỗi thì áp cùng chính sách ở base.
- Reversibility: Easy. Traceability: design-modules/01-resortconfig.md §4; QR-N-002 (base frozen).

### QR-AD-010 — Bổ sung năng lực nền `UniqueConstraintViolationException` vào base (platform/) rồi copy
- Status: Proposed (design Wave Rooms; sẽ triển khai slice B-Rooms.0 — đụng base, làm cẩn thận)
- Date: 2026-07-11
- Decider: AI (phát hiện gap khi thiết kế Rooms; spec không nói).
- Provenance/Evidence: đọc `starhill/src/Bedrock.Infrastructure/Persistence/EfUnitOfWork.cs` — CHỈ dịch `DbUpdateConcurrencyException`→`ConcurrencyConflictException`. grep base `DbUpdateException|23505|PostgresException|UniqueConstraint` = **0 match**. `Bedrock.Domain/Results` không có UniqueConstraintViolationException (resort-qr có ở SharedKernel; use case Rooms Create/Rotate bắt nó). Application ⊥ EF (matrix) → không được bắt DbUpdateException ⇒ cần exception trung lập ở Domain.
- Decision/Change: thêm ở BASE `platform/` (QR-N-002 — năng lực nền, không phải nghiệp vụ QR; thêm ở base rồi copy sang starhill/): (1) `Bedrock.Domain/Results/UniqueConstraintViolationException.cs` (mirror ConcurrencyConflictException, mang `ConstraintName?`); (2) `EfUnitOfWork.SaveChangesAsync` catch `DbUpdateException` inner Postgres SqlState `23505` → ném nó (catch DbUpdateConcurrencyException TRƯỚC vì là con); (3) guard test base Testcontainers; (4) verify `platform\scripts\vp.cmd` rồi copy Bedrock.* đã đổi sang starhill/ + cập nhật base journal (AD mới) — giữ 2 bản đồng bộ.
- Rationale (verifiable): unique-violation→neutral-exception là năng lực persistence TÁI DÙNG mọi module (song song concurrency đã có); đặt ở base giữ Application provider-agnostic + race-safe (DB-constraint là nguồn sự thật, không TOCTOU). Bedrock.Infrastructure đã coupling Npgsql (PlatformDbContext dùng Database.IsNpgsql()) → detect PostgresException nhất quán.
- Alternatives: (a) bắt DbUpdateException trong module (loại: Application ⊥ EF); (b) pre-check AnyAsync thay vì constraint (loại: TOCTOU race, không an toàn thương mại); (c) nhồi exception vào module (loại: không tái dùng, trùng lặp mỗi module).
- Consequences: đụng base platform/ (đóng băng) — hợp lệ theo QR-N-002 cho năng lực nền; cần re-copy sang starhill/. Base journal có thêm AD.
- Reversibility: Medium. Traceability: design-modules/02-rooms.md §1; QR-N-002; base ConcurrencyConflictException (mẫu).

### QR-AD-011 — Rooms là module kế tiếp (sửa thứ tự) + kích hoạt query port ResortConfig + defer Rooms.Api
- Status: Proposed (design; triển khai slice B-Rooms.1/.2)
- Date: 2026-07-11
- Decider: AI.
- Provenance/Evidence: `docs/resort-qr-portal/tasks.md` graph (task 5 GuestAccess dependsOn task 4 Rooms) + `../design.md` §10 (Wave C GuestAccess phụ thuộc Rooms). README trước ghi "next=GuestAccess" — SAI thứ tự. `RenderRoomQrPngUseCase` (resort-qr) đọc `ResortSettings.GuestWebBaseUrl`.
- Decision/Change: (1) làm **Rooms TRƯỚC** GuestAccess; (2) kích hoạt query port đã hoãn ở ResortConfig: thêm `IResortSettingsQuery` (Contracts) + `EfResortSettingsQuery` (Infra) — consumer thật là Rooms.RenderQrPng (đọc GuestWebBaseUrl); (3) `Rooms.Contracts` lộ `IRoomTokenResolver` (cho GuestAccess.resolve + Housekeeping.complete-by-token) + `IRoomStats` (Dashboard); (4) defer `Rooms.Api` (admin CRUD) tới slice cần Identity auth + Role→policy (QR-AD-005), mirror ResortConfig.
- Rationale (verifiable): tôn trọng dependency graph (fix drift thứ tự); query port ResortConfig giờ có consumer thật → hết "speculative API" (đúng I10 — build khi cần); IRoomTokenResolver là surface cross-module đúng cho các module hạ nguồn.
- Alternatives: làm GuestAccess trước (loại: thiếu resolver Rooms → không resolve được token).
- Reversibility: Easy (design). Traceability: design-modules/02-rooms.md §0/§2/§7; QR-AD-002; ResortConfig QR-N-009 (query port hoãn).

### QR-AD-012 — Đảo mô hình tiêu thụ base: D1-a cross-tree ProjectReference (xóa bản-copy base) — SUPERSEDES QR-AD-001
- Status: Done
- Date: 2026-07-13
- Decider: user (duyệt phiên này — chọn "Duyệt D1-a — chạy liền một mạch tới xanh" qua userInput) sau khi AI trình bày lý do fix-tận-gốc.
- Provenance/Evidence: (1) drift ĐO THẬT khi so `platform/src` vs `starhill/src` (Bedrock.*+Adapters, loại bin/obj): same=86, differ=50, only-platform=7, only-starhill=2 → bản-copy đã lệch 50 file, KHÔNG có keyed persistence + mọi fix hardening base. (2) design `platform-base/design.md:16` "lõi nền tái sử dụng cho **nhiều dự án**" → base phải THUẦN. (3) Sau D1-a: `dotnet build Platform.slnx` = 0 warning/0 error (baseline trước đó cũng 0/0 → so sánh sạch); full test 66/0-fail; Host boot `WebApplicationFactory` (ValidateOnBuild) pass.
- Context: QR-AD-001 chọn vendored-copy để "base sạch tuyệt đối + sản phẩm tự chủ". Thực tế: bản-copy KHÔNG được đồng bộ → lệch 50 file, thiếu keyed persistence → P0-1 catastrophic. Bản-copy là GỐC RỄ của drift.
- Decision/Change: (1) XÓA khỏi starhill 6 project base bản-copy (`Bedrock.Domain/Application/Api/Infrastructure/Messaging.Contracts` + `Adapters/Messaging.RabbitMq`) + 6 project test-base bản-copy (`Bedrock.{UnitTests,Api.Tests,ArchitectureTests,Infrastructure.Tests}`, `Messaging.IntegrationTests`, `Adapters/...RabbitMq.Tests`). (2) Mọi module/Host/test nghiệp vụ ProjectReference thẳng `platform/src/*` qua property `$(PlatformSrc)=$(MSBuildThisFileDirectory)..\platform\src` (Directory.Build.props). (3) `starhill/Platform.slnx` chỉ còn project nghiệp vụ QR (base build transitive). (4) `verify.ps1`/`starhill-ci.yml` chỉ gác journal QR; journal base do platform/ gác.
- Rationale (verifiable): fix TẬN GỐC — xóa bản base thứ hai ⇒ chỉ còn MỘT base vật lý ⇒ drift BẤT KHẢ THI (D1-c diff-guard chỉ *phát hiện* drift = fix ngọn). Không cản đa-sản-phẩm: monorepo nhiều sản phẩm cùng ProjectReference `platform/src` là pattern chuẩn. `Directory.*.props` phân giải theo cây từng .csproj → base dùng props platform/, sản phẩm dùng props starhill/ → không lẫn version.
- Alternatives: (a) D1-a' MERGE starhill vào platform/Platform.slnx (loại: nhồi module 1 sản phẩm vào base tái-dùng-nhiều-dự-án — bẩn base); (b) D1-c giữ 2 cây + CI diff-guard (loại: gốc rễ 2-bản vẫn còn = fix ngọn); (c) D1-b NuGet nội bộ (để dành cho khi sản phẩm TÁCH REPO — version isolation; hiện premature).
- Consequences: starhill build phụ thuộc platform/ hiện diện cạnh nó (chấp nhận: cùng repo). Base tiến hóa → sản phẩm nhận NGAY lúc compile (fail-fast tốt, không drift âm thầm). Docker build context phải = repo root (xem QR-AD-015).
- Reversibility: Medium (git revert khôi phục; nhưng không nên — đây là hướng đúng lâu dài).
- Traceability: SUPERSEDES QR-AD-001; QR-AD-013/014/015 (các P0 fix xây trên nền này); QR-TO-004; platform-base design §Overview line 16.

### QR-AD-013 — Keyed persistence 3 module (fix P0-1 catastrophic last-registration-wins)
- Status: Done
- Date: 2026-07-13
- Decider: AI (áp keyed API của base mới; cơ chế đã có sẵn ở platform — mirror module Identity mẫu).
- Provenance/Evidence: (1) TRƯỚC: `starhill/src/Host/Program.cs` gọi `AddIdentityInfrastructure→AddResortConfigInfrastructure→AddRoomsInfrastructure` tuần tự, mỗi cái `AddBedrockPersistence<TContext>(cfg)` UNKEYED → Microsoft DI last-wins → `IUnitOfWork`/`IOutboxWriter`/`IRefreshTokenStore`/`PlatformDbContext`/generic `IRepository<>` resolve về `RoomsDbContext` cho MỌI module. (2) base mới `ICommandUseCase<TInput> : ITransactionalUseCase` (AD-098) compile-enforce `PersistenceKey` → build FAIL đúng 3 void command Rooms (Update/ChangeStatus/Delete) → xác nhận gap tồn tại. (3) Sau fix: build 0-warning; Host boot `ValidateOnBuild=true` pass (StarHill.Api.Tests 3/3); Identity/Rooms/ResortConfig integration (Testcontainers) pass.
- Decision/Change: (1) thêm hằng module key ở Contracts: `IdentityModule/ResortConfigModule/RoomsModule.PersistenceKey` (= schema `identity`/`resort_config`/`rooms`). (2) mỗi Infrastructure DI dùng overload KEYED `AddBedrockPersistence<TContext>(PersistenceKey, cfg)`; Identity thêm `AddBedrockOutbox/Inbox/RefreshTokens<TContext>(key)` (khớp `IdentityDbContext.OnModelCreating`); Rooms thêm `AddBedrockRepository<RoomsDbContext, Room/RoomQrToken>(key)`. (3) use case resolve port qua FACTORY `GetRequiredKeyedService<T>(PersistenceKey)` (mirror platform Identity). (4) 3 void command Rooms khai `public string PersistenceKey => RoomsModule.PersistenceKey`. (5) Host messaging keyed: `AddOutboxDispatcher<IdentityDbContext>(key)`, `AddIntegrationEventConsumer<IdentityDbContext>(key)`, `AddKeyedScoped<IIntegrationEventHandler<...>>(key)`, `o.DispatcherServiceKey=key`.
- Rationale (verifiable): keyed = mỗi port phụ thuộc context resolve theo module key → KHÔNG còn last-registration-wins; compile-enforce (ICommandUseCase) bắt use case void quên key ngay lúc build (không chờ runtime). ResortConfig chỉ cần persistence keyed (seeder/query inject `ResortConfigDbContext` cụ thể — không dùng port dùng chung) nhưng VẪN phải keyed để không đụng `PersistenceRegistrationRegistry` (2 DbContext unkeyed = ném).
- Alternatives: (a) port riêng keyed cho từng module thủ công (loại: trùng lặp; base đã có keyed API); (b) giữ unkeyed + tách Host thành 3 process (loại: phá modular-monolith một-process của design).
- Consequences: test Identity.IntegrationTests phải resolve keyed (`GetRequiredKeyedService<IRefreshTokenStore/IUnitOfWork/IOutboxWriter>(key)`) — đã cập nhật. Module mới sau này BẮT BUỘC theo khuôn keyed.
- Reversibility: Medium. Traceability: QR-AD-012 (nền); base AD-098 (compile-enforce ICommandUseCase); design §4.6; platform Modules/Identity (mẫu keyed).

### QR-AD-014 — Một PostgreSQL vật lý, schema-per-module (fix P0-2) + regenerate migration Identity khớp base mới
- Status: Done
- Date: 2026-07-13
- Decider: AI (sửa code cho khớp design đã chốt — KHÔNG phải quyết định mở).
- Provenance/Evidence: (1) TRƯỚC: `appsettings.json` trỏ 3 DB khác nhau (`starhill_identity`/`starhill_resort_config`/`starhill_rooms`) — LỆCH design `platform-base/design.md` §1.2 line 38 "một PostgreSQL vật lý (schema-per-module)" + §4.6 line 402 "1 DbContext + 1 schema... Cùng một PostgreSQL vật lý nhưng tách schema". (2) Sau fix, boot docker-compose THẬT: log Host `Applying migration InitialCreate` → `CREATE SCHEMA identity` + `CREATE SCHEMA resort_config` + `CREATE SCHEMA rooms` — cả 3 schema tạo trong CÙNG DB `starhill`; `/health/ready`=200.
- Decision/Change: (1) `appsettings.json` gộp cả 3 connection về `Database=starhill` (schema tách ở `DbContext.HasDefaultSchema`, giữ nguyên). (2) Vì base mới đổi schema outbox/inbox/refresh, model `IdentityDbContext` lệch migration cũ (`PendingModelChangesWarning` fail Identity.IntegrationTests) → REGENERATE migration Identity `InitialCreate` (`dotnet ef migrations add`, timestamp 20260713075245) khớp model base mới. ResortConfig/Rooms KHÔNG map outbox/inbox/refresh → migration không lệch (integration pass không cần regen).
- Rationale (verifiable): khớp design "1 DB nhiều schema" — migration per-module tạo `__EFMigrationsHistory` riêng trong schema mình → deploy/migrate độc lập (design §4.6) mà vẫn một DB. Regenerate (không delta) vì module skeleton chưa deploy → một `InitialCreate` sạch phản ánh model hiện tại (delta trên migration chưa từng áp = nhiễu).
- Alternatives: (a) giữ 3 DB (loại: lệch design, tốn tài nguyên, mất "một transaction/atomic khả năng liên schema"); (b) thêm delta migration (loại: skeleton chưa deploy → InitialCreate sạch tốt hơn).
- Consequences: deploy tạo 1 database `starhill`, migrate 3 bundle (Identity/ResortConfig/Rooms) vào đó. `PendingModelChangesWarning` test (Identity.IntegrationTests) giờ là guard chống migration-lệch-model.
- Reversibility: Easy (config + migration files). Traceability: QR-AD-013 (base keyed đổi schema); design §1.2/§4.6.

### QR-AD-015 — Compose boot end-to-end 3 module (fix P0-3): repo-root Docker context + RabbitMQ readiness gate
- Status: Done
- Date: 2026-07-13
- Decider: AI.
- Provenance/Evidence: (1) TRƯỚC: compose `postgres` chỉ tạo `starhill_identity` + host chỉ override `ConnectionStrings__Identity` → ResortConfig/Rooms trỏ `localhost` trong container + DB không tồn tại → migrate fail-fast → boot hỏng. (2) D1-a khiến Host ProjectReference `platform/src` (ngoài context `starhill/`) → Docker build context `starhill/` không thấy `platform/`. (3) Boot thực tế lộ RabbitMQ `BrokerUnreachableException ---> SocketException(111) Connection refused` — consumer kết nối lúc AMQP 5672 chưa mở (healthcheck `ping` báo healthy trước khi listener sẵn) → BackgroundService StopHost → Kestrel bind hủy → host crash. (4) Sau 3 fix: `docker compose up` → cả 3 container Up, `/health/ready`=200, `/health/live`=200.
- Decision/Change: (1) compose `postgres` `POSTGRES_DB=starhill` + host override CẢ 3 `ConnectionStrings__{Identity,ResortConfig,Rooms}` → service `postgres`/DB `starhill`. (2) Dockerfile build context = REPO ROOT (compose `context: ..`, `dockerfile: starhill/src/Host/StarHill.Api/Dockerfile`); Dockerfile `COPY platform/ platform/` + `COPY starhill/ starhill/` (giữ layout tương đối → `$(PlatformSrc)=/src/platform/src`); thêm `.dockerignore` ở repo root (loại bin/obj/.git). (3) RabbitMQ healthcheck `rabbitmq-diagnostics check_port_connectivity` (+ `start_period: 30s`) — gate `depends_on: service_healthy` tới khi AMQP 5672 thật sự nhận kết nối. (4) `starhill-ci.yml` job `docker-image` build context repo root khớp Dockerfile mới.
- Rationale (verifiable): 1-DB + override đủ 3 connection = migrate 3 schema chạy (verify log). Context repo-root = hệ quả bắt buộc của D1-a (base ở cây khác). `check_port_connectivity` kiểm listener THẬT (mạnh hơn `ping` chỉ báo Erlang node up) → hết race Connection-refused.
- Alternatives readiness: (a) thêm retry connect trong consumer (thuộc base platform/ — ngoài phạm vi P0 starhill; để lại resilience sau); (b) giữ `ping` (loại: race đã chứng minh gây crash).
- Consequences: build Docker sản phẩm cần checkout cả platform/ + starhill/ (mono-repo — đã vậy). Broker restart lúc chạy vẫn có thể StopHost (resilience base — defer, mirror platform N-079).
- Reversibility: Easy. Traceability: QR-AD-012 (context hệ quả D1-a); QR-AD-014 (1-DB); design §1.2.


---

### QR-AD-016 — CreateRoom thẩm định tồn tại Resort qua query port (P1(a) — toàn vẹn tham chiếu cross-module)
- Status: Done
- Date: 2026-07-13
- Decider: AI (máy toann, không Docker — verify bằng unit test thuần).
- Provenance/Evidence: đọc `CreateRoomUseCase.cs` — dùng `input.ResortId` TRỰC TIẾP, chỉ `CreateRoomValidator` kiểm `ResortId.NotEmpty()` (rỗng), KHÔNG kiểm resort có TỒN TẠI → tạo được phòng trỏ tới resortId không có thật (phòng "mồ côi"). Vì schema-per-module + KHÔNG FK chéo-schema (QR-DV-003) nên DB không chặn. Đã thêm: query port `IResortExistenceQuery` (ResortConfig.Contracts.Queries) + impl `EfResortExistenceQuery` (AsNoTracking + AnyAsync, chỉ EXISTS) + đăng ký scoped ở `AddResortConfigInfrastructure`; `CreateRoomUseCase` inject + kiểm TRƯỚC token-gen; resort không tồn tại → `RoomsErrors.ResortNotFound` (code mới `resort_not_found`, NotFound). Guard MỚI: project `Rooms.UnitTests` (Rooms trước đây KHÔNG có unit test — vá gap) + `CreateRoomUseCaseTests` (2 test, fake port, KHÔNG Docker): (1) resort vắng → ResortNotFound + KHÔNG Add/Save/token-gen; (2) resort tồn tại → tạo phòng+token Active/Version=1, Save 1 lần. 2/2 pass.
- Decision/Change: toàn vẹn tham chiếu resort làm ở SEAM ứng dụng (query port cross-module qua Contracts — F30), KHÔNG phải validator (validator phải thuần/không I/O; kiểm tồn tại là STATEFUL cần DB). Kiểm đứng TRƯỚC token-gen để dừng sớm, không tốn công/không side-effect. Thêm error code riêng `resort_not_found` (phân biệt với `not_found` của phòng) để client localize.
- Rationale (verifiable): **Root cause:** không có ràng buộc nào chặn resortId ma (không FK chéo-schema — QR-DV-003) → phải kiểm ở tầng ứng dụng. Đặt trong use case (không validator) vì FluentValidation async-DB-rule là anti-pattern (validator nên thuần) và đây là bất biến nghiệp vụ, không phải shape input. Query port giữ Rooms.Application ⊥ ResortConfig.{Domain,Infrastructure} (chỉ .Contracts — RoomsBoundaryTests vẫn xanh).
- Alternatives: (a) kiểm trong CreateRoomValidator qua async rule (loại: validator phụ thuộc DB/port, phá thuần-tính + khó test + chạy 2 lần nếu re-validate); (b) không kiểm, dựa caller (loại: mất toàn vẹn — chính lỗ P1(a)); (c) thêm FK chéo-schema (loại: phá QR-DV-003 schema-per-module ownership). 
- Consequences: thêm 1 round-trip DB (EXISTS) mỗi CreateRoom — chấp nhận (thao tác admin tần suất thấp). TOCTOU resort-bị-xoá về lý thuyết tồn tại nhưng Resort seed-once không có thao tác xoá → không hiện thực (ghi rõ ở docstring port). Error `resort_not_found` HIỆN CHƯA được `ErrorCodeSnapshotTests` gác (snapshot chỉ quét Bedrock.Domain+Identity.Domain, không quét Rooms.Application) — gap sẵn có của catalog Rooms, ghi nhận (QR-N sau nếu mở rộng snapshot sang module Application).
- Reversibility: Easy (gỡ check + port). Traceability: review P1(a); QR-DV-003 (no cross-schema FK), QR-DV-004 (caller phân giải ResortId), F30 (Contracts seam), CP4 (module boundary).


---

### QR-AD-017 — Bất biến default-language "đúng-một + enabled" do MIỀN giữ + primitive set-default nguyên tử (P1(b))
- Status: Done
- Date: 2026-07-13
- Decider: AI (máy toann, không Docker — verify bằng unit test thuần).
- Provenance/Evidence: đọc `ResortLanguage.cs` (IsDefault bool), `ResortConfigDbContext.OnModelCreating` (partial-unique `ux_lang_default` CHỈ trong `if Database.IsNpgsql()`), `ResortConfigSeeder.cs` (mảng `Languages` set cứng en=true), `TranslationResolver.MatchSupported` (nhận `defaultCode`). Phát hiện: (1) index chỉ chặn ">1 default" (KHÔNG chặn "0 default") và Npgsql-only; (2) resort 0-default → i18n mất defaultCode; (3) chưa có thao tác đổi-default; drift dữ liệu seed (sửa mảng thành 0/2 default) KHÔNG ai bắt. Đã làm: policy MIỀN thuần `ResortLanguagePolicy` (`HasExactlyOneDefault`, `SatisfiesInvariant` = đúng-một + default-enabled, `TrySetDefault` nguyên tử in-memory: target phải tồn tại+enabled → set target + gỡ mọi default khác trong CÙNG lượt) + seeder FAIL-FAST (template phải đúng-một default, không thì ném). Guard: `ResortLanguagePolicyTests` (10, không Docker): 0/1/2-default; default-disabled; switch atomic giữ đúng-một; case-insensitive; idempotent; unknown/disabled target → false + bất biến không đổi. 10/10 pass.
- Decision/Change: bất biến "đúng MỘT default + default enabled" do MIỀN enforce (không chỉ dựa DB index). Policy tách hàm THUẦN để (a) use case set-default tương lai áp nguyên tử rồi lưu một lần, (b) seeder fail-fast, (c) unit-test đầy đủ không cần DB. Seeder ném `InvalidOperationException` nếu template không đúng-một-default (biến lỗi cấu hình im lặng → boot fail loud).
- Rationale (verifiable): **Root cause:** bất biến nghiệp vụ bị giao PHẦN cho một DB index yếu (at-most-one, Npgsql-only) → khuyết "0-default" + không portable + không có thao tác chuyển nguyên tử. Fix bản chất = miền sở hữu bất biến qua hàm thuần kiểm chứng được; seeder (đường ghi DUY NHẤT hiện tại đụng IsDefault) fail-fast trên bất biến đó. Mirror pattern platform (policy thuần như RabbitMqDeliveryPolicy/OutboxErrorFormatter — tách quyết định rủi ro thành hàm test được không cần hạ tầng).
- Alternatives: (a) chỉ thêm index cho mọi provider (loại: SQLite không có partial-unique + vẫn khuyết "0-default" + không có thao tác nguyên tử); (b) biến ResortLanguage thành aggregate-root đầy đủ với collection navigation (loại: refactor lớn ORM mapping + chưa có nhu cầu API — over-engineer; policy thuần đạt bất biến tương đương, chi phí thấp); (c) đặt policy ở Application (loại: đây là bất biến MIỀN trên entity miền → thuộc Domain).
- Consequences: seeder giờ fail-fast (an toàn hơn). Policy sẵn sàng cho use case `SetDefaultLanguage` khi có admin API slice (B.3). **Sắc thái CHƯA xử lý (defer có chủ đích — QR-N-018):** khi thao tác set-default thật được viết, trên Npgsql thứ tự UPDATE của EF có thể tạo trạng thái 2-default TẠM THỜI trong transaction → vi phạm `ux_lang_default` (index non-deferrable, kiểm ngay). Cần Docker để kiểm & xử lý (unset trước/set sau, hoặc raw SQL một câu, hoặc DEFERRABLE constraint). Máy này không Docker → KHÔNG tự nhận đã fix; ghi để người kế tiếp xử lý có kiểm chứng.
- Reversibility: Easy (policy thuần + guard seeder). Traceability: review P1(b); CP14 (một-default), QR-DV-002 (i18n), F30; QR-N-018 (defer Npgsql ordering).


---

### QR-AD-018 — Error-code snapshot phủ TẤT CẢ error catalog module (Rooms) + thu cả static property (P1-11)
- Status: Done
- Date: 2026-07-13
- Decider: AI (máy toann, verify không cần Docker).
- Provenance/Evidence: đọc `ErrorCodeSnapshotTests` (starhill) — chỉ quét `Bedrock.Domain` + `Identity.Domain`, và collector chỉ thu static FIELD + static METHOD trả `Error`. `RoomsErrors` (Rooms.Application) dùng static get-only PROPERTY → LỌT LƯỚI cả 2 chiều (không ở assembly quét + không phải field/method). Hệ quả: 3 mã client-facing `qr_generation_failed`, `resort_not_found`, `invalid_configuration` KHÔNG được gác → có thể đổi/xoá âm thầm (breaking client, phá F20/CP12). Đã sửa: (1) collector thu thêm static get-only property trả `Error` (+ bỏ getter `get_X` qua `IsSpecialName` để không đếm trùng); (2) thêm assembly `Rooms.Application` (marker `typeof(RoomsErrors)`) vào catalog quét; (3) cập nhật snapshot ExpectedCodes (+invalid_configuration/qr_generation_failed/resort_not_found). Test `Error_code_registry_matches_snapshot` PASS (không mã lạ phát sinh). Mirror cải tiến collector (property-aware) sang platform `ErrorCodeSnapshotTests` cho nhất quán (base không có catalog property → snapshot base KHÔNG đổi, verify pass) — chống lỗ tiềm ẩn cùng loại ở base.
- Context: re-audit P1-11 "event/error snapshot chỉ quét Bedrock + Identity" — với module QR (Rooms/ResortConfig) có catalog riêng, mã lỗi mới không được drift-guard.
- Decision/Change: error-code snapshot phải phủ error catalog của MỌI module (thêm ProjectReference module có catalog + collector robust cả 3 kiểu field/property/method). Comment trong test nhắc: module mới có catalog → thêm ProjectReference tương ứng.
- Rationale (verifiable): **Root cause KÉP:** (a) collector bỏ sót kiểu authoring "static property" (RoomsErrors) trong khi Identity dùng field → cùng khái niệm khác kiểu, guard chỉ bắt một kiểu; (b) phạm vi assembly quét hardcode thiếu module. Fix bản chất = collector robust mọi kiểu static-Error + phủ assembly catalog module. Không đổi RoomsErrors sang field (không ép code sản phẩm theo test — collector mới là chỗ đúng để sửa).
- Alternatives: (a) đổi RoomsErrors dùng field cho khớp collector cũ (loại: ép style sản phẩm theo hạn chế test — vá ngọn); (b) tự-động discover mọi assembly module lúc runtime (loại: assembly load lười, fragile; explicit ProjectReference rõ ràng + ít module). 
- Consequences: thêm ProjectReference Rooms.Application vào Bedrock.ContractTests. Module mới có error catalog PHẢI thêm ProjectReference (ghi rõ trong test comment — nếu quên, catalog mới không được gác; đây là giới hạn của explicit-reference, chấp nhận cho product ít module). ResortConfig hiện KHÔNG có catalog riêng (dùng CommonErrors/base) nên không cần thêm.
- Reversibility: Easy. Traceability: re-audit P1-11 (error snapshot coverage); CP12/F20/R29.1 (error code contract); liên quan QR-AD-016 (RoomsErrors.ResortNotFound thêm mã mới — nay được gác).

### QR-AD-019 — Rooms.Api slice (B-Rooms.3): admin CRUD + qr.png + rotate; ánh xạ role Req 7.6; response KHÔNG token thô; resortId phân giải ở Api
- Status: Done
- Date: 2026-07-13
- Decider: user (chốt 4 Open Question qua userInput — "Theo toàn bộ khuyến nghị") + AI (thiết kế slice).
- Provenance/Evidence: (1) đọc `docs/resort-qr-portal/requirements.md:130` (Req 7.6): "tạo/sửa/xoá phòng, sinh/thu hồi QR SHALL chỉ dành cho Admin; Staff chỉ được xem". (2) đọc `platform/src/Bedrock.Api/Authentication/BedrockAuthExtensions.cs`: base gọi `AddAuthorization()` + `RoleClaimType="role"` + `MapInboundClaims=false`, cố ý KHÔNG khai Admin/Staff. (3) đọc `IdentityEndpointModule.cs`: pattern `IEndpointModule`+`MapVersionedGroup("/x",V1)`+`ProblemDetailsBuilder.Build(err,CorrelationContext.Resolve(http))`. (4) đọc `RoomContracts.cs`/6 use case (Create=`IUseCase<,>`, Update/ChangeStatus=`ICommandUseCase<TInput>`, Delete=`ICommandUseCase<Guid>`, Rotate/RenderQr=`IUseCase<,>`). (5) đọc `IResortSettingsQuery.cs` (snapshot mang `Guid ResortId`). Sau khi làm: `starhill\scripts\vp.cmd all` = build 0-warning + validate-ci OK + test **StarHill.Api.Tests 15/15** (3 smoke + 12 auth), toàn suite 0-fail (skip = Docker-only).
- Context: module Rooms đã có nửa-Infra + use case; thiếu nửa-Api (seam HTTP admin). Slice này lộ endpoint mà KHÔNG viết nghiệp vụ mới (Api = cơ chế HTTP, F14).
- Decision/Change: (1) tạo project `Rooms.Api` (`RoomsEndpointModule : IEndpointModule` + `AddRoomsApi` đăng ký singleton IEndpointModule); (2) 6 endpoint versioned `/v1/rooms`: POST `/` (create), PUT `/{id}`, PATCH `/{id}/status`, DELETE `/{id}`, POST `/{id}/rotate-token` = **RequireAdmin**; GET `/{id}/qr.png` = **RequireStaff** (Q1 — render token đã tồn tại là "xem", Staff được; cũng cho guard CP8 kiểm nửa "Admin qua endpoint Staff = OK"). (3) DTO Api tách khỏi command Application (client KHÔNG set ResortId — chống mass-assignment). (4) Response Create/Rotate chỉ trả `{roomId, tokenPreview}` — KHÔNG token thô (Q3/D-B/CP1: token thô chỉ tới client qua ảnh qr.png). (5) resortId phân giải ở Api qua `IResortSettingsQuery.GetAsync().ResortId` (single-resort — QR-DV-004); null → `InvalidConfiguration`. (6) wire Host `AddRoomsApi()` + ProjectReference; thêm project vào `Platform.slnx`.
- Rationale (verifiable): ánh xạ role bám ĐÚNG Req 7.6 (mutation+QR=Admin; xem=Staff). Không trả token thô = giảm bề mặt rò rỉ (log/history/proxy) — token là bí mật nhúng QR (CP1). resortId ở Api giữ `Rooms.Application ⊥ ResortConfig` (boundary `RoomsBoundaryTests`), đúng QR-DV-004 đã chốt. DTO tách = đúng mẫu `IdentityEndpointModule.RefreshRequest`.
- Alternatives: (a) qr.png=RequireAdmin (user chọn RequireStaff); (b) trả token thô cho FE render client-side (user chọn KHÔNG — dùng qr.png); (c) GET list/detail trong slice này (defer — chưa có read-model, đúng `RoomContracts.cs` NOTE).
- Consequences: mọi module admin QR sau (GuestAccess/Rules/Faq/Housekeeping/Concierge) mirror khuôn `RoomsEndpointModule` + dùng `StarHillPolicies`. GET list/detail = slice query sau.
- Reversibility: Medium (project mới + wire Host). Traceability: `design-B-Rooms.3-rooms-api.md`; Req 7.6/7.4/7.1; QR-AD-005/QR-AD-020/QR-DV-001/QR-DV-004; base F14/F32/N-041.

### QR-AD-020 — Project dùng chung `StarHill.Authorization`: policy Admin/Staff superset (nền QR-AD-005)
- Status: Done
- Date: 2026-07-13
- Decider: AI (spec không nói nơi đặt policy; nhiều module admin cần cùng 2 policy).
- Provenance/Evidence: (1) grep `starhill/src/**/*.cs` cho `RequireRole|AddPolicy|RequireAuthorization|"admin"|"staff"` = **0 match** → chưa có policy nào; Rooms.Api là nơi đầu tiên. (2) base `BedrockAuthExtensions` comment: "Role/permission cụ thể do module Identity/Host khai — base KHÔNG khai Admin/Staff". (3) `RoleClaimType="role"` → `RequireRole` kiểm claim `role`. Sau khi làm: guard `StarHillAuthorizationPolicyTests` 6/6 (superset) + `RoomsEndpointAuthTests` 6/6.
- Decision/Change: tạo class-lib `starhill/src/StarHill.Authorization` gồm `StarHillPolicies` (hằng `RequireAdmin`/`RequireStaff`/`RoleAdmin="admin"`/`RoleStaff="staff"`) + `AddStarHillAuthorization()` đăng ký: `RequireAdmin=RequireRole("admin")`, `RequireStaff=RequireRole("staff","admin")` (superset). Host gọi 1 lần sau `AddBedrockApi`; mọi module Api tham chiếu hằng.
- Rationale (verifiable): DRY + chống drift — ngữ nghĩa superset định nghĩa MỘT chỗ (module tự viết `RequireRole("staff","admin")` inline dễ quên `admin` → phá superset). Có nhiều consumer thật (mọi module admin QR) → seam hợp lệ, không gold-plate (I10). Không đặt ở base Identity (domain-agnostic — nhồi role sản phẩm làm bẩn base, ngược QR-AD-012). `AddBedrockAuthCore` đã `AddAuthorization()` → thêm named policy additive.
- Alternatives: (a) policy inline mỗi endpoint (loại: drift superset); (b) đặt ở base Identity (loại: bẩn base tái dùng); (c) đặt ở Host (loại: module Api không ref được Host → không có hằng tên policy).
- Consequences: mọi module Api admin ref `StarHill.Authorization`. Đổi mô hình role (thêm vai) = sửa một chỗ.
- Reversibility: Easy. Traceability: QR-AD-005 (nền, nay ✅); QR-AD-019; Req 11.3/7.6.

### QR-AD-021 — Chính sách JSON HTTP sản phẩm: enum (de)serialize dạng STRING (JsonStringEnumConverter toàn cục)
- Status: Done
- Date: 2026-07-13
- Decider: AI (phát hiện khi guard PATCH status FAIL 400; spec không nói định dạng enum wire).
- Provenance/Evidence: (1) guard `RoomsEndpointAuthTests.Admin_mutations_succeed` FAIL: PATCH `/v1/rooms/{id}/status` body `{"status":"Inactive"}` → **400 BadRequest** (System.Text.Json mặc định KHÔNG bind string→enum). (2) grep `platform/starhill/src/**/*.cs` `JsonStringEnumConverter|ConfigureHttpJsonOptions` = 0 (chỉ `OutboxSerialization` cho messaging, không phải HTTP). Sau khi thêm: PATCH 204, toàn bộ guard 12/12 + `vp all` xanh.
- Context: `ChangeRoomStatusRequest(RoomStatus Status)` — enum. Client gửi string "Inactive" (API đọc được). Mặc định STJ chỉ nhận số nguyên → brittle (phụ thuộc thứ tự khai enum) + khó đọc.
- Decision/Change: Host `Program.cs` gọi `services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()))` — áp TOÀN CỤC cho minimal API (mọi module). Test host mirror cùng cấu hình.
- Rationale (verifiable): string-enum = API ổn định (không phụ thuộc giá trị số theo thứ tự khai) + đọc được + chuẩn công nghiệp. Áp toàn cục để nhất quán mọi module (không rải per-endpoint). Base KHÔNG áp (giữ domain-agnostic — style là quyết định sản phẩm ở Host). Blast radius hiện chỉ `RoomStatus` (module khác chưa lộ enum qua HTTP).
- Alternatives: (a) client gửi số nguyên (loại: brittle/khó đọc); (b) DTO nhận string + Enum.TryParse trong handler (loại: mất type-safety, trùng lặp parse mỗi enum); (c) áp ở base AddBedrockApi (loại: ép style lên mọi consumer base — giữ base trung lập).
- Consequences: mọi enum trong request/response HTTP sản phẩm dùng string. Thêm enum mới tự hưởng, không cấu hình thêm.
- Reversibility: Easy (một dòng Host). Traceability: QR-AD-019; Req 7.6 (status endpoint); base Bedrock.Api (không áp).


### QR-AD-022 — Rooms query slice (B-Rooms.4): read-model list/detail nội-module ở Application + phân trang chuẩn + role Staff
- Status: Done
- Date: 2026-07-13
- Decider: AI (slice contained, read-only, mirror pattern read đã có; spec không nói chi tiết list/detail).
- Provenance/Evidence: (1) Req 7.6 "Staff chỉ được **xem** danh sách/thông tin phòng" — B-Rooms.3 mới có qr.png, THIẾU list/detail → Staff chưa xem được phòng. (2) pattern read của sản phẩm = interface + EF impl inject `RoomsDbContext` + scoped (đọc `EfRoomTokenResolver`/`EfResortSettingsQuery`), KHÔNG qua use case pipeline. (3) base có sẵn `PagedRequest`/`PagedResult<T>` (`Bedrock.Application/UseCases/Paging.cs`, cap 100) → tái dùng. (4) `Room:AuditableEntity` có `CreatedAt`; soft-delete có global query filter (verify EfRoomTokenResolver). Sau khi làm: `vp all` = build 0-warning + `RoomQueriesTests` (3, SQLite) + `RoomsEndpointAuthTests` +4 → StarHill.Api.Tests 19/19, Rooms.IntegrationTests 25 pass/2 skip(Docker), toàn suite 0-fail.
- Decision/Change: (1) `Rooms.Application/IRoomQueries.cs`: DTO `RoomListItem` (RoomId/RoomNumber/Building/Floor/Status/ActiveTokenPreview/ActiveTokenVersion/CreatedAt) + port `IRoomQueries.ListAsync(status?,PagedRequest)/GetByIdAsync(id)` — đặt ở Application (read NỘI-MODULE, không Contracts). (2) `Rooms.Infrastructure/Persistence/EfRoomQueries.cs`: 2 truy vấn đơn giản (page rooms → load token Active theo `ids.Contains` → map in-memory) tránh correlated-subquery lồng; đăng ký scoped. (3) `RoomsEndpointModule`: GET `/v1/rooms` (list, query `status/page/pageSize`) + GET `/v1/rooms/{id}` (detail, 404 nếu vắng) — cả hai **RequireStaff**. (4) response trả thẳng `PagedResult<RoomListItem>` (đã là DTO đọc; không token thô — CP1).
- Rationale (verifiable): read pattern nhất quán precedent (không qua pipeline — read không cần transaction/validation). Phân trang chuẩn NGAY từ đầu = tránh breaking change response shape sau này (array→object) — table-stakes API thương mại, không gold-plate. 2-query + map dictionary an toàn dịch trên SQLite lẫn Postgres (ux_qr_active ⇒ ≤1 active/phòng). Role Staff đúng Req 7.6, Admin superset qua `RequireStaff`.
- Alternatives: (a) query use case `IQueryUseCase` (loại: thừa pipeline cho read thuần; precedent dùng query interface trực tiếp); (b) correlated subquery lồng trong Select (loại: rủi ro dịch LINQ đa-provider); (c) không phân trang (loại: breaking change tương lai + rủi ro response không giới hạn); (d) đặt read-model ở Contracts (loại: không cross-module → giữ Contracts sạch).
- Consequences: các module khác về sau có admin-list mirror khuôn `IRoomQueries`/`EfRoomQueries`. Token history/detail sâu hơn = slice sau (chưa cần).
- Reversibility: Easy (read-only, thêm mới). Traceability: `design-B-Rooms.4-rooms-queries.md`; Req 7.6/11.6; QR-AD-019/020/021; base Paging.cs; `EfRoomTokenResolver` (mẫu read).


### QR-AD-023 — ResortConfig write-path đầu tiên (settings Api B-Config.3): admin xem/sửa cấu hình, role Admin, tái dùng CommonErrors
- Status: Done
- Date: 2026-07-13
- Decider: AI (slice contained; mirror pattern Rooms; spec Req 9.3/15.6 định hướng role + validation).
- Provenance/Evidence: (1) `RenderRoomQrPng` (qr.png B-Rooms.3) trả `invalid_configuration` khi thiếu `GuestWebBaseUrl` mà CHƯA có endpoint cấu hình → mắt xích thiếu. (2) ResortConfig trước đó chỉ read (query/seeder inject `ResortConfigDbContext` ở Infra) — CHƯA có write-path/use case nào. (3) `AddResortConfigInfrastructure` đã `AddBedrockPersistence<ResortConfigDbContext>(key)` (⇒ keyed IUnitOfWork) nhưng CHƯA `AddBedrockRepository<...,ResortSettings>`. (4) `CommonErrors.NotFoundGeneric()` code `not_found` ∈ snapshot Bedrock.Domain → tái dùng, KHÔNG cần catalog mới (không đụng ErrorCodeSnapshotTests). (5) Req 9.3 "Admin quản lý settings"; Req 15.6 GuestWebBaseUrl https. Sau khi làm: `vp all` = build 0-warning + StarHill.Api.Tests 22/22 (+3 ResortConfigEndpointAuthTests) + ResortConfig.IntegrationTests 9 pass/3 skip(Docker) (+2 use-case +5 validator), toàn suite 0-fail.
- Decision/Change: (1) `ResortConfig.Application/UpdateResortSettings.cs`: `UpdateResortSettingsInput` (mọi field vận hành, KHÔNG ResortId) + `UpdateResortSettingsUseCase : ICommandUseCase<UpdateResortSettingsInput>` (keyed IRepository<ResortSettings>+IUnitOfWork, load single settings, mutate, Save; null→CommonErrors.NotFoundGeneric) + `UpdateResortSettingsValidator` (GuestWebBaseUrl null|absolute-https; ranges). (2) `ResortConfig.Infrastructure`: +`AddBedrockRepository<ResortConfigDbContext,ResortSettings>(key)` + factory use case + validator. (3) `ResortConfig.Api` (project MỚI): `ResortConfigEndpointModule` GET `/v1/resort/settings` (IResortSettingsQuery) + PUT (use case), CẢ HAI **RequireAdmin** + `AddResortConfigApi`. (4) wire Host + Platform.slnx.
- Rationale (verifiable): write-path mirror Rooms (Option A: keyed repo + pipeline) → giữ Application ⊥ Infrastructure (KHÔNG inject DbContext ở Application như query/seeder Infra). Role Admin cả GET+PUT: settings là cấu hình admin (Req 9.3), Staff không có nhu cầu vận hành đọc raw config (precise; CP8 superset đã gác ở Rooms). Tái dùng CommonErrors tránh phình error catalog + snapshot. Validator https-or-null cho phép lưu trước khi có URL (qr.png vẫn báo tới khi set).
- Alternatives: (a) inject DbContext trực tiếp trong Application command (loại: phá Application ⊥ Infra — DbContext chỉ ở Infra); (b) tạo ResortConfigErrors catalog cho settings-not-found (loại: phình snapshot; CommonErrors đủ); (c) GET=Staff (loại: settings không phải nhu cầu vận hành Staff — Req 9.3 Admin); (d) bắt concurrency tường minh (defer — rủi ro thấp, middleware base map).
- Consequences: module admin khác (Faq/Rules...) sẽ mirror khuôn write-path này (keyed repo + use case + validator + Api RequireAdmin). ResortSettings giờ có repository ghi.
- Reversibility: Easy (thêm mới). Traceability: `design-B-Config.3-settings-api.md`; Req 9.3/15.6/14; QR-AD-019/020/021/022; base CommonErrors/Paged*; Rooms (mẫu write-path).


### QR-AD-024 — GuestAccess dùng bounded context riêng, purpose-built guest config query và fail-closed khi config thiếu
- Status: Accepted/Implemented (C-GA.1 + C-GA.2a; verify QR-N-024/025)
- Date: 2026-07-14
- Decider: AI (chi tiết port GuestAccess không được spec Bedrock hóa sẵn).
- Provenance/Evidence: product tree `starhill/src/Modules` hiện chỉ có Identity/ResortConfig/Rooms; legacy GuestAccess ở `resort-qr/` commit `c7622a5`. `IRoomTokenResolver` trả RoomResolution/null; `IResortSettingsQuery` thiếu resort presentation/languages và là global single-resort. QR-AD-002 cấm DbContext/FK chéo schema.
- Decision/Change: tạo module 5-project/schema/key `guest_access`; GuestSession/GuestVisit thuộc module này; Rooms/ResortConfig chỉ được đọc qua Contracts. Thêm `IResortGuestConfigQuery.GetAsync(Guid resortId)` purpose-built trả resort name/logo, enabled languages/default, flags, portal/idle; null/mismatch fail `configuration_unavailable` trước khi ghi session/visit. Không silently bật feature khi config nền mất.
- Rationale (verifiable): resolve cần một snapshot guest-facing cohesive mà settings query hiện tại không có; query theo Room.ResortId tránh implicit global match và mở đường multi-resort. Fail-closed tránh phát hành portal context từ dữ liệu cấu hình không toàn vẹn.
- Alternatives: mở rộng `IResortSettingsQuery` (loại: phình DTO đang phục vụ Rooms); đọc ResortConfigDbContext từ GuestAccess (loại: phá boundary); trả defaults true (loại: mở feature khi config lỗi).
- Consequences: ResortConfig thêm Contracts query + Infra implementation trong slice C-GA.2; response Rules được defer tới khi Rules tồn tại.
- Reversibility: Medium. Traceability: `design-modules/03-guestaccess.md` §2/§5; Req 1/10/14.2.
- Guard-Tests: `GuestAccessBoundaryTests`, `GuestAccessPostgresConstraintTests`, `ResortGuestConfigQueryTests`, `ResolveTokenUseCaseTests`

### QR-AD-025 — Resolve là POST-body public command; cookie `__Host-`, no-store và không log capability
- Status: Accepted/Implemented (C-GA.3; C-GA.3a bổ sung input/log guards)
- Date: 2026-07-14
- Decider: AI (hardening endpoint/cookie public).
- Provenance/Evidence: legacy `GET /api/guest/resolve/{token}` vừa tạo/touch session+visit vừa đặt cookie; token path có thể vào access log/proxy/APM. Legacy cookie `shq_guest`, 60 ngày, HttpOnly/Secure/SameSite=Lax. Req 11.6 cấm log full token; deploy bắt HTTPS/cùng-origin.
- Decision/Change: API thật `POST /v1/guest/resolve` body `{token}`, AllowAnonymous, `Cache-Control:no-store`; không GET alias. Cookie mặc định `__Host-starhill_guest`, 60 ngày, HttpOnly+Secure+SameSite=Lax+Path=/, không Domain, ValidateOnStart 30–90 ngày. Token QR và session key phải canonical 43-char base64url trước resolver/hash; malformed cookie coi như session mới; endpoint body limit 1 KiB. Raw session key chỉ set-cookie sau commit; token/key không vào response/log/trace. Bedrock global IP limiter là baseline; named resolve policy defer theo QR-TO-007.
- Rationale (verifiable): operation mutate nên POST đúng HTTP semantics; body không nằm trong request-target/access-log mặc định. `__Host-` chống subdomain overwrite bằng browser-enforced constraints. QR vật lý vẫn `/r/{token}` nên API đổi không làm hỏng QR; Guest Web phải scrub browser URL và proxy redact `/r/*`.
- Alternatives: giữ GET path + log middleware redaction (loại: secret vẫn đi qua nhiều tầng trước middleware); cookie thường (bỏ: yếu hơn không có lợi ích).
- Consequences: frontend phải POST và scrub URL; ghi deviation QR-DV-006; HTTP/log guard bắt buộc.
- Reversibility: Medium. Traceability: `design-modules/03-guestaccess.md` §4/§7; QR-DV-001/006; Req 11.2/11.5/11.6.
- Guard-Tests: `GuestAccessResolveEndpointTests`, `GuestAccessResolveLogRedactionTests`, `ResolveTokenUseCaseTests`

### QR-AD-026 — Serialize resolve bằng GuestSession row lock; không catch-query sau PostgreSQL 23505
- Status: Accepted/Implemented (C-GA.2b; verify QR-N-026)
- Date: 2026-07-14
- Decider: AI (sửa root race semantics của legacy).
- Provenance/Evidence: legacy `ResolveTokenUseCase` insert visit rồi catch `UniqueConstraintViolationException` và query lại cùng UoW. Base `TransactionUseCaseDecorator` bọc `ITransactionalUseCase`; `EfUnitOfWork` mở explicit transaction và rollback khi action ném. PostgreSQL unique violation abort transaction đến rollback; DbContext còn entity Added. Vì vậy query trong catch trên transaction/context đó không đáng tin.
- Decision/Change: resolve là `IUseCase<,>` thường, resolve keyed GuestAccess UoW và tự mở transaction hẹp **sau** Rooms/Config read. Session tồn tại được đọc `FOR UPDATE` trước khi đọc/tạo visit. Dưới lock: active-hợp-lệ touch; active-expired update+Save trước rồi mới insert mới; absent insert. Partial unique vẫn defense cuối nhưng không dùng như control flow; nếu nổ thì rollback, không query trên context hỏng.
- Rationale (verifiable): khóa hàng loại TOCTOU trước constraint, giữ Application provider-agnostic qua store port và cho mọi request cùng cookie hội tụ VisitId. Save expire trước insert loại phụ thuộc command-order EF với partial unique.
- Alternatives: catch rồi query (loại: aborted transaction); pre-check không lock (loại: race); serializable/retry toàn use case (khả thi nhưng base không expose isolation/fresh-scope retry; rộng hơn cần thiết); advisory lock (không cần vì session row có thật).
- Consequences: Infra có custom store `FOR UPDATE`; resolve cùng device serialize; PostgreSQL concurrency test là gate bắt buộc.
- Reversibility: Medium. Traceability: `design-modules/03-guestaccess.md` §6; CP9; base EfUnitOfWork/TransactionUseCaseDecorator.
- Guard-Tests: `GuestAccessResolveRaceTests`, `ResolveTokenUseCaseTests`

### QR-AD-027 — Cascade GuestVisitEnded dùng outbox/inbox at-least-once; supersede cascade đồng bộ của QR-AD-002
- Status: Proposed (design; triển khai C-GA.5 khi consumer tồn tại)
- Date: 2026-07-14
- Decider: AI (sửa giả định atomicity sai trong design cũ).
- Provenance/Evidence: QR-AD-002/QR-TO-002 nói gọi 3 module trong “một transaction/scope”, nhưng mỗi module có DbContext/key riêng; cùng DI scope không tạo shared transaction. Base có outbox/inbox keyed và startup guard, RabbitMQ semantics at-least-once. Req 10.8 span GuestAccess/Concierge/Housekeeping.
- Decision/Change: local GuestAccess transaction kết thúc visit + ghi `GuestVisitEndedIntegrationEvent` outbox; Concierge/Housekeeping consume bằng inbox/idempotent handler để close/cancel. Guest write bị chặn ngay bởi trạng thái visit; cleanup chéo module eventual. Không map outbox ở initial resolve, chỉ thêm cùng consumer thật.
- Rationale (verifiable): đảm bảo local atomicity + durable retry mà không giả vờ distributed transaction; handler idempotent chịu duplicate delivery. Đây là at-least-once, không exactly-once.
- Alternatives: sequential sync calls (loại: partial failure không recover); TransactionScope nhiều connection (loại: coupling/operational complexity); dual sync+event (loại: hai nguồn side effect).
- Consequences: cleanup có độ trễ ngắn khi broker/downstream chậm; outbox giữ sự kiện khi broker lỗi. Supersedes **chỉ phần cascade** của QR-AD-002; phân rã/boundary còn hiệu lực.
- Reversibility: Medium. Traceability: `design-modules/03-guestaccess.md` §9; QR-TO-006; Req 10.8.
### QR-AD-028 — EF migration history thuộc schema module, runtime và design-time phải đồng nhất
- Status: Accepted/Implemented (C-GA.3a; QR-N-028 verify Compose + upgrade path)
- Date: 2026-07-14
- Decider: AI (phát hiện drift bằng Compose PostgreSQL thật; spec nói schema-per-module nhưng chưa chốt history ledger).
- Provenance/Evidence: query catalog DB `starhill` sau compose boot cho thấy bốn schema nghiệp vụ nhưng chỉ `public.__EFMigrationsHistory`, chứa 5 MigrationId của Identity/ResortConfig/Rooms/GuestAccess. Mỗi module có DbContext/migration bundle riêng.
- Decision/Change: cấu hình `MigrationsHistoryTable("__EFMigrationsHistory", <module_schema>)` ở mọi Npgsql call site runtime, integration và design-time factory. Thêm transition SQL idempotent copy đúng MigrationId từ public ledger sang từng schema trước binary mới; không drop public ledger trong rollout.
- Rationale (verifiable): history ledger là deployment state của migration chain; để chung tạo shared mutable state, collision namespace và coupling bundle trái ownership schema. Runtime/factory lệch sẽ khiến bundle và Host đánh giá applied migration khác nhau.
- Alternatives: giữ public chung (bỏ: đang chạy nhưng coupling ngầm); đổi chỉ Host (loại: bundle/test lệch); drop ledger public rồi migrate lại (loại: phá upgrade/rollback).
- Consequences: upgrade DB cũ cần transition step bắt buộc; fresh DB tự tạo bốn ledger. Test phải chứng minh migration rows đúng schema và không có pending model drift.
- Reversibility: Medium. Traceability: `design-modules/03-guestaccess.md` §10/§11; QR-TO-008.
- Guard-Tests: `ModuleMigrationHistorySchemaTests`

### QR-AD-029 — Traceability gate INV-6: quyết định "Implemented" phải khai Guard-Tests tồn tại thật trong source
- Status: Accepted/Implemented (2026-07-14; guard tự-gác trong StarHillJournalConsistencyTests)
- Date: 2026-07-14
- Decider: AI (đóng lỗ hổng anti-drift phát hiện ở phiên C-GA.3a review).
- Provenance/Evidence: rà soát C-GA.3a phát hiện drift design↔code (design ghi `ITransactionalUseCase` trong khi code là `IUseCase`; response phẳng vs nested; "no-secret-log by design" không có test) mà cổng INV-1..5 KHÔNG bắt được — vì INV-1..5 chỉ kiểm nội bộ journal (ID liên tục, AD xuất hiện trong anti-drift map, không dangling, có Status/Provenance, CP 1..15). INV-2 keystone chỉ khớp CHUỖI "AD xuất hiện", không chứng minh guard test tồn tại. Đọc `StarHillJournalConsistencyTests.cs` xác nhận không có kiểm tra guard-existence.
- Decision/Change: thêm INV-6 vào `StarHillJournalConsistencyTests`. Mọi QR-AD có dòng `- Status:` chứa "Implemented" PHẢI có dòng `- Guard-Tests:` liệt kê ≥1 test class; và MỌI tên test class được liệt kê (ở bất kỳ QR-AD nào) PHẢI tồn tại thật dưới dạng `class <Name>` trong `starhill/tests/**/*.cs` (quét source, đi lên tổ tiên như RequireJournalDir). Lệch = FAIL BUILD.
- Rationale (verifiable): biến "quyết định đã xong" thành ràng buộc code-enforced tới guard test có thật — nếu ai xóa/đổi tên guard test hoặc tuyên bố Implemented mà không có test, build đỏ. Đây là traceability matrix cưỡng chế bằng build, đóng đúng lớp drift design↔code mà INV-1..5 bỏ sót. Dùng field cấu trúc `Guard-Tests:` + quét source (KHÔNG reflection cross-assembly mong manh, KHÔNG parse prose nhập nhằng).
- Alternatives: reflection nạp mọi test assembly rồi match tên (loại: fragile, cần ref chéo test project + phân biệt tên test/không-test trong prose); attribute `[Traces("QR-AD-###")]` trên test (mạnh nhưng tốn annotate rộng + reflection cross-assembly — để dành nếu cần bidirectional chặt hơn); giữ nguyên INV-1..5 (loại: đã chứng minh bỏ sót drift thật).
- Consequences: mỗi AD chuyển sang Implemented phải kèm Guard-Tests hợp lệ; tên test đổi phải cập nhật journal → journal luôn trỏ guard sống. Chi phí duy trì nhỏ, thẳng vào bản chất chống drift.
- Reversibility: High (chỉ là test governance). Traceability: `journal/05-anti-drift.md` INV-6; QR-TO-009; QR-N-029.
- Guard-Tests: `StarHillJournalConsistencyTests`

### QR-AD-030 — Rules: snapshot-on-publish + acknowledge server-authoritative + rule-gate backend
- Status: Implemented (D-Rules.3a Publish snapshot + 4a guest read + 4b acknowledge + 4c-gate IRuleGate + 4c-api admin/guest endpoints — QR-N-036/038/039/040/041/042)
- Guard-Tests: `PublishRulesUseCaseTests`, `AcknowledgeRulesTests`, `RuleGateTests`, `RulesAdminEndpointAuthTests`, `RulesGuestEndpointTests`, `RulesPostgresConstraintTests`
- Date: 2026-07-14
- Decider: AI (module Rules dựng mới — legacy resort-qr chưa có; chi tiết on-Bedrock không được spec sẵn).
- Provenance/Evidence: `docs/resort-qr-portal/requirements.md` Req 3.7/3.8/3.9/3.11 (ack gắn GuestVisit, backend enforce 403), Req 8.1/8.3/8.4 (Draft→Publish snapshot, preview/history); `docs/resort-qr-portal/design.md` §Data Models (RuleSet/RuleSection(+Translation)/RulePublication(+Section+Translation)/RuleAcknowledgement + unique `RulePublication(ResortId) WHERE IsCurrent`, unique `RuleAcknowledgement(GuestVisitId, RulePublicationId)`), CP3/CP4/CP13. `IResortSettingsQuery.ResortSettingsSnapshot` (đã đọc) mang đủ cờ RequireRuleAckForFaq/Chat/Housekeeping.
- Decision/Change: khách CHỈ đọc `RulePublication IsCurrent` (snapshot bất biến); Publish copy đông cứng Draft→PublicationSection(+Translation) trong một transaction, flip IsCurrent (hạ cũ + SaveChanges TRƯỚC rồi insert mới — tránh 23505 partial-unique giữa transaction). Acknowledge do SERVER xác định IsCurrent (không tin version client — CP13), gắn GuestVisit, idempotent qua unique `(GuestVisitId, RulePublicationId)`. Rule-gate `IRuleGate` (Rules.Contracts) enforce ở backend: cờ tắt→pass; bật+chưa ack→`rule_ack_required` (403).
- Rationale (verifiable): snapshot đảm bảo sửa Draft không lọt ra khách (CP4); server-authoritative chặn client ack version giả/cũ (CP13); backend gate chống bypass frontend (CP3). Flip-before-insert dựa cơ chế partial-unique Postgres (mirror bài học QR-N-018).
- Alternatives: khách đọc thẳng Draft (loại: vỡ CP4); tin version client khi ack (loại: vỡ CP13); chỉ chặn gate ở frontend (loại: vỡ CP3).
- Consequences: thêm mã lỗi `rule_ack_required`/`rules_unavailable`/`session_expired` → cập nhật ErrorCodeSnapshot (QR-AD-018) khi code; Faq/Concierge/Housekeeping ref `Rules.Contracts` gọi gate.
- Reversibility: Medium. Traceability: `design-modules/04-rules.md` §4/§5/§6; CP3/CP4/CP13; Req 3/8.

### QR-AD-031 — IHtmlSanitizer: adapter Ganss ở shared starhill project + RequirePort + sanitize-on-save
- Status: Implemented (adapter D-Rules.2a + sanitize-on-save 2b + RequirePort<IHtmlSanitizer> ở Host 4c-api-1 — QR-N-033/035/041)
- Guard-Tests: `GanssHtmlSanitizerAdapterTests`, `RuleSanitizeTests`
- Date: 2026-07-14
- Decider: AI (đóng mắt xích port bảo mật bắt buộc khi Rules là consumer đầu).
- Provenance/Evidence: `platform/src/Bedrock.Application/Ports/Html/IHtmlSanitizer.cs` — "port bảo mật bắt buộc: không có default, thiếu → chặn boot (§5.5)". Điều tra thật: KHÔNG có adapter impl trong platform/starhill; KHÔNG đăng ký DI; package Ganss/HtmlSanitizer CHƯA pin ở `starhill/Directory.Packages.props`; RequiredPorts hiện KHÔNG liệt IHtmlSanitizer (Host boot được vì chưa ai consume). Req 8.6/11.4/CP12 yêu cầu sanitize.
- Decision/Change: (a) adapter `IHtmlSanitizer` bằng `Ganss.Xss` (allowlist) đặt ở **shared project cấp starhill** (mirror precedent `StarHill.Authorization`), KHÔNG nhét base cũng KHÔNG nhét Rules.Infrastructure; (b) pin package ở `starhill/Directory.Packages.props`; (c) đăng ký DI + RequirePort(IHtmlSanitizer) để boot fail-fast. Sanitize-on-save là bất biến (cột BodyHtmlSanitized); Publish copy nội dung đã sanitize → guest read an toàn by-construction.
- Rationale (verifiable): adapter Ganss generic nhưng do sản phẩm kéo vào — đặt base làm bẩn base (D1-a giữ base sạch); đặt Rules.Infrastructure buộc Faq ref Rules (phá boundary). Shared starhill project tái dùng Rules+Faq, không phá boundary, không đụng base. RequirePort đúng ý "port bảo mật không default".
- Alternatives: adapter trong base `platform/src/Adapters` (loại tạm: đúng domain-agnostic nhưng làm base gánh nhu cầu sản phẩm — cân nhắc lại nếu nhiều sản phẩm cần); adapter trong Rules.Infrastructure (loại: Faq phải ref Rules); tự viết sanitizer (loại: không tái phát minh, rủi ro XSS).
- Consequences: thêm shared project + package + nhánh RequirePort; chốt vị trí RequirePort (Host vs extension) ở slice code.
- Reversibility: Medium. Traceability: `design-modules/04-rules.md` §7; QR-TO-011; CP12; Req 8.6/11.4.

### QR-AD-032 — GuestAccess.Contracts.ICurrentGuestContextResolver (C-GA.4) + portal-window check-before-touch
- Status: Accepted/Implemented (2026-07-15; slice C-GA.4)
- Date: 2026-07-14 (Proposed) → 2026-07-15 (Implemented)
- Decider: AI (Rules là consumer đầu của current-guest-context — kích hoạt C-GA.4 như QR-AD-024 đã hẹn).
- Provenance/Evidence: `GuestAccess.Contracts.csproj` ghi "current-guest-context DTO/port thêm khi consumer đầu tiên (Rules) tồn tại (QR-AD-024)"; `GuestAccess.Contracts` trước CHỈ có `GuestAccessModule`. `docs/resort-qr-portal/design.md` §resolve bước 5: interactive guest API phải kiểm `now - GuestVisit.LastSeenAt > PortalWindowMinutes` TRƯỚC; quá hạn → session_expired KHÔNG touch; còn hạn → xử lý rồi mới touch (nếu touch trước sẽ tự gia hạn vô hạn). Đọc thật: `GuestVisit.LastSeenAt`/`ExpiresAt`; `IResortGuestConfigQuery` mang `PortalWindowMinutes`+`VisitIdleExpiryHours`; `IUnitOfWork.ExecuteInTransactionAsync`; `Sha256GuestSessionKeyHasher` (hex thường 64). Sau code: `GuestAccess.IntegrationTests` 33/33 pass Docker thật (8 test C-GA.4).
- Decision/Change: (1) `GuestAccess.Contracts` DTO `CurrentGuestContext(GuestVisitId, GuestSessionId, RoomId, ResortId)` + port `ICurrentGuestContextResolver` — `ResolveAsync(sessionKey, roomId)` (đọc + kiểm window, KHÔNG touch) trả `Result<CurrentGuestContext>`, `TouchAsync(guestVisitId)` (gọi SAU nghiệp vụ thành công). (2) Impl `EfCurrentGuestContextResolver` (GuestAccess.Infrastructure): cookie rỗng/không phân giải → `guest_context_missing`; không còn visit Active cho phòng HOẶC quá portal-window → `session_expired`; cấu hình nền thiếu → `configuration_unavailable` (fail-closed); còn hạn → context. Dùng CHUNG `IGuestSessionKeyHasher` (nguồn hash duy nhất — không lệch thuật toán). (3) TouchAsync BẢO TOÀN idle-delta (`ExpiresAt - LastSeenAt`) khi trượt cửa sổ → KHÔNG cần query cấu hình ở đường touch; no-op nếu visit không Active. (4) 2 mã lỗi mới `session_expired`/`guest_context_missing` (Unauthorized/401) vào `GuestAccessErrors` + snapshot registry (QR-AD-018). (5) `GuestAccess.Contracts` +ref `Bedrock.Domain` (dùng `Result<T>` — nguyên thủy shared kernel; boundary test cấm Application/Infra/Api, KHÔNG cấm Domain).
- Rationale (verifiable): GuestVisit thuộc schema guest_access → chỉ GuestAccess sở hữu (QR-AD-024); tách Resolve/Touch giữ đúng check-before-touch (chống gia hạn vô hạn — đúng cảnh báo design). Portal-window (phút) KHÔNG vật hoá → phải đọc cấu hình ở Resolve (fail-closed); idle-expiry (giờ) đã vật hoá ở ExpiresAt → Touch bảo toàn delta (robust, không nhánh fail-closed thừa, không dependency cấu hình ở touch). Result<T> ở Contracts cho consumer map thẳng Error→HTTP (401). Reuse hasher = nguồn hash duy nhất, tránh drift.
- Alternatives: Rules tự đọc GuestVisit (loại: phá boundary QR-AD-024/QR-AD-002); gộp touch vào resolve (loại: vỡ check-before-touch, cửa sổ tự gia hạn); truyền entity GuestVisit qua Contracts (loại: lộ Domain — dùng DTO thuần); Touch query cấu hình để lấy idleHours (loại: dependency + nhánh null thừa ở đường touch — delta-preservation đủ và bền); inline SHA-256 trong resolver (loại: 2 nguồn hash → drift; reuse hasher).
- Consequences: GuestAccess.Contracts nay ref Bedrock.Domain; guest rules read/ack + rule-gate (D-Rules.4) tiêu thụ port này. Nếu VisitIdleExpiryHours đổi giữa 2 lần touch → delta cũ giữ tới lần resolve-token kế (chấp nhận: cấu hình hiếm đổi; resolve-token luôn set lại ExpiresAt từ cấu hình mới).
- Reversibility: Medium. Traceability: `design-modules/04-rules.md` §6.2; `design-modules/03-guestaccess.md` §8; QR-AD-024; QR-AD-025 (hash cookie); Req 10.3/10.4; QR-N-037.
- Guard-Tests: `CurrentGuestContextResolverTests`

### QR-AD-033 — OpenAPI phơi CHỈ ở Development (prod tắt); overlay compose dev explicit; không thêm UI stack
- Status: Accepted/Implemented (2026-07-15; guard OpenApiExposureTests)
- Date: 2026-07-15
- Decider: AI (mở "test API qua trình duyệt" khi user hỏi "đã test trên web được chưa" — spec không nói bật OpenAPI ở đâu).
- Provenance/Evidence: probe compose thật cho `openapi=404` (Host chưa gọi AddBedrockOpenApi). `platform/src/Bedrock.Api/OpenApi/BedrockOpenApiExtensions.cs`: OPT-IN, `AddBedrockOpenApi` → `AddOpenApi()` (native .NET 10, document "v1") + marker; `UseBedrockApi` tự map `/openapi/v1.json` khi có marker; base ghi rõ "Swagger UI là tầng trình bày do Host chọn" (DV-015 — không nhồi stack lớn). `docker-compose.yml` đặt `ASPNETCORE_ENVIRONMENT=Production`. Introduction requirements: hệ thống chạy mạng nội bộ, KHÔNG publish internet.
- Decision/Change: Host gọi `AddBedrockOpenApi()` CHỈ khi `builder.Environment.IsDevelopment()`. Thêm `docker-compose.dev.yml` (overlay EXPLICIT `-f`, đặt Development) — compose mặc định GIỮ Production → OpenAPI tắt. KHÔNG thêm Swagger/Scalar UI (giữ DV-015). Guard env-gating bằng test.
- Rationale (verifiable): OpenAPI là bề mặt mô tả API; phơi ở prod đi ngược posture "nội-mạng, không public" + tăng bề mặt tấn công → gate Development. Overlay explicit (không dùng docker-compose.override.yml tự-áp) để `docker compose up` mặc định KHÔNG âm thầm thành Development (tránh bất ngờ / rò OpenAPI ngoài ý muốn). Native JSON đủ để nhập vào client OpenAPI; UI là quyết định riêng (QR-TO-012).
- Alternatives: bật OpenAPI mọi môi trường (loại: phơi prod); dùng docker-compose.override.yml tự-áp (loại: đổi mặc định âm thầm → dễ rò); thêm Scalar/Swashbuckle UI ngay (hoãn: mâu thuẫn DV-015 — QR-TO-012); gate bằng config flag riêng (khả thi nhưng Environment.IsDevelopment là chuẩn ASP.NET, ít plumbing hơn).
- Consequences: dev test API qua `/openapi/v1.json`; prod sạch. Muốn UI/luồng-guest-thật cần quyết định tiếp (QR-TO-012 + dev bootstrap admin/room — chưa làm).
- Reversibility: High (chỉ Host wiring + 1 file compose). Traceability: `design-modules/*` (web-testability); DV-015/AD-068 base; QR-TO-012.
- Guard-Tests: `OpenApiExposureTests`

### QR-AD-034 — StarHill compose MẶC ĐỊNH tối giản (không RabbitMQ); messaging là overlay explicit; supersede compose-messaging-default của QR-AD-015
- Status: Accepted/Implemented (2026-07-15; guard HostSmokeTests boot messaging-off)
- Date: 2026-07-15
- Decider: AI (user yêu cầu giữ StarHill sạch–đơn giản cho ~60 phòng; RabbitMQ bật/tắt).
- Provenance/Evidence: `Program.cs` — messaging opt-in qua `Bedrock:Messaging:Enabled` (mặc định TẮT → `ThrowingEventBusPublisher`, không cần broker). `RequiredPortsValidator`+`BedrockPersistenceExtensions.AddBedrockOutbox` (P1-15): chỉ Identity là outbox producer → off-mode cần `Bedrock:Messaging:AllowOutboxWithoutDispatcher=true`. `HostSmokeTests` (SecretInjectingHostFactory) đang boot Host messaging-OFF và pass → StarHill chạy không cần RabbitMQ đã được kiểm chứng. Cascade GuestVisitEnded (QR-AD-027) — nhu cầu messaging thật — hiện DEFER.
- Decision/Change: `docker-compose.yml` = TỐI GIẢN (postgres + host, messaging off, `AllowOutboxWithoutDispatcher=true`). Tách `docker-compose.messaging.yml` overlay explicit (rabbitmq + `Messaging__Enabled=true` + RabbitMq__*) để bật khi cần. Supersede CHỈ phần "compose default chứa RabbitMQ" của QR-AD-015 (compose vẫn boot end-to-end; RabbitMQ e2e giờ qua overlay + Testcontainers + platform compose).
- Rationale (verifiable): 60 phòng + chưa có consumer cascade → broker thường trực là phức tạp thừa; default tối giản giảm bề mặt vận hành/tài nguyên, đúng "sạch đơn giản". Overlay explicit giữ đường bật khi Concierge/Housekeeping ra đời. Không đụng base (base vốn đã opt-in messaging).
- Alternatives: giữ RabbitMQ trong default (loại: phức tạp thừa cho 60 phòng); dùng override tự-áp (loại: đổi default âm thầm); bỏ hẳn messaging (loại: cascade tương lai cần — QR-AD-027). Cân nhắc thêm QR-TO-013 (mất always-on e2e demo ở default).
- Consequences: default `docker compose up` = 2 service; muốn event-driven phải thêm `-f docker-compose.messaging.yml`. Identity outbox event tích lũy nhẹ ở off-mode (fix tận-gốc = làm emission opt-in ở base — đề xuất riêng, chưa làm).
- Reversibility: High (2 file compose). Traceability: `Program.cs` messaging block; QR-AD-015 (supersede phần compose-messaging); QR-AD-027; QR-TO-013.
- Guard-Tests: `HostSmokeTests`

### QR-AD-035 — Rules Draft "đúng-một RuleSet/resort" bằng DB unique `ux_rule_set_resort` + lazy find-or-create + DraftConflict (không TOCTOU)
- Status: Accepted/Implemented (2026-07-15; D-Rules.2b)
- Date: 2026-07-15
- Decider: AI (spec/product design mô tả Publish đọc "the Draft RuleSet" số ít nhưng KHÔNG nói cách cưỡng chế bất biến "một Draft/resort" ở tầng lưu trữ).
- Provenance/Evidence: `docs/resort-qr-portal/design.md` §Data Models + `design-modules/04-rules.md` §4 — `PublishRulesUseCase` đọc "the Draft RuleSet" (số ít) của resort → ngầm định đúng-một. `CreateRuleSectionUseCase` (đã code) LAZY find-or-create RuleSet theo `ResortId`: nếu hai admin thêm section đầu tiên đồng thời, pre-check `FirstOrDefault(ResortId)` bị TOCTOU → có thể tạo HAI RuleSet mồ côi cho cùng resort ⇒ Publish nhập nhằng. Đọc `RuleSetConfiguration` (EF) + migration `20260715045457_AddRuleSetResortUnique` (đổi index thường `ix_rule_set_resort` → UNIQUE `ux_rule_set_resort`). Cùng lớp cơ chế partial-unique publication (CP4) đã dùng cho `ux_rule_publication_current`.
- Decision/Change: (1) `RuleSet.ResortId` mang ràng buộc UNIQUE cấp DB `ux_rule_set_resort` (nguồn sự thật của bất biến, không TOCTOU); (2) `CreateRuleSectionUseCase` giữ lazy find-or-create nhưng BẮT `UniqueConstraintViolationException` (base QR-AD-010) ở đường đua tạo RuleSet đầu tiên → trả `RulesErrors.DraftConflict` (`rules_conflict`, 409-Conflict) — an toàn, không phá dữ liệu, client thử lại sẽ thấy RuleSet đã có; (3) migration riêng `AddRuleSetResortUnique` (không sửa `InitialCreate` đã áp — delta sạch).
- Rationale (verifiable): DB-constraint là nguồn sự thật race-safe (nhất quán với triết lý QR-AD-010/QR-N-018 — không pre-check TOCTOU). Bắt-và-dịch unique-violation giữ `Rules.Application ⊥ EF` (I7) — use case không thấy `DbUpdateException`. Đặt là AD riêng (không gộp vào QR-AD-030) vì đây là quyết định lưu-trữ tự ra, cần truy vết độc lập.
- Alternatives: (a) pre-check `AnyAsync(ResortId)` rồi insert (loại: TOCTOU, hai request cùng qua check → hai RuleSet); (b) seed sẵn một RuleSet/resort lúc tạo resort (loại: coupling ResortConfig→Rules ngược tầng + RuleSet rỗng thừa cho resort chưa soạn nội quy); (c) khóa bảng/advisory-lock (loại: nặng, unique-index đủ + rẻ).
- Consequences: schema `rules` có unique trên `rule_set.resort_id`; mọi luồng tạo Draft phải chịu được `DraftConflict` (client retry). Migration path: DB đã áp `InitialCreate` cần chạy thêm `AddRuleSetResortUnique`.
- Reversibility: Medium (drop unique → về index thường; nhưng mất bất biến). Traceability: `design-modules/04-rules.md` §3/§4/§12 (D-Rules.2b); QR-AD-010 (unique-violation → neutral exception); QR-AD-030 (Rules approach); QR-N-035.
- Guard-Tests: `RulesPostgresConstraintTests`

### QR-AD-036 — PublishRulesUseCase: read-model `IRuleDraftReader` (F9) + IUseCase tự-quản transaction flip-before-insert + version = current+1 (không MAX query)
- Status: Accepted/Implemented (2026-07-15; D-Rules.3a)
- Date: 2026-07-15
- Decider: AI (spec/product design nói "publish snapshot" + "the Draft RuleSet" nhưng KHÔNG nói cách HIỆN THỰC trên ràng buộc base: `IRepository` cấm IQueryable (F9) + partial-unique kiểm mỗi row-write + không có accessor Repository()).
- Provenance/Evidence: `platform/src/Bedrock.Application/Ports/Persistence/IRepository.cs` — CHỈ FindById/FirstOrDefault/Any/Add/Update/Remove, "KHÔNG phơi IQueryable (F9)... đọc-nhiều → read-model/query riêng". `IUnitOfWork.ExecuteInTransactionAsync` reentrancy-safe (đọc port). `Entity` sinh `Guid.CreateVersion7()` trong ctor → Id publication sẵn cho FK copy TRƯỚC SaveChanges (đọc `Bedrock.Domain/Entities/Entity.cs`). Precedent `ResolveTokenUseCase` (QR-AD-026): IUseCase thường + ExecuteInTransactionAsync + flush-trước-insert cho partial-unique `ux_guest_visit_active`. Migration `ux_rule_publication_current` filter `is_current` (đọc `RulePublicationConfiguration`). Sau code: `Rules.IntegrationTests` 12/12 pass Docker thật (4 test CP4: version-1 frozen ordered, demote+single-current, immutability, empty→fail).
- Decision/Change: (1) đọc Draft (list section+translation ordered) qua **read-model NỘI-MODULE `IRuleDraftReader`** (`Rules.Application` port + `EfRuleDraftReader` no-tracking ở Infra) — KHÔNG ép IRepository list (F9); (2) `PublishRulesUseCase` là **`IUseCase` value-returning tự quản transaction** qua `ExecuteInTransactionAsync` (KHÔNG `ICommandUseCase`) vì cần HAI SaveChanges có kiểm soát thứ tự trong một transaction; (3) **flip-before-insert**: hạ `IsCurrent=false` bản hiện hành + SaveChanges TRƯỚC → insert publication mới + copy đông cứng section/translation (đã sanitize ở Draft — CP12) → SaveChanges; (4) `Version = (current?.Version ?? 0) + 1` — bản current LUÔN là version cao nhất nên KHÔNG cần truy vấn MAX riêng (đơn giản + đúng bất biến); (5) publish Draft rỗng → `RulesErrors.NoPublishableContent` (không tạo publication rỗng).
- Rationale (verifiable): read-model tuân F9 (không rò provider), cùng hướng dẫn IRepository + precedent `IGuestSessionStore`. IUseCase-tự-quản là cách duy nhất kiểm soát hai-phase flush nguyên tử (ICommandUseCase chỉ bọc một transaction quanh một ExecuteAsync, không cho flush-giữa-chừng có chủ đích). Flip-before-insert là cùng lớp bài học Npgsql partial-unique (QR-N-018/QR-AD-026) — fix tận gốc thứ tự lệnh, không dựa may rủi thứ tự EF. version=current+1 tránh query MAX thừa (current là nguồn sự thật).
- Alternatives: (a) `ICommandUseCase` transactional (loại: không cho hai SaveChanges kiểm soát thứ tự — flip phải flush trước insert); (b) insert mới rồi hạ cũ (loại: hai hàng cùng thỏa partial-unique tại insert → 23505); (c) truy vấn `MAX(Version)` (loại: thừa — current.Version đã là max); (d) đọc Draft bằng IRepository nhiều lần FirstOrDefault (loại: không list được, không sort).
- Consequences: module Rules có read-model port riêng cho publish; thêm 3 repo publication keyed. Preview/history (D-Rules.3b) + guest read/ack + gate (D-Rules.4) chưa làm → QR-AD-030 vẫn Proposed (snapshot-publish PART xong).
- Reversibility: Medium. Traceability: `design-modules/04-rules.md` §4/§12 (D-Rules.3); QR-AD-030 (Rules approach); QR-AD-026 (precedent transaction); QR-N-018 (Npgsql ordering); F9 (IRepository no-IQueryable); QR-N-036.
- Guard-Tests: `PublishRulesUseCaseTests`

### QR-AD-037 — Faq module foundation: keyed schema `faq` + 4 entity + cây qua `ParentId` self-FK + KHÔNG Draft/Publish (khác Rules) + xmin + FK Restrict backstop
- Status: Accepted/Implemented (2026-07-16; E-Faq.1 persistence + E-Faq.2 CRUD/sanitize/tree-invariant + E-Faq.3 reorder + E-Faq.4a guest tree read/rule-gate/Api/Host-wiring; admin READ-tree editor+missing-langs hoãn E-Faq.4b pairs FE)
- Date: 2026-07-16
- Decider: AI (product design liệt kê data model Faq* nhưng KHÔNG nói: cách cưỡng chế "cây" ở tầng lưu trữ, có Draft/Publish hay không, base entity/concurrency).
- Provenance/Evidence: `docs/resort-qr-portal/design.md` §Data Models (FaqCategory[Id,ResortId,Key,SortOrder,IsActive]/FaqCategoryTranslation/FaqItem[ParentId self]/FaqItemTranslation, unique `(FaqItemId,LanguageCode)`/`(FaqCategoryId,LanguageCode)`) + §Optimistic concurrency (FaqItem/FaqItemTranslation có concurrency token). Requirements Req 4 (FAQ cha-con do lễ tân soạn, active/inactive) — KHÔNG nhắc version/acknowledge/publish cho FAQ (khác Req 8 nội quy). Mirror khung Rules đã đọc thật (`RuleSet`/`RulesDbContext`/`RulesConfigurations`/`RulesDbContextFactory`). Sau code: migration `20260715192548_InitialCreate` (verify grep: 4 CreateTable; `xid` rowVersion cả 4; unique `ux_faq_category_key`/`ux_faq_category_translation_lang`/`ux_faq_item_translation_lang`; FK `ReferentialAction.Cascade` translation→cha ×2 + `Restrict` item→category & item→parent ×2). `vp all` build 0-warning + full suite 0-fail (StarHill.ArchitectureTests 24 gồm 3 FaqBoundaryTests; Faq.IntegrationTests 4 skip Postgres không-Docker).
- Decision/Change: (1) module `Faq` 5-project (E-Faq.1 dựng Domain/Contracts/Infrastructure), keyed persistence `FaqModule.PersistenceKey="faq"`, `FaqDbContext:PlatformDbContext` schema `faq`, KHÔNG Outbox/Inbox; (2) 4 entity `: Entity, IHasConcurrencyToken` (xmin — CP15) mirror Rules Draft; cây = `FaqItem.ParentId?` self-FK trong CÙNG category; (3) **KHÔNG Draft→Publish/version cho FAQ** (khác Rules) — requirements không yêu cầu ack/version, `IsActive` đủ ẩn/hiện, sửa hiển thị ngay là hành vi mong muốn của lễ tân; tránh gold-plate (I10); (4) mở rộng xmin sang Category/CategoryTranslation (product chỉ liệt kê Item/*) — nhất quán chống ghi đè âm thầm Name; (5) FK **Restrict** item→category & item→parent (backstop DB cho invariant "chặn xóa cha còn tham chiếu" enforce ở use case E-Faq.2); FK **Cascade** translation→cha (bản dịch thuộc cha); (6) migration `InitialCreate` per-module (history table schema `faq` — QR-AD-028).
- Rationale (verifiable): keyed schema + Entity+xmin + FK là cùng khuôn Rules/Rooms/GuestAccess đã kiểm chứng (drift bất khả thi — D1-a). KHÔNG Draft/Publish vì spec không đòi — thêm sẽ là gold-plate + phức tạp thừa cho nội dung FAQ đơn giản. FK Restrict là nguồn-sự-thật race-safe cho toàn vẹn cây (không TOCTOU), song song cách QR-AD-035 dùng unique cho RuleSet.
- Alternatives: (a) FAQ có Draft/Publish như Rules (loại: gold-plate, spec không đòi ack/version); (b) cây bằng bảng closure/materialized-path (loại: thừa cho dữ liệu nhỏ ~chục item; ParentId self-FK + duyệt in-memory đủ — chi tiết E-Faq.2); (c) FK Cascade item→category (loại: xóa category âm thầm cuốn item — muốn CHẶN tường minh); (d) không xmin cho Category (loại: hai lễ tân đổi Name đè nhau âm thầm).
- Consequences: schema `faq` có 4 bảng + 3 unique + 3 index; use case CRUD/reorder (E-Faq.2/3) phải chịu `faq_conflict` (unique) + validate cây; guest read + rule-gate + Api (E-Faq.4) — Faq là consumer đầu tiên của `IRuleGate`. Host wiring + CI bundle `faq` để E-Faq.4 (khi có Api — mirror Rules D-Rules.1 chưa wire Host).
- Reversibility: Medium (drop schema). Traceability: `design-modules/05-faq.md` §2/§3/§10/§11 (E-Faq.1); QR-AD-028 (migration history per-schema); QR-AD-012 (D1-a keyed base); QR-N-045 (design) / QR-N-046 (code).
- Guard-Tests: `FaqBoundaryTests`, `FaqPostgresConstraintTests`, `FaqSanitizeTests`, `FaqItemParentValidationTests`, `FaqAdminCrudTests`, `FaqConcurrencyTests`, `ReorderFaqUseCaseTests`, `ReorderValidatorTests`, `GuestFaqTreeUseCaseTests`, `FaqEndpointAuthTests`

### QR-AD-038 — `IdentityUser` v1 tối giản: KHÔNG mang `ResortId` (single-resort); role Admin/Staff → claim `role`
- Status: Proposed (design Wave F; triển khai slice F.1a)
- Date: 2026-07-16
- Decider: AI (spec không nói schema user trên Bedrock; Identity hiện không có User entity).
- Provenance/Evidence: `Identity.Domain` chỉ có `AuthErrors` (đọc thật — không User); `StarHillPolicies` (role `admin`/`staff`, claim JWT-native `role`); sản phẩm single-resort (QR-DV-004: caller phân giải resortId qua `IResortSettingsQuery`). `RefreshAccessTokenUseCase` chỉ phát `sub` (không role) — comment "chờ user store".
- Decision/Change: `IdentityUser(Id, Username[unique,lower], PasswordHash[Argon2], Role[UserRole Admin/Staff], IsActive, DisplayName?, CreatedAt)` + enum `UserRole`. KHÔNG `ResortId` v1, KHÔNG concurrency token v1. Map role→claim `role`=admin|staff (StarHillPolicies).
- Rationale (verifiable): single-resort hiện tại → resortId không cần trên user (admin phân giải qua settings khi tạo phòng); thêm ResortId khi multi-resort thật (I10, không gold-plate). Không concurrency token vì chưa có màn CRUD user đua-ghi. Claim `role` khớp CHÍNH XÁC hợp đồng policy đã đọc → login xong authorize đúng.
- Alternatives: user mang ResortId ngay (loại: gold-plate cho single-resort); dùng permission-based thay role (loại: sản phẩm chỉ 2 vai — role đơn giản đủ, QR-AD-005).
- Reversibility: Medium (thêm cột ResortId = migration bổ sung sau). Traceability: `design-modules/06-identity-login.md` §2; QR-AD-005/020; QR-N-052.

### QR-AD-039 — Seed admin dev-only qua config (prod tạo admin out-of-band); login generic-error + timing-defense
- Status: Proposed (cần user duyệt cơ chế seed prod)
- Date: 2026-07-16
- Decider: AI (spec không nói cách bootstrap admin đầu tiên).
- Provenance/Evidence: `ResortConfigSeeder` (precedent seeder idempotent dev-gated); F35 (secret ngoài repo); `Argon2idPasswordHasher.Verify` hằng-thời-gian; triết lý `AuthErrors.InvalidRefreshToken` (một mã chung chống oracle).
- Decision/Change: (1) `IdentityUserSeeder` idempotent, GATED cờ dev; username+password từ config (`Identity:SeedAdmin:*`), KHÔNG hardcode; prod KHÔNG bật seeder (admin tạo out-of-band/secret). (2) Login trả MỘT mã `invalid_credentials` cho mọi fail (user lạ/sai pass/inactive) + chạy `Verify` giả khi user null (cân bằng timing) → chống user-enumeration.
- Rationale (verifiable): bootstrap admin cần cơ chế nhưng KHÔNG được nhét password prod vào repo (F35); dev cần seed để test end-to-end. Generic-error + timing-defense là chuẩn chống enumeration (bản chất bảo mật, không phải trang trí).
- Alternatives: hardcode admin/password (loại: rò secret, F35); phân biệt mã lỗi user-not-found vs wrong-password (loại: oracle enumeration); seeder chạy cả prod (loại: password dev lọt prod).
- Reversibility: High (seeder + cờ). Traceability: `design-modules/06-identity-login.md` §7/§1; QR-AD-008 (seeder precedent); F35; QR-N-052.
