using Faq.Application;
using Faq.Contracts;
using Faq.Domain;
using Faq.Infrastructure.Persistence;
using NetArchTest.Rules;
using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>
/// GUARD ranh giới module (CP4) cho module <c>Faq</c>. Kiểm: Contracts thuần (chỉ Bedrock.Messaging.Contracts;
/// KHÔNG rò Domain/Infra, KHÔNG coupling Contracts module khác); Domain ⊥ Infrastructure; Application ⊥ Infra/EF/
/// ASP.NET (I7 — use case bắt exception TRUNG LẬP, không chạm DbUpdateException). Negative control chứng minh engine
/// bắt phụ thuộc thật.
/// </summary>
public sealed class FaqBoundaryTests
{
    private static System.Reflection.Assembly Contracts => typeof(FaqModule).Assembly;
    private static System.Reflection.Assembly Domain => typeof(FaqCategory).Assembly;
    private static System.Reflection.Assembly Application => typeof(CreateFaqCategoryUseCase).Assembly;

    [Fact]
    public void Contracts_should_stay_pure()
    {
        var result = Types.InAssembly(Contracts)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Faq.Domain",
                "Faq.Infrastructure",
                "GuestAccess.Contracts",
                "ResortConfig.Contracts",
                "Rules.Contracts")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Faq.Contracts", result));
    }

    [Fact]
    public void Domain_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Infrastructure", "Bedrock.Api", "Faq.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Faq.Domain", result));
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Faq.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Faq.Application", result));
    }

    // NEGATIVE CONTROL: type giả giữ FaqDbContext (Infrastructure) → luật "ShouldNot dep Infrastructure" PHẢI bắt.
    private sealed class CrossModuleInternalLeak(FaqDbContext context)
    {
        public FaqDbContext Context { get; } = context;
    }

    [Fact]
    public void Control_engine_detects_dependency_on_module_infrastructure()
    {
        var result = Types.InAssembly(typeof(FaqBoundaryTests).Assembly)
            .That()
            .HaveName(nameof(CrossModuleInternalLeak))
            .ShouldNot()
            .HaveDependencyOn("Faq.Infrastructure")
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
