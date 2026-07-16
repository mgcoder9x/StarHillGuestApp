using Housekeeping.Contracts;
using Housekeeping.Domain;
using Housekeeping.Infrastructure.Persistence;
using NetArchTest.Rules;
using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>
/// GUARD ranh giới module (CP4) cho module <c>Housekeeping</c> (H-Hk.1). Kiểm: Contracts thuần (chỉ
/// Bedrock.Messaging.Contracts; KHÔNG rò Domain/Infra, KHÔNG coupling Contracts module khác); Domain ⊥ Infrastructure.
/// (Application ⊥ Infra/EF/ASP.NET thêm ở H-Hk.2 khi Housekeeping.Application tồn tại.) Negative control chứng minh
/// engine bắt phụ thuộc thật.
/// </summary>
public sealed class HousekeepingBoundaryTests
{
    private static System.Reflection.Assembly Contracts => typeof(HousekeepingModule).Assembly;
    private static System.Reflection.Assembly Domain => typeof(HousekeepingTicket).Assembly;

    [Fact]
    public void Contracts_should_stay_pure()
    {
        var result = Types.InAssembly(Contracts)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Housekeeping.Domain",
                "Housekeeping.Infrastructure",
                "GuestAccess.Contracts",
                "ResortConfig.Contracts",
                "Rules.Contracts",
                "Rooms.Contracts")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Housekeeping.Contracts", result));
    }

    [Fact]
    public void Domain_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Infrastructure", "Bedrock.Api", "Housekeeping.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Housekeeping.Domain", result));
    }

    // NEGATIVE CONTROL: type giả giữ HousekeepingDbContext (Infrastructure) → luật "ShouldNot dep Infrastructure" PHẢI bắt.
    private sealed class CrossModuleInternalLeak(HousekeepingDbContext context)
    {
        public HousekeepingDbContext Context { get; } = context;
    }

    [Fact]
    public void Control_engine_detects_dependency_on_module_infrastructure()
    {
        var result = Types.InAssembly(typeof(HousekeepingBoundaryTests).Assembly)
            .That()
            .HaveName(nameof(CrossModuleInternalLeak))
            .ShouldNot()
            .HaveDependencyOn("Housekeeping.Infrastructure")
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
