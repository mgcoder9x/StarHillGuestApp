using Bedrock.Application.DependencyInjection;
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
    StartupValidationOptions options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var missing = options.RequiredPorts
            .Where(portType => scope.ServiceProvider.GetService(portType) is null)
            .Select(portType => portType.FullName ?? portType.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Boot bị chặn (fail-fast mọi môi trường — F7/I9): thiếu port bắt buộc: " + string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
