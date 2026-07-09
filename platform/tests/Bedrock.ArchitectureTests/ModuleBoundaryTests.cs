using Bedrock.Application.UseCases;
using Identity.Infrastructure.Persistence;
using NetArchTest.Rules;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// Task 16.3 — kiểm BẰNG MÁY ba invariant module trên module mẫu <c>Identity</c> thật:
/// <list type="bullet">
///   <item><b>CP4 (Property 4 / I5/F30):</b> module A chạm module B CHỈ qua <c>*.Contracts</c>; Contracts thuần
///     DTO; tầng trong (Domain/Application) không rò xuống Infrastructure/Api.</item>
///   <item><b>CP5 (Property 5):</b> chỉ Host bắc cầu Api+Infrastructure → KHÔNG module nào ref cả hai
///     (module <c>.Api</c> ⊥ mọi Infrastructure; module <c>.Infrastructure</c> ⊥ mọi Api).</item>
///   <item><b>CP11:</b> use case của module ⊥ namespace <c>Messaging.Dispatch</c> (chỉ dùng <c>IOutboxWriter</c>).</item>
/// </list>
/// Mỗi luật đi kèm NEGATIVE CONTROL kiểu "phụ thuộc CÓ THẬT phải bị engine bắt" (mirror DependencyRuleTests):
/// nếu control "thành công" (không bắt) nghĩa là luật positive là false-pass → engine hỏng.
/// </summary>
public sealed class ModuleBoundaryTests
{
    // ─────────────────────────── CP4 — Module boundary ───────────────────────────

    [Fact]
    public void Module_contracts_should_stay_pure_dto()
    {
        // Identity.Contracts CHỈ được phụ thuộc kernel event trung tính (Bedrock.Messaging.Contracts) —
        // KHÔNG trỏ lên Application/Infra/Api, KHÔNG chạm tầng trong của chính module (giữ "DTO thuần" — AD-017/I5).
        var result = Types.InAssembly(ModuleAssemblies.IdentityContracts)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Identity.Domain",
                "Identity.Application",
                "Identity.Infrastructure",
                "Identity.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Identity.Contracts", result));
    }

    [Fact]
    public void Control_engine_detects_contracts_dependency_on_messaging_kernel()
    {
        // Identity.Contracts CHẮC CHẮN phụ thuộc Bedrock.Messaging.Contracts (base IntegrationEvent) →
        // luật "NotHaveDependencyOn(...Messaging.Contracts)" PHẢI thất bại. Chứng minh luật purity trên không false-pass.
        var result = Types.InAssembly(ModuleAssemblies.IdentityContracts)
            .Should()
            .NotHaveDependencyOn("Bedrock.Messaging.Contracts")
            .GetResult();

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void Module_domain_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(ModuleAssemblies.IdentityDomain)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Identity.Infrastructure",
                "Identity.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Identity.Domain", result));
    }

    [Fact]
    public void Module_application_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(ModuleAssemblies.IdentityApplication)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Identity.Infrastructure",
                "Identity.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Identity.Application", result));
    }

    // NEGATIVE CONTROL CP4: engine phát hiện phụ thuộc vào INTERNAL (non-Contracts) của một module.
    // Type giả dưới đây giữ IdentityDbContext (thuộc Identity.Infrastructure — internal module) → luật
    // "ShouldNot HaveDependencyOn(Identity.Infrastructure)" PHẢI bắt. Chứng minh cơ chế "chỉ Contracts" kiểm được.
    private sealed class CrossModuleInternalLeak(IdentityDbContext context)
    {
        public IdentityDbContext Context { get; } = context;
    }

    [Fact]
    public void Control_rule_detects_dependency_on_module_internal()
    {
        var result = Types.InAssembly(typeof(ModuleBoundaryTests).Assembly)
            .That()
            .HaveName(nameof(CrossModuleInternalLeak))
            .ShouldNot()
            .HaveDependencyOn("Identity.Infrastructure")
            .GetResult();

        Assert.False(result.IsSuccessful);
        Assert.Contains(
            result.FailingTypeNames ?? [],
            name => name.Contains(nameof(CrossModuleInternalLeak), StringComparison.Ordinal));
    }

    // ─────────────────────── CP5 — Single composition root ───────────────────────

    [Fact]
    public void Module_api_should_not_depend_on_any_infrastructure()
    {
        // matrix §3.3: Modules.<M>.Api cấm Infrastructure của BẤT KỲ module nào (kể cả của chính mình + Bedrock).
        // → không module Api nào bắc cầu Api+Infra; chỉ Host được (composition root duy nhất).
        var result = Types.InAssembly(ModuleAssemblies.IdentityApi)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Infrastructure", "Identity.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Identity.Api", result));
    }

    [Fact]
    public void Control_engine_detects_api_dependency_on_bedrock_api()
    {
        // Identity.Api CHẮC CHẮN phụ thuộc Bedrock.Api (IEndpointModule/ProblemDetailsBuilder) → luật
        // "NotHaveDependencyOn(Bedrock.Api)" PHẢI thất bại → luật Api⊥Infra ở trên không false-pass.
        var result = Types.InAssembly(ModuleAssemblies.IdentityApi)
            .Should()
            .NotHaveDependencyOn("Bedrock.Api")
            .GetResult();

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void Module_infrastructure_should_not_depend_on_any_api()
    {
        // matrix §3.3: Modules.<M>.Infrastructure cấm Api (Bedrock.Api + Api của mọi module).
        var result = Types.InAssembly(ModuleAssemblies.IdentityInfrastructure)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Api", "Identity.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Identity.Infrastructure", result));
    }

    [Fact]
    public void Control_engine_detects_infrastructure_dependency_on_bedrock_infrastructure()
    {
        // Identity.Infrastructure CHẮC CHẮN phụ thuộc Bedrock.Infrastructure (PlatformDbContext/helpers) →
        // luật "NotHaveDependencyOn(Bedrock.Infrastructure)" PHẢI thất bại → luật Infra⊥Api ở trên không false-pass.
        var result = Types.InAssembly(ModuleAssemblies.IdentityInfrastructure)
            .Should()
            .NotHaveDependencyOn("Bedrock.Infrastructure")
            .GetResult();

        Assert.False(result.IsSuccessful);
    }

    // ─────────────────────── CP11 — use case ⊥ Messaging.Dispatch ───────────────────────

    [Fact]
    public void Module_use_cases_should_not_depend_on_messaging_dispatch()
    {
        // RefreshAccessTokenUseCase (IUseCase<,>) chỉ được thấy IOutboxWriter, KHÔNG namespace *.Messaging.Dispatch.
        var result = Types.InAssembly(ModuleAssemblies.IdentityApplication)
            .That()
            .ImplementInterface(typeof(IUseCase))
            .ShouldNot()
            .HaveDependencyOn("Bedrock.Application.Messaging.Dispatch")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Identity.Application use cases", result));
    }

    private static string Describe(string scope, TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : $"{scope} vi phạm ranh giới module. Type vi phạm: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
