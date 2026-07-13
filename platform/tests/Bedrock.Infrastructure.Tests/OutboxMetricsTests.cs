using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Observability;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bedrock.Infrastructure.Tests;

// DisableParallelization: Meter "Bedrock" là static process-global → nếu test dispatch khác chạy SONG SONG sẽ
// phát cùng instrument bedrock.outbox.*/bedrock.inbox.* làm sai số đếm. Cho collection này chạy MỘT MÌNH (không
// song song với collection khác) → capture chính xác trên mọi máy (kể cả CI có Postgres dispatch tests). Tên KHÔNG
// kết thúc "Collection" (CA1711 — bài học N-060). Dùng chung cho metric outbox VÀ inbox.
[CollectionDefinition("MessagingMetricsSerial", DisableParallelization = true)]
public sealed class MessagingMetricsSerialDefinition;

/// <summary>
/// Task R24.3/§7.2 — metric quan sát Outbox phát INLINE trong dispatcher (AD-065): counter published +
/// dead_lettered + histogram publish-lag, dưới Meter chung <c>Bedrock</c>. Verify bằng <see cref="MeterListener"/>
/// trên SQLite (KHÔNG cần Docker).
/// </summary>
[Collection("MessagingMetricsSerial")]
public sealed class OutboxMetricsTests
{
    [Fact]
    public async Task Successful_dispatch_records_published_counter_and_publish_lag()
    {
        using var capture = new OutboxMetricCapture();
        await using var harness = await CreateHarnessAsync(new RecordingPublisher());

        // OccurredAt 5s trước "now" (clock cố định) → publish-lag = 5s.
        await SeedAsync(harness, occurredOffset: TimeSpan.FromSeconds(-5));
        await RunDispatcherAsync(harness);

        Assert.Equal(1, capture.Counter("bedrock.outbox.published"));
        Assert.Equal(0, capture.Counter("bedrock.outbox.dead_lettered"));
        var lags = capture.Histogram("bedrock.outbox.publish.lag");
        Assert.Single(lags);
        Assert.True(lags[0] >= 5, $"publish-lag mong đợi >= 5s, thực tế {lags[0]}s");
    }

    [Fact]
    public async Task Dead_letter_after_max_attempts_records_dead_lettered_counter()
    {
        using var capture = new OutboxMetricCapture();
        await using var harness = await CreateHarnessAsync(new FailingPublisher(), o => o.MaxAttempts = 1);

        await SeedAsync(harness, occurredOffset: TimeSpan.Zero);
        await RunDispatcherAsync(harness);

        Assert.Equal(1, capture.Counter("bedrock.outbox.dead_lettered"));
        Assert.Equal(0, capture.Counter("bedrock.outbox.published"));
    }

    [Fact]
    public async Task Ownership_changed_during_publish_records_lease_lost_instead_of_published()
    {
        using var capture = new OutboxMetricCapture();
        var publisher = new CallbackPublisher();
        await using var harness = await CreateHarnessAsync(publisher);
        await SeedAsync(harness, occurredOffset: TimeSpan.Zero);

        publisher.OnPublish = async messageId =>
        {
            await using var competingScope = harness.CreateScope();
            var competingDb = competingScope.ServiceProvider.GetRequiredService<TestDbContext>();
            await competingDb.Set<OutboxMessage>()
                .Where(message => message.Id == messageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.ClaimId, Guid.CreateVersion7())
                    .SetProperty(message => message.ClaimedUntil, harness.Clock.UtcNow.AddMinutes(5)));
        };

        await RunDispatcherAsync(harness);

        Assert.Equal(1, publisher.PublishCalls);
        Assert.Equal(1, capture.Counter("bedrock.outbox.lease_lost"));
        Assert.Equal(0, capture.Counter("bedrock.outbox.published"));
    }

    private static async Task<PersistenceHarness> CreateHarnessAsync(
        IEventBusPublisher publisher, Action<OutboxDispatcherOptions>? configure = null) =>
        await PersistenceHarness.CreateAsync(services =>
        {
            services.AddSingleton(publisher);
            services.AddOutboxDispatcher<TestDbContext>(configure);
        });

    private static async Task SeedAsync(PersistenceHarness harness, TimeSpan occurredOffset)
    {
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            EventType = "test.thing_happened",
            Payload = "{}",
            OccurredAt = harness.Clock.UtcNow + occurredOffset,
        });
        await db.SaveChangesAsync();
    }

    private static async Task RunDispatcherAsync(PersistenceHarness harness)
    {
        await using var scope = harness.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
        await dispatcher.DispatchPendingAsync();
    }
}

internal sealed class CallbackPublisher : IEventBusPublisher
{
    public Func<Guid, Task>? OnPublish { get; set; }
    public int PublishCalls { get; private set; }

    public async Task PublishAsync(OutgoingIntegrationMessage message, CancellationToken ct = default)
    {
        PublishCalls++;
        if (OnPublish is not null)
        {
            await OnPublish(message.Id);
        }
    }
}

/// <summary>Bắt measurement của các instrument <c>bedrock.outbox.*</c> trên Meter <c>Bedrock</c> qua MeterListener (BCL).</summary>
internal sealed class OutboxMetricCapture : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly ConcurrentDictionary<string, long> _counters = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, List<double>> _histograms = new(StringComparer.Ordinal);

    public OutboxMetricCapture()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == BedrockTelemetry.Name &&
                instrument.Name.StartsWith("bedrock.outbox.", StringComparison.Ordinal))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            _counters.AddOrUpdate(instrument.Name, value, (_, current) => current + value));
        _listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            var list = _histograms.GetOrAdd(instrument.Name, _ => []);
            lock (list)
            {
                list.Add(value);
            }
        });
        _listener.Start();
    }

    public long Counter(string name) => _counters.TryGetValue(name, out var v) ? v : 0;

    public IReadOnlyList<double> Histogram(string name) =>
        _histograms.TryGetValue(name, out var v) ? v : [];

    public void Dispose() => _listener.Dispose();
}
