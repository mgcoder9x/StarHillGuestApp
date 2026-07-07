# Requirements Document

> **Spec:** platform-base — một **base/platform backend domain-agnostic, modular-monolith-ready** (.NET 10, C#).
> **Nguồn:** rút ra từ `design.md` (thiết kế kỹ thuật), `FOUNDATION-BLUEPRINT.md` (đích), `ARCHITECTURE-REVIEW.md` (findings F1–F35).
> **Định dạng:** EARS. Mỗi tiêu chí truy vết về **F#** (finding) và **I#** (invariant §2 design).

## Introduction

`platform-base` là lõi nền tái sử dụng, tách bạch bốn vai trò: **Bedrock** (lõi chỉ có contract/pipeline, không biết nghiệp vụ/công nghệ), **Adapters** (công nghệ cụ thể), **Modules** (bounded context nghiệp vụ), **Host** (composition root duy nhất). Mục tiêu tối thượng: *thêm công nghệ/nghiệp vụ mới = thêm adapter/module, KHÔNG sửa lõi*.

Tài liệu này mô tả **CÁI GÌ** base phải bảo đảm và **TẠI SAO** (design.md đã mô tả **LÀM THẾ NÀO**). Requirements được nhóm theo build order **P0 → P1 → P1.5 → P2** để tasks breakdown map thẳng.

**Trạng thái thực tế trên đĩa (greenfield — đã kiểm chứng):** thư mục `platform/` **CHƯA tồn tại**; toàn bộ solution sẽ được **tạo mới từ đầu** theo `tasks.md`. Không có "baseline đã test" nào — mọi requirement dưới đây đều ở trạng thái *cần xây và cần nghiệm thu*, không có phần "đã thỏa sẵn". (Một bản dựng thử trước đó đã bị hoàn tác.)

## Glossary

- **Bedrock**: lõi domain-agnostic (Domain/Application/Infrastructure/Api) chỉ chứa contract + pipeline + convention, không biết nghiệp vụ hay SDK công nghệ.
- **Adapter**: project hiện thực một port bằng công nghệ cụ thể (RabbitMQ, Elasticsearch, Redis, S3, Google/Zalo OAuth, Gmail/SMTP).
- **Module**: một bounded context nghiệp vụ, gồm `Contracts/Domain/Application/Infrastructure/Api`.
- **Host**: composition root duy nhất (project chạy được), nơi chọn adapter/module để bật.
- **Port**: interface trừu tượng ở tầng Application mà lõi/nghiệp vụ phụ thuộc; adapter/infra hiện thực.
- **Outbox / Inbox**: bảng transactional-outbox (ghi event cùng transaction state) / bảng idempotency phía consumer.
- **Integration event**: sự kiện cross-boundary giữa các module (khác domain event in-process).
- **Domain event**: sự kiện in-process do entity phát trong một module, dispatch TRƯỚC commit trong cùng transaction (khác integration event cross-module).
- **Claim (outbox)**: thao tác dispatcher "giữ" nguyên tử một batch message pending để instance khác không lấy trùng (row-lock skip-locked hoặc lease qua `next_attempt_at`).
- **Dead-letter**: trạng thái message/document bị cách ly sau khi vượt ngưỡng retry — không chặn luồng chính, chờ xử lý thủ công.
- **Type registry**: ánh xạ `EventType` (string ổn định) → CLR type để consumer deserialize payload an toàn.
- **Correctness Property (CP)**: thuộc tính đúng đắn cấp platform trong design.md, mỗi CP map tới một/nhiều requirement.
- **Finding (F#)**: phát hiện trong `ARCHITECTURE-REVIEW.md`. **Invariant (I#)**: luật bất biến §2 design.md.

## Requirements

> **— Nhóm P0 — Gỡ rò nghiệp vụ & tách vai (F1–F4, F15) —**

### Requirement 1: Lõi không biết nghiệp vụ (domain-agnostic core)

**User Story:** As a platform maintainer, I want the core (`Bedrock.*`) to contain zero business-specific concepts, so that the base is reusable across projects without leakage.

#### Acceptance Criteria
1. THE SYSTEM SHALL đảm bảo không có type/namespace/hằng số/chuỗi trong `Bedrock.*` chứa các khái niệm nghiệp vụ cụ thể (ví dụ `guest`, `room`, `resort`, `Admin`, `Staff`). *(I1/F2/F3/F4)*
2. WHEN một kiến trúc test chạy trên assembly `Bedrock.*`, THE SYSTEM SHALL fail build nếu phát hiện chuỗi nghiệp vụ bị cấm. *(I1)*
3. THE SYSTEM SHALL cung cấp danh sách path cần mask qua options (`MaskedPathPrefixes`) thay vì hardcode path nghiệp vụ trong lõi. *(F2)*
4. THE SYSTEM SHALL cung cấp cơ chế authorization (JWT bearer + `ICurrentUser` + 401/403) nhưng KHÔNG khai role/permission nghiệp vụ cụ thể trong lõi. *(F3)*
5. THE SYSTEM SHALL dùng comment/ví dụ trung lập trong lõi; ví dụ app-specific đặt ở sample/README, không trong `Bedrock.*`. *(F4)*

### Requirement 2: Tách Host khỏi thư viện lõi

**User Story:** As a Host author, I want the base to be a pure library without a runnable host or sample endpoints, so that I can version/package it cleanly and own composition myself.

#### Acceptance Criteria
1. THE SYSTEM SHALL KHÔNG chứa `Program.cs` nghiệp vụ hay endpoint mẫu bên trong `Bedrock.*`. *(F1)*
2. THE SYSTEM SHALL đặt composition root (`Program.cs`, `appsettings.*`, endpoint/module discovery) duy nhất tại project Host (ví dụ `StarHill.Api`). *(F1/I2)*
3. THE SYSTEM SHALL phơi các extension method (`AddBedrockCore/Web/Persistence`, `MapXxx`) để Host lắp ráp, thay vì tự chạy. *(F1)*

### Requirement 3: Che path nhạy cảm ở mọi nơi log

**User Story:** As a platform maintainer, I want every log site (including the exception handler) to mask sensitive paths, so that tokens are never leaked via error logs.

#### Acceptance Criteria
1. WHEN request-logging ghi path, THE SYSTEM SHALL đưa path qua masker dùng chung. *(F15)*
2. WHEN exception handler ghi log cho request lỗi, THE SYSTEM SHALL mask path đúng như request-logging (không log raw token path). *(F15)*
3. THE SYSTEM SHALL lấy danh sách prefix cần mask từ options (`MaskedPathPrefixes`), không hardcode. *(F2/F15)*

> **— Nhóm P1 — Boundary & persistence chắc chắn (F5–F11, F14, F16–F18) —**

### Requirement 4: Api không phụ thuộc Infrastructure

**User Story:** As a Host author, I want `Bedrock.Api` to not reference any Infrastructure, so that the HTTP package doesn't drag EF/Npgsql implementation with it.

#### Acceptance Criteria
1. THE SYSTEM SHALL đảm bảo `Bedrock.Api` KHÔNG có dependency compile-time (trực/gián tiếp) tới bất kỳ `*.Infrastructure`. *(F14/I7)*
2. WHEN kiến trúc test chạy, THE SYSTEM SHALL fail nếu `Bedrock.Api` reference Infrastructure. *(F14)*
3. THE SYSTEM SHALL để việc kết hợp Api + Infrastructure chỉ xảy ra ở Host. *(I2)*

### Requirement 5: Adapter cô lập

**User Story:** As an adapter developer, I want each `Adapters.<Tech>` to depend only on Application ports, so that technologies stay swappable and isolated.

#### Acceptance Criteria
1. THE SYSTEM SHALL đảm bảo `Adapters.<Tech>` chỉ reference `Bedrock.Application`. *(I2/F29)*
2. THE SYSTEM SHALL cấm `Adapters.<Tech>` reference module, adapter khác, hoặc Api. *(F29)*
3. THE SYSTEM SHALL đặt SDK công nghệ cụ thể (RabbitMQ/Elastic/Redis/S3/OAuth) CHỈ trong `Adapters.*`, không trong lõi. *(I2/F24)*

### Requirement 6: Ranh giới module (bounded context)

**User Story:** As a module developer, I want module A to reach module B only via `B.Contracts` + integration events, so that modules stay decoupled bounded contexts.

#### Acceptance Criteria
1. THE SYSTEM SHALL cấm `Modules.A` reference `Modules.B.{Domain,Application,Infrastructure}`. *(F30/I5)*
2. THE SYSTEM SHALL cho phép `Modules.A` reference `Modules.B.Contracts` (DTO + integration event thuần). *(F30)*
3. WHEN module A cần tác động module B, THE SYSTEM SHALL yêu cầu qua integration event (Outbox) chứ không gọi trực tiếp use case của B. *(F30)*

### Requirement 7: Điểm ghi DB duy nhất + giao dịch tường minh

**User Story:** As a module developer, I want a single write point (`IUnitOfWork.SaveChangesAsync`) with explicit transactions, so that multi-entity writes are atomic.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp `IUnitOfWork` với `SaveChangesAsync` là điểm ghi DB duy nhất và `ExecuteInTransactionAsync` cho giao dịch all-or-nothing. *(I3)*
2. WHEN `action` trong `ExecuteInTransactionAsync` ném lỗi, THE SYSTEM SHALL rollback toàn bộ thay đổi. *(I3)*
3. THE SYSTEM SHALL cung cấp `IRepository<T>` KHÔNG phơi `IQueryable` (`Query()`), buộc đọc phức tạp đi qua read-model/query service trả DTO. *(F9)*
4. WHEN `ExecuteInTransactionAsync` được gọi trong lúc một transaction do chính nó mở đang hoạt động, THE SYSTEM SHALL tham gia (join) transaction hiện hành thay vì mở nested transaction — behavior Transaction và use case gọi tường minh không được xung đột. *(I3/F5)*

### Requirement 8: Outbox nguyên tử (transactional outbox)

**User Story:** As a module developer, I want state changes and their integration events persisted in the same transaction, so that data and events never diverge.

#### Acceptance Criteria
1. WHEN một command ghi state và phát integration event, THE SYSTEM SHALL persist entity và `outbox_message` trong cùng một transaction (either both committed or neither). *(F5/F25/I3)*
2. THE SYSTEM SHALL chỉ cho use case gọi `IOutboxWriter.EnqueueAsync` (ghi outbox), KHÔNG cho gọi `IEventBusPublisher`/SDK trực tiếp. *(F25/I4/I8)*
3. WHEN `outbox_message` được tạo, THE SYSTEM SHALL đặt `processed_at = NULL` và gắn `correlation_id` (traceparent hiện hành). *(F25/F34)*
4. THE SYSTEM SHALL cung cấp worker `IOutboxDispatcher` publish message pending qua `IEventBusPublisher` (adapter) theo thứ tự `occurred_at`, đánh dấu `processed_at` khi publish thành công. *(F25/F33)*
5. WHEN publish một message thất bại, THE SYSTEM SHALL tăng `error_count` và đặt `next_attempt_at` theo exponential backoff; IF `error_count` vượt ngưỡng cấu hình, THEN THE SYSTEM SHALL đánh dấu message dead-letter (cách ly, không retry tự động, không chặn message khác). *(F25/F33)*
6. WHILE nhiều dispatcher instance chạy đồng thời, THE SYSTEM SHALL bảo đảm mỗi message pending được claim bởi tối đa một dispatcher tại một thời điểm (atomic claim); at-least-once vẫn được chấp nhận khi crash xảy ra sau publish nhưng trước khi mark processed. *(F25/F5)*

### Requirement 9: Inbox idempotency (consumer)

**User Story:** As a module developer, I want each integration event processed exactly once per consumer, so that redelivery does not cause double effects.

#### Acceptance Criteria
1. WHEN một message được giao, THE SYSTEM SHALL gọi `IInboxStore.TryMarkProcessedAsync(messageId, consumer)` trước khi chạy handler. *(F30)*
2. IF `(message_id, consumer)` đã xử lý trước đó, THEN THE SYSTEM SHALL bỏ qua handler (idempotent). *(F30)*
3. THE SYSTEM SHALL commit inbox mark + business trong cùng một transaction. *(F30)*
4. WHEN handler cần phát integration event mới trong lúc xử lý, THE SYSTEM SHALL yêu cầu đi qua `IOutboxWriter` trong cùng transaction inbox + business (không publish trực tiếp lên bus). *(F25/F30)*

### Requirement 10: Rotation refresh-token nguyên tử

**User Story:** As a platform maintainer, I want refresh-token rotation to be atomic and race-safe, so that concurrent refreshes don't lose tokens or allow reuse.

#### Acceptance Criteria
1. WHEN hai request refresh đồng thời dùng cùng token, THE SYSTEM SHALL để đúng MỘT request thành công và các request còn lại nhận `invalid_refresh_token`. *(F5)*
2. THE SYSTEM SHALL thực hiện consume-if-not-revoked bằng một câu update nguyên tử ở DB (`UPDATE ... WHERE id=@id AND revoked_at IS NULL`). *(F5)*
3. IF việc insert token mới thất bại sau khi consume, THEN THE SYSTEM SHALL rollback cả consume (không mất token). *(F5)*
4. WHEN một token đã revoked được dùng lại (reuse-detection), THE SYSTEM SHALL thu hồi toàn bộ family. *(review §2)*
5. THE SYSTEM SHALL đặt ràng buộc `UNIQUE` trên `token_hash` (hash SHA-256 của token 256-bit CSPRNG). *(F10)*

### Requirement 11: Concurrency token trung lập provider

**User Story:** As a platform maintainer, I want the kernel's concurrency abstraction to be provider-neutral, so that the core doesn't leak Postgres/xmin details.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp `IHasConcurrencyToken` (đổi tên từ `IConcurrencyAware`) không nhắc `xmin`/Npgsql trong kernel. *(F8)*
2. WHEN provider là PostgreSQL, THE SYSTEM SHALL map `RowVersion` tới cơ chế concurrency của provider tại tầng Infrastructure (không tại kernel). *(F8)*
3. WHEN `SaveChanges` gặp xung đột đồng thời, THE SYSTEM SHALL ném `ConcurrencyConflictException` trung lập để Api map HTTP 409 mà không phụ thuộc EF. *(F8)*

### Requirement 12: DI convention rõ ràng (override/duplicate/manual)

**User Story:** As a Host author, I want deterministic DI registration rules, so that duplicate implementations fail loudly instead of silent last-wins.

#### Acceptance Criteria
1. THE SYSTEM SHALL đăng ký port default của lõi bằng `TryAdd` để app override được. *(F18)*
2. IF một port single-implementation có >1 impl không cố ý, THEN THE SYSTEM SHALL fail-fast (duplicate-guard), không last-wins âm thầm. *(F18)*
3. THE SYSTEM SHALL đăng ký port multi-implementation (`IExternalAuthProvider`, `IIntegrationEventHandler<T>`) dưới dạng `IEnumerable<T>` có chủ đích + registry resolve theo tên/type. *(F18)*
4. THE SYSTEM SHALL dùng marker tường minh `IManualRegistration` để loại service hard-require DbContext khỏi auto-scan, thay cho quy ước namespace-string dễ vỡ. *(F6)*
5. THE SYSTEM SHALL cung cấp overload đăng ký nhận thêm assembly của app (`params Assembly[]`) để quét use case/validator ngoài assembly lõi. *(F6)*

### Requirement 13: Fail-fast khi thiếu port/config bắt buộc

**User Story:** As a Host author, I want the app to refuse to boot if a required port or config is missing, in every environment, so that misconfiguration is caught immediately.

#### Acceptance Criteria
1. WHEN host khởi động thiếu bất kỳ port bắt buộc nào trong danh sách tổng hợp (mỗi `AddXxxCore()` tự khai báo port bắt buộc của nhóm nó vào `StartupValidationOptions.RequiredPorts`), THE SYSTEM SHALL chặn boot với thông báo liệt kê ĐẦY ĐỦ các port thiếu, ở MỌI môi trường (kể cả Production). *(F7/F35/I9)*
2. THE SYSTEM SHALL bật `ValidateOnBuild`/`ValidateScopes = true` tường minh (không dựa mặc định chỉ-Development). *(F7)*
3. WHEN một options bắt buộc thiếu giá trị hợp lệ, THE SYSTEM SHALL validate-on-start và chặn boot. *(F35)*

### Requirement 14: Reverse proxy / ForwardedHeaders

**User Story:** As a Host author, I want configurable forwarded headers, so that client IP/scheme are correct behind a reverse proxy and rate-limit partitions by real IP.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp `ForwardedHeadersOptions` cấu hình được (KnownProxies/KnownNetworks) và áp `UseForwardedHeaders` sớm trong pipeline. *(F16)*
2. WHEN sau reverse proxy, THE SYSTEM SHALL phân vùng rate-limit theo client IP thật đã resolve, không theo IP proxy. *(F16)*

### Requirement 15: Cookie/CORS mode

**User Story:** As a Host author, I want an explicit cookie SameSite/CORS mode, so that same-site and cross-site SPA scenarios both work with correct CSRF strategy.

#### Acceptance Criteria
1. THE SYSTEM SHALL phơi options `CookieSameSiteMode` thay vì cứng `Strict`. *(F17)*
2. WHEN mode là same-site, THE SYSTEM SHALL dùng `Strict/Lax` (không cần CSRF token). *(F17)*
3. WHEN mode là cross-site, THE SYSTEM SHALL dùng `SameSite=None` kèm chiến lược CSRF (anti-forgery/double-submit) và siết CORS. *(F17)*

> **— Nhóm P1.5 — Đặt "ổ cắm" mở rộng (F24–F28) —**

### Requirement 16: Extension architecture (AddXxxCore / AddYyy)

**User Story:** As a Host author, I want a consistent `AddXxxCore()`/`AddYyyXxx(cfg)` pattern, so that enabling/disabling a technology is a one-line change without touching the core.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp cặp API: `AddXxxCore()` đăng ký port default/no-op + behavior (luôn gọi); `AddYyyXxx(cfg)` đăng ký adapter cụ thể (chỉ ở Host). *(F24/I2)*
2. WHEN Host bỏ dòng `AddYyyXxx(cfg)`, THE SYSTEM SHALL tắt tính năng đó mà không lỗi biên dịch ở use case. *(F24)*
3. WHEN thêm một adapter công nghệ mới, THE SYSTEM SHALL không yêu cầu sửa file nào trong `Bedrock.Application/Domain`. *(F24)*
4. IF một port optional bị gọi khi chưa có adapter nào đăng ký, THEN THE SYSTEM SHALL ném lỗi rõ ràng (fail-loud, ví dụ "No email adapter registered") thay vì no-op âm thầm; chỉ port có ngữ nghĩa degrade an toàn (ví dụ `IAppCache` miss-through) mới được default no-op. *(F18/F24)*

### Requirement 17: Messaging seam tự chặn dùng sai

**User Story:** As a platform maintainer, I want interface names that prevent misuse, so that developers cannot publish directly to the bus from a use case.

#### Acceptance Criteria
1. THE SYSTEM SHALL phơi cho tầng Application chỉ `IOutboxWriter` (application-facing); `IOutboxDispatcher`/`IEventBusPublisher` chỉ ở worker/adapter. *(F25/I8)*
2. THE SYSTEM SHALL định nghĩa `IntegrationEvent` với `EventType` (string ổn định) + `SchemaVersion` (int). *(F25/F32)*
3. THE SYSTEM SHALL cung cấp type registry ánh xạ `EventType` → CLR type (đăng ký từ các assembly `*.Contracts` tại Host) để consumer deserialize payload; WHEN gặp `EventType` chưa đăng ký, THE SYSTEM SHALL đưa message vào dead-letter thay vì crash consumer. *(F25/F32)*

### Requirement 18: Search projection port

**User Story:** As a module developer, I want search as a projection port (`ISearchIndex`/`ISearchQuery`), so that search is a read-model, not a replacement for the primary repository.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp `ISearchIndex<TDoc>` và `ISearchQuery<TDoc>` như port đọc/ghi index. *(F26)*
2. THE SYSTEM SHALL coi DB là source-of-truth và search là projection (eventual consistency), với alias/mapping version + poison-doc → dead-letter. *(F26)*

### Requirement 19: Port Email / Cache-split / Storage

**User Story:** As a module developer, I want narrow ports for email, cache, lock, idempotency, rate-limit and storage, so that abstractions don't leak and technologies are swappable.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp `IEmailSender` cho gửi email (adapter Gmail/SMTP/SendGrid). *(F28)*
2. THE SYSTEM SHALL tách cache thành các port hẹp: `IAppCache`, `IDistributedLock`, `IIdempotencyStore`, `IRateLimitStore` (không một interface gánh tất). *(F28)*
3. THE SYSTEM SHALL cung cấp `IFileStorage` cho lưu file (adapter S3/local). *(F28)*

### Requirement 20: External auth provider model

**User Story:** As a module/adapter developer, I want a provider-agnostic external-auth contract, so that adding Google/Zalo/Facebook is a new adapter without touching identity use cases.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp `IExternalAuthProvider` (Name, CreateChallengeAsync với state+PKCE+nonce+returnUrl-whitelist, CompleteAsync với verify + replay-protect) + `IExternalAuthProviderRegistry`. *(F27)*
2. THE SYSTEM SHALL chuẩn hóa `ExternalUserProfile` với `Email`/`EmailVerified` NULLABLE (không giả định provider nào cũng trả email). *(F27)*
3. THE SYSTEM SHALL đặt policy provider-user-id uniqueness/account-linking/email-trust ở `Identity.Application`, không ở lõi. *(F27/F3)*

> **— Nhóm P2 — Platform hệ lớn (F12/F13, F19–F23, F29–F35, F8/F9) —**

### Requirement 21: Data ownership & migration per-module

**User Story:** As a module developer, I want each module to own its schema and migrations, so that modules deploy independently without cross-module coupling.

#### Acceptance Criteria
1. THE SYSTEM SHALL cho mỗi module một `DbContext` + một schema riêng (ví dụ `identity`, `rooms`). *(F31/I6)*
2. THE SYSTEM SHALL cấm FK/JOIN chéo schema giữa các module. *(F31)*
3. THE SYSTEM SHALL cho mỗi module một migration history riêng để migrate độc lập. *(F31)*
4. WHEN cần dữ liệu module khác, THE SYSTEM SHALL lấy qua API nội bộ hoặc read-model projection từ integration event. *(F31)*

### Requirement 22: Versioning API & integration event

**User Story:** As a platform maintainer, I want HTTP and event versioning, so that consumers evolve safely without breaking.

#### Acceptance Criteria
1. THE SYSTEM SHALL hỗ trợ API versioning (Asp.Versioning) + OpenAPI group theo version + deprecation policy. *(F32)*
2. WHEN một integration event thêm field, THE SYSTEM SHALL giữ backward-compat bằng cách chỉ thêm field optional. *(F32)*
3. WHEN một thay đổi là breaking, THE SYSTEM SHALL tạo `EventType` mới (v2) chạy song song; consumer là tolerant reader (bỏ qua field lạ). *(F32)*

### Requirement 23: Resilience ở biên adapter

**User Story:** As an adapter developer, I want a standard resilience pipeline at the adapter boundary, so that a slow/dead dependency doesn't cascade.

#### Acceptance Criteria
1. THE SYSTEM SHALL áp resilience (timeout → retry exponential+jitter chỉ cho thao tác idempotent → circuit-breaker → fallback) ở biên adapter, KHÔNG ở use case/lõi. *(F33)*
2. THE SYSTEM SHALL cấu hình resilience per-adapter qua options. *(F33)*

### Requirement 24: Telemetry & correlation unity

**User Story:** As an operator, I want unified tracing/metrics/logs and one correlation id, so that I can correlate a request across header, error body, and traces.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp OpenTelemetry ba trụ (traces/metrics/logs) và propagate W3C `traceparent` xuyên HTTP + bus. *(F34)*
2. THE SYSTEM SHALL đảm bảo `X-Correlation-Id` (header) == `traceId` (ProblemDetails) == trace hiện hành cho mọi request. *(F21)*
3. THE SYSTEM SHALL phát metrics tối thiểu: request rate/latency/error, EF query time, rate-limit rejects, outbox lag (pending age) + dead-letter count, consumer processing time, external-auth success/fail. *(F34)*

### Requirement 25: Secrets & config governance

**User Story:** As a Host author, I want secrets kept out of the repo and required config validated at start, so that misconfiguration and secret leakage are prevented.

#### Acceptance Criteria
1. THE SYSTEM SHALL không commit secret; dev dùng User-Secrets, prod dùng env/Key Vault/SOPS; `appsettings.json` chỉ non-secret + placeholder. *(F35)*
2. THE SYSTEM SHALL validate-on-start mọi options bắt buộc (kết hợp Requirement 13). *(F35/F7)*

### Requirement 26: Auth mechanism vs policy + permission-based authz

**User Story:** As a module developer, I want permission-based authorization with mechanism/policy separation, so that the core stays role-agnostic while modules define their own policies.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp `ICurrentUser` với `UserId`, `Roles`, `Permissions`, `TenantId?`, `SessionId?`, `IsInRole`, `HasPermission`. *(F23)*
2. THE SYSTEM SHALL để role/permission cụ thể được khai ở module Identity + Host, không hardcode trong lõi. *(F3/F23)*
3. THE SYSTEM SHALL hỗ trợ policy theo permission claim (role là một nguồn suy ra permission). *(F23)*

### Requirement 27: JWT key-ring (rotation)

**User Story:** As a platform maintainer, I want JWT signing key rotation, so that rotating keys doesn't invalidate all live tokens.

#### Acceptance Criteria
1. THE SYSTEM SHALL gắn `kid` vào header token và ký bằng active key. *(F22)*
2. THE SYSTEM SHALL verify bằng active + previous keys (rolling rotation). *(F22)*
3. THE SYSTEM SHALL cho phép cân nhắc asymmetric (RS256/ES256) khi nhiều service verify. *(F22)*

### Requirement 28: Persistence entity ẩn khỏi Application

**User Story:** As a platform maintainer, I want persistence models hidden inside Infrastructure, so that Application talks in business terms, not storage shapes.

#### Acceptance Criteria
1. THE SYSTEM SHALL giấu `RefreshTokenRecord` (persistence-facing) trong Infrastructure. *(F19)*
2. THE SYSTEM SHALL để Application thao tác qua port nghiệp vụ (`RotateAsync`/`RevokeFamilyAsync`/`GetActiveTokenAsync`), không lộ shape lưu trữ. *(F19)*

### Requirement 29: Hợp đồng error code ổn định + localization app-layer

**User Story:** As a frontend/consumer developer, I want stable machine error codes with neutral default messages, so that I can localize by code without depending on server language.

#### Acceptance Criteria
1. THE SYSTEM SHALL đảm bảo mọi `Error.Code` là chuỗi ổn định machine-readable. *(F20)*
2. THE SYSTEM SHALL dùng message mặc định English trung lập (developer message); KHÔNG hardcode message người-dùng theo ngôn ngữ trong lõi. *(F20)*
3. THE SYSTEM SHALL đặt `ProblemDetails.title` = developer/neutral message và mang `code` (machine) + `traceId` (= correlationId); UI dịch theo `code`. *(F20/F21)*

### Requirement 30: Semantic folder & naming

**User Story:** As a platform maintainer, I want intention-revealing folder/namespace names, so that the structure scales without "junk drawer" folders.

#### Acceptance Criteria
1. THE SYSTEM SHALL dùng `Application/Ports` (thay `Abstractions`), tách `UseCases`/`Validation`/`Paging` (thay `Common`). *(F12)*
2. THE SYSTEM SHALL dùng `IClock` (thay `IDateTimeProvider`) và `IHasConcurrencyToken` (thay `IConcurrencyAware`). *(F12/F8)*
3. THE SYSTEM SHALL tách `Api/Authentication` + `Api/HttpSecurity` và `Infrastructure/Cryptography|Tokens|Sanitization` (hết chồng nghĩa). *(F12)*

> **— Nhóm Chất lượng — Quality gate & testing (I10, F11) —**

### Requirement 31: Quality gate — build sạch + kiến trúc test làm lưới

**User Story:** As a platform maintainer, I want every change to keep a clean build and pass architecture tests, so that boundaries never silently erode.

#### Acceptance Criteria
1. THE SYSTEM SHALL bật `TreatWarningsAsErrors=true`; mỗi lát thay đổi phải build 0 warning + test xanh. *(I10)*
2. THE SYSTEM SHALL có architecture tests (NetArchTest) cho dependency matrix + module boundary + no-business-in-core + naming, mỗi test kèm negative control. *(review §2/§14)*

### Requirement 32: Chiến lược test đa tầng (kể cả Postgres thật)

**User Story:** As a platform maintainer, I want multi-layered tests including real Postgres, so that provider-specific behaviors (xmin, partial index, race) are verified.

#### Acceptance Criteria
1. THE SYSTEM SHALL có unit test (domain rules + use case với fakes). *(F11)*
2. THE SYSTEM SHALL có integration test provider-agnostic (SQLite) + Testcontainers/PostgreSQL cho Postgres-specific (xmin→409, partial unique index, migration Npgsql, race rotation đa-connection). *(F11)*
3. THE SYSTEM SHALL có adapter integration test (Testcontainers cho RabbitMQ/Elastic/Redis; WireMock cho external-auth) không đụng lõi. *(F29)*
4. THE SYSTEM SHALL có contract test: snapshot registry `Error.Code` (reflection trên assembly lõi/module — phát hiện code bị đổi/mất) + snapshot schema integration-event (chống breaking vô ý); đồng bộ FE (khi FE tồn tại) thực hiện qua artifact export từ registry này, ngoài phạm vi base. *(F20/F32)*

> **— Nhóm bổ sung (chốt khi tinh chỉnh thiết kế) — thuộc scope P1 (F13, F24) —**

### Requirement 33: Domain events in-process (dispatch trước commit)

**User Story:** As a module developer, I want domain events raised by entities to be dispatched in-process within the same transaction, so that side-effects stay atomic with state and domain events can become integration events reliably.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp `IDomainEventDispatcher` dispatch domain events của các entity được track TRƯỚC khi commit, trong cùng SaveChanges/transaction. *(F13/I3)*
2. WHEN một domain event handler ném exception, THE SYSTEM SHALL rollback toàn bộ (state + hiệu ứng handler) — không commit một phần. *(F13/I3)*
3. THE SYSTEM SHALL cho phép handler chuyển domain event thành integration event qua `IOutboxWriter` trong cùng transaction (domain event → outbox → bus). *(F13/F25)*
4. THE SYSTEM SHALL giới hạn số vòng dispatch (max-depth cấu hình được) để chặn vòng lặp vô hạn khi handler phát sinh event mới. *(F13)*

### Requirement 34: Health checks (liveness/readiness)

**User Story:** As an operator, I want separate liveness and readiness endpoints with pluggable checks, so that orchestrators restart/route traffic correctly without the core knowing any technology.

#### Acceptance Criteria
1. THE SYSTEM SHALL cung cấp cơ chế health check với hai endpoint tách biệt: liveness (không phụ thuộc hạ tầng ngoài) và readiness (gồm check DB khi persistence bật). *(F24 — phần kỹ thuật giữ nguyên từ foundation, Blueprint tổng quan)*
2. WHEN một dependency bắt buộc không sẵn sàng, THE SYSTEM SHALL trả unhealthy (HTTP 503) ở readiness trong khi liveness vẫn healthy. *(F24)*
3. THE SYSTEM SHALL cho phép module/adapter đăng ký health check riêng qua extension mà không sửa lõi. *(F24/I2)*

## Traceability — Requirement → Finding/Invariant

| Req | Chủ đề | Finding/Invariant |
|---|---|---|
| 1 | Domain-agnostic core | I1, F2, F3, F4 |
| 2 | Tách Host/library | F1, I2 |
| 3 | Log path masking | F15 |
| 4 | Api ⊥ Infrastructure | F14, I7 |
| 5 | Adapter isolation | I2, F29 |
| 6 | Module boundary | F30, I5 |
| 7 | UoW + giao dịch tường minh (reentrancy) + no leaky IQueryable | I3, F9, F5 |
| 8 | Outbox atomicity + dispatcher claim/backoff/dead-letter | F5, F25, I3, I4, I8, F33 |
| 9 | Inbox idempotency | F30, F25 |
| 10 | Rotation atomicity | F5, F10 |
| 11 | Concurrency token trung lập | F8 |
| 12 | DI convention | F6, F18 |
| 13 | Fail-fast port/config | F7, F35, I9 |
| 14 | ForwardedHeaders | F16 |
| 15 | Cookie/CORS mode | F17 |
| 16 | Extension architecture + fail-loud optional port | F24, I2, F18 |
| 17 | Messaging seam naming + type registry | F25, I8, F32 |
| 18 | Search projection | F26 |
| 19 | Email/Cache/Storage ports | F28 |
| 20 | External auth model | F27, F3 |
| 21 | Data ownership/migration | F31, I6 |
| 22 | Versioning | F32 |
| 23 | Resilience | F33 |
| 24 | Telemetry + correlation | F34, F21 |
| 25 | Secrets governance | F35, F7 |
| 26 | Permission authz | F23, F3 |
| 27 | JWT key-ring | F22 |
| 28 | Persistence entity ẩn | F19 |
| 29 | Error code contract | F20, F21 |
| 30 | Semantic folder/naming | F12, F8 |
| 31 | Quality gate | I10 |
| 32 | Testing đa tầng | F11, F29 |
| 33 | Domain events in-process | F13, I3, F25 |
| 34 | Health checks | F24, I2 |
