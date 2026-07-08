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
| L4 — Spec format + traceability | `getDiagnostics` 4 file spec + journal; bảng truy vết ID | lệch format / ID dangling | ⚙️ chạy tay sau mỗi sửa spec |
| L5 — Authority hierarchy | `design.md` là nguồn sự thật; lệch → sửa theo design + ghi journal | design ↔ journal ↔ code | 🧠 quy trình (N-002) |

## Vòng lặp bắt buộc sau MỖI thay đổi (không được bỏ)

1. `dotnet test Platform.slnx` → **build 0 warning + TẤT CẢ test xanh** (gồm L1/L2/L3). Đỏ = có drift/hồi quy → dừng, sửa.
2. `getDiagnostics` trên `requirements.md`/`design.md`/`tasks.md` + journal → 0 lỗi (L4).
3. Có quyết định mới/đổi? → append journal (AD/DV/TO/N) với **Provenance thật** + (nếu code-enforceable) thêm guard test (KEYSTONE RULE).
4. Quyết định cũ bị lật? → set `Superseded by <ID>`, KHÔNG xoá lịch sử.

## Bản đồ Guard — Correctness Properties (CP1–CP15)

| CP | Nội dung | Guard test | Trạng thái |
|---|---|---|---|
| CP1 | no-business-in-core (tên/namespace) | `NoBusinessInCoreTests` | ✅ ENFORCED (tên); literal → task 20 (PENDING, AD-022) |
| CP2 | Api ⊥ Infrastructure | `Bedrock.Api.Tests/ApiBoundaryTests` (+ chiều ngược Infrastructure⊥Api: `DependencyRuleTests`) | ✅ ENFORCED (task 5.5/6) |
| CP3 | Adapter isolation | (task 14 — Adapters chưa tồn tại) | ⏳ PENDING |
| CP4 | Module boundary | (task 16.3 — Modules chưa tồn tại) | ⏳ PENDING |
| CP5 | Single composition root | (task 16.3 — Host chưa tồn tại) | ⏳ PENDING |
| CP6 | Outbox atomicity | `Bedrock.Infrastructure.Tests/OutboxWriterTests` (enqueue+state commit/rollback CÙNG transaction trên SQLite — DB quan hệ thật) | 🟡 PARTIAL (same-transaction atomic ✅; publish-path + Postgres thật → task 7.4 Testcontainers) |
| CP7 | Rotation atomicity | (task 8.3) | ⏳ PENDING |
| CP8 | Inbox idempotency | (task 7.4) | ⏳ PENDING |
| CP9 | Fail-fast missing port | (task 10.2) | ⏳ PENDING |
| CP10 | Correlation unity | `Bedrock.Api.Tests/BedrockPipelineTests` (header == body.traceId, client-provided id) | ✅ ENFORCED (task 5.4) |
| CP11 | use case ⊥ Messaging.Dispatch | `UseCaseSeamTests` | ✅ ENFORCED |
| CP12 | Error code contract stable | (task 20 — reflection snapshot) | ⏳ PENDING |
| CP13 | Log masking mọi nơi | `PathMaskerTests` + `BedrockPipelineTests` (masked log, no raw token) | ✅ ENFORCED (task 5.4; + posture AD-024 cho framework logging) |
| CP14 | Domain event atomic dispatch | `Bedrock.Infrastructure.Tests/DomainEventDispatchTests` (handler-effect commit cùng transaction; handler ném → rollback; max-depth ném; no-handler no-op) | ✅ ENFORCED (task 6.4) |
| CP15 | Outbox exclusive claim | (task 7.4 — Testcontainers) | ⏳ PENDING |

## Bản đồ Guard — Decisions (AD code-enforceable)

| AD | Quyết định | Guard test | Trạng thái |
|---|---|---|---|
| AD-002 | `Result`/`Result<T>` = sealed class | `DecisionGuardTests.AD002_*` | ✅ ENFORCED |
| AD-017 | `IntegrationEvent` ở `Bedrock.Messaging.Contracts` (zero-dep) | `DecisionGuardTests.AD017_*` + `DependencyRuleTests` | ✅ ENFORCED |
| AD-021 | `IHtmlSanitizer` ở `Ports.Html` | `DecisionGuardTests.AD021_*` | ✅ ENFORCED |
| AD-023 | claim JWT-native (`sub`/`role`/...) | `HttpContextCurrentUserTests` (behavioral) | ✅ ENFORCED |
| AD-001 | prefix `Bedrock.*` | (ngầm: mọi arch test dùng namespace `Bedrock.*`) | 🟡 IMPLICIT |
| AD-018 | Scrutor/DI abstractions cho phép ở Application | `DependencyRuleTests` (Application không ref EF/ASP.NET) | 🟡 PARTIAL (kiểm mặt cấm; whitelist là chủ đích) |
| AD-007 | domain event dispatch TRƯỚC commit (cùng transaction) | `DomainEventDispatchTests` (CP14) | ✅ ENFORCED (task 6.4) |
| AD-012 | UoW reentrancy (nested join transaction) | `TransactionTests.*reentrant*` (nested join + nested rollback) | ✅ ENFORCED (task 6.2) |
| AD-025 | dispatcher generic invoker (không MethodInfo.Invoke) | `DomainEventDispatchTests.Handler_throwing_*` (exception giữ nguyên kiểu) | ✅ ENFORCED (task 6.4) |
| AD-026 | `SaveChanges` đồng bộ bị chặn | `TransactionTests.SaveChanges_sync_is_blocked_*` | ✅ ENFORCED (task 6.1) |
| AD-027 | max dispatch depth → ném lỗi | `DomainEventDispatchTests.Exceeding_max_dispatch_depth_*` | ✅ ENFORCED (task 6.4) |
| AD-028 | pin transitive SQLitePCLRaw 3.50.3 (CVE) | build/restore gate (NuGetAudit + TreatWarningsAsErrors) | ✅ ENFORCED (build gate) |
| AD-029 | `OutboxMessage.Id = IntegrationEvent.Id` (idempotency đầu-cuối) | `OutboxWriterTests.Enqueue_serializes_payload_and_copies_event_fields` | ✅ ENFORCED (task 7.2) |
| AD-004/005/006/008/009/010/011/013/014/015/016 | (cấu trúc/hành vi Infrastructure/Host chưa dựng đủ) | (guard khi task tương ứng dựng) | ⏳ PENDING theo task |
| AD-019/020/022 | analyzer suppress / overload naming / CP1 split | (bản thân là quyết định về công cụ, kiểm gián tiếp qua build 0 warning) | 🟡 IMPLICIT (build gate) |

## Khoảng hở chưa auto-enforce (nói thật, không giấu)

- **CP1 string-literal**: chưa quét hằng chuỗi trong thân method (AD-022) → task 20 (source/IL scan). Rủi ro: một hằng path/role lọt vào lõi mà guard hiện tại không bắt. GIẢM THIỂU: code review + task 20.
- **CP3/4/5/6/7/8/9/10/12/14/15**: chờ project/tính năng tương ứng ra đời (Infrastructure/Adapters/Modules/Host). Mỗi task đó có dòng "Nghiệm thu" + `_Correctness Properties_` buộc thêm guard khi làm.
- **AD tài liệu thuần** (vd quyết định lộ trình): không code-enforceable → thuộc L4/L5 (review + authority hierarchy).

## Thêm một guard mới (thủ tục)

1. Xác định quyết định/CP có code-enforceable không. Nếu có → viết test ở đúng project:
   - Cấu trúc/dependency/naming → `Bedrock.ArchitectureTests` (`DecisionGuardTests` hoặc file luật) hoặc `Bedrock.Api.Tests/Architecture` (cho luật chạm Api).
   - Hành vi → test project tầng tương ứng.
2. Mỗi luật NetArchTest phải có **negative control** (chứng minh bắt được vi phạm).
3. Cập nhật 2 bảng guard ở trên (CP hoặc AD) + trạng thái.
4. Chạy vòng lặp bắt buộc.

> **Tóm tắt một câu:** *Drift bị chặn mạnh nhất khi mỗi quyết định là một test — con người quên, build thì không.*
