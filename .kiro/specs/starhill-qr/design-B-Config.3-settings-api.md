# Design — B-Config.3: ResortConfig.Api settings slice (admin xem + sửa cấu hình resort)

> Mắt xích còn thiếu: `RenderRoomQrPng` (qr.png, B-Rooms.3) trả `invalid_configuration` khi thiếu `GuestWebBaseUrl`
> nhưng CHƯA có endpoint để admin cấu hình (chỉ seeder default). Slice này lộ nửa-Api settings — **thiết lập
> write-path ĐẦU TIÊN cho ResortConfig**. Contained, verify KHÔNG cần Docker. Mọi khẳng định đã kiểm từ code thật.

## 0. Nguồn sự thật (đã verify)
- ResortConfig hiện: chỉ read (`EfResortSettingsQuery`/`EfResortExistenceQuery` inject `ResortConfigDbContext` ở
  **Infrastructure**) + seeder + TranslationResolver. **Chưa có use case/Api nào.**
- `ResortSettings : AuditableEntity, IHasConcurrencyToken` (xmin) — fields: 3 feature flags, 3 require-rule-ack,
  PortalWindowMinutes(30), VisitIdleExpiryHours(24), GuestWebBaseUrl(string?), MaxMessageLength(2000),
  MessageRateLimitPerMinute(10), HousekeepingRateLimitPerHour(12). 1-1 Resort qua ResortId (unique).
- `IResortSettingsQuery.GetAsync()` → `ResortSettingsSnapshot?` (đã có, dùng cho GET).
- `AddResortConfigInfrastructure` gọi `AddBedrockPersistence<ResortConfigDbContext>(key)` (⇒ keyed IUnitOfWork) nhưng
  CHƯA `AddBedrockRepository<...,ResortSettings>` → phải thêm cho write-path.
- `CommonErrors.NotFoundGeneric()` (code `not_found`, ∈ snapshot Bedrock.Domain) — tái dùng cho settings-chưa-seed →
  KHÔNG cần error catalog ResortConfig mới (không đụng `ErrorCodeSnapshotTests`). Validation → `validation_error`.
- `StarHillPolicies.RequireAdmin` (QR-AD-020) + JsonStringEnumConverter (QR-AD-021, không ảnh hưởng — settings không enum).

## 1. Quyết định (kèm lý do)
- **D-A write-path = mirror Rooms (Option A: keyed IRepository + use case pipeline)** — KHÔNG inject DbContext trực
  tiếp trong Application (giữ Application ⊥ Infrastructure I7; DbContext chỉ ở Infra như query/seeder). `UpdateResortSettingsUseCase :
  ICommandUseCase<UpdateResortSettingsInput>` inject keyed `IRepository<ResortSettings>` + `IUnitOfWork` → pipeline lo
  transaction + validation (nhất quán Rooms). Load bản ghi settings đơn (single-resort) qua `FirstOrDefaultAsync(_ => true)`.
- **D-B role = RequireAdmin cho CẢ GET và PUT** — Req 9.3 "Admin có thêm quyền quản lý ... settings"; settings là
  cấu hình admin (feature flag/rate limit/base URL), Staff KHÔNG có nhu cầu vận hành đọc raw config. Precise Req 9.3.
  (CP8 superset đã gác ở Rooms — slice này không cần lặp.)
- **D-C validation** (`UpdateResortSettingsValidator`, FluentValidation): `GuestWebBaseUrl` null HOẶC absolute https
  (Req 15.6 — cho phép null để admin lưu trước khi có URL; qr.png vẫn báo invalid_configuration tới khi set);
  PortalWindowMinutes 1..1440; VisitIdleExpiryHours 1..8760; MaxMessageLength 1..10000; Message/Housekeeping rate 1..1000.
- **D-D settings-chưa-seed** → `CommonErrors.NotFoundGeneric` (không catalog mới). Thực tế luôn seeded lúc boot
  (QR-AD-017 seeder fail-fast) → nhánh phòng thủ.
- **D-E concurrency (xmin)**: không bắt tường minh trong use case — để `ExceptionHandlingMiddleware` base map
  `ConcurrencyConflictException` (rủi ro thấp: một admin sửa settings). Thêm xử lý riêng nếu sau này cần.
- **D-F GET trả `ResortSettingsSnapshot`** (DTO Contracts sẵn có) — không tạo DTO Api trùng.

## 2. Hợp đồng HTTP
- `GET /v1/resort/settings` [RequireAdmin] → 200 `ResortSettingsSnapshot` | 404 `not_found` (chưa seed).
- `PUT /v1/resort/settings` [RequireAdmin] — body = tất cả field sửa được (KHÔNG ResortId) → 204 | 400 `validation_error` | 404 `not_found`.

## 3. Cấu trúc code
- `ResortConfig.Application/UpdateResortSettings.cs` (MỚI): `UpdateResortSettingsInput` + `UpdateResortSettingsUseCase` + `UpdateResortSettingsValidator`. (+FluentValidation vào csproj.)
- `ResortConfig.Infrastructure/DependencyInjection/...`: +`AddBedrockRepository<ResortConfigDbContext, ResortSettings>(key)` + factory đăng ký use case + validator.
- `ResortConfig.Api/` (MỚI project): `ResortConfigEndpointModule` (GET+PUT, RequireAdmin) + `AddResortConfigApi`.
- Host: `AddResortConfigApi()`. `Platform.slnx`: +ResortConfig.Api.

## 4. Verify (KHÔNG Docker)
- `ResortConfigSettingsUseCaseTests` (ResortConfig.IntegrationTests, SQLite): update đổi field + GuestWebBaseUrl;
  validator chặn http/không-absolute/range xấu → validation_error; settings-null → not_found.
- `ResortConfigEndpointAuthTests` (StarHill.Api.Tests, fake): GET/PUT Admin=200/204; Staff→403; no-token→401; PUT body xấu→400.
- `vp all` + `vp journal`.

## 5. Anti-drift
- **QR-AD-023** — ResortConfig write-path đầu tiên (settings Api) + role Admin (Req 9.3) + tái dùng CommonErrors (không catalog mới). Guard map row.
