# Module design — ResortConfig (Wave B, module đầu tiên)

> **Design-first** cho module `ResortConfig` trên nền Bedrock (`starhill/`). Đọc kèm `../design.md` §2/§4.2.
> Nghiệp vụ WHAT: `docs/resort-qr-portal/` (Req 2 i18n, Req 14 ResortSettings, Req 12.4 seed). Nguồn PORT:
> `resort-qr/src/ResortQr.Domain/Resorts/*` + `ResortQr.Application/Localization/*` + `Infrastructure/Localization/*`.
> Mọi mệnh đề dưới đây dựa trên ĐỌC MÃ thật (khuôn `Modules/Identity`, `PlatformDbContext`, abstractions Bedrock) — không suy đoán.

## 0. Vai trò & phạm vi
ResortConfig = **nền cấu hình + i18n** của sản phẩm: sở hữu `Resort`, `ResortSettings` (feature-flags/ngưỡng vận hành), `ResortLanguage` (danh sách ngôn ngữ + mặc định), và **thuật toán resolve i18n** dùng chung. Mọi module khác (GuestAccess/Rules/Faq/Concierge/Housekeeping) **đọc** feature-flags + ngôn ngữ + dùng resolver qua `ResortConfig.Contracts`.

Lý do làm ResortConfig TRƯỚC (đúng dependency graph tasks.md wave 3/§design §10 wave A→B): nó là nền feature-flags/ngôn ngữ; Rules/Faq/Concierge/Housekeeping đều phụ thuộc.

## 1. Cấu trúc 5-project (mirror `Modules/Identity`, ref-graph đã verify)

```
starhill/src/Modules/ResortConfig/
  ResortConfig.Domain          → ref: Bedrock.Domain
  ResortConfig.Contracts       → ref: Bedrock.Messaging.Contracts
  ResortConfig.Application     → ref: ResortConfig.Domain + ResortConfig.Contracts + Bedrock.Application (+FluentValidation khi có validator)
  ResortConfig.Infrastructure  → ref: ResortConfig.Application + Bedrock.Infrastructure (+ EFCore, EFCore.Design PrivateAssets=all)
  ResortConfig.Api             → ref: ResortConfig.Application + Bedrock.Api (+ FrameworkReference ASP.NET)  [SLICE SAU — khi có endpoint admin]
```

**Ranh giới bất biến (guard ModuleBoundaryTests):** Api ⊥ Infrastructure; Contracts KHÔNG ref Application/Domain/Infra; module khác chỉ chạm ResortConfig qua `ResortConfig.Contracts`.

## 2. Entities (`ResortConfig.Domain`) — port SharedKernel→Bedrock

| Entity | Base Bedrock | Ghi chú port |
|---|---|---|
| `Resort` | `Entity` | Name, Timezone (IANA), LogoUrl?, CreatedAt. POCO thuần (ràng buộc ở EF config). |
| `ResortLanguage` | `Entity` | ResortId, Code (BCP-47), DisplayName, IsEnabled(=true), IsDefault, SortOrder. |
| `ResortSettings` | `AuditableEntity`, **`IHasConcurrencyToken`** | 1-1 ResortId; feature-flags (Faq/Chat/Housekeeping Enabled + RequireRuleAckFor*), PortalWindowMinutes(30), VisitIdleExpiryHours(24), GuestWebBaseUrl?, MaxMessageLength(2000), MessageRateLimitPerMinute(10), HousekeepingRateLimitPerHour(12). |

**Đổi so với resort-qr (bản chất):**
- `using ResortQr.SharedKernel.Entities` → `using Bedrock.Domain.Entities`.
- `ResortSettings` trong resort-qr chỉ `: AuditableEntity` và dựa `AuditableEntity` mang xmin. Ở Bedrock, `AuditableEntity` **chỉ** implement `IAuditable` (KHÔNG concurrency). Concurrency là opt-in qua `IHasConcurrencyToken` (RowVersion **`uint`** — PlatformDbContext map→`xmin` chỉ trên Npgsql). → `ResortSettings : AuditableEntity, IHasConcurrencyToken` + `public uint RowVersion { get; set; }`. (Đây là DV — xem §8.)

## 3. i18n (điểm phải đổi thiết kế quan trọng — QR-DV-002)

Nguồn resort-qr: `ITranslation` (SharedKernel.Entities), `Translated<T>`/`Translated` (Application.Localization), `ITranslationResolver : ISingletonService` (Application.Localization), impl `TranslationResolver` (Infrastructure).

**Vấn đề bản chất:** `ITranslationResolver` + `ITranslation` + `Translated<T>` phải dùng được ở **nhiều module** (Rules/Faq) → phải nằm trong `ResortConfig.Contracts` (surface công khai). NHƯNG `Contracts` chỉ được ref `Bedrock.Messaging.Contracts` (bất biến matrix §3.3). Trong khi `ISingletonService` nằm ở `Bedrock.Application`. → **KHÔNG thể** để `ITranslationResolver : ISingletonService` trong Contracts.

**Quyết định (QR-DV-002):**
- Đặt `ITranslation`, `Translated<T>` (+ factory `Translated`), `ITranslationResolver` trong **`ResortConfig.Contracts`** — thuần POCO/interface, KHÔNG kế thừa marker DI.
- Impl `TranslationResolver` (thuật toán thuần, giữ nguyên logic resort-qr: MatchSupported/Resolve/MissingLanguages) đặt trong **`ResortConfig.Application`** (không I/O), **đăng ký thủ công** `services.AddSingleton<ITranslationResolver, TranslationResolver>()` trong `AddResortConfigInfrastructure` (thay cho auto-scan qua ISingletonService).
- Rules/Faq (sau này) ref `ResortConfig.Contracts` → dùng resolver + implement `ITranslation` trên entity `*Translation` của chúng.

Lý do: giữ Contracts sạch phụ thuộc (chỉ Bedrock.Messaging.Contracts), vẫn chia sẻ được i18n cho mọi module; manual-register là chi phí nhỏ, rõ ràng, tránh phá ref-graph.

## 4. Persistence (`ResortConfig.Infrastructure`)

- **`ResortConfigDbContext : PlatformDbContext`** — `HasDefaultSchema("resort_config")`; ctor nhận `IClock`/`ICurrentUser`/`IDomainEventDispatcher` (như IdentityDbContext). **KHÔNG** gọi `AddOutboxInbox`/`AddRefreshTokens` (QR-AD-008: ResortConfig không phát integration-event, không refresh-token → giữ tối giản; thêm outbox khi thực sự phát event). DbSet: `Resorts`, `ResortLanguages`, `ResortSettings`.
- **`ResortConfigDbContextFactory : IDesignTimeDbContextFactory<ResortConfigDbContext>`** — mirror `IdentityDbContextFactory` CHÍNH XÁC: `UseNpgsql(dummy)` + `UseSnakeCaseNamingConvention()` + `DesignTimeStub` cho 3 dependency. (Khớp options runtime để migration không drift.)
- **EF fluent config** (partial/unique index — CP14/CP15/1-1):
  - `Resort`: MaxLength Name/Timezone/LogoUrl.
  - `ResortLanguage`: unique `(ResortId, Code)`; **partial unique** `(ResortId) WHERE IsDefault` (đúng-một-mặc-định — CP14); MaxLength Code/DisplayName.
  - `ResortSettings`: unique `(ResortId)` (1-1 — D5); RowVersion→xmin tự động qua PlatformDbContext (chỉ Npgsql).
- **Migration** `InitialCreate` trong schema `resort_config` (per-module; `dotnet ef migrations bundle` cho CI — thêm job bundle ResortConfig vào `starhill-ci.yml` khi có migration; xem §7).
- **Seeder** `ResortConfigSeeder` (runtime idempotent, port từ resort-qr `ResortSeeder`): seed 1 Resort + ResortSettings mặc định + ResortLanguage en/vi/ko/zh với **`en` IsDefault=true** (Req 12.4/CP14). Gọi từ Host khi cờ dev/compose bật (cùng chỗ `ApplyMigrationsOnStartup`). **Dùng seeder runtime, KHÔNG HasData** (QR-AD-008-b): HasData xung đột với cột xmin store-generated + audit → seeder idempotent an toàn hơn (mirror precedent resort-qr).

## 5. Contracts lộ ra (`ResortConfig.Contracts`)
- i18n: `ITranslation`, `Translated<T>`, `ITranslationResolver` (§3).
- `IResortSettingsQuery` — trả DTO settings (feature-flags + PortalWindowMinutes + VisitIdleExpiryHours + rate limits + GuestWebBaseUrl) cho GuestAccess/Rules/Faq/Concierge/Housekeeping.
- `IResortLanguageQuery` — danh sách ngôn ngữ enabled + mã mặc định (cho resolver `MatchSupported` + `/resolve`).
- DTO thuần (record) cho settings/language (KHÔNG lộ entity Domain ra ngoài module).

## 6. Use cases & slice
- **Slice B.1 (skeleton nền — increment sau design này):** Domain (3 entity) + Contracts (i18n + query DTO/interface) + Application (`TranslationResolver` impl + đăng ký) + Infrastructure (DbContext + Factory + EF config + migration + seeder) + `AddResortConfigInfrastructure`. Guard test CP5 (resolver) unit.
- **Slice B.2:** query impl `EfResortSettingsQuery`/`EfResortLanguageQuery` (Infrastructure) hiện thực Contracts; integration test CP14 (one default) + CP15 (settings concurrency) trên Testcontainers.
- **Slice B.3 (khi Identity auth dùng được):** `ResortConfig.Api` + use case Get/Update ResortSettings (Admin PUT, Staff GET) + quản lý ResortLanguage + endpoint `/v1/settings`, `/v1/languages`. (Cần authorization Role→policy QR-AD-005.)

## 7. Host wiring (`StarHill.Api/Program.cs`)
- Thêm connection string `ConnectionStrings:ResortConfig` (cùng PostgreSQL, schema `resort_config`).
- `services.AddResortConfigInfrastructure(o => o.UseNpgsql(cs))` (đăng ký DbContext + persistence + resolver singleton + query impl); `AddResortConfigApi()` khi có Api (B.3).
- Migrate-on-startup (dev/compose): thêm `ResortConfigDbContext.Database.MigrateAsync()` + gọi `ResortConfigSeeder` sau migrate, cùng cờ `Bedrock:ApplyMigrationsOnStartup`.
- CI: thêm job/bước bundle migration ResortConfig vào `.github/workflows/starhill-ci.yml` khi migration tồn tại (mirror job Identity).

## 8. Correctness Properties → guard test (module này)

### Property 5: Toàn vẹn bản dịch/fallback
`TranslationResolver.Resolve` trả requested (HasContent) → default (IsFallback) → Missing; "row rỗng" coi như thiếu. **Guard**: `ResortConfig.UnitTests` (thuần, không DB) — chạy mọi máy.
**Validates: Requirements 2.2, 2.5, 8.7**

### Property 14: Đúng một ngôn ngữ mặc định
Partial unique `ResortLanguage(ResortId) WHERE IsDefault` chặn 2 default; seeder đặt `en` default. **Guard**: `ResortConfig.IntegrationTests` (Testcontainers Postgres — SKIP nếu thiếu Docker).
**Validates: Requirements 2.1, 2.2**

### Property 15: Chống ghi đè đồng thời (ResortSettings)
`ResortSettings.RowVersion`→xmin; hai update đồng thời → người sau nhận `ConcurrencyConflictException`/409. **Guard**: `ResortConfig.IntegrationTests` (Testcontainers).
**Validates: Requirements 8.1, 8.5**

> Test project mới: `starhill/tests/Modules/ResortConfig.UnitTests` + `ResortConfig.IntegrationTests` (mirror `Identity.UnitTests`/`IntegrationTests`) → đăng ký `Platform.slnx`. ModuleBoundary cho ResortConfig thêm vào arch test khi module tồn tại.

## 9. Quyết định phát sinh (ghi journal)
- **QR-AD-008**: ResortConfigDbContext KHÔNG map Outbox/Inbox/RefreshToken (không phát event, không auth-token) — tối giản, thêm khi cần. Seed bằng seeder runtime idempotent (KHÔNG HasData) do xung đột xmin/audit.
- **QR-DV-002**: i18n (`ITranslation`/`Translated<T>`/`ITranslationResolver`) đặt ở `Contracts` KHÔNG kèm marker `ISingletonService` (Contracts không được ref Bedrock.Application); resolver đăng ký thủ công `AddSingleton` — đổi so với resort-qr (auto-scan qua marker).
- **QR-AD-005** (đã có): Role→policy áp khi làm Api slice B.3.

## 10. Điều kiện "design tốt" trước khi code (tự-valid)
- [x] Ref-graph 5-project khớp khuôn Identity (đã đọc csproj).
- [x] Concurrency: `IHasConcurrencyToken.RowVersion` `uint` (đã đọc Abstractions) → ResortSettings implement.
- [x] i18n placement không phá ref-graph Contracts (đã xác minh ISingletonService ở Bedrock.Application).
- [x] DbContext/Factory pattern khớp IdentityDbContext(Factory) (đã đọc).
- [x] CP5/CP14/CP15 có guard + nơi đặt test.
- [x] (B.1 done) migration `InitialCreate` sinh + verify (CP14 partial-unique, CP15 xmin, 1-1 settings, checks); CI bundle ResortConfig thêm vào `starhill-ci.yml`.
- [ ] (B.2) ModuleBoundary arch test cho ResortConfig; query impl + CP14/CP15 integration; Host wiring + seeder.
