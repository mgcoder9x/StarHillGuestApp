using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Identity.Application.RefreshToken;
using Identity.Infrastructure.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Identity.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL): chứng minh WIRING emission THẬT (AD-057, đóng N-040) mà unit test
/// (fake) + CP6 (DbContext test khác) + smoke (dừng ở validation 400) KHÔNG phủ: gọi <see cref="RefreshAccessTokenUseCase"/>
/// THẬT với store/UoW/outbox-writer EF resolve từ DI (chung một <see cref="IdentityDbContext"/>/scope) trên Postgres
/// thật → sau rotation thành công, một row <c>outbox_message</c> (EventType <c>identity.user_token_refreshed</c>,
/// payload chứa UserId, CHƯA processed) được PERSIST — đọc lại ở SCOPE MỚI (không phải change-tracker) → chứng minh
/// event thực sự vào DB CÙNG transaction rotation (outbox map đúng trong IdentityDbContext). Skip nếu thiếu Docker.
/// </summary>
[Collection(IdentityIntegrationDefinition.Name)]
public sealed class RefreshRotationEmitsEventTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private bool _available;

    public async Task InitializeAsync()
    {
        try
        {
            // Build() validate Docker và NÉM nếu thiếu → phải nằm TRONG try để catch → skip (thiếu Docker, N-012). Root-cause N-067.
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync().ConfigureAwait(false);
            _available = true;
        }
#pragma warning disable CA1031 // CỐ Ý: lỗi khởi động container ⇒ skip (thiếu Docker, N-012).
        catch (Exception)
#pragma warning restore CA1031
        {
            _available = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_available)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [SkippableFact]
    public async Task Successful_rotation_persists_integration_event_in_outbox_same_transaction()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua rotation-emit integration test.");

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddIdentityInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var migrateScope = provider.CreateAsyncScope())
        {
            await migrateScope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        }

        const string rawToken = "known-raw-refresh-token";
        var userId = Guid.CreateVersion7();

        // Seed một refresh token HỢP LỆ (hash khớp cách use case băm) trong một scope riêng.
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var store = seedScope.ServiceProvider.GetRequiredKeyedService<IRefreshTokenStore>(IdentityInfrastructureExtensions.PersistenceKey);
            var uow = seedScope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(IdentityInfrastructureExtensions.PersistenceKey);
            await store.AddAsync(new RefreshTokenSnapshot(
                Guid.CreateVersion7(), userId, Guid.CreateVersion7(),
                Sha256Hex(rawToken), DateTimeOffset.UtcNow.AddDays(30), RevokedAt: null));
            await uow.SaveChangesAsync();
        }

        // Rotation THẬT: store/uow/outbox-writer EF cùng scope (chung IdentityDbContext) → cùng transaction.
        await using (var rotateScope = provider.CreateAsyncScope())
        {
            var sp = rotateScope.ServiceProvider;
            var useCase = new RefreshAccessTokenUseCase(
                sp.GetRequiredKeyedService<IRefreshTokenStore>(IdentityInfrastructureExtensions.PersistenceKey),
                sp.GetRequiredKeyedService<IUnitOfWork>(IdentityInfrastructureExtensions.PersistenceKey),
                sp.GetRequiredService<IClock>(),
                new FakeTokenGenerator(),
                new FakeJwt(),
                sp.GetRequiredKeyedService<IOutboxWriter>(IdentityInfrastructureExtensions.PersistenceKey));

            var result = await useCase.ExecuteAsync(new RefreshTokenCommand(rawToken));
            Assert.True(result.IsSuccess);
        }

        // Đọc lại ở SCOPE MỚI (context mới, không change-tracker cũ) → chứng minh PERSIST thật trong Postgres.
        await using (var verifyScope = provider.CreateAsyncScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var messages = await db.Set<OutboxMessage>().ToListAsync();

            var evt = Assert.Single(messages);
            Assert.Equal("identity.user_token_refreshed", evt.EventType);
            Assert.Null(evt.ProcessedAt);          // chưa publish → chờ worker (AD-056).
            Assert.Null(evt.DeadLetteredAt);
            Assert.NotEqual(Guid.Empty, evt.Id);    // Id = event.Id (AD-029), sinh bởi CreateVersion7 trong use case.

            // Payload (jsonb, camelCase — AD-015) mang UserId, KHÔNG mang refresh token thô.
            var payload = JsonNode.Parse(evt.Payload)!;
            Assert.Equal(userId.ToString(), payload["userId"]!.GetValue<string>());
            Assert.DoesNotContain(rawToken, evt.Payload, StringComparison.Ordinal);
        }
    }

    private static string Sha256Hex(string raw) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    private sealed class FakeTokenGenerator : ITokenGenerator
    {
        public string NewToken(int byteLength = 32) => "new-raw-refresh-token";
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public string Issue(ClaimsIdentity identity) => "access-token";
    }
}
