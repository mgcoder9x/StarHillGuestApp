# 15 — Configuration & Options (fail-fast, tách secret, rõ thứ tự ưu tiên)

> **File authoritative cho:** mô hình cấu hình của base — nguồn cấu hình & thứ tự ưu tiên, strongly-typed Options, **validate-on-startup (fail-fast)**, tách secret, và quan hệ giữa `appsettings` (hạ tầng, tĩnh) với `ResortSettings` (DB, runtime).
>
> **Vì sao quan trọng (bản chất):** reference nhét connection string + mật khẩu `123` thẳng vào `appsettings.json` (E11) và không validate gì lúc khởi động. Sản phẩm thương mại phải: (1) **không** chạy với cấu hình sai/thiếu secret (fail-fast, không "chạy tạm" với default nguy hiểm); (2) **không** commit secret vào repo; (3) rõ ràng giá trị nào tĩnh (hạ tầng) vs giá trị nào admin chỉnh runtime.

## 1. Nguồn cấu hình & thứ tự ưu tiên (precedence)

ASP.NET Core hợp nhất theo thứ tự (sau ghi đè trước):

```
appsettings.json  <  appsettings.{Environment}.json  <  Environment Variables  <  User-Secrets (chỉ Development)
```

- **appsettings.json**: giá trị **không nhạy cảm** + default an toàn (chỉ placeholder cho secret).
- **appsettings.{Env}.json**: khác biệt theo môi trường (Development/Staging/Production), không chứa secret prod.
- **Environment Variables / Secret store**: **nơi DUY NHẤT** chứa secret ở Staging/Prod (`ConnectionStrings__Postgres`, `Jwt__SigningKey`, `Seed__AdminPassword`). ⚠️ nguồn cụ thể tùy hạ tầng — TK-007.
- **User-Secrets**: chỉ Dev (không commit).

> **Không** commit secret vào bất kỳ `appsettings*.json` nào (sửa gốc E11). Repo chỉ có placeholder + `.env.sample`/tài liệu tên biến.

## 2. Cấu trúc cấu hình (strongly-typed Options)

Mỗi nhóm cấu hình là một **Options class** (POCO) bind từ một section, đặt ở `Application`/`Infrastructure` tùy phạm vi:

```jsonc
// appsettings.json (KHÔNG chứa secret — chỉ cấu trúc + default an toàn)
{
  "ConnectionStrings": { "Postgres": "" },          // set qua env ở prod
  "Jwt": {
    "Issuer": "resort-qr",
    "Audience": "resort-qr-admin",
    "SigningKey": "",                                // set qua env; fail-fast nếu rỗng ở prod
    "AccessTokenMinutes": 15,                        // 5..60
    "ClockSkewSeconds": 60
  },
  "RefreshToken": { "ExpiryDays": 30 },              // 7..90
  "Guest": { "CookieName": "shq_guest", "SessionCookieDays": 60 },
  "PasswordHashing": {                               // Argon2id — tune theo OWASP + benchmark (TK-026)
    "MemoryKiB": 0, "Iterations": 0, "Parallelism": 0
  },
  "RateLimitDefaults": {                             // fallback khi ResortSettings null (§5)
    "ResolvePerMinutePerIp": 20,
    "GuestWritePerMinutePerSession": 10
  },
  "OperationalDefaults": {                           // fallback cho ResortSettings (Req 14.5)
    "PortalWindowMinutes": 30, "VisitIdleExpiryHours": 24,
    "MaxMessageLength": 2000
  },
  "Sweeper": { "IntervalMinutes": 5, "BatchSize": 500 },
  "ReverseProxy": { "KnownProxies": [] },            // IP proxy thật (§03 §8, TK-027)
  "Cors": { "AllowedOrigins": [ "https://portal.starhill.local" ] },
  "HealthCheck": { "ReadyDbTimeoutSeconds": 5 },
  "Seed": { "ResortName": "Star Hill", "AdminEmail": "", "AdminPassword": "" }  // secret qua env
}
```

```csharp
public sealed class JwtOptions
{
    public const string Section = "Jwt";
    [Required] public string Issuer { get; init; } = "";
    [Required] public string Audience { get; init; } = "";
    [Required, MinLength(32)] public string SigningKey { get; init; } = "";   // ≥256-bit
    [Range(5, 60)] public int AccessTokenMinutes { get; init; } = 15;
    [Range(0, 300)] public int ClockSkewSeconds { get; init; } = 60;
}
```

## 3. Validate-on-startup (fail-fast) — bắt buộc

Đăng ký Options với **validate DataAnnotations + ValidateOnStart**; thêm `IValidateOptions` cho ràng buộc chéo/secret:

```csharp
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.Section))
    .ValidateDataAnnotations()
    .Validate(o => o.AccessTokenMinutes <= 60, "AccessTokenMinutes vượt trần")
    .ValidateOnStart();     // ← app TỪ CHỐI khởi động nếu cấu hình sai

// Secret bắt buộc theo môi trường
builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtProdSecretValidator>();
```

```csharp
// Ở Production: SigningKey rỗng → app KHÔNG khởi động (thay vì chạy với khóa yếu/rỗng)
public sealed class JwtProdSecretValidator(IHostEnvironment env) : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions o)
        => env.IsProduction() && string.IsNullOrWhiteSpace(o.SigningKey)
           ? ValidateOptionsResult.Fail("Jwt:SigningKey bắt buộc ở Production (đặt qua env/secret).")
           : ValidateOptionsResult.Success;
}
```

**Danh sách fail-fast tối thiểu (Production):** `ConnectionStrings:Postgres`, `Jwt:SigningKey`, `Seed:AdminPassword` (khi seed), `ReverseProxy:KnownProxies` (nếu chạy sau proxy — cảnh báo/âm nếu rỗng), `PasswordHashing` params > 0.

> **Lý do fail-fast (chính xác):** cấu hình sai phát hiện **lúc khởi động** rẻ và an toàn hơn nhiều so với phát hiện lúc chạy (khóa JWT rỗng → token ai cũng giả được; connection rỗng → chết giữa request). "Chạy tạm với default" là phản mẫu bảo mật cho sản phẩm thương mại.

## 4. Dùng Options (không đọc IConfiguration rải rác)

- Inject `IOptions<T>` (singleton) / `IOptionsSnapshot<T>` (scoped, reload theo request) / `IOptionsMonitor<T>` (singleton, reload runtime) tùy nhu cầu.
- **Cấm** đọc `IConfiguration["..."]` rải rác trong use case (khó test, mất kiểu, mất validate). Cấu hình vào qua Options port.
- Ánh xạ vào tính tất định: use case cần giá trị vận hành lấy từ Options/ResortSettings, không hardcode.

## 5. `appsettings` (tĩnh) vs `ResortSettings` (DB, runtime) — phân định rõ

| | `appsettings`/Options | `ResortSettings` (DB) |
|---|---|---|
| Bản chất | hạ tầng/tĩnh, đổi = redeploy/restart | vận hành, **admin sửa runtime** qua UI (Req 14.4) |
| Ví dụ | SigningKey, ConnectionString, KnownProxies, CORS, Argon2 params, default an toàn | cờ ack FAQ/Chat/HK, PortalWindowMinutes, VisitIdleExpiryHours, GuestWebBaseUrl, MaxMessageLength, ngưỡng rate limit |
| Precedence cho giá trị vận hành | **fallback cuối** | **ưu tiên cao nhất**; nếu null → dùng appsettings/hằng số (Req 14.5) |

**Quy tắc phân giải giá trị vận hành (ví dụ PortalWindowMinutes):**
```
value = ResortSettings.PortalWindowMinutes            // DB, admin sửa
     ?? OperationalDefaults.PortalWindowMinutes       // appsettings
     ?? HardConstant.PortalWindowMinutes              // hằng số an toàn cuối cùng
```
> Rate limit cũng theo thứ tự này (Req 13.5): ResortSettings → RateLimitDefaults(appsettings) → hằng số.

## 6. Truy vết
- **Validates: Requirements 11.1, 12.5, 13.5, 14.1, 14.4, 14.5, 15.x**
- Liên quan: `10` §1 (secrets), `03` §5/§8 (rate limit/proxy), TK-004/007/026/027.
