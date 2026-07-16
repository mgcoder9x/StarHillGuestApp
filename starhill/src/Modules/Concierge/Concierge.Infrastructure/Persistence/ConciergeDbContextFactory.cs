using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concierge.Infrastructure.Persistence;

/// <summary>
/// Design-time factory cho <c>dotnet ef migrations</c> (per-module F31/§4.6). Khớp CHÍNH XÁC options runtime
/// (<c>UseNpgsql</c> + <c>MigrationsHistoryTable(..,"concierge")</c> — QR-AD-028 + <c>UseSnakeCaseNamingConvention</c>)
/// để migration không drift model. Connection string DUMMY. Mirror HousekeepingDbContextFactory/RulesDbContextFactory.
/// </summary>
public sealed class ConciergeDbContextFactory : IDesignTimeDbContextFactory<ConciergeDbContext>
{
    public ConciergeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ConciergeDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=concierge_design_time;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "concierge"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ConciergeDbContext(
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
