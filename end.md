# HANDOFF — đọc file này để tiếp tục chính xác (cập nhật 2026-07-28)

Ngôn ngữ: trả lời **Tiếng Việt**. Tuân thủ steering `.kiro/steering/thinking-and-answering.md` (kết luận trước, first-principles, đo bằng bằng chứng — KHÔNG bịa, fix tận gốc không fix ngọn, nêu trade-off).

## 0. Nguồn sự thật
- **Journal** `.kiro/specs/starhill-qr/journal/` = nguồn sự thật (01-decisions QR-AD, 04-notes QR-N, 05-anti-drift). end.md chỉ là bản giao việc; lệch nhau → tin journal.
- Spec đang làm: **`.kiro/specs/military-grade-hardening/`** (requirements.md + design.md + tasks.md — đủ 3, đã qua format check). 9 requirement (R1..R9), thứ tự triển khai chốt trong tasks.md.
- Anti-drift keystone: `StarHillJournalConsistencyTests` INV-1..6 (mọi QR-AD "Implemented" phải có `- Guard-Tests:` trỏ class tồn tại thật). ĐỪNG thêm QR-AD Implemented khi guard class chưa tồn tại.

## 1. ĐÃ LÀM (commit + push develop, sync 0/0)
Chuỗi commit gần nhất trên `develop`:
- `docs(hardening)` — tạo design.md + tasks.md.
- `feat(hardening) task 1` — **QR-AD-059** Health probe: `HealthProbe.cs` + nhánh `--healthcheck` đầu `Program.cs` (top-level cần `return 0;` cuối — CS0161); Dockerfile HEALTHCHECK 4 tham số + ghim digest 2 FROM; compose host healthcheck. Guard `DockerfileHardeningGuardTests`. Verify container thật: healthy 5s, unhealthy ≤40s.
- `fix(migrations)` — **QR-AD-060** BUG có sẵn: base thêm feature outbox-replay-audit nhưng 3 module (Identity/GuestAccess/Housekeeping) thiếu migration → compose crash `PendingModelChangesWarning`. Đã sinh migration `AddOutboxReplayAudit` cho 3 module + guard `PendingModelChangesGuardTests` (Docker-free, 8 context). has-pending 8/8 sạch; compose boot healthy.
- `docs(journal)` — QR-AD-059/060 + QR-N-090 + đồng bộ design C9.1.
- `feat(hardening) task 2` — JSON `/health/ready` writer ở base (`platform/src/Bedrock.Api/Health/HealthEndpoints.cs`) liệt kê check thất bại (R1.6/R1.8). `Bedrock.Api.Tests` 62/62.
- `docs(hardening)` — design C2 revise (DD-11 kiến trúc fuzz).
- `feat(hardening) task 3 (một phần)` — **Fuzz harness** `starhill/tests/Host/StarHill.Api.Tests/FuzzBoundaryTests.cs` + **fix R2.5 ở base** (`BedrockApiExtensions.AddBedrockApi`: `AddProblemDetails()` + `ThrowOnBadRequest=true`). Fuzz PASS 1/1.

**Full BE suite starhill = 421/421** (chạy trước task 2/3). Full suite base có 2 fail môi trường (xem F4).

## 2. ĐANG DỞ / LÀM TIẾP NGAY (task 3 chưa xong 100%)
Fuzz hiện phủ **5 module** (Rooms admin; Rules/Faq/Housekeeping/Concierge admin+guest) ~25 endpoint. **CÒN THIẾU để đủ R2.1**: `POST /v1/identity/token/login` + `/token/refresh`, ResortConfig `/v1/resort/settings`, Dashboard `/v1/dashboard/stats`, `POST /v1/guest/resolve`. → Bổ sung các endpoint này vào `Endpoints()` + đăng ký fake tương ứng (đọc `Authorization/ResortConfigEndpointAuthTests.cs`, `DashboardEndpointTests.cs`, `GuestAccessResolveEndpointTests.cs`, Identity auth test để lấy fake + cách map).
- **Kiến trúc fuzz (DD-11, đã chốt trong design C2 REVISED):** đặt TRONG `StarHill.Api.Tests`, dùng **pipeline THẬT** `AddBedrockApi(JwtTestTokens.BuildConfig())` + `app.UseBedrockApi()` + đăng ký endpoint module qua `IEndpointModule` + fake use case + `FakeCurrentGuestContextResolver` success. KHÔNG full-Host+DB, KHÔNG project riêng.
- **Journal:** cần thêm **QR-AD-061** (fuzz + R2.5 problem+json fix, Status Implemented, `- Guard-Tests: FuzzBoundaryTests`) vào 01-decisions.md + token vào 05-anti-drift.md. INV-1 liên tục → số kế tiếp = 061; QR-N kế = 091.
- **task 3.5** (wire CI): fuzz nằm trong `StarHill.Api.Tests` → đã chạy trong job `build-test` của `dotnet test Platform.slnx`. Coi như đã wired; chỉ cần xác nhận.

## 3. RỦI RO / CHƯA VERIFY (quan trọng)
- **Base full suite CHƯA chạy lại** sau khi thêm `ThrowOnBadRequest=true` + `AddProblemDetails()` vào `AddBedrockApi` (user yêu cầu bỏ test cho nhanh). Đây là thay đổi BASE ảnh hưởng mọi consumer → phiên sau **PHẢI** chạy `cd platform; dotnet test Platform.slnx -c Release` xác nhận không vỡ (đặc biệt các test assert lỗi 400/415/binding). Nếu vỡ = hồi quy do thay đổi này.
- **F4** (đã ghi QR-N-090): 2 test base `RabbitMqResilienceTests` (`Broker_restart...`, `Broker_pause_partition...`) TIMEOUT (`TaskCanceledException`, ngân sách 90s/120s) trên Docker Desktop Windows — **môi trường, KHÔNG phải regression** (dùng `HealthCheckService` trực tiếp, không đụng thay đổi health/fuzz). Gần như xanh trên base CI Linux. Xác nhận qua base CI; đừng chỉnh timeout base mù.
- **F3 DataProtection** (QR-N-090): base KHÔNG cấu hình `AddDataProtection/PersistKeysTo` → key ephemeral trong container, không mã hoá. Guest session KHÔNG bị ảnh hưởng (token opaque đối chiếu DB). Tác động THẤP ở 1-instance, MAJOR nếu đa-instance. Đề xuất thành requirement hardening riêng — chờ user duyệt.

## 4. CÁC TASK CÒN LẠI (tasks.md, thứ tự): 
task 3 (hoàn tất coverage) → 4 supply-chain (SBOM+Trivy+digest guard) → 5 observability prod (5xx counter, exporter-status SPIKE, bounded buffer, structured log, alert-thresholds.json) → 6 hạ tầng test nặng + trait guard → 7 chaos → 8 latency p99 → 9 soak → 10 CI_Nightly + validate_ci → 11 anti-drift journal cuối. Wave graph trong tasks.md. **task 5.2 (exporter connected/disconnected) là SPIKE** — API OTel 1.16 chưa chắc có seam; có đường lùi (log+counter) ghi trong design C8.2.

## 5. BẪY ĐÃ BIẾT (đừng lặp lại)
- **EF `migrations add/remove` TUYỆT ĐỐI KHÔNG `--no-build`** → assembly cũ khiến remove xoá nhầm migration + add sinh rỗng. Đã dính, phải `git checkout` reset. `has-pending-model-changes --no-build` cũng đọc assembly cũ → rebuild trước khi tin.
- `ARCHITECTURE-REVIEW-2026-07-26.md` ở repo-root = review DỰ ÁN KHÁC (vision-platform, Python) — untracked, ĐỪNG triển khai CD/S/A items (QR-N-089).
- `StarHillGuestApp/` (nested, repo-root) = cây stale, untracked — BỎ QUA, đừng commit.
- `.gitignore`: `starhill/.gitignore` có `[Bb]uild/` → tránh tên thư mục `build/ dist/ obj/ bin/ out/`; luôn `git check-ignore -v <path>`.
- Docker Desktop hay tắt → `Start-Process "$env:ProgramFiles\Docker\Docker\Docker Desktop.exe"` rồi poll `docker version`. Container broker stop/start trên Windows CHẬM (gây F4).
- Terminal PowerShell hay nuốt/wrap output → ghi ra file trong workspace rồi `read_file` (temp ngoài workspace KHÔNG đọc được).
- Commit protocol: stage path TƯỜNG MINH; kiểm `git diff --cached --name-only | Select-String "/bin/|/obj/|node_modules|/dist/"` RỖNG; `git -c core.autocrlf=false commit`; verify `git rev-list --count --left-right origin/develop...HEAD` = `0 0`. Push develop (nhánh tích hợp, KHÔNG main/master).

## 6. LỆNH VERIFY BASELINE
- BE 0-warning + arch/guard (Docker-free phần lớn): `cd starhill; dotnet build Platform.slnx -c Release` + `dotnet test tests/StarHill.ArchitectureTests/StarHill.ArchitectureTests.csproj -c Release --no-build`.
- Full BE (cần Docker): `cd starhill; dotnet test Platform.slnx -c Release` (kỳ vọng 421/421 + fuzz).
- Full base (cần Docker, PHẢI chạy sau thay đổi base task 2/3): `cd platform; dotnet test Platform.slnx -c Release` (2 test RabbitMqResilience có thể timeout — F4 môi trường).
- Fuzz riêng: `cd starhill; dotnet test tests/Host/StarHill.Api.Tests/StarHill.Api.Tests.csproj -c Release --filter "FullyQualifiedName~FuzzBoundaryTests"`.
