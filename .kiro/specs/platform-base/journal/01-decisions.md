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
