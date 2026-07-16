using Concierge.Application;

namespace Concierge.Infrastructure;

/// <summary>
/// Impl NO-OP mặc định của <see cref="IConciergeRealtimeNotifier"/> — đăng ký ở <c>AddConciergeInfrastructure</c> để
/// Application chạy/test được KHÔNG cần SignalR (realtime chỉ tăng tốc; polling luôn là nguồn sự thật). Host OVERRIDE
/// bằng <c>SignalRConciergeNotifier</c> ở K-Con.4 (đăng ký SAU nên thắng — last-registration-wins cho service KHÔNG keyed).
/// </summary>
public sealed class NoOpConciergeRealtimeNotifier : IConciergeRealtimeNotifier
{
    public Task NotifyMessageReceivedAsync(Guid resortId, Guid conversationId, Guid messageId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task NotifyConversationUpdatedAsync(Guid resortId, Guid conversationId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task NotifyMessageReadAsync(Guid resortId, Guid conversationId, CancellationToken ct = default) =>
        Task.CompletedTask;
}
