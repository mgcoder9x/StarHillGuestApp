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
