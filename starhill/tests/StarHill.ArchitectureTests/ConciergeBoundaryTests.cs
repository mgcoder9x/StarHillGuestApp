using Concierge.Application;
using Concierge.Contracts;
using Concierge.Domain;
using Concierge.Infrastructure.Persistence;
using NetArchTest.Rules;
using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>
/// GUARD ranh giới module (CP4) cho module <c>Concierge</c>. Kiểm: Contracts thuần (chỉ Bedrock.Messaging.Contracts;
/// KHÔNG rò Domain/Infra, KHÔNG coupling Contracts module khác); Domain ⊥ Infrastructure; Application ⊥ Infra/EF/
/// ASP.NET/SignalR (I7 — use case bắt exception TRUNG LẬP + realtime qua port, không chạm IHubContext). Negative
/// control chứng minh engine bắt phụ thuộc thật.
/// </summary>
public sealed class ConciergeBoundaryTests
{
    private static System.Reflection.Assembly Contracts => typeof(ConciergeModule).Assembly;
    private static System.Reflection.Assembly Domain => typeof(Conversation).Assembly;
    private static System.Reflection.Assembly Application => typeof(SendGuestMessageUseCase).Assembly;

    [Fact]
    public void Contracts_should_stay_pure()
    {
        var result = Types.InAssembly(Contracts)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Concierge.Domain",
                "Concierge.Infrastructure",
                "GuestAccess.Contracts",
                "ResortConfig.Contracts",
                "Rules.Contracts",
                "Rooms.Contracts",
                "Housekeeping.Contracts")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Concierge.Contracts", result));
    }

    [Fact]
    public void Domain_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Infrastructure", "Bedrock.Api", "Concierge.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Concierge.Domain", result));
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Concierge.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Microsoft.AspNetCore.SignalR")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Concierge.Application", result));
    }

    // NEGATIVE CONTROL: type giả giữ ConciergeDbContext (Infrastructure) → luật "ShouldNot dep Infrastructure" PHẢI bắt.
    private sealed class CrossModuleInternalLeak(ConciergeDbContext context)
    {
        public ConciergeDbContext Context { get; } = context;
    }

    [Fact]
    public void Control_engine_detects_dependency_on_module_infrastructure()
    {
        var result = Types.InAssembly(typeof(ConciergeBoundaryTests).Assembly)
            .That()
            .HaveName(nameof(CrossModuleInternalLeak))
            .ShouldNot()
            .HaveDependencyOn("Concierge.Infrastructure")
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
