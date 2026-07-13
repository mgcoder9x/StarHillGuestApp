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
- Status: Proposed (chờ user duyệt design; mặc định áp dụng nếu không phản hồi)
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
