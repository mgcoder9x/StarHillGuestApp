using NetArchTest.Rules;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// CP3 (Property 3 / F29, luật 3 §3.3): <c>Adapters.*</c> chỉ được reference <c>Bedrock.Application</c>
/// (+ Domain/Messaging.Contracts bắc cầu) — KHÔNG <c>Bedrock.Api</c>/<c>Bedrock.Infrastructure</c>/module khác.
/// SDK công nghệ (RabbitMQ.Client) + resilience CHỈ sống trong adapter (I2/§17), không rò vào lõi. Kiểm trên
/// adapter mẫu <c>Adapters.Messaging.RabbitMq</c> (task 14). Negative control theo kiểu "phụ thuộc CÓ THẬT bị bắt".
/// </summary>
public sealed class AdapterIsolationTests
{
    [Fact]
    public void RabbitMq_adapter_should_only_depend_on_application_not_api_infra_or_modules()
    {
        var result = Types.InAssembly(AdapterAssemblies.RabbitMqMessaging)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Api",
                "Bedrock.Infrastructure",
                "Identity",                        // KHÔNG chạm module nào
                "Microsoft.EntityFrameworkCore",   // KHÔNG kéo EF vào adapter
                "Microsoft.AspNetCore",            // KHÔNG kéo ASP.NET vào adapter
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    // NEGATIVE CONTROL: adapter CHẮC CHẮN phụ thuộc Bedrock.Application (IEventBusPublisher + OutboxMessage) →
    // luật "NotHaveDependencyOn(Bedrock.Application)" PHẢI thất bại. Nếu "thành công" nghĩa là engine không
    // thực sự soi được dependency → luật positive ở trên là false-pass.
    [Fact]
    public void Control_engine_detects_adapter_dependency_on_application()
    {
        var result = Types.InAssembly(AdapterAssemblies.RabbitMqMessaging)
            .Should()
            .NotHaveDependencyOn("Bedrock.Application")
            .GetResult();

        Assert.False(result.IsSuccessful);
    }

    private static string Describe(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : $"Adapters.Messaging.RabbitMq vi phạm CP3. Type vi phạm: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
