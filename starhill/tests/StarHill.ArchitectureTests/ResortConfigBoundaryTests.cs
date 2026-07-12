using NetArchTest.Rules;
using ResortConfig.Application.Localization;
using ResortConfig.Contracts.Localization;
using ResortConfig.Domain;
using ResortConfig.Infrastructure.Persistence;
using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>
/// GUARD ranh giới module (CP4/Property 4 — F30/I5) áp cho module QR <c>ResortConfig</c> (mirror
/// Bedrock ModuleBoundaryTests trên module Identity, nhưng ở test project SẢN PHẨM). Kiểm: Contracts thuần
/// (không rò lên Application/Infra/Api hay xuống tầng trong); Domain/Application ⊥ Infrastructure.
/// ResortConfig CHƯA có Api (slice B.3) nên luật Api⊥Infra (CP5) thêm sau. Kèm NEGATIVE CONTROL chứng minh
/// engine bắt được phụ thuộc THẬT (không false-pass).
/// </summary>
public sealed class ResortConfigBoundaryTests
{
    private static System.Reflection.Assembly Contracts => typeof(ITranslation).Assembly;
    private static System.Reflection.Assembly Domain => typeof(Resort).Assembly;
    private static System.Reflection.Assembly Application => typeof(TranslationResolver).Assembly;

    [Fact]
    public void Contracts_should_stay_pure()
    {
        // ResortConfig.Contracts KHÔNG được trỏ lên Application/Infra/Api hay chạm Domain/Infra của chính module.
        var result = Types.InAssembly(Contracts)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "ResortConfig.Domain",
                "ResortConfig.Application",
                "ResortConfig.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("ResortConfig.Contracts", result));
    }

    [Fact]
    public void Domain_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Infrastructure", "Bedrock.Api", "ResortConfig.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("ResortConfig.Domain", result));
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Infrastructure", "Bedrock.Api", "ResortConfig.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("ResortConfig.Application", result));
    }

    // NEGATIVE CONTROL: type giả giữ ResortConfigDbContext (Infrastructure — internal module) → luật
    // "ShouldNot HaveDependencyOn(ResortConfig.Infrastructure)" PHẢI bắt. Chứng minh engine không false-pass.
    private sealed class CrossModuleInternalLeak(ResortConfigDbContext context)
    {
        public ResortConfigDbContext Context { get; } = context;
    }

    [Fact]
    public void Control_engine_detects_dependency_on_module_infrastructure()
    {
        var result = Types.InAssembly(typeof(ResortConfigBoundaryTests).Assembly)
            .That()
            .HaveName(nameof(CrossModuleInternalLeak))
            .ShouldNot()
            .HaveDependencyOn("ResortConfig.Infrastructure")
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
