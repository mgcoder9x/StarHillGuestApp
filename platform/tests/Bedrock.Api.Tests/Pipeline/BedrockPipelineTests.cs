using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bedrock.Api;
using Bedrock.Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bedrock.Api.Tests.Pipeline;

public sealed class BedrockPipelineTests
{
    private const string MaskedPrefix = "/r/";

    private sealed class TestEndpoints : IEndpointModule
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/ok", () => Results.Ok("ok"));
            endpoints.MapGet("/module-ping", () => Results.Ok("pong"));
            endpoints.MapGet("/boom", Boom);
            endpoints.MapGet("/r/{token}", SecretPath);

            static IResult Boom() => throw new InvalidOperationException("kaboom");
            static IResult SecretPath(string token) => throw new InvalidOperationException("boom");
        }
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

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:MaskedPathPrefixes:0"] = MaskedPrefix,
                ["Jwt:ActiveKid"] = "k1",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:Keys:0:Kid"] = "k1",
                ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
            })
            .Build();

    private static async Task<(IHost Host, CapturingLoggerProvider Logs)> StartAsync()
    {
        var logs = new CapturingLoggerProvider();
        var builder = new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services =>
            {
                // Posture bắt buộc để đóng F15 tận gốc (AD-024): hạ log framework xuống Warning để nó KHÔNG
                // tự log raw path (Hosting "Request starting ... /r/{token}"); request-logging đã-mask của
                // Bedrock (category Bedrock.Api.*) thay thế. Bedrock giữ Trace để bắt log masked của mình.
                services.AddLogging(b => b
                    .ClearProviders()
                    .AddProvider(logs)
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddFilter("Microsoft.AspNetCore", LogLevel.Warning));
                services.AddBedrockApi(BuildConfig());
                services.AddSingleton<IEndpointModule, TestEndpoints>();
            });
            web.Configure(app => app.UseBedrockApi());
        });

        var host = await builder.StartAsync();
        return (host, logs);
    }

    [Fact]
    public async Task Liveness_and_readiness_should_return_200()
    {
        var (host, _) = await StartAsync();
        using (host)
        {
            var client = host.GetTestClient();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri("/health/live", UriKind.Relative))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri("/health/ready", UriKind.Relative))).StatusCode);
        }
    }

    [Fact]
    public async Task EndpointModule_should_be_discovered_and_mapped()
    {
        var (host, _) = await StartAsync();
        using (host)
        {
            var client = host.GetTestClient();

            var response = await client.GetAsync(new Uri("/module-ping", UriKind.Relative));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Response_should_carry_correlation_header()
    {
        var (host, _) = await StartAsync();
        using (host)
        {
            var client = host.GetTestClient();

            var response = await client.GetAsync(new Uri("/ok", UriKind.Relative));

            Assert.True(response.Headers.Contains("X-Correlation-Id"));
        }
    }

    [Fact]
    public async Task Unhandled_exception_should_return_problem_details_with_unified_correlation()
    {
        var (host, _) = await StartAsync();
        using (host)
        {
            var client = host.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/boom", UriKind.Relative));
            request.Headers.Add("X-Correlation-Id", "client-corr-1");

            var response = await client.SendAsync(request);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var headerId = response.Headers.GetValues("X-Correlation-Id").Single();
            var traceId = problem.GetProperty("traceId").GetString();

            // CP10: header == body.traceId == correlation đã chốt (client cung cấp).
            Assert.Equal("client-corr-1", headerId);
            Assert.Equal(headerId, traceId);
            Assert.Equal("unexpected", problem.GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task Sensitive_path_must_be_masked_in_logs()
    {
        var (host, logs) = await StartAsync();
        using (host)
        {
            var client = host.GetTestClient();

            await client.GetAsync(new Uri("/r/SUPERSECRETTOKEN", UriKind.Relative));

            var all = logs.Messages.ToArray();
            // CP13/F15: log chứa path đã mask, KHÔNG chứa token thật (kể cả trên request lỗi 500).
            Assert.Contains(all, m => m.Contains("/r/***", StringComparison.Ordinal));
            Assert.DoesNotContain(all, m => m.Contains("SUPERSECRETTOKEN", StringComparison.Ordinal));
        }
    }
}
