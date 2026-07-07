# Foundation — Base backend sạch (tái dùng cho mọi dự án)

Đây là **base domain-agnostic** (không chứa nghiệp vụ nào). Mục đích: copy ra làm điểm khởi đầu cho bất kỳ dự án .NET nào (ví dụ Resort QR), rồi thêm nghiệp vụ ở tầng Domain/Application/Api.

> ⚠️ **Đây là BASE/TEMPLATE, KHÔNG phải một API chạy độc lập được ngay.** App cụ thể **BẮT BUỘC** phải cung cấp phần persistence (xem "Điều app phải cung cấp"). Thiếu → DI **fail-fast** khi khởi động (đúng ý đồ: báo lỗi sớm thay vì chạy nửa vời).

## Cấu trúc (Clean Architecture / modular monolith)

```
foundation/
  Foundation.slnx
  Directory.Build.props        # net10, Nullable, TreatWarningsAsErrors, analyzers
  Directory.Packages.props     # Central Package Management (pin version một chỗ)
  global.json                  # pin .NET SDK 10.0.301 (rollForward latestFeature)
  .editorconfig
  src/
    Foundation.SharedKernel/   # Result/Error (+Details), base Entity (UUIDv7, chặn Guid.Empty), marker DI, Guard
    Foundation.Domain/         # entity riêng theo app (base để trống)
    Foundation.Application/    # use case + port + validation decorator (IUseCase/ICommandUseCase) + Identity flows
    Foundation.Infrastructure/ # Argon2id hasher, JWT service, token gen, SHA-256 refresh hasher, HtmlSanitizer, DI (Scrutor)
    Foundation.Api/            # composition root: Options fail-fast, ProblemDetails, JWT Bearer + policy, security headers/CORS, endpoint /auth/*
  tests/
    Foundation.UnitTests/          # SharedKernel, auth, DI, validation
    Foundation.IntegrationTests/   # HTTP thật (WebApplicationFactory) + (Testcontainers — hoãn tới khi có Docker)
    Foundation.ArchitectureTests/  # NetArchTest: enforce dependency rule
```

## Đã có sẵn trong base (generic, đã test)

- **SharedKernel:** `Result`/`Result<T>`/`Error` (kèm field-errors), base `Entity` (UUIDv7, bất biến Id≠Guid.Empty), `Guard`, marker DI, `ConcurrencyConflictException` (trung lập, Api map 409).
- **Auth:** Argon2id (PHC + rehash-on-login), JWT HS256 (clock inject, ClockSkew=0), refresh token **rotation + reuse-detection + consume nguyên tử** (chống race), login/refresh/logout use case.
- **Persistence (EF Core 10 + PostgreSQL/Npgsql):** `FoundationDbContext` base (audit tự động, xóa mềm filter + Delete→Modified, concurrency `xmin` **Npgsql-only có điều kiện**, snake_case), `EfRepository<T>`, `EfUnitOfWork` (điểm ghi + transaction, chuyển `DbUpdateConcurrencyException`→`ConcurrencyConflictException`), entity `RefreshTokenRecord` + `EfRefreshTokenStore` (**`TryConsumeAsync`/`RevokeFamilyAsync` atomic bằng `ExecuteUpdateAsync`**). Wire qua `AddFoundationPersistence<TContext>()`.
- **Cross-cutting:** DI theo convention (Scrutor + guard xung đột lifetime), Options **validate-on-start** (fail-fast), ProblemDetails (Req 4, gồm 401/403 auth + 409 concurrency), validation pipeline (FluentValidation decorator cho `IUseCase` + `ICommandUseCase`), HtmlSanitizer (allowlist), security headers (CSP/nosniff) + CORS allowlist + HSTS.
- **Endpoint mẫu:** `POST /auth/login|refresh|logout`, `GET /auth/me`. Access token ở body; refresh token cookie HttpOnly/Secure/SameSite=Strict, Path=/auth.
- **Health:** `/health/live` (liveness, không phụ thuộc DB) + `/health/ready` (readiness, chạy check gắn tag `ready` — DB connectivity do `AddFoundationPersistence` đăng ký).

## Điều app PHẢI cung cấp (base cung cấp hạ tầng generic — app cắm phần nghiệp vụ + DB)

Base cung cấp SẴN `EfUnitOfWork`/`EfRepository<T>`/`EfRefreshTokenStore` generic. App chỉ cần:

1. **`AppDbContext : FoundationDbContext`** — thêm `DbSet<>` cho entity nghiệp vụ của mình; ctor nhận `(DbContextOptions<AppDbContext>, IDateTimeProvider, ICurrentUser)`.
2. **Đăng ký DbContext + wire persistence** trong Program.cs:
   ```csharp
   builder.Services.AddDbContext<AppDbContext>(opt => opt
       .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
       .UseSnakeCaseNamingConvention());          // BẮT BUỘC để cột/bảng snake_case
   builder.Services.AddFoundationPersistence<AppDbContext>(); // UoW + RefreshTokenStore + health "ready"
   ```
3. **`IUserAuthStore`** — tra user theo email/id, cập nhật hash mật khẩu (map entity user của app → `AuthenticatedUser`). Auto-đăng ký qua marker `IScopedService`.
4. **Design-time factory + migration** — `dotnet ef migrations add InitialCreate` (chạy KHÔNG cần DB đang chạy). Migration tự gộp bảng `refresh_token` (từ config của base) + bảng nghiệp vụ của app.
5. **Validator** (`AbstractValidator<T>`) cho input use case — `AddFoundation` tự đăng ký nếu nằm trong assembly được quét.

> **Vì sao vẫn "fail-fast":** thiếu `AddDbContext`/`AddFoundationPersistence` → resolve `IUnitOfWork`/`IRefreshTokenStore`/`FoundationDbContext` sẽ lỗi. Persistence nền được wire **tường minh** (không auto-scan) để phụ thuộc DbContext hiện rõ, tránh đăng ký mập mờ.

## Cách dùng trong host (Program.cs)

```csharp
builder.Services.AddFoundation(builder.Configuration); // Options + Scrutor DI + Auth/AuthZ + Security
// app tự thêm: EF DbContext + IUnitOfWork + IUserAuthStore + IRefreshTokenStore + validators...
var app = builder.Build();
app.UseFoundation();                 // Exception→ProblemDetails → HSTS → security headers → CORS → AuthN → AuthZ
app.MapFoundationAuthEndpoints();
app.Run();
```

Cấu hình bắt buộc (validate-on-start): `Jwt:SigningKey` (≥32 byte), `Jwt:Issuer`, `Jwt:Audience`. Tùy chọn: `PasswordHashing:*`, `RefreshToken:RefreshTokenDays`, `Security:AllowedCorsOrigins`, `Security:ContentSecurityPolicy`, `Security:HstsMaxAgeSeconds`.

## Lưu ý vận hành

- **HTTPS redirect KHÔNG do app làm** — reverse proxy terminate TLS (tránh redirect loop sau proxy). App tự phục vụ TLS mới thêm `UseHttpsRedirection`.
- **`xmin` concurrency là Npgsql-only** — `FoundationDbContext` chỉ gắn `xmin` khi provider là Npgsql (`Database.IsNpgsql()`), nên test/chạy trên SQLite không vỡ. ⚠️ Npgsql EF Core 10 đã **bỏ helper `UseXminAsConcurrencyToken()`**; base map THỦ CÔNG `RowVersion → xmin` (type `xid`, DB-generated, concurrency token) — verify từ assembly thật (xem ai-notes DEV-020).
- **Test persistence Docker-free bằng SQLite in-memory** — DB quan hệ THẬT, dùng cho hành vi provider-agnostic (audit, xóa mềm, transaction, atomic consume, snake_case). Những thứ **Postgres-specific** (xmin runtime, partial unique index, race đa-connection thật) vẫn cần **Testcontainers/PostgreSQL** (hoãn tới khi có Docker — ai-notes TK-036).

## Trạng thái
- ✅ Build 0 warning/0 error (`TreatWarningsAsErrors`); Unit + Architecture + Integration (HTTP + SQLite persistence) xanh.
- ✅ EF persistence (DbContext/Repository/UoW/RefreshTokenStore atomic) + `/health/ready` + 409 concurrency mapping: đã hiện thực + test bằng SQLite.
- ⏳ Testcontainers/PostgreSQL (xmin runtime, partial index, race đa-connection) + migration Npgsql thực tế: cần Postgres/Docker.

## Cách tái dùng
Copy `foundation/` → đổi tên project/namespace theo dự án (hoặc giữ `Foundation.*` làm lib tham chiếu) → thêm nghiệp vụ vào Domain/Application/Api + tạo `AppDbContext : FoundationDbContext` + `AddFoundationPersistence<AppDbContext>()` + migration.
