using GuestAccess.Contracts;
using GuestAccess.Domain;
using GuestAccess.Infrastructure.Persistence;
using NetArchTest.Rules;
using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>
/// GUARD ranh giới module (CP4) cho module <c>GuestAccess</c> (slice C-GA.1 — persistence nền, chưa có Api/Application).
/// Kiểm: Contracts thuần (chỉ Bedrock.Messaging.Contracts; không rò Domain/Infra); Domain ⊥ Infrastructure. Kèm
/// negative control chứng minh engine bắt được phụ thuộc THẬT vào Infrastructure module.
/// </summary>
public sealed class GuestAccessBoundaryTests
{
    private static System.Reflection.Assembly Contracts => typeof(GuestAccessModule).Assembly;
    private static System.Reflection.Assembly Domain => typeof(GuestVisit).Assembly;

    [Fact]
    public void Contracts_should_stay_pure()
    {
        var result = Types.InAssembly(Contracts)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "GuestAccess.Domain",
                "GuestAccess.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("GuestAccess.Contracts", result));
    }

    [Fact]
    public void Domain_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Infrastructure", "Bedrock.Api", "GuestAccess.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("GuestAccess.Domain", result));
    }

    // NEGATIVE CONTROL: type giả giữ GuestAccessDbContext (Infrastructure) → luật "ShouldNot dep Infrastructure" PHẢI bắt.
    private sealed class CrossModuleInternalLeak(GuestAccessDbContext context)
    {
        public GuestAccessDbContext Context { get; } = context;
    }

    [Fact]
    public void Control_engine_detects_dependency_on_module_infrastructure()
    {
        var result = Types.InAssembly(typeof(GuestAccessBoundaryTests).Assembly)
            .That()
            .HaveName(nameof(CrossModuleInternalLeak))
            .ShouldNot()
            .HaveDependencyOn("GuestAccess.Infrastructure")
            .GetResult();

        Assert.False(result.IsSuccessful);
        Assert.Contains(
            result.FailingTypeNames ?? [],
            name => name.Contains(nameof(CrossModuleInternalLeak), StringComparison.Ordinal));
    }

    private static string Describe(string scope, TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : $"{scope} vi phạm ranh giới module. Type vi phạm: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
