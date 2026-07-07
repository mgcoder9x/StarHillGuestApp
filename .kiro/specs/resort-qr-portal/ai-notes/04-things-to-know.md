# 04 — Bất kỳ điều gì cần biết (giả định, rủi ro, việc CHƯA xác minh)

> Mục này ghi những điều **chưa chắc chắn** hoặc **cần user/kiểm chứng** trước khi triển khai. Tuyệt đối không coi là fact cho tới khi Status = Verified.

## Kiểm chứng đã làm

- Ngày kiểm chứng code `Reference/Backend`: **2026-07-03**.
- Đã đọc & xác minh: `BaseRepository.cs`, `IBaseRepository.cs`, `UnitOfWork.cs`, `AutoDependencyExtensions.cs`, `Program.cs`, `FresherDev.HMS.Api.csproj`. Kết quả trong `../design/backend/09-reference-reconciliation.md` (E1–E8).

## Việc CHƯA xác minh / cần làm trước khi code

### TK-001: Đã đọc `FresherDev.HMS.Auth` — RỖNG
- Status: ✅ Verified (2026-07-03)
- Kết quả: Project `Auth` chỉ có `ForceLoadAssembly.cs` + `.csproj` (net8.0, reference `Common`). **Không có bất kỳ code auth nào** (không JWT, không hasher, không login). Xem `../design/backend/09-reference-reconciliation.md` E9.
- Phát hiện kèm theo (nghiêm trọng): mật khẩu lưu **plaintext** (E10), không có cấu hình JWT trong appsettings (E11), pipeline không wire authentication (E5).
- Quyết định: **Auth viết mới hoàn toàn** — không có gì để tái dùng. Xem DEC-011 trong `01-autonomous-decisions.md`.

### TK-002: UUIDv7 locality trên PostgreSQL — 1 bước benchmark bắt buộc ở foundation
- Status: ✅ Decision firm (DEC-003) + verification step xác định
- Sự thật đã verify: PG `uuid` sort theo byte canonical; UUIDv7 time-ordered → tuần tự (index ~26-27% nhỏ hơn, scan ~3x). PG18 có `uuidv7()`. `Guid.CreateVersion7()` có .NET 9+/.NET 10. Npgsql serialize Guid canonical (id::text khớp ToString).
- Điểm CHƯA tự benchmark: byte-layout `Guid.CreateVersion7()` có đạt locality PG như suy luận không (1 báo cáo cộng đồng nói ngược — chưa tái lập).
- **Verification bắt buộc (Task foundation):** insert ~100k rows, đo index size/bloat + ordered-scan. Tiêu chí đạt: index không phình đáng kể so với `uuidv7()` DB.
- Nếu KHÔNG đạt → đổi nguồn sinh sang `HasDefaultValueSql("uuidv7()")` (PG18), **cột giữ nguyên `uuid`** (zero-schema-change).
- Refs: DEC-003, TRD-003, `../design/backend/02` §1.

### TK-002b: `Base64Url` cho token
- Status: Verified (.NET 9+) — fallback `Convert.ToBase64String` + URL-safe replace nếu cần.

### TK-003: `xmin` concurrency token với Npgsql — ĐÃ VERIFY + sửa lỗi thiết kế
- Status: ✅ Verified (2026-07-03)
- Kết quả: Npgsql dùng **`UseXminAsConcurrencyToken()`** (map cột hệ thống `xmin`) — xác nhận qua npgsql.org/efcore/modeling/concurrency. **`IsRowVersion()` kiểu SQL Server là SAI** cho Npgsql (khác ngữ nghĩa; từng có bug thêm cột `xmin` thật vào migration).
- **Lỗi thiết kế đã sửa:** snippet `03` §3 trước đây dùng `IsRowVersion()` → đã đổi sang `UseXminAsConcurrencyToken()`.
- Lưu ý phụ: sau `pg_restore`/backup, giá trị `xmin` thay đổi (chỉ ảnh hưởng token concurrency đang bay, không ảnh hưởng đúng đắn dài hạn).
- Hành động: test bằng integration test (Property B10). Npgsql provider phải là dòng 10.x (khớp EF Core 10) — TK-018 (đã verify Npgsql 10 tồn tại).

### TK-004: Mật khẩu admin seed
- Status: NeedsUserInput
- Context: Seeder tạo tài khoản Admin. KHÔNG hardcode mật khẩu.
- Hành động: Lấy từ biến môi trường/`appsettings` (secret). User cần cung cấp/cấu hình giá trị.
- Refs: `../design/backend/04` §6.

### TK-005: Version cụ thể của các package
- Status: Unverified
- Context: `07-testing-strategy.md` liệt kê package **không kèm version** để tránh bịa số. Version thực chốt ở `Directory.Packages.props`.
- Hành động: Khi tạo solution, kiểm tra version tương thích .NET 10 rồi pin.

### TK-006: Frontend reference đã được khảo sát (EPS.Vuexy)
- Status: ✅ Verified (2026-07-03)
- Kết quả: reference là Vuexy Vue 2 (EOL), JS, Vue CLI, chỉ để tham khảo → KHÔNG port (DEC-012). FE thiết kế lại Vue 3 + TS + Vite + Pinia.
- Còn cần user xác nhận: mức chi tiết màn hình mong muốn cho FE (hiện base chỉ dựng skeleton). UI kit đã chốt Element Plus (TRD-007).
- Refs: `../design/frontend/01-reference-assessment.md`, `02-architecture.md`.

### TK-017: Trạng thái archive của PrimeVue cần re-verify khi implement
- Status: Verified-at-2026-07-03 / Re-verify-on-implement
- Context: Web search 2026-07-03 cho thấy repo `primefaces/primevue` archived/read-only (28/6/2026). Archive một UI lib lớn khá bất thường — có thể là dời repo/đổi tổ chức.
- Rủi ro: nếu kết luận sai sẽ loại nhầm một lib tốt. Nhưng nguyên tắc an toàn: không chọn lib đang có tín hiệu ngừng bảo trì.
- Hành động: khi bắt đầu FE, kiểm tra lại npm `element-plus` (đang chọn) và trạng thái PrimeVue; nếu PrimeVue thực chất còn sống và tốt hơn, cân nhắc lại (nhưng Element Plus vẫn là lựa chọn an toàn).

### TK-018: Version patch cụ thể + tương thích chéo cần pin khi implement
- Status: Verify-on-implement
- Context: Đã verify major/minor (từ web): .NET 10, EF Core 10, PostgreSQL 18, Vite 8, Vue 3.5+, Node 24 LTS. Chưa pin patch và chưa verify tương thích chéo cụ thể (Npgsql 10.x ↔ EF10; Element Plus ↔ Vue 3.5; Vite 8 plugin ecosystem cho Vue).
- Hành động: pin ở `Directory.Packages.props` (BE) và `package.json` (FE), chạy thử build, ghi kết quả. Gộp với TK-005.
- Refs: `../design/technology-stack.md`.

## Giả định nền (đang giả định, user có thể phủ nhận)

- **A1:** ✅ ĐÃ GIẢI QUYẾT → tenancy = **instance-per-resort** (DB-per-tenant bằng triển khai), khớp docs D1. Không còn "giả định ngầm" mà là quyết định có chủ đích (DEC-028, `../design/backend/24-tenancy-model.md`). Mỗi deployment một resort; multi-resort = nhân bản triển khai. Chi phí chuyển shared-DB multi-tenant: TK-033.
- **A2:** Cùng origin (`portal.starhill.local`) cho guest/admin/api/hub ở MVP (docs để mở). Ảnh hưởng CORS/cookie.
- **A3:** HTTPS cert hợp lệ do hạ tầng cung cấp (docs yêu cầu) — app không tự lo cert.
- **A4:** Tải nhỏ (WiFi nội bộ) → không cần cache phân tán/scale-out ở base.

## Quy trình bắt buộc (theo yêu cầu user)

1. Thiết kế rõ ràng → user duyệt → valid lại → MỚI triển khai.
2. Mỗi lần fix: tìm bản chất (root cause), không vá ngọn.
3. Cập nhật file này mỗi khi có quyết định/giả định/rủi ro mới.

## Mục production/thương mại cần user quyết (từ 10-production-hardening.md)

### TK-007: Nguồn secret store cho staging/prod
- Status: NeedsUserInput
- Hỏi: dùng biến môi trường OS/Docker, hay có vault (Azure KeyVault/HashiCorp)? Ảnh hưởng cách nạp `Jwt:SigningKey`, connection string.

### TK-008: Chính sách chống brute-force login admin
- Status: NeedsUserInput
- Hỏi: khóa tạm sau N lần sai (N=?), thời gian khóa? Có cần captcha không (nội bộ có thể không cần).

### TK-009: Retention dữ liệu lượt lưu trú cũ
- Status: NeedsUserInput
- Hỏi: giữ chat/ack/ticket của visit đã đóng bao lâu? Có xóa/ẩn tự động sau X ngày không? Base để soft-delete + Expired/Closed, chưa auto-purge.

### TK-010: Bảng AuditTrail riêng cho hành động nhạy cảm
- Status: Proposed / NeedsUserInput
- Đề xuất: có bảng audit riêng (revoke QR, đổi settings, publish, đổi role). User xác nhận có cần cho MVP thương mại hay để wave sau.

### TK-011: Quy trình áp migration khi deploy
- Status: NeedsUserInput
- Đề xuất: KHÔNG auto-migrate khi khởi động ở prod; chạy migration là bước deploy riêng có kiểm soát. Cần chốt.

### TK-012: Công cụ secret scanning + dependency scanning trong CI
- Status: NeedsUserInput
- Phụ thuộc nền CI (GitHub Actions/GitLab/Azure DevOps?). Chưa biết hạ tầng CI của resort.

### TK-013: Backup/DR (tần suất, nơi lưu, test restore)
- Status: NeedsUserInput
- Docs: MVP tối thiểu script dump thủ công. Commercial: pg_dump định kỳ + test restore. Cần chốt tần suất/nơi lưu.

### TK-014: API versioning scheme
- Status: NeedsUserInput / Proposed
- Đề xuất: path-based `/api/v1/...`. Cần user xác nhận trước khi cố định route.

### TK-015: Metrics/Tracing (OpenTelemetry)
- Status: Deferred (Proposed)
- Đề xuất: để giai đoạn sau; base không cản trở việc thêm. Ghi để không quên.

### TK-016: Nền CI/CD & môi trường triển khai thực tế
- Status: NeedsUserInput
- Hỏi: Docker? Reverse proxy nào (Nginx/Caddy/IIS)? OS server? Ảnh hưởng deployment doc.

## Từ audit kiến trúc (11-architecture-review-and-gaps.md)

### TK-019: License QuestPDF theo ngưỡng doanh thu
- Status: NeedsUserInput / Verify-on-implement
- QuestPDF MIT nếu doanh thu < $1M/năm; vượt phải mua (90 ngày ân hạn). Cần theo dõi khi sản phẩm thương mại lớn lên. Cân nhắc alternative (ví dụ tự render HTML→PDF) nếu muốn tránh phụ thuộc.

### TK-020: CSRF cho endpoint guest ghi (cookie-auth)
- Status: To-implement (GAP-2)
- Khi làm auth: SameSite=Lax + yêu cầu custom header cho POST guest + CORS same-origin. Ghi vào hardening §2.

### TK-021: Outbox pattern (tương lai, không phải base)
- Status: Deferred (GAP-4)
- Nếu cần at-least-once cho realtime/side-effect. Base dùng post-commit notify là đủ.

### TK-022: OpenAPI endpoint backend (nguồn sinh type FE)
- Status: ✅ DONE (2026-07-06, DEC-079) — expose `/openapi/v1.json` bằng `Microsoft.AspNetCore.OpenApi` 10.0.9
  (built-in .NET 10, KHÔNG NSwag/Swashbuckle). An-toàn-mặc-định (gate map theo `OpenApi:Enabled`). Còn NỢ:
  chú thích `.Produces<T>()` per-endpoint để schema response đầy đủ (TK-054).

### TK-023: Design-time DbContext factory
- Status: To-implement (GAP-8)
- Thêm `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` để tạo migration.

### TK-024: Guest SignalR auth (cookie) + join-conversation authorize — ✅ đặc tả xong
- Status: ✅ Specified (2026-07-03) ở `../design/backend/21-realtime-signalr.md`
- Kết quả: auth kép (cookie guest / JWT admin qua query `access_token`); `JoinConversation` authorize bằng dữ liệu server (IGuestContext→visit→conv), không tin client; guest không join group staff. GAP-1 resolved.
- Còn lại khi implement: mask `access_token` query trong log (đã ghi `19` §4); test B9 cho hub.

### TK-030: Nguồn cert HTTPS (internal CA vs public qua DNS nội bộ)
- Status: NeedsUserInput (quyết định còn mở từ docs README)
- Ảnh hưởng cấu hình proxy (ACME tự động vs cert thủ công). Caddy auto-TLS chỉ tiện nếu ACME khả dụng; internal CA cần cài root cert vào thiết bị khách.
- Refs: `../design/backend/20-deployment-reverse-proxy.md` §3.

### TK-031: Thay đổi khi scale-out nhiều instance (không làm ở base)
- Status: Deferred
- Nếu scale >1 instance: SignalR cần Redis backplane; rate limiter cần store phân tán; VisitIdleSweeper cần chống chạy trùng (leader/lock). Base single-instance an toàn; thiết kế không cản việc thêm sau.
- Refs: `../design/backend/20` §7, `21` §7.

### TK-025: Npgsql `timestamptz` + `DateTimeOffset` — confirm khi implement
- Status: Unverified (web search không khả dụng lúc thiết kế 2026-07-03)
- Nội dung: theo hiểu biết, Npgsql (từ 6.0) xử lý `timestamp with time zone` nghiêm ngặt: `DateTimeOffset` map `timestamptz` yêu cầu offset = 0 (UTC), offset khác 0 ném lỗi runtime. Ta luôn dùng `IDateTimeProvider.UtcNow` (offset 0) nên an toàn.
- **CHƯA verify lại được** (tool web tạm ngừng) → đánh dấu cần confirm bằng doc Npgsql chính thức khi implement, hoặc test nhỏ (insert/read timestamptz).
- Nếu Npgsql đổi hành vi: cân nhắc dùng `DateTime` (Kind=Utc) thay `DateTimeOffset` cho cột timestamptz.
- Refs: `../design/backend/04` §9.

### TK-026: Tham số Argon2id — tune theo OWASP + benchmark, không hardcode
- Status: NeedsTuning-on-implement
- Nội dung: `memoryKiB`, `iterations`, `parallelism`, `saltLength (≥16)`, `hashLength (≥32)` phải chốt theo OWASP Password Storage Cheat Sheet hiện hành + benchmark phần cứng prod để 1 lần hash ~ vài trăm ms. Không lấy số cứng từ tài liệu (tránh bịa "số thần thánh").
- Lưu PHC string tự mô tả để rehash-on-login khi nâng tham số.
- Refs: `../design/backend/12-identity-and-auth.md` §1.

### TK-027: IP/subnet của reverse proxy (cấu hình KnownProxies)
- Status: NeedsUserInput (phụ thuộc hạ tầng)
- Cần: địa chỉ/subnet thật của reverse proxy để khai `ForwardedHeadersOptions.KnownProxies/KnownNetworks`. Sai → rate limit theo IP + log IP sai, hoặc mở đường giả mạo X-Forwarded-For.
- Refs: `../design/backend/03` §8; DEC-018.

### TK-028: Allowlist HtmlSanitizer cuối cùng (có cần bảng/ảnh trong nội quy/FAQ?)
- Status: NeedsUserInput
- Cần: xác nhận nội dung nội quy/FAQ có cần `<img>`, `<table>` không → mở rộng allowlist an toàn (img: chỉ src same-origin/scheme allowlist). Mặc định base: text + định dạng cơ bản, không img/table.
- Refs: `../design/backend/03` §9; DEC-018.

### TK-029: Phân biệt biến thể chữ Hán (zh-Hant/zh-Hans) nếu resort cần
- Status: NeedsUserInput (nếu phát sinh)
- Nội dung: base so khớp ngôn ngữ theo **primary-subtag** → `zh-Hant` và `zh-Hans` cùng map `zh`. Nếu resort cần phân biệt phồn/giản thể, phải bật mã riêng làm `ResortLanguage.Code` và client gửi đúng mã. Hiện enabled = en/vi/ko/zh (một `zh`).
- Refs: `../design/backend/18-localization.md` §2.

### TK-032: Chạy lại cross-consistency audit sau mỗi lần sửa design lớn
- Status: Process-note
- Nội dung: `../design/backend/22-consistency-audit.md` có ma trận bất biến + con số đa-nơi. Sau mỗi thay đổi design lớn, grep lại các pattern rủi ro (thuật ngữ quyết định, con số) để phát hiện drift sớm. Không dựa trí nhớ — dựa grep + đối chiếu file.
- Refs: `../design/backend/22-consistency-audit.md`.

### TK-033: Checklist chuyển sang shared-DB multi-tenant (CHỈ nếu đổi mô hình bảo mật)
- Status: Deferred (không làm ở base — DEC-028 chọn instance-per-resort)
- Nếu MỘT NGÀY cần SaaS tập trung nhiều resort trong một DB: (1) thêm `ResortId` vào `GuestSession`; (2) đổi `ux_appuser_email` → `(resort_id, email)`; (3) tenant-resolution middleware (host/subdomain/claim) + EF global query filter theo `ResortId` mọi tenant-entity; (4) per-tenant SigningKey/secret; (5) soát mọi partial unique index scope theo ResortId; (6) rà rate-limit/cache phân tenant. Đây là thay đổi mô hình bảo mật (bỏ tiền đề "mạng nội bộ") — cân nhắc kỹ.
- Refs: `../design/backend/24-tenancy-model.md` §6.

### TK-034: Toolchain máy dev THỰC TẾ — chặn Wave 0 build-verified (kiểm chứng bằng lệnh 2026-07-04)
- Status: ✅ Verified (chạy lệnh trực tiếp) — có blocker cần user xử lý trước khi code
- Kết quả đo được (không suy đoán):
  - **.NET SDK: KHÔNG có** — `dotnet --version` trả `No .NET SDKs were found` (+ gợi ý tải `aka.ms/dotnet/download`). ⇒ **Blocker cứng**: không thể `dotnet new`/build 5 project (task #1,#2). Không scaffold "mù" vì sẽ tạo trạng thái không build được (vi phạm "kiểm chứng được rồi mới triển khai").
  - **Node.js: v25.2.1** — nhưng `tasks.md`/`technology-stack.md` chốt **Node 24 LTS**. Sự thật release policy Node.js (ổn định, không cần tra cứu): major **chẵn** mới lên LTS (24 = "Krypton" LTS); major **lẻ** (25) là **"Current" — KHÔNG bao giờ thành LTS**. ⇒ Node 25 **mâu thuẫn DEC-015** (chỉ dùng stable/LTS cho commercial). Rủi ro: engine không được hỗ trợ dài hạn + một số package khai `engines` chỉ tới LTS.
  - **pnpm: chưa cài** (`pnpm` không nhận diện). Design dùng **pnpm workspaces** (monorepo). ⇒ chưa dựng được workspace theo `frontend/05`.
  - **npm: 11.6.2** — có sẵn (đi kèm Node).
- Bản chất (fix gốc, không vá ngọn): đây là **điều kiện tiên quyết môi trường**, không phải lỗi thiết kế. Không hạ chuẩn design cho khớp máy (sẽ sinh nợ kỹ thuật commercial); thay vào đó **chuẩn hoá máy dev về đúng baseline đã chốt**.
- Hành động đề xuất (user thực hiện, kèm lý do chính xác):
  1. **Cài .NET 10 SDK (LTS)** từ nguồn chính thức Microsoft (`https://dotnet.microsoft.com/download` / `aka.ms/dotnet/download`) — bắt buộc để build backend. Verify lại bằng `dotnet --list-sdks` (kỳ vọng dòng `10.x`).
  2. **Cài Node 24 LTS** (thay/đặt song song v25) — khớp policy LTS thương mại. Có thể dùng `nvm-windows` để giữ nhiều bản. Verify `node --version` (kỳ vọng `v24.x`).
  3. **Bật pnpm qua Corepack** (`corepack enable pnpm`) — Corepack đi kèm Node, là cách chính thức cấp pnpm đúng version pin trong `package.json#packageManager` (tái lập được, không lệch máy). Verify `pnpm --version`.
- Điểm CHƯA verify (web search ECONNRESET lúc 2026-07-04): **số patch chính xác** của .NET 10 SDK và Node 24.x mới nhất. → pin số cụ thể ở `Directory.Packages.props`/`package.json` khi cài xong (gộp TK-005/TK-018). KHÔNG ghi số bịa vào doc/code.
- Hệ quả cho tiến độ: **Wave 0 (build-verified) tạm dừng** tới khi (1)+(3) sẵn sàng cho phần backend/monorepo. Có thể làm trước phần KHÔNG cần toolchain nếu user muốn (ví dụ soạn nội dung file cấu hình `Directory.Build.props`/`Directory.Packages.props`/`package.json` dạng bản nháp) — nhưng sẽ ở trạng thái **UNVERIFIED** cho tới khi build được (đánh dấu rõ, đúng nguyên tắc).
- Refs: `tasks.md` Wave 0 (#1,#2,#3); `../design/technology-stack.md`; DEC-015; TK-005/TK-018.

### TK-034 — UPDATE (2026-07-04): đã cài .NET 10 SDK + pnpm; Node LTS còn treo
- **.NET SDK 10.0.301** đã cài qua `winget install Microsoft.DotNet.SDK.10` → verify `dotnet --version`=10.0.301, `dotnet --list-sdks`=`10.0.301 [C:\Program Files\dotnet\sdk]`. ✅ Blocker backend GỠ.
- **pnpm 11.9.0** đã cài qua `npm install -g pnpm` (corepack không có trên PATH nên dùng npm global). ✅
- **Node vẫn v25.2.1** (Current, không LTS). CHƯA đổi sang 24 LTS vì đổi Node ảnh hưởng **toàn máy** (có thể vỡ project khác) → cần user xác nhận cách đổi (khuyến nghị `nvm-windows` để giữ song song). Node 25 CHẠY được tooling FE (Vite/pnpm) cho dev; lệch LTS chỉ là vấn đề chính sách commercial dài hạn (DEC-015) → xử lý trước khi làm Wave 0 task #3 (monorepo FE).
- Patch version .NET/Node: web search vẫn lỗi lúc này; .NET pin theo SDK thật đã cài (10.0.301 trong `global.json`). Node 24.x patch pin khi đổi.

### TK-035: IDE extensions + Docker (prerequisite cho integration test) — 2026-07-04
- **Build/test qua `dotnet` CLI đã chạy được** (không phụ thuộc extension) — extension chỉ để ergonomics editor (IntelliSense/debug).
- **Extension nên có (dev .NET đa nền tảng trên IDE nền VS Code như Kiro):**
  - **C# (`ms-dotnettools.csharp`)** — Roslyn LSP: IntelliSense/debug/refactor. *Must-have.*
  - `.NET Install Tool` — thường là dependency của C#.
  - EditorConfig support (C# ext đã tôn trọng `.editorconfig`; ext riêng giúp file non-C#).
  - **Docker extension + Docker engine** — ⚠️ *prerequisite thật*: IntegrationTests dùng **Testcontainers (PostgreSQL 18)** (DEC-021) → máy phải có Docker Desktop/Podman chạy; không có thì integration test không chạy được (unit/arch test vẫn chạy).
  - (tùy chọn) REST client để test API, PostgreSQL client để soi DB, GitLens.
- **⚠️ Lưu ý license/nguồn:** **C# Dev Kit (`ms-dotnettools.csdevkit`)** tiện (Solution/Test Explorer) NHƯNG license Microsoft giới hạn ở VS Code chính chủ + họ Visual Studio — trên IDE fork có thể **không được cấp phép/không có trên Open VSX**. → Kiểm tra trong Extensions panel của Kiro; nếu không có, **chỉ cần C# extension** là đủ để code. KHÔNG giả định C# Dev Kit khả dụng.
- Chưa kiểm chứng được extension nào Kiro/Open VSX đang cung cấp (web search lỗi) → user xác nhận trong panel Extensions.

### TK-036: Integration test DB thật (transaction/unique/xmin/race refresh) — chờ Docker
- Status: **PARTIALLY DONE (SQLite, Docker-free)** — phần Postgres-specific vẫn Deferred (chờ Docker).
- ĐÃ LÀM (SQLite in-memory thật, DEC-048): `TryConsumeAsync` consume-if-not-revoked (1 true/1 false ở tầng SQL thật, không phải mock lock), `RevokeFamilyAsync` (chỉ token active/đúng family), logout mutate+update+save, transaction rollback/commit all-or-nothing, audit set/không-ghi-đè-Created, xóa mềm filter, snake_case. Model Npgsql build offline (xmin mapping) verify.
- CÒN CHỜ DOCKER (Postgres-specific, KHÔNG kết luận được từ SQLite): (1) `TryConsumeAsync` dưới **2 connection đồng thời THẬT** (row-lock Postgres) — SQLite in-memory 1 connection chỉ kiểm ngữ nghĩa SQL `WHERE revoked_at IS NULL`; (2) partial unique index (`ux_qr_active`, `ux_visit_active`, `ux_pub_current`...) chặn ghi; (3) `xmin` concurrency runtime → 409; (4) `timestamptz` offset=0 (TK-025).
- Khi có Docker: thêm Testcontainers PostgreSQL, chạy N task refresh cùng token → đúng 1 thành công + family revoked khi reuse.
- Refs: DEC-043 #1/#6, DEC-048, TK-034; tests `Foundation.IntegrationTests/Persistence/**`.

### TK-037: Version EF/Npgsql/tool persistence đã verify THẬT (không bịa) — 2026-07-04
- Pin qua `dotnet add` + build xác nhận, dòng .NET 10: `Microsoft.EntityFrameworkCore` **10.0.9**, `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.2** (kéo Npgsql 10.0.3, EFCore.Relational 10.0.4→10.0.9), `Microsoft.EntityFrameworkCore.Design` **10.0.9** (PrivateAssets=all), `EFCore.NamingConventions` **10.0.1** (⚠️ điểm nghi ngại trong doc → xác nhận TƯƠNG THÍCH EF10, test snake_case chạy thật), `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` **10.0.9**; test `Microsoft.EntityFrameworkCore.Sqlite` **10.0.9**.
- Lưu ý: `dotnet add package` với CPM tự thêm `<PackageVersion>` vào `Directory.Packages.props` + `<PackageReference>` (không version) vào csproj — dùng cách này để lấy version THẬT thay vì đoán.

### TK-038: CVE-2025-6965 (SQLite < 3.50.2) trong transitive của EF Sqlite — đã pin vá — 2026-07-04
- `Microsoft.EntityFrameworkCore.Sqlite` 10.0.9 kéo `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (native SQLite < 3.50.2) → GHSA-2m69-gcr7-jv3q (HIGH) → NU1903 (do TreatWarningsAsErrors + audit).
- Vá tận gốc: pin transitive `SQLitePCLRaw.lib.e_sqlite3` = **3.50.3** trong `Directory.Packages.props` (CentralPackageTransitivePinningEnabled đã bật). KHÔNG tắt audit. Chi tiết: DEC-051.
- Bài học: khi thêm package test/native, chạy `dotnet restore` kiểm NU1902/NU1903 ngay; ưu tiên pin version vá thay vì NoWarn.
- Note: SQLitePCLRaw đã đổi ID package (`SQLitePCLRaw.lib.e_sqlite3` → `SourceGear.sqlite3`) từ ~11/2025; hiện chỉ cần pin bản 3.50.3 là đủ vá.



### TK-041: Draft `LanguageCode` lưu TRIM-nguyên-văn (KHÔNG lowercase) — tránh CA1308; khớp resolve OrdinalIgnoreCase — 2026-07-05
- Bối cảnh: `UpdateRuleDraftUseCase` lưu `RuleSectionTranslation.LanguageCode = input.LanguageCode.Trim()` — CHỈ trim, KHÔNG `.ToLowerInvariant()`.
- Lý do (bản chất):
  1. **CA1308** (analyzer `latest-Recommended` + TreatWarningsAsErrors) cảnh báo "normalize to uppercase" — `ToLowerInvariant()` để chuẩn hóa sẽ vướng warning→error (đã "cắn" trước ở TranslationResolver, xem DEC-022 dùng OrdinalIgnoreCase thay ToLower).
  2. **Không cần lowercase để đúng:** editor admin gửi mã canonical khớp `ResortLanguage.Code` đang bật (cùng nguồn). Việc so khớp không phân biệt hoa/thường (fallback resolve) đã do `ITranslationResolver` xử lý bằng `OrdinalIgnoreCase` ở tầng ĐỌC (guest read slice D) — KHÔNG cần chuẩn hóa lúc GHI.
  3. `ux_tr_rule` (RuleSectionId, LanguageCode) unique: nếu editor gửi cả "en" và "EN" cho cùng section → là lỗi dữ liệu editor, sẽ bị unique index chặn (không âm thầm gộp). Chấp nhận cho MVP; nếu cần chuẩn hóa cứng thì làm ở resolver/validator bằng so khớp OrdinalIgnoreCase (không ToLower).
- Ràng buộc chéo: FULL-REPLACE draft (DEC-061) — mỗi PUT xóa hết section cũ rồi chèn mới, nên không có tình huống "trộn" mã ngôn ngữ giữa các lần lưu.
- Refs: DEC-061, DEC-022 (OrdinalIgnoreCase, tránh CA1308), `18` §3/§5.


### TK-042: Sub-slice D tìm thấy ở trạng thái DỞ DANG/lỗi biên dịch trên đĩa — fix tận gốc (không vá ngọn) — 2026-07-05
- Bối cảnh: khi tiếp tục sub-slice D, build FAIL (kiểm chứng bằng `dotnet build`, KHÔNG tin trạng thái "191 xanh" cũ). Nguyên nhân gốc (3 điểm dở dang từ phiên trước):
  1. `GetGuestRulesUseCase.cs` + `AcknowledgeRulesUseCase.cs` THIẾU `using ResortQr.Application.Common;` → không thấy `IUseCase<,>` (2 lỗi CS0246). Fix: thêm using (IUseCase nằm ở `ResortQr.Application.Common`).
  2. `GuestAccessEndpoints.ResolveAsync` dựng `ResolveResponse` thiếu 2 field mới `RulesVersion/RulesAcknowledged` (record đã mở rộng 11 field). Fix: truyền `value.RulesVersion, value.RulesAcknowledged`.
  3. `GetRulesAsync`/`AcknowledgeRulesAsync` được `MapGet/MapPost` nhưng CHƯA có method handler. Fix: hiện thực 2 handler (đọc cookie→rawSessionKey, visitId qua query/body).
  + `ResolveTokenUseCaseTests` cũ construct `ResolveTokenUseCase` thiếu tham số `IRuleAckStatusProvider` mới → fix truyền `RuleAckStatusProvider(uow)` thật (không mock).
- **Bài học kiểm chứng:** LUÔN chạy `dotnet build` khi tiếp tục phiên mới — trạng thái "xanh" ghi trong summary có thể KHÔNG khớp đĩa (file được thêm/sửa ngoài luồng). Đĩa là sự thật.
- **Bẫy seed test `ck_msg_len`:** `ResortSettings` có CHECK constraint `ck_msg_len` yêu cầu `MaxMessageLength > 0`. Seed test để mặc định 0 → `SqliteException 19: CHECK constraint failed: ck_msg_len` (9 test fail). Fix: đặt `MaxMessageLength = 2000` khi seed ResortSettings. (Các seed ResortSettings khác đã có 2000 — nhớ copy đủ.)
- Refs: DEC-063, DEV-026; `GuestAccessEndpoints.cs`, `GuestRulesTests.cs`.


### TK-043: `dotnet test ResortQr.slnx` (cả solution) có thể "Out of memory" trên máy thiếu RAM — chạy tách project — 2026-07-05
- Hiện tượng: chạy `dotnet test ResortQr.slnx` (toàn solution) đôi khi ném **"Out of memory"** ở testhost → 1 test bất kỳ (vd `ResortSeederTests.Seed_is_idempotent_when_run_twice`) FAIL GIẢ (không phải lỗi logic). Máy user cấu hình hạn chế (đã biết: thiếu disk; nay thêm áp lực RAM) — `dotnet test` chạy nhiều testhost (mỗi project 1 process) SONG SONG → cộng dồn RAM → OOM.
- **Bản chất:** đây là flake MÔI TRƯỜNG, KHÔNG phải regression. Kiểm chứng: chạy lại đơn luồng/tách project thì XANH.
- **Cách chạy an toàn (khi nghi OOM):**
  - Tách từng project: `dotnet test tests\ResortQr.UnitTests\...csproj`, `...ArchitectureTests...`, `...IntegrationTests...` (chạy tuần tự).
  - Hoặc giới hạn luồng cho integration: `dotnet test <csproj> -- xUnit.MaxParallelThreads=1 xUnit.ParallelizeTestCollections=false`.
- Kết quả xác nhận (2026-07-05): tách project → 76 unit + 5 arch + 124 integration + 1 skip = **205 xanh, 0 fail**. Con số test tổng nên đối chiếu bằng cách chạy tách nếu solution-run báo fail kèm OOM.
- Refs: DEC-064; TRD-009 (SQLite in-memory — mỗi test mở connection riêng, nhiều test song song tốn RAM).


### TK-044: Phục vụ web tĩnh ra LAN cho điện thoại (python http.server) + firewall — 2026-07-05
- **Chạy:** `python -m http.server 8080 --bind 0.0.0.0 --directory "<...>\resort-qr\web"` (background). Verify nội bộ: `Invoke-WebRequest http://127.0.0.1:8080/{,guest/,admin/}` = 200.
- **Điện thoại truy cập:** cùng WiFi/LAN → `http://192.168.120.102:8080/` (guest `/guest/`, admin `/admin/`). IP lấy bằng `Get-NetIPAddress -AddressFamily IPv4` (bỏ 127.*, 169.254.*, và vEthernet 172.x của Hyper-V/WSL — chọn card Ethernet/WiFi thật).
- **Firewall (bẫy hay gặp):** `New-NetFirewallRule ... -LocalPort 8080` cần **PowerShell elevated (admin)** — không có quyền → "Access is denied", điện thoại KHÔNG kết nối được. Cách mở (chạy trong PowerShell Admin):
  `New-NetFirewallRule -DisplayName "StarHill Web 8080" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 8080`
  (hoặc bấm "Allow access" ở popup Windows Defender lần đầu python bind).
- **nginx thay thế:** `scoop install nginx` rồi `nginx -p <web> -c nginx.conf` (đã có `web/nginx.conf`).
- Refs: DEC-066; `resort-qr/web/README.md`.


### TK-045: Model-drift ẩn — luôn chạy `has-pending-model-changes` khi wave thêm entity — 2026-07-05
- **Bẫy:** entity + EF config + DbSet có thể được thêm mà QUÊN sinh migration → model trôi khỏi snapshot. Test SQLite dùng `context.Database.EnsureCreated()` dựng schema TỪ MODEL (không từ migration) nên **vẫn xanh** → drift bị che. Deploy Postgres chạy `database update` (migration) → **thiếu bảng** → lỗi runtime.
- **Đã dính:** wave FAQ (entity/config/DbSet có, migration THIẾU — DEC-067). Phát hiện khi audit BE.
- **Quy tắc kiểm chứng (bắt buộc sau khi thêm/sửa entity mapping):**
  `dotnet ef migrations has-pending-model-changes --project src\ResortQr.Infrastructure\...csproj --startup-project ...` → phải "No changes". Nếu "Changes have been made" → sinh migration per-wave (`migrations add <Wave>_Init`) rồi verify lại.
- Refs: DEC-067; DEC-010/DEV-006 (migration per-wave additive); TRD-009 (SQLite EnsureCreated vs migration).


### TK-046: Rate-limit guest-write (per-session + per-hour) CHƯA hiện thực — hoãn cho task cross-cutting #18 — 2026-07-05
- **Trạng thái hạ tầng rate-limit hiện có:** global fixed-window theo IP (`ResortQrRateLimitExtensions`, mặc định rộng) + policy `auth`. CHƯA có policy `resolve` (per-IP sliding) và `guest-write` (per-`GuestSessionId`, fallback IP) như Req 13.1–13.3 / tasks #18.
- **Ảnh hưởng:** endpoint guest-write mới (housekeeping create — DEC-071; sau này messages) hiện chỉ được global IP limiter bảo vệ, CHƯA phân vùng theo session, và `ResortSettings.HousekeepingRateLimitPerHour`/`MessageRateLimitPerMinute` CHƯA được enforce.
- **Vì sao hoãn (fix gốc, không ngọn):** rate-limit theo session là cross-cutting, phải áp ĐỒNG BỘ cho MỌI guest-write ở pipeline (task #18) — nhét lẻ vào từng use case sẽ lệch/nhân bản. Chống spam tức thời cho housekeeping đã có bằng bất biến domain **1 ticket mở/phòng** (ux_hk_open). 
- **Khi làm #18:** thêm named policy `guest-write` (partition GuestSessionId từ IGuestContext middleware — DEV-019 P0-1 đã tách GuestCookieRead chạy trước UseRateLimiter) + đọc ngưỡng per-resort từ ResortSettings; áp `RequireRateLimiting("guest-write")` lên POST /api/guest/housekeeping (+ messages). Cân nhắc per-hour limit ở tầng app nếu cần chặt hơn.
- Refs: DEC-071; Req 13.1–13.5; tasks #18; DEV-019 (P0-1 pipeline order).


### TK-047: SQLite KHÔNG `ORDER BY` được `DateTimeOffset` — dùng OrderBy Id (UUIDv7) làm time-proxy — 2026-07-05
- **Bẫy:** EF query `.OrderByDescending(t => t.CreatedAt)` (CreatedAt là DateTimeOffset) dịch xuống SQLite → `NotSupportedException: SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses`. Postgres (Npgsql, timestamptz) thì ORDER BY được bình thường → lỗi CHỈ lộ ở test SQLite (đã dính ở EfHousekeepingQueries.ListAsync).
- **Fix gốc + provider-agnostic:** `.OrderByDescending(t => t.Id)` — Id là **UUIDv7** (DEC-003, sinh client-side time-ordered) nên sắp theo Id ≈ theo thời gian tạo; orderable trên cả SQLite (Guid lưu TEXT) lẫn Postgres (uuid). Bonus: tất định hơn khi clock test cố định (CreatedAt trùng T0 → tie; Id v7 vẫn phân biệt được theo thời gian construct thực).
- **Lưu ý phân biệt:** ordering trên IQueryable (EF→SQL) mới dính; ordering trên list ĐÃ materialize (`ListAsync` rồi `.OrderBy` in-memory = LINQ-to-Objects) thì DateTimeOffset OK (GetGuestHousekeepingUseCase làm vậy — không lỗi).
- **Khi cần ORDER BY thời gian thực trên SQLite:** hoặc order-by-Id (nếu UUIDv7), hoặc materialize rồi sort in-memory (mất paging DB), hoặc cấu hình value-converter DateTimeOffset cho SQLite harness (rộng, cân nhắc riêng).
- Refs: DEC-072/003; TRD-009 (SQLite test Docker-free), TK-036.


### TK-048: Req 13.5 (rate-limit ngưỡng ưu tiên ResortSettings) — phần ResortSettings HOÃN (chỉ appsettings+default) — 2026-07-05
- **Hiện trạng (DEC-074):** policy `resolve`/`guest-write` đọc ngưỡng từ `RateLimitOptions` (appsettings) + default. Req 13.5 nói ưu tiên `ResortSettings` TRƯỚC rồi appsettings.
- **Vì sao hoãn phần ResortSettings:** rate-limiter chạy ở pipeline (UseRateLimiter, trước auth/endpoint). Đọc `ResortSettings` (DB) trong partition factory = **DB read mỗi request** → anti-pattern (chậm, coupling). Snapshot lúc startup thì lỗi thời khi Admin đổi settings runtime (DEC-073). Cần một **cached settings provider** (vd IOptionsMonitor tự làm mới, hoặc memory-cache TTL ngắn, hoặc invalidate khi PUT settings) mới làm đúng mà không đánh DB mỗi request.
- **Khi làm:** thêm provider cache ResortSettings (single-resort → 1 dòng) refresh khi settings đổi; partition factory đọc từ cache (fallback appsettings→default). `MessageRateLimitPerMinute`/`HousekeepingRateLimitPerHour` (per-resort business quota) có thể enforce ở tầng use case thay vì pipeline nếu cần ngữ nghĩa "theo giờ".
- Refs: DEC-074/073; Req 13.5; DEC-040/042 (lazy config, không eager/DB trong pipeline).


### TK-049: Logger phải dùng `[LoggerMessage]` source-gen (CA1848 + TreatWarningsAsErrors) — 2026-07-05
- **Bẫy:** gọi `_logger.LogError(ex, "...", arg)` trực tiếp → CA1848 (performance) → build FAIL do TreatWarningsAsErrors.
- **Fix:** class `partial` + khai báo `[LoggerMessage(EventId=.., Level=.., Message="... {Arg}")] private partial void LogX(Exception ex, T arg);` (source generator dùng field `ILogger` sẵn có trong class). Gọi `LogX(ex, arg)`. Đã áp ở `VisitIdleSweeper` (EventId 1401/1402).
- Refs: DEC-076.


### TK-050: [RESOLVED 2026-07-06 — DEC-085] InternalNote.ConversationId nay có FK Conversation (Restrict) — NỢ ĐÃ TRẢ
> **RESOLVED (DEC-085, Messaging Slice A):** đã thêm `HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(Restrict)` vào `InternalNoteConfiguration`; migration `AddMessaging` sinh FK; NotesTests cập nhật seed conversation thật (không còn conversationId bịa). 286 test pass, `has-pending-model-changes`=No changes. Deployment mới chưa có note rác → migration an toàn (đúng như dự đoán bên dưới).
- **Hiện trạng (DEC-078):** `internal_note.conversation_id` là `uuid` nullable THUẦN (index `ix_note_conversation`), KHÔNG có `HasOne<Conversation>()` vì entity `Conversation` thuộc wave Messaging CHƯA xây. `RoomId` thì đã có FK Room (Restrict).
- **Việc phải làm khi xây wave Messaging:** thêm `builder.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict)` vào `InternalNoteConfiguration`, sinh migration bổ sung FK. Cân nhắc: note tồn tại có thể trỏ tới conversationId "mồ côi" (nếu đã tạo trước Messaging) → migration thêm FK có thể fail nếu có dữ liệu rác; ở deployment mới chưa có note thì an toàn.
- **Vì sao chấp nhận nợ:** không thể tham chiếu type chưa tồn tại; tạo cột trước giữ schema note ổn định. Đây là quyết định có chủ đích, KHÔNG phải thiếu sót.

### TK-051: `FixedCurrentUser.UserId` trong test harness là GET-ONLY — không đổi actor giữa các request cùng harness — 2026-07-06
- **Bẫy:** viết test `harness.CurrentUser.UserId = editor;` để mô phỏng "người khác sửa" → KHÔNG biên dịch (UserId chỉ có getter, gán trong ctor `FixedCurrentUser(Guid? userId)`). Một harness = một actor cố định (một connection SQLite in-memory).
- **Cách đúng:** nếu cần actor khác, hoặc (a) dùng cùng actor và chỉ verify UpdatedByUserId/UpdatedAt được set (đủ chứng minh audit interceptor chạy khi UPDATE), hoặc (b) mở rộng `FixedCurrentUser` cho settable (chưa cần — YAGNI). Đã áp cách (a) ở `NotesTests.Update_changes_body_and_sets_updated_fields`.


### TK-052: Bẫy timing — đọc `builder.Configuration` EAGER ở top-level Program KHÔNG thấy config test tiêm bởi WebApplicationFactory — 2026-07-06
- **Hiện tượng:** `var x = builder.Configuration.GetValue<bool?>("Key")` ở top-level Program (trước `builder.Build()`) → trong integration test (WebApplicationFactory + `WithWebHostBuilder(ConfigureAppConfiguration)`) trả **null** dù test đã set Key. Config test được áp lúc `builder.Build()`, SAU điểm đọc eager.
- **Vì sao Options (JWT...) vẫn "thấy" config test:** Options bind LAZY (lúc resolve/ValidateOnStart, sau build) → thấy đủ. Chỉ đọc EAGER mới dính.
- **Cách đúng:** với giá trị cần test/host override được, đọc SAU build qua `app.Configuration`/`app.Environment` (đã gồm mọi nguồn), hoặc bind qua Options. Đã áp ở OpenAPI: `AddResortQrOpenApi()` luôn đăng ký; `MapResortQrOpenApi()` đọc `app.Configuration` post-build để gate map (DEC-079).

### TK-053: Kỹ thuật VERIFY API surface của package bằng reflection (file-based `dotnet run`) — 2026-07-06
- **Bối cảnh:** cần chữ ký chính xác của `Microsoft.OpenApi` 2.7.5 (major 2.x refactor sang interface `IOpenApiSecurityScheme`...) trước khi viết transformer — "tuyệt đối không bịa".
- **Cách làm (tái dùng được):** file-based app `.cs` với directive `#:sdk Microsoft.NET.Sdk.Web` + `#:package X@ver`, dùng reflection (`Assembly.GetExportedTypes()` / `GetConstructors` / `GetProperties`) in ra chữ ký. **Lưu ý:** (a) Windows PowerShell 5 (.NET Framework) KHÔNG load được assembly net8.0 → phải dùng `dotnet run` (.NET 10). (b) đặt file trong thư mục con có `Directory.Build.props` rỗng + `ManagePackageVersionsCentrally=false` + tắt analyzer/TreatWarningsAsErrors để không dính CPM/analyzer của repo. (c) reflect assembly cần shared-framework (AspNetCore) thì thêm `#:sdk Microsoft.NET.Sdk.Web`. Dọn thư mục sau khi xong.

### TK-054: OpenAPI enrich `.Produces<T>()` per-endpoint — ✅ HOÀN TẤT — 2026-07-06
- **XONG toàn bộ:** Auth (DEC-080) + Guest (DEC-081) + Admin Rooms/QR (DEC-082) + Admin Rules/FAQ/Housekeeping/Notes/Settings/Dashboard (DEC-083). Mọi endpoint có success DTO + error responses trong `/openapi/v1.json`. 282 test xanh (5 test document-hóa OpenApi).
- **Helper:** `OpenApiConventions.ProducesProblems(params int[])` + `ProducesAdminAuthProblems()` (401/403/500).
- **Bài học (DEC-082):** `.Produces(status, contentType)` KHÔNG có responseType → document response THIẾU `content` (chỉ description). Muốn có content schema PHẢI khai kiểu: JSON→`.Produces<TDto>()`; nhị phân (png)→`.Produces<byte[]>(200,"image/png")`.
- **Giới hạn còn lại (không chặn FE type-gen success):** `.ProducesProblem` document ProblemDetails cơ bản, chưa mô tả extension `code`/`errors`. Cân nhắc schema tùy biến nếu FE cần map lỗi chi tiết theo `code`.
- **Cách khai status chính xác (DEC-080 §2):** success đọc endpoint; lỗi nghiệp vụ từ `*Errors`+`ErrorTypeToHttp`; pipeline theo authz(401/403)/rate-limit(429)/validator(400)/exception(500,409). CHỈ khai status có căn cứ (vd login/refresh không validator → không 400).
- **Giới hạn còn lại:** `.ProducesProblem` chưa mô tả extension `code`/`errors` của ProblemDetails trong schema (FE map theo `code`). Cân nhắc schema tùy biến sau — không chặn type-gen success DTO.
- **Không phải lỗi:** document vẫn hợp lệ; response body đang enrich dần theo nhóm FE tiêu thụ.


### TK-055: Máy hiện tại — chạy trực tiếp `ResortQr.Api.exe` bị "Access is denied" — 2026-07-06
- **Hiện tượng:** `dotnet run --project src\ResortQr.Api` → "An error occurred trying to start process '...\bin\Debug\net10.0\ResortQr.Api.exe' ... Access is denied." (nhiều khả năng antivirus/policy chặn thực thi exe trong thư mục user trên máy này — máy `k.nguyen.manh.toan`, khác máy cũ trong end.md).
- **Cách đã dùng để lấy ground-truth OpenAPI mà KHÔNG cần chạy exe:** viết test tạm dùng `WebApplicationFactory<Program>` (chạy in-process, KHÔNG spawn exe) fetch `/openapi/v1.json` rồi `File.WriteAllTextAsync` ra file trong workspace để soi; xóa test tạm + file sau khi xong (TK-053).
- **Hệ quả:** không chạy app trực tiếp để test thủ công bằng trình duyệt/curl trên máy này (trừ khi xử lý được quyền exe). Test tự động (WebApplicationFactory) KHÔNG bị ảnh hưởng vì host in-process. Môi trường: .NET SDK 10.0.301 cài user-scope `%LOCALAPPDATA%\Microsoft\dotnet` (đã thêm User PATH); mọi lệnh dotnet cần prepend PATH đó nếu shell chưa refresh.


### TK-056: Inbox hội thoại đang order theo `Id DESC` (creation-proxy), CHƯA phải recency thật — 2026-07-06
- **Trạng thái:** MVP chấp nhận (TRD-011). `EfConversationQueries.ListAsync` order `OrderByDescending(c=>c.Id)` vì SQLite không ORDER BY DateTimeOffset (TK-047).
- **Hệ quả cần biết:** một hội thoại cũ được reopen (staff reply / guest gửi lại) + có tin mới sẽ KHÔNG nổi lên đầu inbox nếu order toàn cục theo Id. FE hiện gom theo phòng + badge unread nên tác động UX thấp; nhưng nếu sau này FE có "danh sách hoạt động gần nhất" toàn cục thì đây là giới hạn.
- **Đường nâng cấp (có kiểm chứng, KHÔNG làm mù):** đổi `ListAsync` sang `OrderByDescending(c=>c.LastMessageAt).ThenByDescending(c=>c.Id)` + thêm index `(resort_id, room_id, last_message_at desc)` (design §2 đã gợi) + migration. PHẢI verify trên Postgres/Testcontainers (TK-036) vì SQLite ném khi ORDER BY DateTimeOffset (TK-047) — không thể verify Docker-free. `GetDetailAsync` (order message theo Id) KHÔNG cần đổi (message append-only, Id = thời gian thật).
- **Không phải bug:** đây là đánh đổi có chủ đích, ghi rõ để lần sau không "sửa nhầm" thành ORDER BY DateTimeOffset rồi vỡ test SQLite.

### TK-057: Messaging module (A/B/C) — HOÀN TẤT (còn vài defer nhỏ) — 2026-07-06
- **Trạng thái:** Slice A (schema/cascade, DEC-085) + B-guest (DEC-086) + B-admin (DEC-087) + **C1 notify port (DEC-088)** + **C2 SignalR hub transport (DEC-089)** — TẤT CẢ XONG, test xanh (324 pass + 1 skip). Realtime phát thật qua `SignalRRealtimeNotifier` (Replace Null).
- **Còn defer (nhỏ, không chặn):** notify guest-read (`GetGuestConversation`); notify lazy-expiry qua `PortalWindowGuard` (guest tự kích hoạt vốn đã bị reject); backplane đa-instance (TK-031, MVP single-instance). Xem TK-058.

### TK-058: Slice C2 (SignalR hub transport) — ✅ XONG (DEC-089); defer còn lại — 2026-07-06
- **XONG:** `ChatHub` `/hubs/chat` (OnConnectedAsync staff group qua AppUser.ResortId lookup; JoinConversation authorize tái dùng PortalWindowGuard; LeaveConversation); adapter `SignalRRealtimeNotifier` (Replace Null, nuốt lỗi); JWT query `access_token` cho `/hubs` (`OnMessageReceived` + `HubAccessToken` unit-test); EndVisit evict (VisitEnded + rejoin denied). Test 10 (harness/authorize/no-leak/evict/staff/query).
- **DEFER (nhỏ, ưu tiên thấp — làm khi có nhu cầu):**
  - ~~notify guest-read từ `GetGuestConversation`~~ — ✅ XONG (DEC-091 (a)): MessageRead(readBy=guest) post-commit khi đánh dấu tin.
  - notify lazy-expiry qua `PortalWindowGuard`/`ResolveToken` (evict connection lingering khi guest tự kích hoạt hết hạn) — guest đó vốn đã bị reject; chỉ ảnh hưởng connection song song hiếm.
  - **cascade-cancel/close notify khi EndVisit** (`HousekeepingUpdated(Cancelled)`/`ConversationUpdated(Closed)` tới board staff) — GIỮ DEFER có phân tích (DEC-091 (b)): cần đổi contract `IVisitEndHandler` (trả sự kiện) hoặc thêm event-collector post-commit; không tương xứng lợi ích (visit-end không hot-path). Fix tận gốc khi thành yêu cầu thật, KHÔNG chắp vá query chéo module.
  - ~~Housekeeping realtime (`HousekeepingUpdated`)~~ — ✅ XONG (DEC-090, Wave D): notify post-commit vào Create/ChangeStatus/Complete → group staff. CÒN defer nhỏ: cascade-cancel khi EndVisit chưa notify `HousekeepingUpdated(Cancelled)` (cần handler báo ngược ticket hủy về VisitEnder — phức tạp hơn giá trị; board cũ tới refresh kế).
  - Scale đa-instance: cần Redis backplane (TK-031) — MVP single-instance chưa cần.

### TK-059: Kỹ thuật test SignalR hub Docker-free (tái dùng cho hub tests tương lai) — 2026-07-06
- **Kết nối:** `HubConnectionBuilder().WithUrl(new Uri(factory.Server.BaseAddress, "hubs/chat"), o => { o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); o.Transports = HttpTransportType.LongPolling; })`. LongPolling qua TestServer handler (TestServer không phục vụ WS thật — TRD-012).
- **Auth guest:** cookie qua `o.Headers["Cookie"] = "shq_guest={rawKey}"` (raw key phải hash khớp `GuestSession.SessionKeyHash` bằng `Sha256GuestSessionKeyHasher`). **Auth staff:** `o.AccessTokenProvider = () => Task.FromResult(token)` (LongPolling gửi token qua header → JwtBearer validate). Token sub phải khớp AppUser seeded (dùng `factory.IssueToken(role, userId)`).
- **Enum wire:** client `.AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` để khớp server (enum-as-string) khi deserialize payload có enum.
- **RACE `StartAsync` vs `OnConnectedAsync`:** server-initiated group join (OnConnectedAsync) CHƯA xong khi StartAsync trả về → push ngay bị miss. **FENCE:** `await connection.InvokeAsync("LeaveConversation", Guid.Empty)` sau StartAsync — SignalR xử lý client-invocation SAU OnConnectedAsync (guarantee HubConnectionHandler) ⇒ đảm bảo join xong. (JoinConversation invocation cũng tự fence.)
- **Clock:** host chạy **system clock thật** (không MutableClock) → seed visit với `DateTimeOffset.UtcNow` (LastSeenAt/ExpiresAt) để guard portal-window không hết hạn tại thời điểm test chạy.
- **Assert KHÔNG nhận:** `Task.WhenAny(receivedTcs.Task, Task.Delay(3s))` rồi assert `completed != receivedTcs.Task` (no-leak/negative).

### TK-060: access_token (query WS) — VERIFY không bị log ở tầng app; residual proxy — 2026-07-06
- **Verify (đọc code):** `ResortQrObservabilityExtensions.UseSerilogRequestLogging` dùng template `{RequestPathMasked}` = `PathMasker.Mask(httpContext.Request.Path.Value)` — chỉ **PATH**, KHÔNG QueryString. Serilog built-in `RequestPath` cũng là PathString (không query). ⇒ `access_token` ở query của `/hubs?access_token=...` KHÔNG bị ghi log ở tầng app.
- **Residual (infra, không phải code app):** reverse proxy production (Nginx/Caddy) access-log mặc định thường log full URL gồm query → nên cấu hình KHÔNG log query cho path `/hubs*` (hoặc tắt access-log path đó). Ghi vào hướng dẫn deploy (`20`).
- **KHÔNG "fix cái ngọn":** không thêm masking query ở app vì app vốn không log query (verify) — thêm sẽ là code thừa. Chỉ canh chừng nếu ai đổi template log để thêm query.


### TK-061: Tooling Frontend (Node/pnpm) trên máy này — 2026-07-06
- **Node KHÔNG trên PATH.** nvm-windows có 2 bản tại `C:\Users\toann\AppData\Local\nvm\`: **v18.20.8** (EOL) và **v25.2.1**. Lệnh `nvm` cũng không trên PATH.
- **Dùng Node 25.2.1 cho FE:** mọi lệnh FE phải prepend PATH: `$env:PATH = "C:\Users\toann\AppData\Local\nvm\v25.2.1;" + $env:PATH;` → `node`=v25.2.1, `npm`=11.6.2. (Giống pattern .NET user-scope ở TK-055.)
- **npm registry OK** (`npm ping` → PONG ~315ms) → cài package được.
- **pnpm:** shim cũ `~/AppData/Roaming/npm/pnpm.ps1` (gọi node — hỏng khi node không trên PATH). Kích hoạt pnpm qua **corepack** (bundle sẵn Node) khi cần: `corepack enable pnpm` / `corepack prepare pnpm@latest --activate`. (Verify khi scaffold monorepo.)
- **Node 25 là Current (non-LTS)** — chỉ là toolchain build (deploy phục vụ static qua proxy, KHÔNG chạy Node runtime — DEC-025). Production/CI pin Node LTS (24) theo DEC-015; `.nvmrc` sẽ pin bản chuẩn. Node 18.20.8 EOL → KHÔNG dùng (Vite 7/8 cần ≥20.19/22.12).
- **KHÔNG chạy được FE build/verify nếu quên prepend PATH** → mọi lệnh FE trong session phải kèm prefix trên (như .NET).


### TK-062: FE-1 (admin-web skeleton) ĐÃ BUILD XANH; bundle Element Plus cần tối ưu on-demand — 2026-07-06
- **XONG (verify):** `resort-qr/frontend/` monorepo (pnpm workspace + tsconfig.base strict + .nvmrc 24 + .npmrc) + `apps/admin-web` (Vite 8.1.3 + Vue 3.5.39 + TS 6.0.3 + vue-tsc 3.3.6 + Vue Router 5.1 + Pinia 3 + Element Plus 2.14). Layout Vuexy-inspired (sidebar dọc + navbar + content), Login + Dashboard skeleton. `pnpm --filter admin-web build` = `vue-tsc --noEmit` (typecheck strict PASS) + `vite build` → `✓ built` (exit 0). Version LẤY THẬT qua `pnpm add` (không bịa).
- **Bẫy TS 6:** `baseUrl` deprecated (TS5101 → lỗi vì strict). Fix: bỏ `baseUrl`, `paths` dùng `"@/*": ["./src/*"]` (TS 5+ resolve paths tương đối tsconfig, không cần baseUrl). Alias runtime do Vite (`fileURLToPath`).
- **Cần tối ưu (không phải bug — ghi để làm sau):** import FULL Element Plus (`app.use(ElementPlus)` + `element-plus/dist/index.css`) → chunk index ~998kB (gzip ~324kB) + cảnh báo >500kB. Fix đúng: **on-demand import** (`unplugin-vue-components` + `unplugin-auto-import` với `ElementPlusResolver`) → chỉ bundle component dùng. Làm ở increment tối ưu (admin chấp nhận nặng hơn guest, nhưng vẫn nên gọn cho thương mại).
- **Cảnh báo vô hại:** Rolldown `[INVALID_ANNOTATION]` `/* #__PURE__ */` trong `@vueuse/core` (transitive của Element Plus) — không chặn build (đã `✓ built`).
- **Chạy lệnh FE:** luôn prepend `C:\Users\toann\AppData\Local\nvm\v25.2.1` vào PATH + `COREPACK_ENABLE_DOWNLOAD_PROMPT=0` (TK-061). `dist/`, `node_modules/` đã .gitignore.
- **Kế tiếp:** FE-2 packages (shared-types sinh từ OpenAPI `/openapi/v1.json` + api-client + realtime); FE-3 guest-web skeleton; on-demand Element Plus; ESLint flat + vitest theo design `05` DoD.