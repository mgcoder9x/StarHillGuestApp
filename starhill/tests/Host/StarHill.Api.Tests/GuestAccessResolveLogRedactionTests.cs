using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Api;
using Bedrock.Api.Endpoints;
using Bedrock.Application.Behaviors;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using GuestAccess.Api;
using GuestAccess.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace StarHill.Api.Tests;

/// <summary>
/// Guard hồi quy Req 11.6 / QR-AD-025 (C-GA.3a): chạy resolve qua PIPELINE THẬT (Bedrock
/// <c>UseBedrockApi</c> gồm <c>RequestLoggingMiddleware</c> + endpoint thật + <c>LoggingUseCaseDecorator</c>) và
/// khẳng định raw QR token, raw cookie thiết bị và raw session key mới KHÔNG xuất hiện trong bất kỳ dòng log nào.
/// Trước đây bất biến này chỉ được lập luận "by design"; test này biến nó thành guard bắt được regression nếu ai
/// thêm log input/serialize cookie. Fake inner use case → không cần DB; decorator/middleware là mã Bedrock thật.
/// </summary>
public sealed class GuestAccessResolveLogRedactionTests
{
    private const string RawToken = "qr-capability-DO-NOT-LEAK-abcdefghijklmnopqrstuv";
    private const string RawCookie = "device-session-cookie-DO-NOT-LEAK-0123456789";
    private const string IssuedKey = "issued-raw-session-key-DO-NOT-LEAK-9876543210";

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeResolve(Result<ResolveTokenResult> result) : IUseCase<ResolveTokenInput, ResolveTokenResult>
    {
        public Task<Result<ResolveTokenResult>> ExecuteAsync(ResolveTokenInput input, CancellationToken ct = default) =>
            Task.FromResult(result);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                sink.Enqueue(formatter(state, exception));
            }
        }
    }

    private static ResolveTokenResult SampleResult(string? issuedKey) => new(
        RoomId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
        RoomNumber: "A-101",
        Building: "A",
        Floor: 1,
        ResortId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
        ResortName: "Star Hill",
        LogoUrl: null,
        VisitId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
        PortalWindowExpiresAt: new DateTimeOffset(2026, 1, 1, 12, 30, 0, TimeSpan.Zero),
        Languages: ["en", "vi"],
        DefaultLanguageCode: "en",
        FaqEnabled: true,
        ChatEnabled: true,
        HousekeepingEnabled: true,
        RequireRuleAckForFaq: true,
        RequireRuleAckForChat: true,
        RequireRuleAckForHousekeeping: true,
        IssuedSessionKey: issuedKey);

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:MaskedPathPrefixes:0"] = "/r/",
                ["Jwt:ActiveKid"] = "k1",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:Keys:0:Kid"] = "k1",
                ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
            })
            .Build();

    private static async Task<(IHost Host, CapturingLoggerProvider Logs)> StartAsync(Result<ResolveTokenResult> result)
    {
        var logs = new CapturingLoggerProvider();
        var builder = new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services =>
            {
                services.AddLogging(b => b
                    .ClearProviders()
                    .AddProvider(logs)
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddFilter("Microsoft.AspNetCore", LogLevel.Warning));
                services.AddBedrockApi(BuildConfig());
                services.AddSingleton<IClock, FixedClock>();
                services.Configure<GuestAccessOptions>(_ => { });

                // Inner use case fake, BỌC bằng LoggingUseCaseDecorator THẬT để test đúng đường log pipeline.
                services.AddSingleton<IUseCase<ResolveTokenInput, ResolveTokenResult>>(sp =>
                    new LoggingUseCaseDecorator<ResolveTokenInput, ResolveTokenResult>(
                        new FakeResolve(result),
                        sp.GetRequiredService<ILogger<LoggingUseCaseDecorator<ResolveTokenInput, ResolveTokenResult>>>()));

                services.AddSingleton<IEndpointModule, GuestAccessEndpointModule>();
            });
            web.Configure(app => app.UseBedrockApi());
        });

        var host = await builder.StartAsync();
        return (host, logs);
    }

    private static HttpRequestMessage BuildResolveRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/guest/resolve")
        {
            Content = JsonContent.Create(new { token = RawToken }),
        };
        request.Headers.Add("Cookie", $"__Host-starhill_guest={RawCookie}");
        return request;
    }

    private static void AssertNoSecretsLogged(IEnumerable<string> messages)
    {
        var joined = string.Join("\n", messages);

        // Sanity: pipeline THẬT SỰ đã log (nếu rỗng thì test vô nghĩa).
        Assert.Contains("ResolveTokenInput", joined, StringComparison.Ordinal);

        Assert.DoesNotContain(RawToken, joined, StringComparison.Ordinal);
        Assert.DoesNotContain(RawCookie, joined, StringComparison.Ordinal);
        Assert.DoesNotContain(IssuedKey, joined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Success_resolve_does_not_log_token_cookie_or_issued_key()
    {
        var (host, logs) = await StartAsync(Result.Success(SampleResult(IssuedKey)));
        using (host)
        {
            var client = host.GetTestClient();
            var response = await client.SendAsync(BuildResolveRequest());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // Raw session key MỚI phải đi ra qua Set-Cookie (đúng chức năng) nhưng KHÔNG vào log.
            Assert.Contains(
                response.Headers.GetValues("Set-Cookie"),
                v => v.Contains(IssuedKey, StringComparison.Ordinal));

            AssertNoSecretsLogged(logs.Messages);
        }
    }

    [Fact]
    public async Task Failed_resolve_does_not_log_or_echo_token_or_cookie()
    {
        var (host, logs) = await StartAsync(Result.Failure<ResolveTokenResult>(GuestAccessErrors.QrInvalid));
        using (host)
        {
            var client = host.GetTestClient();
            var response = await client.SendAsync(BuildResolveRequest());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(RawToken, body, StringComparison.Ordinal);
            Assert.DoesNotContain(RawCookie, body, StringComparison.Ordinal);
            Assert.Contains("qr_invalid", body, StringComparison.Ordinal);

            AssertNoSecretsLogged(logs.Messages);
        }
    }
}
