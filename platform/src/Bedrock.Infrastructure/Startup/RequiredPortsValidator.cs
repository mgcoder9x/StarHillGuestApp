using Bedrock.Application.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bedrock.Infrastructure.Startup;

/// <summary>
/// Startup validator (fail-fast MỌI môi trường — F7/I9, CP9): kiểm mọi port bắt buộc (khai qua
/// <see cref="StartupValidationOptions.RequiredPorts"/>) đã có implementation; thiếu → chặn boot.
/// <para>
/// Sửa 2 lỗi của bản phác cũ (design §9.4): (1) port scoped KHÔNG resolve từ ROOT provider (với
/// <c>ValidateScopes=true</c> sẽ ném) → TẠO SCOPE rồi resolve; (2) báo GỘP mọi port thiếu, KHÔNG dừng ở cái đầu.
/// </para>
/// </summary>
public sealed class RequiredPortsValidator(
    IServiceScopeFactory scopeFactory,
    StartupValidationOptions options,
    IConfiguration configuration) : IHostedService
{
    /// <summary>P1-15: config key cho phép chạy offline (outbox producer không drainer) — đọc lúc StartAsync.</summary>
    public const string AllowOutboxWithoutDispatcherKey = "Bedrock:Messaging:AllowOutboxWithoutDispatcher";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        // Resolve port bắt buộc TRƯỚC (giữ nguyên hành vi/ưu tiên lỗi cũ — vd IJwtTokenService ctor ném khi key sai).
        var missing = options.RequiredPorts
            .Where(portType => scope.ServiceProvider.GetService(portType) is null)
            .Select(portType => portType.FullName ?? portType.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var violations = new List<string>();
        if (missing.Count > 0)
        {
            violations.Add("thiếu port bắt buộc: " + string.Join(", ", missing));
        }

        // P1-15: outbox producer PHẢI có dispatcher worker, nếu không event ghi vào outbox sẽ KHÔNG bao giờ được
        // phát (tích lũy IM LẶNG — rủi ro mất reaction bảo mật). Chặn boot trừ khi đã khai offline TƯỜNG MINH.
        // Đọc cờ offline TẠI ĐÂY (StartAsync, POST-build) — config từ test-host/env đã hiện (khác composition-time).
        var allowOffline = options.OutboxWithoutDispatcherAllowed
            || (bool.TryParse(configuration[AllowOutboxWithoutDispatcherKey], out var configured) && configured);
        if (!allowOffline)
        {
            var undrained = options.OutboxProducerContexts
                .Where(producer => !options.OutboxDrainerContexts.Contains(producer))
                .Select(producer => producer.FullName ?? producer.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            if (undrained.Count > 0)
            {
                violations.Add(
                    "outbox có producer nhưng KHÔNG có dispatcher worker (event sẽ tích lũy im lặng, không được phát): "
                    + string.Join(", ", undrained)
                    + " — BẬT messaging (AddOutboxDispatcherWorker) HOẶC khai offline tường minh "
                    + "(StartupValidationOptions.AllowOutboxWithoutDispatcher / config Bedrock:Messaging:AllowOutboxWithoutDispatcher=true).");
            }
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "Boot bị chặn (fail-fast mọi môi trường — F7/I9): " + string.Join(" | ", violations));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
