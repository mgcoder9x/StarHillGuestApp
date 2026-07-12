using System.Collections.Concurrent;
using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Events;
using Bedrock.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Persistence;

/// <summary>
/// Dispatcher domain-event in-process mặc định (design §5.3/§7.5, R33). Với mỗi event, resolve MỌI
/// <see cref="IDomainEventHandler{TEvent}"/> đăng ký cho kiểu THỰC của event rồi gọi tuần tự.
/// <para>
/// <b>Cùng scope = cùng transaction (CP14):</b> đăng ký <b>Scoped</b> + inject <see cref="IServiceProvider"/>
/// của scope hiện hành → handler resolve ra chia sẻ đúng <c>PlatformDbContext</c> đang mở transaction, nên
/// hiệu ứng của handler enlist cùng một <c>SaveChanges</c>. Không handler → no-op (domain event là tùy chọn).
/// </para>
/// <para>
/// <b>Vì sao KHÔNG dùng <c>MethodInfo.Invoke</c>:</b> reflection-invoke bọc exception đồng bộ của handler
/// vào <c>TargetInvocationException</c>, làm mất kiểu lỗi gốc (tầng trên không map đúng). Thay bằng invoker
/// generic gọi <c>HandleAsync</c> TRỰC TIẾP → exception giữ nguyên kiểu, và nhanh hơn (không reflection mỗi lần).
/// </para>
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher, IScopedService
{
    // Cache invoker theo kiểu event (dựng 1 lần/kiểu; lời gọi sau là virtual call, không reflection).
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var domainEvent in events)
        {
            // Kiểu THỰC (không phải IDomainEvent) để khớp handler đúng loại event.
            var eventType = domainEvent.GetType();
            var invoker = Invokers.GetOrAdd(
                eventType,
                static t => (HandlerInvoker)Activator.CreateInstance(
                    typeof(HandlerInvoker<>).MakeGenericType(t))!);
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                if (handler is not null)
                {
                    await invoker.InvokeAsync(handler, domainEvent, ct).ConfigureAwait(false);
                }
            }
        }
    }

    private abstract class HandlerInvoker
    {
        public abstract Task InvokeAsync(object handler, IDomainEvent domainEvent, CancellationToken ct);
    }

    private sealed class HandlerInvoker<TEvent> : HandlerInvoker
        where TEvent : IDomainEvent
    {
        public override Task InvokeAsync(object handler, IDomainEvent domainEvent, CancellationToken ct) =>
            ((IDomainEventHandler<TEvent>)handler).HandleAsync((TEvent)domainEvent, ct);
    }
}
