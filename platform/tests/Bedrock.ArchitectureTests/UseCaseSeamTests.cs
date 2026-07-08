using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.UseCases;
using NetArchTest.Rules;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// CP11 / luật 6 (F25/I8): type hiện thực <c>IUseCase</c> KHÔNG được phụ thuộc namespace
/// <c>*.Messaging.Dispatch</c> (chỉ được dùng <c>IOutboxWriter</c>). Kiểm được BẰNG MÁY nhờ tách namespace.
/// </summary>
public sealed class UseCaseSeamTests
{
    [Fact]
    public void No_use_case_in_application_should_depend_on_messaging_dispatch()
    {
        var result = Types.InAssembly(CoreAssemblies.Application)
            .That()
            .ImplementInterface(typeof(IUseCase))
            .ShouldNot()
            .HaveDependencyOn("Bedrock.Application.Messaging.Dispatch")
            .GetResult();

        // Hiện chưa có use case trong Application → tập rỗng, luật đúng vacuously. Framework sẵn sàng.
        Assert.True(result.IsSuccessful);
    }

    // === NEGATIVE CONTROL: use case rò rỉ (phụ thuộc Dispatch) PHẢI bị bắt.
    // LeakyUseCase implement IUseCase và giữ IEventBusPublisher (thuộc Messaging.Dispatch) → vi phạm CP11.
    private sealed class LeakyUseCase(IEventBusPublisher publisher) : IUseCase
    {
        public Task RunAsync(CancellationToken ct) => publisher.PublishAsync(null!, ct);
    }

    [Fact]
    public void Control_rule_detects_use_case_leaking_into_dispatch()
    {
        var result = Types.InAssembly(typeof(UseCaseSeamTests).Assembly)
            .That()
            .ImplementInterface(typeof(IUseCase))
            .ShouldNot()
            .HaveDependencyOn("Bedrock.Application.Messaging.Dispatch")
            .GetResult();

        Assert.False(result.IsSuccessful);
        Assert.Contains(
            result.FailingTypeNames ?? [],
            name => name.Contains(nameof(LeakyUseCase), StringComparison.Ordinal));
    }
}
