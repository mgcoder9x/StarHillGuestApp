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
using StarHill.Api;

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
services.AddIdentityInfrastructure(options => options.UseIdentityNpgsql(identityConnectionString));
services.AddIdentityApi();

// (2b) OPT-IN messaging event-driven — mặc định TẮT (mirror opt-in migrate AD-053). Bật qua config
//      Bedrock:Messaging:Enabled=true (docker-compose đặt cờ). Khi bật: cắm adapter RabbitMQ (OVERRIDE default
//      fail-loud), đăng ký dispatcher outbox cho IdentityDbContext, LÊN LỊCH worker phát tự động (AD-056 —
//      Host quyết lịch, giữ AD-047), và build registry EventType→CLR cho consumer. Khi tắt: giữ default
//      ThrowingEventBusPublisher (không có gì tự phát) → sample host boot không cần RabbitMQ (smoke test xanh).
if (bool.TryParse(configuration["Bedrock:Messaging:Enabled"], out var messagingEnabled) && messagingEnabled)
{
    services.AddRabbitMqMessaging(configuration);
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
}

// Slot #4 (HSTS/HTTPS-redirect) = TRÁCH NHIỆM HOST (AD-035). Sample host này chạy sau reverse-proxy terminate TLS
// (design §3.5) nên KHÔNG tự redirect; deployment thật bật UseHsts/UseHttpsRedirection khi cần.
app.UseBedrockApi();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Lộ entry point cho <c>WebApplicationFactory&lt;Program&gt;</c> (smoke test boot). Không dùng runtime.</summary>
public partial class Program;
