using System.Text.Json.Serialization;
using Adapters.Messaging.RabbitMq;
using Bedrock.Api;
using Bedrock.Api.OpenApi;
using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Messaging;
using Bedrock.Infrastructure.DependencyInjection;
using Identity.Api.DependencyInjection;
using Identity.Contracts.Events;
using Identity.Infrastructure.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ResortConfig.Api.DependencyInjection;
using ResortConfig.Infrastructure.DependencyInjection;
using ResortConfig.Infrastructure.Persistence;
using GuestAccess.Api.DependencyInjection;
using GuestAccess.Infrastructure.DependencyInjection;
using GuestAccess.Infrastructure.Persistence;
using Rooms.Api.DependencyInjection;
using Rooms.Infrastructure.DependencyInjection;
using Rooms.Infrastructure.Persistence;
using Rules.Api.DependencyInjection;
using Rules.Infrastructure.DependencyInjection;
using Rules.Infrastructure.Persistence;
using Faq.Api.DependencyInjection;
using Faq.Infrastructure.DependencyInjection;
using Faq.Infrastructure.Persistence;
using Housekeeping.Api.DependencyInjection;
using Housekeeping.Infrastructure.DependencyInjection;
using Housekeeping.Infrastructure.Persistence;
using Concierge.Api.DependencyInjection;
using Concierge.Infrastructure.DependencyInjection;
using Concierge.Infrastructure.Persistence;
using StarHill.Api;
using StarHill.Authorization;
using StarHill.Html.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Fail-fast DI MỌI môi trường (I9/F7, design §9.4): validate scope + build ngay lúc Build().
builder.Host.UseDefaultServiceProvider((_, options) =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

var services = builder.Services;
var configuration = builder.Configuration;

// (1) Cơ chế Bedrock: Api (auth verify + HTTP hardening + routing + health), Security (Argon2/JWT sign + port bắt buộc),
//     startup validator (RequiredPortsValidator chặn boot nếu thiếu port bắt buộc).
services.AddBedrockApi(configuration);
services.AddBedrockSecurity(configuration);
services.AddBedrockStartupValidation();

// OpenAPI doc-gen (native /openapi/v1.json) — CHỈ bật ở Development (QR-AD-033). Prod GIỮ TẮT để không phơi bề mặt
// API ra ngoài (posture nội-mạng; opt-in DV-015/AD-068 của base). Compose mặc định chạy Production → tắt; muốn test
// qua trình duyệt local: `docker compose -f docker-compose.yml -f docker-compose.dev.yml up` (đặt ASPNETCORE_ENVIRONMENT
// = Development). KHÔNG thêm Swagger/Scalar UI (giữ DV-015 "không nhồi stack lớn" — QR-TO-012); browser xem/nhập JSON.
if (builder.Environment.IsDevelopment())
{
    services.AddBedrockOpenApi();
}

// Policy authorization SẢN PHẨM (QR-AD-020): Admin/Staff superset (Req 7.6/11.3). Base cố ý KHÔNG khai role sản
// phẩm — Host khai qua project dùng chung. AddBedrockAuthCore đã gọi AddAuthorization() nên thêm named policy là
// additive. Mọi module Api admin (Rooms giờ; GuestAccess/Rules/... sau) tham chiếu StarHillPolicies.
services.AddStarHillAuthorization();

// Chính sách JSON HTTP sản phẩm (QR-AD-021): enum (de)serialize dạng STRING (vd RoomStatus "Active"/"Inactive"/
// "Maintenance") thay vì số nguyên — API đọc được + ổn định + không phụ thuộc thứ tự khai enum. Áp toàn cục cho
// minimal API (mọi module) để nhất quán. Base KHÔNG áp (giữ domain-agnostic) → là quyết định style của sản phẩm.
services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Posture DEFAULT AN TOÀN cho MỌI extension port (design §5.5/§6.1/§13 — "AddXxxCore luôn gọi"): mỗi port có
// default degrade (NullAppCache) hoặc fail-loud (Throwing*). BẮT BUỘC vì pipeline behaviors (§8) phụ thuộc port
// default: Idempotency behavior inject IIdempotencyStore → thiếu AddCacheCore = KHÔNG resolve được use case đã
// decorate (N-042). Adapter thật (task 14) OVERRIDE bằng Replace; bỏ adapter = quay về default.
services.AddMessagingCore();
services.AddCacheCore();
services.AddEmailCore();
services.AddSearchCore();
services.AddStorageCore();
services.AddExternalAuthCore();

// (2) Modules — composition tách nửa-Infra + nửa-Api (DV-013). Host CHỌN provider (Npgsql) + connection string.
var identityConnectionString = configuration.GetConnectionString("Identity")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Identity — fail-fast (F35). Cấu hình connection string cho module Identity.");
services.AddIdentityInfrastructure(options => options.UseNpgsql(
    identityConnectionString,
    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
services.AddIdentityApi();

// Module ResortConfig (nền cấu hình + i18n). Connection string riêng (cùng PostgreSQL, schema resort_config).
// Nửa-Infra (persistence + resolver + seeder + query) + nửa-Api (admin xem/sửa settings — B-Config.3, RequireAdmin).
var resortConfigConnectionString = configuration.GetConnectionString("ResortConfig")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:ResortConfig — fail-fast (F35). Cấu hình connection string cho module ResortConfig.");
services.AddResortConfigInfrastructure(options => options.UseNpgsql(
    resortConfigConnectionString,
    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "resort_config")));
services.AddResortConfigApi();

// Module Rooms (phòng + token QR). Nửa-Infra (persistence) + nửa-Api (admin CRUD/QR endpoint — B-Rooms.3,
// role Admin/Staff qua StarHillPolicies).
var roomsConnectionString = configuration.GetConnectionString("Rooms")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Rooms — fail-fast (F35). Cấu hình connection string cho module Rooms.");
services.AddRoomsInfrastructure(options => options.UseNpgsql(
    roomsConnectionString,
    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "rooms")));
services.AddRoomsApi();

// Module GuestAccess (phiên khách + resolve token QR). Nửa-Infra (persistence keyed + resolve use case + store
// row-lock + hasher) + nửa-Api (endpoint công khai POST /v1/guest/resolve — AllowAnonymous, cookie __Host-).
// Connection string riêng (cùng PostgreSQL, schema guest_access).
var guestAccessConnectionString = configuration.GetConnectionString("GuestAccess")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:GuestAccess — fail-fast (F35). Cấu hình connection string cho module GuestAccess.");
services.AddGuestAccessInfrastructure(options => options.UseNpgsql(
    guestAccessConnectionString,
    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "guest_access")));
services.AddGuestAccessApi(configuration);

// Module Rules (nội quy Draft→Publish + guest ack + rule-gate). Nửa-Infra (persistence + use case + IRuleGate) +
// nửa-Api admin (D-Rules.4c-1: section CRUD + translation upsert + publish, RequireStaff). Connection string riêng
// (cùng PostgreSQL, schema rules).
var rulesConnectionString = configuration.GetConnectionString("Rules")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Rules — fail-fast (F35). Cấu hình connection string cho module Rules.");
services.AddRulesInfrastructure(options => options.UseNpgsql(
    rulesConnectionString,
    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "rules")));
services.AddRulesApi();

// Module Faq (FAQ cây cha-con do lễ tân soạn + guest đọc + rule-gate). Nửa-Infra (persistence + CRUD/reorder + guest
// tree read) + nửa-Api (E-Faq.4: admin CRUD/reorder RequireStaff + guest tree AllowAnonymous). Connection string riêng
// (cùng PostgreSQL, schema faq). Faq tiêu thụ IRuleGate (Rules) + IResortGuestConfigQuery/ITranslationResolver (ResortConfig)
// + IHtmlSanitizer (sanitize-on-save, RequirePort đã có từ Rules — không thêm trùng).
var faqConnectionString = configuration.GetConnectionString("Faq")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Faq — fail-fast (F35). Cấu hình connection string cho module Faq.");
services.AddFaqInfrastructure(options => options.UseNpgsql(
    faqConnectionString,
    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "faq")));
services.AddFaqApi();

// Module Housekeeping (yêu cầu dọn phòng: guest tạo + staff hoàn tất app/QR + máy trạng thái + log). Nửa-Infra
// (persistence + use case + read-model) + nửa-Api (H-Hk.3: guest tạo/xem + admin board/complete/status, RequireStaff).
// Connection string riêng (cùng PostgreSQL, schema housekeeping). Tiêu thụ IRuleGate (Rules) + IRoomTokenResolver (Rooms)
// + IResortGuestConfigQuery (ResortConfig) qua Contracts. KHÔNG outbox (cascade GuestVisitEnded = C-GA.5).
var housekeepingConnectionString = configuration.GetConnectionString("Housekeeping")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Housekeeping — fail-fast (F35). Cấu hình connection string cho module Housekeeping.");
services.AddHousekeepingInfrastructure(options => options.UseNpgsql(
    housekeepingConnectionString,
    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "housekeeping")));
services.AddHousekeepingApi();

// Module Concierge (nhắn tin khách↔lễ tân + ghi chú nội bộ; module nghiệp vụ CUỐI 8/8). Nửa-Infra (persistence keyed
// + use case guest/staff/notes + read-model + notifier no-op) + nửa-Api (K-Con.3: guest gửi/đọc + admin board/reply/
// read/close + notes CRUD, RequireStaff). Connection string riêng (cùng PostgreSQL, schema concierge). Tiêu thụ IRuleGate
// (Rules) + IResortGuestConfigQuery/IResortSettingsQuery (ResortConfig) + ICurrentGuestContextResolver (GuestAccess) qua
// Contracts. KHÔNG outbox (cascade GuestVisitEnded = C-GA.5). SignalR notifier override = K-Con.4.
var conciergeConnectionString = configuration.GetConnectionString("Concierge")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Concierge — fail-fast (F35). Cấu hình connection string cho module Concierge.");
services.AddConciergeInfrastructure(options => options.UseNpgsql(
    conciergeConnectionString,
    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "concierge")));
services.AddConciergeApi();

// IHtmlSanitizer (QR-AD-031): adapter Ganss dùng chung (Rules Draft sanitize-on-save; Faq sau). RequirePort → boot
// FAIL-FAST nếu thiếu — port bảo mật KHÔNG default (thiếu = HTML script lọt vào nội dung khách). Host (composition
// root) là nơi DUY NHẤT cắm adapter; UpsertRuleSectionTranslationUseCase inject IHtmlSanitizer → phải có mặt.
services.AddStarHillHtml();
services.AddRequiredPort<Bedrock.Application.Ports.Html.IHtmlSanitizer>();

// (2b) OPT-IN messaging event-driven — mặc định TẮT (mirror opt-in migrate AD-053). Bật qua config
//      Bedrock:Messaging:Enabled=true (docker-compose đặt cờ). Khi bật: cắm adapter RabbitMQ (OVERRIDE default
//      fail-loud), đăng ký dispatcher outbox cho IdentityDbContext, LÊN LỊCH worker phát tự động (AD-056 —
//      Host quyết lịch, giữ AD-047), và build registry EventType→CLR cho consumer. Khi tắt: giữ default
//      ThrowingEventBusPublisher (không có gì tự phát) → sample host boot không cần RabbitMQ (smoke test xanh).
if (bool.TryParse(configuration["Bedrock:Messaging:Enabled"], out var messagingEnabled) && messagingEnabled)
{
    services.AddRabbitMqMessaging(configuration);
    // KEYED (P0-1): dispatcher/consumer gắn IdentityDbContext theo module key Identity → outbox/inbox chạy đúng
    // scope+DbContext module (không còn last-registration-wins). Mirror platform sample Host.
    services.AddOutboxDispatcher<IdentityDbContext>(IdentityInfrastructureExtensions.PersistenceKey);
    services.AddOutboxDispatcherWorker<IdentityDbContext>();
    services.AddIntegrationEventRegistry(typeof(UserTokenRefreshedIntegrationEvent).Assembly);

    // CONSUME side (AD-059): dispatch core agnostic (Bedrock.Infrastructure) + handler demo + subscriber RabbitMQ.
    // Topology TỐI THIỂU cho sample (queue + binding) — quyết định app (N-063). Handler chạy trong transaction
    // consume (inbox + business nguyên tử, F30). Multi-module thật: mỗi module một consumer + scope riêng.
    services.AddIntegrationEventConsumer<IdentityDbContext>(IdentityInfrastructureExtensions.PersistenceKey);
    services.AddKeyedScoped<IIntegrationEventHandler<UserTokenRefreshedIntegrationEvent>, UserTokenRefreshedLogHandler>(
        IdentityInfrastructureExtensions.PersistenceKey);
    services.AddRabbitMqConsumer(o =>
    {
        o.QueueName = "starhill.identity";
        o.DispatcherServiceKey = IdentityInfrastructureExtensions.PersistenceKey;
        o.RoutingKeys.Add("identity.#"); // nhận mọi event của module Identity (topic pattern).
    });
}

// P1-15: khi messaging TẮT, module Identity vẫn ghi outbox event (producer) nhưng KHÔNG có dispatcher worker →
// RequiredPortsValidator CHẶN boot (chống event tích lũy IM LẶNG), TRỪ KHI khai offline tường minh qua config
// Bedrock:Messaging:AllowOutboxWithoutDispatcher=true (validator đọc lúc StartAsync). Production quên messaging =
// fail-fast; dev/smoke offline = đặt cờ CÓ Ý THỨC.

// (3) Bọc pipeline behaviors SAU khi use case đã đăng ký (AD-037 — Scrutor chỉ decorate service đã có mặt).
services.AddBedrockCore();

// (4) Duplicate-guard port single-impl (F18) — chạy trước Build (báo GỘP mọi vi phạm).
services.ValidateSingleImplementationPorts();

var app = builder.Build();

// OPT-IN áp EF migration lúc start — CHỈ cho dev/compose SINGLE-INSTANCE (mặc định TẮT). Production giữ migrate
// OUT-OF-BAND (AD-050 — an toàn đa-instance; nhiều replica không được đua nhau migrate). Bật qua config
// Bedrock:ApplyMigrationsOnStartup=true (docker-compose đặt cờ này). Dùng indexer config (không cần Binder package).
if (bool.TryParse(configuration["Bedrock:ApplyMigrationsOnStartup"], out var applyMigrations) && applyMigrations)
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var identityDb = migrationScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await identityDb.Database.MigrateAsync().ConfigureAwait(false);

    // F.1c: seed admin CHỈ khi có CẢ HAI config Identity:SeedAdmin:Username/Password (dev/compose đặt; prod KHÔNG
    // cấu hình → KHÔNG seed, admin tạo out-of-band — F35/QR-AD-039). Idempotent (chạy lại không nhân đôi).
    var seedAdminUsername = configuration["Identity:SeedAdmin:Username"];
    var seedAdminPassword = configuration["Identity:SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(seedAdminUsername) && !string.IsNullOrWhiteSpace(seedAdminPassword))
    {
        var identityUserSeeder = migrationScope.ServiceProvider.GetRequiredService<IdentityUserSeeder>();
        await identityUserSeeder.SeedAdminAsync(seedAdminUsername, seedAdminPassword).ConfigureAwait(false);
    }

    // ResortConfig: migrate schema resort_config + seed idempotent (resort/settings/languages, en default — Req 12.4).
    var resortConfigDb = migrationScope.ServiceProvider.GetRequiredService<ResortConfigDbContext>();
    await resortConfigDb.Database.MigrateAsync().ConfigureAwait(false);
    var resortConfigSeeder = migrationScope.ServiceProvider.GetRequiredService<ResortConfigSeeder>();
    await resortConfigSeeder.SeedAsync(cancellationToken: default).ConfigureAwait(false);

    // Rooms: migrate schema rooms (chưa seed — phòng do admin tạo qua use case ở B-Rooms.2b/2.3).
    var roomsDb = migrationScope.ServiceProvider.GetRequiredService<RoomsDbContext>();
    await roomsDb.Database.MigrateAsync().ConfigureAwait(false);

    // GuestAccess: migrate schema guest_access (chưa seed — session/visit tạo runtime khi guest resolve).
    var guestAccessDb = migrationScope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
    await guestAccessDb.Database.MigrateAsync().ConfigureAwait(false);

    // Rules: migrate schema rules (chưa seed — nội quy do Staff soạn/publish qua endpoint admin).
    var rulesDb = migrationScope.ServiceProvider.GetRequiredService<RulesDbContext>();
    await rulesDb.Database.MigrateAsync().ConfigureAwait(false);

    // Faq: migrate schema faq (chưa seed — FAQ do Staff soạn qua endpoint admin).
    var faqDb = migrationScope.ServiceProvider.GetRequiredService<FaqDbContext>();
    await faqDb.Database.MigrateAsync().ConfigureAwait(false);

    // Housekeeping: migrate schema housekeeping (chưa seed — ticket tạo runtime bởi guest/staff).
    var housekeepingDb = migrationScope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
    await housekeepingDb.Database.MigrateAsync().ConfigureAwait(false);

    // Concierge: migrate schema concierge (chưa seed — hội thoại/tin/ghi chú tạo runtime bởi guest/staff).
    var conciergeDb = migrationScope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
    await conciergeDb.Database.MigrateAsync().ConfigureAwait(false);
}

// Slot #4 (HSTS/HTTPS-redirect) = TRÁCH NHIỆM HOST (AD-035). Sample host này chạy sau reverse-proxy terminate TLS
// (design §3.5) nên KHÔNG tự redirect; deployment thật bật UseHsts/UseHttpsRedirection khi cần.
app.UseBedrockApi();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Lộ entry point cho <c>WebApplicationFactory&lt;Program&gt;</c> (smoke test boot). Không dùng runtime.</summary>
public partial class Program;
