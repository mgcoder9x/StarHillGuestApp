using System.Text.Json.Serialization;
using Adapters.Messaging.RabbitMq;
using Bedrock.Api;
using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Messaging;
using Bedrock.Infrastructure.DependencyInjection;
using Identity.Api.DependencyInjection;
using Identity.Contracts.Events;
using Identity.Infrastructure.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ResortConfig.Infrastructure.DependencyInjection;
using ResortConfig.Infrastructure.Persistence;
using Rooms.Api.DependencyInjection;
using Rooms.Infrastructure.DependencyInjection;
using Rooms.Infrastructure.Persistence;
using StarHill.Api;
using StarHill.Authorization;

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
services.AddIdentityInfrastructure(options => options.UseNpgsql(identityConnectionString));
services.AddIdentityApi();

// Module ResortConfig (nền cấu hình + i18n). Connection string riêng (cùng PostgreSQL, schema resort_config).
// Chưa có nửa-Api (endpoint admin settings) — slice B.3 (cần Identity auth). B.2: chỉ persistence + resolver + seeder.
var resortConfigConnectionString = configuration.GetConnectionString("ResortConfig")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:ResortConfig — fail-fast (F35). Cấu hình connection string cho module ResortConfig.");
services.AddResortConfigInfrastructure(options => options.UseNpgsql(resortConfigConnectionString));

// Module Rooms (phòng + token QR). Nửa-Infra (persistence) + nửa-Api (admin CRUD/QR endpoint — B-Rooms.3,
// role Admin/Staff qua StarHillPolicies).
var roomsConnectionString = configuration.GetConnectionString("Rooms")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Rooms — fail-fast (F35). Cấu hình connection string cho module Rooms.");
services.AddRoomsInfrastructure(options => options.UseNpgsql(roomsConnectionString));
services.AddRoomsApi();

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

    // ResortConfig: migrate schema resort_config + seed idempotent (resort/settings/languages, en default — Req 12.4).
    var resortConfigDb = migrationScope.ServiceProvider.GetRequiredService<ResortConfigDbContext>();
    await resortConfigDb.Database.MigrateAsync().ConfigureAwait(false);
    var resortConfigSeeder = migrationScope.ServiceProvider.GetRequiredService<ResortConfigSeeder>();
    await resortConfigSeeder.SeedAsync(cancellationToken: default).ConfigureAwait(false);

    // Rooms: migrate schema rooms (chưa seed — phòng do admin tạo qua use case ở B-Rooms.2b/2.3).
    var roomsDb = migrationScope.ServiceProvider.GetRequiredService<RoomsDbContext>();
    await roomsDb.Database.MigrateAsync().ConfigureAwait(false);
}

// Slot #4 (HSTS/HTTPS-redirect) = TRÁCH NHIỆM HOST (AD-035). Sample host này chạy sau reverse-proxy terminate TLS
// (design §3.5) nên KHÔNG tự redirect; deployment thật bật UseHsts/UseHttpsRedirection khi cần.
app.UseBedrockApi();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Lộ entry point cho <c>WebApplicationFactory&lt;Program&gt;</c> (smoke test boot). Không dùng runtime.</summary>
public partial class Program;
