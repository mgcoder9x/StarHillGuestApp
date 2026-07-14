# Module design — GuestAccess (Wave C)

> **Design đã được review và triển khai theo lát cắt C-GA.0..3; phần dưới là baseline có hiệu lực cho code hiện tại
> và các lát cắt tiếp theo.** Tài liệu này thuộc spec hiện hữu `starhill-qr`, chi tiết hóa `../design.md` §4.4.
> WHAT: `docs/resort-qr-portal/requirements.md` Req 1, 10, 11.2/11.5/11.6, 14.2; legacy tham khảo:
> `resort-qr/` tại commit `c7622a5`. Mọi kết luận dưới đây dựa trên file/code/history/lệnh test đã kiểm.

## 0. Đối soát nguồn và phạm vi

- `docs/resort-qr-portal/tasks.md` Task 5 `[x]/[~]` mô tả implementation **legacy monolith `resort-qr/`**;
  nó không phải tracker của product tree mới.
- Product tree hiện đã có đủ 5 project GuestAccess trong `starhill/src/Modules/GuestAccess/`; C-GA.1..3 đã triển khai.
- Source legacy hợp lệ nằm ở root `resort-qr/`, không phải nested stale copy. Logic được port có chọn lọc;
  không copy `SharedKernel`, monolithic UoW, FK chéo module, route hay race handling cũ.
- Increment hiện hành gồm module 5-project, persistence `guest_access`, resolve use case, cookie và endpoint public.
  Portal middleware/context, sweeper, staff-close và cascade vẫn defer theo dependency ở C-GA.4/5.

## 1. Mục tiêu và bất biến

1. Token QR chỉ được phân giải qua `Rooms.Contracts.IRoomTokenResolver`; GuestAccess không đọc RoomsDbContext.
2. Resort/config/ngôn ngữ chỉ đọc qua `ResortConfig.Contracts`; không FK hoặc repository chéo schema.
3. Raw guest-session key chỉ tồn tại ở cookie/request memory; DB chỉ lưu SHA-256 hash 64 hex ký tự.
4. Mỗi `(GuestSessionId, RoomId)` tối đa một `GuestVisit` Active, enforce bằng partial unique index PostgreSQL.
5. Resolve của cùng cookie+room phải hội tụ về cùng VisitId dưới concurrency thật.
6. Portal window được kiểm **trước** touch; request hết cửa sổ không được tự hồi sinh cửa sổ.
7. GuestVisit kết thúc phải chặn guest write ngay từ GuestAccess; cleanup module khác dùng outbox at-least-once.
8. Capability token/session key không xuất hiện trong response, ProblemDetails, structured log, trace tag hay access-log API.

## 2. Cấu trúc module và dependency

```text
starhill/src/Modules/GuestAccess/
  GuestAccess.Domain         -> Bedrock.Domain
  GuestAccess.Contracts      -> Bedrock.Messaging.Contracts
  GuestAccess.Application    -> Domain + Contracts + Rooms.Contracts + ResortConfig.Contracts + Bedrock.Application
  GuestAccess.Infrastructure -> Application + Bedrock.Infrastructure + EF Core/Npgsql
  GuestAccess.Api            -> Application + Bedrock.Api
```

Module key và schema cùng một nguồn: `GuestAccessModule.PersistenceKey = "guest_access"`. Infrastructure dùng
`AddBedrockPersistence<GuestAccessDbContext>(key, ...)` và keyed repository/UoW. Api không reference Infrastructure;
Host là composition root. Cross-module chỉ `.Contracts` và Guid trần.

## 3. Domain model và invariants

### GuestSession

- `Id`, `SessionKeyHash`, `PreferredLanguage?`, `FirstSeenAt`, `LastSeenAt`.
- Session là định danh thiết bị toàn deployment, không gắn ResortId; GuestVisit mới mang ResortId/RoomId.
- `SessionKeyHash` required, max 64, unique `ux_guest_session_key_hash`.
- Không lưu raw key, user-agent, IP hoặc fingerprint ở slice này; tránh dữ liệu nhận dạng không cần thiết.

### GuestVisit

- `Id`, `ResortId`, `RoomId`, `GuestSessionId`, `Status` (`Active|Closed|Expired`), `StartedAt`,
  `LastSeenAt`, `ExpiresAt`, `ClosedAt?`, `ClosedByUserId?`.
- FK nội module `GuestVisit.GuestSessionId -> GuestSession.Id` với delete Restrict.
- `ResortId` và `RoomId` là Guid trần; tuyệt đối không FK sang `resort_config`/`rooms`.
- Partial unique `ux_guest_visit_active(guest_session_id, room_id) WHERE status='Active'`.
- Sweep index `ix_guest_visit_active_expiry(expires_at) WHERE status='Active'`.
- Check constraints: `expires_at >= last_seen_at`; Active có `closed_at IS NULL`; Closed/Expired có `closed_at IS NOT NULL`.
- Chuyển trạng thái một chiều: Active -> Closed hoặc Active -> Expired; không reopen visit đã kết thúc.

`LastSeenAt` là hoạt động guest gần nhất được chấp nhận; `ExpiresAt = LastSeenAt + VisitIdleExpiryHours`.
Portal window không phải cookie expiry và không phải visit expiry.

## 4. Cookie và session credential

- Raw key: `ITokenGenerator.NewToken()` mặc định 32 byte CSPRNG/base64url, nên canonical credential là **đúng 43
  ký tự ASCII `[A-Za-z0-9_-]`**. Rooms QR token và GuestSession key cùng dùng generator này; guard dùng cùng invariant.
  SHA-256 deterministic để lookup; KDF chậm không tăng giá trị vì entropy đầu vào đã cao.
- Cookie mặc định `__Host-starhill_guest`, 60 ngày (nằm trong Req 11.2: 30–90 ngày).
- Thuộc tính bắt buộc: `HttpOnly=true`, `Secure=true`, `SameSite=Lax`, `Path=/`, không `Domain`, `IsEssential=true`.
  Prefix `__Host-` buộc browser giữ Secure/Path=/ và chống subdomain ghi đè cookie.
- Cookie null/lạ/sai canonical format (kể cả whitespace, ký tự ngoài base64url hoặc khác 43 ký tự) được chuẩn hóa
  thành “chưa có session” **trước SHA-256/DB lookup**; hash canonical nhưng không tồn tại cũng cấp session mới.
- Chỉ append cookie sau khi transaction critical-section commit; dùng `IClock.UtcNow` để tính `Expires`.
- Không refresh expiry ở mọi request và không echo key trong JSON/header/log. Rotation/retention session để policy riêng sau MVP.
- `GuestAccessOptions` ValidateOnStart: tên `__Host-`, `SessionCookieDays` trong `[30,90]`; HTTPS posture không thể tắt.

## 5. Cross-module read contract

`IRoomTokenResolver` hiện có đủ cho RoomId/ResortId/RoomNumber/location/IsRoomActive. `null` cố ý gộp token
không tồn tại, revoked và room soft-deleted. GuestAccess trả cùng mã public `qr_invalid` cho nhóm này; phòng được
resolve nhưng không Active trả `room_inactive`, không kèm room metadata.

`IResortSettingsQuery.GetAsync()` hiện là single-resort và thiếu tên/logo/ngôn ngữ. Không mở rộng nó thành DTO
đa trách nhiệm. Đã thêm purpose-built `ResortConfig.Contracts.IResortGuestConfigQuery.GetAsync(Guid resortId)`
(✅ C-GA.2a — QR-N-025) trả:

- ResortId, Name, LogoUrl;
- enabled language codes (theo SortOrder) + exactly one default language;
- toàn bộ feature flags cần resolve;
- PortalWindowMinutes và VisitIdleExpiryHours.

Query bắt buộc theo `room.ResortId`; null/mismatch hoặc cấu hình không hợp lệ -> `configuration_unavailable` và
không tạo/touch session/visit. Không silently bật feature bằng default `true` khi dữ liệu nền mất. Impl
`EfResortGuestConfigQuery` fail-closed khi thiếu resort/settings/default-language; verify SQLite local (3 test).

## 6. Resolve flow và thứ tự transaction

`ResolveTokenUseCase : IUseCase<ResolveTokenInput, ResolveTokenResult>` **không** khai `ITransactionalUseCase`.
Đây là lựa chọn đã triển khai ở C-GA.2b: use case resolve keyed `IUnitOfWork("guest_access")` và tự mở **một
transaction hẹp** chỉ quanh session/visit critical section; các read cross-module không giữ transaction/row lock.

1. Kiểm raw QR token canonical: đúng 43 ký tự base64url; sai -> `qr_invalid` trước resolver/DB, không log input.
2. Gọi `IRoomTokenResolver.ResolveActiveTokenAsync`; null -> `qr_invalid`; inactive -> `room_inactive`.
3. Gọi `IResortGuestConfigQuery.GetAsync(room.ResortId)`; fail-closed nếu thiếu/mismatch.
4. Chuẩn hóa cookie: chỉ credential canonical mới được hash/lookup; malformed được coi như session mới.
5. Mở transaction GuestAccess. Session đã có được đọc `FOR UPDATE`; session mới insert/save trước visit.
6. Dưới cùng session-row lock, đọc Active visit cho `(session, room)`:
   - còn hạn: touch `LastSeenAt=now`, `ExpiresAt=now+idle`;
   - đã quá hạn: update Expired + `ClosedAt=now` và **SaveChanges trước**, sau đó insert visit mới;
   - không có: insert visit mới.
7. Save/commit rồi trả DTO. `PortalWindowExpiresAt = now + PortalWindowMinutes`; Api chỉ append raw key sau success.

Không dùng cách legacy “insert -> catch 23505 -> query lại cùng DbContext”. PostgreSQL đánh dấu transaction aborted
sau unique violation cho tới rollback. Khóa hàng session loại TOCTOU trước partial unique; index vẫn là defense cuối.
Nếu defense cuối nổ, toàn critical section rollback; không tiếp tục trên context hỏng.

**Cơ chế row-lock đã kiểm chứng:** `EfGuestSessionStore.FindByKeyHashForUpdateAsync` dùng SQL model-driven,
parameterized `SELECT ... LIMIT 1 FOR UPDATE` trên Npgsql (không `SKIP LOCKED` vì request sau phải chờ để hội tụ);
SQLite fallback chỉ phục vụ test logic đơn luồng. `IUnitOfWork.ExecuteInTransactionAsync` giữ lock tới commit. Cách này
thay thế mô tả cũ dùng `ICommandUseCase`/transaction decorator: decorator sẽ làm transaction bao cả Rooms/Config read,
tăng thời gian giữ transaction mà không tăng atomicity xuyên DbContext.

### Concurrency boundary

- `FOR UPDATE` phải nằm trong adapter/store Infrastructure; Application chỉ phụ thuộc port GuestAccess của chính module.
- Khóa theo GuestSession serialize các resolve cùng thiết bị (kể cả hai phòng), chi phí chấp nhận được ở tải resort.
- Hai request đầu tiên cùng browser nhưng chưa có cookie có thể sinh hai session khác nhau; client Guest Web phải single-flight
  resolve. Đây không phá isolation/an toàn; session thừa chỉ là dữ liệu cần retention cleanup sau này.
- Test PostgreSQL thật phải chứng minh nhiều request cùng cookie+room trả đúng một Active row và cùng VisitId.

## 7. HTTP contract và bảo mật endpoint public

### Route

Physical QR vẫn mở Guest Web `GET /r/{token}`. API resolve dùng:

```http
POST /v1/guest/resolve
Content-Type: application/json
Cache-Control: no-store

{"token":"<opaque capability>"}
```

Lý do đổi từ legacy `GET .../{token}`: resolve tạo/touch session+visit nên không phải safe GET; token trong path dễ lọt
access log, proxy, APM và trace. Guest Web phải lấy token từ route, lập tức `history.replaceState` để scrub URL rồi POST
body. Reverse proxy/static-host vẫn phải redact/tắt access log cho `/r/*` vì request đầu tiên bắt buộc chứa token.
Không giữ GET alias khi chưa có compatibility requirement.

Endpoint `AllowAnonymous`. Biên ứng dụng từ Bedrock là global fixed-window limiter theo `RemoteIpAddress` sau
trusted ForwardedHeaders; không thêm named limiter với ngưỡng tùy tiện khi chưa có traffic/NAT budget (QR-TO-007).
Kestrel endpoint giới hạn body resolve 1 KiB và Application chỉ chấp nhận token canonical 43 ký tự **trước resolver/DB**.
Hai lớp xử lý hai rủi ro khác nhau: request-size chặn allocation/body abuse; canonical guard chặn lookup/hash vô ích.
Không dùng raw token làm partition key/log key. Named policy chỉ được thêm khi có SLO + dữ liệu shared-NAT thực tế.

### Success response

```json
{
  "room": { "id": "...", "number": "A-203", "building": "A", "floor": 2 },
  "resort": { "id": "...", "name": "Star Hill", "logoUrl": null },
  "languages": ["en", "vi", "ko", "zh"],
  "defaultLanguage": "en",
  "visit": { "id": "...", "portalWindowExpiresAt": "..." },
  "features": {
    "faqEnabled": true, "chatEnabled": true, "housekeepingEnabled": true,
    "ruleAckRequiredForFaq": true, "ruleAckRequiredForChat": true,
    "ruleAckRequiredForHousekeeping": true
  }
}
```

Không trả session key/hash, QR token, ExpiresAt idle, internal status hoặc token reason. Rules version/ack chưa có module
Rules nên không trả field giả; bổ sung additive khi Rules contract tồn tại.

### Failure posture

- `qr_invalid`: token unknown/revoked/soft-deleted; cùng status/shape, không room metadata.
- `room_inactive`: token resolve được nhưng room không hoạt động; không room metadata.
- `configuration_unavailable`: dữ liệu nền thiếu/mismatch; `ErrorType.Failure` -> HTTP 500 theo mapping hiện có `ErrorTypeToHttp` (base chưa có 503).
- Không set/touch cookie/session/visit khi bước room/config fail.
- Response `Cache-Control: no-store`; mọi lỗi chỉ qua `ProblemDetailsBuilder`; cancellation truyền xuyên suốt.
- Guard capture log/trace phải xác nhận raw token và raw cookie không xuất hiện.

## 8. Portal window, lazy expiry và current guest context

Resolve luôn refresh portal window. Với endpoint guest tương tác tương lai:

1. Xác thực cookie -> session -> Active visit.
2. Nếu `now > ExpiresAt`: chuyển Expired; trả `session_expired`; không xử lý nghiệp vụ.
3. Nếu `now - LastSeenAt > PortalWindowMinutes`: trả `session_expired`; **không touch**.
4. Nếu hợp lệ: thực hiện nghiệp vụ; chỉ sau thành công mới touch LastSeenAt/ExpiresAt.

Không để Faq/Rules/Concierge/Housekeeping tự đọc cookie/hash/entity GuestAccess. Khi consumer đầu tiên (Rules) xuất hiện,
GuestAccess.Contracts sẽ lộ current-context DTO/port tối thiểu và Host/GuestAccess adapter chịu trách nhiệm xác thực.
Không khóa sớm shape interface chưa có consumer.

Background sweeper là correctness/cleanup bổ sung, không thay lazy expiry: batch Active có `ExpiresAt < now`, dùng
PostgreSQL locking phù hợp multi-instance (`FOR UPDATE SKIP LOCKED`), chuyển Expired idempotently. Batch size/interval là
options ValidateOnStart. Slice resolve hoạt động đúng ngay cả khi sweeper dừng.

## 9. Kết thúc visit và cascade liên module

Staff-close, sweeper và lazy-expiry đều kết thúc visit trong transaction GuestAccess. Guest write bị chặn ngay bằng
status GuestAccess, không chờ module khác cleanup. Khi Concierge/Housekeeping tồn tại, cùng transaction kết thúc visit sẽ
ghi `GuestVisitEndedIntegrationEvent` vào outbox GuestAccess; consumer inbox idempotent đóng conversation và cancel ticket.

Đây là **at-least-once + eventual consistency**, không exactly-once và không giả vờ atomic xuyên nhiều DbContext. Một
DI scope không tạo transaction chung cho các DbContext; lựa chọn synchronous cũ trong `design.md` được supersede ở phần
cascade. Initial resolve slice chưa map outbox/dispatcher cho tới khi event có consumer thật.

## 10. Persistence và Host wiring

- `GuestAccessDbContext : PlatformDbContext`, default schema `guest_access`; migration chain và
  `__EFMigrationsHistory` thuộc schema module. Mỗi Identity/ResortConfig/Rooms/GuestAccess phải cấu hình cùng history
  schema ở **runtime Host, integration setup và design-time factory** để migration bundle không lệch runtime (QR-AD-028).
- Chuyển từ ledger chung `public.__EFMigrationsHistory` phải dùng script copy idempotent theo MigrationId trước khi
  khởi động binary mới; không xóa ledger public trong cùng rollout để rollback còn đọc được. Fresh DB tạo ledger per-schema.
- Initial resolve không map Outbox/Inbox. Khi cascade event được triển khai, thêm outbox capability, dispatcher keyed
  và startup guard trong cùng increment (không tạo bảng/worker chưa dùng).
- Repositories/stores/UoW resolve bằng `GuestAccessModule.PersistenceKey`; use case đăng ký factory thủ công.
- `Sha256GuestSessionKeyHasher` singleton; custom session/visit store scoped, inject concrete GuestAccessDbContext.
- Host thêm `ConnectionStrings:GuestAccess` cùng DB `starhill`, compose override, migrate gated, health readiness.
- Thêm 5 project + test project vào `starhill/Platform.slnx`; migration bundle GuestAccess vào CI khi migration có.

## 11. Correctness properties và guard tests

### Không Docker

- Unit flow: malformed/noncanonical token dừng trước resolver/DB; null/inactive/config missing không ghi DB; new
  session/visit; reconnect; malformed cookie thành session mới mà không hash/lookup; lazy-expire tạo visit mới;
  portal expiry tính đúng; feature/language DTO; raw key chỉ internal result.
- Hash: deterministic 64 lowercase hex, raw khác hash; canonical credential đúng 43 base64url.
- HTTP/TestServer: endpoint anonymous; POST success; GET-path không tồn tại; cookie đủ `__Host-` attributes; no-store;
  failure không set cookie; stable ProblemDetails; body limit metadata 1 KiB; response shape nested được snapshot.
- Log capture end-to-end endpoint + `RequestLoggingMiddleware` + `LoggingUseCaseDecorator`: raw QR token/raw cookie
  không xuất hiện; generic Bedrock logger vẫn giữ method/masked path/use-case type/error code.
- Architecture: 5-project boundary, cross-module chỉ Rooms.Contracts/ResortConfig.Contracts, keyed persistence,
  Api không reference Infrastructure, module có trong solution.
- Migration ledger: DB sạch có đúng một `__EFMigrationsHistory` trong mỗi schema module; script chuyển ledger chung
  idempotent và không mất MigrationId.

### PostgreSQL Testcontainers bắt buộc

- Migration/model parity; schema/index/filter/check constraints đúng.
- `ux_guest_session_key_hash` và `ux_guest_visit_active` thật.
- N request đồng thời cùng cookie+room -> một Active row + cùng VisitId, không 500/23505.
- Active expired -> update Expired được flush trước insert mới; lịch sử giữ nguyên.
- Resolve hai room cùng session -> hai visit Active độc lập.
- Sweeper multi-worker `SKIP LOCKED` không xử lý trùng khi slice đó được triển khai.

Map CP: CP1 (resolve không lộ), CP6 (visit isolation), CP9 (reconnect/window/end), Req 10.1–10.8. Docker daemon
phải có Server version; không chấp nhận “skip mềm” làm bằng chứng cuối cho race/index.

## 12. Build slices và cổng dừng

1. **C-GA.0 — design/reconciliation (file này):** journal + diagnostics; chưa code. ✅ XONG.
2. **C-GA.1 — Domain/Contracts/Persistence:** entities, keyed DbContext, migration, DB constraints, boundary + Postgres constraint tests. ✅ XONG (QR-N-024): build 0-warning; `dotnet test Platform.slnx` 114/0-fail/0-skip với Docker.
3. **C-GA.2 — Resolve Application:** cross-module guest-config query, hasher, transaction/row-lock flow, unit + PostgreSQL race.
   - **C-GA.2a** ✅ XONG (QR-N-025): `IResortGuestConfigQuery` + `EfResortGuestConfigQuery` fail-closed + đăng ký + 3 test SQLite; build 0-warning; ResortConfig.IntegrationTests 15/0-fail.
   - **C-GA.2b** ✅ XONG (QR-N-026): `GuestAccess.Application` (hasher/errors/`ResolveTokenUseCase`/`IGuestSessionStore`) + Infrastructure (`EfGuestSessionStore` FOR UPDATE + SHA-256 hasher + factory DI) + ErrorCodeSnapshot cập nhật (3 code) + 6 unit SQLite + **1 race PostgreSQL thật** (8 request cùng cookie+room → một Active/cùng VisitId, 0 fail). `dotnet test Platform.slnx` 124/0-fail/0-skip Docker.
4. **C-GA.3 — Public API/Host:** POST resolve, cookie/options/rate limit/log redaction, Host/compose/CI bundle, HTTP tests. ✅ XONG (QR-N-027): `GuestAccess.Api` (`POST /v1/guest/resolve` AllowAnonymous + cookie `__Host-` + no-store) + Host wiring (connection string + migrate) + compose + CI bundle + smoke + 4 HTTP TestServer test. Rate-limit biên = Bedrock.Api global limiter (F16) áp sẵn. `dotnet test Platform.slnx` 128/0-fail/0-skip Docker.
5. **C-GA.3a — reconciliation/hardening:** ✅ XONG (QR-N-028): canonical credential guard trước resolver/hash; malformed
   cookie→session mới; request body 1 KiB (Kestrel enforce thật → 413 qua Compose); nested response contract + snapshot;
   log-capture regression pipeline thật; per-schema migration ledger + transition SQL idempotent (verify upgrade path đầu-cuối).
   Build 0-warning; full test 0-fail/0-skip Docker; diagnostics 0; JournalConsistency 5/5.
6. **C-GA.4 — Current-context + sweeper:** chỉ khi endpoint guest consumer đầu tiên cần; lazy expiry vẫn có từ C-GA.2.
7. **C-GA.5 — Staff close + outbox cascade:** chỉ khi Concierge và Housekeeping contracts/consumers tồn tại.

Mỗi slice dừng nếu build warning/error, journal INV-1..5 fail, migration model drift, Docker race test fail hoặc raw secret
xuất hiện trong log. Không triển khai Rules giả, cascade giả hay query trực tiếp DbContext module khác để “đủ response”.

## 13. Quyết định/trade-off liên quan

- QR-AD-024: boundary/config/error posture GuestAccess.
- QR-AD-025 + QR-DV-006: POST-body resolve + `__Host-` cookie/no-secret logging.
- QR-AD-026: session-row lock thay catch-query sau 23505.
- QR-AD-027 + QR-TO-006: outbox at-least-once supersede cascade synchronous xuyên DbContext.
- QR-DV-005: checkbox Task 5 legacy không phải trạng thái port active.
- QR-TO-005: device session global + cookie 60 ngày, không fingerprint.
- QR-TO-007: giữ global IP limiter đã kiểm chứng; defer named resolve limiter tới khi có SLO/shared-NAT budget.
- QR-AD-028: migration history per-schema + transition idempotent từ ledger public chung.

## 14. Self-validation và reconciliation

- [x] Source active/legacy được phân biệt bằng tree + Git history; nested stale không được dùng.
- [x] Dependency thật `IRoomTokenResolver`/`IResortGuestConfigQuery` đã đọc; guest config fail-closed.
- [x] Implementation transaction hẹp đã reconcile vào design; không còn mô tả sai `ITransactionalUseCase`.
- [x] Cookie/token canonical format lấy từ `CryptoTokenGenerator` + `RoomTokenFactory`, không chọn tùy tiện.
- [x] Portal window và idle expiry tách ngữ nghĩa; thứ tự check-before-touch rõ.
- [x] Cross-DbContext cascade không tuyên bố atomic; semantics at-least-once ghi rõ.
- [x] Response nested trong design là contract có hiệu lực; implementation C-GA.3a phải khớp và có guard.
- [x] Global-vs-named limiter được ghi trade-off, không tuyên bố named policy chưa tồn tại.
- [x] C-GA.3a build/test/Compose/DB/log-capture + diagnostics + JournalConsistency INV-1..5 xanh sau triển khai (QR-N-028).
