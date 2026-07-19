# Current audit — Bedrock platform base (2026-07-18)

> Phạm vi: `platform/src`, `platform/tests`, `platform/tools`, `.github/workflows/ci.yml`, spec/journal hiện hành và cách `starhill/` tiêu thụ base. Mục tiêu là đánh giá code hiện tại, không mặc định tin README/ledger cũ.

## 1. Kết luận điều hành

`platform/src` là base Bedrock duy nhất và đã ở mức kiến trúc mạnh: dependency direction rõ, persistence theo module, transaction/domain-event đúng hướng, outbox/inbox + retry/DLQ có guard, security/API/observability có cơ chế dùng lại, Testcontainers và journal consistency tạo lưới chống drift rộng. Đây không còn là foundation/skeleton; StarHill đang dùng base này đúng mô hình một nguồn.

Kết luận thực dụng:

- **Dùng được ngay cho StarHill 60–100 phòng**; không cần quay lại hoặc revive `foundation/`.
- **Đủ nền cho sản phẩm mới**, nhưng chưa nên gọi là “platform hoàn tất tuyệt đối”. Phần còn thiếu chủ yếu là productization/operability/supply-chain, không phải lỗi layering cơ bản.
- Finding nghiêm trọng nhất của phiên này không nằm trong domain code mà ở **cổng kiểm chứng xanh giả**. AD-104 đã đóng tận gốc: thiếu toolchain/build fail phải đỏ; test không được chạy trên artifact cũ.
- SDK portable .NET 10.0.301 đã được dùng để restore/build/test source hiện tại; AD-104 và toàn bộ architecture/journal guard compile/run local thành công. Test Docker-backed vẫn ghi đúng là skip do daemon không khả dụng, không giả làm bằng chứng runtime broker/database.

## 2. Nguồn sự thật và cây legacy

| Cây | Kết luận |
|---|---|
| `platform/src` | Base vật lý duy nhất, có hiệu lực. |
| `starhill/` | Product tree hiện hành, reference base qua `$(PlatformSrc)`. |
| `foundation/` | Bản cũ + rationale. Không phải base thứ hai; không được sửa tiếp. |
| `resort-qr/` | Product legacy để đọc/port có chọn lọc. Không build/deploy như StarHill hiện hành. |

Khuyến nghị với `foundation/`: **chưa xoá trong lượt này**. Đầu tiên giữ nhãn archival ở root README; khi muốn dọn, làm một quyết định riêng gồm `rg` toàn repo, di chuyển rationale còn độc quyền vào spec và xác nhận CI/solution không reference rồi mới xoá recoverable/commit riêng.

> **CẬP NHẬT 2026-07-18 (SUPERSEDED):** quyết định riêng đã thực hiện — `foundation/` + `resort-qr/` ĐÃ XOÁ theo đúng điều kiện trên (grep toàn repo xác nhận không project/CI/solution reference; rationale giữ ở `.kiro/specs/` + `docs/`; xoá recoverable qua git history + commit riêng). Chi tiết: `starhill-qr` journal QR-N-080. Repo nay một-phiên-bản: `platform/` + `starhill/`.

## 3. Bằng chứng phiên này

| Gate | Kết quả | Ý nghĩa |
|---|---:|---|
| `python platform/tests/validate_ci.py` | PASS | Validator base self-contained, không cần PyYAML. |
| `python starhill/tests/validate_ci.py` | PASS | Workflow product có đủ job backend/frontend/docker/migration. |
| `platform\scripts\vp.cmd all` khi thiếu dotnet | FAIL/127 + test BLOCKED | Fail-closed đúng; không còn xanh giả/artifact cũ. |
| `starhill\scripts\vp.cmd all` khi thiếu dotnet | FAIL/127 + test BLOCKED | Mirror product đúng hành vi base. |
| Production build cả hai SPA | PASS | Vue typecheck + Vite build sạch. |
| Playwright StarHill | 42/42 PASS | Rules/FAQ mutation + responsive + visual QA desktop/phone. |
| Platform C# build/test | PASS | Release build 0 warning/error; full suite 0 failure; Docker-backed tests skip mềm khi daemon unavailable. |
| StarHill C# build/test | PASS | Release build 0 warning/error; full suite 0 failure; stale-edit/read-model/auth/architecture tests pass. |
| `verify.ps1 all` + `journal` (cả hai tree) | PASS | Command governance fail-closed chạy end-to-end; AD-104 guard compile/run local. |

Hồ sơ trước đó ghi nhận full Docker suite 401 pass/0 skip ở một baseline 2026-07-12. Đó là bằng chứng lịch sử hữu ích, **không thay thế** lần chạy CI cho diff hiện tại.

## 4. Đánh giá theo trục chuyên môn

| Trục | Mức hiện tại | Nhận định |
|---|---|---|
| Layering / dependency direction | **A** | Bedrock → adapter/module/Host rõ; guard project graph + negative controls mạnh. |
| Modular persistence / transaction | **A-** | Keyed UoW, bijection context-registration, schema/migration history, domain-event restore full window. |
| Messaging correctness | **A-** | Outbox lease/renew/finalize, inbox atomic, retry transient/permanent, confirms, DLQ, trace context; còn broker-restart/soak proof và replay tooling. |
| Security / HTTP | **A-** | JWT/key-ring validation, trusted forwarded headers, rate limit, ProblemDetails, bad-request 400, redaction; lifecycle/consumer-specific policies vẫn thuộc app. |
| Anti-drift / testing | **A-** sau AD-104 | Suite rộng + journal invariants; gate nay fail-closed. Deep assembly discovery vẫn còn hardcode một phần. |
| Operability | **B** | Có health/OTel/metrics/last_error; thiếu SLO/runbook, oldest-pending và replay/DLQ operator flow. |
| Supply chain / reproducibility | **B-** | SDK/package versions pin tập trung và audit cảnh báo; chưa full lock/action SHA/image digest/SBOM/provenance. |
| Platform productization | **B** | Base dùng được bằng source reference; chưa có public API baseline/ApiCompat, package contract hay module scaffolder chuẩn. |

## 5. Điểm mạnh đã xác nhận từ code/journal hiện hành

1. **Base domain-agnostic thật**: port ở lõi, adapter sở hữu công nghệ, module sở hữu business, Host composition; StarHill không copy Bedrock.
2. **Persistence nhiều module có fail-fast**: key bắt buộc ở command write, context↔registration bijection, health-check name theo context, migration ledger theo schema.
3. **Atomicity có chiều sâu**: domain event dispatch nằm trong SaveChanges window và được restore nếu bất kỳ bước trước commit thất bại; outbox writer cùng transaction.
4. **Messaging không còn happy-path-only**: exclusive lease, renewal, owner-guard finalize, retry delay/limit, permanent quarantine, publisher confirm/mandatory, inbox dedupe, traceparent+tracestate chuẩn.
5. **Security defaults có bằng chứng hành vi**: forwarded header chỉ tin proxy khai báo, generic auth error/timing defense, key-ring validate-on-start, secret redaction, malformed request không thành 500.
6. **Contract/drift governance mạnh**: CP/AD guard, journal INV, error-code snapshots, schema snapshots, project discovery và solution membership.
7. **Delivery artifacts đúng hướng**: CI build/test, Docker image, per-module migration bundles; StarHill có browser gate riêng.
8. **Thiết kế không over-engineer StarHill**: RabbitMQ overlay opt-in, same-origin frontend, một DB nhiều schema, module monolith — phù hợp quy mô nhỏ nhưng không khóa đường nâng cấp.

## 6. Finding đã đóng trong phiên — P0 Verification truthfulness

### Root cause

`verify.ps1` cũ để `$ErrorActionPreference='Continue'`, gọi native command trực tiếp và tin `$LASTEXITCODE`. Khi `dotnet` không tồn tại, PowerShell phát command-not-found nhưng exit code step có thể vẫn là 0. Scope `all` tiếp tục `dotnet test --no-build`, có nguy cơ dùng artifact của build cũ. `validate_ci.py` lại yêu cầu PyYAML chưa bootstrap và có thể vỡ encoding trên Windows.

### Fix

- Resolve native command trước khi gọi; thiếu command → 127.
- Bắt exception ở biên mỗi step; tổng kết dựa trên code đã capture.
- Build fail → test `--no-build` ghi BLOCKED/125, không thực thi.
- Python resolution hỗ trợ `python` hoặc `py -3`.
- Validator có parser stdlib cho subset contract, UTF-8 output; PyYAML chỉ là optional path.
- CI gọi validator thật.
- `VerificationGateTests` khóa source entrypoints để thay đổi sau không làm gate mềm lại.

### Vì sao đây là P0

Mọi correctness/architecture/security test đều vô nghĩa nếu launcher có thể báo xanh mà chưa chạy compiler. Đây là lỗi ở tầng “sự thật của bằng chứng”, nên ưu tiên cao hơn thêm feature hoặc thêm test mới.

## 7. Khoảng trống chiến lược còn thật

### P1 — Operability và recovery

- Thêm metric/gauge **oldest pending age**, queue/DLQ depth và alert threshold có SLO.
- Thiết kế replay tool an toàn: authorization, audit actor/reason, dry-run, idempotency, replay-by-message-id/type/time window.
- Viết runbook: DB/broker outage, poison message, migration rollback/roll-forward, key rotation, consumer unhealthy.
- Acceptance: integration test replay không double-effect; dashboard/metric test; runbook drill có log bằng chứng.

### P1 — Supply-chain reproducibility

- NuGet locked mode + lockfile phù hợp repo; pin GitHub Actions theo commit SHA; Docker base image theo digest.
- Sinh SBOM (CycloneDX/SPDX), vulnerability scan và artifact provenance; định nghĩa policy severity/exception.
- Acceptance: clone sạch/offline-cache build reproducible; CI fail khi lock drift hoặc image/action không pin theo policy.

### P1 — Platform public surface

- Xác định assembly/type nào là supported API; thêm `PublicAPI.Shipped/Unshipped` hoặc ApiCompat.
- Tách “source-internal convenience” khỏi contract dành cho sản phẩm; versioning/deprecation policy rõ.
- Acceptance: breaking public API vô tình phải fail CI; thay đổi có chủ đích cần baseline update + AD.

### P2 — Extensibility productization

- Module scaffolder sinh đủ 5 project, keyed persistence, migration factory, boundary tests, CI/slnx registration và journal stub.
- Mở deep assembly guard khỏi danh sách Identity/RabbitMQ hardcode; auto-load mọi compiled module/adapter an toàn.
- Acceptance: scaffold module mẫu rồi cố tình vi phạm layer/package/slnx → guard bắt.

### P2 — Resilience proof

- Broker restart/network partition/consumer cancellation soak; producer/consumer recovery và readiness xuyên restart.
- Idempotency fencing token + response replay chỉ khi có endpoint/use case thực sự cần; không speculative-rollout toàn base.
- Acceptance: fault-injection test chứng minh không mất message, không stale owner finalize/abort, duplicate effect bị chặn.

## 8. Liên hệ với StarHill

Các gap trên **không chặn** FE.4 hoặc vận hành một resort 60–100 phòng trong mô hình modular monolith một instance/ít instance. Ưu tiên đúng là:

1. Giữ StarHill đơn giản: same-origin, Postgres một DB nhiều schema, RabbitMQ chỉ bật khi cần cascade/realtime nền.
2. Tiếp tục frontend theo vertical slice nhỏ + browser gate.
3. Chạy CI .NET cho diff hiện tại trước baseline/commit release.
4. Nâng operability/supply-chain của base theo nhu cầu sản phẩm, không nhồi multi-region/microservice complexity vào StarHill.

## 9. Verdict

Bedrock hiện tại là một base **mạnh và có tư duy chuyên gia**, đặc biệt ở correctness + anti-drift. Việc cần làm để đạt “đỉnh nhất” không phải viết lại foundation, mà là hoàn thiện ba lớp cuối: **bằng chứng delivery trung thực**, **operability/recovery**, và **public/supply-chain contract**. AD-104 đã đóng lớp đầu ở mức launcher/CI; các mục §7 là roadmap nâng từ “base production-capable” thành “platform product” bền nhiều năm.
