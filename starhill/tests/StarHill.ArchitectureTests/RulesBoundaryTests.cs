using NetArchTest.Rules;
using Rules.Application;
using Rules.Contracts;
using Rules.Domain;
using Rules.Infrastructure.Persistence;
using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>
/// GUARD ranh giới module (CP4) cho module <c>Rules</c>. Kiểm: Contracts thuần (chỉ Bedrock.Messaging.Contracts;
/// KHÔNG rò Domain/Infra, KHÔNG coupling GuestAccess/ResortConfig Contracts — QR-TO-010); Domain ⊥ Infrastructure;
/// và (D-Rules.2b, I7) Application ⊥ Infrastructure/EF/ASP.NET — use case bắt exception TRUNG LẬP của Domain
/// (<c>UniqueConstraintViolationException</c>/<c>ConcurrencyConflictException</c>), KHÔNG chạm EF/DbUpdateException.
/// Negative control chứng minh engine bắt phụ thuộc thật.
/// </summary>
public sealed class RulesBoundaryTests
{
    private static System.Reflection.Assembly Contracts => typeof(RulesModule).Assembly;
    private static System.Reflection.Assembly Domain => typeof(RuleSet).Assembly;
    private static System.Reflection.Assembly Application => typeof(CreateRuleSectionUseCase).Assembly;

    [Fact]
    public void Contracts_should_stay_pure()
    {
        var result = Types.InAssembly(Contracts)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Rules.Domain",
                "Rules.Infrastructure",
                "GuestAccess.Contracts",
                "ResortConfig.Contracts")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Rules.Contracts", result));
    }

    [Fact]
    public void Domain_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Infrastructure", "Bedrock.Api", "Rules.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Rules.Domain", result));
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Rules.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Rules.Application", result));
    }

    // NEGATIVE CONTROL: type giả giữ RulesDbContext (Infrastructure) → luật "ShouldNot dep Infrastructure" PHẢI bắt.
    private sealed class CrossModuleInternalLeak(RulesDbContext context)
    {
        public RulesDbContext Context { get; } = context;
    }

    [Fact]
    public void Control_engine_detects_dependency_on_module_infrastructure()
    {
        var result = Types.InAssembly(typeof(RulesBoundaryTests).Assembly)
            .That()
            .HaveName(nameof(CrossModuleInternalLeak))
            .ShouldNot()
            .HaveDependencyOn("Rules.Infrastructure")
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
