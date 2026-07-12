using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Bedrock.Application.Messaging.Dispatch;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Worker LÊN LỊCH (opt-in) chạy <see cref="IOutboxDispatcher.DispatchPendingAsync"/> cho DbContext
/// <typeparamref name="TContext"/> theo chu kỳ (design §7.2 "Worker phát Outbox").
/// <para>
/// <b>Quan hệ với AD-047 (KHÔNG mâu thuẫn):</b> AD-047 loại phương án "base <i>tự chạy</i> BackgroundService"
/// để tránh HAI mô hình lịch. Worker này chỉ chạy khi Host GỌI TƯỜNG MINH
/// <c>AddOutboxDispatcherWorker&lt;TContext&gt;()</c> — <see cref="Bedrock.Infrastructure.DependencyInjection.OutboxDispatcherExtensions.AddBedrockPersistence"/>
/// KHÔNG tự đăng ký nó. Do đó lịch VẪN do Host quyết (một mô hình duy nhất); base chỉ cấp SẴN vỏ poll-loop
/// (boilerplate dễ sai) để mọi Host khỏi viết lại. Cùng tinh thần opt-in với <c>AddOutboxRetention</c>.
/// </para>
/// <para>
/// <b>Ba bất biến vận hành:</b>
/// (1) <b>Scope mỗi lượt</b> — <see cref="IOutboxDispatcher"/> là <c>Scoped</c> (dùng chung
/// <c>DbContext</c>/UoW); resolve từ <see cref="IServiceScopeFactory"/> mỗi vòng, KHÔNG từ root provider
/// (N-008 — với <c>ValidateScopes=true</c> resolve scoped-từ-root sẽ ném).
/// (2) <b>Không hạ host khi lỗi tạm</b> — DB/broker chập chờn ném ở một lượt được nuốt + log <c>Warning</c> rồi
/// poll lại lượt sau (dispatcher đã tự backoff/dead-letter per-message; lỗi ở đây là lỗi hạ tầng cả lượt).
/// (3) <b>Shutdown êm</b> — tôn trọng <c>stoppingToken</c>: hủy lúc đang delay hoặc đang dispatch thoát vòng lặp
/// sạch, không log như lỗi.
/// </para>
/// </summary>
public sealed partial class OutboxDispatcherHostedService<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<OutboxDispatcherWorkerOptions> optionsMonitor,
    ILogger<OutboxDispatcherHostedService<TContext>> logger) : BackgroundService
    where TContext : PlatformDbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = optionsMonitor.Get(OutboxDispatcherWorkerOptions.KeyFor<TContext>()).PollInterval;
        Log.WorkerStarted(logger, typeof(TContext).Name, pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await DispatchOnceAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(pollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break; // shutdown lúc đang nghỉ — thoát êm.
            }
        }

        Log.WorkerStopped(logger, typeof(TContext).Name);
    }

    private async Task DispatchOnceAsync(CancellationToken ct)
    {
#pragma warning disable CA1031 // CỐ Ý bắt rộng: lỗi hạ tầng CẢ LƯỢT (mất kết nối DB/broker) KHÔNG được hạ host —
        // log + poll lại lượt sau. Lỗi per-message đã do dispatcher xử lý (backoff/dead-letter), không tới đây.
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            // Resolve concrete generic theo TContext: nhiều module không thể rơi vào registration cuối của
            // non-generic IOutboxDispatcher (A-01).
            var dispatcher = scope.ServiceProvider.GetRequiredService<EfOutboxDispatcher<TContext>>();
            await dispatcher.DispatchPendingAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown lúc đang dispatch — không phải lỗi.
        }
        catch (Exception ex)
        {
            Log.DispatchIterationFailed(logger, typeof(TContext).Name, ex);
        }
#pragma warning restore CA1031
    }

    /// <summary>Log source-gen (delegate cache, zero-alloc khi level tắt) — tránh CA1848.</summary>
    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information,
            Message = "Outbox dispatcher worker started for {Context} (poll every {PollInterval}).")]
        public static partial void WorkerStarted(ILogger logger, string context, TimeSpan pollInterval);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information,
            Message = "Outbox dispatcher worker stopped for {Context}.")]
        public static partial void WorkerStopped(ILogger logger, string context);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
            Message = "Outbox dispatch iteration failed for {Context}; retrying next poll.")]
        public static partial void DispatchIterationFailed(ILogger logger, string context, Exception exception);
    }
}
