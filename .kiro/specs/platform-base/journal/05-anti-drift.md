# 05 — Anti-Drift Mechanism (cơ chế chống lệch — cực mạnh, tự động)

> **Vấn đề drift:** theo thời gian, (1) code lệch design/quyết định; (2) journal lệch design; (3) spec tham chiếu ID không tồn tại (F#/CP#/R#/AD# dangling); (4) lệch format. Review bằng tay KHÔNG đủ mạnh — con người quên.
>
> **Nguyên tắc cực mạnh:** *biến mỗi quyết định/invariant kiểm-được thành GUARD TEST chạy mỗi build → lệch là FAIL BUILD.* Enforcement tự động > tài liệu. Đây là lá chắn mạnh nhất vì nó không dựa trí nhớ và chạy mọi lần `dotnet test`.

## KEYSTONE RULE (luật xương sống)

> **KHÔNG có quyết định code-enforceable nào mà thiếu guard test.**
> Mỗi khi thêm/đổi một AD (hoặc CP) mà code có thể kiểm được → PHẢI thêm guard test tương ứng trong cùng lượt. Nếu chưa kiểm được bằng test (vd quyết định tài liệu thuần) → đánh dấu `MANUAL` trong bản đồ guard và nêu lý do.

## 5 lớp phòng thủ

| Lớp | Cơ chế | Bắt loại drift | Tự động? |
|---|---|---|---|
| L1 — Architecture tests | NetArchTest: dependency matrix, no-business-in-core, CP11, Api⊥Infra | code ↔ design (cấu trúc/ranh giới) | ✅ mỗi build |
| L2 — Decision guard tests | Reflection assert từng AD (Result=class, IntegrationEvent placement, Html namespace...) | code ↔ quyết định journal | ✅ mỗi build |
| L3 — Contract/behavior tests | Unit/integration khoá hành vi (claim mapping, PathMasker, Outbox atomic...) | code ↔ hợp đồng | ✅ mỗi build |
| L4 — **Journal/traceability consistency** | `Bedrock.ArchitectureTests/JournalConsistencyTests` parse markdown journal → enforce INV-1..INV-5 (ID duy nhất+liên tục, mọi AD có guard, ref không dangling, schema đủ trường, CP trong 1..15) | journal tự lệch / ID dangling / thêm AD quên guard | ✅ **mỗi build** (AD-030) |
| L4b — Spec format | `getDiagnostics` 4 file spec + journal (heading/EARS/CP/waves) | lệch format Kiro | ⚙️ chạy tay sau mỗi sửa spec (Kiro-only, không chạy được trong test) |
| L5 — Authority hierarchy | `design.md` là nguồn sự thật; lệch → sửa theo design + ghi journal | design ↔ journal ↔ code | 🧠 quy trình (N-002) |

## Vòng lặp bắt buộc sau MỖI thay đổi (không được bỏ)

1. `dotnet test Platform.slnx` → **build 0 warning + TẤT CẢ test xanh** (gồm L1/L2/L3). Đỏ = có drift/hồi quy → dừng, sửa.
2. `getDiagnostics` trên `requirements.md`/`design.md`/`tasks.md` + journal → 0 lỗi (L4).
3. Có quyết định mới/đổi? → append journal (AD/DV/TO/N) với **Provenance thật** + (nếu code-enforceable) thêm guard test (KEYSTONE RULE).
4. Quyết định cũ bị lật? → set `Superseded by <ID>`, KHÔNG xoá lịch sử.

## Bản đồ Guard — Correctness Properties (CP1–CP15)

| CP | Nội dung | Guard test | Trạng thái |
|---|---|---|---|
| CP1 | no-business-in-core (tên/namespace + literal) | `NoBusinessInCoreTests` (tên/namespace, 5 assembly) + `NoBusinessInCoreLiteralTests` (Mono.Cecil quét ldstr + const, 5 assembly) | ✅ ENFORCED đầy đủ (task 4 + task 20; AD-022 resolved bởi AD-046) |
| CP2 | Api ⊥ Infrastructure | `Bedrock.Api.Tests/ApiBoundaryTests` (+ chiều ngược Infrastructure⊥Api: `DependencyRuleTests`) | ✅ ENFORCED (task 5.5/6) |
| CP3 | Adapter isolation | (task 14 — Adapters chưa tồn tại) | ⏳ PENDING |
| CP4 | Module boundary | `ModuleBoundaryTests` (Contracts thuần DTO; Domain/App ⊥ Infra/Api; + negative control engine bắt dep vào internal module) | ✅ ENFORCED (task 16.3; A→B đầy đủ khi có module thứ 2) |
| CP5 | Single composition root | `ModuleBoundaryTests` (module `.Api` ⊥ mọi Infra; module `.Infra` ⊥ mọi Api → không library nào bắc cầu Api+Infra; chỉ Host exe được) + CP2 | ✅ ENFORCED (task 16.3; xem N-044 lý do enforce từ phía module thay vì nạp web-exe) |
| CP6 | Outbox atomicity | `Bedrock.Infrastructure.Tests/OutboxWriterTests` (enqueue+state commit/rollback CÙNG transaction trên SQLite — DB quan hệ thật) | 🟡 PARTIAL (same-transaction atomic ✅; publish-path + Postgres thật → task 7.4 Testcontainers) |
| CP7 | Rotation atomicity | `RefreshTokenStoreTests` (consume-if-not-revoked + consume+add rollback cùng transaction, SQLite) | 🟡 PARTIAL (atomic đơn-luồng ✅; race 2-request đồng thời + Postgres → task 8.3) |
| CP8 | Inbox idempotency | `Bedrock.Infrastructure.Tests/OutboxDispatcherTests` (TryMarkProcessed first-true / dup-false / khác-consumer trên SQLite) | 🟡 PARTIAL (unit idempotency ✅; race đồng thời + Postgres → task 7.4) |
| CP9 | Fail-fast missing port | `RequiredPortsValidatorTests` (thiếu port → ném GỘP; scope-aware) | ✅ ENFORCED (task 10.2) |
| CP10 | Correlation unity | `Bedrock.Api.Tests/BedrockPipelineTests` (gửi `traceparent` → header == body.traceId == trace hiện hành; OTel providers registered; metric http.server.request.duration phát) | ✅ ENFORCED (task 5.4 → siết task 18/AD-044) |
| CP11 | use case ⊥ Messaging.Dispatch | `UseCaseSeamTests` (Bedrock) + `ModuleBoundaryTests` (Identity use case) | ✅ ENFORCED |
| CP12 | Error code contract stable | `Bedrock.ContractTests/ErrorCodeSnapshotTests` (snapshot registry Error.Code — reflect Domain lõi + module) | ✅ ENFORCED (task 20) |
| CP13 | Log masking mọi nơi | `PathMaskerTests` + `BedrockPipelineTests` (masked log, no raw token) | ✅ ENFORCED (task 5.4; + posture AD-024 cho framework logging) |
| CP14 | Domain event atomic dispatch | `Bedrock.Infrastructure.Tests/DomainEventDispatchTests` (handler-effect commit cùng transaction; handler ném → rollback; max-depth ném; no-handler no-op) | ✅ ENFORCED (task 6.4) |
| CP15 | Outbox exclusive claim | (task 7.4 — Testcontainers) | ⏳ PENDING |

## Bản đồ Guard — Decisions (AD code-enforceable)

> **Bất biến INV-2 (auto):** mỗi `AD-###` trong `01-decisions.md` PHẢI xuất hiện (token đầy đủ `AD-0XX`) trong bảng này — `JournalConsistencyTests` fail build nếu thiếu. Nhờ đó KHÔNG thể thêm quyết định mà quên khai trạng thái guard.

| AD | Quyết định | Guard test | Trạng thái |
|---|---|---|---|
| AD-001 | prefix `Bedrock.*` | (ngầm: mọi arch test dùng namespace `Bedrock.*`) | 🟡 IMPLICIT |
| AD-002 | `Result`/`Result<T>` = sealed class | `DecisionGuardTests.AD002_*` | ✅ ENFORCED |
| AD-003 | dead-letter = cột `dead_lettered_at` (không bảng DLQ) | `OutboxWriterTests` (cột `dead_lettered_at` tồn tại) + `OutboxDispatcherTests` (dead-letter sau MaxAttempts + không claim lại) | ✅ ENFORCED (task 7.2/7.3) |
| AD-004 | Outbox/Inbox per-module | `OutboxWriterTests`/`OutboxDispatcherTests` (helper `AddOutboxInbox` map vào DbContext module trên SQLite) | 🟡 PARTIAL (per-module trong module thật → task 16) |
| AD-005 | tách 2 namespace `Messaging` vs `Messaging.Dispatch` | `UseCaseSeamTests` (CP11) | ✅ ENFORCED |
| AD-006 | `IIntegrationEventTypeRegistry` (EventType→CLR type) | `OutboxDispatcherTests.Registry_resolves_known_eventtype_and_null_for_unknown` | ✅ ENFORCED (task 7.3) |
| AD-007 | domain event dispatch TRƯỚC commit (cùng transaction) | `DomainEventDispatchTests` (CP14) | ✅ ENFORCED (task 6.4) |
| AD-008 | `JwtKeyRingOptions` điểm chung ký/verify | ký: `JwtTokenServiceTests` (kid + rotation); verify: `AuthMechanismTests` (TestHost) | ✅ ENFORCED (task 5.3 + 9.2) |
| AD-009 | phân loại default per-port (degrade `NullAppCache` / fail-loud `Throwing*`) | `ExtensionArchitectureTests` (cache miss-through + 8 port fail-loud throw + override Replace + registry) | ✅ ENFORCED (task 13) |
| AD-010 | refresh-token: cơ chế lõi, bảng schema module | `RefreshTokenStoreTests` (store atomic + AddRefreshTokens helper trên SQLite) | 🟡 PARTIAL (store ✅; use case rotation → task 16; race đa-connection → task 8.3) |
| AD-011 | startup validator qua `RequiredPorts` (scope-aware, aggregate) | `RequiredPortsValidatorTests` | ✅ ENFORCED (task 10.2) |
| AD-012 | UoW reentrancy (nested join transaction) | `TransactionTests.*reentrant*` (nested join + nested rollback) | ✅ ENFORCED (task 6.2) |
| AD-013 | thứ tự middleware pipeline §3.5 | `BedrockPipelineTests` (correlation/mask → hàm ý thứ tự) | 🟡 PARTIAL (rate-limit/CORS slot → task 11) |
| AD-014 | `IEndpointModule` hợp đồng discovery | `BedrockPipelineTests` (discovery + map) | ✅ ENFORCED (task 5.4) |
| AD-015 | hợp đồng serialization Outbox (STJ cố định) | `OutboxWriterTests.Enqueue_serializes_payload_*` (camelCase, giữ field record dẫn xuất) | ✅ ENFORCED (task 7.2) |
| AD-016 | dispatcher atomic claim (skip-locked) + exponential backoff | backoff/threshold/dead-letter: `OutboxDispatcherTests.ComputeBackoff_*`/`Dispatch_*` | 🟡 PARTIAL (backoff/dead-letter ✅; skip-locked claim đa-instance → task 7.4) |
| AD-017 | `IntegrationEvent` ở `Bedrock.Messaging.Contracts` (zero-dep) | `DecisionGuardTests.AD017_*` + `DependencyRuleTests` | ✅ ENFORCED |
| AD-018 | Scrutor/DI abstractions cho phép ở Application | `DependencyRuleTests` (Application không ref EF/ASP.NET) | 🟡 PARTIAL (kiểm mặt cấm; whitelist là chủ đích) |
| AD-019 | suppress `CA1000` cho `Result<T>` factory | build 0-warning gate (TreatWarningsAsErrors) | 🟡 IMPLICIT (build gate) |
| AD-020 | tách tên `NotFound`/`NotFoundGeneric` | build gate (CS0111 sẽ tái xuất nếu regress) | 🟡 IMPLICIT (build gate) |
| AD-021 | `IHtmlSanitizer` ở `Ports.Html` | `DecisionGuardTests.AD021_*` | ✅ ENFORCED |
| AD-022 | chia đôi enforcement CP1 (tên/namespace vs literal) | `NoBusinessInCoreTests` (tên/namespace) | ✅ RESOLVED (literal phần bù hoàn tất bởi AD-046/task 20) |
| AD-046 | CP1 literal scan qua Mono.Cecil (ldstr + const, 5 assembly Bedrock.*) | `NoBusinessInCoreLiteralTests` (+ negative control dirty-assembly) | ✅ ENFORCED (task 20) |
| AD-023 | claim JWT-native (`sub`/`role`/`permission`/`tenant_id`/`sid`) | `HttpContextCurrentUserTests` (behavioral) | ✅ ENFORCED |
| AD-024 | request-logging masked thay framework + Host hạ `Microsoft.AspNetCore`=Warning | `BedrockPipelineTests` (e2e no-leak WITH posture) | ✅ ENFORCED (e2e) + 🧠 MANUAL (posture Host — task 16.2) |
| AD-025 | dispatcher generic invoker (không MethodInfo.Invoke) | `DomainEventDispatchTests.Handler_throwing_*` (exception giữ nguyên kiểu) | ✅ ENFORCED (task 6.4) |
| AD-026 | `SaveChanges` đồng bộ bị chặn | `TransactionTests.SaveChanges_sync_is_blocked_*` | ✅ ENFORCED (task 6.1) |
| AD-027 | max dispatch depth → ném lỗi | `DomainEventDispatchTests.Exceeding_max_dispatch_depth_*` | ✅ ENFORCED (task 6.4) |
| AD-028 | pin transitive SQLitePCLRaw 3.50.3 (CVE) | build/restore gate (NuGetAudit + TreatWarningsAsErrors) | ✅ ENFORCED (build gate) |
| AD-029 | `OutboxMessage.Id = IntegrationEvent.Id` (idempotency đầu-cuối) | `OutboxWriterTests.Enqueue_serializes_payload_and_copies_event_fields` | ✅ ENFORCED (task 7.2) |
| AD-030 | cổng journal-consistency tự động (INV-1..5) | `JournalConsistencyTests` (tự gác chính nó) | ✅ ENFORCED (mỗi build) |
| AD-031 | `GetByHashAsync` trả cả token revoked (reuse-detection §7.4) | `RefreshTokenStoreTests.GetByHash_returns_revoked_token_for_reuse_detection` | ✅ ENFORCED (task 8.2) |
| AD-032 | ký JWT bằng `JsonWebTokenHandler` (không legacy handler — tránh static claim-map) | `JwtTokenServiceTests` (claim `sub`/`role` giữ nguyên tên + kid + rotation) | ✅ ENFORCED (task 9.2) |
| AD-033 | marker-scan DI tự viết bằng reflection (không Scrutor) | `RegistrationConventionTests` (lifetime đúng + loại IManualRegistration + self-fallback + duplicate-guard) | ✅ ENFORCED (task 10.1) |
| AD-034 | rate-limit 429 = edge concern (problem+json trực tiếp, không vào ErrorType) | `RateLimitIntegrationTests` (429 khi vượt) + `DecisionGuardTests`? (ErrorType giữ 6 loại — kiểm gián tiếp) | ✅ ENFORCED (task 11.1) |
| AD-035 | HSTS/HTTPS-redirect = trách nhiệm Host (base không ép) | (ràng buộc Host — kiểm ở task 16.2) | 🧠 MANUAL (Host posture) |
| AD-036 | field shape record External Auth (design chỉ đặt tên) | `DependencyRuleTests` (port zero-tech) + build 0-warning | 🟡 IMPLICIT (contract-first; behavior khi có adapter task 12.3-consumer) |
| AD-037 | Scrutor `Decorate` cho pipeline behaviors (§8); marker-scan giữ reflection (revisit AD-033/AD-018) | `PipelineOrderTests` (DI+Scrutor thật: thứ tự Logging→Authz→Validation→Idempotency→Transaction→UseCase; unauthorized+invalid→forbidden) | ✅ ENFORCED (task 15) |
| AD-038 | permission khai `[RequirePermission]` trên KIỂU input, ngữ nghĩa AND (least-privilege) | `AuthorizationDecoratorTests` (missing→forbidden, granted→run, no-attr→pass, AND-semantics, command variant, `PermissionMetadata.For`) | ✅ ENFORCED (task 15) |
| AD-039 | Idempotency v1: gate `IIdempotentCommand` + `TryBegin` (không replay) + TTL 24h → `idempotency_conflict` | `IdempotencyDecoratorTests` (first-run/duplicate-conflict/non-idempotent-skip-store/command-variant/ttl=24h) | ✅ ENFORCED (task 15) |
| AD-040 | Transaction behavior CHỈ `ICommandUseCase<>` (ghi thuần); value-returning tự quản (reentrancy AD-012) | `TransactionDecoratorTests` (body-inside-transaction, failing-propagates) + `PipelineOrderTests` (command mở transaction; query family không; unauthorized→0 transaction) | ✅ ENFORCED (task 15) |
| AD-041 | Refresh token lifetime mặc định 14 ngày (module Identity, promotable Options) | `RefreshAccessTokenUseCaseTests` (lifetime=14d + expiresAt=now+14d) | ✅ ENFORCED (task 16.1) |
| AD-042 | health-check DB đặt tên per-context `database:{TContext}` (multi-module không trùng) | `MultiModulePersistenceTests` (2 context → 2 tên `ready` duy nhất + HealthCheckService resolve không ném) | ✅ ENFORCED (task 16.2) |
| AD-043 | API versioning URL-segment `/v{version}` + `MapVersionedGroup` (default v1, report versions) | `HostSmokeTests` (`/v1/identity/token/refresh` → 400 + header `api-supported-versions: 1.0`) | ✅ ENFORCED (task 17) |
| AD-044 | correlation id = W3C traceId trace hiện hành (bỏ override client; propagation qua traceparent) | `BedrockPipelineTests.Unhandled_exception_*` (traceparent → header==body.traceId==traceId) | ✅ ENFORCED (task 18) |
| AD-045 | JWT validate-on-start (options `.ValidateOnStart()`, không eager); secret ngoài repo | `HostSmokeTests` (boot với secret inject; `Host_fails_fast_when_required_jwt_secret_is_missing` → boot fail "HS256") | ✅ ENFORCED (task 19) |
| AD-047 | Outbox retention: base cấp logic `PurgeAsync` (Host lên lịch); TTL mặc định processed 7d / dead-letter giữ vô thời hạn; vị từ xoá không đụng pending/dead-letter | `OutboxRetentionTests` (xoá đúng processed-cũ, giữ pending/processed-mới/dead-letter; dead-letter cũ chỉ xoá khi bật TTL; 0-khi-không-hết-hạn) | ✅ ENFORCED (task 7.5, nhánh client-side SQLite; nhánh Npgsql ExecuteDelete → task 7.4) |

## Cổng journal-consistency (INV-1..INV-5) — L4 tự động (AD-030)

`Bedrock.ArchitectureTests/JournalConsistencyTests` đọc chính các file journal (`01`–`05`) và biến kỷ luật tài liệu thành **build gate** (trước đây thủ công → nay `dotnet test` bắt). Nếu không tìm thấy thư mục journal (đi lên cây từ thư mục test không gặp `.kiro/specs/platform-base/journal`), test **`Assert.Fail`** rõ ràng — KHÔNG skip: cổng không được tự tắt âm thầm (skip = vô hiệu ngầm = drift ẩn). Test là dev-time nên luôn chạy trong repo có `.kiro`.

| INV | Bất biến | Vì sao (root cause của loại drift bị chặn) |
|---|---|---|
| INV-1 | Mỗi loại ID (AD/DV/TO/N) **duy nhất + liên tục `1..N`** | phát hiện bản ghi bị mất, nhân đôi, hoặc đánh số lại sai |
| INV-2 | Mọi `AD-###` (01) **có mặt trong bảng guard 05** | KEYSTONE RULE tự động: không thể thêm quyết định mà quên khai guard |
| INV-3 | Mọi ref `AD/DV/TO/N-###` trong journal **trỏ tới bản ghi có thật** | chống ID dangling / gõ nhầm một mã không tồn tại |
| INV-4 | Mỗi bản ghi **AD & DV** có đủ `Status:` + `Provenance/Evidence:` | chống bịa — mọi quyết định/độ lệch phải có bằng chứng nguồn |
| INV-5 | Mọi `CP##` tham chiếu trong journal nằm trong **1..15** | chống CP dangling (CP1–CP15 là tập đóng, N-011) |

> **Chốt logic:** L4 xưa là khâu yếu nhất (dựa trí nhớ). INV-1..5 khép nó vào cùng cổng `dotnet test` với code — đúng tinh thần "con người quên, build thì không", nay áp cho CẢ tài liệu.

## Khoảng hở chưa auto-enforce (nói thật, không giấu)

- ~~**CP1 string-literal**~~: ĐÃ ĐÓNG (task 20/AD-046) — `NoBusinessInCoreLiteralTests` quét ldstr + const bằng Mono.Cecil trên cả 5 assembly `Bedrock.*` (có negative control). CP1 nay phủ cả tên/namespace lẫn literal.
- **CP15 exclusive-claim + CP6 full + CP8 race**: dispatcher backoff/threshold/dead-letter/lọc-tới-hạn + inbox idempotency đơn-luồng ĐÃ enforced (task 7.3, SQLite); nhưng **claim skip-locked đa-instance + race Postgres thật** chờ task 7.4 (Testcontainers).
- **CP3 + CP7**: CP3 (adapter isolation) chờ `Adapters.*` (task 14, cần Docker/Testcontainers RabbitMQ); CP7 (rotation race đa-connection) chờ Testcontainers Postgres (task 8.3). CP4/CP5/CP9/CP12 nay ĐÃ enforced. Mỗi task còn lại có dòng "Nghiệm thu" + `_Correctness Properties_` buộc thêm guard khi làm.
- **INV-4b (getDiagnostics format)**: kiểm heading/EARS/waves của Kiro KHÔNG chạy được trong `dotnet test` (là diagnostic của Kiro IDE) → vẫn thủ công sau mỗi sửa spec (L4b).
- **Bản sao lồng `StarHillGuestApp/StarHillGuestApp/`** (N-030): một cây `.kiro` cũ song song, stale → rủi ro sửa nhầm bản. `JournalConsistencyTests` chỉ gác bản GỐC (nơi có `platform/`). Cần người xác nhận/gỡ (không tự xoá).
- **AD tài liệu thuần** (vd quyết định lộ trình): không code-enforceable → INV-2 vẫn buộc có mặt trong bảng guard với trạng thái `IMPLICIT`/`MANUAL` (không được biến mất khỏi radar).

## Thêm một guard mới (thủ tục)

1. Xác định quyết định/CP có code-enforceable không. Nếu có → viết test ở đúng project:
   - Cấu trúc/dependency/naming → `Bedrock.ArchitectureTests` (`DecisionGuardTests` hoặc file luật) hoặc `Bedrock.Api.Tests/Architecture` (cho luật chạm Api).
   - Hành vi → test project tầng tương ứng.
2. Mỗi luật NetArchTest phải có **negative control** (chứng minh bắt được vi phạm).
3. Cập nhật 2 bảng guard ở trên (CP hoặc AD) + trạng thái.
4. Chạy vòng lặp bắt buộc.

> **Tóm tắt một câu:** *Drift bị chặn mạnh nhất khi mỗi quyết định là một test — con người quên, build thì không.*
