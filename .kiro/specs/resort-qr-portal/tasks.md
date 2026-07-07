# Implementation Plan

## Overview

> **Nguồn chân lý thiết kế:** mỗi task trỏ tới file authoritative trong `design/backend/**` và `design/frontend/**`. Requirement truy vết theo `requirements.md` (20 requirements). Con số cấu hình theo `design/backend/15-configuration-and-options.md` (mặc định — DEC-009), phiên bản theo `design/technology-stack.md` (pin khi implement — TK-005/018).
>
> **Chiến lược thực thi (đã chốt với user):** dựng **nền ngang** trước (skeleton → SharedKernel → Infrastructure → cross-cutting → module nền), rồi ráp **lát cắt dọc E2E backbone** (admin tạo phòng → QR → guest resolve). KHÔNG dựng BE+FE song song từ số 0 cho phần feature: **backend contract-first → sinh `shared-types` từ OpenAPI → FE tiêu thụ** (DEC-029), tránh lệch hợp đồng.
>
> **Migration:** incremental per-wave — migration NỀN chỉ chứa entity nền; module thêm bảng khi hiện thực (`04` §4, Req 8.6–8.7). KHÔNG dựng bảng "chết".
>
> **Định luật xuyên suốt:** mọi mã lỗi ∈ catalog `14-error-catalog.md`; mọi bất biến enforce ở DB + domain (correctness-by-construction); test dùng PostgreSQL thật (Testcontainers), KHÔNG InMemory cho ràng buộc.

## Task Dependency Graph

Các wave chạy tuần tự theo phụ thuộc; trong một wave nhiều task có thể song song. Mũi tên = "phải xong trước".

```
Wave 0 (skeleton BE #1,#2 ∥ FE #3)
   └─> Wave 1 SharedKernel (#4→#5→#6→#7, #8 test)
          └─> Wave 2 Infrastructure EF (#9→#10→#11→#12→#13, #14 test)
                 └─> Wave 3 Cross-cutting (#15,#16,#17→#18, #19, #20)   [#17 trước #18: cần GuestCookieRead trước rate limiter]
                        └─> Wave 4 Module nền (#21 Identity, #22 Settings/Localization, #23 Rooms&QR, #24 GuestAccess→#25 Sweeper)
                               └─> Wave 5 SignalR (#26→#27)   [cần GuestAccess/EnforcePortalWindow của #24]
                                      └─> Wave 6 Extension points (#28→#29)
                                             └─> Wave 7 Frontend (#30→#31→{#32 guest, #33 admin})   [#30 cần OpenAPI từ Wave 3/4]
                                                    └─> Wave 8 E2E backbone + kiểm thử tổng (#34→#35→#36)
```

Phụ thuộc chéo đáng chú ý:
- **#30 (sinh shared-types)** phụ thuộc OpenAPI ổn định từ ProblemDetails (#15) + error catalog (#4) + endpoints resolve/auth (#21, #24) → đó là lý do FE feature đi SAU BE contract (DEC-029).
- **#27 (NotifyVisitEndedAsync)** phụ thuộc `EndVisit` (#24) và Hub group `visit-{id}-guest` (#26) — khép kín P0-3.
- **#18 (rate limiter)** phụ thuộc **#17** (GuestCookieRead phải chạy trước `UseRateLimiter`) — khép kín P0-1.

```json
{
  "waves": [
    { "wave": 0, "name": "Solution & Monorepo skeleton", "tasks": [1, 2, 3], "dependsOn": [] },
    { "wave": 1, "name": "SharedKernel abstractions", "tasks": [4, 5, 6, 7, 8], "dependsOn": [0] },
    { "wave": 2, "name": "Infrastructure EF + migration nền", "tasks": [9, 10, 11, 12, 13, 14], "dependsOn": [1] },
    { "wave": 3, "name": "Cross-cutting concerns", "tasks": [15, 16, 17, 18, 19, 20], "dependsOn": [2] },
    { "wave": 4, "name": "Module nền", "tasks": [21, 22, 23, 24, 25], "dependsOn": [3] },
    { "wave": 5, "name": "Realtime SignalR", "tasks": [26, 27], "dependsOn": [4] },
    { "wave": 6, "name": "Extension points", "tasks": [28, 29], "dependsOn": [4] },
    { "wave": 7, "name": "Frontend base", "tasks": [30, 31, 32, 33], "dependsOn": [3, 4] },
    { "wave": 8, "name": "E2E backbone + kiểm thử tổng", "tasks": [34, 35, 36], "dependsOn": [5, 6, 7] }
  ]
}
```

## Tasks

### Wave 0 — Solution & Monorepo skeleton (BE + FE có thể làm song song)

- [ ] 1. Backend solution skeleton 5 project + governance build
  - Tạo 5 project đúng tên: `ResortQr.SharedKernel`, `ResortQr.Domain`, `ResortQr.Application`, `ResortQr.Infrastructure`, `ResortQr.Api` (không thừa/thiếu) — `23-solution-skeleton.md` §1–2.
  - `Directory.Build.props`: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true` (khoanh `WarningsNotAsErrors` cho generated code nếu cần) — `23` §3, `06-conventions.md`.
  - `Directory.Packages.props` (Central Package Management, `ManagePackageVersionsCentrally=true`) pin version một chỗ theo `technology-stack.md` — `23` §4.
  - ProjectReference enforce dependency rule **tĩnh ở csproj**: Domain KHÔNG ref EF/Infra/Api; Application KHÔNG ref Api/Infrastructure-impl — Req 1.2, 1.3; `01-architecture.md` §2.
  - `AssemblyMarker` mỗi project (cho DI quét tường minh) + `AppDbContextFactory : IDesignTimeDbContextFactory` để `dotnet ef` chạy không cần bật app — `23` §5, Req 2.2.
  - _Requirements: 1.1, 1.2, 1.3, 1.6, 2.2_

- [ ] 2. Khung Architecture_Test (NetArchTest) chạy trong CI-local
  - Test dependency rule (Domain/Application không ref sai) + naming convention; fail liệt kê rõ vi phạm — Req 1.5, 19.6, 19.7, 19.8; `07-testing-strategy.md`, `06` §kiểm soát kiến trúc.
  - Ban đầu chỉ cần assert cấu trúc 5 project + dependency rule (chưa cần module).
  - _Requirements: 1.5, 19.6, 19.7, 19.8_

- [ ] 3. Frontend monorepo skeleton (pnpm workspaces)
  - `guest-web` (mobile-first, mặc định `en`) + `admin-web` (`vi`, có đăng nhập) + packages `api-client`, `shared-types`, `realtime`, `ui-kit` — Req 17.1; `frontend/05-monorepo-skeleton.md`, `02-architecture.md`.
  - tsconfig `strict` + `noUncheckedIndexedAccess`; dependency rule FE một chiều (apps→packages); Vite 8 + Vue 3.5 + Pinia + Vue Router + vue-i18n (version theo `technology-stack.md`).
  - `admin-web` cấu hình `base: '/admin/'` (tránh asset 404 sau deploy) — `frontend/05` §build; DEC-029.
  - _Requirements: 17.1_

### Wave 1 — SharedKernel: abstractions lõi (nền cho mọi tầng)

- [ ] 4. Result/Error/AppErrors + catalog mã lỗi
  - `Result<T>` trung lập Ok/Fail (KHÔNG chứa HTTP/header/media-type); Fail mang đúng 1 `ErrorType` + 1 `code` ∈ `AppErrors` — Req 4.1; `02-core-abstractions.md` §2.
  - `AppErrors` là hiện thân code của catalog `14-error-catalog.md` (nguồn chân lý); mỗi code có `ErrorType` để map HTTP sau — Req 4.1; `14`, DEC-019.
  - _Requirements: 4.1_

- [ ] 5. Base entity conventions + marker interfaces
  - `Entity`/`AuditableEntity` khóa `uuid` sinh client-side `Guid.CreateVersion7()`; interface `IAuditable`, `ISoftDeletable`, `IConcurrencyAware` (`RowVersion`) — Req 3.1, 3.7; `02` §1, `04-data-model.md` §1–2, DEC-003.
  - Marker DI interface `IScopedService`/`ISingletonService`/`ITransientService` — Req 2.1; `01` §5.
  - `Guard` helpers + convention async/CancellationToken — `06`, DEV-002.
  - _Requirements: 3.1, 3.7, 2.1_

- [ ] 6. Cross-cutting ports (định tính tất định — testable)
  - Khai báo port trong Application/SharedKernel: `IDateTimeProvider` (Clock), `ITokenGenerator`, `IHtmlSanitizer`, `ICurrentUser`, `IGuestContext`, `IRealtimeNotifier`, `IQrService`, `IPdfService`, `ITranslationResolver` — Req 6.1–6.8; `02` §4–6.
  - `IRealtimeNotifier` có method gửi tới nhóm staff + tới conversation cụ thể (UseCase KHÔNG ref SignalR Hub) — Req 6.4, 6.5.
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8_

- [ ] 7. UseCase base + Repository/UnitOfWork interface
  - UseCase-per-operation nhận input DTO → trả `Result<T>` — Req 4.1; `02` §7.
  - `IRepository` chỉ thao tác ChangeTracker (KHÔNG SaveChanges); `IUnitOfWork.SaveChangesAsync` là điểm ghi duy nhất + `ExecuteInTransactionAsync` — Req 5.1, 5.2, 5.5; `02` §3, DEV-001.
  - _Requirements: 4.1, 5.1, 5.2, 5.5_

- [ ] 8. Unit test SharedKernel + property test nền (không cần DB)
  - PBT: `Result` invariants; token round-trip base64url (Req 7.4); fallback ngôn ngữ luôn khác rỗng; portal window luôn có hạn hữu hạn (B8) — Req 19.2, 19.3, 19.4; `17-test-plan-base.md`, `08-correctness-properties.md`.
  - Test hợp đồng `AppErrors` ↔ FE `ErrorCode` (đối chiếu 1-1) — Req 4.6; `14`, DEC-021.
  - _Requirements: 19.1, 19.2, 19.3, 19.4, 4.6_

### Wave 2 — Infrastructure EF: DbContext + Repository/UoW + migration NỀN

- [ ] 9. AppDbContext (audit / soft-delete / concurrency / snake_case)
  - Interceptor/override `SaveChangesAsync` set `CreatedAt/CreatedByUserId`, `UpdatedAt/UpdatedByUserId` từ `Clock` + `ICurrentUser` (null nếu không có user) — Req 3.2, 3.3.
  - `ISoftDeletable`: set `IsDeleted/DeletedAt` thay xóa cứng + global query filter loại `IsDeleted=true` — Req 3.4, 3.5.
  - snake_case naming; `IConcurrencyAware` → `UseXminAsConcurrencyToken()` (KHÔNG `IsRowVersion` SQL Server) — Req 3.6, 3.7, 3.8; `03-cross-cutting-concerns.md` §3, `04` §9, TK-003.
  - _Requirements: 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [ ] 10. Repository + UnitOfWork implementation (ghi nguyên tử)
  - Repository generic thao tác ChangeTracker; UoW `SaveChangesAsync` ghi tất cả pending trong 1 lần; `ExecuteInTransactionAsync` all-or-nothing + rollback khi lỗi + truyền lỗi/cancel ra caller — Req 5.3, 5.4, 5.7, 5.8; `02` §3, `05-algorithms-and-specs.md` §2.
  - Map `DbUpdateConcurrencyException` → `concurrency_conflict` (409) ở middleware, exception propagate từ UoW (KHÔNG tự nuốt) — Req 5.6; `03` §1/§3, audit C5/C6.
  - _Requirements: 5.3, 5.4, 5.6, 5.7, 5.8_

- [ ] 11. Entity nền + EF fluent config + partial unique index (migration NỀN)
  - Định nghĩa entity nền: `Resort`, `ResortSettings`, `ResortLanguage`, `AppUser`, `RefreshToken`, `Room`, `RoomQrToken`, `GuestSession`, `GuestVisit` — Req 8.1; `04` §3.
  - `GuestSession` lưu `SessionKeyHash` (UNIQUE, indexed) — KHÔNG lưu raw secret (đối xứng RefreshToken lưu hash) — Req 11.3, 11.4; `04` §3/§7.1, `03` §4, P1-3/DEC (SessionKeyHash).
  - `RoomQrToken.Token` UNIQUE toàn cục; partial unique `ux_qr_active` (1 token Active/phòng); `ux_lang_default` (1 default/resort); `ux_visit_active` (1 GuestVisit Active/(session,room)) — Req 8.2, 8.3, 7.x; `04` §5/§7.1, DEV-010/013.
  - Enum lưu string (partial index đọc theo tên) — DEC-006.
  - Tạo **migration foundation** CHỈ gồm entity nền + index của chúng; additive/reviewable, áp lên DB có dữ liệu không mất data — Req 8.6, 8.7; `04` §4, DEC-010/TRD-002.
  - _Requirements: 8.1, 8.2, 8.3, 8.6, 8.7, 11.3, 11.4_

- [ ] 12. Port implementations (Infrastructure) + Token_Generator an toàn
  - `IDateTimeProvider`, `ICurrentUser`, `IGuestContext` (đọc từ cookie), `IHtmlSanitizer` (`Ganss.Xss` allowlist tường minh, cấm `on*`/`script`/`javascript:`) — Req 6, 12.3, 12.4; `03` §9, DEC-018.
  - `ITokenGenerator`: CSPRNG ≥32 byte; base64url ≥43 ký tự không padding; không chứa số phòng; sinh lại tối đa 5 lần khi trùng rồi trả lỗi; lỗi khi CSPRNG không khả dụng — Req 7.1, 7.2, 7.3, 7.5, 7.6; `05` §1, `16-rooms-and-qr.md` §5.
  - Đăng ký DI theo convention (Scrutor quét theo `AssemblyMarker`): 0 impl → bỏ qua; nhiều impl → đăng ký hết cùng lifetime; xung đột lifetime → fail khởi động rõ ràng — Req 2.1, 2.3, 2.4, 2.5, 2.6; `01` §5.
  - _Requirements: 2.1, 2.3, 2.4, 2.5, 2.6, 6.1, 6.2, 7.1, 7.2, 7.3, 7.5, 7.6, 12.3, 12.4_

- [ ] 13. Seeder idempotent + ResortSettings
  - Seed 1 `Resort` (Star Hill), `ResortSettings` mặc định an toàn, `ResortLanguage` (`en` default, `vi`, `ko`, `zh`), 1 `Admin` (mật khẩu từ env/appsettings, KHÔNG hardcode) — Req 15.1; `04` §6, DEC-020.
  - Idempotent: chạy lại không nhân đôi (nhận diện theo code/tên/mã ngôn ngữ/username) — Req 15.2.
  - `ResortSettings` chứa đủ trường: cờ ack FAQ/chat/housekeeping, `PortalWindowMinutes` (30), `VisitIdleExpiryHours` (24), `GuestWebBaseUrl` (https), `MaxMessageLength` (2000), ngưỡng rate gửi tin/ticket — Req 15.3; `15`.
  - _Requirements: 15.1, 15.2, 15.3_

- [ ] 14. Integration test ràng buộc DB (Testcontainers PostgreSQL 18)
  - Xác minh partial/unique index THỰC SỰ chặn ghi vi phạm (1 token Active/phòng, 1 default/resort, 1 visit Active/(session,room)) bằng lỗi ràng buộc, KHÔNG lưu bản ghi vi phạm — Req 8.2, 8.3, 19.5; `17` §2, B3.
  - Test `xmin` concurrency (B10) + soft-delete filter + audit fields — Req 3.x.
  - KHÔNG dùng InMemory (không enforce unique → false green) — DEC-021.
  - _Requirements: 8.2, 8.3, 19.5_

### Wave 3 — Cross-cutting concerns (pipeline HTTP nền)

- [ ] 15. Error middleware + ProblemDetails đầy đủ hợp đồng
  - Map `Result.Fail` + exception chưa bắt → `application/problem+json` với đủ 5 trường: `type` (`/problems/{code}`), `title`, `status`, `code` (∈ `14`), `traceId` — Req 4.2; `03` §1, P2-1.
  - Map `ErrorType`→HTTP: Validation→400, NotFound→404, Conflict→409, Forbidden→403, RateLimited→429, Unexpected→500; type ngoài bảng → 500/unexpected — Req 4.3, 4.4.
  - Exception chưa bắt → 500 + `code=unexpected` + `traceId`, KHÔNG lộ stack trace/chi tiết nội bộ — Req 4.5; `19-observability.md`.
  - _Requirements: 4.2, 4.3, 4.4, 4.5_

- [ ] 16. Validation pipeline (FluentValidation) + sanitize HTML
  - Behavior chạy validator trước thân UseCase; input sai → ProblemDetails `code=validation_error` + danh sách field, KHÔNG chạy thân (không đổi trạng thái) — Req 12.1, 12.2; `03` §2.
  - Rich text sanitize trên đường GHI (allowlist); nội dung lưu không còn `<script>`/`on*`/`javascript:`; rỗng/null → chuẩn hóa rỗng không lỗi (Property B7) — Req 12.3, 12.4, 12.5, 12.6.
  - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6_

- [ ] 17. ForwardedHeaders + Auth kép (JWT admin + guest cookie) — pipeline đúng thứ tự
  - `ForwardedHeadersMiddleware` chạy SỚM NHẤT, chỉ tin `KnownProxies` (để rate-limit theo IP đúng, không bị giả mạo) — Req 13.1; `03` §5/§8, DEC-018.
  - **Tách middleware:** `GuestCookieRead` (đọc cookie existing → nạp `IGuestContext`) chạy **TRƯỚC `UseRateLimiter`**; việc **TẠO GuestSession mới** chỉ trong resolve use case (sau khi token hợp lệ) — Req 11.3; `03` §4/§8, `13` §3, **P0-1**.
  - Đăng ký 2 scheme (JWT Bearer admin/staff + cookie guest), `UseAuthentication` TRƯỚC `UseAuthorization`; policy `RequireAdmin`/`RequireStaff`; `/api/admin/*` thiếu JWT hợp lệ → 401/403 không trả data; cookie guest HttpOnly/Secure/SameSite=Lax không chứa số phòng/token — Req 11.2, 11.3, 11.4, 11.5, 11.6; `12-identity-and-auth.md`, DEV-003.
  - _Requirements: 11.2, 11.3, 11.4, 11.5, 11.6, 13.1_

- [ ] 18. Rate limiting guest surface (sliding window)
  - Policy `resolve` phân vùng theo IP (mặc định 20 req/60s cửa sổ trượt); policy `guest-write` phân vùng theo `GuestSessionId` (10 req/60s), fallback IP khi không có session — Req 13.1, 13.2, 13.3; `03` §5.
  - Vượt ngưỡng → `rate_limited` (429) + số giây chờ, KHÔNG xử lý request (không resolve/tạo tin/ticket); đọc ngưỡng theo precedence `ResortSettings`→`appsettings`→mặc định — Req 13.4, 13.5.
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_

- [ ] 19. Structured logging (Serilog) + mask bí mật + health check
  - Log JSON mỗi request, enrich `RequestId`, `ResortId`, `ConversationId` (khi có), `error code`; `RoomId` CHỈ khi resolve thành công (không lộ dò phòng khi token lỗi) — Req 14.1; `19` §log fields.
  - Mask tại nguồn: không log PublicToken đầy đủ/mật khẩu/refresh token/Authorization/Cookie; path `/r/{token}`→`/r/***`; KHÔNG log nội dung tin nhắn (chỉ ConversationId + độ dài) — Req 14.2; DEC-023.
  - `/health/live` ≤2s (không phụ thuộc DB); `/health/ready` kiểm DB ≤5s, lỗi → không sẵn sàng + không lộ connection string — Req 14.3, 14.6, 14.7; `19` §5.
  - _Requirements: 14.1, 14.2, 14.3, 14.6, 14.7_

- [ ] 20. Security_Layer: CORS + security headers + HTTPS (Req 20)
  - CORS allowlist origin tường minh (guest/admin), KHÔNG wildcard `*`, method cần thiết, `AllowCredentials` chỉ origin khai báo; origin ngoài list → không set `Access-Control-Allow-Origin`; preflight OPTIONS trả header CORS không tới UseCase — Req 20.1, 20.2, 20.3.
  - Security header: CSP (`default-src 'self'`, `frame-ancestors 'none'`, `connect-src 'self' wss:`), `X-Content-Type-Options: nosniff`; HSTS `max-age≥31536000`+`includeSubDomains` khi có TLS; HTTP→HTTPS permanent redirect giữ path/query — Req 20.4, 20.5, 20.6.
  - Áp cho cả guest (`/api/guest/*`, `/r/*`), admin (`/api/admin/*`), Hub (`/hubs/*`) — Req 20.7; `03` §7, `20-deployment-reverse-proxy.md`.
  - _Requirements: 20.1, 20.2, 20.3, 20.4, 20.5, 20.6, 20.7_

### Wave 4 — Module nền (backend, hiện thực đầy đủ)

- [ ] 21. Module Identity — login + refresh rotation + reuse detection
  - Hash mật khẩu **Argon2id** (PHC string, rehash-on-login); login lỗi mơ hồ + hash giả khi user không tồn tại (chống enumeration/timing) — Req 11.1; `12` §1, DEC-016.
  - JWT HS256 access token (mặc định 15', khoảng 5–60); claims + validation; policy `RequireAdmin`/`RequireStaff` — Req 11.1, 11.6; `12` §2.
  - Refresh token opaque CSPRNG **lưu hash**, rotation one-time-use + **reuse detection theo FamilyId** (token đã xoay bị trình lại → thu hồi cả family); access token hết hạn + refresh hợp lệ → cấp mới không bắt login lại; refresh hết hạn/không hợp lệ → xóa cookie + bắt login lại — Req 11.7, 11.8; `12` §3, `04` §3 (RefreshToken schema), DEV-012.
  - _Requirements: 11.1, 11.6, 11.7, 11.8_

- [ ] 22. Module Settings/Localization — TranslationResolver + fallback
  - Mẫu entity gốc + `*Translation`, unique `(ParentId, LanguageCode)`; `LanguageCode` ∈ ngôn ngữ bật của resort — Req 9.1; `04` §2, `18-localization.md`.
  - Resolve theo primary-subtag (`ko-KR`→`ko`, lowercase); có bản dịch nội dung không rỗng → `IsFallback=false`; thiếu/rỗng/không-bật + có default không rỗng → default `IsFallback=true`; **row rỗng = coi như thiếu** — Req 9.2, 9.3, 9.4; `18`, DEC-022.
  - Resolve theo TẬP (1 query chống N+1), cờ `isFallback` riêng từng mục; thiếu cả 2 → missing, không lỗi cả request — Req 9.5, 9.6.
  - `ResortSettings` đọc theo precedence + default an toàn khi null; `GuestWebBaseUrl` phải https hợp lệ khi sinh QR — Req 15.4, 15.5, 15.6.
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 15.4, 15.5, 15.6_

- [ ] 23. Module Rooms & QR — CRUD phòng + cấp/thu hồi token nguyên tử + QR/PDF
  - CRUD `Room` (số phòng 1–20 ký tự unique/resort, toà nhà ≤50, tầng -10..200, trạng thái Active/Inactive/Maintenance) + soft-delete; số phòng trùng/không hợp lệ → từ chối + lỗi — Req 16.1, 16.6.
  - Sinh QR PNG chứa `https://<host>/r/{token}` (host từ `GuestWebBaseUrl`) ≤5s; Room không Active hoặc sinh QR lỗi → không tạo file + lỗi — Req 16.2, 16.7, 15.4.
  - Thu hồi: token cũ → `Revoked`, sinh token mới Active, giữ lịch sử (không xóa cứng); đảm bảo ≤1 token Active/phòng kể cả đồng thời (bắt unique-violation `ux_qr_active`, re-query) — Req 16.4, 16.5; `16` §3–4, B3.
  - Xuất PDF nhãn nhiều phòng (≤500/lần, có số phòng+logo) ≤30s; vượt 500 → từ chối + lỗi — Req 16.3, 16.8; `16` §7.
  - **Migration per-wave:** nếu module này dùng entity/index mới ngoài nền thì thêm trong migration riêng của wave (Rooms/QrToken đã ở nền) — Req 8.7.
  - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.7, 16.8, 15.4_

- [ ] 24. Module GuestAccess — resolve + vòng đời GuestVisit + portal window (sliding)
  - Resolve token Active → Room+Resort; `qr_invalid`/`qr_revoked`/`room_inactive` không lộ phòng khác, không tạo/nối visit — Req 10.1, 10.2; `13` §3.
  - Tạo `GuestSession` (nếu chưa có cookie, lưu `SessionKeyHash`) + `GuestVisit` mới Active (`LastSeenAt=now`, `ExpiresAt=now+PortalWindow`); nối lại đúng visit Active còn hạn (giữ hội thoại/ack) + cập nhật LastSeenAt/ExpiresAt — Req 10.3, 10.4; `13` §3.
  - **Sliding window (chốt P0-2):** resolve LẪN endpoint tương tác thành công TRONG hạn đều refresh `LastSeenAt`/`ExpiresAt`; quá `PortalWindow` → `session_expired` cho endpoint tương tác, KHÔNG cập nhật; chỉ `/resolve` mở khóa lại. Thứ tự **kiểm-tra-TRƯỚC, cập-nhật-SAU** (B8) — Req 10.5, 10.6; `13` §1/§4, DEC-030.
  - Lazy idle-expiry: visit Active nhưng `now>ExpiresAt` → EndVisit ngay (không chỉ chờ sweeper); quá `VisitIdleExpiryHours` (24) → Expired; quét lại sau Expired → visit MỚI (dữ liệu lượt trước không hiện) — Req 10.7; `13` §2/§4, DEV-014.
  - `EndVisit` cascade idempotent (đóng conversation Open, hủy ticket mở, ghi HousekeepingEvent); post-commit gọi `NotifyVisitEndedAsync` — Req 10.8; `13` §5, **P0-3**.
  - `BuildResolveResponse`: Room + Resort + ngôn ngữ bật + trạng thái visit + cờ tính năng (faq/chat/ruleAck*) từ `ResortSettings` — Req 10.8; `13` §3.
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8_

- [ ] 25. VisitIdleSweeper (lưới an toàn, KHÔNG phải nguồn đúng đắn)
  - Hosted service DUY NHẤT chạy chu kỳ cấu hình (mặc định 5'); mỗi visit `now>ExpiresAt` → Expired + đóng conversation Open + hủy ticket mở của visit (dùng chung `EndVisit` idempotent) — Req 14.4; `13` §6, DEC-008.
  - Xử lý 1 visit lỗi → bỏ qua, tiếp tục visit còn lại, giữ nguyên visit lỗi cho lượt sau — Req 14.8.
  - TUYỆT ĐỐI không thay đổi `RoomQrToken` — Req 14.5; DEC-008.
  - _Requirements: 14.4, 14.5, 14.8_

### Wave 5 — Realtime SignalR (nền, giải GAP-1)

- [ ] 26. SignalR Hub + auth kép + JoinConversation server-authoritative
  - Auth handshake: admin gửi JWT qua query `access_token` (WS không set header Authorization) đọc ở `OnMessageReceived` cho path `/hubs` + **mask `access_token` trong log**; guest dùng cookie tự gửi — Req 11.x; `21-realtime-signalr.md` §2, DEC-026.
  - `JoinConversation` **KHÔNG tin `conversationId` client** — server suy `IGuestContext`→visit→conversation; guest chỉ join conv của chính visit; guest không join group staff (chống nghe lén — Property B9) — Req 6.4, 6.5; `21` §4.
  - **Enforce PortalWindow/ExpiresAt ở MỌI điểm** (`OnConnectedAsync` + join/rejoin + method) dùng chung `EnforcePortalWindow` với REST; guest join group `visit-{visitId}-guest` — **P0-3**; `21` §4/§4.1, DEC-031.
  - _Requirements: 6.4, 6.5, 11.2, 11.3_

- [ ] 27. IRealtimeNotifier impl (adapter) + notify post-commit + evict visit
  - Adapter map `NotifyStaffAsync`/`NotifyConversationAsync` → SignalR groups; notify CHỈ SAU commit (tránh event "ma" khi rollback) — Req 6.4, 6.5; `21` §6, DEV-008.
  - `NotifyVisitEndedAsync(visitId)` gửi `VisitEnded` tới group `visit-{visitId}-guest` + chặn rejoin (visit không Active) → guest ngừng nhận realtime sau expiry; gọi post-commit trong `EndVisit` — **P0-3**; `21` §4.1/§6, `13` §5, DEC-031.
  - _Requirements: 6.4, 6.5_

### Wave 6 — Extension points (khung module nghiệp vụ, KHÔNG entity/migration)

- [ ] 28. Khung module Rules/Faq/Messaging/Housekeeping/Notes
  - Mỗi module: thư mục con dưới `Modules/` (Application + Infrastructure) + interface UseCase (chưa hiện thực) + khung controller — Req 18.1, 18.2; `01` §4, `00` §Lộ trình bước 6.
  - KHÔNG tạo entity/bảng/migration cho module chưa code (tránh schema "chết"); wave sau chỉ thêm entity+migration+điền logic, KHÔNG sửa cấu trúc 5 project/DI/migration nền — Req 18.1, 18.2, 18.4, 8.6; DEC-010.
  - Giao tiếp liên module qua interface UseCase/port đã đăng ký DI (không đọc entity nội bộ module khác) — Req 18.3.
  - _Requirements: 18.1, 18.2, 18.3, 18.4_

- [ ] 29. Architecture_Test mở rộng: enforce ranh giới module
  - Test module A không ref trực tiếp entity nội bộ module B → fail chỉ rõ vi phạm — Req 18.5; `06`, `07`.
  - Test migration nền áp lỗi → app dừng khởi động báo lỗi rõ (kiểm ở integration) — Req 18.6.
  - _Requirements: 18.5, 18.6_

### Wave 7 — Frontend base (contract-first: sinh type từ backend OpenAPI)

- [ ] 30. Sinh `shared-types` từ OpenAPI + `api-client`
  - Sinh `shared-types` từ OpenAPI backend + test hợp đồng `ErrorCode`↔`AppErrors` (chống lệch BE↔FE) — Req 4.6; `frontend/05` §type-gen, `14`, DEC-029.
  - `api-client`: status ≥400 → parse ProblemDetails → ném `ApiError{code,status,message}`, không trả data; body không phải ProblemDetails hợp lệ → `ApiError` code `unexpected` không crash; gửi cookie `credentials:'include'` — Req 4.6, 4.7, 17.2, 17.3, 17.4; `frontend/02` §api-client.
  - _Requirements: 4.6, 4.7, 17.2, 17.3, 17.4_

- [ ] 31. `realtime` package (SignalR wrapper) + i18n nền
  - Reconnect tối đa 5 lần backoff 1–30s; WS lỗi/reconnect fail → fallback polling mỗi 10s — Req 17.6, 17.7; `frontend/02`, `06-i18n-and-content.md`.
  - i18n hai tầng (UI text JSON vs content API); render content HTML **chỉ v-html cho nội dung backend đã sanitize** (defense-in-depth); `isFallback` hiển thị badge; thời gian API trả UTC → format theo `Resort.Timezone` (không theo trình duyệt); CJK fallback ko/zh — Req 9.x; `frontend/06`, DEC-029.
  - _Requirements: 17.6, 17.7_

- [ ] 32. guest-web: resolve flow + session store + ErrorCode→UI + RuleGate
  - Khởi tạo ngôn ngữ theo thứ tự `?lang=`→localStorage→`navigator.language`(chuẩn hóa `ko-KR`→`ko`)→default `en`; ngoài danh sách bật → fallback `en` — Req 17.5; `frontend/03-guest-web-flows.md` §resolve.
  - State machine màn hình (Resolving/TokenError/RuleGate/…); session store khớp `/resolve`; TokenError không lộ phòng khác; overlay `session_expired` chỉ thoát khi quét lại QR — Req 10.2, 10.5; `frontend/03`.
  - _Requirements: 17.5, 10.2, 10.5_

- [ ] 33. admin-web: auth store (token in-memory) + silent refresh single-flight + route guards
  - Access token giữ **in-memory** (Pinia), không localStorage (chống XSS); refresh cookie HttpOnly — Req 11.1; `frontend/04-admin-auth-guards.md`, DEC-014.
  - Nhiều 401 song song chia sẻ MỘT lần refresh (single-flight), retry đúng 1 lần; `/refresh` 401 (family revoked) → hard logout — `frontend/04` §2, DEC-024.
  - Route guard: chưa đăng nhập vào route bảo vệ → redirect login; đã đăng nhập nhưng không đủ policy `RequireAdmin`/`RequireStaff` → chặn + báo không đủ quyền — Req 17.8, 17.9.
  - _Requirements: 11.1, 17.8, 17.9_

### Wave 8 — Lát cắt dọc E2E backbone + kiểm thử tổng & DoD

- [ ] 34. E2E backbone "đường xương sống"
  - admin tạo phòng → sinh QR → guest resolve → nhận session/visit/features, chạy được end-to-end (BE + FE) — `00` §Lộ trình bước 8; xác nhận contract-first hoạt động.
  - _Requirements: 10.8, 16.2, 16.5_

- [ ] 35. Hoàn tất ma trận Property B1–B10 + PBT ≥100 iteration
  - Mỗi bất biến nền B1–B10 có test fail-khi-phá (Given/When/Then) chạy trên PostgreSQL 18 thật — Req 19.5; `17` §2, `08`.
  - PBT ≥100 iteration cho: token duy nhất, fallback ngôn ngữ khác rỗng, portal window có hạn hữu hạn, bất biến đơn điệu; counterexample → fail + ghi seed tái lập — Req 19.2, 19.3, 19.4.
  - _Requirements: 19.2, 19.3, 19.4, 19.5_

- [ ] 36. Rà soát cuối: unit test-only cho UseCase + chạy lại consistency audit
  - Xác nhận mọi UseCase test được bằng port giả lập, KHÔNG HTTP/DB thật/mạng ngoài — Req 19.1; `07`.
  - Chạy lại grep consistency audit (`22-consistency-audit.md` §1–3 + TK-032): mọi `error(...)` ∈ catalog `14`; con số đa-nơi khớp; quyết định đã chốt không bị bản cũ mâu thuẫn — DEC-021, audit 6.
  - Cập nhật `ai-notes/**` nếu phát sinh quyết định/deviation/tradeoff/điều-cần-biết mới trong lúc implement.
  - _Requirements: 19.1_

## Notes

- **Phạm vi:** đây là task cho phần **nền (base)** + lát cắt xương sống. Nghiệp vụ đầy đủ (Rules/FAQ/Chat/Housekeeping/Notes) triển khai ở wave sau, xây trên extension point (Wave 6) và migration per-wave — KHÔNG sửa nền.
- **Truy vết requirement:** 20/20 requirements được phủ. Bản đồ nhanh: Req1→#1,#2; Req2→#5,#12; Req3→#5,#9; Req4→#4,#15,#30; Req5→#7,#10; Req6→#6,#12,#26,#27; Req7→#12,#23; Req8→#11,#14; Req9→#22,#31; Req10→#24,#32; Req11→#17,#21,#33; Req12→#16; Req13→#17,#18; Req14→#19,#25; Req15→#13,#22,#23; Req16→#23; Req17→#3,#30,#31,#32,#33; Req18→#28,#29; Req19→#2,#8,#14,#35,#36; Req20→#20.
- **Verify-first:** mỗi task sau khi code phải chạy build + test liên quan trước khi coi là xong; tính năng bảo mật (auth #21, SignalR #26/#27, rate limit #18, security headers #20) bắt buộc có test chứng minh bất biến.
- **Pin version khi implement:** không hardcode số version trong code/doc; lấy từ `design/technology-stack.md` (re-verify license Mapperly/Element Plus + trạng thái PrimeVue tại thời điểm implement — TK-017/019).
- **Cập nhật ai-notes:** trong lúc implement, bất kỳ quyết định tự ra / deviation / tradeoff / điều-cần-biết mới → ghi vào `ai-notes/**` (giữ tính xuyên suốt kiểm chứng).
