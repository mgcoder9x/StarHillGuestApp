# Foundation — Đánh giá kiến trúc cấp chuyên gia (bản chốt, đã verify)

> **Phạm vi:** `foundation/` (base backend `Foundation.*`).
> **Phương pháp:** mọi kết luận dưới đây được **kiểm chứng trực tiếp bằng code thật** trong `foundation/src` + chạy test, KHÔNG suy đoán. Mỗi finding ghi rõ **bằng chứng** (file + symbol) và phân biệt **[FACT — đã verify trong code]** vs **[ĐÁNH GIÁ — khuyến nghị kiến trúc]**.
> **Trạng thái build/test đã xác nhận:** `dotnet test` → **112 passed, 1 skipped** (Unit 76 + Architecture 5 + Integration 31 + 1 skip). Build 0 warning/0 error (`TreatWarningsAsErrors=true`).

---

## 0. Cách đọc

- **Mức độ:** 🔴 Cao (chặn mục tiêu "base cho dự án lớn") · 🟡 Trung bình (nợ kỹ thuật, nên sửa trước khi scale) · 🟢 Thấp (đánh bóng).
- Finding đánh số `F#` để tham chiếu chéo (mục 7 có ma trận finding → file).

---

## 1. Kết luận điều hành

Nền này **không phải code demo**: ranh giới layer là thật (enforce bằng project reference + architecture test), bảo mật auth nghiêm túc (Argon2id, JWT, refresh rotation + reuse-detection nguyên tử), có Options validate-on-start, ProblemDetails, structured logging, test đa tầng. Đây là **khởi đầu tốt**.

Nhưng để xứng danh **"base cực chất cho dự án lớn / thư viện nền dùng lại nhiều dự án"**, nó còn **3 vấn đề gốc**:

1. **Khủng hoảng định danh sản phẩm** (F1): `Foundation.Api` vừa là *base library* vừa là *app host chạy được* (có `Program.cs` + endpoint mẫu). Chưa quyết định rõ nó là library hay template.
2. **Rò nghiệp vụ app cụ thể vào base** (F2, F3, F4): có rò ở **tầng code thật** (không chỉ comment) — `PathMasker` hardcode path guest, `FoundationAuthExtensions` hardcode role `Admin/Staff`.
3. **Kiến trúc ngữ nghĩa folder chưa đủ chín để scale nhiều nghiệp vụ** (F12) + **thiếu các trục nền của hệ lớn** (F13): domain events/outbox, module boundary, API versioning, endpoint/module discovery, mô hình authz mở rộng.

**Chấm điểm (thẳng thắn):**

| Mục tiêu | Điểm | Ghi chú |
|---|---|---|
| Template copy-ra-làm-app-nhỏ | **8/10** | Dùng ngay được, chất lượng tốt. |
| Thư viện nền domain-agnostic dùng nhiều dự án | **7/10** | Vướng rò nghiệp vụ + định danh library. |
| Platform nền cho **hệ thống lớn / modular monolith** | **6/10** | Thiếu module boundary, building blocks (events/outbox/versioning), semantic folder. |
| Sẵn sàng **cắm công nghệ mới sau này** (RabbitMQ/Elasticsearch/external-auth Zalo·Google/Gmail/Redis) **không phá lõi** | **4/10** | Chưa có extension architecture (ports/adapters/registry/optional-packages/outbox). Xem §10. |

> **Danh mục finding đầy đủ: F1–F29.** §3 (F1–F13) = hiện trạng lõi. §9 (F14–F23) = kiểm toán platform-level. §10 (F24–F29) = kiến trúc mở rộng công nghệ (extension architecture).

> Khuyến nghị chiến lược: **KHÔNG xây nghiệp vụ lớn trực tiếp trên cấu trúc folder hiện tại** trước khi xử lý F1–F3 và định hình lại theo mục 4. Phần kỹ thuật tốt (Result/Auth/EF/UoW/validation) **giữ nguyên**, chỉ tái cấu trúc ranh giới + ngữ nghĩa.

---

## 2. Điểm mạnh (đã verify)

- **[FACT] Dependency rule enforce 2 tầng.** Tĩnh ở `.csproj` (Application KHÔNG ref EF/ASP.NET; Api là composition root; Infrastructure implement port) + test `DependencyRuleTests` có **negative control** (khẳng định vi phạm sẽ bị bắt). Đây là điểm senior thực sự.
- **[FACT] Result/Error trung lập HTTP.** `ErrorType` → HTTP status map ở Api (`ErrorTypeToHttp`), Application không biết status code. Catalog lỗi ổn định làm hợp đồng BE↔FE.
- **[FACT] Validation decorator qua Scrutor** (`DependencyInjectionExtensions.AddFoundationServices`): `TryDecorate` bọc `IUseCase<,>`/`ICommandUseCase<>` → use case không gọi validator thủ công.
- **[FACT] Refresh rotation có tư duy bảo mật tốt.** `RefreshTokenUseCase` + `EfRefreshTokenStore.TryConsumeAsync` dùng `ExecuteUpdateAsync` sinh `UPDATE ... WHERE id=@id AND revoked_at IS NULL` **nguyên tử ở DB** (không read-then-write); reuse-detection thu hồi cả family; token raw chỉ ở cookie, DB lưu SHA-256 hash.
- **[FACT] Options validate-on-start** cho Jwt/PasswordHashing/RefreshToken/RateLimit/Security.
- **[FACT] Test đa tầng thật:** SQLite thật cho persistence convention, `WebApplicationFactory` cho auth/security/observability, NetArchTest cho hướng phụ thuộc.

---

## 3. Phát hiện (findings)

### F1 — 🔴 Khủng hoảng định danh: base library hay app host? — [FACT]
**Bằng chứng:** `Foundation.Api/Program.cs` tồn tại và là một **host chạy được** (`WebApplication.CreateBuilder` → `AddFoundation` → `app.Run()`), kèm `public partial class Program;` (cho WebApplicationFactory) và endpoint mẫu `Foundation.Api/Endpoints/AuthEndpoints.cs`.

**Vấn đề:** một base/library tái dùng KHÔNG nên tự chứa host `Program.cs` + endpoint nghiệp vụ mẫu. Hiện `Foundation.Api` mang **hai vai** mâu thuẫn:
- Nếu là **copy-template**: OK (copy cả cây rồi sửa).
- Nếu là **thư viện nền** (giữ `Foundation.*` làm package, app viết assembly riêng): host + endpoint mẫu là thừa và gây nhầm boundary.

**Tác động khi scale:** nhiều app tham chiếu chung một base-library nhưng base lại "ôm" một host cụ thể → không thể versioning/đóng gói NuGet sạch; endpoint mẫu dễ bị coi là "chuẩn" và bị copy lệch.

**Fix:** tách vai:
- `Foundation.*` (SharedKernel/Application/Infrastructure/Api-**extensions**) = **library thuần**, KHÔNG `Program.cs`, KHÔNG endpoint nghiệp vụ. `Foundation.Api` chỉ chứa *extension* (`AddFoundation`/`UseFoundation`/`MapFoundation*`) + middleware/pipeline.
- Tạo project **Host** riêng (vd `StarHill.Api`) chứa `Program.cs`, hoặc để `Program.cs` trong app-template tách biệt. AuthEndpoints mẫu chuyển thành **tài liệu/sample project**, không nằm trong library.

---

### F2 — 🔴 Rò nghiệp vụ tầng CODE: `PathMasker` hardcode path guest — [FACT]
**Bằng chứng:** `Foundation.Api/Observability/PathMasker.cs`:
```csharp
private const string ResolvePrefix = "/api/guest/resolve/";
private const string ShortPrefix   = "/r/";
```
Đây là **URL của app Resort QR** nằm cứng trong base "domain-agnostic". Không phải comment — là **hằng số chi phối logic**.

**Tác động khi scale:** app khác (Bookings/Orders) có path nhạy cảm khác → base không che đúng, hoặc lập trình viên sửa thẳng base (vỡ tính tái dùng).

**Fix (root):** đảo ngược phụ thuộc qua options:
```csharp
// Foundation.Api/Observability/ObservabilityOptions.cs
public string[] MaskedPathPrefixes { get; set; } = [];   // app cấu hình: ["/api/guest/resolve/", "/r/"]
```
`PathMasker` nhận danh sách prefix từ options (app đăng ký), base KHÔNG biết path cụ thể.

---

### F3 — 🔴 Rò nghiệp vụ tầng CODE: `FoundationAuthExtensions` hardcode role `Admin/Staff` — [FACT] (phát hiện MỚI, nhận xét trước chưa nêu)
**Bằng chứng:** `Foundation.Api/Security/FoundationAuthExtensions.cs`:
```csharp
public const string RequireAdminPolicy = "RequireAdmin";
public const string RequireStaffPolicy = "RequireStaff";
...
.AddPolicy(RequireAdminPolicy, p => p.RequireRole("Admin"))
.AddPolicy(RequireStaffPolicy, p => p.RequireRole("Staff", "Admin"));
```
**Role là khái niệm nghiệp vụ.** `Admin/Staff` là mô hình phân quyền của Resort QR — KHÔNG phải khái niệm nền chung. Một base thật sự generic không được giả định app có đúng 2 role này.

**Tác động khi scale:** app có mô hình quyền khác (Owner/Manager/Cashier, hoặc permission-based, hoặc multi-tenant membership) → policy nền sai ngữ nghĩa; team lớn phải "lách" base.

**Fix (root):** base chỉ cung cấp **cơ chế** (JWT bearer, `ICurrentUser`, ProblemDetails cho 401/403, helper tạo policy theo role/claim). App **tự khai** policy + role của mình:
```csharp
// Base cung cấp helper generic:
services.AddFoundationAuthCore();                     // bearer + ICurrentUser + 401/403 problem
// App tự định nghĩa:
services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
```

---

### F4 — 🟡 Rò nghiệp vụ tầng COMMENT (nhiều file) — [FACT]
**Bằng chứng (comment tham chiếu app cụ thể trong base):**
- `SecurityHeadersMiddleware.cs`: "Áp cho toàn bộ bề mặt (**guest/admin/hub**)".
- `SecurityOptions.cs`: "Origin được phép CORS (**guest, admin**)".
- `RateLimitOptions.cs`: "**Resort** thêm policy riêng (**resolve/guest-write** partition theo **GuestSessionId**)".
- `CommonErrors.cs`: "Mã lỗi nghiệp vụ riêng (**vd của resort**)".

**Vấn đề:** không phải coupling biên dịch, nhưng là **ô nhiễm ngữ nghĩa** — base "biết" về app. Với thư viện dùng lại, comment nên nói ở mức khái niệm ("bề mặt công khai/nội bộ", "app-specific policy"), không nêu tên app.

**Fix:** viết lại comment ở mức generic; chuyển ví dụ app-cụ-thể sang README/sample.

---

### F5 — 🟡 Ranh giới transaction của refresh rotation chưa "một điểm ghi" — [FACT]
**Bằng chứng:** `EfRefreshTokenStore.TryConsumeAsync`/`RevokeFamilyAsync` dùng `ExecuteUpdateAsync` → **ghi DB NGAY** (tự commit nếu không có transaction bao ngoài). Trong `RefreshTokenUseCase`, thứ tự là: `TryConsumeAsync` (ghi ngay) → `AddAsync` (stage) → `SaveChangesAsync` (ghi lần 2).

**Hệ quả chính xác:** nếu `SaveChangesAsync` (insert token mới) **fail SAU khi** `TryConsumeAsync` đã thành công → token cũ đã bị revoke nhưng token mới chưa được lưu → **user mất refresh token, phải đăng nhập lại**.
- Mức độ thực tế: **thấp** (insert 1 dòng hiếm khi fail; hậu quả là re-login — degrade nhẹ, KHÔNG mất dữ liệu, KHÔNG lỗ hổng bảo mật).
- Nhưng về **nguyên tắc**: mâu thuẫn với claim "`IUnitOfWork` là điểm ghi DB DUY NHẤT" — `ExecuteUpdateAsync` ghi ngoài `SaveChanges`.

**Fix (root):** bọc rotation trong `IUnitOfWork.ExecuteInTransactionAsync(...)` (khi có transaction mở, `ExecuteUpdateAsync` enlist vào đúng transaction đó → all-or-nothing), HOẶC thêm store method nguyên tử `RotateAsync(oldId, newRecord)` gói cả consume + insert.

---

### F6 — 🟡 DI convention: exclusion theo namespace-string dễ vỡ + thiếu overload cho library — [FACT]
**Bằng chứng:** `AddFoundationServices` loại persistence khỏi auto-scan bằng
`AddClasses(c => c.AssignableTo<IScopedService>().NotInNamespaceOf<Persistence.FoundationDbContext>())`.

**Vấn đề 1 (fragility):** bất kỳ class nào **hard-require `DbContext`** BẮT BUỘC phải nằm **đúng namespace `*.Persistence`** mới bị loại; đặt sai chỗ → Scrutor auto-đăng ký → DI `ValidateOnBuild` **sập host** khi thiếu DbContext. Đây là bẫy ngầm dựa vào quy ước vị trí file, không phải marker tường minh.

**Vấn đề 2 (library):** overload `AddFoundationServices(params Assembly[] assemblies)` **có tồn tại**, NHƯNG entry-point `AddFoundation(configuration)` gọi `AddFoundationServices()` (mặc định chỉ quét assembly của foundation) và **KHÔNG có** `AddFoundation(configuration, params Assembly[] appAssemblies)`. → Nếu app giữ `Foundation.*` làm library và viết use case/validator ở assembly app riêng, `AddFoundation` **không quét** assembly app.
> Đính chính so với nhận xét trước: overload ở tầng `AddFoundationServices` đã có sẵn — đây là **bổ sung nhỏ** (phơi overload ở `AddFoundation`), không phải viết lại.

**Fix:**
- Thay exclusion namespace-string bằng **marker tường minh** (vd `IManualRegistration`/attribute) cho service hard-require DbContext.
- Thêm `AddFoundation(IConfiguration, params Assembly[] appAssemblies)` truyền tiếp xuống scan + `AddValidatorsFromAssemblies`.

---

### F7 — 🟡 "Fail-fast persistence" phụ thuộc môi trường — [FACT + framework behavior]
**Bằng chứng:** `Program.cs` comment: "Thiếu → DI báo lỗi khi khởi động (fail-fast)". Nhưng KHÔNG có startup-validator tường minh; việc fail lúc `builder.Build()` phụ thuộc `ServiceProviderOptions.ValidateOnBuild`/`ValidateScopes`.

**Sự thật framework:** .NET bật `ValidateOnBuild`/`ValidateScopes` **mặc định CHỈ ở Development**. Ở **Production** (mặc định), thiếu `IUnitOfWork`/`IRefreshTokenStore`/`IUserAuthStore` sẽ **không** fail ở `Build()` mà chỉ lộ **lúc resolve use case** (khi có request `/auth/login`) → KHÔNG phải fail-fast thật ở mọi môi trường.

**Fix:** thêm startup validator tường minh (vd `IHostedService`/`IStartupFilter` kiểm các port bắt buộc), hoặc `AddFoundationPersistence<TContext>()` set một marker và `AddFoundation` assert marker đó có mặt khi build.

---

### F8 — 🟢 SharedKernel rò khái niệm provider (`xmin`) — [FACT]
**Bằng chứng:** `Foundation.SharedKernel/Entities/Abstractions.cs`:
```csharp
/// <summary>Optimistic concurrency: map tới xmin của PostgreSQL (Npgsql).</summary>
public interface IConcurrencyAware { uint RowVersion { get; set; } }
```
Không rò **dependency** (chỉ `uint` + comment), nhưng **khái niệm provider** (`xmin`, Npgsql) đã xuất hiện trong kernel — nơi phải sạch nhất.

**Fix:** đổi tên ngữ nghĩa `IHasConcurrencyToken` (comment trung lập), để **Infrastructure** giữ tri thức "map `RowVersion` → `xmin` khi provider là Npgsql". Kernel không nói chữ `xmin`.

---

### F9 — 🟢 `IRepository.Query()` phơi `IQueryable` ra ngoài — [FACT]
**Bằng chứng:** `Application/Abstractions/Persistence/IRepository.cs` khai `IQueryable<TEntity> Query();` — trong khi chính comment của interface nói "tránh leaky IQueryable ra ngoài use case". Mâu thuẫn nội tại: use case có thể dựng query EF-specific → rò chi tiết provider + khó test.

**Fix:** bỏ `Query()` khỏi repository ghi; chuyển đọc phức tạp sang **query service/read-model** (CQRS-lite) trả DTO. Nếu cần, tách `IReadRepository` với method chuyên biệt thay vì phơi `IQueryable`.

---

### F10 — 🟢 Index `TokenHash` chưa `UNIQUE` — [FACT]
**Bằng chứng:** `RefreshTokenConfiguration.cs`: `builder.HasIndex(r => r.TokenHash).HasDatabaseName("ix_refresh_hash")` — KHÔNG `.IsUnique()`.

**Phân tích:** `TokenHash` = SHA-256 của token 256-bit CSPRNG → thực tế duy nhất; lookup refresh là `FirstOrDefault` (giả định ≤1). Thêm `UNIQUE` cho **bảo đảm cấp DB** (không bao giờ 2 record trùng hash) + tối ưu planner. An toàn.

**Fix:** `.IsUnique()` cho `ix_refresh_hash` (đổi tên → `ux_refresh_hash`).

---

### F11 — 🟡 Chưa test trên PostgreSQL thật (Testcontainers) — [FACT]
**Bằng chứng:** `IntegrationTestsPlaceholder.cs` — `[Fact(Skip = ...)]`, hoãn tới khi có Docker.

**Rủi ro chưa phủ:** `xmin` concurrency **runtime** (chỉ map khi provider Npgsql — SQLite test không chạy nhánh này), partial unique index Postgres, migration Npgsql thực tế, race đa-connection thật. Đây là những thứ SQLite in-memory **không** mô phỏng đúng.

**Fix:** thêm Testcontainers/PostgreSQL cho: round-trip `xmin` (2 update đồng thời → 409), partial index, migrate thật, race rotation đa-connection.

---

### F12 — 🟡 Ngữ nghĩa folder chưa chín để scale — [ĐÁNH GIÁ, có cơ sở FACT]
**Bằng chứng cấu trúc hiện tại:** `Application/Abstractions`, `Application/Common`, `Application/Identity`, `Api/Endpoints`, `Api/Security`, `Infrastructure/Security`.

**Ba mùi tên:**
1. **Tên "thùng chứa" quá rộng:** `Abstractions`, `Common`, `Endpoints` → khi lớn lên trở thành "ngăn kéo linh tinh".
2. **Tên chồng nghĩa:** `Api/Security` (HTTP: CORS/HSTS/JWT bearer/current user) vs `Infrastructure/Security` (crypto: Argon2/JWT issue/token/sanitizer) — người mới hỏi "JWT để ở Security nào?".
3. **Layer-first thuần** → một nghiệp vụ bị rải ngang 4–5 folder/project (sửa 1 feature phải nhảy nhiều nơi).

**Fix — bảng đổi tên ở mục 5.** Nguyên tắc: *tên folder phải trả lời "chứa lý do thay đổi gì"* + **vertical-by-feature bên trong mỗi layer**.

---

### F13 — 🟡 Thiếu các trục nền của hệ thống lớn — [ĐÁNH GIÁ]
Base hiện có ~55–60% mảnh ghép của một platform lớn. **Thiếu rõ nhất:**
- **Module boundary** (bounded context tách biệt) + **module/endpoint discovery** (hiện map thủ công trong `Program.cs`).
- **Domain events / Outbox-Inbox / integration events / background jobs** (nhất quán khi tách module + side-effect đáng tin cậy).
- **Application pipeline behaviors** đầy đủ: mới có validation; thiếu **transaction behavior**, **authorization behavior**, **idempotency**, **logging/tracing behavior**.
- **API versioning** + **OpenAPI grouping** + **authz model mở rộng** (permission/policy-based, không chỉ 2 role).
- **Postgres test thật** (F11).

---

## 4. Kiến trúc mục tiêu cho dự án lớn

Chuyển từ **layer-first template** sang **modular monolith + vertical slice + clean boundary**:

```
src/
  BuildingBlocks/                 # KHÔNG biết nghiệp vụ — nền dùng chung
    BuildingBlocks.Domain/        # Entity, ValueObject, DomainEvents, Rules, Results, Guard
    BuildingBlocks.Application/   # Ports (Data/Time/Security/Messaging), Behaviors (Validation/Transaction/Logging), UseCase, Paging
    BuildingBlocks.Infrastructure/# Persistence base, Messaging, Caching, Cryptography, Observability
    BuildingBlocks.Api/           # Errors(ProblemDetails), Authentication(cơ chế), HttpSecurity, Versioning, OpenApi, EndpointRouting
  Modules/                        # Mỗi module = 1 bounded context, tự đủ 4 layer
    Identity/  { Identity.Domain, Identity.Application, Identity.Infrastructure, Identity.Api }
    Rooms/     { Rooms.Domain, Rooms.Application, Rooms.Infrastructure, Rooms.Api }
    Bookings/  { ... }
  Host/
    StarHill.Api/                 # CHỈ ráp: DI, pipeline, endpoint/module discovery, config, observability
      Program.cs
```

**Quy tắc vàng:**
- **BuildingBlocks KHÔNG biết nghiệp vụ.** (⇒ xử lý F2/F3/F4: không path guest, không role Admin/Staff, không tên app.)
- **Module biết nghiệp vụ của chính nó** (Domain/Application/Infrastructure/Api riêng, vertical-by-feature bên trong).
- **Host chỉ ráp module, không chứa business.** (⇒ xử lý F1: host tách khỏi library.)

> Đây KHÔNG phải bỏ Clean Architecture — mà là **giữ layer ở tầng project, feature ở tầng folder**.

---

## 5. Bảng đổi tên folder (chốt)

| Hiện tại | Đổi thành | Lý do |
|---|---|---|
| `Application/Abstractions` | `Application/Ports` (con: `Persistence`, `Security`, `Time`, `Users`, `Html`) | "Ports" = cổng ra ngoài (Hexagonal), không phải "thùng interface". |
| `Application/Common` | tách `Application/UseCases` + `Application/Validation` + `Application/Paging` | tránh tên "Common" thùng rác. |
| `Application/Identity` | `Application/Authentication` (+ `Authorization` khi có) | Identity chỉ dùng khi domain thật là quản lý danh tính. |
| `Api/Endpoints` | folder theo capability: `Api/Authentication`, `Api/{Feature}` | tránh "bãi phẳng" khi nhiều nghiệp vụ. |
| `Api/Security` | `Api/Authentication` + `Api/HttpSecurity` | tách HTTP-auth khỏi headers/CORS. |
| `Infrastructure/Security` | `Infrastructure/Cryptography` + `Infrastructure/Tokens` + `Infrastructure/Sanitization` | rõ công nghệ, hết chồng nghĩa với Api/Security. |
| `Infrastructure/Persistence` | giữ, hoặc `Infrastructure/DataAccess` nếu muốn rõ | tùy chọn. |

---

## 6. Lộ trình refactor ưu tiên (không phá phần kỹ thuật tốt)

**P0 — gỡ rò nghiệp vụ khỏi base (bắt buộc trước khi scale):**
1. F2: `PathMasker` → `ObservabilityOptions.MaskedPathPrefixes` (app cấu hình).
2. F3: `FoundationAuthExtensions` → tách `AddFoundationAuthCore()` (cơ chế) khỏi policy `Admin/Staff` (đưa về app).
3. F4: làm sạch comment app-specific.
4. F1: tách `Program.cs` + endpoint mẫu ra khỏi library (Host riêng / sample project).

**P1 — làm chắc boundary/persistence trước khi có nhiều module:**
5. F5: rotation refresh trong transaction thật (hoặc `RotateAsync` nguyên tử).
6. F6: marker tường minh thay `NotInNamespaceOf` + overload `AddFoundation(config, appAssemblies)`.
7. F7: startup validator cho port bắt buộc (fail-fast mọi môi trường).
8. F11: Testcontainers/PostgreSQL (xmin/partial index/migration/race).
9. F10: `UNIQUE` cho `TokenHash`.

**P2 — nâng lên platform hệ lớn:**
10. F12: refactor folder theo mục 5 + vertical-by-feature.
11. F13: BuildingBlocks + Modules + Host (mục 4); thêm domain events/outbox, transaction/authorization/idempotency behaviors, API versioning, module/endpoint discovery, authz model mở rộng.
12. F8/F9: rename `IHasConcurrencyToken`; bỏ `IRepository.Query()` → read-model.

**Nguyên tắc an toàn:** mỗi bước phải giữ **build 0 warning + test xanh** (đã có architecture test làm lưới); refactor tên/di chuyển làm từng lát nhỏ, chạy test sau mỗi lát.

---

## 7. Ma trận truy vết finding → bằng chứng

| Finding | Mức | Bằng chứng (file · symbol) | Loại |
|---|---|---|---|
| F1 định danh library/host | 🔴 | `Foundation.Api/Program.cs`, `Endpoints/AuthEndpoints.cs` | FACT |
| F2 rò path guest | 🔴 | `Api/Observability/PathMasker.cs` · `ResolvePrefix="/api/guest/resolve/"`, `ShortPrefix="/r/"` | FACT |
| F3 rò role Admin/Staff | 🔴 | `Api/Security/FoundationAuthExtensions.cs` · `RequireRole("Admin")`, `("Staff","Admin")` | FACT |
| F4 rò comment app | 🟡 | `SecurityHeadersMiddleware.cs`, `SecurityOptions.cs`, `RateLimitOptions.cs`, `CommonErrors.cs` | FACT |
| F5 transaction rotation | 🟡 | `Infrastructure/Persistence/EfRefreshTokenStore.cs` · `ExecuteUpdateAsync`; `Application/Identity/RefreshTokenUseCase.cs` | FACT |
| F6 DI scan/overload | 🟡 | `Infrastructure/DependencyInjection/DependencyInjectionExtensions.cs` · `NotInNamespaceOf`; `Api/FoundationApiExtensions.cs` · `AddFoundation` | FACT |
| F7 fail-fast môi trường | 🟡 | `Foundation.Api/Program.cs` (comment) + hành vi mặc định .NET ValidateOnBuild | FACT |
| F8 xmin trong kernel | 🟢 | `SharedKernel/Entities/Abstractions.cs` · `IConcurrencyAware` | FACT |
| F9 IQueryable rò | 🟢 | `Application/Abstractions/Persistence/IRepository.cs` · `Query()` | FACT |
| F10 TokenHash chưa unique | 🟢 | `Infrastructure/Persistence/RefreshTokenConfiguration.cs` · `ix_refresh_hash` | FACT |
| F11 chưa test Postgres | 🟡 | `tests/Foundation.IntegrationTests/IntegrationTestsPlaceholder.cs` · `[Fact(Skip)]` | FACT |
| F12 semantic folder | 🟡 | cấu trúc `Application/Abstractions`, `Api/Security` vs `Infrastructure/Security` | ĐÁNH GIÁ |
| F13 thiếu building blocks | 🟡 | vắng domain events/outbox/versioning/module discovery | ĐÁNH GIÁ |

---

## 8. Ghi chú độ chính xác (không nói quá)

- Mọi finding gắn nhãn **[FACT]** đã được đọc trực tiếp trong `foundation/src` ở phiên này; các trích dẫn là **symbol/hằng số/tên method thật** (không bịa số dòng để tránh sai lệch).
- **F5** mức độ thực tế **thấp** (hậu quả = re-login, không mất dữ liệu/không lỗ hổng) — nêu vì tính **đúng đắn nguyên tắc**, không thổi phồng.
- **F6**: đã **đính chính** nhận xét trước — overload nhận assemblies *đã tồn tại* ở tầng `AddFoundationServices`; thiếu là ở tầng `AddFoundation`.
- **F7**: dựa trên hành vi mặc định .NET (`ValidateOnBuild`/`ValidateScopes` chỉ bật ở Development) — nên claim "fail-fast" đúng ở Dev, chưa đảm bảo ở Production.
- **F12/F13** là **khuyến nghị kiến trúc** (không phải lỗi biên dịch) — đánh dấu rõ để không lẫn với FACT.
- Test **112 passed / 1 skipped** là kết quả chạy thật, khớp với đánh giá "test đa tầng tốt; còn nợ Postgres thật (F11)".

---

## 9. Findings bổ sung — kiểm toán platform-level (F14–F23)

> Tất cả đã **verify trực tiếp trong `foundation/src`** ở phiên này.

### F14 — 🟡 Api kéo thẳng Infrastructure (packaging boundary) — [FACT]
**Bằng chứng:** `Foundation.Api/Foundation.Api.csproj`:
```xml
<ProjectReference Include="..\Foundation.Application\Foundation.Application.csproj" />
<ProjectReference Include="..\Foundation.Infrastructure\Foundation.Infrastructure.csproj" />
```
Với app nhỏ, Api là composition root nên ổn. Nhưng với **base library**, `BuildingBlocks.Api` (HTTP abstraction/middleware/extensions thuần) KHÔNG nên kéo luôn implementation infra (EF/Npgsql/Scrutor/JwtBearer). Hệ quả: bất kỳ ai tham chiếu package Api của base sẽ **luôn lôi theo cả EF/Npgsql**.

**Fix:** tách 3 gói: `BuildingBlocks.Api` (chỉ HTTP: middleware/ProblemDetails/auth mechanism/versioning/OpenApi), `BuildingBlocks.Infrastructure` (EF/JWT/crypto/adapter), **Host** = nơi DUY NHẤT gọi cả `AddApi()` + `AddInfrastructure()`. Api KHÔNG `ProjectReference` Infrastructure.

### F15 — 🔴 Exception handler log **raw path** (rò token qua log lỗi) — [FACT]
**Bằng chứng:** request-logging đã mask (`FoundationObservabilityExtensions` → `PathMasker.Mask(httpContext.Request.Path.Value)`), NHƯNG `ExceptionHandlingMiddleware` ghi **path thô**:
```csharp
Log.UnhandledException(_logger, ex, context.Request.Path);   // "... khi xử lý {Path}" — KHÔNG mask
```
**Hệ quả:** request thường được che, nhưng request lỗi 500 trên `/r/{token}` hoặc `/api/guest/resolve/{token}` sẽ **log token đầy đủ**. Đây là lỗ hổng observability/bảo mật thật (mất tác dụng của `PathMasker` đúng ở nơi nhạy cảm nhất).

**Fix:** dùng `PathMasker.Mask(context.Request.Path)` (hoặc `MaskedPathPrefixes` từ F2) trong log exception; tổng quát hơn: một helper mask dùng chung cho MỌI nơi log path.

### F16 — 🟡 Chưa thiết kế Reverse-proxy / ForwardedHeaders — [FACT]
**Bằng chứng:** grep `ForwardedHeaders|UseForwardedHeaders|X-Forwarded` trong `foundation/src` = **0 kết quả**. README nói TLS terminate ở reverse proxy; rate-limit partition theo `httpContext.Connection.RemoteIpAddress` (`FoundationRateLimitExtensions`).

**Hệ quả sau proxy:** `RemoteIpAddress` = IP của proxy → **mọi user chung một bucket rate-limit** (per-IP mất ý nghĩa). `Request.Scheme`/client IP/HSTS/log cũng có thể sai.

**Fix:** `ForwardedHeadersOptions` cấu hình được (KnownProxies/KnownNetworks/trusted), `UseForwardedHeaders` sớm trong pipeline; rate-limit partition theo IP thật sau khi resolve forwarded.

### F17 — 🟡 Refresh cookie `SameSite=Strict` vs CORS `AllowCredentials` — mâu thuẫn chiến lược — [FACT]
**Bằng chứng:** cookie refresh set `SameSite=SameSiteMode.Strict`, `Secure=true` (`AuthEndpoints.AppendRefreshCookie`); CORS `.WithOrigins(...).AllowCredentials()` (`FoundationSecurityExtensions`).

**Hệ quả:** nếu FE/BE **cross-site thật** (khác site), `SameSite=Strict` khiến refresh cookie **không được gửi** ở request cross-site → `/auth/refresh` fail. Nếu đổi sang `SameSite=None` để cross-site hoạt động thì **bắt buộc** có chiến lược CSRF.

**Fix:** cấu hình rõ **mode**: (a) same-origin/same-site SPA → `Strict/Lax` (hiện tại, không cần CSRF token); (b) cross-site SPA → `SameSite=None` + CSRF (double-submit/anti-forgery) + siết CORS. Base nên phơi `CookieSameSiteMode` qua options, không cứng `Strict`.

### F18 — 🟡 DI convention thiếu luật override/duplicate — [FACT]
**Bằng chứng:** `AddFoundationServices` dùng `.AsImplementedInterfaces().WithScopedLifetime()` (Scrutor `Add`, KHÔNG `TryAdd`). Guard hiện có CHỈ bắt xung đột **lifetime** (một class ≥2 marker), KHÔNG bắt **2 implementation cho cùng một port**.

**Hệ quả:** nếu có 2 impl cho một port, DI resolve **cái đăng ký cuối** (last-wins) — âm thầm, khó debug ở hệ lớn.

**Fix:** chính sách rõ — Foundation default dùng `TryAdd`; app override phải **explicit**; port single-implementation có **duplicate-guard** (fail-fast nếu >1 impl); port multi-implementation phải **marker rõ** (đăng ký `IEnumerable<T>` có chủ đích).

### F19 — 🟡 `RefreshTokenRecord` là persistence model nhưng nằm ở Application — [FACT]
**Bằng chứng:** `Application/Identity/RefreshTokenRecord.cs` tự ghi "(persistence-facing)"; EF map nó ở `Infrastructure/Persistence/RefreshTokenConfiguration.cs`. → Application đang **biết shape lưu trữ**.

**Fix:** hoặc đưa refresh token thành **entity trong `Identity.Domain`**, hoặc **giấu** persistence entity trong Infrastructure và Application chỉ nói qua **port nghiệp vụ** (`RotateRefreshTokenAsync`/`RevokeFamilyAsync`/`GetActiveTokenAsync`) — không lộ record shape.

### F20 — 🟡 Message tiếng Việt hardcode trong base + Title=Message (thiếu tầng localization) — [FACT]
**Bằng chứng:** `CommonErrors`/`AuthErrors` hardcode message tiếng Việt; `ProblemDetailsBuilder.Build` gán `Title = error.Message`. → thông điệp người-dùng bị đóng cứng tiếng Việt trong base, không tách được.

**Fix:** tách 3 tầng — **machine code ổn định** (`invalid_refresh_token`, đã có, tốt) · **default developer message** (English, neutral, trong base) · **localized user message** ở app/UI/localization layer (theo `Accept-Language`). ProblemDetails `title` nên là developer/neutral; UI dịch theo `code`.

### F21 — 🟢 `X-Correlation-Id` (header) ≠ `traceId` (ProblemDetails) — [FACT]
**Bằng chứng:** `CorrelationIdMiddleware` set response header `X-Correlation-Id` = (client-provided | `Activity.Current?.Id` | `TraceIdentifier`). `ProblemDetailsBuilder.TraceId` = `Activity.Current?.Id ?? TraceIdentifier` — **KHÔNG** đọc giá trị correlation-id đã chốt ở middleware.

**Hệ quả:** khi client **gửi** `X-Correlation-Id`, header response và `traceId` trong body lỗi **khác nhau** → khó đối soát khi vận hành.

**Fix:** thống nhất một nguồn — lưu correlationId đã resolve vào `HttpContext.Items`/feature, `ProblemDetailsBuilder.TraceId` đọc lại chính giá trị đó; hoặc trả **cả** `traceId` + `correlationId` trong ProblemDetails.

### F22 — 🟡 JWT chưa có key rotation (`kid`/key-ring) — [FACT]
**Bằng chứng:** `JwtOptions` chỉ có **một** `SigningKey` (HS256). Không có `kid`, không có active+previous keys.

**Hệ quả:** xoay khóa ký = **vô hiệu toàn bộ token đang sống** (không rolling). Nhiều service verify chung 1 secret đối xứng cũng khó quản.

**Fix:** thiết kế key-ring: `kid` trong header token · active key để ký + previous keys để verify (rolling rotation) · cân nhắc **asymmetric** (RS256/ES256) khi có nhiều service verify (chỉ phát tán public key).

### F23 — 🟡 `ICurrentUser` còn thuần role-based — [FACT]
**Bằng chứng:** `HttpContextCurrentUser` chỉ có `UserId`, `IsAuthenticated`, `Roles`, `IsInRole`. Không có `Permissions`/`TenantId`/`OrganizationId`/`SessionId`/custom claims.

**Hệ quả:** hệ lớn thường cần **permission-based** authz, **multi-tenant** (TenantId), gắn **SessionId** cho audit/revoke. Mở rộng sau sẽ đụng nhiều nơi nếu không chuẩn bị.

**Fix:** mở rộng `ICurrentUser` (hoặc `IClaimsExtractor`) hỗ trợ `Permissions`, `TenantId?`, `SessionId?` + điểm mở rộng claim tùy biến; authz chuyển sang **policy theo permission**, role chỉ là một nguồn quyền.

---

## 10. Extension Architecture — chuẩn bị cắm công nghệ mới KHÔNG phá lõi (F24–F29)

> **Câu hỏi thực dụng:** "sau này thêm Zalo/Google login, RabbitMQ, Elasticsearch, Gmail, Redis có sống khỏe không?"
> **Trả lời thẳng (đã kiểm chứng qua cấu trúc hiện tại):** *thêm được, nhưng CHƯA "thoải mái/đẹp"* — vì foundation hiện **chưa có extension architecture chính thức** (ports/adapters/registry/optional-packages/outbox). Nếu thêm bây giờ, khả năng cao sẽ **vá vào chỗ tiện nhất** (RabbitMQ chui vào Infrastructure chung, Elastic chui vào Persistence, Google/Zalo lẫn vào Api/Identity, Gmail chui vào Security) → vẫn chạy nhưng rối dần.
>
> **Nguyên tắc vàng:** *chuẩn bị bằng CONTRACT + EXTENSION POINT, KHÔNG kéo SDK/công nghệ cụ thể vào lõi.* Lõi chỉ biết "cần gửi message / cần search / cần gửi email / cần xác thực ngoài / cần cache / cần lưu file". Công nghệ cụ thể (RabbitMQ/Elastic/Gmail/Zalo/Google/Redis/S3) nằm ở **adapter/package riêng**, chọn bật ở **Host**.

### Mô hình 4 tầng cắm công nghệ
```
Core (BuildingBlocks)  →  chỉ CONTRACT + pipeline (IEventBus, ISearchIndex, IEmailSender, IExternalAuthProvider, ICacheStore...)
Adapters               →  công nghệ cụ thể (Messaging.RabbitMq, Search.Elasticsearch, Email.Gmail, ExternalAuth.Google/Zalo, Cache.Redis)
Modules                →  nghiệp vụ dùng contract (Identity/Rooms/Bookings...)
Host                   →  chọn adapter nào để BẬT (services.AddMessaging().AddRabbitMq(); ...)
```

### F24 — 🔴 Thiếu Extension Architecture / Adapter boundary — [ĐÁNH GIÁ]
Hiện có DI convention + vài extension method, nhưng **chưa có mô hình chuẩn** để thêm provider tùy chọn (external-auth, messaging, search, email, cache, storage). Cần chuẩn hoá cặp API `AddXxxCore()` (contract + behavior) + `AddYyyXxx(config)` (adapter cụ thể), ví dụ:
```
AddFoundationCore() · AddFoundationWeb() · AddFoundationPersistence()
AddMessagingCore()  · AddRabbitMqMessaging(cfg)
AddSearchCore()     · AddElasticsearchSearch(cfg)
AddEmailCore()      · AddGmailEmail(cfg) / AddSmtpEmail(cfg)
AddExternalAuthCore() · AddGoogleExternalAuth(cfg) · AddZaloExternalAuth(cfg)
AddCacheCore()      · AddRedisCache(cfg)
AddStorageCore()    · AddS3Storage(cfg)
```

### F25 — 🔴 Thiếu Messaging + Outbox/Inbox boundary — [ĐÁNH GIÁ]  (chuẩn bị cho RabbitMQ)
**Không** cho use case gọi RabbitMQ trực tiếp. Luồng đúng cho hệ lớn:
```
UseCase → SaveChanges (DB) + ghi OutboxMessage TRONG CÙNG transaction
Worker  → đọc Outbox → publish RabbitMQ
Consumer→ Inbox (idempotency) → IIntegrationEventHandler<T>
```
Vì sao bắt buộc: nếu `SaveChanges` thành công nhưng `Publish` fail (hoặc ngược lại) → **DB và event lệch nhau**. Outbox/Inbox là **must-have** ở hệ lớn, không phải nice-to-have.
**Contract chuẩn bị NGAY (chưa cần SDK):**
```csharp
public abstract record IntegrationEvent(Guid Id, DateTimeOffset OccurredAt);
public interface IIntegrationEventPublisher { Task PublishAsync(IntegrationEvent e, CancellationToken ct); }
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent { Task HandleAsync(TEvent e, CancellationToken ct); }
// + OutboxMessage, InboxMessage entity + dispatcher/worker contract. RabbitMQ impl để SAU (adapter).
```

### F26 — 🟡 Thiếu Search projection boundary — [ĐÁNH GIÁ]  (chuẩn bị cho Elasticsearch)
Elasticsearch **KHÔNG** thay repository chính — nó là **read-model/search projection**; DB vẫn là source-of-truth. Luồng: `domain/integration event → Outbox → projector → Elasticsearch index`. Chấp nhận **eventual consistency** + cần reindex job + alias/mapping versioning.
**Contract:**
```csharp
public interface ISearchIndex<TDocument> { Task IndexAsync(TDocument doc, CancellationToken ct); Task DeleteAsync(string id, CancellationToken ct); }
public interface ISearchQuery<TDocument> { Task<SearchResult<TDocument>> SearchAsync(SearchRequest req, CancellationToken ct); }
```
Đổi sang OpenSearch/Meilisearch sau này = chỉ viết adapter, không đụng use case.

### F27 — 🟡 Thiếu External Auth provider model — [ĐÁNH GIÁ]  (chuẩn bị cho Zalo/Google login)
Đăng nhập Zalo/Google thuộc **module Identity**, KHÔNG thuộc foundation core (và tuyệt đối không hardcode `Google/Zalo` vào `FoundationAuthExtensions` — xem F3). Cần tính trước: OAuth **state + PKCE**, **ReturnUrl whitelist**, **ProviderUserId unique**, **account linking**, **ExternalLogin** entity.
**Contract:**
```csharp
public interface IExternalAuthProvider {
    string Name { get; }                                   // "google" | "zalo" | ...
    Task<ExternalAuthChallenge> CreateChallengeAsync(ExternalAuthRequest req, CancellationToken ct);
    Task<ExternalUserProfile>   CompleteAsync(ExternalAuthCallback cb, CancellationToken ct);
}
public interface IExternalAuthProviderRegistry { IExternalAuthProvider Resolve(string name); }
```
Adapter: `Modules.Identity.ExternalAuth.Google`, `...Zalo`. Thêm Facebook/Apple sau = thêm adapter, không đụng `Start/CompleteExternalLoginUseCase`.

### F28 — 🟡 Thiếu Notification/Email port (+ Cache/Storage) — [ĐÁNH GIÁ]  (chuẩn bị cho Gmail/SMTP, Redis, S3)
"Gmail" có 2 nghĩa: **Google login** (→ F27) HOẶC **gửi email** (→ port email). Use case KHÔNG gọi Gmail SDK trực tiếp.
```csharp
public interface IEmailSender { Task SendAsync(EmailMessage msg, CancellationToken ct); }   // adapter: Email.Gmail / Email.Smtp / Email.SendGrid
public interface ICacheStore  { Task<T?> GetAsync<T>(string key, CancellationToken ct); Task SetAsync<T>(string key, T value, CacheEntryOptions o, CancellationToken ct); Task RemoveAsync(string key, CancellationToken ct); }   // adapter: Cache.Redis / Cache.Memory
public interface IFileStorage { Task<string> SaveAsync(FileBlob blob, CancellationToken ct); Task<Stream> OpenAsync(string key, CancellationToken ct); }   // adapter: Storage.S3 / Storage.Local
```
Email quan trọng (vd xác nhận) cũng nên đi qua **Outbox** (F25) để tránh "DB xong nhưng email fail".

### F29 — 🟡 Thiếu Optional Adapter packaging strategy — [ĐÁNH GIÁ]
Mỗi công nghệ = **package/project riêng** (`*.RabbitMq`, `*.Elasticsearch`, `*.Gmail`, `*.Redis`...) để app chỉ tham chiếu cái nó dùng → không "kéo cả thế giới". Mỗi adapter tự có integration test riêng (Testcontainers) **không đụng lõi** Application/Domain.

### Câu trả lời chốt cho câu hỏi của bạn
- **"Thêm sau thoải mái và tốt đúng không?"** → **Chưa** với cấu trúc hiện tại. Thêm được nhưng sẽ vá lệch. **Có thể chuẩn bị RẤT tốt từ bây giờ** bằng cách để sẵn **ổ cắm** (F24–F29: ports + registry + outbox/inbox + optional packages + module boundary) **mà KHÔNG cài SDK nào**. Khi có ổ cắm, sau này thêm RabbitMQ/Elasticsearch/Zalo/Google/Gmail/Redis chỉ là: *viết adapter → đăng ký ở Host → viết integration test* — **không sửa lõi Application/Domain**.
- Thứ tự khuyến nghị: làm **F25 (Outbox/Inbox + IntegrationEvent contract)** và **F24 (khung AddXxxCore/AddYyy)** SỚM (chúng ảnh hưởng cách viết use case); F26/F27/F28 chỉ cần **định nghĩa port** trước, impl để đúng lúc cần.

---

## 11. Ma trận truy vết bổ sung (F14–F29)

| Finding | Mức | Bằng chứng / Loại |
|---|---|---|
| F14 Api→Infrastructure packaging | 🟡 | `Foundation.Api.csproj` `ProjectReference Infrastructure` · FACT |
| F15 exception log raw path | 🔴 | `ExceptionHandlingMiddleware` `Log.UnhandledException(..., context.Request.Path)` · FACT |
| F16 forwarded headers absent | 🟡 | grep `ForwardedHeaders` = 0; rate-limit theo `RemoteIpAddress` · FACT |
| F17 refresh cookie vs CORS | 🟡 | `AuthEndpoints` SameSite=Strict + `FoundationSecurityExtensions` AllowCredentials · FACT |
| F18 DI duplicate/override | 🟡 | `DependencyInjectionExtensions` `.AsImplementedInterfaces()` (không TryAdd) · FACT |
| F19 RefreshTokenRecord in Application | 🟡 | `Application/Identity/RefreshTokenRecord.cs` "persistence-facing" · FACT |
| F20 hardcoded VI message / Title=Message | 🟡 | `ProblemDetailsBuilder.Title=error.Message` + `CommonErrors/AuthErrors` · FACT |
| F21 correlationId ≠ traceId | 🟢 | `CorrelationIdMiddleware` vs `ProblemDetailsBuilder.TraceId` · FACT |
| F22 JWT key rotation | 🟡 | `JwtOptions.SigningKey` đơn, không kid · FACT |
| F23 auth context role-only | 🟡 | `HttpContextCurrentUser` chỉ UserId/Roles · FACT |
| F24 extension architecture | 🔴 | thiếu khung ports/adapters/registry · ĐÁNH GIÁ |
| F25 messaging + outbox/inbox | 🔴 | vắng IntegrationEvent/Outbox · ĐÁNH GIÁ |
| F26 search projection | 🟡 | vắng ISearchIndex/projector · ĐÁNH GIÁ |
| F27 external auth provider | 🟡 | vắng IExternalAuthProvider/registry · ĐÁNH GIÁ |
| F28 email/cache/storage ports | 🟡 | vắng IEmailSender/ICacheStore/IFileStorage · ĐÁNH GIÁ |
| F29 optional adapter packaging | 🟡 | chưa tách package per-tech · ĐÁNH GIÁ |

---

## 12. Kết luận cuối (sau kiểm toán đầy đủ F1–F29)

- **Là code hiện trạng:** nền tốt, senior thật, test thật, bảo mật auth khá nghiêm. Dùng cho app nhỏ/vừa: ổn.
- **Là base cho dự án lớn + mở rộng công nghệ về sau:** **chưa đủ** cho tới khi xử lý:
  - **P0 (chặn scale):** F1 (tách host/library), F2·F3·F4 (gỡ rò nghiệp vụ), **F15 (rò token qua log lỗi — bảo mật)**.
  - **P1 (chắc nền trước khi nhiều module):** F5 (transaction refresh), F6 (DI marker + overload), F7 (fail-fast mọi env), F11 (Postgres test), F16 (forwarded headers), F17 (cookie/CORS mode), F18 (DI duplicate policy).
  - **P1.5 (đặt "ổ cắm" mở rộng SỚM vì ảnh hưởng cách viết use case):** **F24 + F25** (extension framework + Outbox/Inbox + IntegrationEvent), định nghĩa port F26/F27/F28.
  - **P2 (nâng platform):** F12 (semantic folder), F13 + mục 4 (BuildingBlocks + Modules + Host), F19/F20/F21/F22/F23 (persistence-model boundary, localization, correlation, key rotation, auth context), F29 (packaging), F8/F9/F10.
- **Nguyên tắc bất biến khi refactor:** giữ **build 0 warning + test xanh** sau mỗi lát (architecture test là lưới an toàn); **không** kéo SDK công nghệ vào lõi — chỉ thêm **contract + extension point**.

> **Một câu chốt:** foundation hiện tại là *nền kỹ thuật tốt nhưng chưa phải platform*. Muốn "cực chất cho dự án lớn": **tách vai (library/adapter/module/host) + gỡ rò nghiệp vụ + đặt ổ cắm mở rộng (ports/outbox) TRƯỚC khi xây nghiệp vụ lớn lên trên nó.**

---

## 13. Governance Findings — F30–F35 (biến review thành blueprint)

> **Verified-by-absence:** grep `OpenTelemetry|Polly|Outbox|Inbox|MassTransit|Metrics|Meter` trong `foundation/**` (kể cả `Directory.Packages.props`) = **0** (chỉ xuất hiện trong tài liệu này). Các gap dưới đây là thật.
> Đây là nhóm **governance** — thứ biến "review hay" thành "kim chỉ nam triển khai hệ lớn". Chi tiết thiết kế đầy đủ nằm ở **`FOUNDATION-BLUEPRINT.md`**.

### F30 — 🔴 Thiếu enforce Module Boundary — [ĐÁNH GIÁ]
Hiện chỉ có **dependency-direction** test (`DependencyRuleTests`) giữa các layer. Chưa có luật cấm **module A gọi thẳng internal của module B** (chỉ được qua contract/integration event). Không enforce → modular monolith sẽ thoái hoá thành big-ball-of-mud "ngầm".
**Chuẩn:** mỗi module có `*.Contracts` (public) vs internal; NetArchTest cấm `Module.B.*` reference `Module.A.Application/Domain/Infrastructure` (chỉ được `Module.A.Contracts`); giao tiếp liên-module qua integration event/public contract.

### F31 — 🔴 Thiếu Data Ownership / Schema / Migration strategy — [ĐÁNH GIÁ]
Một `AppDbContext` gộp tất cả (như resort-qr) không scale khi nhiều module. Chưa định nghĩa: ai **sở hữu** bảng nào, **schema-per-module**, migration độc lập theo module, cấm module này JOIN thẳng bảng module khác.
**Chuẩn:** mỗi module 1 `DbContext` + **schema riêng** (`identity.*`, `rooms.*`), migration per-module (mỗi module một migrations assembly/history table), liên-module KHÔNG JOIN — chỉ qua API/event; nếu 1 DB vật lý thì tách bằng schema + cấm FK chéo module.

### F32 — 🟡 Thiếu API + Integration Event versioning — [ĐÁNH GIÁ]
Không có chiến lược version cho **HTTP API** (URL/header) lẫn **integration event** (schema evolution). Ở hệ lớn, event là hợp đồng dài hạn giữa producer/consumer.
**Chuẩn:** API versioning (Asp.Versioning) + OpenAPI group theo version; integration event có `EventType` + `SchemaVersion`, quy tắc **chỉ thêm field optional** (backward-compat), breaking = event mới; consumer khoan dung (tolerant reader).

### F33 — 🟡 Thiếu chuẩn Resilience cho adapter — [ĐÁNH GIÁ] (verified: không có Polly)
Adapter gọi hạ tầng ngoài (RabbitMQ/Elastic/SMTP/HTTP external-auth) chưa có chuẩn timeout/retry/circuit-breaker/bulkhead → một dependency chậm/chết có thể kéo sập app.
**Chuẩn:** resilience pipeline (`Microsoft.Extensions.Resilience`/Polly v8) áp **ở biên adapter** (không ở use case): timeout + retry (exponential+jitter, chỉ cho thao tác idempotent) + circuit-breaker + fallback; cấu hình per-adapter qua options.

### F34 — 🟡 Thiếu chuẩn Telemetry/Metrics (OpenTelemetry) — [ĐÁNH GIÁ] (verified: không có OpenTelemetry)
Có Serilog structured log + correlation, nhưng thiếu **traces (spans)** + **metrics**. Hệ lớn cần 3 trụ observability thống nhất.
**Chuẩn:** OpenTelemetry traces + metrics + logs; propagate **W3C traceparent** (thống nhất với correlationId — xem F21); metrics nghiệp vụ (`Meter`) + hạ tầng (EF/HTTP/rate-limit); export OTLP. `traceId` trong ProblemDetails = trace hiện hành.

### F35 — 🟡 Thiếu Secrets / Configuration governance — [ĐÁNH GIÁ]
Có Options validate-on-start (tốt), nhưng chưa có chuẩn **nguồn secret** (không commit key), phân tách config theo môi trường, và **fail-fast bắt buộc** ở Production (liên quan F7).
**Chuẩn:** secret từ store ngoài (User-Secrets dev · env/Key Vault/SOPS prod), cấm secret trong appsettings commit; validate-on-start cho MỌI options bắt buộc; startup validator chặn boot nếu thiếu (mọi môi trường).

---

## 14. Target Package Graph + Dependency Rules (bản rút gọn — chi tiết ở blueprint)

```
Host/StarHill.Api
  ├─> BuildingBlocks.Api            (HTTP: ProblemDetails, auth mechanism, versioning, OpenApi, rate-limit)   — KHÔNG ref Infrastructure
  ├─> BuildingBlocks.Infrastructure (EF base, crypto, outbox impl, default adapters)
  ├─> Modules.*.Api  + Modules.*.Infrastructure
  └─> Adapters.*     (Messaging.RabbitMq, Search.Elasticsearch, Email.Gmail/Smtp, Cache.Redis, ExternalAuth.Google/Zalo, Storage.S3)

BuildingBlocks.Domain        →  (none)                         # sạch tuyệt đối
BuildingBlocks.Application   →  BuildingBlocks.Domain          # chỉ Ports + Behaviors + UseCase
BuildingBlocks.Infrastructure→  BuildingBlocks.Application     # implement Ports
BuildingBlocks.Api           →  BuildingBlocks.Application     # CHỈ Application (KHÔNG Infrastructure — sửa F14)
Adapters.<Tech>              →  BuildingBlocks.Application (Ports)   # KHÔNG ref module/nghiệp vụ
Modules.<M>.Domain           →  BuildingBlocks.Domain
Modules.<M>.Application       →  Modules.<M>.Domain, BuildingBlocks.Application
Modules.<M>.Infrastructure    →  Modules.<M>.Application, BuildingBlocks.Infrastructure
Modules.<M>.Api               →  Modules.<M>.Application, BuildingBlocks.Api
Modules.<M>.Contracts (public)→  (none/DTO)   # nơi DUY NHẤT module khác được tham chiếu (sửa F30)
Host                         →  compose TẤT CẢ (nơi duy nhất biết adapter cụ thể)
```

**Luật bất biến (enforce bằng NetArchTest):**
1. `BuildingBlocks.*` **KHÔNG** biết nghiệp vụ (không guest/room/resort/Admin/Staff) — sửa F2/F3/F4.
2. `BuildingBlocks.Api` **KHÔNG** reference `*.Infrastructure` — sửa F14.
3. `Adapters.*` chỉ reference **Ports** (Application), KHÔNG module, KHÔNG adapter khác.
4. `Modules.A` chỉ chạm `Modules.B` qua `Modules.B.Contracts` (+ integration event) — sửa F30.
5. Chỉ **Host** được reference đồng thời Api + Infrastructure + Adapters (composition root duy nhất).
6. Use case gọi `IOutboxWriter` (KHÔNG gọi `IEventBusPublisher`/SDK) — sửa F25.

---

## 15. Tinh chỉnh contract (theo phản hồi reviewer — chống dùng sai kiến trúc)

**F25 tách tên để interface tự chặn sai (Application vs Worker vs Consumer):**
```csharp
// Application side — use case CHỈ được thấy cái này:
public interface IOutboxWriter { Task EnqueueAsync(IntegrationEvent e, CancellationToken ct); }   // ghi Outbox CÙNG transaction DB
// Infrastructure worker side (KHÔNG lộ cho use case):
public interface IOutboxDispatcher { Task DispatchPendingAsync(CancellationToken ct); }
public interface IEventBusPublisher { Task PublishAsync(OutboxMessage m, CancellationToken ct); }  // adapter RabbitMQ impl
// Consumer side:
public interface IInboxStore { Task<bool> TryMarkProcessedAsync(Guid messageId, string consumer, CancellationToken ct); }
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent { Task HandleAsync(TEvent e, CancellationToken ct); }
```
> Lý do: tên `IIntegrationEventPublisher.PublishAsync` (bản F25 cũ) **nguy hiểm** — dev dễ gọi publish thẳng RabbitMQ trong use case, phá đảm bảo Outbox. Use case chỉ được thấy `IOutboxWriter.EnqueueAsync`.

**F28 tách cache thành các port hẹp (chống leaky abstraction):**
```csharp
public interface IAppCache          { Task<T?> GetAsync<T>(string key, CancellationToken ct); Task SetAsync<T>(string key, T value, CacheEntryOptions o, CancellationToken ct); Task RemoveAsync(string key, CancellationToken ct); }
public interface IDistributedLock   { Task<ILockHandle?> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct); }
public interface IIdempotencyStore  { Task<bool> TryBeginAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct); }
public interface IRateLimitStore    { Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken ct); }
```
> Một `ICacheStore` gánh tất → leaky. Redis adapter impl cả 4 (SET NX PX cho lock/idempotency); Memory adapter impl `IAppCache` (+ single-node lock).

**F27 (external auth) — điểm thực tế bắt buộc trong blueprint:** OAuth **state store** + **PKCE verifier store** + **nonce**; **ReturnUrl whitelist**; **ProviderUserId uniqueness** + **account linking policy**; **email-trust policy** (KHÔNG giả định provider nào cũng có `email_verified` — Zalo có thể không trả email) → `ExternalUserProfile` chuẩn hoá với `Email?`/`EmailVerified?` nullable; **callback replay protection**.

**F26 (search) — ownership bắt buộc:** mỗi `SearchDocument` **thuộc 1 module**; **index naming + mapping version + alias swap** (blue/green reindex); **reindex job**; **staleness contract** (eventual, DB là source-of-truth); **poison-document** → dead-letter, không chặn cả luồng.

> **Danh mục finding đầy đủ giờ là F1–F35.** Bản THIẾT KẾ để triển khai: **`FOUNDATION-BLUEPRINT.md`**.
