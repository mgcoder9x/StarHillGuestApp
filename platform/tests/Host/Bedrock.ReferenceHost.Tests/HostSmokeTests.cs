using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.ReferenceHost.Operations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bedrock.ReferenceHost.Tests;

/// <summary>
/// Factory boot Host với SECRET nạp qua test-config (KHÔNG lấy từ repo — F35/task 19): <c>appsettings.json</c>
/// chỉ chứa non-secret + placeholder (Jwt secret rỗng, connection string không mật khẩu), nên test PHẢI tự cấp
/// secret giống dev nạp qua User-Secrets. Đây là cách đúng: secret sống ngoài repo, mỗi môi trường tự cấp.
/// </summary>
public sealed class SecretInjectingHostFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Secret HS256 hợp lệ (≥32 byte) — cấp lúc test như dev cấp qua User-Secrets.
            ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
            // Connection string đầy đủ (kèm password) — cấp lúc test; smoke test không chạm DB thật.
            ["ConnectionStrings:Identity"] = "Host=localhost;Port=5432;Database=bedrock_reference_identity_test;Username=postgres;Password=test",
            // P1-15: smoke test boot OFFLINE (messaging tắt) → khai TƯỜNG MINH chấp nhận outbox không drainer,
            // nếu không startup guard sẽ chặn boot (đúng ý đồ fail-fast production). Đây là dev/smoke có ý thức.
            ["Bedrock:Messaging:AllowOutboxWithoutDispatcher"] = "true",
        }));
    }
}

/// <summary>
/// Smoke test COMPOSITION ROOT (task 16.2/19, R2/R25): boot Host thật qua <see cref="WebApplicationFactory{TEntryPoint}"/>
/// với secret nạp NGOÀI repo. Chứng minh compose ráp được + fail-fast DI + RequiredPortsValidator + duplicate-guard
/// PASS lúc boot; endpoint module Identity discovery/map dưới /v1 + pipeline behaviors bọc use case. Test fail-fast
/// khi THIẾU secret bắt buộc (F35) nằm cùng file.
/// </summary>
public sealed class HostSmokeTests : IClassFixture<SecretInjectingHostFactory>
{
    private readonly SecretInjectingHostFactory _factory;

    public HostSmokeTests(SecretInjectingHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task Host_boots_and_liveness_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Identity_refresh_endpoint_is_mapped_and_pipeline_validates()
    {
        var client = _factory.CreateClient();

        // Route versioned (F32): /v1/... . RefreshToken rỗng → ValidationUseCaseDecorator short-circuit → 400
        // (KHÔNG chạm DB). Chứng minh: (a) endpoint module Identity map dưới /v1; (b) pipeline behaviors bọc use case;
        // (c) Result→ProblemDetails; (d) versioning active.
        var response = await client.PostAsJsonAsync(
            new Uri("/v1/identity/token/refresh", UriKind.Relative),
            new { refreshToken = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // ReportApiVersions=true → endpoint versioned trả header liệt kê version hỗ trợ.
        Assert.True(response.Headers.Contains("api-supported-versions"));
        Assert.Contains("1.0", string.Join(",", response.Headers.GetValues("api-supported-versions")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Outbox_replay_surface_requires_operator_permission()
    {
        var client = _factory.CreateClient();

        var anonymous = await client.PostAsJsonAsync(
            "/v1/operations/outbox/replay/preview",
            new { operationId = Guid.CreateVersion7(), reason = "review", eventType = "test.event" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IssueTokenWithoutRole());
        var forbidden = await client.PostAsJsonAsync(
            "/v1/operations/outbox/replay/preview",
            new { operationId = Guid.CreateVersion7(), reason = "review", eventType = "test.event" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Authorized_operator_can_preview_and_review_replay_audit()
    {
        var operationId = Guid.CreateVersion7();
        var fake = new FakeReplayService(operationId);
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            RemoveKeyed<IOutboxReplayService>(services, Identity.Infrastructure.DependencyInjection.IdentityInfrastructureExtensions.PersistenceKey);
            RemoveKeyed<IOutboxReplayAuditReader>(services, Identity.Infrastructure.DependencyInjection.IdentityInfrastructureExtensions.PersistenceKey);
            services.AddKeyedSingleton<IOutboxReplayService>(
                Identity.Infrastructure.DependencyInjection.IdentityInfrastructureExtensions.PersistenceKey, fake);
            services.AddKeyedSingleton<IOutboxReplayAuditReader>(
                Identity.Infrastructure.DependencyInjection.IdentityInfrastructureExtensions.PersistenceKey, fake);
        }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IssueTokenWithReplayPermission());

        var preview = await client.PostAsJsonAsync(
            "/v1/operations/outbox/replay/preview",
            new { operationId, reason = "confirmed downstream fix", eventType = "test.event", maxMessages = 2 });
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.True(fake.LastRequest?.DryRun);
        Assert.Equal("confirmed downstream fix", fake.LastRequest?.Reason);
        Assert.Equal("test.event", fake.LastRequest?.EventType);
        Assert.NotEqual(Guid.Empty, fake.LastRequest?.OperationId);

        var audit = await client.GetAsync($"/v1/operations/outbox/replay/{operationId}");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        Assert.Contains(operationId.ToString(), await audit.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_fails_fast_when_required_jwt_secret_is_missing()
    {
        // Chỉ cấp connection string, KHÔNG cấp Jwt secret → appsettings để trống (F35: không secret trong repo)
        // → validate-on-start (AddBedrockSecurity) PHẢI chặn boot. Test này VỪA chứng minh fail-fast (R25.2)
        // VỪA là guard "appsettings KHÔNG chứa JWT secret" (nếu có secret thật trong repo, boot đã KHÔNG fail).
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = "Host=localhost;Database=x;Username=postgres;Password=test",
            })));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        var text = Flatten(exception);
        Assert.Contains("HS256", text, StringComparison.OrdinalIgnoreCase);
    }

    private static string Flatten(Exception exception)
    {
        var text = string.Empty;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            text += current.Message + " ";
        }

        return text;
    }

    private static string IssueTokenWithReplayPermission()
    {
        var key = new SymmetricSecurityKey(new byte[32]) { KeyId = "dev" };
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "bedrock-reference",
            Audience = "bedrock-reference-clients",
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = Guid.CreateVersion7().ToString(),
                ["permission"] = OutboxReplayAuthorization.Permission,
            },
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static string IssueTokenWithoutRole()
    {
        var key = new SymmetricSecurityKey(new byte[32]) { KeyId = "dev" };
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "bedrock-reference",
            Audience = "bedrock-reference-clients",
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { ["sub"] = Guid.CreateVersion7().ToString() },
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static void RemoveKeyed<T>(IServiceCollection services, object key)
    {
        foreach (var descriptor in services.Where(item =>
                     item.ServiceType == typeof(T)
                     && item.IsKeyedService
                     && Equals(item.ServiceKey, key)).ToArray())
        {
            services.Remove(descriptor);
        }
    }

    private sealed class FakeReplayService(Guid operationId) : IOutboxReplayService, IOutboxReplayAuditReader
    {
        public OutboxReplayRequest? LastRequest { get; private set; }

        public Task<OutboxReplayResult> ExecuteAsync(OutboxReplayRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(new OutboxReplayResult(
                request.OperationId,
                request.DryRun,
                [new OutboxReplayCandidate(Guid.CreateVersion7(), "test.event", DateTimeOffset.UtcNow.AddMinutes(-2), DateTimeOffset.UtcNow.AddMinutes(-1), 2, "poison")],
                request.DryRun ? 0 : 1));
        }

        public Task<OutboxReplayAuditRecord?> GetAsync(Guid requestedOperationId, CancellationToken ct = default) =>
            Task.FromResult<OutboxReplayAuditRecord?>(requestedOperationId == operationId
                ? new OutboxReplayAuditRecord(operationId, "operator", "review", DateTimeOffset.UtcNow, 1, [])
                : null);
    }
}
