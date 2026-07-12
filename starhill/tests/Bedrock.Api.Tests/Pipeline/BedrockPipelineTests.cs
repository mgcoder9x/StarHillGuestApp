using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bedrock.Api;
using Bedrock.Api.Endpoints;
using Bedrock.Api.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
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

    private static async Task<(IHost Host, CapturingLoggerProvider Logs)> StartAsync(
        Action<IServiceCollection>? configureServices = null)
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
                configureServices?.Invoke(services);
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
            // Propagation chuẩn W3C: client gửi traceparent → trace hiện hành mang traceId này (R24.2).
            const string incomingTraceId = "0af7651916cd43dd8448eb211c80319c";
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/boom", UriKind.Relative));
            request.Headers.Add("traceparent", $"00-{incomingTraceId}-b7ad6b7169203331-01");

            var response = await client.SendAsync(request);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var headerId = response.Headers.GetValues("X-Correlation-Id").Single();
            var traceId = problem.GetProperty("traceId").GetString();

            // CP10 (R24.2): header == body.traceId == trace hiện hành (= traceId propagated qua traceparent).
            Assert.Equal(incomingTraceId, headerId);
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

    [Fact]
    public async Task Readiness_returns_503_when_ready_check_unhealthy_but_liveness_stays_200()
    {
        // R34.2/DoD §16: dependency (check gắn tag "ready") down → /health/ready 503, /health/live vẫn 200
        // (orchestrator ngừng route traffic nhưng KHÔNG restart process). Kiểm bằng check "ready" cố ý Unhealthy.
        var (host, _) = await StartAsync(services => services
            .AddHealthChecks()
            .AddCheck("dependency", () => HealthCheckResult.Unhealthy("down"), tags: [HealthEndpoints.ReadyTag]));
        using (host)
        {
            var client = host.GetTestClient();

            Assert.Equal(
                HttpStatusCode.ServiceUnavailable,
                (await client.GetAsync(new Uri("/health/ready", UriKind.Relative))).StatusCode);
            Assert.Equal(
                HttpStatusCode.OK,
                (await client.GetAsync(new Uri("/health/live", UriKind.Relative))).StatusCode);
        }
    }

    [Fact]
    public async Task OpenTelemetry_providers_should_be_registered()
    {
        var (host, _) = await StartAsync();
        using (host)
        {
            // AddBedrockObservability wire đủ 3 trụ → TracerProvider + MeterProvider có mặt trong DI (R24.1).
            Assert.NotNull(host.Services.GetService<TracerProvider>());
            Assert.NotNull(host.Services.GetService<MeterProvider>());
        }
    }

    [Fact]
    public async Task Aspnetcore_request_metric_should_be_emitted()
    {
        // Trụ metrics hoạt động (R24.3 request latency/rate): ASP.NET phát instrument http.server.request.duration.
        var observed = new ConcurrentQueue<string>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (string.Equals(instrument.Meter.Name, "Microsoft.AspNetCore.Hosting", StringComparison.Ordinal))
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, _, _) => observed.Enqueue(instrument.Name));
        listener.Start();

        var (host, _) = await StartAsync();
        using (host)
        {
            await host.GetTestClient().GetAsync(new Uri("/ok", UriKind.Relative));

            // Metric ghi ở thời điểm request hoàn tất (có thể ngay sau khi GetAsync trả về) → poll ngắn tránh race.
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (!observed.Contains("http.server.request.duration") && DateTime.UtcNow < deadline)
            {
                await Task.Delay(25);
            }
        }

        Assert.Contains(observed, name => name == "http.server.request.duration");
    }
}
