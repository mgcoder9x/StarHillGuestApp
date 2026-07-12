using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Observability;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task R24.3/§7.2 — metric quan sát CONSUME phát trong <see cref="EfIntegrationEventDispatcher"/> (AD-066):
/// counter <c>bedrock.inbox.dispatched</c> (tag outcome) + histogram <c>bedrock.inbox.processing.duration</c>,
/// dưới Meter chung <c>Bedrock</c>. Verify 4 outcome (handled/duplicate/dead_lettered/failed) trên SQLite (KHÔNG Docker).
/// Cùng collection <c>MessagingMetricsSerial</c> (DisableParallelization) — Meter static process-global.
/// </summary>
[Collection("MessagingMetricsSerial")]
public sealed class InboxMetricsTests
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public async Task Handled_message_records_handled_outcome_and_duration()
    {
        var handler = new CountingHandler();
        using var capture = new InboxMetricCapture();
        await using var harness = await CreateHarnessAsync(handler);

        var outcome = await DispatchAsync(harness, NewMessage(out _));

        Assert.Equal(InboxDispatchOutcome.Handled, outcome);
        Assert.Equal(1, handler.Count);
        Assert.Equal(1, capture.Dispatched(InboxMetrics.OutcomeHandled));
        Assert.True(capture.DurationCount >= 1);
    }

    [Fact]
    public async Task Redelivered_message_records_duplicate_outcome()
    {
        var handler = new CountingHandler();
        using var capture = new InboxMetricCapture();
        await using var harness = await CreateHarnessAsync(handler);
        var message = NewMessage(out _);

        Assert.Equal(InboxDispatchOutcome.Handled, await DispatchAsync(harness, message));
        Assert.Equal(InboxDispatchOutcome.Duplicate, await DispatchAsync(harness, message)); // cùng MessageId → idempotent.

        Assert.Equal(1, capture.Dispatched(InboxMetrics.OutcomeHandled));
        Assert.Equal(1, capture.Dispatched(InboxMetrics.OutcomeDuplicate));
        Assert.Equal(1, handler.Count); // handler chạy đúng MỘT lần (CP8).
    }

    [Fact]
    public async Task Unknown_event_type_records_dead_lettered_outcome()
    {
        using var capture = new InboxMetricCapture();
        await using var harness = await CreateHarnessAsync(new CountingHandler());

        var message = new IncomingIntegrationMessage(
            Guid.CreateVersion7(), "test-consumer", "does.not.exist", "{}"u8.ToArray())
        {
            ContentType = "application/json",
        };
        var outcome = await DispatchAsync(harness, message);

        Assert.Equal(InboxDispatchOutcome.DeadLettered, outcome);
        Assert.Equal(1, capture.Dispatched(InboxMetrics.OutcomeDeadLettered));
    }

    [Fact]
    public async Task Handler_throwing_records_failed_outcome_and_rethrows()
    {
        using var capture = new InboxMetricCapture();
        await using var harness = await CreateHarnessAsync(new ThrowingHandler());

        await Assert.ThrowsAsync<InvalidOperationException>(() => DispatchAsync(harness, NewMessage(out _)));

        Assert.Equal(1, capture.Dispatched(InboxMetrics.OutcomeFailed)); // lỗi handler → outcome "failed" (R24.3).
    }

    private static async Task<PersistenceHarness> CreateHarnessAsync(IIntegrationEventHandler<ThingHappened> handler) =>
        await PersistenceHarness.CreateAsync(services =>
        {
            services.AddIntegrationEventRegistry(typeof(ThingHappened).Assembly);
            services.AddIntegrationEventConsumer();
            services.AddSingleton(handler);
        });

    private static IncomingIntegrationMessage NewMessage(out Guid id)
    {
        var evt = new ThingHappened(Guid.CreateVersion7(), new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "x");
        id = evt.Id;
        var payload = JsonSerializer.SerializeToUtf8Bytes(evt, CamelCase);
        return new IncomingIntegrationMessage(evt.Id, "test-consumer", "test.thing_happened", payload)
        {
            ContentType = "application/json",
        };
    }

    private static async Task<InboxDispatchOutcome> DispatchAsync(PersistenceHarness harness, IncomingIntegrationMessage message)
    {
        await using var scope = harness.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
        return await dispatcher.DispatchAsync(message);
    }

    private sealed class CountingHandler : IIntegrationEventHandler<ThingHappened>
    {
        public int Count { get; private set; }

        public Task HandleAsync(ThingHappened integrationEvent, CancellationToken ct = default)
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IIntegrationEventHandler<ThingHappened>
    {
        public Task HandleAsync(ThingHappened integrationEvent, CancellationToken ct = default) =>
            throw new InvalidOperationException("handler cố tình ném để test outcome=failed.");
    }
}

/// <summary>Bắt measurement <c>bedrock.inbox.*</c> trên Meter <c>Bedrock</c> qua MeterListener (BCL), tách theo tag outcome.</summary>
internal sealed class InboxMetricCapture : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly ConcurrentDictionary<string, long> _dispatched = new(StringComparer.Ordinal);
    private readonly List<double> _durations = [];
    private readonly Lock _durationsLock = new();

    public InboxMetricCapture()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == BedrockTelemetry.Name &&
                instrument.Name.StartsWith("bedrock.inbox.", StringComparison.Ordinal))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (instrument.Name == "bedrock.inbox.dispatched")
            {
                var outcome = OutcomeOf(tags);
                _dispatched.AddOrUpdate(outcome, value, (_, current) => current + value);
            }
        });
        _listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (instrument.Name == "bedrock.inbox.processing.duration")
            {
                lock (_durationsLock)
                {
                    _durations.Add(value);
                }
            }
        });
        _listener.Start();
    }

    public long Dispatched(string outcome) => _dispatched.TryGetValue(outcome, out var v) ? v : 0;

    public int DurationCount
    {
        get
        {
            lock (_durationsLock)
            {
                return _durations.Count;
            }
        }
    }

    public void Dispose() => _listener.Dispose();

    private static string OutcomeOf(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == "outcome")
            {
                return tag.Value?.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
