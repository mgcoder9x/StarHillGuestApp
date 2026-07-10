using System.Collections.Concurrent;
using System.Text.Json;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
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
    IServiceProvider serviceProvider) : IIntegrationEventDispatcher
{
    // Cache invoker theo kiểu event (dựng 1 lần/kiểu; lời gọi sau là virtual call, không reflection).
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    public async Task<InboxDispatchOutcome> DispatchAsync(IncomingIntegrationMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // EventType lạ → dead-letter TRƯỚC khi mở transaction (không deserialize/handle được — R17.3).
        var clrType = registry.Resolve(message.EventType);
        if (clrType is null)
        {
            return InboxDispatchOutcome.DeadLettered;
        }

        return await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var isFirstTime = await inboxStore
                    .TryMarkProcessedAsync(message.MessageId, message.Consumer, token)
                    .ConfigureAwait(false);
                if (!isFirstTime)
                {
                    return InboxDispatchOutcome.Duplicate; // idempotent skip; không stage business → commit no-op.
                }

                var integrationEvent = (IntegrationEvent)JsonSerializer.Deserialize(
                    message.Payload.Span, clrType, OutboxSerialization.Options)!;

                var invoker = Invokers.GetOrAdd(
                    clrType,
                    static t => (HandlerInvoker)Activator.CreateInstance(
                        typeof(HandlerInvoker<>).MakeGenericType(t))!);
                var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(clrType);

                foreach (var handler in serviceProvider.GetServices(handlerType))
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
