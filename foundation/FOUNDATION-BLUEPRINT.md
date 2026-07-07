# FOUNDATION BLUEPRINT — Thiết kế nền (target) cho hệ thống lớn

> **Loại tài liệu:** BẢN THIẾT KẾ ĐỂ XÂY (prescription), khác `ARCHITECTURE-REVIEW.md` (chẩn đoán hiện trạng F1–F35).
> **Mục tiêu:** một base **cực chất, domain-agnostic, modular-monolith-ready**, cắm công nghệ mới (RabbitMQ/Elasticsearch/Zalo·Google/Gmail/Redis/S3) bằng **thêm adapter/module**, KHÔNG sửa lõi.
> **Quan hệ với foundation hiện tại:** giữ toàn bộ phần kỹ thuật tốt (Result/Error, auth Argon2id+JWT+refresh rotation, EF base/UoW, validation decorator, ProblemDetails, security headers, health, rate-limit); **tái cấu trúc ranh giới + đặt ổ cắm mở rộng** theo tài liệu này. Mỗi finding F# dưới đây map tới review.

---

## 1. Mười luật bất biến (invariants)

1. **Lõi không biết nghiệp vụ.** `BuildingBlocks.*` không chứa guest/room/resort/Admin/Staff (F2/F3/F4).
2. **Lõi không biết công nghệ cụ thể.** Lõi chỉ có **port**; RabbitMQ/Elastic/Gmail/Zalo/Redis/S3 nằm ở `Adapters.*` (F24).
3. **Một điểm ghi DB cho mỗi giao dịch.** Use case thay đổi dữ liệu + ghi Outbox **trong cùng transaction** (F5/F25).
4. **Use case không gọi hạ tầng ngoài trực tiếp.** Không publish bus, không gọi SDK — chỉ qua port (`IOutboxWriter`, `IEmailSender`, ...) (F25).
5. **Module là bounded context.** Module A chỉ chạm module B qua `B.Contracts` + integration event (F30).
6. **Dữ liệu thuộc về module.** Schema-per-module, migration per-module, không JOIN chéo module (F31).
7. **Api không kéo Infrastructure.** Composition chỉ ở Host (F14).
8. **Interface tự chặn sai.** Đặt tên/scope port sao cho lập trình viên không thể dùng sai kiến trúc (F25).
9. **Fail-fast mọi môi trường.** Thiếu cấu hình/port bắt buộc → chặn boot (không chỉ Development) (F7/F35).
10. **Mọi lát refactor giữ build 0 warning + test xanh.** Architecture test là lưới an toàn.

---

## 2. Solution layout (target)

```
src/
  BuildingBlocks/
    BuildingBlocks.Domain/            # Entity, ValueObject, DomainEvent, Result, Error, Guard, IConcurrencyToken
    BuildingBlocks.Application/       # Ports/*, Behaviors/*, UseCase, Paging, IntegrationEvent, IOutboxWriter
    BuildingBlocks.Infrastructure/    # EF base (DbContext/UoW/Repo), Cryptography, Tokens, Outbox impl, Inbox impl, defaults
    BuildingBlocks.Api/               # ProblemDetails, Authentication(mechanism), HttpSecurity, Versioning, OpenApi, RateLimit, Observability  (KHÔNG ref Infrastructure)
  Adapters/
    Messaging.RabbitMq/  Search.Elasticsearch/  Email.Gmail/  Email.Smtp/
    Cache.Redis/  Storage.S3/  ExternalAuth.Google/  ExternalAuth.Zalo/
  Modules/
    Identity/     { Identity.Contracts, Identity.Domain, Identity.Application, Identity.Infrastructure, Identity.Api }
    Rooms/        { Rooms.Contracts,    Rooms.Domain,    Rooms.Application,    Rooms.Infrastructure,    Rooms.Api }
  Host/
    StarHill.Api/                     # Program.cs, appsettings.*, composition (chọn adapter bật)
tests/
  BuildingBlocks.ArchitectureTests/   # dependency rule + module boundary + no-business-in-core
  <Module>.UnitTests / <Module>.IntegrationTests
  Adapters.<Tech>.IntegrationTests    # Testcontainers per adapter
```

**Khác biệt cốt lõi so với hiện tại:** `Foundation.Api/Application/Infrastructure` đang **vừa là base vừa là app** → tách thành **BuildingBlocks (nền) + Modules (nghiệp vụ) + Adapters (công nghệ) + Host (ráp)** (F1/F13).

---

## 3. Dependency matrix (enforce bằng NetArchTest)

| Project | Được reference | TUYỆT ĐỐI KHÔNG |
|---|---|---|
| `BuildingBlocks.Domain` | — | mọi thứ khác |
| `BuildingBlocks.Application` | Domain | EF, ASP.NET, adapter, module |
| `BuildingBlocks.Infrastructure` | Application, Domain | ASP.NET Http pipeline, module, adapter cụ thể |
| `BuildingBlocks.Api` | Application | **Infrastructure** (F14), module, adapter |
| `Adapters.<Tech>` | Application (Ports) | module, adapter khác, Api |
| `Modules.<M>.Contracts` | — (DTO thuần) | mọi thứ |
| `Modules.<M>.Domain` | BuildingBlocks.Domain | EF, ASP.NET, module khác |
| `Modules.<M>.Application` | M.Domain, BuildingBlocks.Application, **B.Contracts** (module khác chỉ Contracts) | M.Infrastructure, EF, ASP.NET |
| `Modules.<M>.Infrastructure` | M.Application, BuildingBlocks.Infrastructure | Api, module khác (trừ Contracts) |
| `Modules.<M>.Api` | M.Application, BuildingBlocks.Api | Infrastructure của module khác |
| `Host` | tất cả (Api + Infrastructure + Adapters + Modules) | (là composition root duy nhất) |

**Test bắt buộc:** (a) no-business-in-core (cấm chuỗi guest/room/resort/Admin/Staff trong `BuildingBlocks.*`); (b) `BuildingBlocks.Api` không ref `*.Infrastructure`; (c) `Modules.A` không ref `Modules.B.{Domain,Application,Infrastructure}`; (d) `Adapters.*` chỉ ref Application.

---

## 4. DI composition contract

**Quy ước cặp API:** `AddXxxCore()` (đăng ký **port default/no-op + behavior**, luôn gọi) ↔ `AddYyyXxx(cfg)` (đăng ký **adapter cụ thể**, chỉ ở Host).

```csharp
// Host/Program.cs — nơi DUY NHẤT compose:
builder.Services
    .AddBuildingBlocksCore()                 // Result/behaviors/clock/currentuser
    .AddBuildingBlocksWeb(cfg)               // ProblemDetails, auth mechanism, versioning, OpenApi, rate-limit, observability
    .AddBuildingBlocksPersistence(cfg);      // EF base + outbox/inbox impl + startup validator

builder.Services
    .AddMessagingCore()      .AddRabbitMqMessaging(cfg)       // bỏ dòng adapter = tắt tính năng
    .AddSearchCore()         .AddElasticsearchSearch(cfg)
    .AddEmailCore()          .AddSmtpEmail(cfg)               // đổi .AddGmailEmail(cfg) không đụng use case
    .AddCacheCore()          .AddRedisCache(cfg)
    .AddExternalAuthCore()   .AddGoogleAuth(cfg).AddZaloAuth(cfg);

builder.Services
    .AddIdentityModule(cfg)
    .AddRoomsModule(cfg);
```

**Luật DI (sửa F18):**
- Foundation default dùng **`TryAdd`** (app override được).
- Port **single-implementation** có **duplicate-guard** (fail-fast nếu >1 impl không cố ý).
- Port **multi-implementation** (vd `IExternalAuthProvider`, `IIntegrationEventHandler<T>`) đăng ký `IEnumerable<T>` **có chủ đích** + registry resolve theo tên/type.
- Thay `NotInNamespaceOf<DbContext>` bằng **marker tường minh** `IManualRegistration` cho service hard-require DbContext (sửa F6).
- `AddFoundation()` all-in-one **chỉ dùng trong template/sample**, KHÔNG trong library core.

---

## 5. Ports chuẩn (final contracts)

### 5.1 Persistence / Unit of Work (giữ, làm chặt)
```csharp
public interface IUnitOfWork {
    IRepository<T> Repository<T>() where T : Entity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);
}
```
- Bỏ `IRepository.Query()` (F9) → đọc phức tạp qua **query service/read-model** trả DTO.
- Rotation refresh + Outbox **luôn** trong `ExecuteInTransactionAsync` (F5).

### 5.2 Messaging — Outbox/Inbox (tên tự chặn sai — F25)
```csharp
// Application (use case CHỈ thấy cái này):
public interface IOutboxWriter { Task EnqueueAsync(IntegrationEvent e, CancellationToken ct); }
// Worker/Infrastructure (KHÔNG lộ use case):
public interface IOutboxDispatcher { Task DispatchPendingAsync(CancellationToken ct); }
public interface IEventBusPublisher { Task PublishAsync(OutboxMessage m, CancellationToken ct); }   // impl ở Adapters.Messaging.RabbitMq
// Consumer:
public interface IInboxStore { Task<bool> TryMarkProcessedAsync(Guid messageId, string consumer, CancellationToken ct); }
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent { Task HandleAsync(TEvent e, CancellationToken ct); }

public abstract record IntegrationEvent(Guid Id, DateTimeOffset OccurredAt) { public abstract string EventType { get; } public virtual int SchemaVersion => 1; }
```

### 5.3 Search (ownership — F26)
```csharp
public interface ISearchIndex<TDoc> { Task IndexAsync(TDoc doc, CancellationToken ct); Task DeleteAsync(string id, CancellationToken ct); }
public interface ISearchQuery<TDoc> { Task<SearchResult<TDoc>> SearchAsync(SearchRequest req, CancellationToken ct); }
// mỗi TDoc thuộc 1 module; index-name + mappingVersion + alias swap (blue/green reindex); poison-doc → dead-letter.
```

### 5.4 Email / Cache split / Storage (F28)
```csharp
public interface IEmailSender  { Task SendAsync(EmailMessage msg, CancellationToken ct); }              // Gmail/Smtp/SendGrid adapter
public interface IAppCache         { Task<T?> GetAsync<T>(string k, CancellationToken ct); Task SetAsync<T>(string k, T v, CacheEntryOptions o, CancellationToken ct); Task RemoveAsync(string k, CancellationToken ct); }
public interface IDistributedLock  { Task<ILockHandle?> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct); }
public interface IIdempotencyStore { Task<bool> TryBeginAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct); }
public interface IRateLimitStore   { Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken ct); }
public interface IFileStorage      { Task<string> SaveAsync(FileBlob blob, CancellationToken ct); Task<Stream> OpenAsync(string key, CancellationToken ct); }
```

### 5.5 External Auth (F27 — chi tiết thực tế)
```csharp
public interface IExternalAuthProvider {
    string Name { get; }                                                     // "google" | "zalo"
    Task<ExternalAuthChallenge> CreateChallengeAsync(ExternalAuthRequest r, CancellationToken ct);   // state + PKCE + nonce + returnUrl(whitelist)
    Task<ExternalUserProfile>   CompleteAsync(ExternalAuthCallback cb, CancellationToken ct);         // verify state/pkce, replay-protect
}
public interface IExternalAuthProviderRegistry { IExternalAuthProvider Resolve(string name); }
public sealed record ExternalUserProfile(string Provider, string ProviderUserId, string? Email, bool? EmailVerified, string? DisplayName);
// KHÔNG giả định provider nào cũng có email_verified (Zalo có thể không trả email) → Email/EmailVerified nullable.
// ProviderUserId unique + account-linking policy + email-trust policy ở Identity.Application.
```

### 5.6 Cross-cutting (giữ, tách sạch)
`IClock` · `ICurrentUser` (mở rộng `Permissions/TenantId?/SessionId?` — F23) · `ITokenGenerator` · `IPasswordHasher` · `IJwtTokenService` (key-ring `kid` — F22) · `IHtmlSanitizer` · `IConcurrencyToken` (đổi tên khỏi xmin — F8).

---

## 6. Anatomy of a Module (khuôn mẫu — vertical slice trong layer)

```
Modules/Rooms/
  Rooms.Contracts/            # PUBLIC: DTO + integration event + hằng số cho module khác dùng
    Events/ RoomCreatedIntegrationEvent.cs
    Dtos/   RoomSummaryDto.cs
  Rooms.Domain/               # Room, RoomStatus, RoomQrToken, domain rules/events (thuần POCO)
  Rooms.Application/          # vertical-by-feature
    CreateRoom/   { CreateRoomCommand, CreateRoomUseCase, CreateRoomValidator }
    RotateToken/  { ... }
    Ports/        # port riêng module (nếu có), read-model IRoomQueries
  Rooms.Infrastructure/
    Persistence/  RoomsDbContext (schema "rooms"), configurations, migrations, EfRoomQueries
    Integration/  RoomsIntegrationEventHandlers (nếu consume event module khác)
  Rooms.Api/      RoomEndpoints.cs, RoomContracts(http request/response)  # per-endpoint policy
```

**Đăng ký module một dòng ở Host:** `AddRoomsModule(cfg)` → nội bộ gọi `AddDbContext<RoomsDbContext>` + `AddRoomsPersistence` + validators + endpoint registrar. Module tự đủ, gỡ module = xóa 1 dòng + 1 thư mục.

**Giao tiếp liên-module:** Rooms phát `RoomCreatedIntegrationEvent` (trong `Rooms.Contracts`) qua `IOutboxWriter`; module khác consume qua `IIntegrationEventHandler<RoomCreatedIntegrationEvent>`. **Không** gọi thẳng `Rooms.Application` từ module khác (F30).

---

## 7. Outbox / Inbox — luồng chuẩn (F5/F25)

**Ghi (producer) — nguyên tử:**
```
UseCase.ExecuteAsync:
  uow.ExecuteInTransactionAsync(async ct => {
     repo.Add(entity);                       // thay đổi state
     await outboxWriter.EnqueueAsync(evt);   // ghi OutboxMessage CÙNG transaction (chưa publish)
     await uow.SaveChangesAsync(ct);         // 1 commit — state + outbox all-or-nothing
  });
```
**Phát (worker nền):** `IOutboxDispatcher.DispatchPendingAsync` đọc `outbox WHERE processed_at IS NULL ORDER BY occurred_at LIMIT n` → `IEventBusPublisher.PublishAsync` (RabbitMQ) → đánh dấu `processed_at`. Retry + backoff; poison → `error_count`/dead-letter.
**Nhận (consumer):** nhận message → `IInboxStore.TryMarkProcessedAsync(messageId, consumerName)` (idempotency, chống xử lý trùng) → nếu mới thì chạy `IIntegrationEventHandler<T>`; xong commit inbox + business trong 1 transaction.

**Schema (per-module hoặc shared infra schema):**
```
outbox_message(id, event_type, schema_version, payload jsonb, occurred_at, processed_at?, error_count, correlation_id)
inbox_message (message_id, consumer, processed_at)  PK(message_id, consumer)
```
> Đảm bảo: **at-least-once** publish + **idempotent** consume = hiệu ứng đúng-một-lần về mặt nghiệp vụ. DB là source-of-truth; bus chỉ là kênh.

---

## 8. Data ownership / migration (F31)

- **Mỗi module 1 `DbContext` + 1 schema** (`identity`, `rooms`...). Cùng một PostgreSQL vật lý nhưng **tách schema**; cấm FK chéo schema module.
- **Migration per-module:** mỗi module có history table riêng (`__EFMigrationsHistory` theo schema) → deploy/migrate độc lập.
- **Liên-module KHÔNG JOIN.** Cần dữ liệu module khác → gọi API nội bộ / đọc read-model được projection qua integration event.
- Outbox/Inbox: đặt ở **schema infra dùng chung** hoặc per-module (chọn per-module nếu muốn module tự chủ hoàn toàn).

---

## 9. Versioning (F32)

- **HTTP API:** `Asp.Versioning` (URL `/v1`, hoặc header). OpenAPI group theo version. Deprecation policy có thời hạn.
- **Integration event:** `EventType` (string ổn định) + `SchemaVersion` (int). Quy tắc evolution: **chỉ thêm field optional** = backward-compat; **breaking** = tạo `EventType` mới (v2) chạy song song tới khi consumer chuyển xong. Consumer là **tolerant reader** (bỏ qua field lạ).

---

## 10. Resilience standard cho adapter (F33)

- Dùng `Microsoft.Extensions.Resilience`/Polly v8, áp **ở biên adapter** (KHÔNG ở use case/lõi).
- Pipeline chuẩn per-adapter: **timeout** → **retry** (exponential + jitter, CHỈ thao tác idempotent) → **circuit-breaker** → **fallback**. Cấu hình qua options per-adapter (`RabbitMq:Resilience`, `Elasticsearch:Resilience`...).
- Nguyên tắc: một dependency chậm/chết KHÔNG được kéo sập request khác (bulkhead/isolation). Publish outbox retry ở worker, không ở request path người dùng.

---

## 11. Telemetry standard (F34/F21)

- **OpenTelemetry** 3 trụ: **traces** (spans cho HTTP/EF/bus/handler), **metrics** (`Meter` nghiệp vụ + hạ tầng), **logs** (Serilog → OTLP). Export OTLP (collector).
- **Propagate W3C `traceparent`** xuyên HTTP + bus (outbox message mang `correlation_id`/traceparent). **Thống nhất** `X-Correlation-Id` (header) = `traceId` (ProblemDetails) = trace hiện hành (sửa F21): resolve 1 lần, lưu `HttpContext.Items`, cả header lẫn ProblemDetails đọc lại.
- Metrics tối thiểu: request rate/latency/error, EF query time, rate-limit rejects, outbox lag (pending age), consumer processing time, external-auth success/fail.

---

## 12. Secrets / config governance (F35/F7)

- Secret KHÔNG commit: dev = User-Secrets; prod = env/Key Vault/SOPS. `appsettings.json` chỉ chứa non-secret + placeholder.
- **Validate-on-start cho MỌI options bắt buộc** + **startup validator tường minh** (IStartupFilter/IHostedService) kiểm các **port bắt buộc** đã đăng ký (IUnitOfWork/IUserAuthStore/IRefreshTokenStore/IOutboxWriter...) → chặn boot ở **mọi môi trường** (không chỉ Development — sửa F7). Bật `ValidateOnBuild`/`ValidateScopes = true` tường minh.

---

## 13. Auth / AuthZ target

- **Mechanism vs policy tách bạch (F3):** `BuildingBlocks.Api` cung cấp **cơ chế** (JWT bearer, `ICurrentUser`, 401/403 ProblemDetails, helper tạo policy). Role/permission cụ thể (Admin/Staff/...) do **module Identity + Host** khai.
- **Permission-based authz (F23):** policy theo **permission claim**; role là một nguồn suy ra permission. `ICurrentUser` mở rộng `Permissions`, `TenantId?`, `SessionId?`.
- **JWT key-ring (F22):** `kid` trong header; active key ký + previous keys verify (rolling); cân nhắc RS256/ES256 khi nhiều service verify.
- **External auth (F27):** module Identity, adapter per-provider; state/PKCE/nonce/returnUrl-whitelist/replay-protection/account-linking; profile chuẩn hoá email nullable.
- **Cookie/CORS mode (F17):** options `CookieSameSiteMode`; same-site → Strict/Lax (không CSRF token); cross-site → SameSite=None + CSRF (anti-forgery/double-submit) + CORS siết.
- **Log an toàn (F15):** MỌI nơi log path đi qua masker dùng chung (kể cả exception handler).

---

## 14. Application pipeline behaviors (F13)

Bọc quanh use case theo thứ tự (decorator/pipeline): **Logging/Tracing** → **Validation** (đã có) → **Authorization** (permission check) → **Idempotency** (cho command có `IdempotencyKey`, dùng `IIdempotencyStore`) → **Transaction** (mở `ExecuteInTransactionAsync` cho command ghi + outbox) → **UseCase**. Behavior là generic, đăng ký ở `AddBuildingBlocksCore`.

---

## 15. Testing strategy (F11)

- **Unit:** domain rules + use case (fakes cho port).
- **Architecture:** dependency matrix (§3) + module boundary (F30) + no-business-in-core (F2/F3/F4) + naming.
- **Integration (module):** SQLite Docker-free cho provider-agnostic + **Testcontainers/PostgreSQL** cho Postgres-specific (xmin, partial index, migration, race — F11).
- **Adapter integration:** Testcontainers cho RabbitMQ/Elastic/Redis; WireMock cho external-auth (Google/Zalo) — **không đụng lõi**.
- **Contract:** `AppErrors` code ↔ FE `ErrorCode` (reflection); integration-event schema snapshot (chống breaking vô ý).

---

## 16. Lộ trình xây (build order) — map từ hiện trạng

| Giai đoạn | Việc | Finding |
|---|---|---|
| **P0** | Tách Host khỏi library; gỡ rò nghiệp vụ (PathMasker→options, auth role→app, comment); **fix log raw path** | F1,F2,F3,F4,**F15** |
| **P1** | Refresh rotation trong transaction; DI marker+overload+duplicate policy; startup validator fail-fast; ForwardedHeaders; cookie/CORS mode; Testcontainers Postgres; unique TokenHash | F5,F6,F7,F16,F17,F18,F11,F10 |
| **P1.5** | **Đặt ổ cắm SỚM** (ảnh hưởng cách viết use case): IntegrationEvent + IOutboxWriter/Dispatcher/Inbox + khung AddXxxCore/AddYyy; định nghĩa port Search/Email/Cache-split/ExternalAuth/Storage | F24,F25,F26,F27,F28 |
| **P2** | BuildingBlocks+Modules+Host + semantic folder; module boundary tests; schema-per-module + migration; API/event versioning; resilience; OpenTelemetry; secrets governance; permission authz + JWT key-ring; correlation thống nhất | F12,F13,F30–F35,F19,F20,F21,F22,F23,F29,F8,F9 |

**Nguyên tắc chuyển đổi:** làm từng lát nhỏ, mỗi lát **build 0 warning + test xanh**; KHÔNG kéo SDK công nghệ vào lõi — chỉ thêm **contract + extension point** ở P1.5, impl adapter khi thật sự cần dùng.

---

## 17. Definition of Done — "base cực chất"

- [ ] `BuildingBlocks.*` pass test **no-business-in-core** + Api không ref Infrastructure.
- [ ] Thêm 1 module mới = tạo 5 project theo khuôn + `AddXModule(cfg)` một dòng ở Host; module boundary test xanh.
- [ ] Thêm 1 công nghệ mới (vd RabbitMQ) = tạo `Adapters.Messaging.RabbitMq` + `AddRabbitMqMessaging(cfg)` ở Host + integration test; **0 file lõi Application/Domain bị sửa**.
- [ ] Command ghi dữ liệu luôn đi kèm outbox trong 1 transaction; consumer idempotent qua inbox.
- [ ] Boot fail-fast ở mọi môi trường khi thiếu config/port bắt buộc.
- [ ] 3 trụ telemetry hoạt động; `traceId` = `X-Correlation-Id` = trace hiện hành.
- [ ] Không secret trong repo; validate-on-start phủ mọi options bắt buộc.

> **Tóm tắt một câu:** *Lõi biết "cần gì" (port), adapter biết "làm bằng gì" (tech), module biết "nghiệp vụ gì", Host biết "bật cái nào". Thêm công nghệ/nghiệp vụ = thêm adapter/module, không sửa lõi.* Đó là base cực chất cho dự án lớn.
