# 01 — Autonomous Decisions (quyết định AI tự ra mà spec không nói)

> Ghi các quyết định KHÔNG có sẵn trong blueprint/requirements gốc, do AI đề xuất và (nếu có) được user chốt. Mỗi bản ghi truy vết nguồn thật.

---

### AD-001 — Prefix lõi = `Bedrock.*`
- Status: Confirmed
- Date: 2026-07-07
- Decider: user (chốt) trên đề xuất AI(Kiro) + AI(review)
- Provenance/Evidence: chat phiên này — user trả lời "1: Bedrock"; đã áp 146 chỗ đổi `BuildingBlocks`→`Bedrock`, verified bằng grep (0 kết quả `BuildingBlocks` còn lại trong 4 file spec); `design.md` header "Quyết định đã chốt (a)".
- Context: Blueprint gốc dùng tiền tố `Foundation.*`; bản thiết kế trung gian dùng `BuildingBlocks.*` (thuật ngữ modular-monolith phổ biến). User thấy `BuildingBlocks` "làm sao ấy", yêu cầu đề xuất một-từ.
- Decision/Change: Toàn bộ project/namespace lõi dùng `Bedrock.*` (`Bedrock.Domain/Application/Infrastructure/Api`). Thư mục giải pháp giữ `platform/`.
- Rationale (verifiable):
  - **MAX_PATH Windows (root cause thật):** `BuildingBlocks.Infrastructure` + `bin/Debug/net10.0` + path test lồng sâu dễ chạm giới hạn 260 ký tự trên Windows; `Bedrock.*` ngắn hơn ~7 ký tự/segment → giảm rủi ro. (Hệ điều hành mục tiêu = Windows, xác nhận từ EnvironmentContext.)
  - **Chống lẫn part/whole:** thư mục `platform/` là *toàn bộ giải pháp*; nếu lấy `Platform.*` cho *riêng lõi* thì tên cái toàn thể bị gán cho bộ phận → mơ hồ. `Bedrock` (tầng đá nền) chỉ đúng phần lõi.
  - **Khớp invariant I1:** "bedrock" = nền mọi thứ đứng trên = lõi domain-agnostic.
- Alternatives: `Platform.*` (loại: lẫn part/whole, generic); `Core.*` (loại: đụng khái niệm .NET Core, chung chung); giữ `BuildingBlocks.*` (loại: dài, số nhiều, chạm MAX_PATH).
- Consequences: Va chạm tên với thư viện niche `Bedrock.Framework` (David Fowler) — KHÔNG ảnh hưởng vì chỉ dùng làm namespace nội bộ, không publish trùng package-id. Nếu sau này publish NuGet công khai, cân nhắc lại package-id.
- Reversibility: High khi greenfield (find-replace toàn cục); Low sau khi có nhiều code.
- Traceability: F1/F13 (định danh library), design §1.1, review §4.

---

### AD-002 — `Result`/`Result<T>` = `sealed class` + factory `Success()/Failure()`
- Status: Confirmed
- Date: 2026-07-07
- Decider: user (chốt) trên đề xuất AI(Kiro) + AI(review)
- Provenance/Evidence: chat — user "2: class + Success/Failure"; `design.md` §4.4 đã đổi `readonly struct`→`sealed class` (verified grep: không còn khai báo `struct Result`, chỉ còn ghi chú "KHÔNG readonly struct"); `tasks.md` task 2 ghi "sealed class".
- Context: Bản design trung gian đề xuất `readonly struct` (zero-alloc) kèm invariant `default(Result)=failure`. Cần chốt struct vs class trước khi code type dùng nhiều nhất toàn platform.
- Decision/Change: `sealed class`; `Success()/Failure()`; truy cập `Value` khi `IsFailure` → ném `InvalidOperationException` (không trả `default` âm thầm).
- Rationale (verifiable):
  - **Root cause của lựa chọn:** platform là web API **I/O-bound** → một cấp phát gen0 cho `Result` là không đáng kể so với latency DB/HTTP; lợi ích zero-alloc của struct gần như vô nghĩa ở tầng này.
  - **Loại bỏ footgun:** `readonly struct` có bẫy `default(Result)` (trạng thái rỗng bị coi là kết quả hợp lệ) — rủi ro thật cho team nhiều cấp độ. `class` không có bẫy đó.
- Alternatives: `readonly struct` (loại: footgun default-state; lợi ích alloc không phù hợp ngữ cảnh I/O-bound). Tên `Ok/Fail` (loại: chọn `Success/Failure` cho khớp văn design §7).
- Consequences: Cấp phát nhỏ mỗi kết quả (gen0, rẻ). Nếu sau này profiling chỉ ra hot-path in-memory thực sự mới cân nhắc struct.
- Reversibility: Medium (đổi khai báo + call-site; hợp đồng member giữ nguyên theo thiết kế).
- Traceability: F20 (error contract), design §4.4, R29.

---

### AD-003 — Dead-letter biểu diễn bằng cột `dead_lettered_at` (không bảng DLQ riêng)
- Status: Confirmed
- Date: 2026-07-07
- Decider: user (chốt) trên đề xuất AI(review) + AI(Kiro)
- Provenance/Evidence: chat — user "3: column"; `design.md` §4.5 schema `outbox_message` có cột `dead_lettered_at` (verified grep); đã thêm `tasks.md` task 7.5 retention.
- Context: Dispatcher cần cách ly message poison sau ngưỡng retry. Chọn cột trên `outbox_message` vs bảng DLQ tách riêng.
- Decision/Change: Cột `dead_lettered_at timestamptz NULL` trên `outbox_message`; thêm task retention/cleanup (7.5) dọn row `processed`/giữ `dead_lettered_at`.
- Rationale (verifiable):
  - **Đơn giản + đúng:** giữ một bảng → claim nguyên tử của dispatcher không cần thao tác "move" (delete+insert) chéo bảng; query loại trừ `dead_lettered_at IS NOT NULL`.
  - **Đây là hạ tầng nội bộ, không phải public contract** → đổi biểu diễn sau này rủi ro thấp (có thể migrate lên bảng DLQ khi ops cần replay chuyên dụng).
- Alternatives: Bảng DLQ riêng (loại hiện tại: thêm bộ phận + move nguyên tử; chỉ cần khi ops đòi nơi soi/replay chuyên dụng).
- Consequences: `outbox_message` chứa cả row dead + processed → cần job retention (đã thêm task 7.5) dù sao cũng cần cho row processed.
- Reversibility: High (nội bộ, migrate được).
- Traceability: F25/F33, R8.5, CP15, design §4.5/§7.2, task 7.3/7.5.

---

### AD-004 — Outbox/Inbox theo per-module (không dùng schema infra dùng chung)
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D9; `design.md` §4.6 + helper `modelBuilder.AddOutboxInbox()` (verified trong tasks 7.1); blueprint gốc để ngỏ "shared infra schema HOẶC per-module".
- Context: Blueprint không chốt vị trí bảng outbox/inbox. Đây là lựa chọn **đúng đắn**, không phải khẩu vị.
- Decision/Change: Outbox/inbox nằm trong chính `DbContext`/schema của module (per-module).
- Rationale (verifiable): **Root cause:** CP6 (outbox atomicity) chỉ giữ **by construction** khi outbox ghi CÙNG `DbContext`/transaction với thay đổi state của module. Schema dùng chung → khác DbContext → không còn nguyên tử cùng một `SaveChanges`.
- Alternatives: Shared infra schema (loại: phá tính nguyên tử một-transaction của CP6).
- Consequences: Mỗi module cần một dispatcher (chi phí vận hành thêm) — chấp nhận để đổi lấy nguyên tử + module tự chủ.
- Reversibility: Medium.
- Traceability: F5/F25/I3, CP6, design §4.6, task 7.1.

---

### AD-005 — Tách seam messaging thành 2 namespace (`Messaging` vs `Messaging.Dispatch`)
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D3; `design.md` §5.2 namespace `Bedrock.Application.Messaging.Dispatch` + luật ArchTest #6 + CP11 (verified grep `Messaging.Dispatch`).
- Context: Bản cũ để tất cả interface messaging chung một namespace → luật "use case không được publish thẳng bus" (CP11) KHÔNG kiểm được bằng máy ("làm sao phát hiện một use case?").
- Decision/Change: Interface use-case-facing (`IOutboxWriter`, `IntegrationEvent`, `IIntegrationEventHandler<T>`) ở namespace `Messaging`; interface worker/adapter (`IEventBusPublisher`, `IOutboxDispatcher`, `IInboxStore`, `IIntegrationEventTypeRegistry`, `OutboxMessage`) ở `Messaging.Dispatch`.
- Rationale (verifiable): **Root cause:** biến ràng buộc "không dùng sai" từ *lời khuyên không enforce được* thành **luật NetArchTest** đơn giản: type implement `IUseCase*` không được phụ thuộc namespace `*.Messaging.Dispatch`.
- Alternatives: Giữ một namespace + dựa code review (loại: không enforce được, vi phạm I8 "interface tự chặn sai").
- Consequences: Hai namespace cho một chủ đề — chấp nhận để đổi lấy khả năng kiểm bằng máy.
- Reversibility: Medium.
- Traceability: F25/I8, CP11, design §5.2, task 3.2.

---

### AD-006 — `IIntegrationEventTypeRegistry` (ánh xạ EventType→CLR type); EventType lạ → dead-letter
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D4; `design.md` §5.2 interface `IIntegrationEventTypeRegistry` (verified grep); R17.3.
- Context: Bản cũ không có cơ chế phân giải `EventType` (string) → CLR type ⇒ consumer **không thể deserialize** payload; hành vi với type lạ không định nghĩa (nguy cơ crash loop).
- Decision/Change: Registry build lúc boot từ các assembly `*.Contracts`; consumer dùng nó để deserialize; `EventType` chưa đăng ký → đưa vào dead-letter (không crash).
- Rationale (verifiable): **Root cause:** không có registry thì Outbox/Inbox không vận hành được end-to-end (gap chức năng thật).
- Alternatives: Nhúng CLR type-name vào message (loại: adapter phải biết schema, coupling; vỡ khi refactor namespace).
- Consequences: Host phải khai các assembly Contracts để build registry (một dòng compose).
- Reversibility: Medium.
- Traceability: F25/F32, R17.3, design §5.2, task 7.3.

---

### AD-007 — Domain event in-process: `IDomainEventDispatcher`, dispatch TRƯỚC commit
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D8; `design.md` §5.3/§7.5 interface `IDomainEventDispatcher` (verified grep); R33, CP14.
- Context: Kernel `Entity` gom domain event (`RaiseDomainEvent`) nhưng **không ai dispatch** — tính năng cụt; F13 liệt kê domain events là trụ còn thiếu.
- Decision/Change: `PlatformDbContext.SaveChangesAsync` dispatch domain event TRƯỚC commit (cùng transaction), có vòng lặp + max-depth chặn đệ quy vô hạn.
- Rationale (verifiable): **Root cause:** feature nửa vời (raise nhưng không dispatch) là nợ kỹ thuật; dispatch-before-commit giữ side-effect nguyên tử với state (CP14).
- Alternatives: Dispatch sau commit (loại: mất nguyên tử, side-effect có thể chạy dù state rollback).
- Consequences: Handler chạy trong transaction → phải nhanh/không I/O ngoài; việc ra ngoài process đi qua Outbox.
- Reversibility: Medium.
- Traceability: F13/I3, R33, CP14, design §7.5, task 6.4.

---

### AD-008 — `JwtKeyRingOptions` làm điểm chung ký/verify (không phá F14)
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D10; `design.md` §5.7 (verified grep `JwtKeyRingOptions`).
- Context: Ký (key-ring, F22) ở Infrastructure; verify (JwtBearer) ở Api; nhưng Api KHÔNG được ref Infrastructure (F14) → thiết kế cũ không nói làm sao chia sẻ key material (cạm bẫy composition).
- Decision/Change: Cả hai đọc chung `JwtKeyRingOptions` (bind từ config: `ActiveKid`, `Keys[{kid,secret}]`, `Issuer`, `Audience`). Infra ký bằng active key (header `kid`); Api cấu hình `IssuerSigningKeyResolver` theo `kid` (verify active + previous). Không cần project reference.
- Rationale (verifiable): **Root cause:** chia sẻ qua *config/options* (thứ cả hai tầng đều được phép đọc) thay vì qua *project reference* (bị F14 cấm).
- Alternatives: Cho Api ref Infrastructure (loại: phá F14/CP2).
- Consequences: Key material quản qua config/secret governance (F35) — nhất quán với R25.
- Reversibility: Medium.
- Traceability: F14/F22, R27, design §5.7, task 5.3/9.2.

---

### AD-009 — Phân loại default cho từng port (degrade an toàn / fail-loud / chặn boot)
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D11; `design.md` §5.5; R16.4.
- Context: "no-op default" đồng loạt là NGUY HIỂM: no-op `IEmailSender` mất mail âm thầm; no-op `IDistributedLock` tái tạo race.
- Decision/Change: Phân loại per-port: (a) degrade an toàn (`NullAppCache` — miss-through); (b) fail-loud (`Throwing*` khi chưa cắm adapter); (c) không default + chặn boot cho port bảo mật (`IPasswordHasher`, `IHtmlSanitizer`).
- Rationale (verifiable): **Root cause:** "vắng adapter" có ngữ nghĩa khác nhau theo port — cache vắng thì bỏ qua được, nhưng mất mail/mất khóa/thiếu hasher là lỗi phải nổ.
- Alternatives: no-op đồng loạt (loại: mất dữ liệu/tái tạo race âm thầm); throw đồng loạt (loại: cache lẽ ra degrade được lại làm chết luồng).
- Consequences: Mỗi `AddXxxCore` phải khai đúng loại default + đóng góp `RequiredPortsValidator`.
- Reversibility: Medium.
- Traceability: F24, R16.4, design §5.5, task 13.

---

### AD-010 — Refresh-token: cơ chế ở lõi `Bedrock`, bảng ở schema module tiêu thụ
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D13 + phần Assumptions; `design.md` — port `IRefreshTokenStore` ở Application, `RefreshTokenRecord` ẩn ở Infrastructure, helper `AddRefreshTokens(schema)` (verified grep `IRefreshTokenStore`).
- Context: Căng thẳng F19 (ẩn persistence entity) vs "Identity là một module, không thuộc lõi". Nếu dồn hết vào Modules.Identity thì mất khả năng tái dùng store rotation nguyên tử (race-safe) cho module khác.
- Decision/Change: Cơ chế store nguyên tử (`IRefreshTokenStore` + `EfRefreshTokenStore` + `RefreshTokenRecord` ẩn) ở `Bedrock`; bảng do module tiêu thụ sở hữu trong schema của nó qua `AddRefreshTokens(schema)`. Use case login/refresh (nghiệp vụ) ở `Modules.Identity`.
- Rationale (verifiable): **Root cause:** tách *cơ chế tái dùng* (an toàn race, khó viết đúng) khỏi *nghiệp vụ* (policy role/permission) — cơ chế lên lõi, nghiệp vụ xuống module.
- Alternatives: Dồn tất cả vào Modules.Identity (loại: mất tái dùng store race-safe; phương án này "defensible" theo review nhưng đánh đổi tái dùng).
- Consequences: Lõi có một khái niệm hơi "gần nghiệp vụ" (refresh token) nhưng thuần cơ chế; bảng vẫn thuộc schema module (giữ F31 data ownership).
- Reversibility: Medium.
- Traceability: F19/F5/F10/F31, R28, R10, design §5.7, task 8.

---

### AD-011 — Startup validator qua `StartupValidationOptions.RequiredPorts` (mỗi `AddXxxCore` tự đóng góp)
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D5; `design.md` §9.4; R13.1.
- Context: Mẫu `RequiredPortsValidator` cũ (a) resolve scoped port từ **root provider** → với `ValidateScopes=true` (chính design bật) sẽ ném lỗi lúc boot dù cấu hình đúng; (b) hardcode danh sách port + dừng ở port thiếu đầu tiên.
- Decision/Change: Validator dùng `IServiceScopeFactory` (scope đúng), gom thông báo mọi port thiếu; danh sách port bắt buộc do mỗi `AddXxxCore()` đóng góp vào `StartupValidationOptions.RequiredPorts` (thêm capability không bao giờ phải sửa validator).
- Rationale (verifiable): **Root cause kép:** (1) lỗi vòng đời DI (scoped-from-root) là bug thật với `ValidateScopes=true`; (2) danh sách hardcode + ellipsis là không kiểm được và vi phạm open/closed.
- Alternatives: Danh sách hardcode (loại: sửa validator mỗi lần thêm capability; R4 chỉ ra ellipsis "untestable").
- Consequences: Fail-fast mọi môi trường (CP9), thông báo liệt kê đầy đủ port thiếu.
- Reversibility: Medium.
- Traceability: F7/F35/I9, R13.1, CP9, design §9.4, task 10.2.

---

### AD-012 — `IUnitOfWork.ExecuteInTransactionAsync` có ngữ nghĩa REENTRANCY (lời gọi lồng tham gia transaction hiện hành)
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D6; `design.md` §5.1 (comment REENTRANCY R7.4 trong khai báo `IUnitOfWork`, verified grep); `requirements.md` R7.4.
- Context: Transaction behavior (§8) BỌC command trong `ExecuteInTransactionAsync`, NHƯNG use case bên trong cũng có thể tự gọi `ExecuteInTransactionAsync` (vd rotation refresh + outbox).
- Decision/Change: Nếu đã có transaction do chính UoW này mở đang hoạt động → lời gọi lồng **THAM GIA** transaction hiện hành (không `BEGIN` lồng, không commit sớm).
- Rationale (verifiable): **Root cause:** không định nghĩa reentrancy thì hai lớp cùng mở transaction → `BEGIN` lồng, ngữ nghĩa không xác định và **fail trên Npgsql** (Npgsql không hỗ trợ nested BEGIN thật). Đây là bug thật, không phải tinh chỉnh phong cách.
- Alternatives: Cấm use case tự gọi `ExecuteInTransactionAsync` (loại: cứng nhắc, use case đôi khi cần transaction tường minh); dùng savepoint lồng (loại: phức tạp hoá, chưa cần).
- Consequences: Impl `EfUnitOfWork` phải "reentrancy-aware" (đếm độ sâu/kiểm tra ambient transaction). Test ở task 6.2/6.3 (transaction lồng không nổ).
- Reversibility: Medium.
- Traceability: I3/F5/F25, R7.4, design §5.1/§8, task 6.2.

---

### AD-013 — Thứ tự middleware pipeline HTTP cố định (§3.5, 11 slot)
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D13; `design.md` §3.5 bảng thứ tự (verified grep: slot #1 ForwardedHeaders … #9 RateLimiter … #10 AuthN/AuthZ … #11 Endpoints).
- Context: Bản cũ KHÔNG có thứ tự middleware, nhưng F16 (ForwardedHeaders phải chạy sớm để resolve IP thật) và F21 (correlation) đều **phụ thuộc thứ tự** → không định nghĩa = hành vi sai/không xác định.
- Decision/Change: Chốt thứ tự tường minh: ForwardedHeaders (#1) → Correlation → ExceptionHandler → SecurityHeaders → HSTS → HTTPS redirect → UseRouting (#7) → CORS (#8) → RateLimiter (#9, partition theo IP đã resolve ở #1) → AuthN→AuthZ (#10) → Endpoints (#11).
- Rationale (verifiable): **Root cause:** rate-limit partition theo IP CHỈ đúng nếu ForwardedHeaders resolve IP thật TRƯỚC; correlation phải bọc ngoài để mọi log/lỗi có cùng id. Thứ tự là điều kiện đúng đắn, không phải khẩu vị.
- Alternatives: Để Host tự sắp (loại: dễ sai thứ tự nhạy cảm bảo mật; base phải cung cấp `MapBedrockApi()` áp thứ tự chuẩn).
- Consequences: `MapBedrockApi()` áp thứ tự này; module chỉ thêm endpoint, không tự sắp middleware nền.
- Reversibility: Medium.
- Traceability: F16/F21/F17/F3, design §3.5, task 5.4.

---

### AD-014 — `IEndpointModule` làm hợp đồng discovery (thay "endpoint/module discovery" mơ hồ)
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `design.md` §6 + Components (`namespace Bedrock.Api.Endpoints; public interface IEndpointModule { void MapEndpoints(IEndpointRouteBuilder); }`, verified grep).
- Context: Blueprint nói "endpoint/module discovery" nhưng không có cơ chế cụ thể → dễ thành reflection-scan mờ ám hoặc magic string.
- Decision/Change: Mỗi `Modules.<M>.Api` khai một class implement `IEndpointModule`; `AddXModule` đăng ký instance; Host `MapBedrockApi()` resolve `IEnumerable<IEndpointModule>` và gọi `MapEndpoints`.
- Rationale (verifiable): **Root cause:** biến "thêm module = 1 dòng" từ khẩu hiệu thành **cơ chế cụ thể, testable, không reflection magic** — mỗi module tự khai endpoint + policy/versioning của nó.
- Alternatives: Reflection scan assembly (loại: mờ ám, khó test, dễ gom nhầm); map tay ở Host (loại: Host biết chi tiết module, phá tách bạch).
- Consequences: Contract `IEndpointModule` nằm ở `Bedrock.Api`; module Api phụ thuộc nó (đúng matrix).
- Reversibility: Medium.
- Traceability: F1/F13/F30, design §6, task 5.4/16.2.

---

### AD-015 — Hợp đồng serialization Outbox (System.Text.Json cố định; publish payload thô + headers)
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `design.md` §5.2 "Serialization contract (chốt để không mơ hồ)" (verified grep).
- Context: Không chốt serialization thì mỗi impl tự chọn → payload không tương thích giữa producer/consumer, và adapter có thể phải biết CLR type (coupling).
- Decision/Change: `IOutboxWriter` (Infrastructure) serialize bằng System.Text.Json với options CỐ ĐỊNH (camelCase, bỏ null), lưu `EventType`+`SchemaVersion` từ chính event, `correlation_id` từ `Activity.Current`. `IEventBusPublisher` publish **payload thô + headers** (không cần CLR type — adapter mỏng). Consumer dùng `IIntegrationEventTypeRegistry` (AD-006) để deserialize.
- Rationale (verifiable): **Root cause:** giữ adapter "mỏng" (không biết schema) + đảm bảo producer/consumer thống nhất định dạng → không lệch payload; options cố định để tránh drift.
- Alternatives: Để adapter serialize theo CLR type (loại: adapter phụ thuộc schema, vỡ khi refactor); mỗi impl tự chọn options (loại: drift, không tương thích).
- Consequences: Đổi options serialization là thay đổi có ảnh hưởng version event → gắn với versioning F32.
- Reversibility: Medium.
- Traceability: F25/F32, design §5.2, task 7.2/7.3.

---

### AD-016 — Dispatcher: atomic claim (skip-locked/lease) + exponential backoff
- Status: Confirmed (in design)
- Date: 2026-07-07
- Decider: AI(review)
- Provenance/Evidence: `review.md` D2; `design.md` §7.2 (thuật toán claim + `next_attempt_at ← now + backoff(error_count)` + vượt ngưỡng → `dead_lettered_at`, verified grep); `requirements.md` R8.5/R8.6; CP15.
- Context: Bản cũ `SELECT ... WHERE processed_at IS NULL LIMIT n` KHÔNG có claim → (a) hai host instance **publish trùng** + tranh `error_count`; (b) không có `next_attempt_at` → message poison **retry mỗi tick mãi mãi**.
- Decision/Change: Dispatcher CLAIM nguyên tử batch pending (row-lock skip-locked hoặc lease qua `next_attempt_at`); fail → tăng `error_count` + đặt `next_attempt_at` theo exponential backoff (+ jitter); vượt ngưỡng → `dead_lettered_at` (AD-003). Ghi provider-specific (skip-locked) chỉ ở Infrastructure.
- Rationale (verifiable): **Root cause kép:** (1) thiếu claim → double-publish khi scale nhiều instance (bug phân tán thật); (2) thiếu lịch retry → poison làm nghẽn. CP15 (exclusive claim) chỉ đúng khi có claim nguyên tử.
- Alternatives: LIMIT không claim (loại: double-publish); retry mỗi tick không backoff (loại: nghẽn poison + tải DB).
- Consequences: Cần index partial pending + test đa-connection (Testcontainers) chứng minh không claim trùng (task 7.4).
- Reversibility: Medium.
- Traceability: F25/F33/F5, R8.5/R8.6, CP15, design §7.2, task 7.3/7.4.

---

### AD-017 — Tách `IntegrationEvent` ra assembly trung tính `Bedrock.Messaging.Contracts` (supersedes DV-001)
- Status: Confirmed
- Date: 2026-07-07
- Decider: user (chốt "extract") trên đề xuất AI(review) + AI(Kiro)
- Provenance/Evidence: chat — AI chốt "2: extract", user "Bedrock.Messaging.Contracts; áp đi"; đã áp vào `design.md` §3.2 layout, §3.3 matrix + footnote¹, §4.5 namespace `IntegrationEvent`, components list (verified qua str_replace phiên này); `requirements.md` R17.2.
- Context: Trước đó (DV-001) `IntegrationEvent` ở `Bedrock.Application` → buộc `Modules.*.Contracts` (bề mặt DTO công khai) phải trỏ ngược lên tầng Application. Đây là điểm DUY NHẤT phá "Contracts = DTO thuần".
- Decision/Change: Tạo assembly **`Bedrock.Messaging.Contracts`** (zero-dependency) chứa DUY NHẤT base record `IntegrationEvent` (`Id`/`OccurredAt`/`EventType`/`SchemaVersion`). `Bedrock.Application` VÀ mọi `Modules.*.Contracts` cùng reference assembly này. Các port messaging (`IOutboxWriter`, `IIntegrationEventHandler<T>`) vẫn ở `Bedrock.Application.Messaging`; các type worker/adapter (`IEventBusPublisher`, `IOutboxDispatcher`, `IInboxStore`, `OutboxMessage`, `IIntegrationEventTypeRegistry`) vẫn ở `Bedrock.Application.Messaging.Dispatch`.
- Rationale (verifiable): **Root cause fix (không phải vá ngọn):** gốc của độ lệch DV-001 là base event bị đặt sai tầng (Application) trong khi nó là *hợp đồng chung* của cả producer (Application) lẫn consumer contract (module Contracts). Đặt nó vào một kernel trung tính zero-dep khôi phục invariant "Contracts thuần DTO, không trỏ lên Application" và giữ đồ thị phụ thuộc acyclic + tối thiểu.
- Alternatives: (a) đặt vào `Bedrock.Domain` (loại: trộn khái niệm messaging cross-boundary vào domain kernel thuần); (b) giữ ở Application như DV-001 (loại: phá Contracts thuần DTO — chính vấn đề cần sửa).
- Consequences: +1 project rất nhỏ. Mọi `*.Contracts` ref `Bedrock.Messaging.Contracts`; ArchTest luật #4/module-boundary cần biết assembly này là "được phép" cho Contracts.
- Reversibility: Medium (gộp lại vào Application là refactor có kiểm soát, nhưng không nên).
- Traceability: F25/F30/F32, R17.2, design §3.2/§3.3/§4.5, supersedes DV-001, resolves TO-005.

---

### AD-018 — Cho phép `Scrutor` + `Microsoft.Extensions.DependencyInjection/Logging.Abstractions` trong `Bedrock.Application` (chốt TO-004)
- Status: Confirmed
- Date: 2026-07-07
- Decider: user (chốt "allow") trên đề xuất AI
- Provenance/Evidence: chat — AI chốt "1: allow", user "áp đi"; `design.md` §17 dependency whitelist (đã có sẵn), TO-004.
- Context: Câu hỏi triết lý: Application (lõi) có được phụ thuộc Scrutor + DI/Logging abstractions không? (thuần Clean-Arch nói không).
- Decision/Change: **Cho phép** — coi chúng là *composition plumbing trung lập*, giữ `AddBedrockCore()` ở `Bedrock.Application`.
- Rationale (verifiable): **Bản chất:** `Microsoft.Extensions.DependencyInjection.Abstractions`/`Logging.Abstractions` là **hợp đồng chuẩn của .NET**, KHÔNG phải một implementation công nghệ có thể swap (khác EF/RabbitMQ/Redis). `Scrutor` chỉ chạm đăng ký DI (composition), không chạm logic domain/use-case. Do đó "allow" KHÔNG vi phạm I2 (lõi không chứa SDK công nghệ cụ thể) — thứ thật sự cần chặn. Phương án "move" bắt Host luôn kéo Infrastructure chỉ để đăng ký service Application → ma sát nhiều, lợi ích tinh khiết ~0 vì đây không phải tech để swap.
- Alternatives: "move" đăng ký xuống Infrastructure (loại: mất ergonomics Host, tách code đăng ký khỏi thứ nó đăng ký; lợi ích thuần lý thuyết).
- Consequences: `design.md` §17 whitelist rõ 3 dependency này cho Application; ArchTest "no concrete tech in core" phải whitelist chúng (không tính là vi phạm I2).
- Reversibility: Medium.
- Traceability: I2/F24, design §17, resolves TO-004.

---

### AD-019 — Suppress `CA1000` cho `Result<T>` factory (`Success`/`Failure` static trên generic type)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time), theo đúng quyết định thiết kế đã chốt AD-002
- Provenance/Evidence: build thật — `dotnet test Platform.slnx` báo lỗi CS/CA thật: `CA1000: Do not declare static members on generic types` tại `ResultT.cs:30,36` (verified qua execute_pwsh phiên này, trước khi sửa).
- Context: `Directory.Build.props` bật `AnalysisLevel=latest-Recommended` + `TreatWarningsAsErrors=true` (R31.1) → mọi warning là lỗi chặn build. `Result<T>.Success(value)`/`Failure(error)` là static factory trên generic type — đúng chủ đích theo design §4.4 (AD-002), nhưng analyzer mặc định coi đây là code smell (CA1000: khó gọi qua kiểu dẫn xuất với static member generic).
- Decision/Change: Thêm `dotnet_diagnostic.CA1000.severity = none` vào `.editorconfig` kèm comment giải thích rõ đây là factory chủ đích, không phải API tĩnh chung.
- Rationale (verifiable): **Bản chất (không phải vá ngọn):** đây là xung đột giữa MỘT quyết định thiết kế đã chốt có chủ đích (factory pattern cho Result — cách duy nhất tạo `Result<T>` hợp lệ mà vẫn suy luận được `T` từ tham số, không có constructor public) và MỘT analyzer rule chung. Xoá factory để né CA1000 sẽ phá hợp đồng design đã chốt (đổi API, mất khả năng suy luận kiểu) — đó mới là "sửa ngọn". Suppress có lý do rõ mới là sửa đúng gốc (chấp nhận rule không áp dụng cho case này).
- Alternatives: (a) xoá static factory, dùng constructor public (loại: phá invariant "Result luôn qua factory", lộ constructor cho misuse); (b) dời factory ra class `Result` non-generic gọi `Result<T>` qua reflection/generic method (loại: phức tạp hoá không cần thiết, `Result` (non-generic) đã có `Result.Success<T>()`/`Failure<T>()` làm lối vào thay thế — factory trên `Result<T>` vẫn cần cho nội bộ/implicit operator).
- Consequences: Rule CA1000 tắt toàn solution (không chỉ file này) — cần lưu ý nếu sau này có static member khác trên generic type thật sự là smell (sẽ không bị bắt tự động, phải tự review).
- Reversibility: High (chỉ là dòng .editorconfig).
- Traceability: AD-002, design §4.4, R31.1, N-005 (tiền lệ suppress-có-lý-do tương tự cho CA1711).

---

### AD-020 — Tách `CommonErrors.NotFound(entity)` và `CommonErrors.NotFoundGeneric(message)` thành 2 tên khác nhau
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time)
- Provenance/Evidence: build thật — `CS0111: Type 'CommonErrors' already defines a member called 'NotFound' with the same parameter types` (verified qua execute_pwsh phiên này, trước khi sửa).
- Context: Lúc viết `CommonErrors`, tôi tự thêm overload `NotFound(string entity)` (tiện ích lấy theo tên entity, code `{entity}.not_found`) NGOÀI overload `NotFound(string message = ...)` mà design §4.4 mô tả (code cố định `not_found`). Cả hai đều `(string)` → C# không phân biệt được theo tên tham số, xung đột overload thật (không phải lỗi lý thuyết).
- Decision/Change: Đổi tên overload chung thành `NotFoundGeneric(string message = ...)`; giữ `NotFound(string entity)` cho trường hợp gắn entity cụ thể.
- Rationale (verifiable): **Root cause:** đây là lỗi overload resolution do TÔI tự thêm một tiện ích (`NotFound(entity)`) không có trong design §4.4 gốc mà không đổi tên để tránh đụng. Sửa gốc = đặt lại tên rõ nghĩa cho cả hai (phân biệt bằng tên method, không dựa vào overload mơ hồ), KHÔNG xoá tiện ích (vẫn hữu ích, dùng trong test `CommonErrors.NotFound("Room")`).
- Alternatives: xoá overload `NotFound(entity)` (loại: mất tiện ích hợp lý, quay lại đúng-min design nhưng giảm ergonomics không cần thiết).
- Consequences: API `CommonErrors` có 2 tên cho khái niệm not-found — cần nhất quán khi viết docs/README tránh nhầm.
- Reversibility: High.
- Traceability: design §4.4 CommonErrors, task 2.

---

### AD-021 — `IHtmlSanitizer` đặt ở `Ports.Html` (hoà giải mâu thuẫn nội bộ design §5.7 vs §6.4)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time), hoà giải mâu thuẫn tài liệu
- Provenance/Evidence: grep design.md — 3 nơi ghi `Ports.Html`/`Html/IHtmlSanitizer` (bảng đổi tên folder §6.4 dòng 703; mục "Cần tạo" §10 dòng 965; `tasks.md` task 3.1) vs 1 nơi ghi `Ports.Security` (khối code §5.7 dòng 627-628). Đã sửa outlier §5.7 phiên này (verified str_replace).
- Context: Khi bắt đầu task 3.1, phát hiện design tự mâu thuẫn về namespace của `IHtmlSanitizer`: khối contract §5.7 gộp nó chung `Ports.Security` (cùng ITokenGenerator/IPasswordHasher), nhưng bảng đổi tên folder §6.4 (quyết định folder có thẩm quyền) + §10 + task 3.1 đều liệt kê `Html` là sub-folder riêng.
- Decision/Change: Đặt `IHtmlSanitizer` ở `Bedrock.Application.Ports.Html`. Sửa khối code §5.7 (outlier) để tách `IHtmlSanitizer` ra `namespace ...Ports.Html` riêng, giữ `Ports.Security` cho auth/crypto (token/password/jwt/refresh).
- Rationale (verifiable): **Root cause:** §5.7 gộp cho gọn khi trình bày (một khối `namespace Ports.Security` cho nhiều interface) → tạo mâu thuẫn với quyết định folder §6.4. Đa số (3 vs 1) + bảng folder có thẩm quyền nghiêng về `Html`. Về bản chất: HtmlSanitizer là "làm sạch nội dung chống XSS" — mối quan tâm KHÁC auth/crypto; tách folder riêng tránh biến `Security` thành nơi gom mọi thứ "bảo mật-ish". Sửa 1 chỗ outlier thay vì sửa 3 chỗ = tôn trọng nguồn có thẩm quyền hơn.
- Alternatives: gộp vào `Ports.Security` (loại: đi ngược bảng folder §6.4 + task; trộn 2 loại mối quan tâm). Để nguyên mâu thuẫn (loại: vi phạm "design là nguồn sự thật tự-nhất-quán").
- Consequences: `Ports.Html` chứa 1 interface (chấp nhận — mối quan tâm riêng biệt, có thể thêm sau vd sanitize markdown). §5.5 default table vẫn đúng (IHtmlSanitizer là port bảo mật bắt buộc, không có default).
- Reversibility: High (đổi namespace khi greenfield).
- Traceability: design §5.7/§6.4/§10, tasks 3.1, F12.

---

### AD-022 — Chia đôi enforcement CP1: tên/namespace (task 4, reflection+NetArchTest) vs string literal (task 20, cơ chế khác)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time), do giới hạn thật của công cụ
- Provenance/Evidence: CP1 (design §Correctness Properties) ghi "∀ type t trong Bedrock.*, **tên/namespace/chuỗi** của t KHÔNG chứa guest|room|resort|Admin|Staff". Kiểm chứng khả năng công cụ: NetArchTest + reflection chỉ đọc **metadata** (tên type/namespace/dependency), KHÔNG đọc được **string literal trong thân method** (vd hằng `"/api/guest/resolve/"` kiểu F2). Đây là giới hạn kỹ thuật thật, không phải lười.
- Context: Task 4 dựng no-business-in-core. CP1 nói cả "chuỗi" (string literal). Nhưng reflection/NetArchTest không thấy IL string constants trong method body.
- Decision/Change: Chia CP1 thành 2 lớp enforcement: (a) **tên type + namespace** → phủ NGAY ở task 4 bằng reflection scanner (+ negative control chứng minh bắt được); (b) **string literal trong code** → phủ ở **task 20** ("Contract tests + no-business-in-core cuối cùng") bằng cơ chế khác (source-text scan các file .cs dưới src/Bedrock.*, hoặc IL/Roslyn) — vì lúc đó toàn bộ lõi đã có, quét một lần cuối hợp lý hơn.
- Rationale (verifiable): **Bản chất:** một công cụ (NetArchTest metadata) KHÔNG đủ phủ toàn bộ CP1; cố nhét literal-scan vào NetArchTest là "vá ngọn" (nó không làm được). Sửa đúng = nhận diện giới hạn, phủ phần làm được ngay + lên lịch phần còn lại bằng đúng công cụ, và NÓI RÕ giới hạn trong test (không tuyên bố phủ literal khi chưa).
- Alternatives: (a) thêm Mono.Cecil/Roslyn ngay task 4 để quét literal (loại: kéo dependency nặng vào bước sớm, task 20 làm gọn hơn khi lõi đủ); (b) im lặng coi như đã phủ literal (loại: BỊA độ phủ — vi phạm nguyên tắc).
- Consequences: Đến hết task 4, CP1 mới phủ phần tên/namespace; phần literal còn hở tới task 20 — đã ghi rõ trong docstring test `NoBusinessInCoreTests` và N-021 để không ai hiểu nhầm.
- Reversibility: High.
- Traceability: CP1, design §Correctness Properties, R1.1/R1.2, task 4 + task 20.

---

### AD-023 — JWT: `MapInboundClaims=false` + claim names JWT-native (`sub`/`role`/`permission`/`tenant_id`/`sid`)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time), spec không chỉ định chiến lược claim mapping/tên claim
- Provenance/Evidence: `BedrockAuthExtensions.AddBedrockAuthCore` (MapInboundClaims=false, NameClaimType="sub", RoleClaimType="role") + `HttpContextCurrentUser` đọc claim `sub`/`role`/`permission`/`tenant_id`/`sid`; verified `dotnet test` (401/403/valid-token qua TestHost với token ký thật xanh). design §5.7 chỉ mô tả `ICurrentUser` shape (F23) + key-ring, KHÔNG chỉ định tên claim hay behavior mapping.
- Context: JwtBearer MẶC ĐỊNH map claim inbound sang URI dài của .NET (vd `sub`→`ClaimTypes.NameIdentifier` `http://schemas.xmlsoap.org/...`) — di sản WS-*. Cần chốt tên claim để `HttpContextCurrentUser` đọc nhất quán với token do `IJwtTokenService` (task 9) phát.
- Decision/Change: Tắt map (`MapInboundClaims=false`) → giữ claim JWT-native. Quy ước tên claim của base: `sub`=UserId, `role`=roles (multi), `permission`=permissions (multi), `tenant_id`=TenantId, `sid`=SessionId. `TokenValidationParameters.NameClaimType="sub"`, `RoleClaimType="role"`.
- Rationale (verifiable): **Bản chất:** map mặc định là hành vi legacy gây tên claim dài, khó đọc, khó khớp giữa nơi phát và nơi đọc. Giữ JWT-native (chuẩn OIDC/JWT hiện đại) → `HttpContextCurrentUser` đọc trực tiếp tên chuẩn, dev đọc token thấy đúng tên. Tránh phụ thuộc hằng URI WS-*.
- Alternatives: để mặc định (loại: tên claim URI dài, phải map ngược, dễ lệch giữa issue/read); dùng ClaimTypes.* URI (loại: verbose, không chuẩn JWT).
- Consequences (RÀNG BUỘC cross-cutting): `IJwtTokenService` (task 9) + mọi nơi phát token PHẢI emit đúng tên claim này (`sub`/`role`/`permission`/`tenant_id`/`sid`). External-auth (task 12.3) map profile về các claim này. Ghi để module Identity (task 16) tuân thủ.
- Reversibility: Medium (đổi tên claim = breaking cho token đang lưu hành).
- Traceability: F23/F22, design §5.7, R26.2/R27.2, task 5.3 (+ ràng buộc task 9/12.3/16).

---

### AD-024 — Request-logging đã-mask của Bedrock THAY THẾ framework logging; Host phải hạ `Microsoft.AspNetCore` xuống Warning (đóng F15 tận gốc)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time) — phát hiện qua test CP13 FAIL
- Provenance/Evidence: test `BedrockPipelineTests.Sensitive_path_must_be_masked_in_logs` FAIL lần đầu vì log chứa `SUPERSECRETTOKEN` (verified). Điều tra: `PathMasker` của ta hoạt động ("/r/***" có mặt) NHƯNG `Microsoft.AspNetCore.Hosting.Diagnostics` tự log "Request starting ... /r/SUPERSECRETTOKEN" ở Information. Fix bằng `AddFilter("Microsoft.AspNetCore", LogLevel.Warning)` → test xanh (verified).
- Context: F15/CP13 = "mọi nơi log path đi qua masker". Nhưng masker của ta chỉ áp cho log site CỦA TA (RequestLogging + ExceptionHandler). Framework ASP.NET tự gọi ILogger với raw path — ta KHÔNG chặn được bằng masker (nó là site do framework sở hữu).
- Decision/Change: (1) `RequestLoggingMiddleware` (đã-mask) của Bedrock là NGUỒN request-log chính; (2) Host PHẢI hạ logging category `Microsoft.AspNetCore` (tối thiểu `...Hosting`) xuống **Warning** để framework KHÔNG emit raw path ở Information → đóng hẳn kênh leak. CP13 guarantee: masker phủ site của Bedrock; kênh framework đóng bằng logging posture này.
- Rationale (verifiable): **Root cause thật:** không có cách nào để masker của ta chặn lời gọi ILogger nội bộ của framework (khác process/khác site). Cách duy nhất đúng bản chất là cấu hình logging level cho category framework — và đó là lãnh địa Host. Vá ngọn (chỉ mask site của ta) sẽ để lại leak thật qua Hosting diagnostics.
- Alternatives: (a) chỉ mask site của ta, bỏ qua framework (loại: leak thật còn nguyên — vi phạm F15); (b) viết middleware ghi đè/nuốt framework log (loại: hack mong manh, không đáng tin).
- Consequences (RÀNG BUỘC Host — task 16.2): Host composition PHẢI đặt filter `Microsoft.AspNetCore` = Warning (hoặc dùng helper logging của base — ứng viên task 18 telemetry). Đây là posture bắt buộc, không optional. Khó guard tự động ở library (logging config thuộc Host) → ghi N-026 làm ràng buộc + guard end-to-end đã có trong `BedrockPipelineTests` (chứng minh WITH posture đúng thì 0 leak).
- Reversibility: Medium (đổi posture logging).
- Traceability: F15/CP13, design §3.5/Error Handling, task 5.4 (+ ràng buộc task 16.2/18).

---

### AD-025 — `DomainEventDispatcher` gọi handler qua generic invoker (KHÔNG `MethodInfo.Invoke`)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time) — phát hiện qua test CP14 FAIL
- Provenance/Evidence: test `DomainEventDispatchTests.Handler_throwing_rolls_back_state_and_effects` FAIL lần đầu: `Assert.Throws<InvalidOperationException>` nhận `TargetInvocationException` (verified execute_pwsh). Sau khi đổi sang generic invoker → 14/14 test Infrastructure xanh (verified).
- Context: Dispatcher phải resolve `IDomainEventHandler<TEvent>` theo kiểu THỰC của event (chỉ biết lúc runtime). Bản đầu dùng `MethodInfo.Invoke(handler, [event, ct])`.
- Decision/Change: Thay reflection-invoke bằng cặp `HandlerInvoker` (abstract) + `HandlerInvoker<TEvent>` (sealed) cache theo kiểu event; `InvokeAsync` ép kiểu `(IDomainEventHandler<TEvent>)handler` và gọi `HandleAsync` TRỰC TIẾP.
- Rationale (verifiable): **Root cause (không vá ngọn):** `MethodInfo.Invoke` bọc exception đồng bộ của handler vào `TargetInvocationException`, làm MẤT kiểu lỗi gốc → tầng trên (ExceptionHandling middleware, mapping 409...) không nhận diện được lỗi thật của handler. Vá ngọn = catch+unwrap `TargetInvocationException` (còn sót gốc: vẫn dùng reflection-invoke, còn chi phí + còn nguy cơ bọc ở đường khác). Sửa gốc = KHÔNG dùng invoke-reflection: gọi phương thức qua virtual call typed → exception propagate nguyên kiểu, và nhanh hơn (dựng invoker 1 lần/kiểu, không reflect mỗi lần).
- Alternatives: (a) catch `TargetInvocationException` rồi `ExceptionDispatchInfo.Throw(inner)` (loại: vá ngọn, vẫn reflection mỗi lần); (b) `dynamic` dispatch (loại: chi phí DLR + che kiểu lỗi tương tự).
- Consequences: Thêm 2 nested private class nhỏ; hành vi lỗi handler nay trong suốt cho tầng trên (đúng CP14/R33.2).
- Reversibility: High (nội bộ dispatcher).
- Traceability: AD-007, R33.2, CP14, design §7.5, task 6.4.

---

### AD-026 — `PlatformDbContext.SaveChanges` (đồng bộ) bị chặn tường minh (`NotSupportedException`)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time), spec không nói về đường ghi đồng bộ
- Provenance/Evidence: `PlatformDbContext.SaveChanges(bool)` override ném `NotSupportedException`; test `TransactionTests.SaveChanges_sync_is_blocked_to_avoid_skipping_dispatch` xanh (verified).
- Context: Dispatch domain-event (R33) là BẤT ĐỒNG BỘ (handler trả `Task`). EF có hai điểm ghi: `SaveChangesAsync(bool,ct)` (đã override để dispatch) và `SaveChanges(bool)` (đồng bộ). Nếu để `SaveChanges` đồng bộ đi thẳng `base` thì domain-event bị BỎ QUA âm thầm.
- Decision/Change: Override `SaveChanges(bool)` ném `NotSupportedException` với thông điệp hướng dùng `SaveChangesAsync`.
- Rationale (verifiable): **Bản chất:** đường ghi đồng bộ không thể chạy dispatch async an toàn (block `.GetAwaiter().GetResult()` có nguy cơ deadlock). Fail-loud (chặn tường minh) tốt hơn silent-drop event — nhất quán triết lý AD-009 (fail-loud thay vì mất dữ liệu âm thầm).
- Alternatives: (a) để `SaveChanges` đồng bộ chạy base không dispatch (loại: mất event âm thầm — bug thật); (b) block chạy dispatch async trong sync (loại: nguy cơ deadlock, sai kiến trúc).
- Consequences: App/test PHẢI dùng `SaveChangesAsync`. UoW của base vốn chỉ dùng async → không ảnh hưởng đường chính.
- Reversibility: High.
- Traceability: AD-007/AD-009, R33, design §7.5, task 6.1/6.4.

---

### AD-027 — `MaxDomainEventDispatchDepth` = 25 (virtual), vượt trần → NÉM lỗi (hoà giải BREAK-vs-throw của design §7.5)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time), hoà giải mâu thuẫn nội bộ design §7.5
- Provenance/Evidence: design §7.5 pseudocode ghi `IF events rỗng OR depth >= MaxDispatchDepth: BREAK` NHƯNG postcondition cùng mục ghi "Vượt MaxDispatchDepth → ném lỗi rõ ràng (không silent-drop event)"; `tasks.md` 6.4 ghi "max-depth ném lỗi rõ". Test `Exceeding_max_dispatch_depth_throws_clear_error` (đặt trần 3) xanh (verified).
- Context: Pseudocode §7.5 (BREAK) MÂU THUẪN với postcondition + tasks 6.4 (throw). Ngoài ra design chỉ nói "MaxDispatchDepth" không cho con số.
- Decision/Change: Vòng lặp `while`: hết event → return bình thường; CÒN event mà `depth >= trần` → ném `InvalidOperationException` rõ ràng (không BREAK âm thầm). Trần mặc định = **25**, khai `protected virtual` để test/app override.
- Rationale (verifiable): **Root cause:** postcondition + acceptance (tasks 6.4) là thẩm quyền về HÀNH VI cần đạt (no silent-drop) — pseudocode BREAK là mô tả rút gọn dễ hiểu nhầm. Chọn throw thoả cả hai yêu cầu có thẩm quyền. Con số 25 đủ lớn cho chuỗi event hợp lệ sâu bất thường, đủ nhỏ để chặn vòng vô hạn sớm; `virtual` để test biên rẻ (đặt 3) và app điều chỉnh nếu cần.
- Alternatives: (a) BREAK theo pseudocode (loại: silent-drop event — vi phạm postcondition + tasks 6.4); (b) hằng cứng không override (loại: khó test biên, kém linh hoạt).
- Consequences: design §7.5 pseudocode nên được ghi chú "BREAK" thực chất là "throw khi còn event" — đã phản ánh ở đây (journal) làm nguồn hoà giải; nếu sửa design sau này, đồng bộ theo mục này.
- Reversibility: High (đổi số/hành vi nội bộ).
- Traceability: R33.4, CP14, design §7.5 (postcondition), task 6.4.

---

### AD-028 — Pin transitive `SQLitePCLRaw.lib.e_sqlite3` = 3.50.3 (vá CVE-2025-6965 / GHSA-2m69-gcr7-jv3q)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time) — phát hiện qua restore FAIL
- Provenance/Evidence: `dotnet add Microsoft.EntityFrameworkCore.Sqlite` (10.0.9) → restore báo `NU1903 Warning As Error: SQLitePCLRaw.lib.e_sqlite3 2.1.11 has a known high severity vulnerability` (verified execute_pwsh). Web search xác nhận CVE-2025-6965: SQLite < 3.50.2, khuyến nghị nâng 3.50.x. Sau khi pin 3.50.3 → `dotnet restore` sạch, 14/14 test SQLite xanh (verified).
- Context: `Directory.Build.props` bật NuGetAudit + `TreatWarningsAsErrors=true` (R31) → advisory transitive thành lỗi chặn restore. EF Sqlite 10.0.9 kéo `SQLitePCLRaw.bundle_e_sqlite3` → `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (SQLite < 3.50.2, lỗ hổng HIGH).
- Decision/Change: Với `CentralPackageTransitivePinningEnabled=true`, thêm `<PackageVersion Include="SQLitePCLRaw.lib.e_sqlite3" Version="3.50.3" />` (ItemGroup "Security transitive pins") → nâng native SQLite lên bản đã vá.
- Rationale (verifiable): **Root cause:** lỗ hổng nằm ở SQLite native bundle transitive, KHÔNG ở gói EF trực tiếp → không thể sửa bằng đổi version EF; phải pin đúng transitive package. Đây là cùng cách foundation cũ đã verify (DEC-051, 3.50.3, 110 test xanh). Chỉ ảnh hưởng đường TEST provider-agnostic (SQLite), không vào runtime production (Npgsql).
- Alternatives: (a) tắt NuGetAudit / hạ TreatWarningsAsErrors (loại: mù rủi ro bảo mật — phản mục tiêu commercial-grade); (b) bỏ test SQLite (loại: mất lưới test provider-agnostic không-Docker).
- Consequences: Khi nâng EF Sqlite sau này, kiểm lại pin còn cần không (nếu bundle mới đã ≥ 3.50.2 thì gỡ pin). Ghi rõ ở comment trong `Directory.Packages.props`.
- Reversibility: High (một dòng pin).
- Traceability: R31 (0 warning gate), persistence-layer-design §6 (chiến lược SQLite test), tiền lệ foundation DEC-051, task 6.3.

---

### AD-029 — `OutboxMessage.Id = IntegrationEvent.Id` (id event chảy xuyên suốt outbox → bus → inbox)
- Status: Confirmed
- Date: 2026-07-08
- Decider: AI (implementation-time), suy ra từ hợp đồng idempotency của design
- Provenance/Evidence: `EfOutboxWriter.EnqueueAsync` đặt `Id = integrationEvent.Id`; test `Enqueue_serializes_payload_and_copies_event_fields` khẳng định `message.Id == eventId` (verified). `IntegrationEvent.Id` doc: "Định danh event (dùng cho idempotency phía consumer — Inbox)"; `inbox_message` PK `(message_id, consumer)`.
- Context: `OutboxMessage.Id` mặc định `Guid.CreateVersion7()` (id riêng của row outbox). Consumer khử trùng qua `IInboxStore.TryMarkProcessedAsync(messageId, consumer)`. Cần chốt `messageId` mà consumer dùng là gì để idempotency đầu-cuối đúng.
- Decision/Change: Writer đặt `OutboxMessage.Id = IntegrationEvent.Id` (KHÔNG dùng id ngẫu nhiên mới) → id event là khoá xuyên suốt: outbox.id → message publish → consumer messageId → inbox.message_id.
- Rationale (verifiable): **Bản chất:** design nói `IntegrationEvent.Id` "dùng cho idempotency phía consumer". Idempotency chỉ đúng nếu id đó CHẢY tới inbox. Nếu outbox sinh id riêng thì inbox dedup theo id outbox, không theo id event → hai lần enqueue cùng một event (retry nghiệp vụ) sẽ có id khác nhau, consumer xử lý trùng. Đặt `outbox.Id = event.Id` khôi phục đúng ngữ nghĩa "đúng-một-lần về nghiệp vụ" (design §4.5).
- Alternatives: (a) id outbox ngẫu nhiên riêng (loại: phá idempotency đầu-cuối như trên); (b) thêm cột `event_id` riêng bên cạnh `id` (loại: dư thừa — id event đã đủ làm PK; đơn giản hơn là dùng luôn).
- Consequences (RÀNG BUỘC task 7.3/consumer): `IEventBusPublisher` publish message mang `Id` này; consumer PHẢI dùng `message.Id` làm `messageId` khi gọi `IInboxStore`. Enqueue cùng một `IntegrationEvent.Id` hai lần → PK conflict ở outbox (đúng: một event = một row).
- Reversibility: Medium (đổi = ảnh hưởng khoá idempotency).
- Traceability: F25/F30, design §4.5/§7.3, R8/R9, task 7.2 (+ ràng buộc 7.3).


---

### AD-030 — Cổng journal-consistency tự động (`JournalConsistencyTests`) — biến kỷ luật tài liệu thành build gate
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) trên yêu cầu user ("cần 1 cách nào cực mạnh để tránh drift")
- Provenance/Evidence: file `tests/Bedrock.ArchitectureTests/JournalConsistencyTests.cs` (tạo phiên này); phát hiện thật khi audit: **AD-003 thiếu trong bảng guard `05-anti-drift.md`** (KEYSTONE RULE bị vi phạm âm thầm vì không có cổng máy) — chính test này bắt được, verified qua `dotnet test` phiên này (đỏ trước khi bổ sung AD-003, xanh sau).
- Context: Anti-drift L1–L3 (guard test qua `dotnet test`) mạnh cho CODE, nhưng lớp L4/L5 (đồng bộ journal, ID không dangling, KEYSTONE RULE "mỗi AD phải có guard") là **quy trình THỦ CÔNG** → dựa trí nhớ, đúng thứ anti-drift muốn loại. Journal có thể tự lệch mà không gì báo (đã xảy ra: AD-003).
- Decision/Change: Thêm test `JournalConsistencyTests` (trong `Bedrock.ArchitectureTests` — test dev-time, KHÔNG ship theo product) parse markdown journal và enforce 5 bất biến: INV-1 (ID mỗi loại duy nhất + liên tục 1..N), INV-2 (mọi AD trong 01 phải có trong bảng guard 05 — KEYSTONE tự động), INV-3 (mọi ref AD/DV/TO/N trỏ tới bản ghi thật), INV-4 (mỗi AD & DV có `Status:` + `Provenance/Evidence:`), INV-5 (mọi CP## trong 1..15). Không tìm thấy journal → `Assert.Fail` (KHÔNG skip — xem Rationale).
- Ghi chú thực thi: ban đầu định dùng `Assert.Skip` nhưng API đó KHÔNG có trong xUnit v2 (2.9.3) — chỉ có ở xUnit v3. Đổi sang `Assert.Fail`, và đây LẠI là lựa chọn ĐÚNG BẢN CHẤT hơn: cổng anti-drift không được tự tắt âm thầm khi không tìm thấy journal (skip = cổng vô hiệu ngầm = drift ẩn). Test project là dev-time nên luôn chạy trong repo có `.kiro`.
- Rationale (verifiable): **Bản chất (sửa gốc, không vá ngọn):** gốc của drift tài liệu là "thiếu cổng tự động" — không phải "thiếu nhắc nhở". Thêm nhắc nhở/checklist là vá ngọn (vẫn dựa người). Thêm test chạy mỗi build biến L4 thành enforcement cùng hạng với code (triết lý "con người quên, build thì không" — nay áp cho tài liệu). Test tự gác chính nó (AD-030 phải nằm trong bảng guard 05 theo INV-2) → self-reinforcing.
- Alternatives: (a) script PowerShell chạy tay (loại: không tự động = vẫn dựa người, yếu); (b) Kiro hook nhắc chạy loop (loại: nhắc ≠ chặn; bổ trợ được nhưng không thay cổng); (c) không làm gì, giữ thủ công (loại: đã chứng minh lệch thật — AD-003).
- Consequences: `Bedrock.ArchitectureTests` nay phụ thuộc layout repo (journal ở `.kiro` ngoài `platform/`) — chấp nhận vì test không ship; discovery đi lên cây thư mục tìm `.kiro/specs/platform-base/journal`, không thấy thì skip. Mọi lần thêm AD/DV/TO/N phải giữ liên tục ID + (với AD) khai guard trong 05, nếu không build đỏ.
- Reversibility: High (xoá 1 file test).
- Traceability: KEYSTONE RULE (05-anti-drift.md), N-007 (quality gate), R31 (0 warning + test xanh), N-030/N-031.


---

### AD-031 — Đổi `IRefreshTokenStore.GetActiveByHashAsync` → `GetByHashAsync` (trả theo hash BẤT KỂ revoked/expiry) — hoà giải §5.7 vs §7.4
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — phát hiện mâu thuẫn nội bộ design khi bắt đầu task 8
- Provenance/Evidence: đọc `design.md` §5.7 (port doc "trả token còn hiệu lực, null nếu không active") vs §7.4 (rotation: `IF current.RevokedAt IS NOT NULL → RevokeFamily`, tức lookup PHẢI trả record đã revoked); `IRefreshTokenStore.cs` — `RefreshTokenSnapshot` có field `RevokedAt` (vô nghĩa nếu chỉ trả active); `foundation/.../EfRefreshTokenStore.cs` dùng `FindByHashAsync` (trả theo hash, không lọc active) — bản đã chạy; `persistence-layer-design.md` §4. Đã sửa design §5.7+§7.4 + port + verified `dotnet test` (RefreshTokenStoreTests xanh).
- Context: Port §5.7 đặt tên `GetActiveByHashAsync` + doc "active-only", nhưng thuật toán reuse-detection §7.4 kiểm `current.RevokedAt`/`current.ExpiresAt` SAU lookup → lookup buộc phải trả cả token đã revoked/hết hạn. Nếu lọc active-only thì token đánh cắp (đã revoked) khi bị dùng lại sẽ trả `null` → KHÔNG kích hoạt RevokeFamily → **mất tính năng bảo mật reuse-detection**. Đây là mâu thuẫn nội bộ design (tên/doc §5.7 ↔ hành vi §7.4).
- Decision/Change: Đổi tên `GetActiveByHashAsync` → `GetByHashAsync`; ngữ nghĩa = trả record theo hash BẤT KỂ revoked/expiry; snapshot mang `RevokedAt`/`ExpiresAt` để use case (task 16) tự quyết. Đồng bộ `design.md` §5.7 + §7.4 + port XML-doc.
- Rationale (verifiable): **Bản chất (không vá ngọn):** gốc của lỗi là TÊN method mô tả sai hành vi bắt buộc → nếu chỉ sửa doc mà giữ tên "Active" thì để lại bẫy misuse (vi phạm I8 "interface tự chặn sai" — dev tưởng nó lọc active). Đổi tên = sửa gốc. Ba nguồn độc lập xác nhận ngữ nghĩa by-hash (snapshot.RevokedAt, §7.4, foundation FindByHashAsync) → không phải suy đoán.
- Alternatives: (a) giữ tên + chỉ sửa doc (loại: vá ngọn, tên vẫn gây hiểu nhầm — I8); (b) tách 2 method `GetActive` + `GetAnyByHash` (loại: dư thừa, use case luôn cần bản "any" để reuse-detect; `GetActive` không ai dùng).
- Consequences: use case rotation (task 16 Identity) gọi `GetByHashAsync`. Guard test `RefreshTokenStoreTests.GetByHash_returns_revoked_token_for_reuse_detection` khoá ngữ nghĩa này (revoked vẫn trả về).
- Reversibility: High (greenfield, chưa có consumer — chỉ interface declaration).
- Traceability: F5/F10/F19, design §5.7/§7.4, DV-011, task 8.1/8.2, I8.


---

### AD-032 — Ký JWT bằng `JsonWebTokenHandler` (Microsoft.IdentityModel.JsonWebTokens), KHÔNG dùng legacy `JwtSecurityTokenHandler`
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — spec §5.7 không chỉ định thư viện/handler ký JWT
- Provenance/Evidence: `JwtTokenService.cs` dùng `JsonWebTokenHandler.CreateToken(SecurityTokenDescriptor)`; pin `Microsoft.IdentityModel.JsonWebTokens` **8.0.1** (= version JwtBearer 10.0.9 kéo transitive — verified trong `Bedrock.Api/obj/project.assets.json`); test `JwtTokenServiceTests` xanh (kid header = ActiveKid, claim `sub`/`role` giữ nguyên tên, rotation verify).
- Context: Cần chọn handler ký JWT ở Infrastructure. Hai lựa chọn: (a) `System.IdentityModel.Tokens.Jwt` / `JwtSecurityTokenHandler` (foundation cũ dùng, 8.19.1); (b) `Microsoft.IdentityModel.JsonWebTokens` / `JsonWebTokenHandler` (bản mới Microsoft khuyến nghị).
- Decision/Change: Dùng `JsonWebTokenHandler`.
- Rationale (verifiable): **Bản chất:** `JwtSecurityTokenHandler` có **`DefaultOutboundClaimTypeMap` TĨNH (process-global, mutable)** — có thể remap tên claim khi KÝ (di sản WS-*). Điều này đe doạ trực tiếp AD-023 (yêu cầu tên claim JWT-native `sub`/`role`/`permission`/`tenant_id`/`sid` giữ NGUYÊN). `JsonWebTokenHandler` KHÔNG có map tĩnh đó → ghi claim as-is, không phụ thuộc global state, lại nhanh hơn và là hướng Microsoft khuyến nghị. Chọn (b) là loại bỏ nguồn lỗi tận gốc thay vì phải nhớ `DefaultOutboundClaimTypeMap.Clear()` (footgun dễ quên — vá ngọn).
- Alternatives: `JwtSecurityTokenHandler` (loại: static mutable map là footgun global; dù hoạt động nếu claim đã short-name, vẫn phụ thuộc trạng thái tĩnh dễ bị nơi khác đổi).
- Consequences: Infrastructure ref `Microsoft.IdentityModel.JsonWebTokens 8.0.1`; verify side (Api JwtBearer) đã tương thích (cùng chuẩn JWT). Ghim đúng 8.0.1 tránh xung đột version với JwtBearer.
- Reversibility: Medium (đổi handler nội bộ JwtTokenService).
- Traceability: AD-023 (tên claim), F22, design §5.7, R27, task 9.2.


---

### AD-033 — Marker-scan DI tự hiện thực bằng reflection, KHÔNG dùng Scrutor (dù AD-018 cho phép)
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — spec §6.2/§Components gợi ý Scrutor; đây là lựa chọn triển khai
- Provenance/Evidence: `BedrockRegistrationExtensions.AddBedrockConventions` dùng `assembly.GetTypes()` + `ServiceDescriptor` thuần; Application csproj chỉ thêm `Microsoft.Extensions.DependencyInjection.Abstractions` (KHÔNG Scrutor); test `RegistrationConventionTests` (6) xanh.
- Context: Task 10.1 cần auto-scan class theo marker (IScopedService/…) đăng ký theo lifetime. AD-018 đã "cho phép" Scrutor như plumbing. Nhưng logic scan (lọc marker + interface nghiệp vụ + loại System.* + self-fallback + loại IManualRegistration) đủ đơn giản để tự viết + test.
- Decision/Change: Tự hiện thực marker-scan bằng reflection thuần; KHÔNG thêm Scrutor vào lõi Application.
- Rationale (verifiable): **Bản chất (hướng lâu dài, sản phẩm thương mại):** (1) giảm dependency bên-thứ-ba trong LÕI Application — mỗi dep ngoài là rủi ro bảo trì/version/CVE dài hạn; (2) tránh coupling version Scrutor với .NET 10 (Scrutor phải theo kịp DI abstractions); (3) logic ~50 dòng, tự sở hữu + test kỹ (negative-control-ish: manual-exclusion, self-fallback, lifetime đúng, duplicate-guard). AD-018 "CHO PHÉP" chứ không "BẮT BUỘC" → không dùng KHÔNG vi phạm; Scrutor vẫn được phép nếu sau này cần decorator/scan phức tạp.
- Alternatives: Scrutor (loại hiện tại: thêm dep ngoài cho việc tự làm được; giữ lại như tuỳ chọn tương lai nếu cần `Decorate`/scan nâng cao).
- Consequences: Application chỉ phụ thuộc `Microsoft.Extensions.DependencyInjection.Abstractions` (abstraction chuẩn .NET, đúng lớp AD-018 whitelist) + FluentValidation. Nếu sau cần Scrutor thật (vd decorator pipeline task 15) thì mở lại — không mâu thuẫn.
- Reversibility: High (đổi sang Scrutor là thay 1 method, không đụng call-site).
- Traceability: F6/F18, AD-018 (không mâu thuẫn), design §6.2/§6.3, task 10.1.


---

### AD-034 — Rate-limit 429 là mối quan tâm TẦNG EDGE (ghi problem+json trực tiếp), KHÔNG thêm vào `ErrorType`
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — spec không nói 429 map thế nào; platform `ErrorType` (§4.4) cố ý 6 loại không có RateLimited
- Provenance/Evidence: `Bedrock.Domain/Results/ErrorType` = {Validation,NotFound,Conflict,Unauthorized,Forbidden,Failure} (verified task 2); `BedrockHttpSecurityExtensions.ConfigureRateLimiter.OnRejected` ghi `{"...","status":429,"code":"rate_limited"}` trực tiếp; test rate-limit 429 xanh.
- Context: RateLimiter middleware từ chối request vượt ngưỡng → cần trả 429. `ErrorTypeToHttp` (task 5.1) chỉ map 6 ErrorType (không có 429). Câu hỏi: thêm `RateLimited` vào ErrorType, hay ghi 429 riêng?
- Decision/Change: KHÔNG thêm `RateLimited` vào `ErrorType`. RateLimiter middleware ghi 429 problem+json trực tiếp (title/status/code + Retry-After header).
- Rationale (verifiable): **Bản chất kiến trúc:** rate-limit xảy ra ở TẦNG EDGE/transport (middleware chặn TRƯỚC khi vào use case) — KHÔNG phải một domain-result error mà use case trả về. `ErrorType`/`Result` là hợp đồng LỖI NGHIỆP VỤ của use case (§4.4). Nhét RateLimited vào đó = trộn mối quan tâm transport vào domain kernel (làm bẩn type dùng nhiều nhất, mở rộng enum core cho một thứ không phải domain). Ghi 429 tại middleware là đặt trách nhiệm đúng tầng. Vì vậy KHÔNG mở rộng ErrorType là "sửa đúng gốc" (giữ ranh giới domain/edge), không phải né việc.
- Alternatives: (a) thêm `ErrorType.RateLimited` + map 429 (loại: rò mối quan tâm edge vào domain kernel; use case không bao giờ trả RateLimited); (b) trả 500 (loại: sai HTTP semantic).
- Consequences: 429 body do middleware tự ghi (không qua `ProblemDetailsBuilder`/Error). Nếu sau cần thống nhất tuyệt đối format problem+json, tách một `EdgeProblem` writer dùng chung (chưa cần).
- Reversibility: High (đổi cách ghi 429 nội bộ middleware).
- Traceability: F16, design §3.5 slot #9/§4.4, R14, task 11.1.

---

### AD-035 — HSTS/HTTPS-redirect (pipeline slot #4) là trách nhiệm HOST, KHÔNG ép trong library lõi
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — design §3.5 liệt kê slot #4 nhưng không nói ai sở hữu
- Provenance/Evidence: `BedrockApiExtensions.UseBedrockApi` — slot #4 ghi comment "TRÁCH NHIỆM HOST" (không gọi `UseHsts`/`UseHttpsRedirection`); design §3.5 bảng có slot #4.
- Context: §3.5 có slot #4 "HSTS / HTTPS redirect". Nhưng base library có thể chạy SAU reverse proxy terminate TLS (traffic nội bộ là HTTP), và HSTS trong Development gây kẹt cache trình duyệt.
- Decision/Change: Base KHÔNG gọi `UseHsts`/`UseHttpsRedirection`; để Host quyết theo deployment/TLS. Base chỉ cấp cơ chế ForwardedHeaders/CORS/RateLimiter (#1/#8/#9).
- Rationale (verifiable): **Bản chất:** HSTS/HTTPS-redirect phụ thuộc topology triển khai (proxy terminate TLS? edge TLS?) và môi trường (dev vs prod) — thông tin Host mới có, library lõi KHÔNG biết. Ép trong lõi sẽ sai/gây hại (redirect vô hạn sau proxy, HSTS kẹt dev). Đặt đúng tầng = Host. `UseForwardedHeaders` (#1) resolve scheme thật để Host quyết định redirect nếu cần.
- Alternatives: (a) `UseHsts` vô điều kiện trong base (loại: hại sau proxy + dev); (b) thêm options bật/tắt HSTS trong base (loại: base vẫn không biết topology; tăng bề mặt cấu hình cho thứ thuộc Host).
- Consequences (RÀNG BUỘC Host — task 16.2): Host áp `UseHsts()`/`UseHttpsRedirection()` (không-dev) ở đúng vị trí trước `UseBedrockApi` hoặc Host tự chèn. Ghi để không ai tưởng base lo HTTPS.
- Reversibility: High.
- Traceability: F16, design §3.5 slot #4, task 11 (+ ràng buộc task 16.2).


---

### AD-036 — Chốt field shape cho record External Auth (`ExternalAuthRequest`/`Challenge`/`Callback`) — design chỉ đặt tên
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — design §5.6 ĐẶT TÊN các record nhưng KHÔNG cho field cụ thể
- Provenance/Evidence: design §5.6 — `CreateChallengeAsync(ExternalAuthRequest)→ExternalAuthChallenge`, `CompleteAsync(ExternalAuthCallback)→ExternalUserProfile`, comment "state + PKCE + nonce + returnUrl(whitelist)" / "verify state/pkce, replay-protect"; `ExternalUserProfile` cho field đầy đủ. File `ExternalAuthPorts.cs`; build 0 warning.
- Context: Task 12.3 cần định nghĩa 3 record External Auth. Design mô tả MỤC ĐÍCH (PKCE/nonce/state/returnUrl) nhưng không liệt kê field → phải chốt shape mà không bịa.
- Decision/Change: Định nghĩa TỐI THIỂU bám luồng OAuth/OIDC design mô tả:
  - `ExternalAuthRequest(string ReturnUrl)` — app khai nơi quay về (adapter whitelist).
  - `ExternalAuthChallenge(Uri RedirectUri, string State)` — URL authorize để redirect + state đối chứng.
  - `ExternalAuthCallback(string State, string Code)` — code + state trả về.
  - `ExternalUserProfile(...)` — theo ĐÚNG design (Provider/ProviderUserId/Email?/EmailVerified?/DisplayName).
- Rationale (verifiable): **Bản chất:** PKCE `code_verifier` + nonce là bí mật đối chứng SERVER-SIDE, adapter GIỮ nội bộ (keyed theo State) — KHÔNG thuộc bề mặt port (lộ ra chỉ tăng rủi ro + coupling). Port chỉ cần token tương quan (State) + code + redirect URL. `Uri` (không string) cho RedirectUri → type-safe, né CA1056. Đây là shape tối thiểu-đủ đúng chuẩn OAuth, không phải suy đoán tuỳ tiện (bám mô tả design + chuẩn ngành).
- Alternatives: (a) nhét PKCE/nonce vào DTO port (loại: lộ bí mật server-side ra hợp đồng, coupling adapter-cụ-thể); (b) để record rỗng "tự hiểu" (loại: vi phạm contract-first — không compile được consumer).
- Consequences: adapter (task ngoài phạm vi base — Adapters.ExternalAuth.Google/Zalo) tự quản PKCE/nonce/replay keyed theo State; nếu provider cần thêm field (vd id_token) → mở rộng record (thêm optional, backward-compat).
- Reversibility: High (greenfield, chưa có adapter/consumer).
- Traceability: F27, design §5.6, R20, task 12.3.

### AD-037 — Dùng Scrutor `Decorate` (open-generic) để wiring decorator pipeline (§8) — REVISIT AD-033
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro)
- Provenance/Evidence: `platform/src/Bedrock.Application/DependencyInjection/BedrockCoreExtensions.cs` (`AddBedrockCore` → `TryDecorate(typeof(IUseCase<,>), ...)` x4 + `typeof(ICommandUseCase<>), ...` x5); package `Scrutor` 7.0.0 (Directory.Packages.props) + PackageReference trong `Bedrock.Application.csproj`; guard `PipelineOrderTests` (DI thật) xanh; build 0 warning.
- Context: Task 15 cần bọc use case bằng 4–5 decorator generic theo thứ tự §8. AD-033 đã CHỐT không dùng Scrutor cho marker-scan, nhưng ghi rõ tiên liệu "thêm Scrutor NẾU decorator pipeline cần". Decorate open-generic đúng-thứ-tự-nesting viết tay bằng reflection rất dễ sai (phải thay ImplementationFactory, giữ lifetime, resolve inner) — là bài toán Scrutor giải chuẩn.
- Decision/Change: Thêm Scrutor CHỈ cho decorator pipeline. Marker-scan DI (`BedrockRegistrationExtensions`) GIỮ NGUYÊN reflection tự viết (AD-033 không bị lật — chỉ thu hẹp phạm vi "không Scrutor" xuống "không Scrutor cho marker-scan"). Dùng `TryDecorate` (không ném khi chưa có use case → host/test tối thiểu vẫn boot). Thứ tự gọi: lời đầu = lớp TRONG, lời cuối = lớp NGOÀI → đăng ký từ Transaction (trong) ra Logging (ngoài).
- Rationale (verifiable): **Bản chất:** decoration open-generic đúng-thứ-tự là năng lực Scrutor cung cấp ổn định, còn marker-scan chỉ là vòng lặp reflection đơn giản không cần lib. Tách đôi phạm vi giữ AD-018 (whitelist plumbing) + AD-033 (tự sở hữu scan) nhất quán mà vẫn không tự dựng lại decoration engine dễ lỗi. `PipelineOrderTests` chứng minh nesting đúng (unauthorized+invalid → forbidden, không phải validation_error).
- Alternatives: (a) tự viết Decorate open-generic (loại: dễ sai, phải bảo trì; không có giá trị so lib chuẩn); (b) MediatR pipeline behaviors (loại: kéo cả mediator + đổi mô hình use case hiện có, over-engineering cho base).
- Consequences: Application phụ thuộc thêm Scrutor (đã whitelist AD-018). Host PHẢI gọi `AddBedrockCore` SAU khi đăng ký use case (đã ghi XML-doc). Thêm behavior mới = thêm một `TryDecorate` đúng vị trí thứ tự.
- Reversibility: Medium (gỡ Scrutor phải tự viết lại decoration — nhưng API `AddBedrockCore` không đổi với caller).
- Traceability: F13, design §8, R26.3, task 15; revisit AD-033/AD-018.

### AD-038 — Khai permission bằng `[RequirePermission]` trên KIỂU input, ngữ nghĩa AND (least-privilege)
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — design §8 nói "khai báo trên command qua attribute/metadata" nhưng KHÔNG cho tên/shape/ngữ nghĩa nhiều permission.
- Provenance/Evidence: `platform/src/Bedrock.Application/Authorization/RequirePermissionAttribute.cs` (`[AttributeUsage(Class|Struct, AllowMultiple=true, Inherited=true)]` + `PermissionMetadata.For(Type)` cache theo kiểu) + `AuthorizationUseCaseDecorator`/command variant (cache `static readonly string[]` per closed-generic); guard `AuthorizationDecoratorTests` (missing→forbidden, granted→run, no-attr→pass, AND-semantics, command variant, metadata reader) xanh.
- Context: Authorization behavior cần biết use case yêu cầu quyền gì. Design chỉ định "attribute/metadata" trên command. Phải chốt: (1) attribute vs interface; (2) đặt ở đâu; (3) nhiều permission = AND hay OR.
- Decision/Change: (1) ATTRIBUTE (permission là hằng của LOẠI thao tác, không đổi theo instance → metadata tĩnh, cache được — khác `IIdempotentCommand` là giá trị runtime nên phải interface, AD-039); (2) gắn trên `typeof(TInput)`, đọc qua `PermissionMetadata.For` (cache `ConcurrentDictionary` + `static readonly` per closed-generic → reflection chạy một lần); (3) AllowMultiple + **AND** (phải có ĐỦ mọi permission khai) = least-privilege, an toàn mặc định. Không khai → pass-through.
- Rationale (verifiable): **Bản chất:** quyền cần để chạy một use case là bất biến của kiểu → thuộc metadata kiểu, không phải state. AND là mặc định fail-safe: thiếu bất kỳ quyền nào → chặn; muốn OR thì app tự gộp thành một permission tổng hợp (đơn giản, không mơ hồ). Cache per-kiểu tránh reflection nóng mỗi request.
- Alternatives: (a) interface `IRequirePermission { string[] Permissions }` (loại: buộc mọi input tự implement, permission là hằng-kiểu nên attribute hợp hơn); (b) OR-semantics mặc định (loại: nới quyền ngầm — nguy hiểm cho base thương mại); (c) policy-string như ASP.NET (loại: kéo AuthorizationPolicy vào Application, coupling framework).
- Consequences: Authorization theo TRẠNG THÁI resource (cần load dữ liệu) vẫn nằm TRONG use case (design §8 xác nhận). App/module định nghĩa tập permission (F3 — lõi không hardcode). Muốn OR → tạo permission tổng hợp.
- Reversibility: High (attribute là bề mặt cộng thêm; đổi ngữ nghĩa chỉ sửa vòng lặp trong decorator + guard test).
- Traceability: F23, design §8, R26.3, task 15.

### AD-039 — Idempotency v1: gate bằng `IIdempotentCommand`, `TryBegin` (KHÔNG replay response), TTL 24h
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — design §8 chốt "command mang IdempotencyKey → TryBeginAsync false → idempotency_conflict, KHÔNG replay ở v1" nhưng KHÔNG cho cơ chế mang key + con số TTL.
- Provenance/Evidence: `platform/src/Bedrock.Application/UseCases/IIdempotentCommand.cs` (`string IdempotencyKey`) + `IdempotencyUseCaseDecorator`/command variant (gate `input is IIdempotentCommand` → `IIdempotencyStore.TryBeginAsync(key, IdempotencyDefaults.Ttl)`) + `IdempotencyDefaults.Ttl = 24h` / `Conflict = Error.Conflict("idempotency_conflict", ...)`; guard `IdempotencyDecoratorTests` (first-run/duplicate-conflict/non-idempotent-skip-store/command-variant/ttl=24h) xanh.
- Context: Behavior cần lấy key từ input + hạn TTL. Design để mở "command mang IdempotencyKey".
- Decision/Change: (1) key qua INTERFACE `IIdempotentCommand` (giá trị RUNTIME do client cấp per-instance → không thể attribute); (2) behavior GATED — input không implement → pass-through, KHÔNG chạm store (khỏi tốn round-trip cho thao tác không idempotent); (3) TTL hằng 24h (`IdempotencyDefaults.Ttl`); (4) trùng key → `Error.Conflict("idempotency_conflict")`, KHÔNG replay response (đúng design §8 v1).
- Rationale (verifiable): **Bản chất:** IdempotencyKey là dữ liệu runtime (mỗi request khác) → phải là thành viên instance (interface), trái ngược permission (hằng-kiểu → attribute, AD-038). 24h đủ dài phủ mọi retry hợp lý của client/gateway/mạng, đủ ngắn để store tự dọn key. Không replay response tránh phải serialize/deserialize response vào store (phức tạp + rủi ro version response) — replay là phần mở rộng của adapter store nếu cần.
- Alternatives: (a) attribute cho key (loại: key là runtime, attribute là compile-time); (b) TTL cấu hình qua Options (hoãn — chưa có nhu cầu; hằng đủ cho v1, dễ nâng thành Options sau); (c) replay response ở v1 (loại: design đã loại — phức tạp/serialize response).
- Consequences: Command muốn idempotent → implement `IIdempotentCommand`. Client nhận `idempotency_conflict` khi trùng (không nhận lại response cũ) — hợp đồng v1. Nâng cấp TTL→Options hoặc replay = mở rộng backward-compat.
- Reversibility: High (interface + hằng; nâng TTL thành Options không phá caller).
- Traceability: F28, design §8, task 15; đối chiếu AD-038 (attribute vs interface).

### AD-040 — Transaction behavior CHỈ áp cho `ICommandUseCase<>` (ghi thuần); value-returning tự quản
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — design §8 nói "mở ExecuteInTransactionAsync cho command ghi + outbox" nhưng không định rõ họ use case nào (value-returning gồm cả query đọc lẫn command-trả-giá-trị).
- Provenance/Evidence: `platform/src/Bedrock.Application/Behaviors/TransactionCommandUseCaseDecorator.cs` (chỉ `ICommandUseCase<TInput>`); `BedrockCoreExtensions.DecoratePipeline` — họ `IUseCase<,>` KHÔNG có `TryDecorate` Transaction, họ `ICommandUseCase<>` CÓ; guard `TransactionDecoratorTests` (body-runs-inside-transaction, failing-propagates) + `PipelineOrderTests` (command mở transaction; query family không) xanh.
- Context: Value-returning `IUseCase<TIn,TOut>` gồm CẢ query (chỉ đọc) lẫn command trả giá trị (vd tạo → trả id). Bọc transaction cho query chỉ-đọc = mở transaction thừa (lãng phí + giữ connection). Không thể phân biệt tĩnh query vs write trong họ value-returning.
- Decision/Change: Transaction behavior CHỈ bọc `ICommandUseCase<TInput>` (họ ghi-thuần, chắc chắn write). Use case value-returning tự quản transaction TƯỜNG MINH khi cần ghi — reentrancy R7.4 (AD-012) đảm bảo lời gọi `ExecuteInTransactionAsync` lồng THAM GIA transaction hiện hành, không xung đột nếu sau này có thêm lớp bọc.
- Rationale (verifiable): **Bản chất:** transaction chỉ cần cho thao tác GHI; `ICommandUseCase` là tín hiệu tĩnh rõ ràng nhất cho "ghi thuần". Bọc mọi value-returning = mở transaction cho query đọc (sai bản chất, tốn tài nguyên). Reentrancy (đã có + test) làm việc "value-returning tự quản" an toàn tuyệt đối kể cả khi lồng. Đây là lựa chọn bám design ("command ghi") + né tác dụng phụ, không phải bỏ sót.
- Alternatives: (a) bọc cả value-returning (loại: transaction thừa cho query đọc); (b) thêm marker `ITransactionalUseCase` cho value-returning-write (hoãn: chưa có nhu cầu; reentrancy đã đủ an toàn để use case tự gọi — thêm sau backward-compat nếu cần); (c) đoán bằng tên/heuristic (loại: bịa/không đáng tin).
- Consequences: Command ghi (void) → transaction tự động (+ outbox atomic). Value-returning-write → gọi `ExecuteInTransactionAsync` trong thân (reentrancy an toàn). Tài liệu hoá rõ trong XML-doc decorator + AD này.
- Reversibility: High (thêm bọc cho value-returning về sau chỉ là thêm `TryDecorate` + marker, không phá hợp đồng).
- Traceability: I3/F5/F25, design §8/§5.1, R8.1, task 15; dựa AD-012 (reentrancy).

### AD-041 — Refresh token lifetime mặc định 14 ngày (design không chốt con số)
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — design §7.4/§4.7 mô tả `expires_at` nhưng KHÔNG cho giá trị TTL.
- Provenance/Evidence: `platform/src/Modules/Identity/Identity.Application/RefreshToken/RefreshAccessTokenUseCase.cs` (`internal static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14)`); guard `RefreshAccessTokenUseCaseTests.Refresh_token_lifetime_is_14_days` + `Valid_token_rotates_and_returns_new_tokens` (assert expiresAt = now + 14d). Build 0 warning.
- Context: Rotation tạo token mới cần `ExpiresAt = now + TTL`. Design để mở.
- Decision/Change: TTL refresh token = 14 ngày, khai `internal static readonly` (test thấy qua InternalsVisibleTo, KHÔNG phơi API công khai).
- Rationale (verifiable): 14 ngày cân bằng UX (không bắt đăng nhập lại quá thường) vs cửa sổ rủi ro nếu token rò (reuse-detection §7.4 thu hồi family khi phát hiện). Là hằng module (không phải hằng lõi Bedrock — F3), promotable thành Options per-app sau (backward-compat) khi có nhu cầu chính sách khác nhau.
- Alternatives: (a) Options ngay (hoãn: over-engineering cho skeleton, chưa có app cần khác biệt); (b) TTL rất dài (loại: cửa sổ rủi ro lớn); (c) rất ngắn (loại: UX kém, refresh dồn dập).
- Consequences: mọi refresh token mới hết hạn sau 14 ngày. Nâng thành Options = thêm IdentityOptions binding, không phá caller.
- Reversibility: High (hằng nội bộ; đổi số/nâng Options cục bộ).
- Traceability: design §7.4/§4.7, F10, task 16.1.

### AD-042 — Health-check DB đặt tên PER-CONTEXT (`database:{TContext}`) để multi-module không trùng tên
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI(Kiro) — fix tận gốc landmine N-040 (design không nói tên health-check).
- Provenance/Evidence: `platform/src/Bedrock.Infrastructure/DependencyInjection/BedrockPersistenceExtensions.cs` (`healthCheckName = $"{DatabaseHealthCheckPrefix}:{typeof(TContext).Name}"`); guard `Bedrock.Infrastructure.Tests/MultiModulePersistenceTests` (2 context → 2 tên `ready` duy nhất + resolve HealthCheckService không ném). Build 0 warning; 182 test xanh.
- Context: `AddBedrockPersistence<TContext>` trước hardcode tên `"database"`. Hai module (2 DbContext) cùng gọi → `DefaultHealthCheckService` ném "duplicate registration" lúc resolve → crash boot. Xuất hiện đúng khi task 16 ráp ≥2 module (hoặc tương lai thêm Rooms).
- Decision/Change: tên health-check = `database:{typeof(TContext).Name}` (vd `database:IdentityDbContext`), giữ tag `ready`; vòng set timeout khớp tên per-context.
- Rationale (verifiable): **Bản chất:** health-check name PHẢI duy nhất toàn ứng dụng (ràng buộc của HealthCheckService). Persistence là per-module (mỗi module một DbContext) → tên phải gắn định danh context để duy nhất theo cấu trúc, không phụ thuộc thứ tự đăng ký. `typeof(TContext).Name` ổn định + đọc được (readiness report phân biệt được module nào chưa sẵn sàng). Đây là fix GỐC (đúng bản chất "một DB check mỗi module"), không phải vá (không phải append số ngẫu nhiên).
- Alternatives: (a) tên do caller truyền (loại: thêm tham số bắt buộc, dễ quên/đặt trùng); (b) một health-check gộp mọi context (loại: mất khả năng phân biệt module nào down); (c) giữ "database" + chỉ một module được có DB (loại: phá mô hình multi-module — bản chất platform).
- Consequences: readiness `/health/ready` liệt kê nhiều check `database:*` (một mỗi module) — rõ ràng hơn. Không phá test cũ (không test nào assert tên "database"; Api.Tests readiness không có DB check).
- Reversibility: High (đổi quy ước tên cục bộ trong một extension).
- Traceability: N-040, task 6.3/16.1/16.2, R34, F31/I6.

---

### AD-043 — API versioning qua URL-segment `/v{version}` (không header) + helper `MapVersionedGroup`
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI (implementation-time), design §9.1 cho chọn "URL hoặc header" nhưng không chốt
- Provenance/Evidence: design §9.1 "HTTP API: `Asp.Versioning` (URL `/v1` hoặc header)"; impl `Bedrock.Api/Versioning/BedrockApiVersioning.cs` (`UrlSegmentApiVersionReader`, `V1=new(1,0)`, `MapVersionedGroup`); package `Asp.Versioning.Http` 10.0.0 (verified `dotnet add`); `HostSmokeTests` POST `/v1/identity/token/refresh` → 400 + header `api-supported-versions: 1.0` xanh (verified `dotnet test`, 195 pass).
- Context: design cho phép URL-segment HOẶC header versioning. Phải chốt MỘT scheme để nhất quán toàn platform (module không mỗi nơi một kiểu).
- Decision/Change: URL-segment `/v{version:apiVersion}` (reader = `UrlSegmentApiVersionReader`), `DefaultApiVersion=v1`, `AssumeDefaultVersionWhenUnspecified=true`, `ReportApiVersions=true`. Base cấp helper `MapVersionedGroup(prefix, versions)` để module khai version tại một chỗ; endpoint dùng `.MapToApiVersion(V1)`.
- Rationale (verifiable): **Bản chất:** URL-segment hiện version NGAY trong đường dẫn → dễ đọc log/debug, cache-key tự nhiên theo path cho proxy/CDN, không bị proxy strip như header. Header/query versioning ẩn, khó test qua URL thuần, dễ bị hạ tầng trung gian bỏ. URL-segment là chuẩn phổ biến cho REST public API thương mại. Helper tập trung convention `/v{n}` → chống drift mỗi module tự chế route.
- Alternatives: (a) header/query versioning (loại: ẩn, cache khó, dễ bị proxy strip); (b) media-type/content negotiation (loại: phức tạp cho client, ít dùng ở REST thương mại).
- Consequences: mọi endpoint versioned nằm dưới `/v{n}`; module BẮT BUỘC qua `MapVersionedGroup` (nhất quán); route thiếu version → 404 rõ ràng. `HostSmokeTests` đổi path sang `/v1/...`.
- Reversibility: Medium (đổi reader = đổi shape URL của mọi endpoint — breaking cho client).
- Traceability: F32, design §9.1, R22.1, task 17.

---

### AD-044 — Correlation id = W3C traceId của trace hiện hành (bỏ override tuỳ tiện từ client)
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI (implementation-time) — siết CP10 để đạt R24.2 "== trace hiện hành"
- Provenance/Evidence: R24.2 "`X-Correlation-Id` == `traceId` == trace hiện hành"; design §9.3. Impl mới `CorrelationContext.CurrentTraceId = Activity.Current?.TraceId.ToString() ?? TraceIdentifier`; `CorrelationIdMiddleware` bỏ đọc/override header client. Test `BedrockPipelineTests.Unhandled_exception_*` gửi `traceparent: 00-0af7651916cd43dd8448eb211c80319c-...` → header `X-Correlation-Id` == body `traceId` == `0af7651916cd43dd8448eb211c80319c` (verified `dotnet test`, 197 pass, 0 warning).
- Context: bản trước (task 5.4) cho phép client gửi `X-Correlation-Id` tuỳ ý và echo lại → thoả "header == traceId" NHƯNG KHÔNG thoả "== trace hiện hành" (giá trị client không phải W3C trace id). Sau khi wire OpenTelemetry (task 18), mỗi request LUÔN có `Activity` với TraceId thật → siết được đúng R24.2.
- Decision/Change: id chính tắc = `Activity.Current.TraceId` (32-hex W3C). `X-Correlation-Id` (header) + `ProblemDetails.traceId` = giá trị này (đọc 1 nguồn qua `CorrelationContext`). BỎ nhận override từ header client; propagation xuyên service dùng chuẩn **W3C `traceparent`** (OTel/ASP.NET trích tự động → Activity nối trace theo caller).
- Rationale (verifiable): **Bản chất F21:** correlation chỉ có giá trị khi nó CHÍNH LÀ trace id — để log, trace (Jaeger/Tempo), và error-body cùng khoá một id. Cho client ghi đè bằng chuỗi tuỳ ý phá liên kết với trace thật (không tra được trong hệ trace). Chuẩn ngành: propagation bằng `traceparent`, KHÔNG bằng header tuỳ biến. Đây là fix TẬN GỐC (đổi nguồn id), không phải vá (giữ override rồi thêm điều kiện).
- Alternatives: (a) giữ override client + fallback trace (loại: vẫn phá "== trace hiện hành" khi client gửi id lạ — chính lỗ hổng F21); (b) sinh GUID riêng cho correlation (loại: tách rời trace → không tra chéo được log↔trace).
- Consequences: client muốn nối correlation phải gửi `traceparent` (chuẩn), không phải `X-Correlation-Id`. `X-Correlation-Id` giờ THUẦN OUTPUT (server phơi trace id). Outbox `correlation_id` mang `Activity.Id` (full traceparent) để consumer nối trace — cùng trace, khác biểu diễn (bare traceId cho HTTP, full traceparent cho bus) — xem N-046.
- Reversibility: Medium (đổi lại là nới lỏng, nhưng sẽ tái mở F21).
- Traceability: F21/F34, R24.2, CP10, design §9.3, task 18 (siết task 5.4).

---

### AD-045 — JWT key-ring validate lúc HOST START (options `.ValidateOnStart()`), KHÔNG eager lúc đăng ký; singleton lazy từ IOptions
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI (implementation-time) — đúng chữ design §9.4 "validate-on-start"; phát hiện qua test task 19
- Provenance/Evidence: design §9.4 "Validate-on-start cho MỌI options bắt buộc"; trước đây `AddBedrockSecurity` gọi `JwtKeyRingValidation.Validate(keyRing)` NGAY lúc đăng ký (đọc `builder.Configuration` TRƯỚC `Build()`). Test `HostSmokeTests` (task 19) với secret nạp qua `WebApplicationFactory.ConfigureAppConfiguration` (áp lúc Build) FAIL vì eager-read không thấy config muộn. Sau khi đổi sang `AddOptions<JwtKeyRingOptions>().Configure(bind).ValidateOnStart()` + `IValidateOptions` (`JwtKeyRingOptionsValidator`) + singleton `sp => IOptions.Value` + bỏ `TryAddSingleton(keyRing)` eager ở `AddBedrockAuthCore` → 198 test xanh (2 boot-with-injected-secret + 1 fail-fast-missing-secret), 0 warning (verified).
- Context: secret KHÔNG được ở repo (F35) → dev nạp qua User-Secrets, test nạp qua test-config, prod qua env/Key Vault — TẤT CẢ đều nạp SAU thời điểm eager-read pre-Build. Eager validation lúc đăng ký khiến (a) không thể nạp secret muộn (test/User-Secrets ở một số đường), (b) `JwtTokenService` ctor (build `SymmetricSecurityKey`) nhận `JwtKeyRingOptions` concrete bound eager (rỗng) → ném lúc `RequiredPortsValidator` resolve.
- Decision/Change: (1) `JwtKeyRingValidation` bọc trong `IValidateOptions<JwtKeyRingOptions>` (`JwtKeyRingOptionsValidator`) + `AddOptions<>().Configure(bind lazy).ValidateOnStart()` → validate lúc host START (sau Build). (2) Singleton concrete `JwtKeyRingOptions` = `sp => sp.GetRequiredService<IOptions<JwtKeyRingOptions>>().Value` (lazy, post-Build). (3) Bỏ `TryAddSingleton(keyRing)` eager ở `AddBedrockAuthCore` (JwtBearer vẫn bind local keyRing cho Issuer/Audience/ResolveKeys — resolver chạy lazy lúc verify token).
- Rationale (verifiable): **Bản chất:** "validate-on-start" (design §9.4) NGHĨA LÀ validate lúc host start, không phải lúc đăng ký service. Eager-read pre-Build phá khả năng nạp secret muộn (đúng mô hình secret-ngoài-repo F35) — mà secret-ngoài-repo lại là YÊU CẦU của chính task 19. Hai yêu cầu (fail-fast + secret nạp muộn) chỉ đồng thời thỏa khi validation chạy SAU khi mọi nguồn config đã hợp nhất (post-Build) = `.ValidateOnStart()`. Đây là fix TẬN GỐC (đổi thời điểm validate), không phải vá (giữ eager rồi lách).
- Alternatives: (a) giữ eager + nhét dev-secret placeholder trong appsettings (loại: secret/khóa-hợp-lệ trong repo, vi phạm F35 + scanner flag); (b) nạp secret qua env var lúc test (loại: env process-global → ô nhiễm giữa test, khó cô lập fail-fast test); (c) `.Validate(predicate,string)` (loại: mất thông điệp chi tiết — dùng `IValidateOptions` giữ message "HS256 ≥32 byte").
- Consequences: JWT sai/thiếu → boot FAIL lúc start với `OptionsValidationException` (message chi tiết). `JwtTokenService` nhận key-ring post-Build (nạp muộn OK). `AddBedrockAuthCore` không còn đăng ký concrete `JwtKeyRingOptions` (đến từ `AddBedrockSecurity`). Tương đương AD-011/DV-012 tinh thần (fail-fast mọi môi trường) nhưng qua options-pattern chuẩn cho JWT.
- Reversibility: Medium (đổi lại eager là quay về lỗ hổng nạp-muộn).
- Traceability: F35/F7/F22, R25.2/R13, design §9.4, AD-008 (shared config), task 19 (siết task 9.2).

---

### AD-046 — CP1 literal-scan qua Mono.Cecil (quét `ldstr` + `const string` trong IL) — hoàn tất no-business-in-core, resolves AD-022
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI (implementation-time) — hoàn tất phần literal của CP1 mà AD-022 đã hoãn sang task 20
- Provenance/Evidence: design CP1 "∀ type t trong Bedrock.*, tên/namespace/**chuỗi** của t KHÔNG chứa guest|room|resort|admin|staff"; AD-022 ghi rõ reflection chỉ bắt tên, hoãn literal sang task 20. Impl `tests/Bedrock.ArchitectureTests/NoBusinessInCoreLiteralTests.cs` (Mono.Cecil 0.11.6) quét `OpCodes.Ldstr` + `FieldDefinition.HasConstant` trên 5 assembly `Bedrock.*` (Domain/Messaging.Contracts/Application/Infrastructure/Api). Chạy thật: 0 vi phạm (lõi sạch literal) + negative control (assembly test bẩn → bắt được seed) xanh; 199 test, 0 warning (verified).
- Context: reflection/NetArchTest CHỈ thấy metadata (tên type/member) — KHÔNG thấy chuỗi literal trong thân method (vd hằng path/role nghiệp vụ lọt vào lõi). Cần quét IL để phủ nốt CP1. `System.Reflection.Metadata` (BCL) không expose enumerate #US heap gọn; decode IL thủ công cần bảng opcode đầy đủ (dễ sai). Mono.Cecil decode IL đúng chuẩn + đã là transitive-dep của NetArchTest.Rules.
- Decision/Change: Thêm `Mono.Cecil` (PackageReference tường minh, pin 0.11.6) vào `Bedrock.ArchitectureTests`; quét `ldstr` (literal thân method) + `const string` (hằng field) trên toàn 5 assembly `Bedrock.*`, đọc DLL từ **bytes** (`MemoryStream`) để không khoá file. Token cấm case-insensitive `guest|room|resort|admin|staff`. Có negative control (scan chính assembly test — vốn chứa mảng token → phải ra khác rỗng). Đồng thời mở rộng `NoBusinessInCoreTests` (name-scan) ra đủ 5 assembly (trước chỉ 3).
- Rationale (verifiable): **Bản chất:** CP1 gồm cả literal (design nói rõ "chuỗi của t"); bỏ literal là để hở đúng thứ F2/F15 lo (hằng path/role nghiệp vụ lọt lõi). Mono.Cecil là công cụ decode-IL đúng đắn, chi phí thấp (đã có transitive), tránh tự viết IL-decoder dễ sai. Đọc bytes tránh lock DLL đang nạp (chạy song song an toàn).
- Alternatives: (a) `System.Reflection.Metadata` tự decode IL (loại: cần bảng opcode đầy đủ để skip operand — dễ sai/false-positive); (b) quét raw #US heap (loại: API BCL không expose gọn); (c) source-scan .cs (loại: không thấy code generated + fragile theo layout file).
- Consequences: mỗi build chạy Cecil scan 5 DLL (nhanh, <150ms). Match là SUBSTRING case-insensitive → nếu sau này có literal hạ tầng vô tình chứa token (vd cột DB "showroom") sẽ báo — khi đó cân nhắc word-boundary; hiện 0 false-positive.
- Reversibility: High (test-only, gỡ package + file là xong).
- Traceability: CP1/I1/F2/F3/F4, R1.1, design §Correctness Properties, resolves AD-022, task 20.

---

### AD-047 — Outbox retention: base cấp LOGIC `PurgeAsync` (Host lên lịch), TTL mặc định processed 7 ngày / dead-letter giữ vô thời hạn
- Status: Confirmed
- Date: 2026-07-09
- Decider: AI (implementation-time) — design/R8.5 nói "dọn outbox" nhưng KHÔNG chốt kiến trúc (job vs hosted-service) lẫn con số TTL
- Provenance/Evidence: R8.5 (retention/cleanup outbox); impl `platform/src/Bedrock.Infrastructure/Persistence/Messaging/OutboxRetentionOptions.cs` (`ProcessedRetention = TimeSpan.FromDays(7)`, `DeadLetterRetention = null`) + `EfOutboxRetention.cs` (`PurgeAsync`, provider-conditional) + DI `AddOutboxRetention<TContext>`; guard `platform/tests/Bedrock.Infrastructure.Tests/OutboxRetentionTests.cs` (3 test: xoá đúng processed-cũ giữ pending/processed-mới/dead-letter; dead-letter cũ chỉ xoá khi bật TTL; 0-khi-không-có-gì-hết-hạn). Build 0 warning; 203 test xanh (verified `dotnet test Platform.slnx`).
- Context: bảng `outbox_message` phình vô hạn nếu không dọn (mỗi event là một row, giữ mãi cả sau publish). Dead-letter là CỘT trên chính bảng (AD-003, không bảng DLQ riêng) → job dọn phải phân biệt row đã-publish (dọn được) vs pending (KHÔNG được mất) vs dead-letter (cần soi/replay thủ công). Design để mở CẢ (a) đặt job ở đâu và (b) TTL bao nhiêu.
- Decision/Change: (1) **Base cấp LOGIC** `EfOutboxRetention<TContext>.PurgeAsync()` + `AddOutboxRetention<TContext>` (scoped, named-options per-context); **Host lên lịch** chạy định kỳ — base KHÔNG có hosted-service. (2) TTL mặc định: `ProcessedRetention = 7 ngày`; `DeadLetterRetention = null` (giữ vô thời hạn, chỉ dọn khi app set giá trị). (3) Vị từ xoá processed LUÔN kèm `dead_lettered_at == null` → không đụng dead-letter; pending (`processed_at == null`) không bao giờ khớp. (4) Provider-conditional (mirror `EfOutboxDispatcher`): Npgsql = `ExecuteDeleteAsync` set-based; provider khác (SQLite) = nạp client-side + `RemoveRange` + `SaveChanges` (SQLite không dịch được so sánh `DateTimeOffset`, cùng lý do DV-010).
- Rationale (verifiable): **Bản chất — nhất quán trách nhiệm:** dispatcher (task 7.3) đã theo mô hình "base cấp logic một-lượt, Host quyết lịch/threading" (base KHÔNG có hosted-service). Retention là cùng loại tác vụ nền → PHẢI theo cùng mô hình, nếu không platform có hai kiểu lịch trái ngược (drift kiến trúc). Base không áp scheduler vì lịch dọn là quyết định vận hành (tần suất, giờ thấp tải) thuộc app, không thuộc lõi domain-agnostic (F3). **TTL mặc định:** 7 ngày cho processed đủ cửa sổ đối soát/điều tra sự cố publish gần đây mà không giữ rác lâu; dead-letter giữ-vô-thời-hạn theo mặc định vì mất dead-letter = mất bằng chứng poison message (chỉ app biết khi nào đã export/xử lý xong để cho phép dọn) — mirror tinh thần AD-041 (con số mặc định hợp lý, promotable qua Options per-app). **An toàn:** điều kiện xoá bất biến "chỉ dọn cái đã hoàn tất publish / hoặc dead-letter đã quá TTL do app chủ động bật" → không mất message chưa gửi.
- Alternatives: (a) base tự chạy `BackgroundService` dọn (loại: mâu thuẫn mô hình dispatcher — base không ôm scheduler; ép chu kỳ vào lõi); (b) xoá cả dead-letter theo cùng TTL processed (loại: mất bằng chứng poison trước khi kịp điều tra — nguy hiểm vận hành); (c) TTL cứng không cấu hình (loại: app khác nhau cần chính sách lưu trữ khác — compliance/audit); (d) TRUNCATE/xoá theo tuổi bất kể trạng thái (loại: mất pending = mất event chưa publish, phá at-least-once).
- Consequences: Host phải wire scheduler (vd `BackgroundService`/Quartz) gọi `PurgeAsync` — chưa có Host job trong skeleton (giống dispatcher chưa có hosted-service). App muốn dọn dead-letter phải chủ động set `DeadLetterRetention`. Trên SQLite test đi nhánh client-side (không test được nhánh `ExecuteDeleteAsync` Npgsql — thuộc task 7.4 Testcontainers, cùng giới hạn DV-010).
- Reversibility: High (thêm class + extension + options; đổi số TTL hoặc nâng scheduler cục bộ, không phá caller).
- Traceability: R8.5, AD-003 (dead-letter là cột), AD-041 (tiền lệ default-number promotable), DV-010 (SQLite không dịch DateTimeOffset), design §7.2, task 7.5.

### AD-048 — Thiết kế adapter RabbitMQ (topic exchange + publisher-confirms + persistent + resilience) — design chỉ đặt tên
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — design §3.2/§9.2 ĐẶT TÊN `Adapters.Messaging.RabbitMq` + "resilience biên adapter" nhưng KHÔNG chốt exchange/routing/confirms/lifecycle/số resilience.
- Provenance/Evidence: `platform/src/Adapters/Messaging.RabbitMq/*` (`RabbitMqEventBusPublisher`, `RabbitMqMessageMapper`, `RabbitMqResiliencePipelineFactory`, `RabbitMqOptions`, `RabbitMqMessagingExtensions`); packages pin RabbitMQ.Client 7.2.1 + Microsoft.Extensions.Resilience 10.7.0 (Directory.Packages.props); API 7.x xác minh qua tài liệu chính thức rabbitmq.com (async `CreateConnectionAsync`/`CreateChannelAsync(CreateChannelOptions)`/`BasicPublishAsync(exchange,routingKey,mandatory,basicProperties,body,ct)` + publisher-confirms qua `CreateChannelOptions`). Guard: `AdapterIsolationTests` (CP3) + `RabbitMqMessageMapperTests`/`RabbitMqResiliencePipelineTests`/`RabbitMqOptionsTests`/`AddRabbitMqMessagingTests` (13 test) — build 0 warning, toàn suite xanh.
- Decision/Change (các điểm chốt, có lý do):
  - **Topic exchange, routing key = `EventType`** — cho consumer bind theo pattern (`identity.*`, `*.created`); linh hoạt hơn direct/fanout mà vẫn định tuyến chính xác theo loại event.
  - **Publisher-confirms (await broker ack) + message `Persistent`** — publish CHỈ coi là thành công khi broker đã nhận+ghi bền; khớp đảm bảo at-least-once của Outbox (dispatcher chỉ set `processed_at` khi PublishAsync trả về không lỗi — nếu confirm fail → exception → retry/không mark processed). Không confirm = có thể mark processed khi message chưa tới broker (mất event).
  - **Resilience biên (§9.2): `timeout → retry(exponential+jitter) → circuit-breaker`** thứ tự add ngoài→trong; retry an toàn vì publish idempotent ở mức hệ thống (Inbox dedup). Default knob (timeout 10s, retry 3, base 200ms, circuit 50%/10/30s/15s) — hợp lý cho bus, cấu hình per-adapter qua `RabbitMq:Resilience`.
  - **Override bằng `services.Replace`** (không Add) → đúng MỘT registration `IEventBusPublisher` (duplicate-guard F18 không báo); bỏ dòng `AddRabbitMqMessaging` = quay về `ThrowingEventBusPublisher` default (fail-loud), KHÔNG sửa lõi (F29).
  - **Connection/channel dùng chung, lazy tái tạo; truy cập channel serialize qua `SemaphoreSlim`** — `IChannel` KHÔNG thread-safe cho publish đồng thời; dispatcher vốn publish tuần tự nên lock nhẹ, đúng đắn.
  - **Validate-on-start** (`RabbitMqOptions.Validate` lúc `AddRabbitMqMessaging`) — cấu hình sai chặn boot (F35), gồm ràng buộc Polly (MinimumThroughput≥2, SamplingDuration≥0.5s) bắt sớm thay vì nổ lúc build pipeline.
- Rationale (verifiable): mọi lựa chọn bám bản chất "reliable publish khớp Outbox at-least-once" + "SDK/resilience chỉ ở adapter (I2/§17)" + chuẩn RabbitMQ. Mapper/resilience/options tách THUẦN → test được KHÔNG cần broker (13 test); phần I/O publish → Testcontainers (Docker, task 14 còn lại cùng nhóm 7.4/8.3).
- Alternatives: (a) direct exchange (loại: kém linh hoạt cho consumer topic-pattern); (b) không confirms (loại: phá at-least-once — có thể mark processed khi chưa tới broker); (c) channel-per-publish (loại: tốn kém; connection/channel reuse + lock hiệu quả hơn cho dispatcher tuần tự); (d) Add thay Replace (loại: 2 registration → duplicate-guard báo + hành vi last-wins mơ hồ).
- Consequences: Host bật bằng `AddMessagingCore().AddRabbitMqMessaging(cfg)`; consumer inbox dedup. Publish path chỉ kiểm chứng đầy-đủ khi có Docker (Testcontainers) — hiện verify qua compile-đúng-API + unit thuần + CP3.
- Reversibility: High (adapter là plug-in; gỡ = xóa project + 1 dòng Host, lõi không đổi — chính là F29).
- Traceability: F29/F33, design §3.2/§5.2/§9.2/§17, R5/R16.3/R23, CP3, task 14.

### AD-049 — Claim Outbox trên Npgsql bằng raw SQL `FOR UPDATE SKIP LOCKED` (hiện thực CP15, hoàn tất AD-016)
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — design §4.5 YÊU CẦU "row-lock skip-locked trên PostgreSQL để nhiều dispatcher không lấy trùng batch (R8.6/CP15)" nhưng code trước đó (task 7.3) mới SELECT thường (chưa khoá) — comment ghi "để task 7.4".
- Provenance/Evidence: `platform/src/Bedrock.Infrastructure/Persistence/Messaging/EfOutboxDispatcher.cs` (`BuildNpgsqlClaimSql` + `FromSqlRaw(sql, now, batchSize)` với `SELECT * FROM {schema-qualified} WHERE processed_at IS NULL AND dead_lettered_at IS NULL AND (next_attempt_at IS NULL OR next_attempt_at <= {0}) ORDER BY occurred_at LIMIT {1} FOR UPDATE SKIP LOCKED`); guard `platform/tests/Bedrock.Infrastructure.Tests/PostgresOutboxInboxTests.Concurrent_dispatchers_never_double_claim` (2 dispatcher đồng thời, BatchSize=3, 20 message → mỗi message publish ĐÚNG 1 lần: `Ids.Count == Distinct().Count() == 20`). Build 0 warning; SQLite dispatcher tests (8) không hồi quy.
- Context: EF LINQ KHÔNG dịch được `FOR UPDATE SKIP LOCKED`. Không khoá → 2 dispatcher instance cùng SELECT tập pending (READ COMMITTED thấy row chưa-commit của nhau) → publish TRÙNG (dù Inbox khử ở consumer, vẫn phí + vi phạm CP15 "exclusive claim").
- Decision/Change: nhánh Npgsql của `ClaimBatchAsync` dùng `FromSqlRaw` với `FOR UPDATE SKIP LOCKED`. Tên bảng/schema lấy TỪ MODEL (`context.Model.FindEntityType(...).GetTableName()/GetSchema()`, quote an toàn) → khớp per-module schema, KHÔNG hardcode, KHÔNG injection. `now`/`batchSize` là tham số. Row trả về vẫn được ChangeTracker theo dõi → set `ProcessedAt` persist ở SaveChanges. Nhánh non-Npgsql (SQLite test) GIỮ NGUYÊN LINQ client-side (provider-agnostic).
- Rationale (verifiable): SKIP LOCKED là cơ chế CHUẨN Postgres cho queue-claim đa-worker: A khoá row nó lấy, B bỏ qua row đang khoá (không chờ, không trùng) → exclusive claim + không chặn nhau. Lấy tên bảng từ model để đúng schema module (design §4.6) mà không lộ tên cứng. Đây là fix TẬN GỐC đúng thiết kế §4.5 (không phải vá: giải đúng bản chất "claim nguyên tử đa-instance").
- Alternatives: (a) advisory lock (loại: thô hơn, phải quản key); (b) SERIALIZABLE isolation + retry (loại: đắt + nhiều abort dưới tải); (c) một dispatcher duy nhất/leader-election (loại: giảm thông lượng + thêm hạ tầng); (d) dựa hoàn toàn Inbox dedup (loại: vẫn double-publish phí băng thông + vi phạm CP15 như thiết kế yêu cầu).
- Consequences: production đa-instance dispatcher an toàn (không double-publish). SQLite test không dùng nhánh này (đúng, SQLite đơn-connection). Chỉ Npgsql có SKIP LOCKED (provider đích §1.2).
- Reversibility: Medium (đổi lại SELECT thường là hồi quy CP15 — không nên).
- Traceability: design §4.5, R8.6, CP15, task 7.4; hoàn tất phần deferred của AD-016.

### AD-050 — Vận hành hoá: EF migrations PER-MODULE (design-time factory) thay `EnsureCreated`; migrate OUT-OF-BAND
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — user chọn hướng "vận hành hoá base cho production". Design §4.6 YÊU CẦU "migration per-module, history table riêng theo schema, deploy/migrate độc lập" nhưng chưa có migration nào (Host runtime KHÔNG tạo schema — gap production thật).
- Provenance/Evidence: `platform/.config/dotnet-tools.json` (pin `dotnet-ef` 10.0.9); `platform/src/Modules/Identity/Identity.Infrastructure/Persistence/IdentityDbContextFactory.cs` (`IDesignTimeDbContextFactory`, khớp CHÍNH XÁC options runtime UseNpgsql+UseSnakeCaseNamingConvention); `.../Persistence/Migrations/*InitialCreate*` (EnsureSchema "identity", payload jsonb, ux_refresh_hash, ix_outbox_pending); guard `platform/tests/Modules/Identity.IntegrationTests/IdentityMigrationTests` (Testcontainers/Postgres: `MigrateAsync` → __EFMigrationsHistory chứa InitialCreate + query outbox + round-trip refresh store). Build 0 warning; 227 test xanh.
- Context: `EnsureCreated` chỉ dev (design §Non-goals) — không hỗ trợ evolve schema, không dùng production. Sản phẩm thương mại cần migration versioned + áp có kiểm soát.
- Decision/Change: (1) `dotnet-ef` local tool pin trong `.config/dotnet-tools.json` (tái lập CI). (2) Design-time factory để `dotnet ef` dựng context không cần Host, KHỚP options runtime (nếu lệch → migration lệch model). (3) Migration nằm trong Identity.Infrastructure (EF tự tìm ở assembly DbContext); history table vào schema `identity` (nhờ `HasDefaultSchema`). (4) **Migrate OUT-OF-BAND** (`dotnet ef database update`/bundle ở bước deploy), app KHÔNG tự migrate lúc start — an toàn đa-instance (tránh 2 instance migrate đồng thời).
- Rationale (verifiable): migration versioned là chuẩn production để evolve schema an toàn + audit. Design-time factory tách tooling khỏi Host (không cần bật app để sinh migration). Migrate out-of-band tránh race đa-instance + cho phép rollback có kiểm soát (khác auto-migrate-on-start dễ hỏng khi nhiều replica). `IdentityMigrationTests` chứng minh migration KHỚP model runtime (chạy MigrateAsync thật, round-trip) → không drift.
- Alternatives: (a) auto-migrate lúc start (loại: race đa-instance + rollback khó + rủi ro downtime); (b) giữ EnsureCreated (loại: không evolve được, dev-only); (c) SQL script thủ công (loại: mất đồng bộ với model, dễ drift).
- Consequences: deploy có bước "apply migrations" (CLI/bundle). Thêm module = thêm migration của module đó (độc lập). Đổi model → thêm migration mới (versioned).
- Reversibility: High (migration là artifact cộng thêm; có thể `migrations remove` khi chưa apply).
- Traceability: design §4.6, F31/I6, R (vận hành), post-base operational.

### AD-051 — Dockerfile Host multi-stage, non-root, secret qua env, KHÔNG bake secret / KHÔNG auto-migrate
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — hướng vận hành hoá (đóng gói deploy). Spec base không nói cách container hoá.
- Provenance/Evidence: `platform/src/Host/StarHill.Api/Dockerfile` (SDK 10.0 build → aspnet 10.0 runtime, `USER $APP_UID`, cổng 8080) + `platform/.dockerignore`; verified in-session 2026-07-10: `docker build` thành công; `docker run` KHÔNG secret → fail-fast (StartupValidator, đúng F35); `docker run` CÓ secret qua env (`Jwt__Keys__0__Secret`, `ConnectionStrings__Identity`) → `/health/live` = 200.
- Context: base cần đóng gói chạy được để thành sản phẩm deploy.
- Decision/Change: multi-stage (build SDK → runtime aspnet gọn); chạy **non-root** (`$APP_UID` sẵn trong image .NET — hardening); secret nạp qua ENV lúc chạy (KHÔNG bake vào image, khớp F35); migration áp out-of-band (ENTRYPOINT chỉ chạy app, không migrate).
- Rationale (verifiable): multi-stage → image runtime nhỏ, không chứa SDK/source. Non-root → giảm blast-radius nếu bị chiếm. Secret qua env → không rò secret vào layer image (bất biến F35). Đã chứng minh CẢ fail-fast (thiếu secret) LẪN happy-path (có secret → 200) bằng docker run thật.
- Alternatives: (a) single-stage (loại: image phình + chứa source/SDK); (b) chạy root (loại: rủi ro bảo mật); (c) bake secret/appsettings production vào image (loại: rò secret — vi phạm F35); (d) auto-migrate trong ENTRYPOINT (loại: race đa-instance — xem AD-050).
- Consequences: deploy cấp secret qua orchestrator (env/secret store) + chạy migration ở bước riêng. Image chỉ là runtime thuần.
- Reversibility: High (Dockerfile là artifact cộng thêm).
- Traceability: F35, AD-045 (secret ngoài repo), AD-050 (migrate out-of-band), post-base operational.

### AD-052 — CI pipeline (GitHub Actions): tool-restore + build 0-warning + full test (Testcontainers) + docker build
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — hướng vận hành hoá (tự động hoá kiểm chứng). Spec base không nói CI.
- Provenance/Evidence: `.github/workflows/ci.yml` (job `build-test`: setup-dotnet theo `global.json` → `dotnet tool restore` → `dotnet build -c Release` (cổng 0-warning) → `dotnet test` full suite; job `docker-image`: `docker build` Dockerfile Host). YAML inspection-verified; chạy trên `ubuntu-latest` (Docker sẵn → Testcontainers RabbitMQ/Postgres chạy được).
- Context: kiểm chứng thủ công không bền cho sản phẩm thương mại; cần cổng tự động mỗi push/PR.
- Decision/Change: workflow 2 job — (1) build+test toàn bộ (gồm `JournalConsistencyTests` INV-1..5 → journal lệch = fail CI, biến anti-drift thành cổng CI); (2) docker build Host. Pin SDK theo `global.json` + tool theo `.config/dotnet-tools.json` (tái lập chính xác).
- Rationale (verifiable): CI biến "build 0 warning + 227 test xanh + anti-drift INV" thành cổng KHÔNG-người-quên trên mỗi thay đổi — đúng tinh thần "con người quên, build thì không", nay áp ở tầng repo/PR. ubuntu-latest có Docker nên integration Testcontainers chạy thật (không skip). KHÔNG chạy được cục bộ (là hạ tầng CI) → verify bằng inspection + tính hợp lệ YAML + tương đương lệnh đã chạy tay trong phiên.
- Alternatives: (a) không CI (loại: dựa kiểm tay — không bền thương mại); (b) chỉ build không test (loại: mất lưới an toàn); (c) skip integration trên CI (loại: bỏ phần CP6/7/8/15/adapter — runner có Docker nên không cần skip).
- Consequences: mỗi push/PR chạy full gate. Cần secret cho phần cần (không có ở job test vì integration dùng Testcontainers tự cấp, không cần secret app).
- Reversibility: High (file workflow cộng thêm).
- Traceability: R31 (0 warning + test xanh mỗi thay đổi), AD-030 (journal-consistency gate) nâng lên tầng CI, post-base operational.

### AD-053 — Opt-in `Bedrock:ApplyMigrationsOnStartup` (mặc định TẮT) cho dev/compose; prod giữ out-of-band
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — cần chạy cả stack qua compose (single-instance) mà không phá stance out-of-band của AD-050.
- Provenance/Evidence: `platform/src/Host/StarHill.Api/Program.cs` (sau Build: `if bool.TryParse(config["Bedrock:ApplyMigrationsOnStartup"]) → scope → IdentityDbContext.Database.MigrateAsync()`); guard mặc-định-TẮT: `HostSmokeTests` (không set cờ → boot không migrate, vẫn xanh); happy-path bật: docker-compose verified in-session (host migrate → /health/ready=200).
- Context: compose/dev single-instance cần schema tự sẵn sàng để chạy nhanh; nhưng AD-050 cấm auto-migrate đa-instance (race).
- Decision/Change: thêm cờ config **mặc định FALSE**. TRUE → Host áp `MigrateAsync` lúc start (1 lần, trong scope). Compose đặt cờ = true (single-instance dev/staging). Production KHÔNG bật → giữ migrate out-of-band (AD-050) nguyên vẹn.
- Rationale (verifiable): mặc-định-tắt bảo toàn bất biến AD-050 (prod đa-instance không tự migrate); opt-in chỉ là tiện ích dev/compose single-instance (không có race vì 1 instance). Dùng `bool.TryParse` trên indexer config → không kéo thêm package Binder. Đã chứng minh CẢ nhánh tắt (HostSmokeTests xanh) LẪN nhánh bật (compose /health/ready=200).
- Alternatives: (a) luôn auto-migrate (loại: phá AD-050 đa-instance); (b) migrator service riêng dùng EF bundle (đúng nhất cho prod nhưng nặng — hoãn; compose dev không cần); (c) không hỗ trợ (loại: compose phải migrate thủ công, kém tiện dev).
- Consequences: dev/compose bật cờ = tiện; prod để tắt + migrate out-of-band. Tài liệu hoá rõ trong compose + Program.cs comment.
- Reversibility: High (cờ cộng thêm; bỏ = quay lại chỉ out-of-band).
- Traceability: AD-050 (out-of-band prod), F35, post-base operational.

### AD-054 — docker-compose full-stack (PostgreSQL + Host) chứng minh sản phẩm chạy end-to-end
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — capstone vận hành hoá: chạy cả hệ để kiểm chứng tích hợp thật.
- Provenance/Evidence: `platform/docker-compose.yml` (service `postgres` healthcheck pg_isready + `host` build từ Dockerfile, depends_on service_healthy, env connection/secret/ApplyMigrationsOnStartup); verified in-session 2026-07-10: `docker compose up -d --build` → postgres healthy → host migrate → **/health/ready=200** (log `SELECT 1` readiness DB thật) + /health/live=200 → `docker compose down -v` sạch.
- Context: các mảnh (migration, image) đã verify riêng; cần bằng chứng CHÚNG chạy CÙNG NHAU (Host nối Postgres thật + readiness DB-backed).
- Decision/Change: compose 2 service — postgres (healthcheck gating) + host (build Dockerfile, migrate opt-in, secret+connection qua env). `depends_on: condition: service_healthy` → host chỉ start khi DB sẵn sàng. Port 18080→8080.
- Rationale (verifiable): /health/ready dùng `AddDbContextCheck` → 200 CHỈ khi Host kết nối được Postgres; container còn sống + serve 200 ⇒ migrate thành công (MigrateAsync fail → app crash → không serve). Vậy một lần `compose up` + /health/ready=200 chứng minh: build image + nối DB + migrate + boot fail-fast pass + serve — toàn chuỗi.
- Alternatives: (a) chỉ chạy Host image trần (đã làm AD-051 — nhưng không có DB thật); (b) thêm RabbitMQ + dispatcher vào compose (hoãn: Host chưa wire adapter/dispatcher — là feature riêng, không over-build ở bước này).
- Consequences: dev/staging có lệnh một dòng dựng cả hệ. Mở rộng: thêm RabbitMQ + registry push + k8s manifest khi cần.
- Reversibility: High (compose file cộng thêm).
- Traceability: AD-050/051/053, F35, post-base operational.

### AD-055 — EF migration bundle self-contained = artifact migrate OUT-OF-BAND cho production (đóng loop AD-050)
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — AD-050/AD-053 nêu "prod migrate out-of-band" nhưng chưa tạo artifact; opt-in-startup (AD-053) chỉ cho dev/single-instance. Cần cơ chế đúng cho prod đa-instance.
- Provenance/Evidence: `dotnet ef migrations bundle --self-contained -r <rid>` sinh executable độc lập; verified in-session 2026-07-10: sinh bundle win-x64 (103.9 MB) → chạy `efbundle --connection <Postgres container>` → log "Applying migration '20260710040446_InitialCreate'. Done." → `\dt identity.*` cho 3 bảng (inbox/outbox/refresh_token). CI job `migration-bundle` trong `.github/workflows/ci.yml` sinh bundle linux-x64 + upload artifact; `.gitignore` loại `efbundle*` (không commit ~100MB).
- Context: production đa-instance KHÔNG được auto-migrate (race — AD-050); target runtime KHÔNG có SDK/ef-tool. Cần artifact áp migration ở bước deploy, tách khỏi app.
- Decision/Change: pipeline deploy dùng **EF migration bundle** (self-contained, per-module) — bước deploy chạy `./efbundle-identity --connection "$CONN"` TRƯỚC khi rollout Host mới. App KHÔNG tự migrate ở prod (opt-in-startup AD-053 chỉ dev/compose).
- Rationale (verifiable): bundle self-contained không cần .NET SDK/ef-tool trên target (chỉ 1 executable + connection) → hợp deploy container/CD. Áp ở bước riêng (không trong app) → kiểm soát thứ tự (migrate xong mới rollout), không race đa-instance, rollback được. Đã chứng minh bundle ÁP migration thật lên Postgres (không chỉ sinh file).
- Alternatives: (a) `dotnet ef database update` ở deploy (cần SDK+tool+source trên runner deploy — nặng hơn bundle); (b) auto-migrate app (loại: race đa-instance — AD-050); (c) SQL script thủ công (loại: dễ lệch model). Bundle là chuẩn EF cho CD.
- Consequences: CI sinh bundle như artifact; CD chạy bundle trước rollout. Mỗi module một bundle (per-module §4.6). Bundle không commit (gitignore).
- Reversibility: High (bundle là artifact sinh lại được; đổi sang `database update` chỉ là đổi lệnh CD).
- Traceability: design §4.6, AD-050/AD-053, F35, post-base operational.
### AD-056 — Worker LÊN LỊCH dispatcher outbox = OPT-IN ở base (`AddOutboxDispatcherWorker<TContext>`); Host bật qua config (mặc định TẮT) — REFINES AD-047 (không mâu thuẫn)
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — post-base operational; đóng khoảng hở runtime N-059 ("chưa gì tự chạy dispatcher"). Đã validate design + AD-047 trước khi code (design-first).
- Provenance/Evidence: `platform/src/Bedrock.Infrastructure/Persistence/Messaging/OutboxDispatcherHostedService.cs` (BackgroundService generic) + `OutboxDispatcherWorkerOptions.cs` (PollInterval mặc định 5s, named-options per-context) + `OutboxDispatcherExtensions.AddOutboxDispatcherWorker<TContext>` (opt-in, `AddHostedService`); Host `Program.cs` khối gate `if bool.TryParse(config["Bedrock:Messaging:Enabled"])` → `AddRabbitMqMessaging + AddOutboxDispatcher<IdentityDbContext> + AddOutboxDispatcherWorker<IdentityDbContext> + AddIntegrationEventRegistry`. Verified in-session 2026-07-10: `Messaging.IntegrationTests/OutboxDispatcherWorkerEndToEndTests` (Postgres+RabbitMQ THẬT) — seed outbox → worker qua đường THẬT (`AddOutboxDispatcherWorker`→`AddHostedService`→`BackgroundService.StartAsync`) TỰ ĐỘNG claim+publish+mark, KHÔNG gọi `DispatchPendingAsync` thủ công; full suite 229 xanh, 0 warning, 0 skip; `StarHill.Api.Tests` (3) vẫn xanh với messaging TẮT (không hồi quy).
- Context: `EfOutboxDispatcher` (một lượt phát) đã có + verify CP15; publisher→broker đã verify (N-051); chuỗi đầy đủ đã verify (N-059). NHƯNG runtime KHÔNG có gì gọi `DispatchPendingAsync` theo chu kỳ → event nằm trong outbox mãi. AD-047 chốt "Host lên lịch chạy", loại phương án "base **tự chạy** BackgroundService" vì sợ HAI mô hình lịch.
- Decision/Change: base cấp SẴN vỏ poll-loop `OutboxDispatcherHostedService<TContext>` + helper **opt-in** `AddOutboxDispatcherWorker<TContext>()`. `AddBedrockPersistence`/`AddOutboxDispatcher` **KHÔNG** tự đăng ký worker — Host phải gọi tường minh. Sample Host gate sau `Bedrock:Messaging:Enabled` (mặc định TẮT, mirror opt-in migrate AD-053).
- Rationale (verifiable — vì sao đây là REFINE chứ không phá AD-047):
  - **Bản chất AD-047 = cấm "base *tự chạy*" (auto-run tạo mô hình lịch thứ 2).** Worker opt-in KHÔNG tự chạy: chỉ chạy khi Host gọi `AddOutboxDispatcherWorker` → lịch VẪN do Host quyết = MỘT mô hình duy nhất. Bất biến AD-047 nguyên vẹn.
  - **Tiền lệ:** base ĐÃ ship hosted-service opt-in `RequiredPortsValidator` (`AddBedrockStartupValidation`→`AddHostedService`). "Base có hosted-service khi Host bật" là pattern đã thiết lập; AD-047 chỉ chặn *scheduler tự chạy ngầm*.
  - **Vì sao đặt ở base (opt-in) thay vì viết tay trong Host:** (1) vòng poll là boilerplate dễ sai TINH VI — scope-per-iteration (dispatcher Scoped, N-008: resolve scoped-từ-root ném với ValidateScopes=true), không hạ host khi lỗi hạ tầng tạm, shutdown êm theo stoppingToken → viết đúng MỘT lần + guard test an toàn hơn N bản sao phân kỳ; (2) testable ngay ở `Messaging.IntegrationTests` (Postgres+RabbitMQ) không cần nạp web-exe; (3) đối xứng `AddOutboxRetention` (base cấp logic, Host lên lịch) — cùng triết lý.
  - **Mặc định TẮT ở sample Host** → không ép messaging model lên sample, smoke test boot không cần RabbitMQ (giữ xanh); bật = một cờ config.
- Alternatives:
  - (a) Viết worker tay trong Host (StarHill.Api): tôn trọng AD-047 tuyệt đối nhưng KHÔNG tái dùng (mỗi Host chép ~40 dòng dễ lệch), khó test (web-exe). Loại: theo N-041 "tránh trừu tượng sớm" đúng cho code NGHIỆP VỤ, nhưng đây là plumbing hạ tầng dễ sai → tập trung ở base tốt hơn.
  - (b) Base **tự chạy** worker trong `AddOutboxDispatcher`: loại — đây CHÍNH là thứ AD-047 cấm (auto-run, 2 mô hình lịch).
  - (c) Không làm, để runtime không tự phát: loại — nền tảng event-driven mà event không rời outbox là thiếu sót vận hành thật.
- Consequences: Host tùy chọn bật event-driven bằng cờ. Khi bật, cần RabbitMQ (adapter) + connection config; khi tắt giữ `ThrowingEventBusPublisher` (không phát). Để chuỗi "refresh token → event trên bus" chạy trong sample cần thêm: EMIT `UserTokenRefreshedIntegrationEvent` từ use case rotation (N-040 còn mở — bước kế tiếp, có chủ đích tách riêng).
- Reversibility: High (worker + helper cộng thêm; gỡ `AddOutboxDispatcherWorker` = quay về không tự phát, không đụng lõi).
- Traceability: design §7.2 (worker phát Outbox), AD-047 (refines — Host lên lịch), AD-016/AD-049 (dispatcher/claim), AD-053 (mẫu opt-in default-off), F29 (adapter Host cắm), N-008/N-059/N-040, post-base operational.
### AD-057 — EMIT `UserTokenRefreshedIntegrationEvent` từ use case rotation qua `IOutboxWriter` (đóng N-040)
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — post-base; nối phần design §7.4 để ngỏ (event khai contract-first nhưng chưa emit). Đã có consumer path (worker→RabbitMQ, AD-056) nên emission KHÔNG còn đầu cơ.
- Provenance/Evidence: `platform/src/Modules/Identity/Identity.Application/RefreshToken/RefreshAccessTokenUseCase.cs` (sau `AddAsync(newSnapshot)`, TRƯỚC `SaveChangesAsync`: `_outboxWriter.EnqueueAsync(new UserTokenRefreshedIntegrationEvent(Guid.CreateVersion7(), now, current.UserId), token)`); csproj Identity.Application ĐÃ ref Identity.Contracts "để phát event" (thiết kế đón sẵn). Verified in-session 2026-07-10: `Identity.UnitTests/RefreshAccessTokenUseCaseTests` (emit-on-success + `Failed_rotations_do_not_emit_event` 4 nhánh fail không emit + enqueue TRƯỚC SaveChanges) + `Identity.IntegrationTests/RefreshRotationEmitsEventTests` (Postgres THẬT: rotation → row `outbox_message` EventType `identity.user_token_refreshed`, payload chứa UserId, KHÔNG chứa refresh token thô, ProcessedAt null — đọc lại ở SCOPE MỚI = persist thật cùng transaction); full suite 231 xanh, 0 warning, 0 skip; smoke Host 3 xanh (không hồi quy).
- Context: N-040 để ngỏ emission ("sẽ được nối khi có consumer thật hoặc DoD task 21"). Trước AD-056 chưa có gì consume/phát → emit ra sẽ nằm chết trong outbox (không kiểm chứng được đầu-cuối) → cố ý hoãn. Nay worker phát tự động → chuỗi "refresh token → event trên bus" chạy được thật.
- Decision/Change: rotation thành công enqueue event vào outbox trong CÙNG transaction (sau consume+insert, trước SaveChanges). Chỉ emit trên đường THÀNH CÔNG. Dùng `IOutboxWriter` (namespace `Messaging`, KHÔNG `Dispatch`).
- Rationale (verifiable):
  - **Đúng chỗ trong transaction (bản chất):** enqueue TRƯỚC `SaveChangesAsync` + bên trong `ExecuteInTransactionAsync` → event + consume + insert token vào MỘT commit (CP6/F5 all-or-nothing). Rollback rotation ⇒ KHÔNG publish event ma. Chỉ emit khi thành công ⇒ không phát event cho rotation thất bại (reuse/expired/lost-race).
  - **CP11 nguyên vẹn:** `IOutboxWriter` ở `Bedrock.Application.Messaging` (không `.Dispatch`) → use case KHÔNG publish thẳng bus; ArchTest `ModuleBoundaryTests`/`UseCaseSeamTests` vẫn xanh (verified 33 arch test).
  - **Không rò bí mật:** payload chỉ mang `UserId` (không refresh token thô) — integration test assert `DoesNotContain(rawToken)`.
  - **Vì sao thêm integration test THẬT (không chỉ unit):** unit dùng fake writer; CP6 dùng DbContext test khác (không phải IdentityDbContext); smoke dừng ở validation 400 → KHÔNG nơi nào chứng minh IdentityDbContext map outbox đúng + writer cùng-context ghi được row. Integration test trên Postgres đóng đúng seam đó (bắt lỗi mapping/wiring mà fake không thấy) — "valid nhiều lần" ở đúng tầng.
- Alternatives: (a) emit qua domain event → `IDomainEventHandler` → `IOutboxWriter` (đúng khi rotation raise domain event; nhưng skeleton chưa có aggregate raise event → thêm tầng thừa, hoãn); (b) emit SAU commit (loại: mất nguyên tử — event có thể phát dù state rollback); (c) tiếp tục hoãn (loại: consumer path đã có, để ngỏ là nợ chức năng thật).
- Consequences: mỗi rotation thành công sinh một outbox row; khi Host bật messaging (AD-056) worker phát lên bus. Claim access token vẫn chỉ `sub` (role/permission cần user store — N-040 vẫn đúng phần đó). Chuỗi refresh→bus giờ chạy được trong compose nếu thêm RabbitMQ (bước hạ tầng tách riêng).
- Reversibility: High (bỏ một dòng enqueue = quay về không emit; event record giữ nguyên contract-first).
- Traceability: design §7.4 (rotation), AD-056 (worker consumer path), AD-016/AD-029 (outbox/idempotency), CP6/CP7/CP11, F25/F32, closes N-040, post-base operational.
### AD-058 — docker-compose capstone event-driven: RabbitMQ + messaging opt-in trên ARTIFACT THẬT (image build)
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — capstone: chứng minh chuỗi event-driven chạy trên IMAGE BUILD THẬT + broker thật + mạng compose (khác Testcontainers dùng ServiceCollection/localhost), đóng phần "compose demo" N-061 để ngỏ.
- Provenance/Evidence: `platform/docker-compose.yml` (+ service `rabbitmq:3.13` healthcheck `rabbitmq-diagnostics ping`; host env `Bedrock__Messaging__Enabled=true` + `RabbitMq__HostName=rabbitmq` + user `bedrock`; `depends_on rabbitmq: service_healthy`). Verified in-session 2026-07-10 (`docker compose up -d --build`): postgres+rabbitmq HEALTHY → host `/health/live`=200 + `/health/ready`=200 → log `Applying migration 'InitialCreate'` + `Outbox dispatcher worker started for IdentityDbContext (poll every 00:00:05)` + poll `... FOR UPDATE SKIP LOCKED`. Seed 1 row outbox (`identity.user_token_refreshed`) qua psql → sau 10s: `processed_at IS NOT NULL` (t), `error_count=0`, `dead_lettered_at` null → worker của IMAGE THẬT đã claim+publish tới RabbitMQ compose (publisher-confirms ack) + mark processed. `docker compose down -v` sạch.
- Context: N-056/057/059/061 để ngỏ "thêm RabbitMQ vào compose để demo end-to-end trên artifact thật". Đủ mảnh sau AD-056 (worker) + AD-057 (producer) + AD-048 (adapter). Testcontainers đã phủ code path; còn thiếu bằng chứng "IMAGE BUILD + config env + mạng compose + credential" hoạt động.
- Decision/Change: compose bật messaging opt-in + thêm RabbitMQ; verify chuỗi Outbox→worker→RabbitMQ trên image thật.
- Rationale (verifiable — vì sao KHÔNG dùng 'guest', bản chất):
  - **RabbitMQ user `guest` CHỈ connect qua LOOPBACK** (mặc định `loopback_users=guest`); container `host` → service `rabbitmq` là non-loopback → guest BỊ TỪ CHỐI. Đây là lỗi deployment THẬT mà compose-với-image phơi ra còn Testcontainers (map localhost) che mất. Fix gốc = dùng user riêng (`RABBITMQ_DEFAULT_USER=bedrock`), khớp `RabbitMq__UserName/Password` — không vá bằng bật loopback cho guest.
  - **Verify bằng `processed_at` (không cần bind queue):** publisher-confirms ack kể cả message unroutable (mandatory=false) → `processed_at` set = broker ĐÃ nhận vào exchange. Nếu broker unreachable/auth-refused → publish ném → `error_count`≥1, `processed_at` null. Nên `processed=t & error_count=0` là bằng chứng dứt khoát kết nối+publish thành công. Không cần management API/queue → ít mảnh, tất định.
  - **Bổ trợ (không trùng) với Testcontainers:** integration test phủ CODE PATH (ServiceCollection); compose phủ ARTIFACT (Dockerfile publish + config binding từ env + service networking + boot ordering + credential thật). Hai lớp khác nhau.
- Alternatives: (a) chỉ boot healthy, không seed (loại: publisher lazy → outbox rỗng KHÔNG chạm RabbitMQ → không chứng minh được kết nối broker); (b) trigger rotation qua HTTP (loại: skeleton chưa có login endpoint tạo refresh token → cần seed token; seed outbox trực tiếp đơn giản + tất định hơn); (c) dùng guest (loại: bị loopback-refuse — chính lỗi cần tránh).
- Consequences: compose giờ là full event-driven stack demo (dev/staging). Prod: KHÔNG dùng credential mặc định + secret qua store + migrate out-of-band (AD-050) — đã ghi chú trong compose.
- Reversibility: High (gỡ service rabbitmq + 4 dòng env = quay về compose Postgres+Host; cờ messaging tắt).
- Traceability: AD-056 (worker), AD-057 (producer), AD-048 (adapter RabbitMQ), AD-051/054 (Docker/compose), F29/F35, closes phần compose-demo của N-059/N-061, post-base operational.
### AD-059 — Nửa CONSUME: port `IIntegrationEventDispatcher` (Application) + core agnostic `EfIntegrationEventDispatcher` (Infrastructure), hiện thực §7.3
- Status: Confirmed
- Date: 2026-07-10
- Decider: user (chốt "core + subscriber + topology tối thiểu") + AI(Kiro) thiết kế
- Provenance/Evidence: `platform/src/Bedrock.Application/Messaging/Dispatch/{IIntegrationEventDispatcher,IncomingIntegrationMessage,InboxDispatchOutcome}.cs` + `platform/src/Bedrock.Infrastructure/Persistence/Messaging/EfIntegrationEventDispatcher.cs` + DI `OutboxDispatcherExtensions.AddIntegrationEventConsumer`. Verified in-session 2026-07-10: `Bedrock.Infrastructure.Tests/PostgresIntegrationEventDispatcherTests` (Postgres THẬT, 3 test): First_delivery_handles_once_and_records_inbox / Redelivery_is_idempotent_handler_runs_once (Duplicate, handler chạy đúng 1 lần) / Unknown_event_type_is_dead_lettered_without_inbox_or_handler. Full suite 235 xanh, 0 warning, 0 skip.
- Context: N-063 phát hiện building-blocks consume (inbox store/registry/handler contract) CÓ nhưng KHÔNG orchestrator; §7.3 định nghĩa protocol nhưng base 21-task chỉ dựng building-blocks. Registry (AddIntegrationEventRegistry, AD-056) đang registered-but-unused.
- Decision/Change: thêm PORT `IIntegrationEventDispatcher` (namespace `Messaging.Dispatch`) + impl agnostic `EfIntegrationEventDispatcher`: registry resolve EventType→CLR (unknown→`DeadLettered`, không crash R17.3) → `IUnitOfWork.ExecuteInTransactionAsync`: `TryMarkProcessedAsync` (trùng→`Duplicate`) → deserialize (OutboxSerialization.Options — CỐ ĐỊNH giống producer, tolerant reader) → gọi mọi `IIntegrationEventHandler<T>` (invoker generic AD-025) → `SaveChangesAsync` (inbox+business nguyên tử) → `Handled`. Lỗi handler → NÉM (rollback+rethrow) → transport NACK. Registry nay ĐƯỢC DÙNG (hết loose-end N-063).
- Rationale (verifiable):
  - **Port ở Application, impl ở Infrastructure (bản chất CP3):** adapter subscriber (RabbitMQ) chỉ được ref `Bedrock.Application` (CP3/`AdapterIsolationTests`), KHÔNG `Bedrock.Infrastructure`. Nên logic dispatch phải lộ qua PORT ở Application để adapter gọi — ĐỐI XỨNG publish (`IEventBusPublisher` port, impl adapter). Namespace `Messaging.Dispatch` → use case KHÔNG được ref (CP11 nguyên vẹn — verified 33 arch test).
  - **Đối xứng `EfOutboxDispatcher`:** cùng triết lý cơ chế-tái-dùng-race-sensitive ở base (AD-010); invoker generic giữ kiểu exception (AD-025); transaction inbox+business nguyên tử = §7.3 postcondition (F30/CP8).
  - **Scope mỗi message:** đăng ký Scoped + subscriber tạo scope/message → inbox/handler chung PlatformDbContext đang mở transaction.
- Alternatives: (a) core generic theo TContext như EfOutboxDispatcher (loại: IInboxStore/IUnitOfWork đã trừu tượng context; non-generic gọn hơn cho single-module; multi-module = consumer per-module scope, ghi N-064); (b) đặt cả logic trong adapter (loại: phá CP3 — logic agnostic không được sống ở adapter tech; mất tái dùng cho adapter khác).
- Consequences: consume có cơ chế đúng-một-lần tái dùng; adapter bất kỳ (RabbitMQ/khác) cắm vào port. Multi-module cần consumer + scope per-module (như dispatcher per-module).
- Reversibility: Medium (port + impl + DI cộng thêm; gỡ = mất consume orchestrator).
- Traceability: design §7.3/§5.2, AD-010/AD-025/AD-029, CP3/CP8/CP11, F30/R17.3, addresses N-063, post-base operational.

### AD-060 — Subscriber RabbitMQ (adapter) + topology tối thiểu Host + chính sách NACK requeue=false (DLX cho prod)
- Status: Confirmed
- Date: 2026-07-10
- Decider: user (chốt phương án 2) + AI(Kiro)
- Provenance/Evidence: `platform/src/Adapters/Messaging.RabbitMq/{RabbitMqConsumer.cs,RabbitMqConsumerOptions.cs}` + `RabbitMqMessagingExtensions.AddRabbitMqConsumer`; Host `Program.cs` (block messaging: `AddIntegrationEventConsumer` + handler demo `UserTokenRefreshedLogHandler` + `AddRabbitMqConsumer(queue=starhill.identity, binding=identity.#)`). Verified in-session 2026-07-10: `Messaging.IntegrationTests/RabbitMqConsumeEndToEndTests` (Postgres+RabbitMQ THẬT: publish→subscriber qua đường thật `AddRabbitMqConsumer`→`AddHostedService`→`BackgroundService`→dispatch→handler chạy 1 lần + inbox mark persist). Full suite 235 xanh, 0 warning, 0 skip.
- Context: cần transport nhận message từ RabbitMQ và feed core AD-059. Topology (queue/binding/nack) là quyết định app (N-063).
- Decision/Change: `RabbitMqConsumer : BackgroundService` (mirror publisher): declare exchange(topic,durable)+queue(durable)+binding + QoS prefetch + `BasicConsumeAsync` (manual ack); mỗi delivery tạo DI SCOPE → resolve port `IIntegrationEventDispatcher` → dispatch → ACK (Handled/Duplicate/DeadLettered) hoặc NACK requeue=false (handler lỗi). Header decode: MessageId=`BasicProperties.MessageId`, event-type=header (byte[]→UTF8). Envelope hỏng → ACK-drop+log. Topology Host: queue `starhill.identity`, binding `identity.#`, consumer-name=queue.
- Rationale (verifiable):
  - **NACK requeue=false (không requeue=true):** requeue=true → hot-loop VÔ HẠN khi handler luôn lỗi (poison) — tệ hơn. requeue=false tránh hot-loop; production cấu hình DLX (`x-dead-letter-exchange` qua `QueueArguments`) để requeue=false ĐI VÀO DLX thay vì drop. Race-loser PK inbox: winner đã handle → drop loser an toàn (hiệu ứng đã xảy ra 1 lần).
  - **Scope mỗi delivery:** dispatcher/handler/inbox Scoped → mỗi message một PlatformDbContext → an toàn xử lý song song (prefetch>1).
  - **Adapter ref port (Application), KHÔNG Infrastructure:** giữ CP3 (`Hosting.Abstractions` không nằm trong danh sách cấm CP3). Verified build + AdapterIsolationTests xanh.
  - **Header event-type là byte[]:** AMQP field table encode string thành longstr → consume ra byte[]; decode UTF-8 (không giả định string).
- Alternatives: (a) requeue=true (loại: hot-loop poison); (b) không manual-ack/autoAck=true (loại: mất message khi crash giữa nhận và xử lý — phá at-least-once); (c) bake DLX mặc định vào base (loại: topology là app — N-063; để `QueueArguments` cho Host khai).
- Consequences: sample Host consume `identity.#` + log; production PHẢI cấu hình DLX (nếu không, handler-lỗi-liên-tục = drop). Ghi rõ trong code + N-064.
- Reversibility: High (bỏ AddRabbitMqConsumer = không consume; core AD-059 vẫn dùng được với adapter khác).
- Traceability: AD-059 (core/port), AD-048 (publisher đối xứng), F29/F35/CP3, design §7.3, post-base operational.

### AD-061 — CI hardening: gate nhánh tích hợp `develop` + concurrency cancel-in-progress + least-privilege permissions + job timeouts — REFINES AD-052
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — phát hiện khiếm khuyết THẬT khi rà CI (AD-052) đối chiếu thực tế nhánh git; user duyệt hướng "CI/anti-drift cho sản phẩm thương mại lâu dài".
- Provenance/Evidence: `.github/workflows/ci.yml` (đã sửa: `push.branches` = `[main, master, develop]`; thêm `concurrency: {group: ci-…-${{ github.ref }}, cancel-in-progress: true}`; `permissions: {contents: read}`; `timeout-minutes` 30/20/20 cho build-test/docker-image/migration-bundle). VERIFY: `git log --merges` = **0 merge toàn lịch sử** + `origin/HEAD → develop` + mọi commit đổ thẳng develop → chứng minh `push:[main,master]`+`pull_request` CŨ khiến CI **chưa từng chạy**. Validate cú pháp: PyYAML 6.0.2 `safe_load` OK; assert `push.branches` chứa develop, `concurrency`/`permissions`/`timeouts` đúng shape (chạy in-session).
- Context: AD-052 dựng CI làm cổng anti-drift ở quy mô team (0-warning + full test Testcontainers + JournalConsistency INV-1..5). NHƯNG trigger chỉ `push` vào `main/master` + `pull_request`. Nhánh làm việc thực tế là `develop` (origin/HEAD), và lịch sử KHÔNG có PR nào (0 merge) → cổng gần như **không bao giờ kích hoạt** trên nhánh nơi công việc thật đổ vào. Bản chất: cơ chế chống-drift mạnh nhất đang NGỦ.
- Decision/Change: (1) thêm `develop` vào `push.branches` → mọi commit vào nhánh tích hợp chạy cổng. (2) `concurrency` group theo `github.ref` + `cancel-in-progress` → push mới hủy run cũ CÙNG ref (tiết kiệm runner + tránh nhiều run e2e song song tranh Docker/Testcontainers — đúng lớp flaky N-053/N-064; group theo ref nên nhánh khác KHÔNG hủy nhau). (3) `permissions: contents: read` cấp workflow → least-privilege (checkout + upload-artifact không cần quyền ghi). (4) `timeout-minutes` mỗi job → chặn treo Docker/Testcontainers ăn hết 6h mặc định.
- Rationale (verifiable): **fix TẬN GỐC, không fix ngọn** — gốc là "cổng không chạy trên nhánh thật", nên sửa đúng danh sách trigger để cổng thực thi đúng chỗ; các mục còn lại là hardening có căn cứ TỪ CHÍNH bài học/nguyên tắc dự án (contention Testcontainers N-053/N-064 → concurrency; least-privilege F35/tinh thần AD-038 → permissions; robust vận hành → timeout). Không suy đoán: mỗi thay đổi kiểm chứng được (git evidence + PyYAML parse + GHA schema chuẩn).
- Alternatives: (a) chỉ dựa `pull_request` + bắt buộc PR workflow (loại: đổi quy trình làm việc của team đơn phương; hiện 0 PR → cổng vẫn ngủ ngay); (b) chạy CI mọi nhánh `**` (loại: tốn runner cho nhánh nháp; develop là đủ vì là nhánh tích hợp); (c) không concurrency (loại: giữ nguy cơ contention + phí runner đã ghi nhận); (d) giữ permissions mặc định (loại: quyền rộng không cần thiết — trái least-privilege).
- Consequences: từ nay push develop chạy full cổng (0-warning + Testcontainers + JournalConsistency) → drift bị bắt tự động trên nhánh thật. Run cũ bị hủy khi có push mới cùng ref (chấp nhận được: commit mới nhất mới là thứ cần gác). KHÔNG execute được Actions cục bộ (cần push GitHub) — verify tối đa ở mức cú pháp/schema + lý lẽ; lần chạy thật đầu tiên sẽ do push develop kích hoạt.
- Reversibility: High (chỉ sửa YAML khai báo; revert = bỏ develop/concurrency/permissions/timeouts).
- Traceability: AD-052 (CI gốc), AD-030 (JournalConsistency chạy trong CI), N-053/N-064 (contention Testcontainers), F35/AD-038 (least-privilege), post-base operational anti-drift.

### AD-062 — Command-execution governance: launcher CỐ ĐỊNH `scripts/vp.cmd` → `tools/verify.ps1` + `tests/validate_ci.py` (không one-liner ad-hoc)
- Status: Confirmed
- Date: 2026-07-10
- Decider: user (ra luật) + AI(Kiro) chọn cấu trúc/vị trí. User: "CHỈ chạy lệnh qua launcher/script cố định; TUYỆT ĐỐI KHÔNG `python -c` hay one-liner powershell tuỳ biến; logic mới → bỏ vào script cố định".
- Provenance/Evidence: `platform/scripts/vp.cmd` (launcher mỏng) → `platform/tools/verify.ps1` (orchestrator: scope build|ci|test|journal|all) + `platform/tests/validate_ci.py` (validate `.github/workflows/ci.yml`, thay logic `python -c` ad-hoc lần trước). VERIFY: chạy `scripts\vp.cmd` in-session → build 0-warning OK + `VALIDATE CI: OK` + test 235 (219 pass/16 skip/0 fail) → exit 0. 
- Context: lần trước tôi validate CI bằng `python -c "..."` (one-liner) — không tái lập, không review được, là nguồn drift/rủi ro. User siết governance: mọi thao tác qua entry-point cố định, versioned, review được.
- Decision/Change: (1) MỘT lệnh ổn định `vp` (verify platform) = `scripts/vp.cmd`; KHÔNG viết logic trong .cmd — ủy quyền `tools/verify.ps1`. (2) Toàn bộ logic build/test/validate-ci/journal sống trong `verify.ps1` (+ `validate_ci.py` cho phần CI-yaml) → "cần logic mới → sửa file cố định, không đổi tên lệnh". (3) **Vị trí = TRONG `platform/`** (`platform/scripts/vp.cmd` + `platform/tools/verify.ps1` + `platform/tests/validate_ci.py`) → base TỰ-CHỨA (có thể tách repo riêng sau, cô lập khỏi QR); riêng `.github/workflows/ci.yml` PHẢI ở repo-root (yêu cầu GitHub) nhưng nhắm `working-directory: platform`. (Ban đầu đặt repo-root; DỜI vào platform/ theo yêu cầu cô lập base — N-072.) (4) `validate_ci.py` xử lý quirk YAML-1.1 (`on:`→bool True) để đọc đúng trigger.
- Rationale (verifiable): entry-point cố định = tái lập + review được + chống drift (một nguồn sự thật cho "cách verify", không rải rác one-liner trí-nhớ). Đây là lá chắn anti-drift ở tầng THAO TÁC (bổ sung cho guard-test tầng CODE và JournalConsistency tầng TÀI LIỆU). Thin-launcher→logic-file: đổi lệnh gọi (cmd) hiếm, đổi logic (ps1/py) thường → tách để sửa logic không phá hợp đồng lệnh.
- Alternatives: (a) tiếp tục one-liner ad-hoc (loại: user cấm; không tái lập/review); (b) nhét logic vào vp.cmd (loại: cmd nghèo nàn, khó bảo trì; ps1 mạnh hơn); (c) [ĐÃ CHỌN sau — N-072] đặt scripts trong platform/ (base tự-chứa); validate_ci vẫn nhắm CI repo-root qua path tương đối `parents[2]`, không làm bẩn test-project vì là file .py rời (không csproj nào ref).
- Consequences: từ nay mọi verify chạy `vp` (all|build|ci|test|journal). Thêm bước kiểm mới → sửa `verify.ps1`/`validate_ci.py`. `vp` là công cụ DEV cục bộ (Windows, có python); CI trên runner vẫn dùng trực tiếp `ci.yml` (không phụ thuộc vp).
- Reversibility: High (script cộng thêm; xóa = quay lại gọi dotnet trực tiếp).
- Traceability: user command-governance rule 2026-07-10; AD-061 (validate_ci nội dung); AD-030 (anti-drift tầng tài liệu) — vp là anti-drift tầng thao tác; post-base operational.

### AD-063 — Fix GỐC: Testcontainers fixture build container LAZY trong try/catch → thiếu Docker thì SKIP (đúng hợp đồng N-012), không FAIL
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — phát hiện khi chạy suite trên máy không Docker: 16 test integration FAIL thay vì SKIP dù mọi test có `[SkippableFact]`+`Skip.IfNot(Available)` và comment ghi rõ "skip nếu thiếu Docker (N-012)".
- Provenance/Evidence: 7 file fixture (`Bedrock.Infrastructure.Tests/PostgresOutboxInboxTests.cs` PostgresFixture; `Identity.IntegrationTests/{IdentityMigrationTests,RefreshRotationEmitsEventTests}.cs`; `Messaging.IntegrationTests/{RabbitMqConsumeEndToEndTests,OutboxToRabbitMqEndToEndTests,OutboxDispatcherWorkerEndToEndTests}.cs`; `Adapters.Messaging.RabbitMq.Tests/RabbitMqPublishIntegrationTests.cs`). VERIFY: `scripts\vp.cmd` in-session → total 235, **0 failed, 16 skipped** (trước fix: 10 failed chỉ riêng Bedrock.Infrastructure.Tests do `DockerUnavailableException`). Máy có Docker: hành vi KHÔNG đổi (Build+Start đều trong try, đều chạy).
- Context: container tạo bằng field-initializer `new PostgreSqlBuilder(...).Build()` / `new RabbitMqBuilder(...).Build()`. `.Build()` gọi `Validate()` → **ping Docker và NÉM `DockerUnavailableException` nếu thiếu Docker** (thấy trong stack trace). `try/catch` lại CHỈ bọc `StartAsync()` → `.Build()` ở field-init ném TRƯỚC khi fixture ctor xong → toàn bộ test trong collection FAIL lúc khởi tạo, không bao giờ tới `Skip.IfNot`. Bug ẩn trên máy có Docker (máy kia/CI luôn có Docker → 235 xanh), chỉ lộ trên máy không Docker.
- Decision/Change: chuyển `new XxxBuilder(...).Build()` từ field-initializer VÀO trong khối `try` của `InitializeAsync` (trước `StartAsync`); field khai `= null!` (gán trong Initialize). Thiếu Docker → `.Build()` ném → `catch` → `Available=false` → `[SkippableFact]` + `Skip.IfNot` → SKIP đúng thiết kế. Use-site (sau `Skip.IfNot`) an toàn null nhờ đã guard.
- Rationale (verifiable): fix ĐÚNG BẢN CHẤT — gốc là "kiểm-tra-Docker xảy ra ở `.Build()` (ctor) NẰM NGOÀI guard skip", nên di chuyển `.Build()` vào trong guard; KHÔNG phải vá ngọn (không lọc tên test, không xoá test). Khôi phục đúng HỢP ĐỒNG đã tài liệu hoá (`Skip nếu thiếu Docker — N-012`). Kết quả kiểm chứng được: FAIL→SKIP trên máy này; no-op trên máy Docker. Làm cổng verify cục bộ (`vp`) trở nên có nghĩa (0 fail) trên MỌI môi trường dev, kể cả không Docker.
- Alternatives: (a) lọc test theo tên/Trait trong launcher (loại: mong manh + vá ngọn, không sửa hợp đồng skip đã hỏng); (b) tách project integration riêng để loại khỏi build local (loại: không chạy được cùng suite; vẫn không sửa bản chất); (c) bọc thêm try quanh field-init bằng lazy property phức tạp (loại: rườm rà hơn di chuyển vào Initialize).
- Consequences: suite chạy sạch (skip, không fail) trên máy thiếu Docker → `vp`/dev-loop tin cậy. CI/máy có Docker: 16 test integration vẫn chạy thật (không đổi). Nếu `.Build()` thành công nhưng `.StartAsync()` fail (Docker có nhưng lỗi start) → Available=false, container không dispose (như hành vi cũ — chấp nhận, hiếm).
- Reversibility: High (đưa `.Build()` về field-init là tái hiện bug — không nên).
- Traceability: N-012 (skip-nếu-thiếu-Docker), N-049 (10 test Docker-gated), task 7.4/8.3/14 (các integration test), post-base robustness.

### AD-064 — RabbitMQ readiness health-check (design §9.6/R34) + gom `RabbitMqConnectionFactory` dùng chung (DRY)
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — phát hiện khoảng hở khi rà "sản phẩm thương mại + an toàn": adapter RabbitMQ (publisher+consumer) KHÔNG đăng ký health-check nào, dù design §9.6 nói rõ "Adapter đăng ký check riêng qua IHealthChecksBuilder trong AddYyyXxx(cfg)".
- Provenance/Evidence: grep `platform/src` "Health" → adapter KHÔNG có health-check (chỉ persistence `AddDbContextCheck` per-context AD-042). Impl: `RabbitMqHealthCheck` (IHealthCheck, mở connection ngắn → Healthy/Unhealthy) + đăng ký trong `AddRabbitMqMessaging` (`AddHealthChecks().AddTypeActivatedCheck<RabbitMqHealthCheck>("rabbitmq", Unhealthy, tags:["ready"])`) + helper `RabbitMqConnectionFactory.Create` (publisher/consumer/health-check dùng chung). Package `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.9 (CPM). Guard: `AddRabbitMqMessagingTests.Registers_rabbitmq_readiness_health_check_tagged_ready` (không Docker) + `RabbitMqPublishIntegrationTests.Health_check_reports_healthy_when_broker_reachable` ([SkippableFact], broker thật). VERIFY qua `vp`: build 0-warning + test 237 (220 pass/17 skip/0 fail) + CP3 (`AdapterIsolationTests`) vẫn xanh.
- Context: khi `Bedrock:Messaging:Enabled=true`, broker là DEPENDENCY của cả publish (worker) lẫn consume (subscriber). Trước đó `/health/ready` KHÔNG phản ánh broker → k8s readiness probe vẫn "ready" khi broker chết → pod nhận traffic nhưng outbox dồn ứ / consumer chết âm thầm (rủi ro vận hành thật).
- Decision/Change: (1) `RabbitMqHealthCheck` mở connection ngắn tới broker (Healthy nếu mở được, Unhealthy nếu ném/không mở) — `public` để `AddTypeActivatedCheck` (ActivatorUtilities) tìm được ctor lúc runtime. (2) Đăng ký trong `AddRabbitMqMessaging` với tag `"ready"` (literal, như persistence — adapter KHÔNG ref Bedrock.Api nên không dùng hằng `HealthEndpoints.ReadyTag`, giữ CP3) → tự chảy vào `/health/ready` qua `MapBedrockHealth`. (3) Gom `RabbitMqConnectionFactory.Create(options)` — publisher + consumer + health-check dùng chung (trước trùng 2 chỗ, nay 1 nguồn).
- Rationale (verifiable): §9.6/R34 yêu cầu readiness phản ánh dependency; broker là dependency khi messaging bật → thiếu check = readiness nói dối → nguy hiểm rollout/route (bản chất an toàn vận hành). Chỉ đăng ký trong `AddRabbitMqMessaging` (gọi khi messaging bật) → messaging TẮT (mặc định) không thêm check (không hồi quy HostSmokeTests). Gom ConnectionFactory = fix gốc DRY (đổi credential/TLS về sau chỉ MỘT chỗ, chống drift). Verify được cục bộ (đăng ký) + CI (hành vi broker thật).
- Alternatives: (a) không health-check (loại: vi phạm §9.6/R34; readiness mù broker — rủi ro thật); (b) tái dùng connection của publisher cho probe (loại: ghép chặt internals publisher/consumer; connection ngắn độc lập rõ ràng hơn — TO-010); (c) mỗi nơi tự new ConnectionFactory (loại: trùng 3 chỗ, drift credential); (d) để internal RabbitMqHealthCheck (loại: ActivatorUtilities không thấy ctor → fail runtime).
- Consequences: messaging bật → `/health/ready` phản ánh broker (chết → 503 → orchestrator ngừng route). Adapter thêm dep `Microsoft.Extensions.Diagnostics.HealthChecks` (abstraction chuẩn .NET, §9.6 sanction — không phá CP3). Probe mở connection ngắn mỗi lần (TO-010).
- Reversibility: High (health-check + helper cộng thêm; gỡ registration = quay lại readiness không có broker check).
- Traceability: design §9.6/R34, F29/I2/§17, AD-042 (health-check per-context precedent), AD-048/AD-060 (adapter publish/consume), CP3, post-base commercial hardening.

### AD-065 — Chuyển `BedrockTelemetry` xuống Bedrock.Application (shared) + metric quan sát Outbox (R24.3/§7.2) — đóng khoản hoãn N-046
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — thực thi ĐÚNG khuyến nghị đã ghi trước (N-046): "BedrockTelemetry ở Bedrock.Api → Infrastructure không emit được metric dưới nguồn chung; NÊN chuyển xuống khi cần instrument outbox-lag"; R24.3 catalog ghi outbox-lag + dead-letter count = "instrument khi finalize".
- Provenance/Evidence: move `platform/src/Bedrock.Application/Observability/BedrockTelemetry.cs` (từ Bedrock.Api; namespace `Bedrock.Application.Observability`; `Meter`/`ActivitySource` "Bedrock"); `BedrockObservabilityExtensions` cập nhật using (Api → Application OK theo matrix). `platform/src/Bedrock.Infrastructure/Persistence/Messaging/OutboxMetrics.cs` (3 instrument dưới Meter "Bedrock") + emit trong `EfOutboxDispatcher.TryPublishAsync` (published+lag khi success; dead_lettered khi vượt MaxAttempts). Guard: `Bedrock.Infrastructure.Tests/OutboxMetricsTests` (MeterListener, SQLite, KHÔNG Docker; collection `DisableParallelization` chống ô nhiễm metric process-global). VERIFY qua `vp`: build 0-warning + test 239 (222 pass/17 skip/0 fail); telemetry test cũ (BedrockPipelineTests) vẫn xanh (AddMeter("Bedrock") vẫn gom sau move).
- Context: `BedrockTelemetry` (Meter "Bedrock") nằm ở Bedrock.Api, nhưng XML-doc của chính nó nói "Api/Infrastructure/module cùng phát dưới Name". Vì Api⊥Infra (matrix §3.3), Infrastructure (nơi có dispatcher/outbox) KHÔNG ref được → KHÔNG emit metric hạ tầng dưới nguồn chung. R24.3/§7.2 yêu cầu outbox-lag + dead-letter count nhưng chưa có instrument nào (chỉ Meter rỗng được AddMeter).
- Decision/Change: (1) chuyển `BedrockTelemetry` xuống **Bedrock.Application** (tầng chung Api+Infra+module đều ref) — dùng BCL `System.Diagnostics.Metrics` (không dep tech, không phá whitelist §17/DependencyRuleTests). (2) `OutboxMetrics` (Infrastructure) phát dưới Meter "Bedrock": counter `bedrock.outbox.published`, counter `bedrock.outbox.dead_lettered` (= R24.3 dead-letter count), histogram `bedrock.outbox.publish.lag` giây (now-occurred_at lúc publish = proxy outbox-lag). (3) Emit INLINE trong dispatcher (không DB-poll gauge). Không đổi wiring Api (AddMeter("Bedrock") gom sẵn).
- Rationale (verifiable): move là fix TẬN GỐC mâu thuẫn doc-vs-placement (khuyến nghị N-046) — làm lời-doc thành đúng; nguồn telemetry chung phải ở tầng chung. Emit inline tránh gauge sync-over-async + scoped-DbContext-trong-gauge (bản chất kỹ thuật). "publish-lag" (tuổi-lúc-publish) đo trễ end-to-end thực dụng cho alerting + rẻ + inline; "oldest pending age" tuyệt đối cần query pending riêng → hoãn (TO-011). Verify cục bộ bằng MeterListener trên SQLite (không Docker) + `DisableParallelization` để đếm chính xác (Meter static process-global — bài học tránh flaky).
- Alternatives: (a) giữ BedrockTelemetry ở Api + Infra tự tạo Meter "Bedrock" riêng (loại: trùng tên ở 2 assembly → drift); (b) meter con "Bedrock.Messaging" + Api wildcard `AddMeter("Bedrock*")` (khả thi, OTel hỗ trợ, nhưng move đúng khuyến nghị N-046 hơn + gom một nguồn); (c) ObservableGauge DB-poll oldest-pending-age (loại giai đoạn này: sync-over-async + scoped context trong gauge callback — phức tạp/giòn; TO-011 hoãn).
- Consequences: mọi tầng giờ emit telemetry dưới `Bedrock.Application.Observability.BedrockTelemetry`. Outbox phát 3 metric → OTLP (khi Host cấu hình endpoint). Component khác (consumer/external-auth) có thể thêm instrument dưới cùng Meter sau. `BedrockTelemetry` thành public API của Application.
- Reversibility: High (metric cộng thêm; move là đổi namespace 1 type + 1 reference).
- Traceability: R24.3/§7.2/§9.3, F34, N-046 (khuyến nghị move — nay thực thi), AD-044 (correlation/trace), AD-016 (dead-letter), post-base observability finalize.

### AD-066 — Metric quan sát CONSUME (R24.3 "consumer time") trong `EfIntegrationEventDispatcher` — đối xứng AD-065
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — hoàn tất phần R24.3 catalog "consumer time" (N-046 ghi hoãn "instrument khi finalize"); đối xứng với outbox metrics (AD-065).
- Provenance/Evidence: `platform/src/Bedrock.Infrastructure/Persistence/Messaging/InboxMetrics.cs` (counter `bedrock.inbox.dispatched` tag `outcome` + histogram `bedrock.inbox.processing.duration` s, dưới Meter chung "Bedrock") + emit trong `EfIntegrationEventDispatcher.DispatchAsync` (try/finally + `Stopwatch.GetElapsedTime`). Guard: `Bedrock.Infrastructure.Tests/InboxMetricsTests` (SQLite, KHÔNG Docker, 4 outcome: handled/duplicate/dead_lettered/failed) — cùng collection `MessagingMetricsSerial` (DisableParallelization). VERIFY qua `vp`: build 0-warning + test 243 (226 pass/17 skip/0 fail).
- Context: sau AD-065 (publish metrics), phía CONSUME (`EfIntegrationEventDispatcher` — core agnostic mọi transport đi qua) chưa có metric nào → không quan sát được tỉ lệ handled/duplicate/dead-letter/lỗi + thời gian xử lý (R24.3 "consumer time"). Grep xác nhận 0 instrument phía consume.
- Decision/Change: (1) đo trong dispatch CORE (không ở adapter subscriber) — vì core là điểm agnostic mọi transport chảy qua → metric áp dụng cho MỌI adapter tương lai (không lặp mỗi adapter). (2) một counter `bedrock.inbox.dispatched` với tag `outcome` (thay 4 counter riêng — chuẩn OTel dimension) = handled/duplicate/dead_lettered/**failed**. (3) `failed` (KHÔNG phải InboxDispatchOutcome — lỗi handler = NÉM exception → NACK) ghi trong `finally` khi outcomeTag chưa set (exception propagate) → quan sát được TỈ LỆ LỖI CONSUME (quan trọng cho alert). (4) histogram `processing.duration` = thời gian toàn bộ DispatchAsync.
- Rationale (verifiable): đo ở core = phủ mọi transport + đúng nơi biết outcome; tag outcome cho phép alert theo tỉ lệ (failed/dead_lettered cao = poison/hạ tầng). Đo INLINE (Stopwatch) rẻ, chính xác. `failed` bắt được nhánh handler-ném mà 3 outcome enum không biểu diễn — quan sát vận hành thật. Verify cục bộ trên SQLite (dispatch thật qua UoW/inbox/handler) bằng MeterListener + DisableParallelization (Meter static — chống flaky, bài học AD-065).
- Alternatives: (a) đo ở adapter subscriber RabbitMqConsumer (loại: lặp mỗi adapter + không phủ transport khác; core là điểm chung đúng); (b) 4 counter riêng thay tag (loại: OTel khuyến nghị dimension bằng tag; khó tổng hợp); (c) bỏ "failed", chỉ 3 enum outcome (loại: mất quan sát tỉ lệ lỗi consume — R24.3 cần); (d) ObservableGauge (loại: đây là event-đếm, counter/histogram đúng bản chất).
- Consequences: mỗi lần consume phát 1 counter (tag outcome) + 1 histogram duration → OTLP khi Host cấu hình. Observability giờ đối xứng publish↔consume. `bedrock.inbox.*` + `bedrock.outbox.*` cùng Meter "Bedrock".
- Reversibility: High (metric cộng thêm; wrap try/finally cục bộ trong dispatcher).
- Traceability: R24.3/§7.2/§7.3/§9.3, F34, AD-065 (đối xứng publish + move BedrockTelemetry), AD-059 (dispatch core), N-046 (catalog hoãn — nay finalize), post-base observability.

### AD-067 — Bộ security response headers của base (nosniff/DENY/no-referrer/none), CSP để app; + guard test (§3.5 slot #5 chỉ nói "SecurityHeaders")
- Status: Confirmed
- Date: 2026-07-10
- Decider: AI(Kiro) — formalize + guard một quyết định TRƯỚC ĐÓ ngầm & KHÔNG có guard: design §3.5 pipeline slot #5 chỉ ghi "SecurityHeaders — áp headers cho mọi response" NHƯNG không nói bộ header/giá trị cụ thể; `SecurityHeadersMiddleware` đã hiện thực (bởi máy kia) nhưng KHÔNG có test nào guard.
- Provenance/Evidence: `platform/src/Bedrock.Api/HttpSecurity/SecurityHeadersMiddleware.cs` (set `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `X-Permitted-Cross-Domain-Policies: none` qua `OnStarting`); guard MỚI `platform/tests/Bedrock.Api.Tests/HttpSecurity/SecurityHeadersTests.cs` (Theory /ok 200 + /boom 500 → 4 header + giá trị có mặt CẢ trên response lỗi). VERIFY qua `vp`: build 0-warning + test **245** (228 pass/17 skip/0 fail; +2). Grep xác nhận trước đó 0 test guard các header này (drift bảo mật thầm lặng nếu ai xoá/đổi).
- Context: keystone anti-drift (05): "KHÔNG có quyết định code-enforceable nào mà thiếu guard test". SecurityHeaders là control BẢO MẬT code-enforceable nhưng thiếu guard → vi phạm keystone. Đồng thời design không chốt BỘ header cụ thể → việc chọn header/giá trị là autonomous decision chưa ghi journal.
- Decision/Change: (1) formalize bộ header phòng thủ TRUNG LẬP NGHIỆP VỤ, an toàn cho MỌI app: `nosniff` (chống MIME-sniffing), `X-Frame-Options: DENY` (chống clickjacking — API không nên bị frame), `Referrer-Policy: no-referrer` (không rò Referer), `X-Permitted-Cross-Domain-Policies: none`. (2) **KHÔNG set CSP mặc định** — CSP phụ thuộc frontend/CDN/inline-script của từng app; base ép CSP sẽ VỠ app (cùng tinh thần AD-035 "cái gì phụ-thuộc-deployment/app thì để app"). App tự thêm CSP qua middleware riêng khi cần. (3) thêm guard test (e2e) khoá 4 header + giá trị, kiểm cả trên response lỗi (headers set qua `OnStarting` → có mặt trước body kể cả 500).
- Rationale (verifiable): 4 header này universally-safe cho bất kỳ HTTP API (không cấu hình app-specific) → hợp lý set mặc định ở base. CSP thì app-specific → KHÔNG ép (fix đúng bản chất: base cấp cái phổ quát, app quyết cái riêng). Guard test đóng lỗ hổng anti-drift: security control giờ FAIL BUILD nếu bị gỡ/đổi. Verify e2e không cần Docker (WebApplicationFactory/TestServer). Kiểm cả nhánh lỗi 500 = bằng chứng "MỌI response" (đúng doc middleware).
- Alternatives: (a) để nguyên không guard (loại: vi phạm keystone; security regress thầm lặng); (b) set CSP mặc định (loại: vỡ app có frontend/Swagger — app-specific, như AD-035); (c) làm 4 header cấu hình được qua options (hoãn: hiện giá trị universally-safe, chưa có app cần khác; thêm options khi có nhu cầu — tránh over-engineering).
- Consequences: mọi response (kể cả lỗi) mang 4 header phòng thủ; đổi/gỡ chúng → `SecurityHeadersTests` FAIL. App cần CSP tự thêm. Nếu sau này cần header cấu hình được → nâng qua `HttpSecurityOptions` (backward-compat).
- Reversibility: High (guard test cộng thêm; bộ header là 4 dòng trong middleware).
- Traceability: design §3.5 slot #5, §1 (security headers preserved), F17/§9.5, AD-035 (app/deployment-specific để Host/app), keystone anti-drift (05), post-base security hardening.

### AD-068 — OpenAPI doc-gen của base qua native `Microsoft.AspNetCore.OpenApi` (.NET 10), OPT-IN — hoàn tất phần base DV-015 (R22.1/§9.1/§120)
- Status: Confirmed
- Date: 2026-07-10
- Decider: user duyệt (khuyến nghị A) + AI(Kiro). Hoàn tất năng lực design-mandated §120 ("Bedrock.Api gồm OpenApi") + R22.1 ("OpenAPI group theo version") mà DV-015 đã HOÃN với ghi chú "bổ sung AddBedrockOpenApi SAU".
- Provenance/Evidence: `platform/src/Bedrock.Api/OpenApi/BedrockOpenApiExtensions.cs` (`AddBedrockOpenApi` = native `AddOpenApi()` + marker; `MapBedrockOpenApi` = `MapOpenApi()`); wire opt-in trong `UseBedrockApi` (map khi marker `BedrockOpenApiMarker` có trong DI). Package `Microsoft.AspNetCore.OpenApi` 10.0.9 + pin transitive CVE `Microsoft.OpenApi` 2.7.5. Guard: `Bedrock.Api.Tests/OpenApi/BedrockOpenApiTests` (opt-in → `/openapi/v1.json` 200 + chứa endpoint versioned "oa/ping"; KHÔNG opt-in → 404). VERIFY qua `vp`: build 0-warning + test **247** (230 pass/17 skip/0 fail).
- Context: §120/R22.1 mandate OpenAPI ở Bedrock.Api; DV-015 hoãn (doc-gen là tầng trình bày, "làm khi thực sự cần — I10", lo "tăng bề mặt phụ thuộc"). Sản phẩm thương mại cần OpenAPI (client SDK-gen, hợp đồng API) = "nhu cầu thật" mà DV-015 chờ.
- Decision/Change: (1) dùng **NATIVE `Microsoft.AspNetCore.OpenApi` (.NET 10)** — bề mặt tối thiểu (đúng lo ngại DV-015: không nhồi Swashbuckle). (2) **OPT-IN**: Host gọi `AddBedrockOpenApi()`; `UseBedrockApi` tự `MapOpenApi()` KHI marker có (giữ một-dòng + không ép OpenAPI lên mọi Host — tinh thần DV-015 "Host bật"). (3) **MỘT document mặc định "v1"** (`/openapi/v1.json`) — base chỉ có V1 → đủ + tối thiểu; tách doc-per-version khi có v2 (I10). Endpoint versioned mang URL `/v{n}` → spec phản ánh version trong path (verify: doc chứa "oa/ping"). (4) Swagger UI (trình bày) VẪN do Host chọn. (5) pin transitive `Microsoft.OpenApi` 2.7.5 (GHSA-v5pm-xwqc-g5wc HIGH trên 2.0.0 mà native kéo về) — mirror cơ chế AD-028.
- Rationale (verifiable): finalize đúng phần base của §120/R22.1 mà DV-015 để mở ("bổ sung sau", Reversibility High) — KHÔNG ghi đè DV-015 mà HOÀN TẤT nó; tách doc-gen (base infra) vs UI (Host trình bày) tôn trọng ranh giới DV-015. Native = tối thiểu phụ thuộc (giải chính lo ngại DV-015). Opt-in = không ép (I10, không over-reach). Single-doc = chỉ làm khi cần (I10). CVE pin = giữ 0-warning/NuGetAudit gate. Verify e2e không-Docker: doc served + chứa endpoint versioned + opt-in guard (404).
- Alternatives: (a) Swashbuckle (loại: bề mặt lớn, DV-015 lo ngại; native đủ .NET 10); (b) doc-per-version ngay (loại: base chỉ có V1 → over-engineer; I10 — thêm khi có v2); (c) map OpenAPI mặc định trong UseBedrockApi không opt-in (loại: ép OpenAPI lên mọi Host, trái DV-015 "Host bật"); (d) để nguyên DV-015 hoãn (loại: §120/R22.1 mandate + nhu cầu thương mại thật đã tới).
- Consequences: Host bật OpenAPI bằng `AddBedrockOpenApi()` (một dòng) → `/openapi/v1.json` tự phơi, document mọi endpoint (gồm versioned). Thêm dep native OpenApi + pin Microsoft.OpenApi 2.7.5 (CVE). Khi có v2: mở rộng sang doc-per-version (ShouldInclude theo GroupName).
- Reversibility: High (opt-in, cộng thêm; gỡ = bỏ AddBedrockOpenApi call).
- Traceability: R22.1/§9.1/§6/§120, F32, DV-015 (RESOLVED phần base), AD-043 (versioning URL-segment), AD-028 (tiền lệ pin CVE transitive), I10, post-base commercial.

---

### AD-069 — Năng lực nền: dịch vi phạm UNIQUE (Npgsql 23505) → `UniqueConstraintViolationException` trung lập
- Status: Confirmed
- Date: 2026-07-11
- Decider: AI (phát hiện khi thiết kế module Rooms của sản phẩm StarHill trên base — pha starhill-qr).
- Provenance/Evidence: đọc `Bedrock.Infrastructure/Persistence/EfUnitOfWork.cs` — CHỈ dịch `DbUpdateConcurrencyException`→`ConcurrencyConflictException`; grep base `DbUpdateException|23505|PostgresException|UniqueConstraint` = 0 match (không có dịch unique-violation). `Bedrock.Domain/Results` không có exception unique. Module nghiệp vụ (Rooms: tạo phòng ux_room_number, rotate token ux_qr_active) cần bắt vi phạm UNIQUE RACE-SAFE nhưng Application ⊥ EF (matrix §3.3) → không được bắt `DbUpdateException`.
- Context: Vi phạm UNIQUE là tình huống ghi phổ biến (số phòng trùng, token active trùng). Không có exception trung lập → module hoặc phải ref EF (phá matrix) hoặc pre-check TOCTOU (không an toàn).
- Decision/Change: (1) thêm `Bedrock.Domain/Results/UniqueConstraintViolationException.cs` (trung lập, mang `ConstraintName?`) — song song `ConcurrencyConflictException`. (2) `EfUnitOfWork.SaveChangesAsync` thêm `catch (DbUpdateException) when (inner là Npgsql PostgresException SqlState 23505)` → ném `UniqueConstraintViolationException(ConstraintName)`; catch `DbUpdateConcurrencyException` (con) TRƯỚC; DbUpdateException khác (FK/check) nổi lên nguyên trạng. (3) `ExceptionHandlingMiddleware` map `UniqueConstraintViolationException`→409 (`CommonErrors.Conflict()`, code "conflict" — không thêm mã mới nên không đụng CP12 snapshot).
- Rationale (verifiable): song song năng lực đã có (concurrency) — giữ Application/Api sạch (không rò EF/Npgsql), module bắt được exception trung lập để map Error nghiệp vụ; DB-constraint là nguồn sự thật (race-safe, không TOCTOU). Bedrock.Infrastructure đã ref Npgsql (PlatformDbContext dùng `Database.IsNpgsql()`; csproj có `Npgsql.EntityFrameworkCore.PostgreSQL`) → detect `PostgresException` nhất quán, không thêm coupling mới.
- Alternatives: (a) module bắt DbUpdateException (loại: Application ⊥ EF); (b) pre-check AnyAsync (loại: TOCTOU race); (c) đặt exception ở module (loại: không tái dùng, trùng lặp). SQLite (test provider-agnostic) KHÔNG khớp filter → giữ DbUpdateException (translation là năng lực provider đích Postgres).
- Consequences: Test qua UoW trên Postgres gặp unique-violation nay nhận `UniqueConstraintViolationException` (cập nhật `RefreshTokenRotationRaceTests`); test SQLite (`RefreshTokenStoreTests.Duplicate_hash`) vẫn nhận `DbUpdateException` (đúng — filter Postgres-only). Đây là bổ sung base cho nhu cầu sản phẩm (pha starhill-qr QR-AD-010) — base vẫn domain-agnostic.
- Reversibility: Medium.
- Traceability: EfUnitOfWork §5.1, ConcurrencyConflictException (mẫu), CP12 (giữ mã "conflict"); pha QR: QR-AD-010, design-modules/02-rooms.md §1.

---

### AD-070 — `.editorconfig generated_code=true` cho EF migrations (miễn analyzer style/quality)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro)
- Provenance/Evidence: build thật `platform\scripts\vp.cmd build` FAIL `CA1861` tại `Identity.Infrastructure/Persistence/Migrations/20260712055600_AddOutboxClaimLease.cs(32)` (composite index `columns: new[]{...}`); sau khi thêm luật → `vp build` 0-warning (verified execute_pwsh phiên này). Mirror `starhill/.editorconfig` (QR-AD-009).
- Context: đợt hardening tạo migration mới `AddOutboxClaimLease` (cho A-08 outbox lease) với composite index EF sinh dạng `new[]{...}` → CA1861 (constant array arg). `platform/.editorconfig` THIẾU luật miễn analyzer cho migration (khác `starhill/` đã có QR-AD-009). `TreatWarningsAsErrors=true` biến CA1861 thành lỗi chặn build.
- Decision/Change: thêm khối `[**/Persistence/Migrations/*.cs]` → `generated_code = true` vào `platform/.editorconfig`.
- Rationale (verifiable): **Root cause:** migration là CODE SINH TỰ ĐỘNG (`dotnet ef migrations`); sửa tay mảng thành `static readonly` để né CA1861 = fragile (mất khi regenerate) = fix ngọn. Đánh dấu `generated_code` → analyzer bỏ qua đúng bản chất + future-proof MỌI migration sau (không tái diễn).
- Alternatives: (a) sửa tay migration thành `static readonly` array (loại: fragile, mất khi scaffold lại); (b) `NoWarn CA1861` toàn cục (loại: che rule ở code thường, mất bảo vệ nơi cần).
- Consequences: mọi file dưới `**/Persistence/Migrations/` coi là generated → analyzer style/quality nới cho chúng. Chấp nhận: KHÔNG viết logic tay trong migration.
- Reversibility: High (1 khối `.editorconfig`).
- Traceability: mirror QR-AD-009 (starhill); review A-08 (migration của lease); build gate R31.1.

---

### AD-071 — `JwtKeyRingOptions` đăng ký IDEMPOTENT + LAZY, chia sẻ 1 instance giữa sign (Infra) và verify (Api)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro)
- Provenance/Evidence: test thật (3 vòng `vp all` phiên này): trước fix `HostSmokeTests` 2 fail `IDX10703 key length zero` (JwtTokenService nhận key-ring rỗng); sau sửa lần 1 `Bedrock.Api.Tests` 10 fail `No service for JwtKeyRingOptions`; sau sửa lần 2 `HostSmokeTests` 3 fail `JwtKeyRingOptions có Kid trùng: dev`; sau fix cuối `vp all` = 248 test/0 fail. File: `Bedrock.Api/Authentication/BedrockAuthExtensions.cs`, `Bedrock.Infrastructure/DependencyInjection/BedrockSecurityExtensions.cs` (đã đọc).
- Context: đợt hardening (review A-18) muốn sign + verify dùng CHUNG key material đã validate. AI đợt trước thêm `services.TryAddSingleton(keyRing)` với `keyRing` bind EAGER lúc registration ở `AddBedrockAuthCore`. Vì `AddBedrockApi`(AuthCore) chạy TRƯỚC `AddBedrockSecurity`, và secret nạp MUỘN (User-Secrets/env/test-inject), eager chụp secret RỖNG rồi SHADOW instance lazy đúng → sign+verify nhận key-ring rỗng (regression IDX10703).
- Decision/Change: đăng ký binding + concrete `JwtKeyRingOptions` **IDEMPOTENT** — guard `services.All(d => d.ServiceType != typeof(JwtKeyRingOptions))` ở CẢ `AuthCore` lẫn `AddBedrockSecurity`; bind **LAZY** qua `AddOptions<JwtKeyRingOptions>().Configure(cfg.GetSection.Bind)`; `ValidateOnStart` + validator do sign-side (`AddBedrockSecurity`) sở hữu. JwtBearer verify dùng `.Configure<JwtKeyRingOptions>((o, shared) => …)` resolve instance chia sẻ.
- Rationale (verifiable): **Root cause kép:** (1) bind EAGER lúc registration chụp config rỗng (secret nạp muộn) → phải bind LAZY để thấy config nạp sau (F35); (2) hai `AddOptions().Configure` cùng `.Bind` list `Keys` → config binder APPEND → 2 kid `dev` → validator "Kid trùng" → phải IDEMPOTENT (guard theo concrete, vì `AddOptions().Configure` KHÔNG idempotent). Đạt A-18 (một instance validate chia sẻ sign+verify, không drift) MÀ `Bedrock.Api` vẫn dùng độc lập được (verify-only, không cần AddBedrockSecurity).
- Alternatives: (a) eager `TryAddSingleton(keyRing)` (loại: shadow instance rỗng — chính regression); (b) chỉ `AddBedrockSecurity` đăng ký (loại: `Bedrock.Api` standalone verify-only không resolve được → 500); (c) hai `Configure` không guard (loại: double-bind list Keys → trùng kid).
- Consequences: guard `services.All(...)` là O(n) scan descriptor lúc boot (rẻ, một lần). AuthCore chạy trước → đăng ký binding+concrete; Security thêm ValidateOnStart+validator (idempotent).
- Reversibility: Medium (đổi ở 2 extension method).
- Traceability: review A-18; REFINES AD-008 (JwtKeyRingOptions điểm-chung ký/verify), AD-045 (JWT validate-on-start).

---

### AD-072 — Outbox dispatch dùng LEASE (claim ngắn → publish ngoài transaction → finalize guard-by-lease → lease-expiry recovery)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-08)
- Provenance/Evidence: đọc code thật `Bedrock.Infrastructure/Persistence/Messaging/EfOutboxDispatcher.cs` (`ClaimBatchWithLeaseAsync` = transaction NGẮN set `ClaimId`+`ClaimedUntil` rồi commit; `TryPublishAndFinalizeAsync` publish NGOÀI transaction, `ExecuteUpdate` guard `candidate.ClaimId == claimId`; claim query có `claimed_until IS NULL OR claimed_until <= now`) + `OutboxMessage.{ClaimId,ClaimedUntil}` + migration `20260712055600_AddOutboxClaimLease` (cột + index `ix_outbox_claimable`) + `OutboxDispatcherOptions.ClaimLease` (default 5m, `IsValid` > 0). Guard test: `OutboxDispatcherTests.{Expired_lease_is_reclaimed_and_published, Active_lease_held_by_other_instance_is_not_claimed}` — SQLite local, PASS 10/10 (verified execute_pwsh phiên này).
- Context: review A-08 — dispatcher cũ (AD-016/AD-049) giữ DB transaction + row lock TRONG lúc gọi broker; broker chậm/timeout 10s/message × batch → giữ connection/lock lâu, kéo DB pool, nhân theo số module. Đợt hardening đã đổi sang lease pattern nhưng CHƯA có guard test crash-recovery → mục A-08 còn "đang dở".
- Decision/Change: (1) claim batch bằng transaction NGẮN (`ClaimId`=Guid v7 + `ClaimedUntil`=now+ClaimLease) rồi commit ngay (nhả lock); (2) publish NGOÀI transaction; (3) finalize (ProcessedAt / backoff / dead-letter) qua `ExecuteUpdate` CHỈ khi còn giữ lease (`ClaimId==claimId`); (4) claim query loại row đang có lease còn hạn → lease hết hạn cho instance khác recovery sau crash.
- Rationale (verifiable): **Root cause A-08:** giữ lock DB suốt thời gian gọi broker là sai — broker là hệ ngoài chậm/không tin cậy. Tách claim (ngắn, có lock) khỏi publish (ngoài lock) giảm blast radius. At-least-once giữ nguyên: crash sau publish trước finalize → lease hết hạn → phát lại → inbox dedupe (hiệu ứng đúng-một-lần nghiệp vụ). Finalize-guard-by-lease chống 2 instance cùng ghi.
- Alternatives: (a) giữ row-lock suốt publish (loại: chính vấn đề A-08 — giữ lock khi broker chậm); (b) không lease, chỉ `next_attempt_at` (loại: crash giữa publish→mark làm message kẹt tới hết backoff, recovery chậm + không phân biệt "đang xử lý" vs "chờ retry").
- Consequences: +2 cột (`claim_id`/`claimed_until`) + index `ix_outbox_claimable`; `ClaimLease` phải > worst-case publish một batch (nếu quá ngắn → 2 instance cùng xử lý một message, vẫn an toàn nhờ inbox nhưng phí publish).
- Reversibility: Medium (rollback về AD-016 pure row-lock được, nhưng không nên).
- Traceability: review A-08; REFINES AD-016 (atomic claim + backoff) + AD-049 (Npgsql SKIP LOCKED); R8.4–8.6; CP15; design §7.2.

---

### AD-073 — RabbitMQ reliability hardening (A-03/04/05): publisher gate-wraps-publish + reconnect + mandatory-confirm; consumer broker-side DLX/DLQ quarantine
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-03/A-04/A-05)
- Provenance/Evidence: đọc code thật:
  - `RabbitMqEventBusPublisher` — `_channelGate.WaitAsync` bao TRỌN `_resiliencePipeline.ExecuteAsync(publish+confirm+retry)` (finally Release), KHÔNG chỉ bao tạo channel (A-04); `GetOrCreateChannelUnderLockAsync` dispose+tái tạo khi `_channel`/`_connection` KHÔNG `{ IsOpen: true }` (A-04 reconnect); `BasicPublishAsync(..., mandatory: true, ...)` + `CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true)` → unroutable/không-confirm ném → dispatcher (AD-072) KHÔNG mark processed (A-05).
  - `RabbitMqConsumer` — provision durable DLX (`DeadLetterExchangeName` mặc định `bedrock.dead-letter`) + DLQ (`{QueueName}.dead-letter` bind "#") + set `x-dead-letter-exchange` trên queue chính; Handled/Duplicate → ACK; unknown event-type (DeadLettered) / handler-fail / malformed-envelope → `BasicNackAsync(requeue:false)` → DLX (quarantine BROKER-SIDE, KHÔNG drop) (A-03).
  - Guard local: `RabbitMqConsumerOptionsTests` (9 test — validate fail-fast QueueName/RoutingKeys/PrefetchCount/DeadLetterExchangeName + DLQ/consumer-name defaults) PASS 9/9 (verified phiên này). Runtime broker: `RabbitMqPublishIntegrationTests` + `RabbitMqConsumeEndToEndTests` (Testcontainers/CI).
- Context: review A-03 (consume "dead-letter" thực chất ACK/drop — unknown/malformed/handler-fail biến mất), A-04 (semaphore chỉ bao tạo channel, không bao publish → concurrent publish hỏng frame; `_connection ??=` không tái tạo connection đã đóng), A-05 (`mandatory:false` → unroutable bị discard nhưng dispatcher vẫn mark processed → mất event "giả thành công").
- Decision/Change: (A-04) gate ôm trọn publish+confirm+retry + reconnect khi đóng; (A-05) `mandatory:true` + confirmations-tracking → unroutable/không-confirm = publish failure (không mark processed); (A-03) consumer TỰ provision DLX+DLQ + `x-dead-letter-exchange`, mọi NACK requeue=false đi vào DLX (malformed/unknown/handler-fail đều quarantine, không drop). Sửa comment drift (docstring/log cũ ghi "ACK-drop" trong khi code NACK→DLX).
- Rationale (verifiable): **A-04:** `IChannel` RabbitMQ.Client 7.x KHÔNG thread-safe cho publish đồng thời → phải serialize TRỌN thao tác, không chỉ tạo channel; connection đóng phải tái tạo (nếu không publish vĩnh viễn lỗi sau mạng chập). **A-05:** publisher-confirm chỉ chứng minh broker NHẬN, mandatory mới chứng minh CÓ QUEUE route — thiếu mandatory thì unroutable bị discard mà outbox tưởng thành công (mất event). **A-03:** dead-letter BROKER-SIDE (x-dead-letter-exchange + NACK requeue=false) nguyên tử với ack, không cần app publish quarantine (không có cửa mất khi quarantine-publish lỗi) — mạnh hơn app-side quarantine mà review gợi ý.
- Alternatives: (a) app-side publish quarantine queue (loại: thêm publish có thể lỗi → cửa mất message; broker-side DLX nguyên tử hơn); (b) requeue=true khi handler lỗi (loại: hot-loop poison vô hạn); (c) giữ mandatory:false + alternate-exchange (loại: vẫn cần topology, mandatory+confirm đơn giản + đúng hơn).
- Consequences: consumer luôn tạo DLX/DLQ (dù app chưa dùng) — chi phí topology nhỏ, đổi lấy không-drop. `mandatory:true` yêu cầu có queue bound trước khi publyيش (hoặc message vào DLX/return) — khớp thứ tự provision consumer-trước hoặc retry outbox.
- Reversibility: Medium.
- Traceability: review A-03/A-04/A-05; REFINES AD-048 (publisher confirms/resilience/channel) + AD-060 (subscriber + NACK requeue=false); R8; §9.2/§7.3.

---

### AD-074 — Idempotency behavior: Complete/Abort + key namespace (operation-type + principal) — hết "claim rồi quên"
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-13)
- Provenance/Evidence: đọc code thật `Bedrock.Application/Behaviors/IdempotencyCommandUseCaseDecorator.cs` (+ `IdempotencyUseCaseDecorator`): `IdempotencyKeyScope.For<TInput> = {typeof(TInput).FullName}:{TenantId ?? UserId ?? "anonymous"}:{rawKey}`; Success → `CompleteAsync`, Failure → `AbortAsync`, exception → `AbortAsync`+rethrow. `Bedrock.Application/Ports/Caching/CachingPorts.cs` — `IIdempotencyStore` có `TryBeginAsync`/`CompleteAsync`/`AbortAsync`. Guard: `IdempotencyDecoratorTests.{Failed_inner_releases_key_so_retry_is_allowed, Different_input_types_with_same_raw_key_do_not_collide}` PASS 6/6 (verified phiên này).
- Context: review A-13 — idempotency v1 là "claim rồi quên" (chỉ `TryBegin`): inner trả Failure vẫn GIỮ key 24h; exception sau claim → retry hợp lệ bị chặn; key KHÔNG namespace → 2 command khác cùng raw key va chạm.
- Decision/Change: `IIdempotencyStore` +`CompleteAsync`/`AbortAsync`; decorator Success→Complete, Failure/exception→Abort (nhả key); key namespaced theo operation-type (`typeof(TInput).FullName`) + principal (tenant/user/anonymous).
- Rationale (verifiable): **Root cause A-13:** thiếu nhả claim → thất bại tạm thời khoá key đến hết TTL (chặn retry đúng). Abort-on-failure sửa gốc. Namespace theo type+principal chống va chạm cross-command/cross-tenant (2 lệnh khác nhau cùng "key-1" không đè nhau).
- Alternatives: (a) full state-machine InProgress/Completed/Failed + response-hash replay (loại: là enhancement; v1 gate + Complete/Abort đủ cho dedup biên — giữ scope AD-039, KHÔNG over-engineer); (b) giữ chỉ TryBegin (loại: chính vấn đề A-13).
- Consequences: adapter store phải impl Complete/Abort. GIỚI HẠN đã biết (ghi rõ, không giấu): idempotency store (distributed cache) KHÔNG nguyên tử với DB transaction nghiệp vụ → là gate best-effort ở biên, không phải exactly-once tuyệt đối; đủ cho dedup client/gateway retry.
- Reversibility: Medium.
- Traceability: review A-13; REFINES AD-039 (Idempotency v1 gate); §8.

---

### AD-075 — Domain-event dispatch LUÔN trong explicit transaction + restore-on-failure (không mất event)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-14)
- Provenance/Evidence: đọc code thật `Bedrock.Infrastructure/Persistence/EfUnitOfWork.cs` (`SaveChangesAsync`: nếu `context.Database.CurrentTransaction is null` → bọc `ExecuteInTransactionAsync(SaveChangesCoreAsync)`, ngược lại tham gia transaction hiện hành — reentrancy AD-012) + `PlatformDbContext.DispatchDomainEventsAsync` (dequeue+`ClearDomainEvents` → dispatch → `catch { RestoreDomainEvents(dequeued) per entity; throw; }`) + `Bedrock.Domain/Entities/Entity.RestoreDomainEvents` (InsertRange đầu hàng đợi). Guard: `DomainEventDispatchTests.Domain_events_are_restored_after_handler_failure` PASS 5/5 (verified phiên này).
- Context: review A-14 — (1) `PlatformDbContext.SaveChangesAsync` dispatch event TRƯỚC `base.SaveChangesAsync`; nếu caller KHÔNG bọc `ExecuteInTransactionAsync` thì handler dùng `ExecuteUpdate`/raw-SQL/side-effect nằm NGOÀI atomic boundary mà CP14 tuyên bố; (2) event bị clear TRƯỚC dispatch → handler ném thì event MẤT, retry `SaveChangesAsync` cùng context không dispatch lại.
- Decision/Change: (1) `EfUnitOfWork.SaveChangesAsync` tự mở explicit transaction khi chưa có → domain-event handler LUÔN chạy trong transaction (kể cả gọi SaveChanges trực tiếp); (2) clear event nhưng SNAPSHOT + `RestoreDomainEvents` khi dispatch ném → event không mất, retry re-dispatch được.
- Rationale (verifiable): **Root cause A-14:** handler ghi thẳng DB (ExecuteUpdate/raw-SQL) phải cùng transaction với `base.SaveChanges` để rollback đồng bộ — nếu SaveChanges không mở transaction, ghi của handler auto-commit ngay, base fail sau đó KHÔNG rollback được ghi handler (CP14 vỡ). Wrap-in-transaction sửa gốc. Restore-on-failure giữ semantics "event không mất" cho in-process dispatch.
- Alternatives: (a) dispatch sau commit (loại: mất nguyên tử — side-effect chạy dù state rollback); (b) clear chỉ sau dispatch-success (loại: handler có thể raise thêm event ở vòng sau nên phải dequeue trước; snapshot+restore đơn giản + đúng hơn).
- Consequences: mọi `SaveChangesAsync` mở một transaction (BEGIN/COMMIT — chi phí nhỏ; reentrancy AD-012 tránh lồng khi đã có transaction).
- Reversibility: Medium.
- Traceability: review A-14; REFINES AD-007 (dispatch trước commit) + AD-012 (reentrancy) + CP14; R33; §7.5.

---

### AD-076 — Fail-fast validate-on-start cho MỌI options family (A-16)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-16)
- Provenance/Evidence: đọc code thật wiring: `Bedrock.Api/HttpSecurity/BedrockHttpSecurityExtensions.cs` (`HttpSecurityOptions.Validate(options)` eager lúc registration) + `Bedrock.Infrastructure/DependencyInjection/OutboxDispatcherExtensions.cs` (`OutboxDispatcher/Worker/Retention` đều `.Configure(...).Validate(IsValid, ...).ValidateOnStart()`) + `Bedrock.Api/BedrockApiExtensions.cs` (`ObservabilityOptions` `.Validate(ObservabilityOptions.IsValid).ValidateOnStart()`) + `BedrockSecurityExtensions.cs` (`PasswordHashingOptions.Validate` eager). Guard LOCAL: `OptionsValidationTests` (PasswordHashing + Outbox Dispatcher/Worker/Retention) + `HttpSecurityOptionsTests` (proxy IP/CIDR/rate-limit/CORS) + `RabbitMqConsumerOptionsTests` (AD-073) + `RabbitMqOptionsTests` (AD-048).
- Context: review A-16 — nhiều options mới chỉ bind, KHÔNG validate: `HttpSecurityOptions` (limit/window/queue/forward, CIDR/IP sai bị BỎ QUA ÂM THẦM, CrossSite origin rỗng không fail), `PasswordHashingOptions`, `OutboxDispatcher/Worker/Retention`, `ObservabilityOptions` → lệch R13.3/R25.2 (fail-fast mọi môi trường).
- Decision/Change: mỗi options family có `Validate`/`IsValid` (biên rõ ràng) + wire fail-fast: options đăng ký qua `AddOptions<T>` dùng `.Validate(...).ValidateOnStart()`; options bind eager (HttpSecurity/PasswordHashing) gọi `Validate(...)` ngay tại `AddXxx` (ném trước Build). HttpSecurity nay REJECT IP/CIDR sai + CrossSite bắt buộc origin cụ thể (không wildcard + credentials).
- Rationale (verifiable): **Root cause A-16:** bind-không-validate = cấu hình sai lọt tới runtime rồi hỏng khó chẩn (hoặc tệ hơn: CIDR sai bị bỏ qua → proxy không được tin → IP client sai → rate-limit/audit sai). Fail-fast chuyển lỗi cấu hình về lúc BOOT (một mô hình duy nhất R25.2), CI/deploy bắt ngay.
- Alternatives: (a) validate lazy khi dùng (loại: lỗi hiện muộn, khó chẩn, có thể chạy một phần rồi mới chết); (b) bỏ qua giá trị sai (loại: chính lỗ hổng "CIDR sai âm thầm" review nêu).
- Consequences: cấu hình sai chặn boot (đúng ý). Test giữ validator là pure/đúng biên.
- Reversibility: High (validator là hàm thuần + một dòng wiring).
- Traceability: review A-16; R13.3/R25.2; bổ trợ AD-045 (JWT ValidateOnStart), AD-034 (rate-limit), AD-048/AD-073 (RabbitMq options).

---

### AD-077 — Argon2 PHC: bound tham số + enforce version + giới hạn input, TỪ CHỐI TRƯỚC allocation (A-17)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-17)
- Provenance/Evidence: đọc code thật `Bedrock.Infrastructure/Cryptography/Argon2idPasswordHasher.cs` (`Verify`: `PasswordHashingOptions.IsWithinBounds(parsed.Memory, parsed.Iterations, parsed.Parallelism, salt.Length, hash.Length)` gọi TRƯỚC `Compute` → out-of-bounds trả false KHÔNG cấp phát; `TryParse` enforce `parts[2]=="v=19"`; password `GetByteCount > 4096` → false; `Hash.EnsurePasswordSize` ném ArgumentException) + `PasswordHashingOptions` (const Min/Max: memory 8MiB..1GiB, iter 1..20, par 1..32, salt 16..64, hash 32..128). Guard: `PasswordHasherTests.{Verify_rejects_tampered_hash_with_out_of_bounds_params, Verify_rejects_unsupported_version, Oversized_password_is_bounded}` (thêm phiên này) + roundtrip/malformed cũ.
- Context: review A-17 — `Verify` parse memory/iterations/parallelism/hash-size TỪ chuỗi PHC lưu trữ rồi cấp phát/tính TRỰC TIẾP; parser KHÔNG enforce version, KHÔNG chặn tham số cực lớn → DB corruption/tampering (đổi m=2000000) gây CPU/memory DoS.
- Decision/Change: `Verify` gọi `IsWithinBounds(parsed.*)` TRƯỚC `Compute` (reject cấu hình vượt policy trước khi cấp phát); enforce Argon2 version 19; giới hạn password ≤ 4096 byte (Hash ném, Verify false); options tạo hash cũng validate bounds (AD-076).
- Rationale (verifiable): **Root cause A-17:** tin mù tham số trong dữ liệu-cung-cấp (PHC từ DB) = lỗ hổng DoS thật — một hash bị sửa m=2GB làm mỗi lần verify cấp phát 2GB. Reject-before-allocate cắt đúng gốc: kiểm biên rẻ (so sánh int) TRƯỚC thao tác đắt (cấp phát + Argon2). Version enforce tránh nhầm thuật toán/tham số khác nghĩa.
- Alternatives: (a) tin tham số PHC (loại: DoS — chính vấn đề); (b) chỉ giới hạn lúc Hash không lúc Verify (loại: Verify mới là bề mặt nhận dữ-liệu-cung-cấp/tampered).
- Consequences: hash tạo bởi cấu hình NGOÀI policy hiện tại (vd đổi bound thu hẹp) sẽ verify=false → cần rehash; đánh đổi chấp nhận (an toàn > tương thích cấu hình cực đoan). `NeedsRehash`/`VerifyResult` (rehash-on-login khi nâng cost) CHƯA thêm vào port — enhancement hoãn (PHC self-describing đã hỗ trợ về mặt dữ liệu).
- Reversibility: Medium.
- Traceability: review A-17; R27/R28; bổ trợ AD-076 (bound options), §5.7.

---

### AD-078 — Error invariant (code/message non-empty) + RateLimited thành first-class ErrorType (→429) (A-26)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-26)
- Provenance/Evidence: đọc code thật `Bedrock.Domain/Results/Error.cs` (ctor public `ArgumentException.ThrowIfNullOrWhiteSpace(code)`+`(message)`; `Error.None` qua private ctor `allowEmpty`) + `ErrorType.cs` (thêm `RateLimited`) + `ErrorTypeToHttp.cs` (`RateLimited => 429`) + `CommonErrors.cs` (`RateLimited`). Guard: `ErrorTests` (blank code/message ném; None rỗng OK; factory→đúng type) + `ErrorHandlingTests.ToStatusCode_should_map_each_error_type` (+RateLimited→429).
- Context: review A-26 — `Error` là positional record KHÔNG validate → tạo được Error code/message rỗng vô nghĩa; và rate-limit 429 xử lý edge-only (AD-034) tạo bất nhất: `ErrorType` không có RateLimited nên use case/domain không thể biểu diễn 429 qua Result.
- Decision/Change: (1) `Error` enforce code/message non-empty ở ctor (None là ngoại lệ sentinel chủ đích); (2) thêm `ErrorType.RateLimited` + map 429 nhất quán + `CommonErrors.RateLimited` → 429 giờ có đường đi qua Error/ProblemDetails thống nhất (không chỉ edge middleware).
- Rationale (verifiable): **Root cause A-26:** thiếu invariant → Error rỗng lọt được (bug ẩn); ErrorType thiếu RateLimited → mô hình lỗi không đồng nhất (một status có 2 cách sinh khác nhau). Enforce ở ctor bắt sớm; promote RateLimited làm mọi 429 đi qua một shape ProblemDetails (review option (b): "add type mapping consistently").
- Alternatives: (a) RateLimited edge-only, bỏ khỏi ErrorType (loại: use case không biểu diễn được 429; giữ bất nhất); (b) giữ positional record không validate (loại: chính lỗ hổng — Error rỗng).
- Consequences: `ErrorType` 7 loại (thêm RateLimited) → snapshot CP12 (`ErrorCodeSnapshotTests`) gồm `rate_limited` (vp all xanh xác nhận đồng bộ). Edge rate-limit middleware (AD-034 `OnRejected`) VẪN dùng `CommonErrors.RateLimited()` trực tiếp — nay cùng type/shape với đường Result.
- Reversibility: Medium.
- Traceability: review A-26; REFINES AD-034 (rate-limit 429 — nay RateLimited là first-class ErrorType, không còn "chỉ edge"); §4.4; CP12.

---

### AD-079 — ExceptionHandlingMiddleware phân biệt client-abort vs lỗi server; response-đã-start thì rethrow (A-27)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-27)
- Provenance/Evidence: đọc code thật `Bedrock.Api/ErrorHandling/ExceptionHandlingMiddleware.cs`: `catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)` → nuốt êm (không log, không ghi); mọi catch có `if (context.Response.HasStarted) throw;` (giữ nguyên exception gốc). Guard: `ExceptionHandlingMiddlewareTests.{Client_aborted_cancellation_is_swallowed_not_500, Cancellation_not_caused_by_abort_becomes_500, Unhandled_exception_becomes_500_problem_details}`.
- Context: review A-27 — catch-all cũ bắt CẢ request-aborted `OperationCanceledException` → log Error + cố ghi 500 lên connection ĐÃ ĐÓNG (noise + status sai + có thể ném khi ghi). Và khi response đã started, tạo exception mới làm mất context gốc.
- Decision/Change: (1) branch riêng cho client-abort (OCE + RequestAborted.IsCancellationRequested) → không log/không ghi; (2) OCE KHÔNG do abort (vd timeout nội bộ) rơi xuống generic → 500 đúng nghĩa; (3) `Response.HasStarted` → rethrow exception gốc (preserve context) thay vì ghi đè.
- Rationale (verifiable): **Root cause A-27:** client tự ngắt KHÔNG phải lỗi server — log Error là nhiễu (false alarm on-call), ghi 500 lên connection đã đóng là vô nghĩa/có thể ném. Dùng `when` filter theo `RequestAborted` phân biệt chính xác abort thật với cancellation nội bộ (timeout → vẫn là 500). HasStarted→rethrow giữ stack/type gốc cho tầng trên.
- Alternatives: (a) catch mọi OCE thành 500 (loại: noise + status sai — chính vấn đề); (b) nuốt MỌI OCE (loại: che cả timeout nội bộ đáng-500).
- Consequences: request client-abort không xuất hiện log Error (đúng) → nếu cần đo abort rate, dùng metric riêng (không phải error log). 
- Reversibility: Medium.
- Traceability: review A-27; §3.5 slot #3; bổ trợ CP13 (masked log).

---

### AD-080 — Soft-delete = NAMED query filter (compose, không ghi đè) + audit-created metadata bất biến trước caller (A-25)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-25). REFINES quyết định soft-delete filter + audit set gốc (design §7.5 / AD-004-liên-quan persistence conventions), không lật.
- Provenance/Evidence: verify EF Core 10.0.9 có overload `EntityTypeBuilder.HasQueryFilter(string filterKey, LambdaExpression)` (đọc `~/.nuget/packages/microsoft.entityframeworkcore/10.0.9/.../Microsoft.EntityFrameworkCore.xml`). Đọc code thật `Bedrock.Infrastructure/Persistence/PlatformDbContext.cs`: (1) `public const string SoftDeleteFilterName = "SoftDelete"`, `OnModelCreating` gọi `HasQueryFilter(SoftDeleteFilterName, BuildIsNotDeletedFilter(clrType))`; (2) `ApplyAudit` case Modified set `entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false` + `CreatedByUserId` tương tự. Guard: `ConventionTests.Audit_created_metadata_is_protected_from_caller_tampering` + `SoftDeleteFilterCompositionTests.Module_named_filter_composes_with_soft_delete_filter_instead_of_overriding` (2/2 pass local SQLite).
- Context: review A-25. (1) `HasQueryFilter(expr)` UNNAMED cũ: EF Core 10 chỉ giữ MỘT unnamed filter mỗi entity → module thêm filter tenant riêng sẽ GHI ĐÈ filter soft-delete → bản ghi đã xóa mềm lộ trở lại (lỗ bảo mật/đúng đắn). (2) `ApplyAudit` Modified cũ chỉ set Updated* → caller cố ý gán `CreatedAt`/`CreatedByUserId` rồi Save thì EF vẫn persist (metadata tạo bị giả mạo).
- Decision/Change: (1) đặt TÊN cho soft-delete filter (`SoftDeleteFilterName`) → EF Core 10 COMPOSE (AND) các named filter khác tên → module bổ sung filter (tên khác) mà KHÔNG mất soft-delete; (2) trong case Modified, đánh dấu `CreatedAt`/`CreatedByUserId` là `IsModified = false` → EF loại khỏi câu UPDATE → metadata tạo do infrastructure sở hữu, caller không ghi đè được.
- Rationale (verifiable): **Root cause A-25:** unnamed filter là API "thay thế" theo thiết kế của EF → phụ thuộc thứ tự đăng ký + ẩn; named filter là hợp đồng compose tường minh (fix tận gốc thay vì workaround gộp điều kiện tenant vào lambda soft-delete — vốn phá tách biệt lõi⊥module F3/F4). Bảo vệ Created* bằng `IsModified=false` là cơ chế EF chuẩn (per-property), không cần shadow/backing-field trick.
- Alternatives: (a) gộp điều kiện tenant vào chính lambda soft-delete (loại: lõi phải biết khái niệm tenant của module → phá F3/F4; không mở rộng cho filter khác); (b) chặn tamper bằng cách reload+copy Created* trước Save (loại: thêm round-trip DB + không nguyên tử); (c) để Created* trong shadow property (loại: đổi shape entity + API nghiệp vụ, breaking).
- Consequences: Module thêm query filter PHẢI dùng overload có tên (khác `"SoftDelete"`) để compose; nếu lỡ dùng unnamed sẽ ghi đè — đã có `SoftDeleteFilterCompositionTests` làm mẫu + cảnh báo trong docstring `SoftDeleteFilterName`. Caller không còn cách set lại Created* qua đường Save thường (đúng ý đồ); nếu cần chỉnh sửa hành chính (rất hiếm) phải thao tác chủ đích ngoài audit convention.
- Reversibility: Medium (đổi lại unnamed + bỏ IsModified=false là 2 dòng, nhưng sẽ tái mở lỗ hổng).
- Traceability: review A-25; design §7.5 (soft-delete + audit conventions); bổ trợ tách lõi⊥module (F3/F4).

---

### AD-081 — Tách envelope transport BẤT BIẾN `OutgoingIntegrationMessage` khỏi record persistence `OutboxMessage` (A-20)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-20).
- Provenance/Evidence: đọc code thật `Bedrock.Application/Messaging/Dispatch/{IEventBusPublisher.cs, OutboxMessage.cs}`, `Adapters/Messaging.RabbitMq/{RabbitMqEventBusPublisher.cs, RabbitMqMessageMapper.cs}`, `Bedrock.Infrastructure/Persistence/Messaging/EfOutboxDispatcher.cs`. Xác nhận adapter chỉ dùng `Id/EventType/SchemaVersion/Payload/CorrelationId` (mapper) — KHÔNG dùng cột retry. Tạo mới `OutgoingIntegrationMessage.cs` (sealed record, init-only, đối xứng `IncomingIntegrationMessage`). Guard: `DecisionGuardTests.AD081_EventBusPublisher_port_takes_immutable_outgoing_envelope_not_outbox_record` (param type = OutgoingIntegrationMessage; không có 6 cột retry; mọi property init-only). Build 0-warning; guard + `RabbitMqMessageMapperTests` (4) pass.
- Context: review A-20 — `IEventBusPublisher.PublishAsync(OutboxMessage)` khiến adapter bus (pluggable, F29) nhận nguyên record persistence mutable với các cột retry/lease (`ProcessedAt/ErrorCount/NextAttemptAt/DeadLetteredAt/ClaimId/ClaimedUntil`). Adapter không cần và KHÔNG NÊN biết trạng thái outbox nội bộ; record mutable còn dễ bị adapter vô tình sửa.
- Decision/Change: (1) thêm `OutgoingIntegrationMessage` (Application, `Messaging.Dispatch`) — sealed record BẤT BIẾN chỉ id/type/version/payload/occurred/correlation; (2) đổi `IEventBusPublisher.PublishAsync` nhận envelope này; (3) `EfOutboxDispatcher` map record→envelope (`ToOutgoing`) ngay trước publish; (4) RabbitMq mapper/publisher + `ThrowingEventBusPublisher` + toàn bộ test đổi theo. Wire format (MessageId/headers/routing key/body) GIỮ NGUYÊN.
- Rationale (verifiable): **Root cause A-20:** port là ranh giới lõi↔adapter (F29 "cắm không sửa lõi") → hợp đồng port phải tối thiểu + bất biến, không lộ chi tiết lưu trữ. Envelope immutable loại nguy cơ adapter mutate trạng thái + thu hẹp bề mặt phụ thuộc (adapter chỉ thấy dữ liệu gửi đi). Đây là fix bản chất (đổi hợp đồng port) thay vì fix ngọn (chỉ ẩn field bằng comment).
- Alternatives: (a) giữ `OutboxMessage` nhưng để interface con chỉ lộ field cần (loại: vẫn truyền reference mutable, adapter cast được về record đầy đủ); (b) đánh dấu các cột retry `internal` trên OutboxMessage (loại: EF cần map chúng + writer/dispatcher cùng assembly Infrastructure ≠ Application nơi OutboxMessage đang ở → không khả thi sạch).
- Consequences: DEFER (chưa làm trong turn này, ghi rõ để không mất dấu — xem N-075): review còn đề xuất dời `OutboxMessage`→`OutboxRecord` INTERNAL trong Infrastructure + `InboxMessage` internal (dùng `InternalsVisibleTo` cho test). Sau khi port đã sạch, đây là tinh chỉnh namespace/visibility (KHÔNG còn là rò rỉ port) nhưng đụng ~6 file test đa-assembly → tách increment riêng để tránh drift.
- Reversibility: Medium (gộp lại envelope vào record là đảo được, nhưng tái mở rò rỉ).
- Traceability: review A-20; design §5.2 (adapter mỏng, payload thô + headers), §7.2 (dispatcher); F29 (adapter pluggable); đối xứng `IncomingIntegrationMessage` (A-10).

---

### AD-082 — Consume-side validate envelope: mang content-type/schema-version + dispatcher yêu cầu JSON trước handler (A-10)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-10).
- Provenance/Evidence: đọc code thật `RabbitMqConsumer.OnReceivedAsync` (CHỈ đọc MessageId + event-type header, VỨT content-type/schema-version property/header), `IncomingIntegrationMessage` (thiếu ContentType/SchemaVersion), `EfIntegrationEventDispatcher.DispatchAsync` (đã CÓ id-match `integrationEvent.Id != message.MessageId → throw`; deserialize-fail đã đi NACK→DLX). Đã sửa: `OutboxSerialization.{ContentType, IsJsonContentType}`, thêm `IncomingIntegrationMessage.{ContentType, SchemaVersion}` (init, đối xứng OutgoingIntegrationMessage), validate content-type ở đầu `DispatchAsync` (fail-fast → DeadLettered), consumer populate 2 field từ delivery. Guard mới: `IntegrationEventEnvelopeValidationTests` (4: non-json→DeadLettered, missing→DeadLettered, json+charset→Handled, id-mismatch→throw) — SQLite, 8/8 pass (cùng InboxMetricsTests). Build 0-warning.
- Context: review A-10 — R22/R24 envelope integrity mới đúng nửa PRODUCER, chưa xuyên bus: consumer vứt content-type + schema-version → dispatcher không thể validate; message content-type lạ (foreign producer/misconfig) chỉ bị bắt gián tiếp khi deserialize ném (muộn, sau khi đã mở transaction + mark inbox).
- Decision/Change: (1) `IncomingIntegrationMessage` mang `ContentType` + `SchemaVersion` (ngừng vứt metadata — đối xứng envelope OUTGOING AD-081); (2) dispatcher AGNOSTIC validate `IsJsonContentType(ContentType)` TRƯỚC khi resolve/deserialize/mở-transaction → lạ/thiếu → `DeadLettered` (quarantine sạch, consumer NACK requeue=false → DLX); (3) giữ + KHOÁ id-match integrity bằng guard test.
- Rationale (verifiable): **Root cause A-10:** metadata envelope bị vứt ở tầng transport → tầng agnostic mất khả năng validate. Fix bản chất = mang metadata lên envelope (hợp đồng) + validate ở dispatcher agnostic (mọi transport hưởng lợi — F30/§7.3), KHÔNG nhét validate rải rác trong adapter. Content-type là hợp đồng JSON CỐ ĐỊNH (AD-015/§5.2) nên đây là BẤT BIẾN kiểm được, không phải policy. Fail-fast trước transaction tiết kiệm 1 vòng inbox-mark + rõ diagnostics (DeadLettered thay vì exception mơ hồ).
- Alternatives: (a) validate content-type trong RabbitMqConsumer (loại: chỉ RabbitMQ hưởng, transport khác vẫn hở — vi phạm "dispatcher validate" của review + F30 agnostic); (b) dựa deserialize-exception hiện có (loại: bắt muộn sau khi mở transaction/mark inbox, diagnostics mơ hồ, tốn 1 vòng).
- Consequences: DEFER có chủ đích, KHÔNG bịa policy: (i) kiểm schema-version *tương thích theo từng EventType* cần registry keyed `(EventType, Version)` → thuộc **A-21** (SchemaVersion nay đã được MANG sẵn để A-21 dùng); (ii) khôi phục W3C trace context + tạo child Activity từ traceparent → thuộc **A-29**. Test dựng `IncomingIntegrationMessage` trực tiếp PHẢI set `ContentType="application/json"` (message thật luôn có) — đã cập nhật InboxMetricsTests + PostgresIntegrationEventDispatcherTests.
- Reversibility: Medium.
- Traceability: review A-10; design §5.2 (hợp đồng JSON), §7.3 (dispatcher agnostic), R22/R24, F30; đối xứng AD-081; liên quan A-21/A-29.

---

### AD-083 — Registry integration-event: enforce hợp đồng metadata field-independent; KEY theo EventType (không (EventType,SchemaVersion)) (A-21)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-21).
- Provenance/Evidence: đọc code thật `IntegrationEventTypeRegistry.ReadEventType` (dùng `RuntimeHelpers.GetUninitializedObject` + đọc virtual `EventType`; getter phụ thuộc field → NRE mơ hồ lúc boot), `IntegrationEventSchemaSnapshotTests` (cùng cơ chế), `IntegrationEvent` base (`EventType` abstract + `SchemaVersion` virtual=1). VERIFY versioning trong `requirements.md`: **R22.2** (thêm field ⇒ chỉ optional, backward-compat), **R22.3** (breaking ⇒ EventType MỚI v2 song song + consumer tolerant reader). Đã sửa: hardening `ReadEventType` (try/catch → lỗi RÕ nêu type + hợp đồng), thêm guard `Bedrock.ContractTests/IntegrationEventMetadataContractTests` (2 test: metadata field-independent + non-empty + SchemaVersion≥1; EventType unique) — 4/4 pass (cùng snapshot cũ). Build 0-warning.
- Context: review A-21 nêu 2 điều: (1) đọc metadata qua uninitialized-instance là brittle, compiler không enforce → event getter phụ-thuộc-field làm boot lỗi khó hiểu; (2) "registry key nên là (EventType, SchemaVersion) hoặc có policy version compatibility".
- Decision/Change:
  - (1) **Enforce hợp đồng field-independence bằng build-gate** thay vì đổi cơ chế: hardening `ReadEventType` báo lỗi rõ (nêu type + yêu cầu biểu thức hằng/độc-lập-field) + guard test quét MỌI IntegrationEvent trong Contracts assembly (EventType đọc-được/khác-rỗng + SchemaVersion≥1 + unique). Lỗi bị bắt lúc BUILD, không phải boot.
  - (2) **GIỮ key = EventType (KHÔNG (EventType,SchemaVersion), KHÔNG version-rejection).**
- Rationale (verifiable): **Về (2) — lý do CHÍNH XÁC, có kiểm chứng, chỉnh lại giả định của review:** mô hình versioning của DESIGN (R22.2/R22.3, §9.1 tolerant reader) quy định: cùng một `EventType` chỉ tiến hoá THÊM FIELD OPTIONAL (backward/forward-compat qua tolerant reader — System.Text.Json bỏ field lạ, field thiếu = default); mọi thay đổi BREAKING phải đổi sang `EventType` MỚI (chuỗi khác). ⇒ Hai message cùng EventType nhưng khác SchemaVersion LUÔN tương thích đọc được ⇒ (a) key theo (EventType,SchemaVersion) là THỪA và sẽ MÂU THUẪN design (design không cho nhiều schema-khác-nhau chung một EventType); (b) version-rejection là KHÔNG cần (không có version nào của một EventType đã-biết mà không đọc được). Vậy key theo EventType là ĐÚNG THEO THIẾT KẾ. SchemaVersion là metadata cho snapshot/quan sát (F32), không phải khoá định tuyến. **Về (1):** đổi sang attribute/static-abstract là phương án robust hơn nhưng LÀ THAY ĐỔI hợp đồng tác giả event (design §4.5/AD-006 định EventType là instance getter) + đụng mọi event + cần DV/sửa design → chọn ENFORCE (đúng triết lý dự án "biến quy ước thành guard test = fail build") đạt an toàn tương đương mà không lệch design.
- Alternatives: (a) attribute `[IntegrationEvent("code", Version=n)]` làm nguồn metadata type-level (loại lúc này: thay đổi authoring model + DV + churn nhiều event; enforcement đạt an toàn tương đương); (b) static-abstract interface member (loại: registry vẫn quét reflection nên không hưởng compile-time; lại buộc giữ CẢ static + instance cho publish → drift); (c) key (EventType,SchemaVersion) + policy compat (loại: mâu thuẫn mô hình versioning R22.3 như trên — sẽ là speculation vì design không định nghĩa luật reject version).
- Consequences: Nếu tương lai đổi versioning strategy (cho nhiều schema chung EventType) thì phải mở AD mới + rất có thể key lại — nhưng đó là thay đổi design, không làm bây giờ. Guard test hiện quét 2 Contracts assembly (giống snapshot); khi A-11 (auto-discover toàn solution) làm xong sẽ cập nhật cả hai cùng lúc.
- Reversibility: High (guard test + hardening là bổ sung; không đổi API).
- Traceability: review A-21; R22.2/R22.3 (versioning), §9.1 (tolerant reader), F32; AD-006 (registry EventType→type); liên quan A-22 (snapshot mạnh hơn) + A-11 (auto-discover).

---

### AD-084 — Trace xuyên bus: dispatcher tạo consumer span là con của trace gốc (propagate qua CorrelationId W3C) (A-29)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-29).
- Provenance/Evidence: đọc code thật `EfOutboxWriter` (`CorrelationId = Activity.Current?.Id` — W3C traceparent lúc enqueue), `RabbitMqMessageMapper` (set AMQP `CorrelationId` property từ envelope), `RabbitMqConsumer` (BỎ QUA CorrelationId, KHÔNG tạo Activity), `EfIntegrationEventDispatcher` (chỉ metric, không span), `BedrockTelemetry.ActivitySource` (đã có, name "Bedrock", đã `AddSource` trong OTel wiring). Đã sửa: thêm `IncomingIntegrationMessage.CorrelationId`, consumer populate từ `ea.BasicProperties.CorrelationId`, dispatcher `StartActivity($"consume {EventType}", Consumer, parentContext)` với parent parse từ CorrelationId (`ActivityContext.TryParse`). Guard: `IntegrationEventTracePropagationTests` (2: child-of-propagated-context qua ActivityListener + graceful-no-correlation) — 2/2 pass non-Docker. Build 0-warning.
- Context: review A-29 — "Outbox lưu Activity.Current.Id nhưng consumer bỏ qua BasicProperties.CorrelationId và không start child Activity; không có producer/consumer/handler spans". Trace GỐC (request enqueue) không nối được với xử lý phía consumer → mất khả năng đối soát xuyên bus (F34/F21/R24).
- Decision/Change: (1) `IncomingIntegrationMessage` mang `CorrelationId` (traceparent W3C — hoàn tất metadata envelope, tiếp nối A-10); (2) dispatcher AGNOSTIC tạo consumer span (`ActivityKind.Consumer`) parent = context parse từ CorrelationId (parse fail/null → ambient `Activity.Current`, graceful); (3) span mang tag messaging.* + outcome. Handler chạy TRONG span này → mọi span/log con nối về trace gốc.
- Rationale (verifiable): **Root cause A-29 (xuyên bus):** với Outbox, publish do WORKER chạy ở trace khác/không-trace; linkage Ý NGHĨA là trace GỐC lúc enqueue → xử lý consume. `CorrelationId = Activity.Current.Id` (W3C) CHÍNH LÀ traceparent của trace gốc → `ActivityContext.TryParse` khôi phục được → tạo consumer span là con của nó. Đặt span ở dispatcher AGNOSTIC (không ở adapter) → MỌI transport hưởng lợi (F30/§7.3, nhất quán AD-082). `StartActivity` trả null khi không có listener → zero-overhead khi tắt telemetry (mọi `?.` an toàn). Dùng `CorrelationId` (traceparent-format có sẵn) thay vì thêm header `traceparent` riêng: khớp thiết kế hiện hành (producer đã lưu Activity.Id vào CorrelationId), không đổi wire format.
- Alternatives: (a) thêm header `traceparent`/`tracestate` riêng theo OTel messaging convention thay vì dùng CorrelationId (loại lúc này: đổi wire format + producer phải ghi header riêng; CorrelationId ĐÃ mang đúng traceparent-format → dùng lại là tối thiểu + đủ; tracestate propagation là enhancement sau); (b) tạo span ở RabbitMqConsumer (loại: chỉ RabbitMQ hưởng, transport khác vẫn hở — vi phạm F30 agnostic).
- Consequences: DEFER có chủ đích (KHÔNG bịa, ghi rõ): các phần KHÁC của A-29 tách riêng — (i) oldest-pending backlog GAUGE (observable gauge có cache/throttle) trùng phạm vi **A-28** (operability); (ii) metric external-auth success/fail thuộc adapter external-auth (chưa có consumer adapter); (iii) verify EF query-time metric emission (AddMeter EF) cần integration. `tracestate` chưa propagate (chỉ traceparent). Producer/publish span (worker) chưa thêm — trace gốc đã là span API request (AspNetCore instrumentation), consumer span nối về đó là đủ chứng minh xuyên bus.
- Reversibility: High (span là bổ sung; không đổi API công khai ngoài field envelope).
- Traceability: review A-29; F34/F21 (correlation/trace), R24 (telemetry), §7.3 (dispatcher agnostic); tiếp nối A-10 (envelope) + AD-082; liên quan A-28 (gauge).

---

### AD-085 — AddBedrockCore idempotency-guard: gọi lần 2 → NÉM (chống double-decoration pipeline) (A-12)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-12).
- Provenance/Evidence: đọc code thật `BedrockCoreExtensions.AddBedrockCore` (gọi `RegisterValidators` + `DecoratePipeline` với Scrutor `TryDecorate` — KHÔNG guard số lần gọi) + verify MỌI call site (`grep AddBedrockCore`): chỉ Host `StarHill.Api/Program.cs` (platform + starhill) gọi ĐÚNG 1 lần + 2 test PipelineOrderTests; KHÔNG module nào gọi → guard throw-lần-2 an toàn (không vỡ call site hợp lệ). Đã thêm sentinel `BedrockCoreMarker` (đăng ký instance, ctor private) + throw khi đã tồn tại. Guard: `BedrockCoreIdempotencyTests` (gọi-1-lần OK; gọi-2-lần throw) — 7/7 pass (cùng PipelineOrderTests). Build 0-warning.
- Context: review A-12 nêu nhiều điểm về DI-registration; một trong đó: "Gọi AddBedrockCore hai lần có nguy cơ duplicate validator và double-decoration". Scrutor `TryDecorate` bọc MỌI service hiện có → gọi 2 lần = bọc HAI lớp behaviors ⇒ validation/authorization/idempotency/transaction chạy 2 lần (transaction lồng, idempotency claim đúp) = sai nghiêm trọng + validator đăng ký trùng.
- Decision/Change: `AddBedrockCore` idempotency-guarded — sentinel `BedrockCoreMarker` (instance, ctor private → ValidateOnBuild an toàn); gọi lần 2 → `InvalidOperationException` fail-loud (KHÔNG no-op âm thầm) nêu rõ chỉ gọi 1 lần ở composition root sau khi module đã đăng ký use case.
- Rationale (verifiable): **Root cause:** double-decoration là bug composition thật + âm thầm (không lỗi biên dịch). Fail-loud lộ ngay lúc compose (đồng nhất triết lý F35/R13 + AD-071 idempotent-register). Chọn THROW (không no-op) vì gọi 2 lần LUÔN là lỗi (không có ngữ cảnh hợp lệ) — no-op sẽ giấu lỗi cấu hình.
- Alternatives: (a) no-op lần 2 (loại: giấu lỗi composition — im lặng bỏ lần gọi thứ 2 có thể khiến dev tưởng đã thêm gì đó); (b) làm DecoratePipeline tự-idempotent bằng cách kiểm đã-decorate (loại: fragile, Scrutor không expose trạng thái sạch; sentinel rõ ràng hơn).
- Consequences: A-12 còn 3 phần CHƯA làm (ghi rõ, KHÔNG bịa — A-12 giữ PARTIAL): (i) `ValidateSingleImplementationPorts` bỏ qua OPEN-GENERIC — nhưng scenario chính "duplicate IRepository<> qua 2 context unkeyed" ĐÃ bị `PersistenceRegistrationRegistry.AddUnkeyed` chặn (A-01/`Two_unkeyed_contexts_fail_fast`); mở rộng guard open-generic blanket có rủi ro FALSE-POSITIVE (decorator/validator open-generic hợp lệ) → cần thiết kế whitelist cẩn thận, hoãn; (ii) use case đăng ký SAU AddBedrockCore không được bọc pipeline (ordering hazard) → cần finalization pattern (`FinalizeBedrock`) là thay đổi API lớn hơn; (iii) `AddBedrockCore(params Assembly[])` chỉ scan validator (không scan use case) — đúng thiết kế hiện tại (use case đăng ký riêng), chỉ là tên/comment dễ gây kỳ vọng.
- Reversibility: High (guard là bổ sung; không đổi chữ ký công khai).
- Traceability: review A-12; §8 (pipeline behaviors), AD-037 (Scrutor decorate), F35/R13 (fail-fast); liên quan A-01 (PersistenceRegistrationRegistry đã chặn duplicate context).

---

### AD-086 — Hợp đồng giao Outbox = at-least-once, KHÔNG cam kết ordering; poison độc-lập không chặn message khác (A-09)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-09).
- Provenance/Evidence: VERIFY design §7.2 (dòng ~398) ĐÃ ghi "**Không cam kết global ordering** khi có retry/claim song song — consumer phải tolerant thứ tự xấp xỉ"; requirements R8.4 mô tả claim "theo thứ tự occurred_at" (dễ HIỂU NHẦM là guarantee giao); R8.5 (backoff per-message, "không chặn message khác"); đọc `EfOutboxDispatcher` (`ORDER BY occurred_at` = heuristic claim; `TryPublishAndFinalizeAsync` per-message độc lập, guard-by-lease). Đã sửa: docstring `IOutboxDispatcher` nêu HỢP ĐỒNG unordered tường minh (contract surface); thêm guard `OutboxDispatcherTests.Poison_message_does_not_block_other_messages_in_batch`. Build 0-warning; 11/11 OutboxDispatcherTests pass.
- Context: review A-09 — "Query order theo OccurredAt nhưng nhiều dispatcher SKIP LOCKED có thể publish event sau của cùng aggregate trước event trước... Nếu không bảo đảm ordering, phải ghi rõ contract là unordered; không nên chỉ nói theo occurred_at." Design đã nói ở §7.2 nhưng hợp đồng CHƯA hiện diện ở PORT surface (`IOutboxDispatcher`) và chưa có guard cho tính chất hành vi cốt lõi.
- Decision/Change: (1) Formalize hợp đồng: giao Outbox là **at-least-once, KHÔNG ordering** (không global, không per-aggregate FIFO); `ORDER BY occurred_at` chỉ best-effort claim; (2) mỗi message xử lý ĐỘC LẬP — poison (backoff/dead-letter) KHÔNG chặn message khác trong batch; (3) ordered delivery (nếu module cần) = mở rộng PartitionKey+sequence, KHÔNG phải mặc định base. Nêu hợp đồng ở docstring `IOutboxDispatcher` + guard tính độc-lập.
- Rationale (verifiable): **Root cause A-09:** rủi ro là DEV HIỂU NHẦM "theo occurred_at" = FIFO guarantee → thiết kế nghiệp vụ dựa FIFO tuyệt đối (sai). Fix bản chất = phát biểu hợp đồng CHÍNH XÁC tại contract surface (port) + KHOÁ tính chất hành vi có thật (poison-isolation/độc-lập per-message) bằng test — KHÔNG bịa tính năng ordering (chưa module nào cần → xây PartitionKey+sequence lúc này là speculation, vi phạm "không suy đoán"). Poison-isolation là tính chất ĐÚNG của at-least-once unordered (mỗi message độc lập) và kiểm chứng được (SelectiveFailurePublisher: poison đứng trước vẫn không chặn message tốt).
- Alternatives: (a) thêm PartitionKey+sequence + single-active-consumer để bảo đảm ordering (loại lúc này: speculation — chưa có yêu cầu ordering từ module; là mở rộng lớn khi thật sự cần); (b) chỉ sửa doc không guard (loại: bỏ lỡ cơ hội khoá tính chất độc-lập-per-message chống hồi quy).
- Consequences: Module KHÔNG được thiết kế dựa FIFO tuyệt đối của bus (đã nêu ở port docstring + design §7.2). Nếu tương lai một module cần ordering nghiêm ngặt → mở AD mới + thêm PartitionKey/sequence (không phá hợp đồng hiện tại vì "unordered" là superset).
- Reversibility: High (doc + guard bổ sung; không đổi behavior/chữ ký).
- Traceability: review A-09; design §7.2 ("không cam kết global ordering"), requirements R8.4/R8.5, F25/F33; bổ trợ AD-016 (backoff)/AD-072 (lease).

---

### AD-087 — Outbox operability: cột `last_error`/`last_attempt_at` (sanitize+bound) cho chẩn đoán fail (A-28)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-28).
- Provenance/Evidence: đọc `OutboxMessage` (chỉ ErrorCount/NextAttemptAt/DeadLetteredAt/ClaimId/ClaimedUntil — KHÔNG lưu vì-sao-fail), `EfOutboxDispatcher.TryPublishAndFinalizeAsync` (catch KHÔNG capture exception), `OutboxInboxModelBuilderExtensions`, migration mẫu `AddOutboxClaimLease` + `IdentityDbContextFactory` (design-time). VERIFY tooling: `dotnet tool restore` + `dotnet ef --version`=10.0.9 OK trước khi sinh migration. Đã thêm: `OutboxMessage.{LastError, LastAttemptAt, MaxLastErrorLength=1024}`; map `LastError` HasMaxLength(1024); dispatcher `catch (Exception ex)` set `last_error=SanitizeError(ex)`+`last_attempt_at=failedAt` ở CẢ nhánh backoff + dead-letter; migration `20260712104027_AddOutboxLastError` (2 cột, schema identity, Down chuẩn) + snapshot cập nhật (verify chứa last_error/last_attempt_at). Guard: `OutboxDispatcherTests.{Dispatch_failure_records_last_error_and_last_attempt, Dead_letter_records_last_error}` — 13/13 pass. Build 0-warning.
- Context: review A-28 — "Outbox chỉ lưu error count/next attempt, không lưu last_error/last_attempt_at... Khi production lỗi, operator khó trả lời vì sao, event nào". Chẩn đoán fail là thông tin ops thiết yếu cho sản phẩm thương mại.
- Decision/Change: thêm `last_error` (varchar 1024, nullable) + `last_attempt_at` (timestamptz, nullable) vào `outbox_message`; dispatcher ghi `KiểuException: message` (cắt bound) + thời điểm ở mọi lần publish thất bại (backoff và dead-letter). Migration per-module chuẩn (AD-050) sinh bằng `dotnet ef` (đã verify tooling), snapshot đồng bộ (không model-drift).
- Rationale (verifiable): **Root cause A-28 (phần này):** thiếu dữ liệu chẩn đoán → dead-letter chỉ là "đã bỏ" mà không biết VÌ SAO. Lưu `last_error` (sanitize = kiểu+message, bound 1024 chống text vô hạn + giới hạn rò rỉ vào cột) + `last_attempt_at` cho operator trả lời trực tiếp. Sinh migration bằng `dotnet ef` (verify tooling TRƯỚC) thay vì viết tay = đúng chuẩn + snapshot tự đồng bộ (không drift). Exception ở đây từ `IEventBusPublisher.PublishAsync` (network/serialize adapter) — hiếm khi chứa payload nên type+message an toàn.
- Alternatives: (a) bảng audit riêng cho lỗi (loại lúc này: over-engineer; cột trên outbox đủ + nguyên tử với finalize); (b) lưu full stack-trace (loại: dài vô hạn + dễ rò; type+message bound đủ chẩn đoán); (c) viết migration tay (loại: rủi ro lệch snapshot — dùng `dotnet ef` chuẩn hơn).
- Consequences: DEFER các phần khác của A-28 (ghi rõ): (i) **oldest-pending backlog GAUGE** — cần cache refresh bởi worker + ObservableGauge (callback sync không được query DB mỗi scrape); thiết kế throttle riêng → increment sau; (ii) **replay/admin/requeue contract** — cần quyết định bảo mật/quyền + audit (sản phẩm), chưa làm; (iii) **inbox status/error/duration** — schema change inbox, chưa cần. ĐỒNG BỘ starhill (Task C): starhill có bản copy outbox → khi port phải thêm cùng migration + verify.
- Reversibility: Medium (migration có Down; cột nullable không phá dữ liệu cũ).
- Traceability: review A-28; AD-050 (migration per-module), AD-003/AD-016/AD-072 (dead-letter/backoff/lease), R8.5; F25/F33.

---

### AD-088 — Auth-model quyết định: base = JWT bearer token-in-body/header (không auth-cookie); cookie/CSRF/CORS = coherent + opt-in (A-15)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-15 yêu cầu "chọn 1 trong 2 phương án").
- Provenance/Evidence: đọc code thật: `Identity.Api/IdentityEndpointModule` (`/identity/token/refresh` nhận `RefreshRequest` BODY → `RefreshTokenCommand(request.RefreshToken)`, trả `RefreshTokenResult{AccessToken,RefreshToken,...}` trong RESPONSE BODY, `.AllowAnonymous()` — KHÔNG Set-Cookie); `HttpSecurityOptions.Validate` (CrossSite bắt buộc CorsAllowedOrigins, cấm `*`, origin phải absolute http/https + path=="/"); `BedrockHttpSecurityExtensions` (CookiePolicy HttpOnly.Always+Secure.Always+SameSite theo mode; `AddAntiforgery` header X-CSRF-TOKEN + cookie HttpOnly/Secure/SameSite theo mode; CORS cross-site chỉ origin khai + credentials, same-site đóng); `BedrockApiExtensions.UseBedrockApi` (UseCookiePolicy #1.5, UseCors #8, UseAntiforgery sau authz); `CookieSecurityDefaults`. Guard hiện có: `HttpSecurityOptionsTests` (Validate đầy đủ) + `HttpSecurityConfigTests` (ForwardedHeaders + cookie flags + **antiforgery-by-mode MỚI**) — 16/16 pass. Build 0-warning.
- Context: review A-15 cho rằng cookie/CSRF là "half-measure" (snapshot TRƯỚC đợt hardening). Sau đợt hardening (AI trước) + verify của tôi: hạ tầng cookie/CSRF/CORS ĐÃ đầy đủ + coherent; và auth THẬT của base là bearer-in-body.
- Decision/Change: chốt auth-model là **HYBRID**: (1) endpoint base tự thân dùng **JWT bearer** (access token ở header Authorization, refresh token ở JSON body) — KHÔNG dùng auth-cookie → **CSRF-as-cookie KHÔNG áp dụng** cho endpoint base (bearer không được trình duyệt tự gửi cross-site); (2) base VẪN cung cấp hạ tầng cookie/CSRF/CORS **coherent + OPT-IN** cho app/module chọn luồng cookie hoặc FE cross-site: `CookieSecurityDefaults` (HttpOnly+Secure+SameSite), antiforgery (X-CSRF-TOKEN), CORS siết (CrossSite chỉ origin khai + credentials; SameSite đóng), `Validate` chặn cấu hình nguy hiểm (`*` + credentials, origin sai). Đây là phương án 1 (cho base) + hạ tầng phương án 2 (opt-in) — KHÔNG half-measure.
- Rationale (verifiable): **Root cause A-15:** rủi ro "false security" khi có option cookie nửa vời. Sự thật sau verify: (a) base không set auth-cookie nên không có CSRF-cookie vector cho chính nó (đúng phương án 1); (b) hạ tầng cookie/CSRF/CORS đã đầy đủ + `Validate` nghiêm (cấm wildcard+credentials — lỗ hổng CORS kinh điển) nên KHÔNG phải nửa vời. Ghi rõ quyết định = loại bỏ "cảm giác an toàn giả" bằng cách nói ĐÚNG mô hình.
- Alternatives: (a) bỏ hẳn cookie/CSRF khỏi base (loại: mất khả năng hỗ trợ app cookie/cross-site — hạ tầng opt-in đã coherent, giữ lại có giá trị); (b) chuyển refresh token sang HttpOnly cookie mặc định (loại: đổi hợp đồng API + buộc mọi client dùng cookie — quyết định sản phẩm, không nên ép ở base).
- Consequences: Module/app dùng luồng cookie PHẢI tự set cookie qua `CookieSecurityDefaults` + bật CSRF token khi CrossSite (RequiresCsrf). **CORS behavioral e2e** (preflight allow origin khai / reject origin lạ) CHƯA có guard end-to-end — hiện chỉ guard `Validate` (chặn injection cấu hình) + registration; ghi rõ để KHÔNG tự nhận đã phủ e2e (có thể thêm TestHost sau). HSTS/HTTPS-redirect vẫn là trách nhiệm Host (AD-035).
- Reversibility: High (quyết định + guard bổ sung; không đổi behavior).
- Traceability: review A-15; R15 (cookie/CSRF), F17 (cookie/CORS mode), F16 (forwarded headers); bổ trợ AD-023 (claim JWT-native)/AD-035 (HSTS Host)/AD-067 (security headers).

---

### AD-089 — Contract snapshot descriptor CANONICAL (generic args + array + nullability value/reference) (A-22)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — verify + guard-hoá công việc hardening (review A-22).
- Provenance/Evidence: đọc `IntegrationEventSchemaSnapshotTests.FriendlyTypeName` cũ (`Type.Name` thuần + chỉ Nullable<value>) → `List<string>` và `List<int>` đều ra "List`1", không thấy nullability reference. Đã tạo `ContractSchema.{FriendlyTypeName, DescribeProperty}` (generic args đầy đủ + array đệ quy + Nullable<value> + reference-nullable qua `NullabilityInfoContext`); snapshot test dùng helper. Guard: `ContractSchemaPrecisionTests` (4: generic-arg distinguish List<String>≠List<Int32> + Dictionary<String,Int32>; array String[]; value Guid≠Guid?; reference String≠String?). Snapshot hiện tại KHÔNG đổi (props toàn Guid/DateTimeOffset non-null → render y hệt) → ExpectedSchema giữ nguyên. 8/8 ContractTests pass, build 0-warning.
- Context: review A-22 — "Event snapshot dùng Type.Name; List<string> và List<int> đều hiện List`1. Không ghi generic arguments đầy đủ, nullable annotations..." → snapshot BỎ SÓT breaking-change tinh vi (đổi element generic / thêm nullable) — điểm yếu của chính cơ chế chống-drift.
- Decision/Change: làm descriptor snapshot CHÍNH XÁC hơn — render generic args đầy đủ, array element, và nullability CẢ value (Nullable<T>) LẪN reference (NullabilityInfoContext). Không đổi snapshot hiện có (props hiện tại không generic/nullable).
- Rationale (verifiable): **Root cause A-22 (phần descriptor):** snapshot chỉ mạnh bằng độ chính xác của descriptor; `Type.Name` gộp `List<string>`≡`List<int>` ⇒ đổi kiểu element (breaking) KHÔNG bị bắt. Làm descriptor phân biệt = tăng SỨC của guard chống-drift đúng như user yêu cầu ("cách cực mạnh tránh drift"). Đây là hardening MECHANISM (không phải feature speculation) — verify bằng test với type tổng hợp.
- Alternatives: (a) full canonical JSON Schema/OpenAPI cho mỗi event (loại lúc này: lớn + cần golden-file infra; descriptor chuỗi đã bắt được các khác biệt compatibility chính); (b) giữ Type.Name (loại: bỏ sót generic/nullable — chính vấn đề).
- Consequences: DEFER (ghi rõ): (i) discover TOÀN contract assemblies tự động (thay hardcode 2 assembly) trùng phạm vi **A-11** (auto-discover); (ii) JSON property-name/converter/enum-values/default-values + golden-file — nâng cấp sâu hơn khi cần versioning contract nghiêm ngặt; (iii) error snapshot cũng nên discover-all (A-11). Hiện `ContractSchemaPrecisionTests` bảo đảm descriptor đúng để khi contract có generic/nullable thì snapshot sẽ bắt.
- Reversibility: High (helper + test bổ sung; snapshot không đổi).
- Traceability: review A-22; F32 (versioning), R32.4 (contract test); bổ trợ AD-083 (metadata contract)/CP12 (error code snapshot); liên quan A-11 (auto-discover).

---

### AD-090 — Gỡ build-artifact (bin/obj) khỏi Git index + guard chống tái-track (A-30)
- Status: Confirmed
- Date: 2026-07-12
- Decider: user (duyệt tường minh "OK — repo-wide") trên đề xuất + plan của AI(Kiro).
- Provenance/Evidence: `git ls-files "**/bin/**" "**/obj/**"` = 4238 file tracked (platform 2125, foundation 1006, resort-qr 1067, Reference 40; starhill 0). `platform/.gitignore`+`starhill/.gitignore` ĐÃ có `bin/`+`obj/` (nguyên nhân: track TRƯỚC khi ignore). Thực thi `git rm --cached --pathspec-from-file` (list ghi UTF-8 no-BOM qua .NET vì `>` PowerShell ra UTF-16+BOM làm git lỗi pathspec) → gỡ 4238 khỏi index. VERIFY: file đĩa còn (Test-Path=True), tracked bin/obj còn lại = 0, staged-deletion = 4238, CHƯA commit (user tự commit). Guard: `validate_ci.py::validate_no_tracked_artifacts` (chạy trong `vp ci`) fail nếu `git ls-files` còn khớp bin/obj. `vp all`/`vp ci`/`vp journal` xanh sau khi gỡ.
- Context: review A-30 — 2.125 file/235MB (platform) build-artifact bị Git track → clone/diff/status chậm, binary noise che thay đổi thật, merge-conflict artifact sau mỗi build. Review dặn làm RIÊNG + review trước khi chạy (worktree đang nhiều thay đổi user).
- Decision/Change: (1) `git rm --cached` repo-wide (4238) — chỉ gỡ INDEX, KHÔNG xoá file đĩa, KHÔNG commit (user commit); (2) thêm guard build-gate ở `validate_ci.py` (vp ci): tracked bin/obj > 0 → FAIL (anti-drift permanent, chống `git add -f`).
- Rationale (verifiable): **Root cause A-30:** `.gitignore` đúng nhưng git vẫn theo dõi file đã-track từ trước → fix gốc = gỡ khỏi index (không phải sửa .gitignore — đã đúng). `--cached` giữ file đĩa nên build không ảnh hưởng (verify `vp all` xanh). Guard trong `vp ci` biến "đừng commit artifact" từ quy ước thành build-gate (đúng triết lý anti-drift). Repo-wide (thay chỉ platform) theo user chọn để sạch triệt để; starhill vốn đã sạch.
- Alternatives: (a) chỉ platform (2125) (loại: user chọn repo-wide cho sạch toàn bộ); (b) `git filter-branch`/BFG xoá khỏi LỊCH SỬ (loại: rewrite history nguy hiểm + đụng mọi clone — chỉ gỡ tracking hiện tại là đủ cho hygiene, lịch sử cũ không ảnh hưởng workflow); (c) chỉ sửa .gitignore (loại: KHÔNG gỡ file đã-track — không giải quyết gì).
- Consequences: User PHẢI commit việc gỡ (4238 staged deletion) để hoàn tất; sau commit, `git status`/clone sạch. Guard `vp ci` từ nay chặn tái-track. Lịch sử Git cũ vẫn chứa artifact (không rewrite — chấp nhận: chỉ ảnh hưởng size lịch sử, không ảnh hưởng workflow hiện tại). ĐỒNG BỘ starhill (Task C): starhill đã sạch, chỉ cần giữ .gitignore.
- Reversibility: High (chưa commit; nếu muốn hoàn tác: `git reset` để unstage — file đĩa nguyên).
- Traceability: review A-30 (repository hygiene); AD-062 (command-governance validate_ci là nhà cố định); bổ trợ AD-061 (CI invariants).

---

### AD-091 — [P0 SECURITY] Forwarded Headers an-toàn-mặc-định: chỉ xử lý X-Forwarded-* khi có proxy/network tin cậy (re-audit P0-01)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — xử lý finding P0-01 của re-audit `re-audit-architecture-implementation-2026-07-12.md`.
- Provenance/Evidence: re-audit P0-01 tuyên bố `ConfigureForwardedHeaders` clear trust-list + không cấu hình → trust mọi proxy → spoof IP. TÔI KHÔNG tin suy luận doc (search cho thấy .NET 8.0.17/9.0.6 "ignore từ proxy lạ" — dễ tưởng đã an toàn) mà VIẾT TEST BEHAVIORAL: `ForwardedHeadersTrustTests` (TestServer + `RemoteIpAddress`=203.0.113.9 untrusted + header `X-Forwarded-For: 1.2.3.4`). KẾT QUẢ TRƯỚC FIX: observed = "1.2.3.4" ⇒ **SPOOF ĐƯỢC** (audit ĐÚNG, giả thuyết doc của tôi SAI). Fix: gate `ForwardedHeaders = None` khi `KnownProxies.Count==0 && KnownIPNetworks.Count==0`, ngược lại `XForwardedFor|XForwardedProto`. SAU FIX: untrusted→giữ IP thật (không spoof), trusted-proxy→honor, `RateLimitIntegrationTests` vẫn xanh — 3/3 pass.
- Context: middleware ForwardedHeaders chạy #1, RemoteIpAddress sau đó là partition-key của rate-limiter #9. Trust-list rỗng + XFF bật ⇒ (trên build .NET 10 hiện tại) header từ client trực tiếp VẪN được xử lý ⇒ client giả IP để né/đổ rate-limit; X-Forwarded-Proto giả ảnh hưởng URL/cookie/redirect.
- Decision/Change: xử lý X-Forwarded-* CHỈ khi có ≥1 proxy/network khai tin cậy (`hasTrustedSource`); không có → `ForwardedHeaders.None` (bỏ qua header, giữ IP kết nối thật). An-toàn-mặc-định, ĐỘC LẬP phiên bản framework (không dựa vào hành vi hardening có thể khác giữa các bản .NET).
- Rationale (verifiable): **Root cause:** bật parsing forwarded-header mà KHÔNG có nguồn tin cậy = trust-all (chứng minh bằng test empirical, không phải suy luận). Fix bản chất = không parse khi chưa khai tin cậy (declare-to-trust) → an toàn bất kể framework. Prod sau reverse-proxy khai `KnownProxies`/`KnownNetworks` → parsing bật + chỉ tin nguồn đó. Nếu quên khai → header bị bỏ qua (rate-limit degrade về per-proxy — AN TOÀN, không spoof — operator sẽ nhận ra để khai).
- Alternatives: (a) thêm cờ `ForwardedHeadersEnabled` + fail-fast nếu bật mà thiếu proxy (loại lúc này: thêm option + đổi hành vi; gate theo trust-list đã an-toàn-mặc-định + đủ, ít bề mặt hơn); (b) tin hành vi .NET tự ignore (loại: TEST CHỨNG MINH build hiện tại KHÔNG ignore → không được dựa vào).
- Consequences: Prod behind-proxy PHẢI khai `HttpSecurity:KnownProxies`/`KnownNetworks` để rate-limit partition theo client thật (nếu không → per-proxy, an toàn nhưng kém mịn). Ghi rõ cho deployment. Đây là finding re-audit ngoài A-01..35 (liên quan F16/A-15-security).
- Reversibility: High (gate là 3 dòng; nhưng bỏ = tái mở P0).
- Traceability: re-audit P0-01; F16 (forwarded headers), N-014 (rate-limit 2 tầng); bổ trợ AD-088 (auth-model)/AD-034 (rate-limit edge).

---

### AD-092 — [Gate 0] Idempotency scope key = tenant VÀ user (hai chiều) + hash rawKey (re-audit P1-03, REFINES AD-074)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — xử lý Gate-0 finding P1-03 của re-audit.
- Provenance/Evidence: đọc `IdempotencyKeyScope.For` — cũ: `principal = TenantId ?? UserId ?? "anonymous"` + rawKey ghép thẳng. Bug: FALLBACK → khi có tenant thì UserId bị bỏ → hai user cùng tenant + cùng rawKey đụng key. Đã sửa: `t={TenantId|"-"}:u={UserId|"anon"}` (hai chiều độc lập) + `Convert.ToHexStringLower(SHA256.HashData(rawKey))`. Guard: `IdempotencyDecoratorTests.{Same_key_same_tenant_different_users_do_not_collide, Same_key_same_identity_still_conflicts}` (+7 test cũ) — 9/9 pass. Build 0-warning.
- Context: re-audit P1-03 — idempotency scope collision xuyên user cùng tenant + anonymous chung namespace + rawKey không hash/bound.
- Decision/Change: (1) scope key gồm CẢ tenant VÀ user (không fallback loại trừ) → key = `{TInput.FullName}:t={tenant}:u={user}:{sha256(rawKey)}`; (2) hash rawKey (SHA-256 hex thường) → bound độ dài + không lưu raw key (có thể nhạy cảm) vào store/log.
- Rationale (verifiable): **Root cause P1-03:** fallback `??` làm mất chiều user khi có tenant → hai user hợp lệ chặn nhau (correctness + nhẹ là DoS lẫn nhau). Fix bản chất = tenant/user là HAI CHIỀU trong key, không phải loại trừ. Hash rawKey vừa bound (chống phình store/log) vừa tránh rò dữ liệu nhạy cảm client nhét vào key. Guard chứng minh cross-user không đụng + cùng-identity vẫn idempotent (không phá tính năng).
- Alternatives: (a) chỉ thêm user vào fallback chain (loại: vẫn là một chiều, không sửa gốc); (b) không hash, chỉ bound cắt (loại: raw key vẫn lộ + cắt có thể va chạm prefix).
- Consequences: DEFER (ghi rõ — Wave 1, cần đổi CONTRACT port `IIdempotencyStore`): fencing token (`TryBegin` trả owner token, `Complete/Abort` compare-and-set — chống stale abort) + response replay (trả kết quả cũ thay vì conflict). Anonymous (tenant+user đều null → `t=-:u=anon`) VẪN chung namespace — hạn chế đã biết: endpoint cần idempotency cho ẩn danh phải cấp client/session key ổn định (policy endpoint, ngoài decorator). Đổi format key → khoá cũ trong store (nếu có) coi như mới (chấp nhận: TTL 24h tự dọn).
- Reversibility: High (đổi hàm build key; behavior idempotency giữ nguyên cho cùng-identity).
- Traceability: re-audit P1-03; REFINES AD-074 (Complete/Abort + namespace), AD-039 (idempotency v1); F23 (multi-tenant); Gate 0.

---

### AD-093 — [Gate 0] schema-version là invariant: thiếu/lỗi/overflow → 0 (invalid), dispatcher quarantine nếu <1 (re-audit P1-02, REFINES AD-082)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — xử lý Gate-0 finding P1-02 của re-audit.
- Provenance/Evidence: đọc `RabbitMqConsumer.DecodeSchemaVersion` cũ (default 1 khi thiếu; `long→(int)` cast trực tiếp → overflow wrap) + `EfIntegrationEventDispatcher` (không validate SchemaVersion). Đã sửa: consumer trả 0 (sentinel invalid) khi thiếu/không-phân-giải/overflow/âm; dispatcher validate `SchemaVersion < 1 → DeadLettered` (đặt cạnh content-type check). Sửa comment `IncomingIntegrationMessage` (nói "registry keyed theo version" — SAI theo AD-083 → viết lại). Guard: `IntegrationEventEnvelopeValidationTests.Invalid_schema_version_is_dead_lettered` (0 và -1). 6/6 pass, build 0-warning.
- Context: re-audit P1-02 — schema-version chưa là invariant runtime: header thiếu/rác giả dạng v1; `long` overflow wrap; dispatcher chỉ validate content-type + payload-id, không validate version.
- Decision/Change: (1) consumer decode KHÔNG default 1 âm thầm — thiếu/không-phân-giải/overflow → 0 (invalid); `long` chỉ cast khi trong range int, ngoài range → 0; (2) dispatcher AGNOSTIC quarantine khi `SchemaVersion < 1` (invariant, đối xứng content-type — mọi transport hưởng lợi).
- Rationale (verifiable): **Root cause P1-02:** default-1 âm thầm + overflow-wrap khiến producer hỏng/foreign qua được như v1 → deserialize "thành công" nhưng sai semantic. Fix bản chất = coi version bất thường là malformed envelope (quarantine), validate ở tầng agnostic. Về "compatibility policy": mô hình versioning (AD-083/R22.3: breaking → EventType MỚI + tolerant reader) đã bảo đảm mọi version của MỘT EventType tương thích → chỉ cần invariant `>=1` (bắt producer sai), KHÔNG cần range-check per-event (sẽ mâu thuẫn AD-083).
- Alternatives: (a) giữ default 1 (loại: chính là bug — giả dạng v1); (b) thêm supported-version-range per EventType vào registry (loại: mâu thuẫn AD-083 "breaking→EventType mới"; là speculation vì chưa có multi-version cùng EventType).
- Consequences: Producer PHẢI gắn schema-version header > 0 (Bedrock publisher luôn gắn từ `OutgoingIntegrationMessage.SchemaVersion` >=1). Message thiếu header (foreign/legacy) nay bị quarantine thay vì xử lý như v1 — đúng ý (fail-loud). Record default `IncomingIntegrationMessage.SchemaVersion=1` giữ cho construction lập trình (test); transport là nguồn authoritative set 0 khi header bất thường.
- Reversibility: High (validate + decode; behavior message hợp lệ không đổi).
- Traceability: re-audit P1-02; REFINES AD-082 (envelope validate consume), AD-083 (versioning model), R22.3; Gate 0.

---

### AD-094 — [Gate 0] Domain-event failure boundary bao TRỌN dispatch→convention→base-save (re-audit P1-01, mở rộng AD-075)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — xử lý Gate-0 finding P1-01 của re-audit.
- Provenance/Evidence: đọc `PlatformDbContext.SaveChangesAsync` — cũ: `DispatchDomainEventsAsync` tự restore CHỈ khi dispatch ném; `ApplySoftDelete`/`ApplyAudit`/`base.SaveChangesAsync` chạy SAU, ngoài vùng restore → ném ở đó làm mất event (đã clear). Đã sửa: một failure boundary ở `SaveChangesAsync` — sink `List<(Entity,Events)>` gom mọi dequeue; catch restore (thứ tự ngược) rồi rethrow; bỏ try/catch cục bộ trong dispatch loop. Guard: `DomainEventDispatchTests.Domain_events_restored_when_convention_or_save_fails_after_dispatch` (TestClock.Throw → ApplySoftDelete ném SAU dispatch → event restored) — FAIL trên code cũ, PASS sau fix. 6/6 pass, build 0-warning.
- Context: re-audit P1-01 — domain event chỉ restore nếu dispatcher ném; nếu dispatch thành công nhưng base.SaveChanges/audit/soft-delete/depth-limit ném sau đó → event đã clear + KHÔNG restore → retry commit state nhưng mất side-effect/reaction.
- Decision/Change: gom mọi event đã dequeue trong một attempt vào sink; bọc dispatch + convention + base-save trong try/catch chung; ném ở BẤT KỲ bước nào sau dequeue → restore toàn bộ (thứ tự ngược để giữ đúng thứ tự event) → rethrow. Depth-limit throw giờ cũng được restore (không silent-drop).
- Rationale (verifiable): **Root cause P1-01:** vùng restore quá hẹp (chỉ quanh DispatchAsync). Vì dispatch dequeue+clear event khỏi entity TRƯỚC các bước ghi, mọi lỗi sau đó làm mất event in-memory. Fix bản chất = failure boundary phủ TOÀN BỘ cửa sổ trước-commit (một nơi restore duy nhất, phủ mọi đường ném). Test dùng clock ném để mô phỏng bước-sau-dispatch fail một cách tất định (dispatch không dùng clock; ApplySoftDelete/ApplyAudit dùng).
- Alternatives: (a) chỉ thêm try/catch quanh base.SaveChanges (loại: bỏ sót ApplySoftDelete/ApplyAudit/depth-limit — vá ngọn); (b) dispatch SAU base.SaveChanges (loại: phá CP14 atomic — side-effect handler phải commit cùng transaction với state).
- Consequences: Nếu caller RETRY SaveChanges trên CÙNG context sau lỗi, event (gồm event do handler raise) được restore → dispatch lại toàn bộ (re-stage handler effect — đúng vì attempt lỗi đã rollback). Nuance: handler-raised event khi restore + retry có thể chạy lại handler tương ứng (chấp nhận: attempt lỗi không commit gì; thường retry tạo scope mới). Ghi rõ.
- Reversibility: High (thu hẹp boundary lại là đảo được, nhưng tái mở lỗ mất event).
- Traceability: re-audit P1-01; mở rộng AD-075 (restore-on-dispatch-throw), CP14 (atomic dispatch), R33; Gate 0.

---

### AD-095 — [Gate 0] Consumer retry tier phân tầng transient/permanent (re-audit P0-02)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — xử lý Gate-0 finding P0-02 của re-audit (mục cuối Gate 0).
- Provenance/Evidence: đọc `RabbitMqConsumer.OnReceivedAsync` cũ — MỌI lỗi dispatch (handler ném) đều NACK requeue=false → DLX NGAY (không phân biệt lỗi tạm thời với lỗi vĩnh viễn) → một hiccup DB/network làm message rơi thẳng quarantine, mất tự-hồi-phục. Đã sửa: tách `RabbitMqDeliveryPolicy` (hàm THUẦN) + retry topology + retry-publish channel confirms trong consumer. Guard: `RabbitMqDeliveryPolicyTests` (10 test, exhaustive ForOutcome/ForException/IsPermanent + ngưỡng attempt) + `RabbitMqConsumerOptionsTests` (MaxDeliveryAttempts<1, RetryDelay<=0, retry exchange rỗng, effective retry queue, defaults) — 32/32 pass, build 0-warning; e2e `RabbitMqConsumeEndToEndTests.Transient_handler_failure_is_retried_with_delay_then_succeeds` (Testcontainers, skip khi thiếu Docker) chứng minh retry→delay→success + inbox đúng-một-lần.
- Context: re-audit P0-02 — thiếu retry tier: lỗi transient (DB/network/timeout/handler tạm) bị đối xử như poison vĩnh viễn → mất khả năng tự hồi phục của consumer; không có backoff/giới hạn số lần.
- Decision/Change: phân loại lỗi tại `RabbitMqDeliveryPolicy`:
  - **Permanent** = dispatch trả `InboxDispatchOutcome.DeadLettered` (unknown type/content-type/schema sai) HOẶC exception `JsonException` (payload không deserialize được) → `DeadLetter` NGAY (retry vô ích).
  - **Transient** = exception khác → `Retry` nếu `priorAttempts + 1 < MaxDeliveryAttempts` (còn lượt), hết lượt → `DeadLetter`.
  - Retry = APP-PUBLISH sang retry-exchange (topic durable) → retry-queue có `x-message-ttl`=RetryDelay + `x-dead-letter-exchange`=MAIN exchange → hết TTL tự dead-letter QUAY LẠI main (giữ routing key gốc) → redeliver. Đếm số vòng bằng header ứng dụng `x-bedrock-attempt` (app tự tăng), KHÔNG parse `x-death`.
  - Final DLQ GIỮ NGUYÊN cơ chế broker-side NACK requeue=false (main queue `x-dead-letter-exchange`=DLX không đổi → tránh PRECONDITION_FAILED khi re-declare).
- Rationale (verifiable):
  - **Root cause P0-02:** không có kênh trung gian giữa "ack" và "quarantine vĩnh viễn" → mọi trục trặc tạm thời = mất message reaction. Fix bản chất = thêm tầng retry có delay + giới hạn deterministic, tách quyết định vào hàm thuần để kiểm chứng đầy đủ KHÔNG cần broker (phần logic rủi ro nhất).
  - **Vì sao channel retry RIÊNG có publisher-confirms:** retry là app-publish (khác dead-letter final broker-side). Nếu ACK bản gốc TRƯỚC khi broker xác nhận nhận bản retry → hiccup broker làm MẤT message (phá at-least-once). Confirms bắt `TryPublishToRetryAsync` chờ broker-ack; publish lỗi → KHÔNG ack, NACK requeue=false → DLX final (bảo toàn, không rơi message). Channel tách khỏi consume channel để không xen chuỗi delivery/ack; publish serialize qua gate (IChannel không an toàn publish đồng thời).
  - **Vì sao đếm qua header ứng dụng, không `x-death`:** `x-death` gộp theo cặp (queue,reason), có thể reset/di dời khi topology đổi → không tin cậy để giới hạn. App tự tăng → ngưỡng deterministic, unit-test được.
  - **Vì sao JsonException = permanent:** deserialize/id-mismatch là hỏng payload/envelope — retry cho ra lỗi y hệt, chỉ tốn vòng lặp; quarantine ngay để soi.
- Alternatives: (a) plugin `rabbitmq_delayed_message_exchange` (loại: phụ thuộc plugin broker, không thuần AMQP, khó portable); (b) parse `x-death` để đếm (loại: không tin cậy như trên); (c) publish retry trên chính consume channel không confirms (loại: mất message khi broker hiccup — phá at-least-once); (d) requeue=true để retry (loại: hot-loop poison khi handler luôn lỗi, không có delay/giới hạn).
- Consequences: message transient bị trễ tối đa `MaxDeliveryAttempts × RetryDelay` trước khi vào DLQ (đánh đổi latency lấy tự-hồi-phục). Retry giữ MessageId → inbox idempotency dedupe nếu redeliver trùng. Retry topology thêm 1 exchange + 1 queue/consumer; retry-publish thêm 1 channel confirms/consumer. Config sai (MaxDeliveryAttempts<1, RetryDelay<=0) chặn boot (fail-fast).
- Reversibility: Medium (gỡ retry tier → quay lại DLX-ngay; nhưng tái mở lỗ mất tự-hồi-phục transient).
- Traceability: re-audit P0-02 (Gate 0 — mục cuối); liên quan AD-073 (RabbitMQ reliability), AD-082 (envelope content-type → DeadLettered), AD-093 (schema-version invalid → DeadLettered), CP11; F29.

---

### AD-096 — [Wave 1] Error invariant fields GET-ONLY (không init) — re-audit P1-08 (refine AD-078/A-26)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — xử lý Wave-1 finding P1-08 của re-audit.
- Provenance/Evidence: đọc `Error.cs` — `Code`/`Message`/`Type` là public `init` → dù constructor cấm rỗng, caller vẫn `validError with { Code = "" }` (record clone) hoặc đổi Type sau validate → A-26 KHÔNG thực sự enforce. Đã sửa: ba field thành GET-ONLY (bỏ `init`), giữ `Details { get; init; }` để `WithDetails` clone. Guard: `ErrorTests.{Invariant_fields_have_no_public_setter (Code/Message/Type, reflection SetMethod == null), Details_remains_settable_for_with_expression, WithDetails_preserves_core_fields_and_attaches_details}`. 18/18 pass, build 0-warning.
- Context: re-audit P1-08 — A-26 chỉ validate ở constructor; public init cho phép record `with` bỏ qua invariant → error rỗng/không đồng nhất type có thể lọt vào snapshot/error-mapping.
- Decision/Change: `Code`/`Message`/`Type` get-only; mọi tạo error đi qua constructor/factory (đã validate). `Details` giữ `init` (không thuộc invariant code/message) để `WithDetails` = `this with { Details = ... }` vẫn hoạt động; record copy-constructor sao chép backing field ba core → giữ nguyên qua clone.
- Rationale (verifiable): **Root cause P1-08:** invariant chỉ ở một cửa (constructor) trong khi có cửa thứ hai (init setter qua `with`). Fix bản chất = ĐÓNG cửa thứ hai (get-only) thay vì thêm validate rải rác. Reflection test khẳng định không còn setter — bắt được nếu ai đó thêm lại `init`.
- Alternatives: (a) thêm validate trong một custom init setter (loại: C# init không cho validate gọn + vẫn cho gán rỗng nếu quên); (b) đổi Error thành class thường bỏ record (loại: mất giá trị equality/`with` cho Details, phá call-site). Get-only giữ record semantics mà vẫn khoá invariant.
- Consequences: KHÔNG thể `with { Code = ... }` (đúng ý đồ). `WithDetails` vẫn dùng được. Không call-site nào trong repo dùng `with { Code/Message/Type }` (đã grep — 0 kết quả) nên không phá build.
- Reversibility: High (thêm lại `init` là đảo được, nhưng tái mở lỗ bypass).
- Traceability: re-audit P1-08; refine AD-078 (A-26 Error invariants); F20 error contract; R29.

---

### AD-097 — [Wave 1] Outbox last_error REDACT thật (không chỉ truncate) — re-audit P1-07 (refine AD-087)
- Status: Confirmed
- Date: 2026-07-12
- Decider: AI(Kiro) — xử lý Wave-1 finding P1-07 của re-audit.
- Provenance/Evidence: đọc `EfOutboxDispatcher.SanitizeError` — chỉ `"{Kiểu}: {ex.Message}"` + cắt `MaxLastErrorLength`; tên "Sanitize" gây hiểu nhầm ĐÃ làm sạch trong khi `ex.Message` từ driver có thể chứa connection string/credential/token/query → rò vào cột `outbox.last_error` + admin/telemetry. Đã sửa: tách helper THUẦN `OutboxErrorFormatter.Redact` (redact URI-credentials + cặp key nhạy cảm + Bearer token) + đổi tên phản ánh đúng; dispatcher gọi nó. Guard: `OutboxErrorFormatterTests` (13: classification prefix, URI creds, kv password/pwd/token/api_key/access_key/secret, bearer có/không key Authorization, message vô hại giữ nguyên, bound length, null throws). 13/13 pass, build 0-warning.
- Context: re-audit P1-07 — `SanitizeError` chỉ truncate → không thực sự sanitize; rủi ro lưu secret/PII.
- Decision/Change: helper thuần `OutboxErrorFormatter.Redact(ex)` = `{KiểuException}: {redact(message)}` cắt `MaxLastErrorLength`. Redaction (regex source-generated `[GeneratedRegex]`): (1) `scheme://user:pass@` → `scheme://***:***@` (giữ scheme/host để chẩn đoán); (2) `Bearer <token>` → `Bearer ***`; (3) `(password|pwd|passwd|token|secret|api[_-]?key|access[_-]?key|authorization|auth)[=:]value` → `key=***`.
- Rationale (verifiable): **Root cause P1-07:** tin `Exception.Message` là an toàn để lưu nguyên. Fix bản chất = redact các mẫu secret phổ biến TRƯỚC khi lưu + giữ classification (tên kiểu — máy-đọc, không nhạy cảm) làm tín hiệu chẩn đoán ổn định. Tách hàm thuần cho phép unit-test đầy đủ luật redaction (không cần DB). GHI RÕ giới hạn: redaction theo mẫu là BEST-EFFORT (giảm rủi ro), không thay thế việc chỉ log detail đầy đủ vào sink có kiểm soát access — nêu trong docstring.
- Alternatives: (a) chỉ đổi tên `FormatBoundedError` (loại: đúng tên nhưng KHÔNG giảm rủi ro rò — re-audit yêu cầu redact); (b) chỉ lưu tên kiểu, bỏ message (loại: mất khả năng chẩn đoán "vì sao fail" — đánh mất giá trị A-28); (c) redact toàn bộ message thành "***" (loại: mất chẩn đoán). Redact-theo-mẫu giữ cân bằng chẩn đoán/bảo mật.
- Consequences: `outbox.last_error` giờ đã redact + bound. Redaction best-effort (mẫu mới lạ có thể lọt) → chấp nhận, ghi rõ. Không đổi schema (cột không đổi) → không cần migration.
- Reversibility: High (helper thuần, đổi luật redaction dễ; không đụng schema).
- Traceability: re-audit P1-07; refine AD-087 (A-28 last_error); F25/F33.
