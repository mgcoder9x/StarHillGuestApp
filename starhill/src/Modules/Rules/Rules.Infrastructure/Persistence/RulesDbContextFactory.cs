using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rules.Infrastructure.Persistence;

/// <summary>
/// Design-time factory cho <c>dotnet ef migrations</c> (per-module F31/§4.6). Khớp CHÍNH XÁC options runtime
/// (<c>UseNpgsql</c> + <c>MigrationsHistoryTable(..,"rules")</c> — QR-AD-028 + <c>UseSnakeCaseNamingConvention</c>)
/// để migration không drift model. Connection string DUMMY (migrations add không kết nối).
/// </summary>
public sealed class RulesDbContextFactory : IDesignTimeDbContextFactory<RulesDbContext>
{
    public RulesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RulesDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=rules_design_time;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "rules"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new RulesDbContext(
            options, DesignTimeStub.Instance, DesignTimeStub.Instance, DesignTimeStub.Instance);
    }

    private sealed class DesignTimeStub : IClock, ICurrentUser, IDomainEventDispatcher
    {
        public static readonly DesignTimeStub Instance = new();

        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;

        public Guid? UserId => null;

        public bool IsAuthenticated => false;

        public IReadOnlyCollection<string> Roles => [];

        public IReadOnlyCollection<string> Permissions => [];

        public Guid? TenantId => null;

        public Guid? SessionId => null;

        public bool IsInRole(string role) => false;

        public bool HasPermission(string permission) => false;

        public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
