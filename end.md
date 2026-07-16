# HANDOFF — StarHill QR (chuyển máy / phiên mới)

> File bàn giao xuyên suốt. Đọc TOÀN BỘ trước khi làm. Cập nhật lại file này mỗi khi chuyển máy.
> Ngôn ngữ làm việc: **tiếng Việt**. Nguyên tắc: **tuyệt đối không bịa/không suy đoán — verify bằng code thật + lệnh thật, valid nhiều lần**.

---

## 0. TRẠNG THÁI CHỐT (đã verify lúc viết file này)

- **Nhánh:** `develop`. **Remote:** GitHub `mgcoder9x/StarHillGuestApp` (token nhúng URL). **HEAD = `c029cb6`**, đồng bộ origin/develop (`git rev-list --left-right --count origin/develop...HEAD` = `0 0`). Working tree SẠCH.
- **Verify trên máy hiện tại (KHÔNG Docker):** `starhill\scripts\vp.cmd all` → **build 0-warning + validate-ci OK + full suite 0-fail**. `vp journal` → **INV-1..6 xanh (6/6)**.
  - Test hiện tại: StarHill.Api.Tests **62**, Identity.UnitTests 9, Identity.IntegrationTests 9 (Docker), StarHill.ArchitectureTests 25, Faq.IntegrationTests 26 pass/5 skip(Postgres), Rules 27/9-skip, Rooms 25/2-skip, GuestAccess 20/13-skip, ResortConfig 12/3-skip, StarHill.Html 6, Rooms.UnitTests 2, ResortConfig.UnitTests 20, Bedrock.ContractTests 2.
  - Skip = test buộc PostgreSQL/Testcontainers (máy này không có Docker) → chạy THẬT trên CI/máy có Docker.

---

## 1. KIẾN TRÚC (D1-a — bất di bất dịch)

- **Hai cây song song:**
  - `platform/` = base Bedrock (.NET 10, modular monolith, prefix `Bedrock.*`) — **NGUỒN DUY NHẤT**. Solution `platform/Platform.slnx`. Sửa năng lực NỀN ở đây (không nhồi nghiệp vụ QR).
  - `starhill/` = sản phẩm QR — **ProjectReference thẳng vào `platform/src/*`** qua `$(PlatformSrc)` (Directory.Build.props). Solution `starhill/Platform.slnx` chỉ chứa project NGHIỆP VỤ QR. Drift base BẤT KHẢ THI (chỉ một base).
- **Module QR** (`starhill/src/Modules/<M>`) mỗi cái ~5 project: `<M>.{Domain,Contracts,Application,Infrastructure,Api}`. Cross-module CHỈ qua `<M>.Contracts` + Id trần (Guid), KHÔNG FK chéo-schema. Keyed persistence (mỗi module 1 schema Postgres, cùng 1 DB `starhill`).
- **Host** `starhill/src/Host/StarHill.Api` = composition root DUY NHẤT (ráp Infra + Api mọi module).
- ⚠️ **BẪY:** có thư mục nested stale `StarHillGuestApp/StarHillGuestApp/` — TUYỆT ĐỐI KHÔNG commit.

---

## 2. TIẾN ĐỘ BACKEND — 6/8 module + Auth Wave F

Phạm vi sản phẩm (design.md §2): **8 module + Dashboard**. Trạng thái:

| Module | Trạng thái |
|---|---|
| **Identity** | ✅ + **Wave F Login** (F.1 login `/v1/token/login` Argon2+JWT role-claim + seed-admin dev; F.2 refresh giữ role + chặn user inactive). Auth end-to-end verify (login→refresh→admin 2xx, reuse→401). |
| **ResortConfig** | ✅ settings + ngôn ngữ + i18n resolver (CP5/CP14/CP15) |
| **Rooms** | ✅ CRUD + QR token + render PNG + query |
| **GuestAccess** | ✅ resolve token + session/visit + portal-window + `ICurrentGuestContextResolver` (C-GA.4) |
| **Rules** | ✅ Draft→Publish snapshot + guest read + acknowledge (CP13) + `IRuleGate` (CP3) + admin preview/history |
| **Faq** | ✅ **E-Faq.1..4a** (persistence + CRUD/sanitize/cycle-check + reorder + guest tree read + rule-gate). **CÒN E-Faq.4b** (admin READ-tree editor: full tree incl inactive + raw translations mỗi ngôn ngữ + `MissingLanguages` — HOÃN pairs admin FE). |
| **Concierge** (chat khách↔lễ tân, SignalR, Conversation/Message/InternalNote) | ❌ **DỰNG MỚI** |
| **Housekeeping** (ticket dọn phòng) | ❌ **DỰNG MỚI** |
| **Dashboard** (thống kê, ghép ở Host) | ❌ |

**Còn lại khác:** C-GA.5 cascade (visit kết thúc → đóng hội thoại + hủy ticket, qua outbox/inbox — chỉ làm được khi có Concierge/Housekeeping); GuestAccess sweeper (defer); **Frontend Vue chưa dựng** (guest-web + admin-web — N-076).

---

## 3. BƯỚC KẾ TIẾP (đề xuất, theo dependency graph)

Chọn 1 trong (design-first cho mọi mục lớn: viết `design-modules/*.md` → diagnostics 0 → đọc lại valid → mới code):

1. **Housekeeping** (ticket dọn phòng) — đơn giản hơn Concierge (không SignalR), tái dùng `IRuleGate`/`ICurrentGuestContextResolver`/`IRoomTokenResolver` đã có. Kích hoạt C-GA.5 cascade (một phần).
2. **Concierge** (chat) — phức tạp nhất (realtime SignalR). Kích hoạt C-GA.5 cascade (đóng hội thoại).
3. **E-Faq.4b** — admin READ-tree (nhỏ, nhưng gắn với admin FE chưa dựng).
4. **Frontend** — guest-web (Vue) đọc nội quy/FAQ; hoặc admin-web. FE chưa bắt đầu; cần Playwright MCP để test browser (chưa cài — xem §5).

Khuyến nghị: **Housekeeping** trước (contained, tái dùng hạ tầng, mở đường cascade), rồi Concierge, rồi Dashboard, rồi FE. Hỏi user nếu muốn ưu tiên FE sớm để trực quan.

---

## 4. HỆ THỐNG JOURNAL + CHỐNG DRIFT (duy trì XUYÊN SUỐT — bất di bất dịch)

- **2 journal, mỗi cái 5 file** (yêu cầu "4-việc" của user + anti-drift):
  - **QR:** `.kiro/specs/starhill-qr/journal/{01-decisions, 02-deviations, 03-tradeoffs, 04-notes, 05-anti-drift}.md` — tiền tố `QR-`.
    - Hiện: **QR-AD max = 040, QR-N max = 056**, QR-DV-001..007, QR-TO-001..013.
    - `01-decisions` = quyết định AI tự ra spec không nói; `02-deviations` = chỗ đổi so yêu cầu; `03-tradeoffs` = trade-off; `04-notes` = điều cần biết + "NEXT" mỗi slice; `05-anti-drift` = cơ chế + **state line** (dòng "Journal QR: QR-AD-001..NNN ... QR-N-001..MMM ...") phải cập nhật mỗi lần.
  - **BASE:** `.kiro/specs/platform-base/journal/` (prefix trần AD/DV/TO/N). AD max hiện ~103 (AD-103 = fix BadHttpRequestException 500→400, do máy Docker thêm). Guard `platform/tests/Bedrock.ArchitectureTests/JournalConsistencyTests.cs`.
- **Guard QR:** `starhill/tests/StarHill.ArchitectureTests/StarHillJournalConsistencyTests.cs` — **INV-1..6**:
  - INV-1 ID unique+liên tục; INV-2 (KEYSTONE) mọi QR-AD phải có trong `05-anti-drift`; INV-3 không dangling ref (⚠️ KHÔNG ghi forward-ref số AD chưa tồn tại — dùng placeholder "QR-AD-0xx" trong design-modules là an toàn); INV-4 AD/DV có Status+Provenance; INV-5 CP## ∈ 1..15; **INV-6 (KEYSTONE)** mọi QR-AD Status chứa "Implemented" phải khai `- Guard-Tests: \`ClassA\`, \`ClassB\`` và MỌI class tồn tại THẬT (`class <Name>`) trong `starhill/tests/**/*.cs`.
- **VERIFY GATE mỗi increment (bắt buộc):** `cmd /c "starhill\scripts\vp.cmd all"` (build 0-warning + validate-ci + full test --no-build) + `cmd /c "starhill\scripts\vp.cmd journal"` (INV-1..6). Terminal quirk: redirect `> "$env:TEMP\x.txt" 2>&1` rồi `Get-Content | Select-String`.

---

## 5. MÔI TRƯỜNG + SỰ THẬT ĐÃ KIỂM (không bịa)

- **Máy hiện tại KHÔNG có Docker** → test Testcontainers/Postgres SKIP mềm (không phải fail). Verify không-Docker: SQLite in-memory + WebApplicationFactory/TestServer. Test Postgres (unique/xmin/race/constraint) chạy trên CI (`starhill-ci.yml`, ubuntu-latest có Docker) hoặc **máy khác có Docker** (máy đó đã dùng để chạy Docker verification — QR-N-050/051 tìm+sửa 2 bug thật).
- **KHÔNG có MCP/power nào** (`kiro_powers list` = trống; không có `.kiro/settings/mcp.json` ở workspace/user). ⇒ KHÔNG lái được browser. `web_fetch` chỉ HTTPS công khai (không localhost). Nếu cần test browser/E2E FE: đề xuất user cài **Playwright MCP** qua `kiro_powers configure`.
- Browser-substitute đang dùng: `HostEndpointWiringSmokeTests` (WebApplicationFactory boot Host thật, KHÔNG DB) — bắt lỗi wiring tầng HTTP (404 quên wire / 200 quên bảo vệ / auth sai) + `runtime HTTP probe` (QR-N-051).

---

## 6. GIT — KỶ LUẬT (bắt buộc)

- Commit **path tường minh**, KHÔNG `git add -A`. Trước commit kiểm: `git diff --cached --name-only | Select-String "/bin/|/obj/|StarHillGuestApp/StarHillGuestApp"` phải **RỖNG**.
- Commit: `git -c core.autocrlf=false commit -m "..."`.
- Push: `git push origin develop` — PowerShell hiển thị **exit=1 do stderr** dù push THẬT thành công. Xác nhận bằng dòng `X..Y develop -> develop` + `git rev-list --left-right --count origin/develop...HEAD` = `0 0`.
- ⚠️ **CÓ cơ chế auto-commit** trong môi trường này (xuất hiện commit tên "update" gộp file đang chờ). **BÀI HỌC (QR-N-053):** COMMIT NGAY mỗi slice có migration/file mới — work chưa-commit vắt qua phiên/máy từng gây migration trùng. Đừng để file treo lâu.
- Nhánh develop có thể được máy khác đẩy commit mới → luôn `git fetch` + kiểm HEAD/log trước khi bắt đầu (đầu phiên này HEAD đã nhảy từ Faq E-Faq.4a sang Identity F.2 do máy khác làm tiếp).

---

## 7. PATTERN CODE (tuân thủ khi thêm module — mirror module đã có)

- **Đọc port đọc:** interface (Application) + EF impl inject DbContext cụ thể + scoped (như `EfFaqReader`/`EfRulePublicationReader`). Read-model KHÔNG dùng `IRepository` (F9 cấm IQueryable) — trả DTO snapshot, ghép cây/list trong bộ nhớ.
- **Write use case** value-returning MỘT SaveChanges = `IUseCase<TIn,TOut>` (mirror CreateRoom/CreateFaqCategory). Void command = `ICommandUseCase<TInput>` khai `PersistenceKey`.
- **Keyed DI:** `AddBedrockPersistence<Db>(key, cfg)` + `AddBedrockRepository<Db,Entity>(key)` + use case factory `sp.GetRequiredKeyedService<IRepository<E>>(key)`. Validator `AddTransient<IValidator<TIn>, V>()`.
- **Bắt `UniqueConstraintViolationException`** (base QR-AD-010) → dịch sang Error module (KHÔNG để DbUpdateException lọt Application — I7 boundary test).
- **Sanitize-on-save (CP12):** nội dung HTML admin nhập qua `IHtmlSanitizer` (StarHill.Html, `AddStarHillHtml`, RequirePort đã cắm ở Host) TRƯỚC khi lưu.
- **i18n (CP5):** `ITranslationResolver.MatchSupported` + `Resolve<T>` (T implement `ITranslation`, đặt trên DTO read-model — QR-DV-007, KHÔNG trên entity Domain).
- **Guest endpoint:** resolve `ICurrentGuestContextResolver` (cookie `GuestAccessModule.SessionCookieName` + roomId) → use case → check-before-touch (GET rules KHÔNG touch; GET faq TOUCH sau thành công — QR-N-045); `Cache-Control: no-store`; map Result→HTTP qua `ProblemDetailsBuilder`; KHÔNG log cookie.
- **Admin endpoint:** RequireStaff/RequireAdmin (StarHill.Authorization); resortId phân giải server-side qua `IResortSettingsQuery.GetAsync()`.
- **Mã lỗi mới:** thêm vào `<M>Errors` + cập nhật `starhill/tests/Bedrock.ContractTests/ErrorCodeSnapshotTests.cs` (ExpectedCodes theo Ordinal + assembly catalog) — QR-AD-018.
- **Migration:** `dotnet tool run dotnet-ef migrations add <Name> --project ... --startup-project ... -o Persistence/Migrations` (local tool dotnet-ef 10.0.9; không cần DB). `.editorconfig` đã miễn analyzer cho `**/Persistence/Migrations/*.cs` (QR-AD-009). Thêm bundle module vào `starhill-ci.yml` khi wire Host.

---

## 8. USER — YÊU CẦU CỐ ĐỊNH

- Trả lời tiếng Việt; nghiêm ngặt, không bịa/không suy đoán, verify nhiều lần.
- **Design-first** cho mục lớn (design doc → đọc lại valid → mới code). Mục contained thì code + verify trực tiếp.
- **Fix tận gốc, không fix ngọn.** Không tiết kiệm token để nhanh xong. Sản phẩm thương mại lâu dài.
- Khi khuyến nghị PHẢI nói LÝ DO CHÍNH XÁC.
- Duy trì thư mục journal 4-việc + cơ chế chống drift cực mạnh (INV-1..6).
- FE (khi tới): ưu tiên **Vue.js**.

---

## 9. FILE QUAN TRỌNG

- Design tổng: `.kiro/specs/starhill-qr/design.md` + `design-modules/{01-resortconfig,02-rooms,03-guestaccess,04-rules,05-faq}.md`.
- Spec nguồn sản phẩm: `docs/resort-qr-portal/{requirements,design,tasks,test-plan,deep-solution-design}.md`.
- Journal: `.kiro/specs/starhill-qr/journal/*.md` (đọc `04-notes.md` đuôi để lấy "NEXT" + `05-anti-drift.md` state line để lấy trạng thái).
- Verify: `starhill/scripts/vp.cmd` (build|all|journal|ci) + `starhill/tools/verify.ps1`.
- Reference theme (nếu làm admin FE): `Reference/EPS.Vuexy/` (Vue 3 + Vuexy). Legacy logic tham chiếu: `resort-qr/` (bản cũ standalone, KHÔNG phải nền Bedrock).
