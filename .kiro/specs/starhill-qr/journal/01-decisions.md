# 01 — Autonomous Decisions (pha QR) — quyết định AI tự ra mà spec không nói

> Journal RIÊNG cho pha `starhill-qr` (tách khỏi journal `platform-base`). ID tiền tố `QR-AD-###`. Mỗi bản ghi có Provenance/Evidence thật.

---

### QR-AD-001 — Sản phẩm QR = bản VENDORED-COPY của base tại `starhill/`; `platform/` giữ SẠCH/tái dùng
- Status: Confirmed
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
