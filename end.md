# HANDOFF — StarHill QR (cập nhật 2026-07-27)

> File này CHỈ là bản giao việc ngắn. **Nguồn sự thật đầy đủ = journal** `.kiro/specs/starhill-qr/journal/`
> (01-decisions / 02-deviations / 03-tradeoffs / 04-notes / 05-anti-drift). Nếu file này lệch journal → TIN JOURNAL.

## 1. Vị trí hiện tại

- Nhánh `develop`, HEAD = commit cuối của phiên này (xem `git log -1`), sync `0/0`.
- **BE xong 8/8 module** (Identity, ResortConfig, Rooms, GuestAccess, Rules, Faq, Housekeeping, Concierge)
  + cascade event-driven `GuestVisitEnded` (outbox/inbox at-least-once, đóng CP9) + SignalR hub `/hubs/chat`.
- **FE xong hành trình khách 4 capability thật** (rules force-read, faq, chat polling ~4s, housekeeping polling ~5s)
  + admin (login, dashboard, rooms, rules, faq). Route thật: `/r/:token` → `HomeShell` + `JourneyCore`; mockup ở `/demo`.
- Journal: `QR-AD-058`, `QR-DV-008`, `QR-TO-020`, `QR-N-089`.

## 2. Baseline XANH (đã đo phiên này)

| Cổng | Kết quả |
|---|---|
| `dotnet build starhill/Platform.slnx -c Release` | 0 warning / 0 error |
| `StarHill.ArchitectureTests` | **40/40** (gồm JournalConsistency INV-1..6) |
| `pnpm build` (2 SPA, có `vue-tsc --noEmit`) | EXIT=0 |
| `pnpm e2e` (Playwright) | **65/65** |

Full BE suite (Testcontainers) **chưa chạy lại** ở phiên này — cần Docker bật; lần chạy trước đó 0-fail/0-skip.

## 3. Việc phiên này đã làm

1. **QR-N-088** — fix gốc: refactor FE.5a bỏ dở để lại gateway trùng `guest-web/src/api/guestApi.ts`
   (hàm chết `resolveGuestToken` + class `GuestApiError` THỨ HAI → bẫy `instanceof` im lặng). Đã xoá module,
   dồn type về `core/apiGateway`, ghim guard vào seam thật, thêm guard "một gateway / một error type".
2. **QR-AD-058 / QR-N-089** — đo được lỗ thật: `VITE_STARHILL_MOCK=1 pnpm build` từng **nhúng dữ liệu bịa vào bundle guest**
   (Vite đọc cả `process.env VITE_*`). Fix fail-closed: `starhill/web/tooling/assertMockNotBundled.ts` → mọi `vite build`
   có cờ mock **FAIL cứng**; `serve`/`--mode mock` không ảnh hưởng. Guard `FrontendDeliveryGuardTests`.
3. **Spec mới `.kiro/specs/military-grade-hardening/requirements.md`** — 9 requirement EARS, ngưỡng đã chốt (xem §4).

## 4. Spec đang mở: `military-grade-hardening` (Phase = Requirements XONG, chờ Design)

Thứ tự ưu tiên đã chốt: **health probe → fuzz → chuỗi cung ứng → phân tầng CI → chaos → SLO → soak → observability → journal guard**.

Ngưỡng đã chốt (không hỏi lại):
- Health probe: `HEALTHCHECK` interval 10s / timeout 3s / retries 3 / start-period 20s; unhealthy ≤40s;
  **KHÔNG cài package OS vào image runtime**; `/health/ready` = 503 ngay khi mất DB (không ân hạn).
- Fuzz: ≥500 payload/endpoint, mọi biến dạng → 4xx `application/problem+json`, **không 5xx**, không rò stack/secret;
  endpoint admin phải kèm JWT role đúng (401 = FAIL bài fuzz).
- Chuỗi cung ứng: SBOM CycloneDX (BE + web), chặn build khi có `High`/`Critical`, **pin digest `@sha256:`** cả 2 image base.
- CI: PR nhanh (≤30 phút/job, chạy fuzz + supply-chain + guard) — **nightly riêng** (≤90 phút/job) chạy chaos/latency/soak.
- SLO: 5 endpoint khách (`resolve`, `rules`, `conversation`, `housekeeping`, `messages`), **p50 ≤ 80ms, p99 ≤ 400ms**.
- Soak: **60 phiên đồng thời (biên vật lý 60 phòng) = 30 rps**, 30 phút (~54.000 request); PASS = heap ≤ +10%,
  handle không tăng đơn điệu, connection không tăng, 5xx = 0, outbox tồn ≤ 10; fail nếu generator không đạt 30 rps ±10%.
- Observability: OTel/OTLP **fail-open** (telemetry không được giết dịch vụ khách) + bounded buffer + drop-not-block
  + chỉ báo exporter `connected`/`disconnected` + 4 ngưỡng cảnh báo; readiness KHÔNG phụ thuộc exporter.
- Exactly-once đo bằng **số dòng tác dụng phụ = 1** (không tin cờ nội bộ).

**NEXT trên máy mới:** sinh `design.md` cho spec này (Requirements-first), rồi `tasks.md`, rồi triển khai theo thứ tự ưu tiên.

## 5. Bẫy đã biết — đọc trước khi làm

- `ARCHITECTURE-REVIEW-2026-07-26.md` ở repo-root **KHÔNG phải review của dự án này** — nó review `vision-platform/`
  (Python, SHM ring, RTSP/ONNX/torch/ZMQ). Grep định danh trong repo = 0 match. **ĐỪNG triển khai CD-1..9 / S-1..7 / A-1..6.**
  File để untracked; chuyển ra khỏi repo nếu muốn dọn.
- **`.gitignore` bẫy**: `starhill/.gitignore` có `[Bb]uild/` → file mới trong thư mục tên `build/` sẽ KHÔNG vào repo
  (local xanh, CI vỡ). Luôn `git check-ignore -v <path>` khi tạo file ở thư mục mới; tránh tên `build/ dist/ obj/ bin/ out/`.
- **Playwright**: nếu `pnpm e2e` fail hàng loạt kiểu `Executable doesn't exist ... chromium_headless_shell`,
  đó là **thiếu binary**, không phải lỗi code → `pnpm --filter @starhill/e2e exec playwright install chromium`.
- **Migration EF**: đừng dùng `--no-build` khi model vừa đổi (sinh migration RỖNG). Commit ngay slice có migration.
- **Journal INV-4**: nhãn bắt buộc phải đúng nguyên văn `- Provenance/Evidence:` (thêm chú thích trong ngoặc trước dấu `:` = FAIL build).
- PowerShell 7: khi probe HTTP, Content-Type nằm ở `$_.Exception.Response.Content.Headers`, body ở `$_.ErrorDetails.Message`.
  Đọc BODY/`code` rồi mới kết luận, đừng nhìn status trần.

## 6. Lệnh hay dùng

```powershell
# BE
cd starhill; dotnet build Platform.slnx -c Release
dotnet test tests/StarHill.ArchitectureTests/StarHill.ArchitectureTests.csproj -c Release --no-build
dotnet test Platform.slnx -c Release --no-build          # cần Docker cho Testcontainers

# FE
cd starhill/web; pnpm build; pnpm e2e
pnpm dev:guest   # 5173   |   pnpm dev:admin   # 5174   |   *:mock để xem UI không cần backend

# Runtime thật (browser-substitute)
cd starhill; docker compose -f docker-compose.yml up -d --build   # http://localhost:18080
# overlay messaging (RabbitMQ, cần cho cascade/outbox):
# docker compose -f docker-compose.yml -f docker-compose.messaging.yml up -d --build
```

Dev admin (compose): `admin` / `DevAdmin!2026`. Secret JWT dev nằm trong `docker-compose.yml` (placeholder, không phải prod).

## 7. Quy tắc làm việc (giữ nguyên)

- Trả lời **Tiếng Việt**. Design-first: đọc/valid nhiều lần, kiểm chứng được rồi mới code. **Không bịa, không suy đoán.**
- Fix **tận gốc**, không vá ngọn. Mọi quyết định mới → journal (QR-AD/DV/TO/N) + **guard test** (INV-6) + cập nhật `05-anti-drift.md`.
- Docker sẵn thì chạy Testcontainers thật; **không lấy test SKIP làm bằng chứng**.
- Commit: stage path **tường minh**, kiểm `git diff --cached --name-only` không lẫn `bin/ obj/ node_modules/ dist/`,
  dùng `git -c core.autocrlf=false commit`, rồi verify `git rev-list --count --left-right origin/develop...HEAD` = `0 0`.
