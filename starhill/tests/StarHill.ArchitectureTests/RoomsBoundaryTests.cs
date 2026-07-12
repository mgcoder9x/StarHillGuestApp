using NetArchTest.Rules;
using Rooms.Contracts;
using Rooms.Domain;
using Rooms.Infrastructure.Persistence;
using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>
/// GUARD ranh giới module (CP4) cho module QR <c>Rooms</c> (slice B-Rooms.2a — chưa có Api/Application riêng).
/// Kiểm: Contracts thuần (chỉ Bedrock.Messaging.Contracts; không rò Domain/Infra); Domain ⊥ Infrastructure.
/// Kèm negative control chứng minh engine bắt được phụ thuộc THẬT.
/// </summary>
public sealed class RoomsBoundaryTests
{
    private static System.Reflection.Assembly Contracts => typeof(IRoomTokenResolver).Assembly;
    private static System.Reflection.Assembly Domain => typeof(Room).Assembly;
    private static System.Reflection.Assembly Application => typeof(Rooms.Application.CreateRoomUseCase).Assembly;

    [Fact]
    public void Contracts_should_stay_pure()
    {
        var result = Types.InAssembly(Contracts)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Application",
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Rooms.Domain",
                "Rooms.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Rooms.Contracts", result));
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        // Use case ⊥ EF/ASP.NET (I7): bắt UniqueConstraintViolationException TRUNG LẬP (Bedrock.Domain), KHÔNG
        // DbUpdateException; ánh xạ lỗi qua Result — không rò chi tiết provider/HTTP. Cross-module CHỈ qua
        // ResortConfig.Contracts (QR-AD-002/QR-DV-003) — KHÔNG chạm Domain/Application/Infrastructure của ResortConfig.
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Bedrock.Infrastructure",
                "Bedrock.Api",
                "Rooms.Infrastructure",
                "ResortConfig.Domain",
                "ResortConfig.Application",
                "ResortConfig.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Rooms.Application", result));
    }

    [Fact]
    public void Domain_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny("Bedrock.Infrastructure", "Bedrock.Api", "Rooms.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Rooms.Domain", result));
    }

    // NEGATIVE CONTROL: type giả giữ RoomsDbContext (Infrastructure) → luật "ShouldNot dep Rooms.Infrastructure" PHẢI bắt.
    private sealed class CrossModuleInternalLeak(RoomsDbContext context)
    {
        public RoomsDbContext Context { get; } = context;
    }

    [Fact]
    public void Control_engine_detects_dependency_on_module_infrastructure()
    {
        var result = Types.InAssembly(typeof(RoomsBoundaryTests).Assembly)
            .That()
            .HaveName(nameof(CrossModuleInternalLeak))
            .ShouldNot()
            .HaveDependencyOn("Rooms.Infrastructure")
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
