using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Observability;
using Bedrock.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// A-29 — TRACE XUYÊN BUS: dispatcher tạo consumer span (<c>ActivityKind.Consumer</c>) là CON của trace GỐC được
/// propagate qua <c>CorrelationId</c> (traceparent W3C mà producer lưu <c>Activity.Current.Id</c> lúc enqueue).
/// Chứng minh bằng <see cref="ActivityListener"/> (BCL, KHÔNG cần OTel SDK/Docker): span consume có cùng TraceId +
/// ParentSpanId = span gốc. Fallback: không có CorrelationId → span vẫn tạo (parent ambient), không phụ thuộc telemetry.
/// <para>
/// <b>Cách ly dưới song song:</b> listener là process-global (các test khác cũng dispatch cùng OperationName song song)
/// → KHÔNG assert theo OperationName đơn thuần; mỗi test dùng một root span DUY NHẤT và lọc theo <c>ParentSpanId</c>
/// của chính nó. Thu qua <see cref="ConcurrentBag{T}"/> (thread-safe vì callback bắn từ nhiều thread test).
/// </para>
/// </summary>
public sealed class IntegrationEventTracePropagationTests
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private sealed class CountingHandler : IIntegrationEventHandler<ThingHappened>
    {
        public int Count { get; private set; }

        public Task HandleAsync(ThingHappened integrationEvent, CancellationToken ct = default)
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    private static async Task<PersistenceHarness> CreateHarnessAsync(CountingHandler handler) =>
        await PersistenceHarness.CreateAsync(services =>
        {
            services.AddIntegrationEventRegistry(typeof(ThingHappened).Assembly);
            services.AddIntegrationEventConsumer();
            services.AddSingleton<IIntegrationEventHandler<ThingHappened>>(handler);
        });

    private static async Task<InboxDispatchOutcome> DispatchAsync(PersistenceHarness harness, IncomingIntegrationMessage message)
    {
        await using var scope = harness.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
        return await dispatcher.DispatchAsync(message);
    }

    private static byte[] PayloadFor(Guid id) =>
        JsonSerializer.SerializeToUtf8Bytes(
            new ThingHappened(id, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "x"), CamelCase);

    private static (ActivityListener Listener, ConcurrentBag<Activity> Captured) StartCapture()
    {
        var captured = new ConcurrentBag<Activity>();
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BedrockTelemetry.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = captured.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return (listener, captured);
    }

    [Fact]
    public async Task Consume_span_is_child_of_trace_context_propagated_via_correlation_id()
    {
        var (listener, captured) = StartCapture();
        using var _ = listener;

        var handler = new CountingHandler();
        await using var harness = await CreateHarnessAsync(handler);

        // Trace GỐC (W3C) — giả lập trace của request lúc enqueue; traceparent chính là Activity.Id.
        using var root = new Activity("producer-root");
        root.SetIdFormat(ActivityIdFormat.W3C);
        root.Start();
        root.TraceStateString = "bedrockvendor=p1-14"; // P1-14: tracestate (vendor sampling) — TRƯỚC ĐÂY BỊ VỨT.
        var traceParent = root.Id!;
        var traceState = root.TraceStateString;
        var rootTraceId = root.TraceId;
        var rootSpanId = root.SpanId;
        root.Stop(); // Activity.Current = null → linkage CHỈ có thể đến từ trace context propagated (chứng minh chặt).

        var id = Guid.CreateVersion7();
        var message = new IncomingIntegrationMessage(id, "test-consumer", "test.thing_happened", PayloadFor(id))
        {
            ContentType = "application/json",
            TraceParent = traceParent,
            TraceState = traceState,
        };

        var outcome = await DispatchAsync(harness, message);

        Assert.Equal(InboxDispatchOutcome.Handled, outcome);
        Assert.Equal(1, handler.Count);

        // Lọc theo ParentSpanId của root DUY NHẤT test này → tất định dù chạy song song với test khác cùng OperationName.
        var consume = Assert.Single(captured, a => a.ParentSpanId == rootSpanId);
        Assert.Equal("consume test.thing_happened", consume.OperationName);
        Assert.Equal(ActivityKind.Consumer, consume.Kind);
        Assert.Equal(rootTraceId, consume.TraceId);          // cùng trace (xuyên bus).
        Assert.Equal(traceState, consume.TraceStateString);  // P1-14: tracestate GIỮ NGUYÊN (không mất vendor state).
    }

    [Fact]
    public async Task Consume_span_falls_back_to_ambient_parent_without_correlation_id()
    {
        var (listener, captured) = StartCapture();
        using var _ = listener;

        var handler = new CountingHandler();
        await using var harness = await CreateHarnessAsync(handler);

        // Ambient root GIỮ current suốt dispatch → khi KHÔNG có CorrelationId, consume span nhận parent ambient này.
        using var ambient = new Activity("ambient-root");
        ambient.SetIdFormat(ActivityIdFormat.W3C);
        ambient.Start();
        var ambientSpanId = ambient.SpanId;

        var id = Guid.CreateVersion7();
        var message = new IncomingIntegrationMessage(id, "test-consumer", "test.thing_happened", PayloadFor(id))
        {
            ContentType = "application/json",
            // TraceParent = null → không có context propagated → fallback ambient (graceful, không ném).
        };

        var outcome = await DispatchAsync(harness, message);

        Assert.Equal(InboxDispatchOutcome.Handled, outcome);
        var consume = Assert.Single(captured, a => a.ParentSpanId == ambientSpanId);
        Assert.Equal("consume test.thing_happened", consume.OperationName);
    }
}
