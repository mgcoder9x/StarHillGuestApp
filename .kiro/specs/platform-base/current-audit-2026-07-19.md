# Base-only architecture audit — 2026-07-19

## Phạm vi và kết luận

Audit này chỉ đánh giá `platform/`: Bedrock runtime, module boundaries, adapters, CI/supply-chain,
operability và frontend workspace. Không đánh giá, không phụ thuộc và không đưa nghiệp vụ sản phẩm nào vào
thiết kế base.

> Completion addendum: phần "khoảng trống còn lại" phía dưới ghi lại trạng thái tại thời điểm audit ban đầu. Các mục
> replay exposure, alert/dashboard, fault matrix/soak, FE adapter/browser a11y và package release đã được hoàn tất trong
> cùng ngày; trạng thái cuối cùng được chốt ở mục "Completion addendum" cuối tài liệu.

Kết luận: platform đã vượt mức starter skeleton và đang là một reusable modular-monolith base nghiêm túc.
Các quyết định quan trọng đã được chuyển từ quy ước thành compile gate, architecture test, runtime test hoặc CI
validator. Core đã đủ linh hoạt để lắp nhiều domain; phần còn thiếu chủ yếu là productization và bằng chứng vận hành
đa sự cố, không phải lỗi layering/correctness đã biết.

Không nên gọi platform là một package product đã hoàn thiện tuyệt đối: public-package release lifecycle, module
scaffolding, framework adapters và Docker-backed resilience evidence vẫn là các lát cắt tiếp theo.

## Chấm theo năng lực

| Năng lực | Đánh giá | Nhận định chuyên gia |
|---|---:|---|
| Dependency direction và module isolation | A | Lõi không biết công nghệ; module chỉ giao tiếp qua contracts; discovery kiểm tra project graph và compiled references. |
| Transaction, persistence và domain-event atomicity | A- | Unit of Work keyed, outbox/inbox, lease ownership và restore-on-failure có guard; race đa instance còn cần Docker evidence thường xuyên. |
| Messaging delivery correctness | A- | At-least-once, retry tier, DLQ, confirm, readiness, idempotent Inbox và replay audit đã có; partition/soak chưa đủ bằng chứng. |
| API/security/error contract | A- | Startup validation, Problem Details, security headers, malformed-request mapping và public API snapshots đã đóng các drift lớn. |
| Operability/recovery | B+ | SLO, gauges, runbook, drill checklist và replay mechanism đã có; dashboard/alert backend và operator endpoint vẫn thuộc Host/product. |
| Frontend platform boundary | B+ | Transport/config/auth seam và semantic accessible tokens tốt; chưa có framework adapter, component primitives hay automated browser accessibility harness. |
| Supply-chain/reproducibility | A- | SDK, NuGet, actions, images và Testcontainers immutable; SBOM/provenance được bắt buộc trong CI nhưng chưa chạy được local. |
| Test/evidence maturity | B+ | 405 pass local, 21 Docker skips rõ ràng; CI phải chạy Docker để biến compile-only resilience thành runtime evidence. |

Đánh giá tổng thể: **strong platform base, production-capable core, chưa phải fully productized platform package**.

## Những lát cắt đã triển khai

### Backend và runtime

1. **Reference host trung tính**: host mẫu được đặt tên `Bedrock.ReferenceHost`; nó chỉ chứng minh composition
   root và five-layer module wiring, không mang business identity của một sản phẩm.
2. **Outbox operability**: thêm các instrument theo module `bedrock.outbox.pending`,
   `bedrock.outbox.oldest_pending.age`, `bedrock.outbox.dead_letter.depth`, cùng publish/lease-lost/dead-letter
   counters. Npgsql dùng aggregate query phía server; SQLite có fallback test/dev; refresh bị throttle bằng
   `OperationalMetricsRefreshInterval`.
3. **Replay an toàn**: `IOutboxReplayService` và `EfOutboxReplayService<TContext>` yêu cầu operation id, actor,
   reason, selector bounded và giới hạn 1–1000 message. Dry-run mặc định bật; chỉ dead-letter mới được replay;
   claim một `outbox_replay_operation` single-use, reset message và ghi snapshot từng item vào
   `outbox_replay_audit` trong cùng transaction. Host không tự giả định authorization — quyền phải nằm ở
   composition/API của nơi sử dụng.
4. **Public API compatibility**: compiled snapshots cho sáu assembly platform, gồm public/protected surface,
   inheritance/interfaces, nested nullability, generic constraints, parameter defaults/modifiers, accessor
   visibility, constants, required/init và deprecation metadata. Baseline policy về SemVer, deprecation và event
   schema versioning; thay đổi baseline phải là hành động có chủ ý.
5. **Assembly boundary discovery**: architecture test tự quét module/adapter từ disk, resolve compiled DLL theo
   TFM/configuration và kiểm tra reference thực tế, không chỉ danh sách hardcode.
6. **Fault-injection slice**: test broker restart kiểm tra consumer readiness → stop broker → readiness unhealthy →
   restart → readiness recovered → xử lý message mới và business effect đúng một lần.
7. **Module scaffolding**: `bedrock-module` template tạo đủ Contracts/Domain/Application/Infrastructure/Api và
   UnitTests; `new_module.py` đăng ký project vào `Platform.slnx`; disposable verifier restore/build/test thật.

### Frontend base

`platform/web` là workspace domain-agnostic, gồm:

- `@bedrock/web-core`: runtime config strict validation, relative URL safety, timeout/cancellation classification,
  Problem Details normalization, correlation/idempotency headers, no implicit retry và request preparation hook cho
  cookie/Bearer/BFF mà không khóa auth policy.
- `@bedrock/design-tokens`: semantic theme contract, CSS variable generation ổn định, WCAG AA contrast guard,
  focus defaults và reduced-motion defaults.

Không có product routes, pages, roles, business stores hoặc auth policy trong workspace này.

### Supply-chain và vận hành

- `global.json` khóa SDK `10.0.301` và tắt roll-forward.
- Mọi .NET project có NuGet lockfile và locked restore.
- Frontend workspace có lockfile + local artifact ignore; CI chặn tracked `node_modules`/`dist` cùng .NET `bin/obj`.
- GitHub Actions, Docker base images và Testcontainers images được pin bằng full commit SHA/digest.
- CI có high/all NuGet audit, SPDX SBOM, high/critical vulnerability policy và push-only provenance attestation.
- `operations/SLO.md`, `operations/RUNBOOK.md` và `operations/RECOVERY_DRILL.md` định nghĩa SLO, RTO/RPO, alert
  signals, replay safety và recovery evidence.

## Bằng chứng đã chạy

| Gate | Kết quả |
|---|---|
| `dotnet restore Platform.slnx --locked-mode` | PASS |
| `dotnet build Platform.slnx -c Release --no-restore` | PASS — 0 warning, 0 error |
| `dotnet test Platform.slnx -c Release --no-build -p:RestoreLockedMode=true` | PASS — 408 passed, 0 failed, 26 skipped |
| `PublicApiCompatibilityTests` | PASS — 1/1 |
| `tools/verify-module-template.py` | PASS — generated 6 projects, restore/build/test; solution-registration sandbox verified |
| `tools/verify.ps1 all` | PASS — build, CI validator, module scaffold, frontend gate và full test gate |
| Frontend (`pnpm typecheck`, `pnpm test`, `pnpm build`) | PASS — 22 tests trên 4 package |
| Browser accessibility (`pnpm a11y`) | PASS — Playwright + axe, 2/2 desktop/mobile Chromium |
| Local release pack | PASS — 6 NuGet, 6 symbol packages, 4 npm tarballs; mọi NuGet chứa README platform |
| `python tests/validate_ci.py` | PASS — workflow invariant validator |

26 test skip là các Testcontainers PostgreSQL/RabbitMQ vì máy xác minh hiện tại không có Docker. Đây là skip có
chủ đích và được log rõ; không được diễn giải thành runtime pass. CI phải có Docker và chạy các test này fail-closed,
đặc biệt fault matrix broker restart/partition, consumer cancellation, duplicate effect, database interruption và soak.

## Ranh giới còn lại và mức độ

### Bằng chứng môi trường chưa thể đóng local

- **Docker-backed runtime evidence**: source, build và discovery gate đã xác minh fault matrix, nhưng máy local không có
  Docker nên chưa chứng minh runtime cho PostgreSQL race, RabbitMQ restart/partition/cancellation/duplicate effect và soak.
  CI fail-closed là nơi phải đóng bằng chứng này.
- **Release attestation evidence**: package set đã được pack local và kiểm README, nhưng SPDX/provenance attestation cần
  GitHub runner, OIDC và một tag `v<semver>` thật.

### Ranh giới cố ý của base

- **Module policy**: scaffolder sinh boundary/layer/test seam và đăng ký solution; DbContext, migration strategy,
  authorization policy và integration contract thuộc module author, vì base không được đoán nghiệp vụ.
- **Frontend framework**: base cung cấp core, adapter state và accessible primitives nhưng không khóa React/Vue/Svelte,
  router, data-cache hoặc auth UX.
- **Operator deployment**: reference Host chứng minh authN/authZ, preview/execute/audit và metrics wiring; sản phẩm thật vẫn
  phải chọn identity provider, rate limit, network exposure, alert thresholds và dashboard tenancy.

## Quyết định kiến trúc chốt

- Giữ modular monolith làm default; module sở hữu business/persistence, core sở hữu mechanism và neutral contracts.
- Giữ adapter technology-specific; không đưa EF, ASP.NET hoặc broker types vào Application/Domain ports.
- Giữ at-least-once + Inbox/idempotency thay vì hứa exactly-once xuyên broker.
- Giữ replay ở Application/Infrastructure service, còn authorization và exposure ở Host.
- Giữ frontend core framework-neutral; tokens là semantic/accessibility contract, không phải product design system.
- Giữ CI fail-closed và immutable dependencies; không dùng local green dựa trên stale artifacts.

## Lộ trình đề xuất

1. Chạy CI Docker-backed và lưu artifact/log cho PostgreSQL, RabbitMQ fault matrix và soak.
2. Tạo một tag release thử nghiệm để xác minh package upload, SPDX SBOM và provenance attestation end-to-end.
3. Dùng consumer/reference system thứ hai để kiểm nghiệm ergonomics, versioning và compatibility policy mà không đưa
   nghiệp vụ vào base.

## Files of record

- Runtime: `platform/src/Bedrock.Application`, `platform/src/Bedrock.Infrastructure`, `platform/src/Bedrock.Api`.
- Architecture tests: `platform/tests/Bedrock.ArchitectureTests`.
- Replay/metrics tests: `platform/tests/Bedrock.Infrastructure.Tests`.
- Broker resilience: `platform/tests/Messaging.IntegrationTests/RabbitMqResilienceTests.cs`.
- Frontend: `platform/web/packages/web-core`, `platform/web/packages/design-tokens`,
  `platform/web/packages/web-adapter`, `platform/web/packages/ui-primitives`.
- Operations: `platform/operations`.
- API policy: `platform/contracts`.
- CI policy: `.github/workflows/ci.yml`, `platform/tests/validate_ci.py`.

## Completion addendum — platform product baseline

Các residual productization đã được đóng bằng code + gate, không chỉ bằng tài liệu:

1. **Operator recovery surface:** reference Host có preview, execute và audit lookup versioned; policy yêu cầu `sub` +
   `platform.outbox.replay`, actor không lấy từ body. `IOutboxReplayAuditReader` đọc operation và immutable snapshots.
2. **Operational backend:** Prometheus alert rules cho burn rate/outbox age/dead-letter/backlog/stall và Grafana dashboard
   trung tính theo module; validator chạy local + CI.
3. **Fault matrix:** broker pause/unpause, broker restart, consumer replacement, duplicate delivery/effect fencing,
   database interruption và soak 600 giây theo lịch. Local không Docker chỉ chứng minh compile; CI vẫn fail-closed.
4. **Frontend productization:** framework-neutral async resource adapter, accessible dialog/ARIA primitives và
   Playwright+axe harness trên desktop/mobile Chromium. Không có product routes, role, business store hay brand.
5. **Release lifecycle:** sáu supported assemblies có NuGet metadata/symbol packages; bốn FE packages có publish
   metadata; tag `v<semver>` kiểm API baseline, pack artifact, sinh SPDX SBOM và provenance.
6. **Scaffolder hardening:** template README nằm trong module output, không còn ghi đè README platform; verifier xác
   nhận hash README platform không đổi và sandbox registration không cần project references bị thiếu.

Đánh giá cuối: **fully productized platform baseline ở cấp source/gate local; production confidence còn phụ thuộc hai
bằng chứng ngoại vi không thể giả lập trên máy này: một CI Docker-backed thành công và một tag release có attestation.**
Đây là giới hạn bằng chứng môi trường, không còn là khoảng trống thiết kế/source đã biết.
