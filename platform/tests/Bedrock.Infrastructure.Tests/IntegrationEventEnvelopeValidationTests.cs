using System.Text.Json;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// A-10 — validate envelope ở CONSUME (agnostic dispatcher, mọi transport hưởng lợi). Khoá 3 hành vi:
/// (1) content-type lạ/thiếu → <see cref="InboxDispatchOutcome.DeadLettered"/> (quarantine) TRƯỚC deserialize;
/// (2) id trong payload lệch <c>MessageId</c> envelope → ném (integrity, redeliver/DLX); (3) positive control JSON hợp lệ → Handled.
/// SQLite harness (KHÔNG Docker) — dispatcher là cơ chế agnostic, không cần Postgres.
/// </summary>
public sealed class IntegrationEventEnvelopeValidationTests
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

    [Fact]
    public async Task Non_json_content_type_is_dead_lettered_before_handler()
    {
        var handler = new CountingHandler();
        await using var harness = await CreateHarnessAsync(handler);
        var id = Guid.CreateVersion7();
        var message = new IncomingIntegrationMessage(id, "test-consumer", "test.thing_happened", PayloadFor(id))
        {
            ContentType = "application/octet-stream", // KHÔNG phải JSON → quarantine.
        };

        var outcome = await DispatchAsync(harness, message);

        Assert.Equal(InboxDispatchOutcome.DeadLettered, outcome);
        Assert.Equal(0, handler.Count); // handler KHÔNG chạy (fail-fast trước transaction).
    }

    [Fact]
    public async Task Missing_content_type_is_dead_lettered()
    {
        var handler = new CountingHandler();
        await using var harness = await CreateHarnessAsync(handler);
        var id = Guid.CreateVersion7();
        var message = new IncomingIntegrationMessage(id, "test-consumer", "test.thing_happened", PayloadFor(id));
        // ContentType = null (mặc định) → envelope không hợp lệ theo hợp đồng cố định.

        var outcome = await DispatchAsync(harness, message);

        Assert.Equal(InboxDispatchOutcome.DeadLettered, outcome);
        Assert.Equal(0, handler.Count);
    }

    [Theory] // P1-02: schema-version phải >= 1; thiếu/lỗi/overflow (transport set 0) hoặc âm → quarantine, KHÔNG chạy handler.
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Invalid_schema_version_is_dead_lettered(int schemaVersion)
    {
        var handler = new CountingHandler();
        await using var harness = await CreateHarnessAsync(handler);
        var id = Guid.CreateVersion7();
        var message = new IncomingIntegrationMessage(id, "test-consumer", "test.thing_happened", PayloadFor(id))
        {
            ContentType = "application/json",
            SchemaVersion = schemaVersion,
        };

        var outcome = await DispatchAsync(harness, message);

        Assert.Equal(InboxDispatchOutcome.DeadLettered, outcome);
        Assert.Equal(0, handler.Count);
    }

    [Fact]
    public async Task Json_content_type_with_charset_parameter_is_accepted()
    {
        var handler = new CountingHandler();
        await using var harness = await CreateHarnessAsync(handler);
        var id = Guid.CreateVersion7();
        var message = new IncomingIntegrationMessage(id, "test-consumer", "test.thing_happened", PayloadFor(id))
        {
            ContentType = "application/json; charset=utf-8", // tham số bị bỏ qua → vẫn JSON.
        };

        var outcome = await DispatchAsync(harness, message);

        Assert.Equal(InboxDispatchOutcome.Handled, outcome);
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async Task Payload_id_mismatch_throws_integrity_error() // khoá integrity check (id payload == MessageId)
    {
        var handler = new CountingHandler();
        await using var harness = await CreateHarnessAsync(handler);
        var envelopeId = Guid.CreateVersion7();
        var payloadId = Guid.CreateVersion7(); // KHÁC envelopeId → không khớp.
        var message = new IncomingIntegrationMessage(envelopeId, "test-consumer", "test.thing_happened", PayloadFor(payloadId))
        {
            ContentType = "application/json",
        };

        await Assert.ThrowsAsync<JsonException>(() => DispatchAsync(harness, message));
        Assert.Equal(0, handler.Count); // rollback → handler-effect không commit.
    }
}
