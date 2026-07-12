# Tái kiểm toán chuyên sâu kiến trúc và triển khai `platform-base`

> Ngày kiểm toán: 2026-07-12  
> Phạm vi: `.kiro/specs/platform-base`, `platform/src`, `platform/tests`, `platform/tools`, `platform/scripts`, `platform/docker-compose.yml`, `.github/workflows/ci.yml`  
> Baseline Git quan sát: `7d13499` (`2026-07-12 12:39:58 +0700`) + toàn bộ thay đổi chưa commit đang có trong worktree  
> Mục đích: đánh giá lại sau đợt hardening; không mặc định tin trạng thái `DONE` trong ledger; đối chiếu code, test, cấu hình chạy và rủi ro vận hành thực tế.

---

## 1. Kết luận điều hành

Nền tảng đã tiến bộ lớn về cấu trúc: dependency direction rõ, module Identity được tách tầng, persistence đã có keyed registration, outbox có lease, inbox claim nguyên tử, publisher dùng confirm + `mandatory`, migration history theo schema, test kiến trúc có negative control và CI đã fail-closed cho Testcontainers. Đây không còn là skeleton yếu.

Tuy nhiên trạng thái hiện tại **chưa đạt 10/10 và chưa nên gọi là production-grade hoàn chỉnh**. Điểm hợp lý tại baseline này là **7,2/10**. Lý do chính không nằm ở style hay số lượng test, mà ở một số lỗ hổng correctness/security còn đi xuyên qua các guard hiện có:

1. **P0 Security:** cấu hình Forwarded Headers xóa cả danh sách proxy/network tin cậy. Khi danh sách rỗng, ASP.NET Core cho phép mọi nguồn chuyển tiếp `X-Forwarded-*`; client trực tiếp có thể giả IP/scheme. Rate limiter đang partition theo IP sau bước này nên có thể bị bypass/spoof.
2. **P0 Reliability:** mọi exception của integration-event handler bị `NACK requeue=false` ngay và đi thẳng DLQ. Lỗi DB/network tạm thời bị đối xử như poison message vĩnh viễn; không có durable retry tier hay replay workflow.
3. **P1 Correctness:** domain event chỉ được restore nếu chính dispatcher ném. Nếu dispatch thành công nhưng `base.SaveChangesAsync`/audit/soft-delete/depth limit thất bại, event đã bị clear và không được restore.
4. **P1 Contract:** `schema-version` thiếu/sai bị mặc định về `1`, version âm/không tương thích không bị dispatcher kiểm tra; metadata hiện được “mang theo” nhưng chưa thực sự bảo vệ contract.
5. **P1 Idempotency:** scope key chọn `TenantId` *thay cho* `UserId`; hai user cùng tenant có thể đụng key. Anonymous caller toàn hệ dùng chung namespace `anonymous`. Store contract cũng chưa có owner/fencing token, response replay hay giới hạn/hash raw key.
6. **P1 Runtime:** RabbitMQ consumer chưa có lifecycle/recovery/health semantics đủ mạnh; health check broker xanh không chứng minh consumer còn đăng ký và đang nhận message.
7. **P1 Anti-drift:** architecture/contract tests vẫn hardcode Identity và RabbitMQ. Module/adapter/contracts assembly mới có thể xuất hiện mà không được guard quét.

Do đó, ledger hardening là bằng chứng triển khai hữu ích nhưng **không thể dùng như chứng nhận hoàn tất**. Nhiều mục `DONE` cần đổi thành `PARTIAL`.

---

## 2. Bằng chứng kiểm chứng

### 2.1 Quality gate đã chạy

| Gate | Kết quả |
|---|---:|
| `platform/scripts/vp.cmd all` | PASS |
| Build Debug | 0 warning, 0 error |
| `validate_ci.py` | PASS |
| Test Debug | 311 pass, 17 skip, 0 fail, tổng 328 |
| Build Release | 0 warning, 0 error |
| Test Release | 311 pass, 17 skip, 0 fail, tổng 328 |
| `dotnet list Platform.slnx package --vulnerable --include-transitive` | Không thấy advisory đã biết từ nguồn NuGet hiện tại |
| Tracked `bin/obj` tại index hiện tại | 0 |

### 2.2 Giới hạn của bằng chứng

- 17 test bị skip vì Docker không khả dụng tại máy audit. Chúng gồm Postgres concurrency/migration và RabbitMQ end-to-end. Code đã được thiết kế để CI GitHub (`CI=true`) không swallow lỗi container, nhưng lần audit local này **không chứng minh runtime Docker**.
- Test xanh chủ yếu chứng minh các scenario đã viết. Nó không phủ broker restart, consumer recovery, unknown proxy spoof, transient handler retry, DLQ isolation nhiều module, lease expiry giữa batch, hoặc SaveChanges fail sau domain-event dispatch.
- Worktree chưa phải một baseline commit sạch: 4.365 status entries, gồm 4.238 staged deletions `bin/obj`, 74 modified và 53 untracked. Báo cáo đánh giá đúng trạng thái filesystem hiện tại, không khẳng định trạng thái này đã được version hóa hay reproducible từ clone mới.

---

## 3. Bảng điểm chi tiết

| Mặt đánh giá | Điểm | Nhận xét ngắn |
|---|---:|---|
| Dependency direction / layering | 8,4 | Ma trận project reference tốt; Domain/Contracts sạch; Api không ref Infrastructure. |
| Modular monolith boundaries | 7,2 | Keyed persistence đã đúng hướng; discovery và public surface còn hardcode/rộng. |
| Persistence / transaction | 7,8 | UoW, migration schema, inbox atomic tốt; domain-event failure window và capability coupling còn lỗi. |
| Outbox producer | 8,0 | Lease + confirm + mandatory + backoff tốt; lease renewal/finalize diagnostics/operability chưa đủ. |
| Event consumer | 5,8 | Inbox atomic tốt nhưng retry classification, lifecycle, health và DLQ isolation yếu. |
| Security | 6,2 | JWT/Argon/options tiến bộ; Forwarded Headers default là release blocker. |
| API/application patterns | 7,1 | Result/pipeline rõ; command-vs-query transaction và Error invariant còn bypass. |
| Observability/operations | 6,6 | Có OTel/metrics/health; backlog/replay/consumer liveness chưa có. |
| Tests/anti-drift | 7,7 | Suite rộng, negative controls tốt; discovery hardcode, Docker local skip, không có coverage/mutation gate. |
| Delivery/reproducibility | 6,4 | CI + Dockerfile + migration bundle tốt; images/actions/SDK/restore chưa khóa đủ. |
| **Tổng hợp** | **7,2/10** | **Khung tốt, chưa production-safe tuyệt đối.** |

---

## 4. Findings mới và findings chưa đóng thật

### P0-01 — Forwarded Headers đang tin mọi proxy khi cấu hình mặc định

**Bằng chứng**

- `BedrockHttpSecurityExtensions.cs:64-65` gọi `KnownProxies.Clear()` và `KnownIPNetworks.Clear()`.
- `appsettings.json` không khai `HttpSecurity:KnownProxies` hoặc `KnownNetworks`; hai list mặc định rỗng.
- Middleware chạy trước rate limiter; `RemoteIpAddress` sau forwarded-header được dùng làm partition key.
- Tài liệu chính thức của Microsoft ghi rõ việc clear cả hai list cho phép bất kỳ proxy/network chuyển tiếp header và không được khuyến nghị vì rủi ro spoof: [Forwarded Headers Middleware ignores headers from unknown proxies](https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/8.0/forwarded-headers-unknown-proxies).

**Tác động**

- Client gọi trực tiếp có thể đặt `X-Forwarded-For` để thay IP quan sát.
- Rate limiting theo IP có thể bị né hoặc dùng để làm người khác bị limit.
- `X-Forwarded-Proto` giả có thể ảnh hưởng logic sinh URL/cookie/redirect về sau.
- Comment trong code nói “chỉ tin proxy khai tường minh”, nhưng hành vi thực tế ngược lại.

**Sửa bắt buộc**

1. Không clear default trust list khi không có cấu hình.
2. Thêm `ForwardedHeadersEnabled`; nếu bật cho deployment sau reverse proxy thì yêu cầu ít nhất một `KnownProxy`/`KnownNetwork`, hoặc một policy cloud được quyết định tường minh.
3. Thêm behavioral tests:
   - remote IP không tin cậy + `X-Forwarded-For` phải bị bỏ qua;
   - proxy tin cậy phải được chấp nhận;
   - rate-limit partition không đổi theo header giả từ nguồn không tin cậy.
4. Không chỉ test object options; phải chạy middleware bằng `TestServer` với `Connection.RemoteIpAddress` cụ thể.

**Trạng thái:** OPEN — release blocker.

### P0-02 — Handler lỗi tạm thời bị đưa thẳng vào DLQ, không có durable retry

**Bằng chứng**

- `RabbitMqConsumer.cs:160-163` bắt mọi `Exception` từ dispatcher rồi `BasicNack(... requeue:false)`.
- Cùng action được dùng cho malformed envelope, unknown event type và lỗi hạ tầng/handler.
- Không có retry exchange/queue, delivery-attempt policy, delayed retry, classification exception, hay replay service.

**Tác động**

- PostgreSQL timeout 1 giây, network chập chờn, deadlock hoặc dependency tạm lỗi đều biến message hợp lệ thành quarantine vĩnh viễn.
- “Không hot-loop” là đúng, nhưng “đi thẳng DLQ” không phải giải pháp retry production.
- Outbox phía producer đã có backoff/attempts; consume side không có semantics tương đương.

**Sửa bắt buộc**

1. Phân loại lỗi:
   - permanent: envelope hỏng, event type không hỗ trợ, schema không tương thích → DLQ ngay;
   - transient: DB/network/timeout/circuit-open → durable retry có delay và max attempts;
   - handler business rejection: quyết định rõ ACK, retry hay quarantine theo contract.
2. Dùng broker-side retry topology/policy hoặc retry scheduler bền; không dùng `requeue=true` vô hạn.
3. Sau max attempts mới chuyển final DLQ, giữ attempt count, first/last failure, event type, consumer và correlation.
4. Có replay command/admin API được audit + authorization, không yêu cầu sửa DB tay.
5. E2E test transient fail N lần rồi thành công; permanent fail vào DLQ; max-attempt vào final DLQ đúng một lần.

**Trạng thái:** PARTIAL — DLQ đã có, reliability policy chưa có.

### P1-01 — Domain event vẫn có thể bị mất sau khi dispatch thành công

**Bằng chứng**

- `PlatformDbContext.cs:144` clear event trước dispatch.
- `RestoreDomainEvents` chỉ nằm trong `catch` quanh `_domainEventDispatcher.DispatchAsync` (`:148-158`).
- `base.SaveChangesAsync` chạy sau toàn bộ dispatch tại `:60`, ngoài vùng restore.
- Nếu audit/soft-delete, depth guard hoặc database write thất bại sau dispatch, original events đã mất khỏi entity.

**Tác động**

- Retry trên cùng aggregate/context có thể commit state nhưng không phát lại side effect/domain reaction tương ứng.
- Ledger A-14 gọi `DONE`, nhưng guard hiện chỉ phủ handler ném, không phủ SaveChanges ném sau handler thành công.

**Sửa đề xuất**

- Đặt toàn bộ lifecycle dispatch → conventions → base save trong một failure boundary và restore chính xác các event đã dequeue khi bất kỳ bước sau đó fail.
- Tránh restore trùng event mới được handler raise; cần collector theo entity + sequence của một SaveChanges attempt.
- Thêm test `handler succeeds -> provider SaveChanges fails -> original event remains -> retry dispatches once`.
- Thêm test vượt `MaxDomainEventDispatchDepth` cũng không silent-drop các event chưa commit.

**Trạng thái:** PARTIAL.

### P1-02 — `schema-version` chưa là invariant runtime

**Bằng chứng**

- `RabbitMqConsumer.DecodeSchemaVersion` trả `1` khi header thiếu hoặc parse lỗi (`RabbitMqConsumer.cs:188-207`).
- `long` bị cast trực tiếp sang `int`; overflow có thể wrap.
- `EfIntegrationEventDispatcher` validate content type và payload ID nhưng không validate `SchemaVersion`.
- Registry chỉ key theo `EventType`; version metadata không tham gia compatibility policy.
- Comment `IncomingIntegrationMessage` nói “registry keyed theo version”, trái với code và AD-083.

**Tác động**

- Producer hỏng/foreign producer có header rác bị giả dạng v1.
- Schema breaking cùng event type có thể deserialize “thành công” nhưng sai semantic.
- Snapshot build-time không bảo vệ message đến từ deployment khác version.

**Sửa đề xuất**

- Missing/invalid/non-positive/overflow version phải là malformed envelope, không default im lặng.
- Registry cần metadata descriptor: event type, supported version/range, CLR type, compatibility strategy.
- Nếu policy là breaking change luôn đổi EventType, vẫn phải validate `SchemaVersion >= 1` và consistency với descriptor để phát hiện producer sai.
- Thêm tests missing/zero/negative/overflow/future incompatible version.

**Trạng thái:** PARTIAL; hạ A-10 từ DONE xuống PARTIAL.

### P1-03 — Idempotency scope gây collision xuyên user/anonymous và contract store thiếu fencing

**Bằng chứng**

- `IdempotencyKeyScope.For<TInput>` chọn `TenantId ?? UserId ?? "anonymous"`.
- Khi có `TenantId`, `UserId` bị bỏ khỏi key; mọi user trong tenant dùng chung namespace.
- Mọi anonymous caller dùng chung literal `anonymous`.
- Raw key được ghép thẳng, không giới hạn hoặc hash.
- `TryBegin/Complete/Abort` không trả claim owner/fencing token; một stale request có thể `Abort` claim mới sau khi claim cũ hết TTL tùy adapter.
- Success chỉ lưu trạng thái completed, không replay response.

**Tác động**

- Hai người dùng hợp lệ có cùng idempotency key có thể chặn nhau.
- Anonymous endpoint có thể bị gây xung đột diện rộng.
- Key quá dài làm phình cache/storage/log; raw key có thể chứa dữ liệu nhạy cảm.
- Nếu DB commit thành công nhưng `CompleteAsync` lỗi, client nhận lỗi trong khi side effect đã commit; retry bị conflict đến TTL và không có response để replay.

**Sửa đề xuất**

- Scope cấu trúc: `{app}:{module}:{useCase}:{tenant?}:{user/client}:{hash(rawKey)}`; tenant và user là hai chiều, không phải fallback loại trừ.
- Anonymous idempotency phải có caller identity ổn định hoặc chỉ bật trên endpoint có client key/device/session đáng tin.
- Validate raw key length/charset; lưu hash cố định độ dài.
- `TryBegin` trả claim token; `Complete/Abort` compare-and-set theo token.
- Chốt semantics: replay response/status hoặc trả conflict có endpoint/status query; không chỉ “completed nhưng không biết kết quả”.
- Integration test đa-node/TTL rollover/stale abort/complete failure sau DB commit.

**Trạng thái:** PARTIAL; A-13 chưa DONE.

### P1-04 — RabbitMQ consumer lifecycle, shutdown và liveness chưa production-grade

**Bằng chứng**

- Consumer mở một connection/channel rồi `Task.Delay(Timeout.Infinite)`; không theo dõi consumer tag, shutdown event hoặc trạng thái đăng ký.
- Dispatcher không nhận `stoppingToken` từ BackgroundService.
- `StopAsync` đợi base service rồi dispose channel/connection, nhưng không quản lý rõ các callback đang in-flight.
- Health check chỉ kiểm kết nối broker bằng một probe; không chứng minh consumer channel/registration còn sống.
- Không có test broker restart, channel close, topology recovery hay graceful shutdown với handler đang chạy.

**Tác động**

- Broker có thể reachable trong health check nhưng subscriber đã chết/stale.
- Shutdown có thể đóng channel khi callback đang ACK/NACK.
- Pod vẫn ready dù consume path không hoạt động, backlog tăng âm thầm.

**Sửa đề xuất**

- Quản lý explicit state machine: connecting → topology-ready → consuming → recovering → stopped.
- Track consumer tag; cancel consumer; ngừng nhận mới; chờ in-flight theo timeout; sau đó dispose.
- Truyền linked shutdown token vào dispatcher/handler.
- Readiness phải phản ánh consumer registration khi consume được bật; liveness/recovery metric riêng.
- Test broker restart và forced channel closure bằng Testcontainers.

**Trạng thái:** OPEN/PARTIAL.

### P1-05 — Default DLX dùng chung + bind `#` làm lẫn DLQ giữa module

**Bằng chứng**

- Mọi consumer mặc định dùng `DeadLetterExchangeName = "bedrock.dead-letter"`.
- Mỗi DLQ được bind vào exchange này bằng routing key `#`.
- NACK không đặt dead-letter routing key riêng nên giữ original routing key.

**Tác động**

- Khi có nhiều queue/module, mỗi DLQ mặc định nhận dead-letter của tất cả module, tạo bản sao chéo, lẫn ownership và có thể rò payload giữa bounded contexts/đội vận hành.

**Sửa đề xuất**

- Dùng DLX/DLQ per consumer, hoặc routing key quarantine namespaced theo queue/consumer và binding chính xác.
- Validate uniqueness của queue, consumer, DLQ và dispatcher key trong composition.
- E2E test hai consumer: poison của A chỉ xuất hiện trong DLQ A.

**Trạng thái:** OPEN.

### P1-06 — Outbox lease chưa được ràng buộc với worst-case batch và mất lease bị silent

**Bằng chứng**

- Một lease áp cho cả batch; publish tuần tự từng message.
- Validation chỉ yêu cầu `ClaimLease > 0`, trong khi doc nói phải dài hơn worst-case batch.
- Không renew lease trong vòng publish.
- Nếu finalize `ExecuteUpdate` trả `0`, code không log/metric và tiếp tục.

**Tác động**

- Batch lớn hoặc broker chậm làm lease hết trước message cuối; dispatcher khác reclaim và publish song song.
- At-least-once cho phép duplicate, nhưng duplicate do cấu hình sai không nên vô hình.
- Operator không biết instance mất ownership sau publish.

**Sửa đề xuất**

- Claim nhỏ/per-message hoặc renew lease có fencing.
- Validate quan hệ `ClaimLease`, batch size và publish timeout; tốt hơn là bỏ coupling bằng per-message claim.
- Metric/log cho finalize affected=0 (`outbox.lease_lost`), kèm message/claim/context.
- Test clock advance giữa batch + hai dispatcher.

**Trạng thái:** PARTIAL; A-08 chưa hoàn toàn đóng.

### P1-07 — `SanitizeError` chỉ truncate, không sanitize

**Bằng chứng**

- `EfOutboxDispatcher.SanitizeError` lưu `ExceptionType: ex.Message`, tối đa 1024 ký tự.
- Exception message từ driver/adapter có thể chứa host, virtual host, connection detail, query fragment hoặc dữ liệu do upstream tạo.

**Tác động**

- Tên method/comment tạo cảm giác dữ liệu đã được làm sạch trong khi chỉ được giới hạn độ dài.
- Có nguy cơ lưu secret/PII vào bảng outbox và công cụ admin/telemetry sau này.

**Sửa đề xuất**

- Đổi tên thành `FormatBoundedError` nếu chỉ truncate.
- Lưu error code/classification ổn định; detail nhạy cảm chỉ log vào sink đã redaction và access control.
- Redact connection strings, credentials, token, payload; không dựa vào `Exception.Message` là an toàn.

**Trạng thái:** OPEN.

### P1-08 — `Error` constructor invariant bị bypass bởi public `init`

**Bằng chứng**

- Constructor cấm code/message rỗng.
- Nhưng `Error.Code`, `Error.Message`, `Error.Type` là public `init` (`Error.cs:27-31`).
- Vì là record, caller có thể tạo `validError with { Code = "" }` hoặc đổi type sau validation.

**Tác động**

- A-26 không thực sự enforce invariant.
- Snapshot/error mapping có thể nhận error rỗng hoặc type không đồng nhất với factory.

**Sửa đề xuất**

- Cho `Code`, `Message`, `Type` get-only; mọi mutation đi qua constructor/factory.
- Nếu cần `WithDetails`, giữ ba core fields bất biến và copy sâu details hoặc dùng immutable collection.
- Thêm test `with`/reflection đảm bảo không có public init setter cho invariant fields.

**Trạng thái:** PARTIAL; A-26 chưa DONE.

### P1-09 — Use-case abstraction không phân biệt command trả giá trị và query

**Bằng chứng**

- `ICommandUseCase<T>` có Transaction decorator.
- Toàn bộ `IUseCase<TInput,TOutput>` không được transaction-wrap vì có thể là query.
- Command trả kết quả, như refresh-token rotation, phải tự nhớ mở transaction.

**Tác động**

- Safety phụ thuộc kỷ luật từng developer. Command mới trả DTO có thể quên transaction nhưng vẫn compile và test kiến trúc pass.
- Pipeline contract không biểu đạt semantic read/write.

**Sửa đề xuất**

- Tách `IQuery<TIn,TOut>`, `ICommand<TIn>`, `ICommand<TIn,TOut>` hoặc marker `ITransactionalUseCase`.
- Transaction decorate cả hai họ command, không decorate query.
- Guard test mọi type write/command có transaction behavior; registration order được finalize sau khi compose module.

**Trạng thái:** OPEN; liên quan A-12.

### P1-10 — Keyed persistence còn hai khe hở multi-module

**Bằng chứng**

- Registry chặn trùng key nhưng không chặn cùng `TContext` đăng ký bằng nhiều key.
- Mọi call vẫn `AddDbContext<TContext>` unkeyed. Cùng type context với hai configure delegate có thể tạo registration/options order-dependent.
- `AddBedrockPersistence` mặc định đăng ký outbox writer, inbox và refresh-token store dù module có thể không map các bảng tương ứng.

**Tác động**

- Key khác nhau tạo cảm giác isolation nhưng cùng concrete context/config có thể last-wins.
- Persistence foundation bị kéo capability Identity-specific (`IRefreshTokenStore`) vào mọi module.
- Service resolve thành công nhưng fail runtime vì thiếu `AddRefreshTokens`/`AddOutboxInbox`.

**Sửa đề xuất**

- Registry enforce bijection context type ↔ module key, trừ khi có use case được thiết kế rõ.
- Tách `AddBedrockPersistence`, `AddOutboxPersistence`, `AddInboxPersistence`, `AddRefreshTokenPersistence`.
- Startup validate model chứa entity/table bắt buộc khi capability được bật.
- Test hai module thật, hai context, hai database, two workers và wrong-capability fail at startup.

**Trạng thái:** PARTIAL; A-01/A-19/A-35 liên quan.

### P1-11 — Architecture và contract guards vẫn hardcode module/adapter hiện tại

**Bằng chứng**

- `CoreAssemblies.cs` biết trực tiếp Identity và RabbitMQ.
- `ModuleBoundaryTests` chỉ kiểm Identity.
- `AdapterIsolationTests` chỉ kiểm RabbitMQ.
- Event/error snapshot chỉ quét Bedrock + Identity assemblies.

**Tác động**

- Thêm `Rooms`, `Booking`, `Redis`, `S3` hoặc contracts mới mà quên sửa tests sẽ không bị kiểm.
- Guard mạnh trên phạm vi nhỏ tạo false confidence toàn solution.

**Sửa đề xuất**

- Discover project/assembly theo convention từ solution hoặc output manifest do build tạo.
- Assert ít nhất một module/adapter và assert discovered count khớp project graph để tránh vacuous pass.
- Áp cùng rules cho mọi `Modules.*.{Domain,Application,Infrastructure,Api,Contracts}` và `Adapters.*`.
- Contract catalog nên là manifest/golden file do module contribute, không là mảng type hardcode trong test.

**Trạng thái:** OPEN; A-11.

### P1-12 — Registry vẫn dựa vào object chưa khởi tạo

**Bằng chứng**

- Runtime registry và contract tests dùng `RuntimeHelpers.GetUninitializedObject`.
- Hardening hiện tại chỉ thêm lỗi rõ + test buộc mọi event tuân theo kỹ thuật này.

**Tác động**

- Guard đã “đóng đinh workaround” thay vì loại bỏ root cause.
- Metadata không được compiler enforce tại authoring surface; event assembly mới ngoài danh sách vẫn có thể fail boot.

**Sửa đề xuất**

- Dùng attribute bất biến trên type, static abstract metadata contract, hoặc registry descriptor đăng ký explicit/source-generated.
- Không instantiate object để đọc type metadata.
- Source generator/analyzer có thể enforce unique EventType và schema version compile-time.

**Trạng thái:** PARTIAL; A-21 chưa đóng tận gốc.

### P1-13 — JWT verify-only composition không validate key ring lúc start

**Bằng chứng**

- `AddBedrockAuthCore` bind `JwtKeyRingOptions` nhưng không đăng ký validator/`ValidateOnStart`.
- Validator chỉ được `AddBedrockSecurity` (sign side) thêm.
- Comment khẳng định `Bedrock.Api` có thể dùng standalone verify-only.

**Tác động**

- Consumer chỉ dùng API verification có thể boot với key ring rỗng/sai và lỗi muộn khi authenticate.

**Sửa đề xuất**

- Đặt key-ring validator ở assembly trung lập mà cả sign và verify composition dùng idempotently.
- Verify-only host phải fail startup với duplicate kid, active kid thiếu, secret invalid, issuer/audience rỗng.

**Trạng thái:** PARTIAL; A-16/A-18.

### P1-14 — Trace context đang trộn với correlation ID và chưa theo chuẩn propagation đầy đủ

**Bằng chứng**

- `Activity.Current.Id` được lưu vào `CorrelationId` và RabbitMQ `BasicProperties.CorrelationId`.
- Consumer parse giá trị này như W3C traceparent.
- Không truyền `tracestate`, baggage hoặc producer/publish Activity.

**Tác động**

- Business correlation ID và distributed trace context bị trộn semantic.
- Vendor/adapter khác khó interop chuẩn; sampling/vendor state bị mất.

**Sửa đề xuất**

- Tách `CorrelationId` khỏi `traceparent`/`tracestate` headers.
- Dùng `DistributedContextPropagator`/OpenTelemetry propagator để inject/extract.
- Tạo producer span khi publish và consumer span khi process; link về original enqueue trace khi cần.

**Trạng thái:** PARTIAL; A-29.

### P1-15 — Messaging tắt mặc định nhưng module vẫn tạo outbox event

**Bằng chứng**

- Refresh rotation luôn enqueue `UserTokenRefreshedIntegrationEvent`.
- Host chỉ đăng ký dispatcher/worker khi `Bedrock:Messaging:Enabled=true`.
- Khi false/thiếu, không có worker và không có readiness/warning cho backlog.

**Tác động**

- Event được commit nhưng không bao giờ phát; lỗi là silent accumulation thay vì fail-loud.

**Sửa đề xuất**

- Nếu event delivery là required capability của module, messaging phải là startup requirement.
- Nếu offline mode hợp lệ, phải có explicit mode + warning/health degraded + retention/backlog policy.
- Test host production config thiếu flag phải fail hoặc báo degraded theo decision đã chốt.

**Trạng thái:** OPEN.

### P1-16 — Operability của outbox/inbox/DLQ chưa đủ để trực chiến

Đã có `last_error`, counters và publish lag, nhưng còn thiếu:

- oldest pending age/backlog gauge theo module;
- claimed-but-expired/lease-lost metric;
- retry/dead-letter/replay counts và reason code;
- inbox handler duration/failure status;
- DLQ depth/readiness/alert;
- secure replay/audit workflow;
- runbook xử lý poison, schema mismatch, broker outage và migration rollback.

**Trạng thái:** PARTIAL; A-28.

---

## 5. Findings P2 về cấu trúc, maintainability và delivery

### P2-01 — Public surface quá rộng

- `Bedrock.Infrastructure` lộ khoảng 38 public types, gồm nhiều concrete implementation (`EfInboxStore`, `EfOutboxWriter`, `EfRefreshTokenStore`, `Throwing*`, `DomainEventDispatcher`) không cần là API consumer-facing.
- Identity lộ DbContext, design-time factory, validator, use case và HTTP DTO.
- Public surface rộng tăng coupling và làm refactor/migration thành breaking change giả.

**Khuyến nghị:** public chủ yếu cho extension methods, options cần cấu hình, base context và ports thực sự; implementation để `internal`, test qua `InternalsVisibleTo` đã có.

### P2-02 — `OutboxMessage` persistence model vẫn nằm trong Application

`IEventBusPublisher` đã được tách khỏi persistence shape bằng `OutgoingIntegrationMessage`, đây là cải tiến đúng. Nhưng mutable EF record `OutboxMessage` vẫn nằm trong `Bedrock.Application.Messaging.Dispatch`, khiến Application sở hữu claim/retry/dead-letter schema. Nên dời record/map về Infrastructure; Application chỉ giữ ports/envelopes.

### P2-03 — Capability package vẫn là dependency magnet

`Bedrock.Application` chứa caching, email, storage, search, external auth, messaging, security và persistence ports; `Bedrock.Infrastructure` chứa implementation/default của hầu hết capability. Nếu đây là monorepo nội bộ nhỏ thì chấp nhận được, nhưng nếu mục tiêu là base tái sử dụng/publishable, consumer bị kéo dependency và public API không cần thiết. Cần chốt chiến lược: internal platform kit hay package ecosystem; hiện code đang ở giữa hai mô hình.

### P2-04 — Contract snapshot chưa mô tả đầy đủ wire schema

Descriptor đã tốt hơn `Type.Name`, nhưng còn bỏ sót:

- nested generic nullability (`List<string?>` so với `List<string>`);
- JSON property name/converter/ignore/number handling;
- enum member values;
- required/optional/default semantic;
- polymorphism/discriminator;
- serializer options/version;
- discovered assemblies ngoài Identity.

Do đó A-22 chỉ nên là PARTIAL.

### P2-05 — `NoBusinessInCore` guard dùng danh sách literal thủ công

Danh sách `guest/room/resort/admin/staff` hữu ích làm smoke guard nhưng dễ false-negative với domain term mới và false-positive theo substring. Nó không thay thế dependency boundaries. Nên coi đây là heuristic supplementary, không là chứng minh core hoàn toàn business-agnostic.

### P2-06 — Comment lịch sử quá dài làm chìm contract hiện tại

Nhiều source file chứa task ID, tên finding, lịch sử sửa và giải thích dài hàng chục dòng. Quyết định lịch sử nên nằm trong ADR/journal; source comment nên tập trung invariant, precondition và failure semantics hiện tại. Ví dụ Rabbit consumer, dispatcher, PlatformDbContext và DI extensions đã khó scan hơn cần thiết.

### P2-07 — Spec/ledger drift

- Ledger vẫn ghi 248 tests trong khi baseline hiện tại là 328 total.
- Bảng A-01..A-35 bỏ hẳn A-23 và A-24.
- Một số mục `DONE` mâu thuẫn code: A-10, A-13, A-14, A-16, A-21, A-26, A-29.
- `IncomingIntegrationMessage` nói registry keyed theo version trong khi AD-083 quyết định ngược lại.
- Ledger vừa là nhật ký dài vừa là status dashboard; status đầu file nhanh stale.

**Khuyến nghị:** tách immutable audit history khỏi machine-readable current status; CI chỉ validate facts có thể máy kiểm, không cố validate tuyên bố chủ quan “DONE”.

### P2-08 — Chưa có module scaffolder/template

Module thứ hai vẫn phải copy nhiều project, DI, migration factory, architecture registration và contract catalog bằng tay. Đây là nguồn drift trực tiếp với A-11/A-33. Cần `dotnet new` template hoặc script generator có test snapshot và tự đăng ký manifest.

### P2-09 — Reproducibility/supply-chain chưa khóa đủ

- `global.json` dùng `rollForward: latestFeature`.
- Không có `packages.lock.json`/locked restore.
- Docker base images dùng floating tags `10.0`; Postgres/RabbitMQ dùng tags không digest.
- GitHub Actions dùng major tags `@v4`, không pin commit SHA.
- Không thấy SBOM/provenance/signature/image scan/dependency automation policy.
- Audit vulnerability hiện sạch là điểm tốt, nhưng là snapshot theo nguồn hiện tại, không phải guarantee tương lai.

### P2-10 — Line-ending policy chưa rõ

Git cảnh báo nhiều file LF sẽ chuyển thành CRLF khi Git chạm. Repo không có `.gitattributes`; `.editorconfig` không chốt `end_of_line`. Điều này tạo diff noise đa nền tảng. Nên thêm `.gitattributes` và quyết định LF thống nhất cho source/config/scripts.

### P2-11 — Test quality gate chưa có coverage/mutation budget

Các project có coverlet collector nhưng CI không collect/publish coverage và không có threshold. Không nên chạy theo “100% coverage”, nhưng các vùng critical (idempotency, transaction, envelope, security middleware, retry state machine) cần branch coverage và mutation testing có mục tiêu. Hiện suite nhiều nhưng vẫn bỏ lọt P0-01 vì chỉ test cấu hình, không test behavior.

### P2-12 — Identity sample chưa chứng minh một auth lifecycle hoàn chỉnh

Module hiện chỉ có refresh rotation. Không có login/issue initial refresh token, user/session store đầy đủ, revoke endpoint, logout, key rollover runbook hay permission policy thực. Nó chứng minh pattern transaction/outbox, chưa chứng minh product Identity production. Không nên dùng độ xanh của sample để suy ra toàn nền tảng đã sẵn sàng thương mại.

### P2-13 — Worktree chưa phải delivery unit an toàn

4.238 artifact deletions đang staged, còn source/spec/migration/test mới chưa commit. Trước merge cần chia commit có chủ đề, kiểm migrations và report riêng. Một commit khổng lồ 4.365 entries làm review, bisect và rollback khó đáng kể.

---

## 6. Đánh giá lại A-01 đến A-35

| Finding cũ | Đánh giá lại | Lý do |
|---|---|---|
| A-01 module-scoped persistence | **PARTIAL** | Keyed ports tốt; chưa chặn cùng context nhiều key, capability registrations còn trộn, test 2 DB/2 worker thật local chưa chạy. |
| A-02 inbox atomic claim | **CLOSED về code / V2 runtime** | Raw insert atomic trong transaction đúng hướng; Postgres race test bị skip local nhưng CI fail-closed. |
| A-03 consumer DLQ/retry | **PARTIAL** | DLQ chống drop đã có; transient retry/replay chưa có. |
| A-04 publisher serialization/reconnect | **PARTIAL-CLOSED producer** | Channel gate/recreate tốt; broker restart runtime chưa chứng minh; consumer recovery còn mở. |
| A-05 mandatory + confirm | **CLOSED về code / V2 runtime** | `mandatory:true` + confirmation tracking; unroutable runtime test còn thiếu. |
| A-06 migration history per schema | **CLOSED về code / V2 runtime** | Helper và migration test đúng; local Docker skip. |
| A-07 Testcontainers fail-closed CI | **CLOSED** | Catch filter không swallow khi `CI=true`; GitHub Actions đặt CI. |
| A-08 outbox lease | **PARTIAL** | Không giữ DB tx qua broker là đúng; lease whole-batch/không renew/affected=0 silent. |
| A-09 ordering contract | **CLOSED nếu unordered được chấp nhận** | Port đã nói rõ at-least-once/no FIFO; module nghiệp vụ không được dựa ordering. |
| A-10 envelope validation | **PARTIAL** | Content type + ID có; schema version invalid bị default, compatibility chưa enforce. |
| A-11 auto-discovery | **OPEN** | Tests/catalog hardcode Identity/RabbitMQ. |
| A-12 DI/pipeline finalization | **PARTIAL** | Double-call guard có; late registration, command/query semantics, duplicate keyed context còn mở. |
| A-13 idempotency | **PARTIAL** | Complete/Abort có nhưng scope collision, anonymous namespace, raw key và fencing/replay còn lỗi. |
| A-14 domain-event transaction | **PARTIAL** | Handler throw restore; SaveChanges/convention/depth failure sau dispatch vẫn mất event. |
| A-15 cookie/CSRF model | **MOSTLY CLOSED** | Bearer model rõ; antiforgery opt-in có. Forwarded-header P0 là security finding độc lập cần chặn release. |
| A-16 options validation | **PARTIAL** | Nhiều family đã validate; verify-only JWT không ValidateOnStart; một số adapter dùng eager object thay Options lifecycle. |
| A-17 Argon PHC bounds | **PARTIAL-CLOSED scope cũ** | Reject-before-compute tốt; NeedsRehash/rehash lifecycle chưa có; max 1 GiB cần threat-model/tune. |
| A-18 JWT key ring | **PARTIAL** | Sign/verify host chính chia sẻ; verify-only validation thiếu; options mutable singleton. |
| A-19 capability packages | **OPEN** | Dependency magnet và capability-specific persistence vẫn còn. |
| A-20 persistence model leak | **PARTIAL** | Publisher port đã sạch; `OutboxMessage` vẫn ở Application. |
| A-21 registry metadata | **PARTIAL** | Lỗi rõ hơn nhưng vẫn dùng uninitialized object và hardcoded test catalog. |
| A-22 contract snapshot | **PARTIAL** | Generic/root nullability tốt hơn; wire schema/discovery/nested nullability còn thiếu. |
| A-23 module internals public | **OPEN** | Ledger bỏ dòng này; public surface vẫn rộng. |
| A-24 Identity sample shallow | **OPEN** | Vẫn chỉ refresh-token rotation/demo handler. |
| A-25 filter composition/audit protect | **CLOSED về code** | Named filter + protected created metadata, có tests. Cần thêm multi-tenant module thực khi xuất hiện. |
| A-26 Error/Result footgun | **PARTIAL** | Constructor invariant có nhưng public init + record `with` bypass được. |
| A-27 cancellation middleware | **CLOSED cho HTTP path** | Request-aborted không map 500; response-started rethrow. Consumer cancellation còn finding riêng. |
| A-28 outbox/inbox operability | **PARTIAL** | Last error/attempt có; replay/gauge/inbox/DLQ/lease metrics thiếu. |
| A-29 telemetry xuyên bus | **PARTIAL** | Parent trace nối được; correlation conflation, tracestate/baggage/producer span thiếu. |
| A-30 tracked artifacts | **PENDING COMMIT** | Index hiện 0 tracked artifact nhưng 4.238 deletions chưa thành baseline commit. |
| A-31 spec drift | **OPEN/PARTIAL** | Journal guard xanh nhưng status/text vẫn stale và mâu thuẫn. |
| A-32 comment history | **OPEN** | Source comments vẫn quá dài và chứa lịch sử task/finding. |
| A-33 module template | **OPEN** | Chưa có scaffolder. |
| A-34 supply chain/reproducibility | **OPEN** | Không lock restore/digest/SHA/SBOM; SDK roll-forward. |
| A-35 placeholder ports | **OPEN** | Nhiều fail-loud/default ports chưa có production adapter semantics; capability validation chưa gắn model. |

Tổng hợp:

- Closed hoặc closed trong scope hẹp: A-02, A-05, A-06, A-07, A-09, A-25, A-27.
- Partial: A-01, A-03, A-04, A-08, A-10, A-12–A-22 (trừ A-19 open), A-26, A-28–A-31.
- Open: A-11, A-19, A-23, A-24, A-32–A-35.

---

## 7. Điểm thiết kế đang làm tốt và nên giữ

1. **Project graph đúng chiều:** Domain và Messaging.Contracts không phụ thuộc tầng ngoài; Application không kéo EF/ASP.NET/Npgsql; adapter RabbitMQ không ref Infrastructure/Api.
2. **Composition root rõ:** Host là nơi duy nhất nối Api + Infrastructure + adapter + module.
3. **Persistence multi-module đã có hướng đúng:** module key explicit, keyed UoW/outbox/inbox/refresh store và keyed handlers.
4. **Inbox atomic claim đúng bản chất:** arbitration xảy ra ở DB trước handler, cùng transaction với business effects.
5. **Outbox không giữ transaction qua broker I/O:** lease claim ngắn rồi publish ngoài transaction là cải tiến quan trọng.
6. **Publisher reliability tốt hơn:** channel gate, confirm tracking, persistent message, `mandatory:true`, recreate connection/channel.
7. **Migration per schema:** `MigrationsHistoryTable` theo Identity schema và migration bundle out-of-band là quyết định đúng cho multi-instance.
8. **Security primitives:** Argon2id PHC reject bounds trước compute; JWT unique `kid`, HS256 algorithm allowlist, key ring rotation.
9. **Test architecture có negative controls:** giúp tránh NetArchTest false-green/vacuous engine.
10. **CI fail-closed Docker:** local developer vẫn làm việc khi thiếu Docker, CI không được tự skip container startup error.
11. **Artifact cleanup có guard:** `validate_ci.py` ngăn re-track `bin/obj`.
12. **Result/ProblemDetails/correlation path khá nhất quán:** HTTP mapping tập trung, request-abort không bị log như server error.

Các điểm này là nền tốt để nâng lên 10/10; không cần rewrite toàn bộ hoặc đổi kiến trúc nền.

---

## 8. Kế hoạch sửa theo thứ tự rủi ro

### Gate 0 — Bắt buộc trước merge/release

1. Sửa trust policy Forwarded Headers + behavioral security tests.
2. Thiết kế transient retry/final DLQ/replay cho consumer; không đưa mọi exception thẳng DLQ.
3. Validate schema-version runtime, không default invalid về v1.
4. Sửa domain-event restore cho toàn bộ SaveChanges failure window.
5. Sửa idempotency scope tenant+user/anonymous, hash/limit key và claim fencing.
6. Commit artifact cleanup riêng; commit source/migrations/tests riêng; chạy lại Release + CI Docker.

### Wave 1 — Runtime correctness và liveness

1. Consumer lifecycle/recovery/graceful shutdown/readiness.
2. DLQ isolation per module/consumer.
3. Outbox lease renewal/fencing + metric lost lease.
4. Bỏ bypass invariant của `Error`.
5. Verify-only JWT validation.
6. Messaging-disabled mode fail-fast/degraded explicit.

### Wave 2 — Kiến trúc mở rộng module

1. Tách command/query/command-with-result abstractions.
2. Tách persistence capabilities khỏi foundation.
3. Enforce context↔module-key uniqueness và duplicate worker/consumer registration.
4. Auto-discover mọi module/adapter/contracts assembly.
5. Dời mutable outbox model về Infrastructure; thu hẹp public surface.

### Wave 3 — Operability và contract maturity

1. Backlog/oldest age/lease/DLQ/inbox metrics + alerts.
2. Secure replay API/CLI + audit log + runbook.
3. Chuẩn hóa trace propagation (`traceparent`, `tracestate`, baggage, producer span).
4. Contract descriptor theo JSON wire semantics hoặc schema artifact chuẩn.

### Wave 4 — Delivery hygiene

1. Module template/scaffolder + manifest.
2. Locked restore, SDK exact policy, image digest, Actions SHA, SBOM/image scan.
3. Coverage critical-path + mutation budget.
4. Rút gọn comments; sync design/requirements/tasks/ledger.
5. Chốt chiến lược package: internal monolith kit hay publishable capabilities.

---

## 9. Definition of Done thực tế cho 10/10

Chỉ nên chấm 10/10 khi thỏa đồng thời:

- Không còn P0/P1 open trong báo cáo này, hoặc có accepted risk được owner ký và deadline rõ.
- Unknown proxy spoof test fail trước fix và pass sau fix.
- Consumer chứng minh transient retry, permanent DLQ, max-attempt DLQ, replay và broker restart bằng runtime test thật.
- Domain event không mất trên mọi failure point trước commit.
- Idempotency chứng minh isolation tenant+user/anonymous và fencing dưới concurrency/TTL rollover.
- Hai module thật dùng hai DbContext/database/worker/consumer mà không last-wins hay cross-DLQ.
- Architecture/contract tests tự discover module/adapter mới và có count/non-vacuous guard.
- Release test + Docker integration + migration clean-database + migration upgrade-database đều xanh trên CI.
- Có backlog/DLQ/consumer-liveness alert và runbook được diễn tập.
- Worktree sạch, migrations/source/spec được commit theo lát reviewable; clone sạch restore/build/test reproducible.
- Supply-chain artifacts có pin/provenance/SBOM phù hợp threat model.

---

## 10. Phán quyết cuối

Đợt hardening đã xử lý đúng nhiều lỗi nền tảng và đáng giữ. Nhưng cách ghi `DONE` hiện tại thiên về “đã thêm code/test cho một nhánh” hơn là “invariant không còn đường phá”. Ba ví dụ rõ nhất là:

- options test xanh nhưng Forwarded Headers runtime vẫn trust-all;
- domain-event restore test xanh nhưng SaveChanges failure vẫn mất event;
- idempotency Complete/Abort test xanh nhưng key scope vẫn collision xuyên user.

Vì vậy quyết định chuyên gia tại baseline này là:

> **Không rewrite. Không tuyên bố 10/10. Giữ kiến trúc lõi, đóng Gate 0 và Wave 1 trước; sau đó mới mở rộng module/capability.**

Sau Gate 0 + Wave 1, nền tảng có thể tiến tới khoảng 8,5–9/10. Mức 10/10 chỉ hợp lý khi runtime failure modes, multi-module discovery, operability và reproducibility được chứng minh bằng guard/test thực, không chỉ bằng comment hoặc ledger.
