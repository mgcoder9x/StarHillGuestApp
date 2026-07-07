# Resort QR — App backend

> 📦 **Workspace APP Resort QR.** Được **copy từ base `foundation/`** (domain-agnostic, sạch) ngày 2026-07-04, rồi **đổi tên toàn bộ `Foundation.*` → `ResortQr.*`** (namespace + project + class/method + file/folder). Solution: `ResortQr.slnx`.
>
> **Base gốc `foundation/` được giữ SẠCH, không đụng tới** — vẫn mang tên `Foundation.*` để tái dùng cho dự án khác. Vì đã đổi tên nên bản copy này là một cây độc lập (không còn diff trực tiếp với base gốc — nếu sau này cần lấy bản vá của base thì merge thủ công theo layer).
>
> Tầng `ResortQr.*` hiện tại = **nền tái dùng đã đổi tên** (SharedKernel/Application/Infrastructure/Api). Nghiệp vụ resort (Resort, Room, RoomQrToken, GuestVisit, chat, housekeeping...) sẽ thêm TRÊN nền này (xem "Bước tiếp theo").

---

## Nền (đã đổi tên `ResortQr.*`) — domain-agnostic, đã test

> ⚠️ **Tầng nền là BASE/TEMPLATE, KHÔNG phải một API chạy độc lập được ngay.** Host cụ thể **BẮT BUỘC** phải cung cấp phần persistence (xem "Điều app phải cung cấp"). Thiếu → DI **fail-fast** khi khởi động (đúng ý đồ: báo lỗi sớm thay vì chạy nửa vời).

## Cấu trúc (Clean Architecture / modular monolith)

```
resort-qr/
  ResortQr.slnx
  Directory.Build.props        # net10, Nullable, TreatWarningsAsErrors, analyzers
  Directory.Packages.props     # Central Package Management (pin version một chỗ)
  global.json                  # pin .NET SDK 10.0.301 (rollForward latestFeature)
  .editorconfig
  src/
    ResortQr.SharedKernel/   # Result/Error (+Details), base Entity (UUIDv7, chặn Guid.Empty), marker DI, Guard
    ResortQr.Domain/         # entity riêng theo app (base để trống)
    ResortQr.Application/    # use case + port + validation decorator (IUseCase/ICommandUseCase) + Identity flows
    ResortQr.Infrastructure/ # Argon2id hasher, JWT service, token gen, SHA-256 refresh hasher, HtmlSanitizer, DI (Scrutor)
    ResortQr.Api/            # composition root: Options fail-fast, ProblemDetails, JWT Bearer + policy, security headers/CORS, endpoint /auth/*
  tests/
    ResortQr.UnitTests/          # SharedKernel, auth, DI, validation
    ResortQr.IntegrationTests/   # HTTP thật (WebApplicationFactory) + (Testcontainers — hoãn tới khi có Docker)
    ResortQr.ArchitectureTests/  # NetArchTest: enforce dependency rule
```

## Đã có sẵn trong base (generic, đã test)

- **SharedKernel:** `Result`/`Result<T>`/`Error` (kèm field-errors), base `Entity` (UUIDv7, bất biến Id≠Guid.Empty), `Guard`, marker DI, `ConcurrencyConflictException` (trung lập, Api map 409).
- **Auth:** Argon2id (PHC + rehash-on-login), JWT HS256 (clock inject, ClockSkew=0), refresh token **rotation + reuse-detection + consume nguyên tử** (chống race), login/refresh/logout use case.
- **Persistence (EF Core 10 + PostgreSQL/Npgsql):** `ResortQrDbContext` base (audit tự động, xóa mềm filter + Delete→Modified, concurrency `xmin` **Npgsql-only có điều kiện**, snake_case), `EfRepository<T>`, `EfUnitOfWork` (điểm ghi + transaction, chuyển `DbUpdateConcurrencyException`→`ConcurrencyConflictException`), entity `RefreshTokenRecord` + `EfRefreshTokenStore` (**`TryConsumeAsync`/`RevokeFamilyAsync` atomic bằng `ExecuteUpdateAsync`**). Wire qua `AddResortQrPersistence<TContext>()`.
- **Cross-cutting:** DI theo convention (Scrutor + guard xung đột lifetime), Options **validate-on-start** (fail-fast), ProblemDetails (Req 4, gồm 401/403 auth + 409 concurrency), validation pipeline (FluentValidation decorator cho `IUseCase` + `ICommandUseCase`), HtmlSanitizer (allowlist), security headers (CSP/nosniff) + CORS allowlist + HSTS.
- **Endpoint auth:** `POST /auth/login|refresh|logout`, `GET /auth/me`. Access token ở body; refresh token cookie HttpOnly/Secure/SameSite=Strict, Path=/auth.
- **Endpoint guest:** `GET /api/guest/resolve/{token}` — quét QR → phòng + mở/nối `GuestVisit` (lazy idle-expiry, nối lại Active, race DB-arbitrated); set cookie guest HttpOnly/Secure/SameSite=Lax. (Nghiệp vụ resort — `ResolveTokenUseCase`, DEC-056.)
- **Endpoint admin phòng/QR:** GET list/detail (`RequireStaff`); `POST /rooms` (tạo + issue token nguyên tử), `PUT /rooms/{id}` (sửa), `PUT /rooms/{id}/status`, `DELETE /rooms/{id}` (soft-delete), `POST /rooms/{id}/revoke-token` (rotate atomic + chịu race), `GET /rooms/{id}/qr.png` (QR PNG — QRCoder managed, đa nền tảng) — đều `RequireAdmin`. Read-model qua `IRoomQueries` (CQRS-lite). (DEC-057/058/059. PDF nhãn hoãn.)
- **Health:** `/health/live` (liveness, không phụ thuộc DB) + `/health/ready` (readiness, chạy check gắn tag `ready` — DB connectivity do `AddResortQrPersistence` đăng ký).
- **Admin Settings (DEC-073):** `GET/PUT /api/admin/settings` (`RequireAdmin`) — đọc/cập nhật ResortSettings runtime (bật/tắt FAQ/Chat/Housekeeping, yêu cầu ack nội quy, cửa sổ phiên/idle, GuestWebBaseUrl HTTPS, ngưỡng); validator khớp CHECK constraint DB. Mở khóa các cổng RuleGate/feature-enabled đã xây.
- **EndVisit cascade (DEC-075):** port `IVisitEndHandler` (GuestAccess sở hữu, Housekeeping implement) — khi visit kết thúc → hủy ticket dọn phòng đang mở + ghi event, CÙNG 1 transaction (STAGE-only).
- **VisitIdleSweeper (DEC-076, DEC-008):** hosted service DUY NHẤT dọn visit idle quá hạn (mặc định 5', delay-trước) + cascade qua `IVisitEndHandler`; mỗi visit 1 scope (cách ly lỗi Req 14.8); KHÔNG đụng RoomQrToken. Logic lõi `IVisitEnder` (idempotent, tái dùng cho staff-close). **→ vòng đời visit khép kín** (lazy-expiry + proactive sweep).
- **Admin Dashboard (DEC-077):** `GET /api/admin/dashboard` (`RequireStaff`) — tổng quan vận hành: phòng (tổng/active), ticket mở (Requested/InProgress), version nội quy hiện hành, FAQ active, visit active. Read-model `IDashboardQueries` (CountAsync, AsNoTracking).
- **Rate-limit guest (DEC-074, Req 13):** policy `resolve` (per-IP 20/60s) trên GET resolve + `guest-write` (per-session hash-cookie, fallback IP, 10/60s) trên POST rules/acknowledge + housekeeping — sliding-window, vượt → 429 `rate_limited`. Ngưỡng qua `RateLimit` appsettings. (Precedence ResortSettings — TK-048 — hoãn.)
- **Wave Rules (đang dựng):** domain nội quy Draft→Publish snapshot + ack (7 entity, migration `Rules_Init` per-wave, ràng buộc ux_pub_current/ux_ack/ux_tr_rule) — sub-slice A xong (schema), **B xong (authoring draft)**: `GET/PUT /api/admin/rules/draft` (`RequireStaff`) — FULL-REPLACE nguyên tử + sanitize HTML tầng ghi + `MissingLanguages`; **C xong (publish snapshot)**: `POST /api/admin/rules/publish` (`RequireAdmin`) — đóng băng draft→publication bất biến (Version++, đúng 1 IsCurrent, 2-SaveChanges nguyên tử); **D xong (guest read + ack)**: `GET /api/guest/rules?visitId=&lang=` (resolve fallback) + `POST /api/guest/rules/acknowledge` (idempotent ux_ack, server tự xác định version) bảo vệ bằng `IPortalWindowGuard`; **E xong (ack-gate)**: cổng tái dùng `IRuleGate` trả `rule_ack_required` (fail-open khi chưa publish) cho wave sau. **→ WAVE RULES HOÀN TẤT A→E** (DEC-060→064, `docs/rules-wave-design.md`).
- **Wave FAQ (đang dựng):** CMS đơn giản category→item đa ngôn ngữ (4 entity, migration `Faq_Init` per-wave, ux_tr_faq_cat/ux_tr_faq) — **A xong (schema)** + fix model-drift (migration trước bị thiếu — DEC-067), **C xong (guest read)**: `GET /api/guest/faq?visitId=&lang=` resolve fallback + `IPortalWindowGuard` + `IRuleGate(Faq)` + tắt tính năng→rỗng; **B xong (admin CRUD)**: `GET /api/admin/faq` + CRUD category/item (`RequireStaff`), MERGE bản dịch + sanitize answer. **→ WAVE FAQ HOÀN TẤT A+B+C** (DEC-067/068/069, `docs/faq-wave-design.md`).
- **Wave Housekeeping (đang dựng):** ticket dọn phòng + nhật ký (2 entity, migration `Housekeeping_Init`, bất biến **1 ticket mở/phòng** ux_hk_open partial) — **A xong (schema)**: entities/enums/config/migration + 5 test ràng buộc; **B xong (guest create+read)**: `POST/GET /api/guest/housekeeping` — idempotent theo phòng + `IPortalWindowGuard` + `IRuleGate(Housekeeping)`; **C xong (staff)**: `/api/admin/housekeeping` (`RequireStaff`) — list + máy trạng thái + complete-by-room/token + staff-create. **→ WAVE HOUSEKEEPING HOÀN TẤT A+B+C** (DEC-070/071/072); rate-limit guest-write hoãn task #18 (TK-046), EndVisit hủy ticket hoãn (`docs/housekeeping-wave-design.md`).
- **Wave Notes (DEC-078):** ghi chú nội bộ staff (Req 9.4, entity `InternalNote : AuditableEntity`, migration `Notes_Init`) — gắn phòng HOẶC hội thoại; tác giả = audit `CreatedByUserId` (không nhận qua body → chống giả mạo); `ConversationId` là cột nullable KHÔNG FK (Conversation thuộc wave Messaging — nợ FK, TK-050). **P7 (không lộ cho khách) đảm bảo BẰNG KIẾN TRÚC**: chỉ `/api/admin/notes` (`RequireStaff`) + `INoteQueries` chạm entity — không use case guest nào tham chiếu. Endpoints: GET (lọc roomId/conversationId), POST (201), PUT/{id} (204), DELETE/{id} (204, hard-delete). **→ WAVE NOTES HOÀN TẤT.**
- **OpenAPI (DEC-079, GAP-6/TK-022):** expose `/openapi/v1.json` bằng `Microsoft.AspNetCore.OpenApi` 10.0.9 (built-in .NET 10) — hợp đồng máy-đọc để FE sinh `shared-types` (contract-first). Có Info + securityScheme `Bearer` + gắn Bearer per-operation theo authz. **An-toàn-mặc-định:** đăng ký generator LUÔN nhưng chỉ PHƠI endpoint khi `OpenApi:Enabled` (mặc định: BẬT ở Development, TẮT nơi khác — tránh lộ bề mặt API ra public Production). Đã pin transitive `Microsoft.OpenApi=2.7.5` vá CVE-2026-49451 (fix tận gốc, không NoWarn). Nợ: `.Produces<T>()` per-endpoint (TK-054).

## Persistence app — ĐÃ hiện thực (entity nền + EF mapping)

Tầng persistence app đã dựng (DEC-053/054), test bằng SQLite Docker-free:
- **`ResortQr.Domain`**: 8 entity nền (Resort, ResortSettings, ResortLanguage, AppUser, Room, RoomQrToken, GuestSession, GuestVisit) + enum — POCO thuần (`docs/domain-layer-design.md`).
- **`ResortQr.Infrastructure.Persistence`**: `AppDbContext : ResortQrDbContext` (DbSet) + 8 `IEntityTypeConfiguration<>` (enum→string, MaxLength, FK Restrict, **unique/partial-unique index** `04` §5/§7, CHECK constraint) + `AppUserAuthStore` (impl `IUserAuthStore`) + `AppDbContextFactory` (design-time) (`docs/persistence-mapping-design.md`).

**Host ĐÃ wire** (`ResortQr.Api/Program.cs`): `AddResortQr(config)` + `AddResortQrDatabase(connString)` (DbContext Npgsql + snake_case + `AddResortQrPersistence`). Migrate+seed **opt-in** qua `Database:MigrateOnStartup` (mặc định tắt → không mở kết nối trong test/dev chưa có DB; deployment single-instance bật).

- **Migration `InitialCreate`** đã sinh (`src/ResortQr.Infrastructure/Migrations/`) qua `dotnet ef` (local tool, dùng `AppDbContextFactory`). DDL Npgsql verify offline: `xmin`→`xid`, CHECK, partial index đúng literal. Thêm migration sau: `dotnet ef migrations add <Name> --project src/ResortQr.Infrastructure`.
- **Seeder** (`ResortSeeder`, `04` §6): idempotent — resort + settings + languages (en mặc định, vi/ko/zh) + admin (băm Argon2id, mật khẩu từ `Seed:AdminPassword` env). Chạy khi `Database:MigrateOnStartup=true`.

**Còn lại:**
- Chạy thật trên **PostgreSQL** (Docker): set `ConnectionStrings:Postgres` + `Database:MigrateOnStartup=true` + `Seed:AdminEmail/AdminPassword` (env) → app tự migrate + seed admin.
- **Validator** (`AbstractValidator<T>`) cho use case — thêm khi dựng use case (Application), `AddResortQr` tự quét đăng ký.
- **Testcontainers/Postgres** cho xmin runtime, race đa-connection (TK-036).

> **Fail-fast:** thiếu `AddDbContext`/`AddResortQrPersistence` → resolve `IUnitOfWork`/`IRefreshTokenStore`/`IUserAuthStore`/`ResortQrDbContext` lỗi lúc khởi động. Persistence + user store wire **tường minh** (không auto-scan) để phụ thuộc DbContext hiện rõ (DEC-049). Ở Production thiếu `ConnectionStrings:Postgres` → app từ chối khởi động (`15` §3).

## Cách dùng trong host (Program.cs)

```csharp
builder.Services.AddResortQr(builder.Configuration); // Options + Scrutor DI + Auth/AuthZ + Security
// app tự thêm: EF DbContext + IUnitOfWork + IUserAuthStore + IRefreshTokenStore + validators...
var app = builder.Build();
app.UseResortQr();                 // Exception→ProblemDetails → HSTS → security headers → CORS → AuthN → AuthZ
app.MapResortQrAuthEndpoints();
app.Run();
```

Cấu hình bắt buộc (validate-on-start): `Jwt:SigningKey` (≥32 byte), `Jwt:Issuer`, `Jwt:Audience`. Tùy chọn: `PasswordHashing:*`, `RefreshToken:RefreshTokenDays`, `Security:AllowedCorsOrigins`, `Security:ContentSecurityPolicy`, `Security:HstsMaxAgeSeconds`.

## Lưu ý vận hành

- **HTTPS redirect KHÔNG do app làm** — reverse proxy terminate TLS (tránh redirect loop sau proxy). App tự phục vụ TLS mới thêm `UseHttpsRedirection`.
- **`xmin` concurrency là Npgsql-only** — `ResortQrDbContext` chỉ gắn `xmin` khi provider là Npgsql (`Database.IsNpgsql()`), nên test/chạy trên SQLite không vỡ. ⚠️ Npgsql EF Core 10 đã **bỏ helper `UseXminAsConcurrencyToken()`**; base map THỦ CÔNG `RowVersion → xmin` (type `xid`, DB-generated, concurrency token) — verify từ assembly thật (xem ai-notes DEV-020).
- **Test persistence Docker-free bằng SQLite in-memory** — DB quan hệ THẬT, dùng cho hành vi provider-agnostic (audit, xóa mềm, transaction, atomic consume, snake_case). Những thứ **Postgres-specific** (xmin runtime, partial unique index, race đa-connection thật) vẫn cần **Testcontainers/PostgreSQL** (hoãn tới khi có Docker — ai-notes TK-036).

## Trạng thái
- ✅ Build 0 warning/0 error (`TreatWarningsAsErrors`); Unit + Architecture + Integration (HTTP + SQLite persistence) xanh.
- ✅ EF persistence (DbContext/Repository/UoW/RefreshTokenStore atomic) + `/health/ready` + 409 concurrency mapping: đã hiện thực + test bằng SQLite.
- ⏳ Testcontainers/PostgreSQL (xmin runtime, partial index, race đa-connection) + migration Npgsql thực tế: cần Postgres/Docker.

---

## Bước tiếp theo — dựng nghiệp vụ Resort QR

Sau khi đổi tên, **không tách "base" vs "app" nữa** — nghiệp vụ resort thêm THẲNG vào các project `ResortQr.*` hiện có (những phần nền như Result/Entity/EF/auth đã nằm sẵn trong đó). Theo spec `.kiro/specs/resort-qr-portal/` (design/backend + tasks.md):

```
resort-qr/src/
  ResortQr.SharedKernel/    # (nền) Result/Error/Entity/Guard — thường không sửa
  ResortQr.Domain/          # + entity nghiệp vụ: Resort, ResortSettings, AppUser, Room, RoomQrToken,
                            #   GuestSession, GuestVisit... (kế thừa AuditableEntity/ISoftDeletable)
  ResortQr.Application/     # (đã có Identity) + use case resort: resolve QR, chat, housekeeping, rules, faq + validator
  ResortQr.Infrastructure/  # + AppDbContext : ResortQrDbContext (DbSet + IEntityTypeConfiguration + partial index)
                            #   + IUserAuthStore impl + migration
  ResortQr.Api/             # (đã có /auth) + endpoint nghiệp vụ resort; host wire AddDbContext + AddResortQrPersistence
```

Trình tự đề xuất (design-first, có DB sau):
1. `ResortQr.Domain`: khai entity nền (`04-data-model` §3) — chưa cần DB.
2. `ResortQr.Infrastructure`: `AppDbContext : ResortQrDbContext` + `IEntityTypeConfiguration<>` mỗi entity (partial unique index `04` §5/§7) + `IUserAuthStore` impl.
3. `ResortQr.Api`: `AddDbContext<AppDbContext>(UseNpgsql + UseSnakeCaseNamingConvention)` + `AddResortQrPersistence<AppDbContext>()` + endpoint resort.
4. Migration + seed (`04` §6) — chạy khi có Postgres/Docker.
5. Test: unit (use case) + integration SQLite (Docker-free) + Testcontainers (Postgres-specific) khi có Docker.

> Đã sẵn để dùng ngay: `AddResortQrPersistence<TContext>()`, `ResortQrDbContext` (audit/soft-delete/xmin-Npgsql/snake_case), `EfUnitOfWork`/`EfRepository<T>`, `EfRefreshTokenStore` atomic, auth flow, ProblemDetails, health, security headers, rate limit, localization resolver.