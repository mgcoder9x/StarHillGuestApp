using NetArchTest.Rules;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// Dependency matrix §3.3 — phần enforceable với các assembly đã tồn tại (Domain, Messaging.Contracts,
/// Application). Luật Api⊥Infrastructure (2), Adapters (3), Module boundary (4), Host (5) HOÃN tới task
/// tương ứng (5.5/14/16.3) vì các project đó chưa tồn tại — không viết luật rỗng giả tạo.
/// </summary>
public sealed class DependencyRuleTests
{
    [Fact]
    public void Domain_should_be_zero_dependency_on_other_bedrock_layers()
    {
        var result = Types.InAssembly(CoreAssemblies.Domain)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Messaging.Contracts",
                "Bedrock.Infrastructure",
                "Bedrock.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Bedrock.Domain", result));
    }

    [Fact]
    public void MessagingContracts_should_be_zero_dependency_on_other_bedrock_layers()
    {
        var result = Types.InAssembly(CoreAssemblies.MessagingContracts)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Domain",
                "Bedrock.Infrastructure",
                "Bedrock.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Bedrock.Messaging.Contracts", result));
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_api_or_provider_sdks()
    {
        var result = Types.InAssembly(CoreAssemblies.Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Bedrock.Application", result));
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_api()
    {
        // Matrix §3.3: Infrastructure ref Application(+Domain bắc cầu), KHÔNG ref Api (Api mới là biên HTTP).
        var result = Types.InAssembly(CoreAssemblies.Infrastructure)
            .Should()
            .NotHaveDependencyOn("Bedrock.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Bedrock.Infrastructure", result));
    }

    // === NEGATIVE CONTROL: chứng minh engine THẬT SỰ phát hiện dependency có thật (không luôn trả success).
    // Bedrock.Application chắc chắn phụ thuộc Bedrock.Domain (Result/Error dùng khắp nơi) → luật
    // "NotHaveDependencyOn(Bedrock.Domain)" PHẢI thất bại. Nếu nó "thành công" nghĩa là engine hỏng.
    [Fact]
    public void Control_engine_detects_real_dependency_application_on_domain()
    {
        var result = Types.InAssembly(CoreAssemblies.Application)
            .Should()
            .NotHaveDependencyOn("Bedrock.Domain")
            .GetResult();

        Assert.False(result.IsSuccessful);
    }

    // Infrastructure chắc chắn phụ thuộc Application (impl IRepository/IUnitOfWork/IClock...) → luật
    // "NotHaveDependencyOn(Bedrock.Application)" PHẢI thất bại. Chứng minh guard Infrastructure⊥Api ở trên
    // không phải "luôn success" giả tạo.
    [Fact]
    public void Control_engine_detects_real_dependency_infrastructure_on_application()
    {
        var result = Types.InAssembly(CoreAssemblies.Infrastructure)
            .Should()
            .NotHaveDependencyOn("Bedrock.Application")
            .GetResult();

        Assert.False(result.IsSuccessful);
    }

    private static string Describe(string layer, TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : $"{layer} vi phạm dependency matrix. Type vi phạm: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
