using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Observability;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Impl agnostic của <see cref="IIntegrationEventDispatcher"/> (design §7.3, F30) — MIRROR consume của
/// <see cref="EfOutboxDispatcher{TContext}"/>. Một lần <see cref="DispatchAsync"/>:
/// registry resolve <c>EventType</c> → CLR type (lạ → <see cref="InboxDispatchOutcome.DeadLettered"/>, KHÔNG crash,
/// R17.3) → <see cref="IUnitOfWork.ExecuteInTransactionAsync"/>: <c>TryMarkProcessedAsync</c> (trùng →
/// <see cref="InboxDispatchOutcome.Duplicate"/>) → deserialize (options CỐ ĐỊNH giống producer — AD-015, tolerant
/// reader §9.1) → gọi MỌI <see cref="IIntegrationEventHandler{TEvent}"/> → <c>SaveChangesAsync</c> (inbox mark +
/// business của handler CÙNG một commit → đúng-một-lần, F30) → <see cref="InboxDispatchOutcome.Handled"/>.
/// <para>
/// <b>Lỗi handler → NÉM ra ngoài</b> (ExecuteInTransactionAsync rollback + rethrow) → transport NACK/redeliver
/// (khác dispatcher publish tự backoff/dead-letter per-message: consume dựa redeliver của broker + inbox khử trùng).
/// <b>Race PK inbox</b> (2 consumer đồng thời qua được kiểm tra tồn tại) → PK violation lúc SaveChanges → ném →
/// NACK → redeliver → lần sau thấy trùng → Duplicate → ACK (đúng CP8).
/// </para>
/// <para>
/// <b>Cùng scope = cùng transaction:</b> đăng ký Scoped + inject <see cref="IServiceProvider"/> của scope hiện
/// hành (adapter tạo scope MỖI message) → <c>IInboxStore</c>/handler resolve ra chia sẻ đúng <c>PlatformDbContext</c>
/// đang mở transaction. Invoker generic (KHÔNG <c>MethodInfo.Invoke</c>) giữ nguyên kiểu exception handler (AD-025).
/// </para>
/// </summary>
public sealed class EfIntegrationEventDispatcher(
    IUnitOfWork unitOfWork,
    IInboxStore inboxStore,
    IIntegrationEventTypeRegistry registry,
    IServiceProvider serviceProvider,
    object? handlerServiceKey = null) : IIntegrationEventDispatcher
{
    // Cache invoker theo kiểu event (dựng 1 lần/kiểu; lời gọi sau là virtual call, không reflection).
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    public async Task<InboxDispatchOutcome> DispatchAsync(IncomingIntegrationMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // A-29/P1-14: tạo consumer span là CON của trace GỐC (W3C traceparent + tracestate do producer lưu). Parse
        // 2-THAM-SỐ (traceParent, traceState) → GIỮ tracestate (vendor sampling) — trước đây TryParse 1 string mất
        // tracestate. Parse được → parent tường minh (xuyên bus); không → Activity.Current (ambient). StartActivity trả
        // null nếu KHÔNG có listener (không OTel) → mọi truy cập qua ?. an toàn, zero-overhead khi tắt telemetry.
        using var activity = ActivityContext.TryParse(message.TraceParent, message.TraceState, out var parentContext)
            ? BedrockTelemetry.ActivitySource.StartActivity($"consume {message.EventType}", ActivityKind.Consumer, parentContext)
            : BedrockTelemetry.ActivitySource.StartActivity($"consume {message.EventType}", ActivityKind.Consumer);
        activity?.SetTag("messaging.system", "bedrock");
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.destination.name", message.EventType);
        activity?.SetTag("messaging.message.id", message.MessageId.ToString());
        if (!string.IsNullOrEmpty(message.CorrelationId))
        {
            activity?.SetTag("bedrock.correlation_id", message.CorrelationId); // business correlation (tách trace).
        }

        var startTimestamp = Stopwatch.GetTimestamp();
        var outcomeTag = InboxMetrics.OutcomeFailed; // handler NÉM trước khi set → ghi "failed" (R24.3 consumer failure).
        try
        {
            // A-10: validate envelope TRƯỚC (fail-fast, chưa mở transaction/chưa deserialize). Content-type phải là
            // JSON theo hợp đồng cố định (AD-015/§5.2) — lạ/thiếu → quarantine (dead-letter) thay vì cố deserialize rác.
            if (!OutboxSerialization.IsJsonContentType(message.ContentType))
            {
                outcomeTag = InboxMetrics.OutcomeDeadLettered;
                return InboxDispatchOutcome.DeadLettered;
            }

            // P1-02 (AD-093): schema-version là INVARIANT — phải >= 1. Transport set 0 khi header thiếu/không phân giải
            // được/overflow (KHÔNG default 1 âm thầm) → producer hỏng/foreign bị quarantine, không giả dạng v1. Compat
            // theo từng EventType do mô hình versioning "breaking → EventType mới" đảm bảo (AD-083), không cần range ở đây.
            if (message.SchemaVersion < 1)
            {
                outcomeTag = InboxMetrics.OutcomeDeadLettered;
                return InboxDispatchOutcome.DeadLettered;
            }

            // EventType lạ → dead-letter TRƯỚC khi mở transaction (không deserialize/handle được — R17.3).
            var clrType = registry.Resolve(message.EventType);
            if (clrType is null)
            {
                outcomeTag = InboxMetrics.OutcomeDeadLettered;
                return InboxDispatchOutcome.DeadLettered;
            }

            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(clrType);
            var handlers = (handlerServiceKey is null
                    ? serviceProvider.GetServices(handlerType)
                    : serviceProvider.GetKeyedServices(handlerType, handlerServiceKey))
                .Where(handler => handler is not null)
                .ToArray();
            if (handlers.Length == 0)
            {
                outcomeTag = InboxMetrics.OutcomeDeadLettered;
                return InboxDispatchOutcome.DeadLettered;
            }

            var outcome = await unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var isFirstTime = await inboxStore
                        .TryMarkProcessedAsync(message.MessageId, message.Consumer, token)
                        .ConfigureAwait(false);
                    if (!isFirstTime)
                    {
                        return InboxDispatchOutcome.Duplicate; // idempotent skip; không stage business → commit no-op.
                    }

                    var integrationEvent = JsonSerializer.Deserialize(
                            message.Payload.Span, clrType, OutboxSerialization.Options) as IntegrationEvent
                        ?? throw new JsonException("Integration-event payload deserialized to null or an invalid type.");
                    if (integrationEvent.Id != message.MessageId)
                    {
                        throw new JsonException(
                            $"Envelope MessageId '{message.MessageId}' does not match payload Id '{integrationEvent.Id}'.");
                    }

                    var invoker = Invokers.GetOrAdd(
                        clrType,
                        static t => (HandlerInvoker)Activator.CreateInstance(
                            typeof(HandlerInvoker<>).MakeGenericType(t))!);
                    foreach (var handler in handlers)
                    {
                        if (handler is not null)
                        {
                            await invoker.InvokeAsync(handler, integrationEvent, token).ConfigureAwait(false);
                        }
                    }

                    // Flush inbox mark (staged) + business của handler CÙNG transaction → all-or-nothing (F30/CP8).
                    await unitOfWork.SaveChangesAsync(token).ConfigureAwait(false);
                    return InboxDispatchOutcome.Handled;
                },
                ct).ConfigureAwait(false);

            outcomeTag = outcome switch
            {
                InboxDispatchOutcome.Handled => InboxMetrics.OutcomeHandled,
                InboxDispatchOutcome.Duplicate => InboxMetrics.OutcomeDuplicate,
                InboxDispatchOutcome.DeadLettered => InboxMetrics.OutcomeDeadLettered,
                _ => outcomeTag,
            };
            return outcome;
        }
        finally
        {
            activity?.SetTag("messaging.outcome", outcomeTag);
            InboxMetrics.Record(outcomeTag, Stopwatch.GetElapsedTime(startTimestamp));
        }
    }

    private abstract class HandlerInvoker
    {
        public abstract Task InvokeAsync(object handler, IntegrationEvent integrationEvent, CancellationToken ct);
    }

    private sealed class HandlerInvoker<TEvent> : HandlerInvoker
        where TEvent : IntegrationEvent
    {
        public override Task InvokeAsync(object handler, IntegrationEvent integrationEvent, CancellationToken ct) =>
            ((IIntegrationEventHandler<TEvent>)handler).HandleAsync((TEvent)integrationEvent, ct);
    }
}
